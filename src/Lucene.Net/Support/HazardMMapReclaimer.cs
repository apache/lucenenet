using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

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
    /// LUCENENET specific: a lock-free reclaimer that defers the unmap of a
    /// memory-mapped region until every in-flight reader has drained, so a
    /// concurrent close can never unmap a view out from under a reader
    /// mid-dereference (the #1013 <c>AccessViolationException</c>) without the
    /// per-access native refcount that caused #1151.
    /// <para/>
    /// One reclaimer is owned by one mapping and shared by every reader that views
    /// it (a root <see cref="Store.IndexInput"/>, its clones, and any slices). Each
    /// reader calls <see cref="Register"/> once to obtain its own <see cref="Slot"/>,
    /// then brackets each pointer dereference with <c>using (slot.Enter()) { ... }</c>.
    /// <see cref="Close"/> runs the supplied unmap action once all in-flight readers
    /// have drained.
    /// <para/>
    /// The handshake is a hazard-pointer-style announce/scan: <see cref="Slot.Enter"/>
    /// bumps a reader-private depth then reads the shared closed flag, and
    /// <see cref="Close"/> publishes the flag then scans every slot's depth. With
    /// asymmetric fencing (see <see cref="Slot.Enter"/>/<see cref="Close"/>) the
    /// reader's bracket is fence-free on the hot path; the heavy barrier lives only
    /// in the rare <see cref="Close"/>.
    /// </summary>
    internal sealed class HazardMMapReclaimer
    {
        /// <summary>
        /// Per-reader state: a re-entrancy depth plus a back-reference to the owning
        /// reclaimer. One <see cref="Slot"/> per reader (root, clone, or slice); a
        /// reader is single-threaded, so only its own thread writes <see cref="Depth"/>
        /// (a concurrent <see cref="Close"/> scan only reads it).
        /// </summary>
        internal sealed class Slot
        {
            internal int Depth;
            private readonly HazardMMapReclaimer owner;

            // Test-only hook: when set, Enter parks here (inside the bracket) so a
            // test can drive a concurrent Close while this reader is mid-dereference.
            // Null on every production read; a single predictably-not-taken branch.
            internal Action OnEnterForTest;

            internal Slot(HazardMMapReclaimer owner) => this.owner = owner;

            /// <summary>
            /// Begin a dereference. Returns a <see cref="ReadScope"/> whose
            /// <c>Dispose</c> ends it; use with <c>using</c>. Throws
            /// <see cref="AlreadyClosedException"/> if the mapping is already closed.
            /// <para/>
            /// The hot read paths avoid the <c>using</c>/<c>try-finally</c> frame
            /// (which inhibits enregistration across the protected region) by calling
            /// <see cref="Enter"/> and <see cref="Exit"/> directly: the bracketed body
            /// is a single raw load that can only AVE (uncatchable) and never throws a
            /// managed exception, so a <c>finally</c> would never have anything to do.
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
            /// run anyway; the only thing a skipped <see cref="Exit"/> would cost is a
            /// stuck <c>Depth</c> that defers this mapping's unmap to process exit (a
            /// bounded native leak, never an AVE or wrong data). Do NOT add a throwing
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
                    throw AlreadyClosedException.Create(nameof(HazardMMapReclaimer), "Already closed: mapping is closed");
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
                    throw AlreadyClosedException.Create(nameof(HazardMMapReclaimer), "Already closed: mapping is closed");
                }
#endif
                if (OnEnterForTest != null)
                {
                    OnEnterForTest();
                }
            }

            // Ends a dereference (called by ReadScope.Dispose or directly by the hot
            // read paths). If this drains the last reference after a Close, runs the
            // deferred unmap; otherwise Close's own scan will reclaim once it sees
            // Depth back at 0.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Exit()
            {
#if FEATURE_MEMORYBARRIER_PROCESSWIDE
                int depth = --Depth;
#else
                int depth = Volatile.Read(ref Depth) - 1;
                Volatile.Write(ref Depth, depth);
#endif
                if (depth == 0 && owner._closed)
                {
                    owner.TryReclaim();
                }
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
        private Action _unmap;
        private int _reclaimed;

        // Upper bound on how long Close actively spins waiting for readers to drain
        // before handing the unmap off to the last reader's Exit. Reads are short,
        // so a drain is near-immediate; the bound only caps a pathological stall.
        private static readonly long _spinBoundTicks =
            (long)(TimeSpan.FromMilliseconds(100).TotalSeconds * Stopwatch.Frequency);

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
        /// Close the mapping. New <see cref="Slot.Enter"/> calls throw from now on;
        /// the supplied <paramref name="unmap"/> action runs once all in-flight
        /// readers have drained (immediately if none are active).
        /// </summary>
        public void Close(Action unmap)
        {
            _unmap = unmap;
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

            if (TryReclaim())
            {
                return; // nobody active; reclaimed inline
            }

            // Spin briefly for in-flight readers to drain; if they outlast the
            // bound, the draining reader's Exit will run the unmap instead.
            var spin = new SpinWait();
            long deadline = Stopwatch.GetTimestamp() + _spinBoundTicks;
            while (Stopwatch.GetTimestamp() < deadline)
            {
                if (TryReclaim())
                {
                    return;
                }
                spin.SpinOnce();
            }
        }

        private bool TryReclaim()
        {
            if (!_closed || AnyReaderActive())
            {
                return false;
            }

            // First caller to win the swap runs the unmap exactly once.
            if (Interlocked.Exchange(ref _reclaimed, 1) == 0)
            {
                _unmap();
                _unmap = null;
            }
            return true;
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
