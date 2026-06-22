using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

#nullable enable

namespace Lucene.Net.Support
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// LUCENENET specific: a lock-free reclaimer that runs a cleanup action on a
    /// shared resource only once every in-flight user has drained, so the cleanup can
    /// never run while a user is still touching the resource. It is a per-user drain
    /// barrier (hazard-pointer-inspired, but using a re-entrancy counter per user
    /// rather than tagging a specific pointer).
    /// <para/>
    /// In Lucene.NET it backs <see cref="Store.MMapDirectory"/>: one reclaimer per shared
    /// memory mapping. <see cref="Close"/> blocks until in-flight reads drain, then
    /// unmaps - which avoids the #1013 <c>AccessViolationException</c> (a concurrent
    /// close unmapping a view out from under a reader) without the per-access native
    /// refcount that caused #1151. It is not tied to memory mapping, though.
    /// <para/>
    /// One reclaimer is shared by every user of the resource. Each user calls
    /// <see cref="Register"/> once to obtain its own <see cref="Slot"/>, then brackets
    /// each access with <c>using (slot.Enter()) { ... }</c> (or
    /// <see cref="Slot.EnterCore"/>/<see cref="Slot.Exit"/> directly on hot paths).
    /// <see cref="Close"/> spin-waits for all in-flight users to drain and then runs
    /// the cleanup inline, so teardown is synchronous from the caller's point of view.
    /// <para/>
    /// The handshake is an announce/scan: <see cref="Slot.EnterCore"/> bumps a
    /// user-private depth then reads the shared closed flag, and <see cref="Close"/>
    /// publishes the flag then scans every slot's depth. With asymmetric fencing (see
    /// <see cref="Slot.EnterCore"/>/<see cref="Close"/>) the user's bracket is
    /// fence-free on the hot path; the heavy barrier lives only in the rare
    /// <see cref="Close"/>.
    /// </summary>
    internal sealed class DrainReclaimer
    {
        /// <summary>
        /// Per-user state: a re-entrancy depth plus a back-reference to the owning
        /// reclaimer. One <see cref="Slot"/> per user; a user is single-threaded, so
        /// only its own thread writes <see cref="Depth"/> (a concurrent
        /// <see cref="Close"/> scan only reads it).
        /// </summary>
        internal sealed class Slot
        {
            internal int Depth;
            private readonly DrainReclaimer owner;

            // Test-only hook: when set, Enter parks here (inside the bracket) so a
            // test can drive a concurrent Close while this reader is mid-dereference.
            // Null on every production read; a single predictably-not-taken branch.
            internal Action? OnEnterForTest;

            internal Slot(DrainReclaimer owner) => this.owner = owner;

            /// <summary>
            /// Begin a dereference. Returns a <see cref="ReadScope"/> whose
            /// <c>Dispose</c> ends it; use with <c>using</c>. Throws
            /// <see cref="AlreadyClosedException"/> if the resource is already closed.
            /// <para/>
            /// This is the convenient form, used where the bracket is amortized over a
            /// bulk operation. The hottest single-value paths instead call
            /// <see cref="EnterCore"/>/<see cref="Exit"/> directly to skip the
            /// <c>using</c>/<c>try-finally</c> frame (which inhibits enregistration
            /// across the protected region); see <see cref="EnterCore"/> for the
            /// invariant that makes skipping the <c>finally</c> safe there.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadScope Enter()
            {
                EnterCore();
                return new ReadScope(this);
            }

            /// <summary>
            /// Begin a dereference without allocating a scope. The caller MUST pair
            /// this with <see cref="Exit"/>, and - because the hot paths deliberately
            /// omit a <c>try-finally</c> for speed - the code between the two calls
            /// MUST be allocation-free and unable to throw a managed exception (a bare
            /// pointer read/copy qualifies). A true AccessViolation there is
            /// uncatchable and tears down the process, so a <c>finally</c> would never
            /// run anyway. A skipped <see cref="Exit"/> would leave <c>Depth</c> stuck
            /// above zero, which would make a concurrent <see cref="Close"/> spin-wait
            /// forever (it blocks until every slot drains) - so do NOT add a throwing
            /// call between <see cref="EnterCore"/> and <see cref="Exit"/>.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void EnterCore()
            {
#if FEATURE_MEMORYBARRIER_PROCESSWIDE
                // Asymmetric fencing: NO StoreLoad barrier on the hot path. Announce
                // with a plain store (this slot is reader-private) then read _closed
                // plain. The store and read may reorder on this core, but Close issues
                // a PROCESS-WIDE barrier after setting _closed, so either our Depth
                // store is visible to Close's scan or our _closed read observes true -
                // never both passing the check AND staying invisible to the scan.
                Depth++;

                if (owner._closed)
                {
                    Depth--;
                    throw AlreadyClosedException.Create(nameof(DrainReclaimer), "Already closed: mapping is closed");
                }
#else
                // No process-wide barrier on this TFM (net462 / netstandard2.0): fall
                // back to symmetric fencing - a per-read StoreLoad barrier orders the
                // announce before the _closed check. Slower, but correct everywhere.
                Volatile.Write(ref Depth, Volatile.Read(ref Depth) + 1);
                Interlocked.MemoryBarrier();

                if (owner._closed)
                {
                    Volatile.Write(ref Depth, Volatile.Read(ref Depth) - 1);
                    throw AlreadyClosedException.Create(nameof(DrainReclaimer), "Already closed: mapping is closed");
                }
#endif
                if (OnEnterForTest != null)
                {
                    OnEnterForTest();
                }
            }

            // Ends a dereference (called by ReadScope.Dispose or directly by the hot
            // read paths). Just publishes the decrement; a concurrent Close blocks
            // until it observes every slot drained and then unmaps itself, so Exit
            // never has to run the cleanup.
            // <para/>
            // The decrement MUST be a release store. The protected pointer load in the
            // caller and this Depth-- have no data dependency, so on a weakly-ordered
            // CPU (ARM64) a plain store could be globally visible BEFORE the load
            // retires - letting Close's scan see Depth==0 and unmap the page while the
            // load is still outstanding -> AVE. Volatile.Write is a release barrier:
            // no preceding memory op (incl. the load) may move after it, so the load
            // is guaranteed complete before this slot looks drained. (The asymmetric
            // process-wide barrier in Close only covers the Enter/announce side; the
            // Exit/retire side needs this release. The net462 fallback already had it.)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Exit()
            {
                Volatile.Write(ref Depth, Depth - 1);
            }
        }

        /// <summary>
        /// A stack-only read bracket. <c>Dispose</c> ends the dereference begun by
        /// <see cref="Slot.Enter"/>. Being a <c>ref struct</c>, it never heap-allocates
        /// and its <c>Dispose</c> binds at compile time, so a <c>using</c> over it
        /// inlines to a plain depth decrement with no interface dispatch.
        /// </summary>
        internal readonly ref struct ReadScope
        {
            private readonly Slot slot;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ReadScope(Slot slot) => this.slot = slot;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() => slot.Exit();
        }

        // Strong refs to every registered reader's slot. Readers (clones, slices)
        // may be GC'd before Close, but their slot stays here until the reclaimer
        // itself is collected (it dies with the mapping). A collected reader can't
        // be mid-dereference, so its slot reads Depth == 0 and the scan skips it;
        // the retained slots are tiny and bounded by the reader count.
        private readonly List<Slot> _slots = new();

        private volatile bool _closed;

        public bool IsClosed => _closed;

        /// <summary>
        /// Register a reader (a root input, a clone, or a slice) and return its slot.
        /// Each reader registers exactly once, on construction or cloning.
        /// </summary>
        public Slot Register()
        {
            var slot = new Slot(this);
            lock (_slots)
            {
                _slots.Add(slot);
            }
            return slot;
        }

        /// <summary>
        /// Close the resource. New <see cref="Slot.Enter"/> calls throw from now on,
        /// and this BLOCKS (spin-waiting) until every in-flight user has drained, then
        /// runs the supplied <paramref name="unmap"/> action inline. So when
        /// <see cref="Close"/> returns, the cleanup has definitely happened - the
        /// caller (e.g. <c>SharedMapping.Dispose</c>) gets a synchronous teardown.
        /// <para/>
        /// This is safe from deadlock under the one-input-per-thread contract: a
        /// bracket is never held across calls (each read closes its own
        /// <see cref="Slot.Enter"/>/<see cref="Slot.Exit"/> before returning), so the
        /// thread that calls <see cref="Close"/> can never itself be holding a bracket
        /// open. The wait therefore only ever blocks on OTHER threads' short reads,
        /// the same basis the JVM shared-Arena close relies on.
        /// </summary>
        public void Close(Action unmap)
        {
            _closed = true;
            // The one place the heavy synchronization lives. After this barrier, every
            // reader's prior Depth store is visible to AnyReaderActive below, and every
            // reader's next _closed read observes true - which is what lets Enter/Exit
            // run fence-free on the hot path (asymmetric / "biased" synchronization, as
            // used by hazard-pointer reclamation and the JVM's shared Arena). Without a
            // process-wide barrier the hot path fences per read instead (see Enter), so
            // a plain barrier here suffices to pair with it.
#if FEATURE_MEMORYBARRIER_PROCESSWIDE
            Interlocked.MemoryBarrierProcessWide();
#else
            Interlocked.MemoryBarrier();
#endif

            // Block until all in-flight users drain. SpinOnce escalates from a busy
            // spin to yielding, so a briefly-descheduled reader (e.g. one that page-
            // faults on a cold mmap page mid-read) does not pin a core. Reads are
            // short and bounded (a bracket spans at most one chunk copy), so this
            // returns promptly in practice.
            var spin = new SpinWait();
            while (AnyReaderActive())
            {
                spin.SpinOnce();
            }

            // No user is active and none can newly enter (_closed is published), so
            // this runs exactly once, here, with no outstanding reader.
            unmap();
        }

        private bool AnyReaderActive()
        {
            lock (_slots)
            {
                foreach (Slot slot in _slots)
                {
                    if (Volatile.Read(ref slot.Depth) > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
