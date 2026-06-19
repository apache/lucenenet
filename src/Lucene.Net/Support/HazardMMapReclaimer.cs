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
    /// LUCENENET specific: a lock-free <see cref="IMMapReclaimer"/> using a
    /// hazard-pointer-style handshake. Each reader owns a <see cref="Slot"/> with a
    /// re-entrancy <c>Depth</c>; a dereference is announced by bumping <c>Depth</c>
    /// then reading the shared <c>_closed</c> flag, and a <see cref="Close"/>
    /// publishes <c>_closed</c> then scans every slot's <c>Depth</c>. Because the
    /// announce-then-check on the reader is symmetric to the publish-then-scan on
    /// the closer (each separated by a full memory barrier), a reader that passes
    /// its <c>_closed</c> check is guaranteed visible to the closer's scan, and a
    /// closer that has not yet published is guaranteed visible to the reader's
    /// check. So the unmap never runs while a reader is mid-dereference, and it does
    /// so without a shared interlocked counter on the hot path - the common case is
    /// an uncontended write+read of one reader-private slot.
    /// </summary>
    internal sealed class HazardMMapReclaimer : IMMapReclaimer
    {
        internal sealed class Slot : IMMapReclaimer.IReaderToken
        {
            // Re-entrancy depth of in-flight dereferences for one reader. A reader
            // is single-threaded, so this is written only by its owning thread; it
            // is read by a concurrent Close's scan, hence the Volatile accesses.
            public int Depth;
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

        public IMMapReclaimer.IReaderToken Register(object input)
        {
            var slot = new Slot();
            lock (_slots)
            {
                _slots.Add(slot);
            }
            return slot;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enter(IMMapReclaimer.IReaderToken token)
        {
            var slot = (Slot)token;
#if FEATURE_MEMORYBARRIER_PROCESSWIDE
            // Asymmetric fencing: the hot path takes NO StoreLoad barrier. We announce
            // the dereference with a plain store (this slot is reader-private, so only
            // our thread writes it) then read _closed with a plain volatile read. The
            // store and the read may reorder on the reader's core, but that is safe
            // because Close (the rare side) issues a PROCESS-WIDE barrier after setting
            // _closed: that barrier flushes every core's store buffer, so either our
            // Depth store is visible to Close's scan, or our _closed read here observes
            // true. We never both pass the check AND stay invisible to the scan.
            slot.Depth++;

            if (_closed)
            {
                slot.Depth--;
                throw AlreadyClosedException.Create(nameof(HazardMMapReclaimer), "Already closed: mapping is closed");
            }
#else
            // No process-wide barrier on this TFM (net462 / netstandard2.0): fall back
            // to symmetric fencing - a per-read StoreLoad barrier orders the announce
            // before the _closed check. Slower, but correct on every reader's core.
            Volatile.Write(ref slot.Depth, Volatile.Read(ref slot.Depth) + 1);
            Interlocked.MemoryBarrier();

            if (_closed)
            {
                Volatile.Write(ref slot.Depth, Volatile.Read(ref slot.Depth) - 1);
                throw AlreadyClosedException.Create(nameof(HazardMMapReclaimer), "Already closed: mapping is closed");
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exit(IMMapReclaimer.IReaderToken token)
        {
            var slot = (Slot)token;
#if FEATURE_MEMORYBARRIER_PROCESSWIDE
            int depth = --slot.Depth;
#else
            int depth = Volatile.Read(ref slot.Depth) - 1;
            Volatile.Write(ref slot.Depth, depth);
#endif
            if (depth != 0)
            {
                return; // nested dereference still in progress on this reader
            }

            // We may be the reader that drains the last reference after a Close. If
            // Close already published, we see _closed and run the deferred unmap; if
            // not, Close's own scan will find Depth back at 0 and reclaim there.
            if (_closed)
            {
                TryReclaim();
            }
        }

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
