using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Util
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
    /// <see cref="Sorter"/> implementation based on the
    /// <a href="http://svn.python.org/projects/python/trunk/Objects/listsort.txt">TimSort</a>
    /// algorithm.
    /// <para/>This implementation is especially good at sorting partially-sorted
    /// arrays and sorts small arrays with binary sort.
    /// <para/><b>NOTE</b>:There are a few differences with the original implementation:
    /// <list type="bullet">
    ///     <item><description><a name="maxTempSlots"/>The extra amount of memory to perform merges is
    ///         configurable. This allows small merges to be very fast while large merges
    ///         will be performed in-place (slightly slower). You can make sure that the
    ///         fast merge routine will always be used by having <c>maxTempSlots</c>
    ///         equal to half of the length of the slice of data to sort.</description></item>
    ///     <item><description>Only the fast merge routine can gallop (the one that doesn't run
    ///         in-place) and it only gallops on the longest slice.</description></item>
    /// </list>
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public abstract class TimSorter : Sorter
    {
        internal const int MINRUN = 32;
        internal new const int THRESHOLD = 64;
        internal const int STACKSIZE = 40; // depends on MINRUN
        internal const int MIN_GALLOP = 7;

        internal readonly int maxTempSlots;
        internal int minRun;
        internal int to;
        internal int stackSize;
        internal int[] runEnds;

        /// <summary>
        /// Create a new <see cref="TimSorter"/>. </summary>
        /// <param name="maxTempSlots"> The <a href="#maxTempSlots">maximum amount of extra memory to run merges</a> </param>
        protected TimSorter(int maxTempSlots)
            : base()
        {
            runEnds = new int[1 + STACKSIZE];
            this.maxTempSlots = maxTempSlots;
        }

        /// <summary>
        /// Minimum run length for an array of length <paramref name="length"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int MinRun(int length)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(length >= MINRUN);
            int n = length;
            int r = 0;
            while (n >= 64)
            {
                r |= n & 1;
                n = n.TripleShift(1);
            }
            int minRun = n + r;
            if (Debugging.AssertsEnabled) Debugging.Assert(minRun >= MINRUN && minRun <= THRESHOLD);
            return minRun;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int RunLen(int i)
        {
            int off = stackSize - i;
            return runEnds[off] - runEnds[off - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int RunBase(int i)
        {
            return runEnds[stackSize - i - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int RunEnd(int i) // LUCENENET TODO: API - change to indexer
        {
            return runEnds[stackSize - i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void SetRunEnd(int i, int runEnd)
        {
            runEnds[stackSize - i] = runEnd;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void PushRunLen(int len)
        {
            runEnds[stackSize + 1] = runEnds[stackSize] + len;
            ++stackSize;
        }

        /// <summary>
        /// Compute the length of the next run, make the run sorted and return its
        /// length.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int NextRun()
        {
            int runBase = RunEnd(0);
            if (Debugging.AssertsEnabled) Debugging.Assert(runBase < to);
            if (runBase == to - 1)
            {
                return 1;
            }
            int o = runBase + 2;
            if (Compare(runBase, runBase + 1) > 0)
            {
                // run must be strictly descending
                while (o < to && Compare(o - 1, o) > 0)
                {
                    ++o;
                }
                Reverse(runBase, o);
            }
            else
            {
                // run must be non-descending
                while (o < to && Compare(o - 1, o) <= 0)
                {
                    ++o;
                }
            }
            int runHi = Math.Max(o, Math.Min(to, runBase + minRun));
            BinarySort(runBase, runHi, o);
            return runHi - runBase;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void EnsureInvariants()
        {
            while (stackSize > 1)
            {
                int runLen0 = RunLen(0);
                int runLen1 = RunLen(1);

                if (stackSize > 2)
                {
                    int runLen2 = RunLen(2);

                    if (runLen2 <= runLen1 + runLen0)
                    {
                        // merge the smaller of 0 and 2 with 1
                        if (runLen2 < runLen0)
                        {
                            MergeAt(1);
                        }
                        else
                        {
                            MergeAt(0);
                        }
                        continue;
                    }
                }

                if (runLen1 <= runLen0)
                {
                    MergeAt(0);
                    continue;
                }

                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void ExhaustStack()
        {
            while (stackSize > 1)
            {
                MergeAt(0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void Reset(int from, int to)
        {
            stackSize = 0;
            Arrays.Fill(runEnds, 0);
            runEnds[0] = from;
            this.to = to;
            int length = to - from;
            this.minRun = length <= THRESHOLD ? length : MinRun(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void MergeAt(int n)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(stackSize >= 2);
            Merge(RunBase(n + 1), RunBase(n), RunEnd(n));
            for (int j = n + 1; j > 0; --j)
            {
                SetRunEnd(j, RunEnd(j - 1));
            }
            --stackSize;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal virtual void Merge(int lo, int mid, int hi)
        {
            if (Compare(mid - 1, mid) <= 0)
            {
                return;
            }
            lo = Upper2(lo, mid, mid);
            hi = Lower2(mid, hi, mid - 1);

            if (hi - mid <= mid - lo && hi - mid <= maxTempSlots)
            {
                MergeHi(lo, mid, hi);
            }
            else if (mid - lo <= maxTempSlots)
            {
                MergeLo(lo, mid, hi);
            }
            else
            {
                MergeInPlace(lo, mid, hi);
            }
        }

        /// <summary>
        /// Sort the slice which starts at <paramref name="from"/> (inclusive) and ends at
        /// <paramref name="to"/> (exclusive).
        /// </summary>
        public override void Sort(int from, int to)
        {
            CheckRange(from, to);
            if (to - from <= 1)
            {
                return;
            }
            Reset(from, to);
            do
            {
                EnsureInvariants();
                PushRunLen(NextRun());
            } while (RunEnd(0) < to);
            ExhaustStack();
            if (Debugging.AssertsEnabled) Debugging.Assert(RunEnd(0) == to);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override void DoRotate(int lo, int mid, int hi)
        {
            int len1 = mid - lo;
            int len2 = hi - mid;
            if (len1 == len2)
            {
                while (mid < hi)
                {
                    Swap(lo++, mid++);
                }
            }
            else if (len2 < len1 && len2 <= maxTempSlots)
            {
                Save(mid, len2);
                for (int i = lo + len1 - 1, j = hi - 1; i >= lo; --i, --j)
                {
                    Copy(i, j);
                }
                for (int i = 0, j = lo; i < len2; ++i, ++j)
                {
                    Restore(i, j);
                }
            }
            else if (len1 <= maxTempSlots)
            {
                Save(lo, len1);
                for (int i = mid, j = lo; i < hi; ++i, ++j)
                {
                    Copy(i, j);
                }
                for (int i = 0, j = lo + len2; j < hi; ++i, ++j)
                {
                    Restore(i, j);
                }
            }
            else
            {
                Reverse(lo, mid);
                Reverse(mid, hi);
                Reverse(lo, hi);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void MergeLo(int lo, int mid, int hi)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(Compare(lo, mid) > 0);
            int len1 = mid - lo;
            Save(lo, len1);
            Copy(mid, lo);
            int i = 0, j = mid + 1, dest = lo + 1;
            for (; ; )
            {
                for (int count = 0; count < MIN_GALLOP; )
                {
                    if (i >= len1 || j >= hi)
                    {
                        goto outerBreak;
                    }
                    else if (CompareSaved(i, j) <= 0)
                    {
                        Restore(i++, dest++);
                        count = 0;
                    }
                    else
                    {
                        Copy(j++, dest++);
                        ++count;
                    }
                }
                // galloping...
                int next = LowerSaved3(j, hi, i);
                for (; j < next; ++dest)
                {
                    Copy(j++, dest);
                }
                Restore(i++, dest++);
            //outerContinue: ; // LUCENENET NOTE: Not referenced
            }
        outerBreak:
            for (; i < len1; ++dest)
            {
                Restore(i++, dest);
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(j == dest);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void MergeHi(int lo, int mid, int hi)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(Compare(mid - 1, hi - 1) > 0);
            int len2 = hi - mid;
            Save(mid, len2);
            Copy(mid - 1, hi - 1);
            int i = mid - 2, j = len2 - 1, dest = hi - 2;
            for (; ; )
            {
                for (int count = 0; count < MIN_GALLOP; )
                {
                    if (i < lo || j < 0)
                    {
                        goto outerBreak;
                    }
                    else if (CompareSaved(j, i) >= 0)
                    {
                        Restore(j--, dest--);
                        count = 0;
                    }
                    else
                    {
                        Copy(i--, dest--);
                        ++count;
                    }
                }
                // galloping
                int next = UpperSaved3(lo, i + 1, j);
                while (i >= next)
                {
                    Copy(i--, dest--);
                }
                Restore(j--, dest--);
            //outerContinue: ; // LUCENENET NOTE: Not referenced
            }
        outerBreak:
            for (; j >= 0; --dest)
            {
                Restore(j--, dest);
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(i == dest);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int LowerSaved(int from, int to, int val)
        {
            int len = to - from;
            while (len > 0)
            {
                int half = len.TripleShift(1);
                int mid = from + half;
                if (CompareSaved(val, mid) > 0)
                {
                    from = mid + 1;
                    len = len - half - 1;
                }
                else
                {
                    len = half;
                }
            }
            return from;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int UpperSaved(int from, int to, int val)
        {
            int len = to - from;
            while (len > 0)
            {
                int half = len.TripleShift(1);
                int mid = from + half;
                if (CompareSaved(val, mid) < 0)
                {
                    len = half;
                }
                else
                {
                    from = mid + 1;
                    len = len - half - 1;
                }
            }
            return from;
        }

        // faster than lowerSaved when val is at the beginning of [from:to[
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int LowerSaved3(int from, int to, int val)
        {
            int f = from, t = f + 1;
            while (t < to)
            {
                if (CompareSaved(val, t) <= 0)
                {
                    return LowerSaved(f, t, val);
                }
                int delta = t - f;
                f = t;
                t += delta << 1;
            }
            return LowerSaved(f, to, val);
        }

        //faster than upperSaved when val is at the end of [from:to[
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int UpperSaved3(int from, int to, int val)
        {
            int f = to - 1, t = to;
            while (f > from)
            {
                if (CompareSaved(val, f) >= 0)
                {
                    return UpperSaved(f, t, val);
                }
                int delta = t - f;
                t = f;
                f -= delta << 1;
            }
            return UpperSaved(from, t, val);
        }

        /// <summary>
        /// Copy data from slot <paramref name="src"/> to slot <paramref name="dest"/>>. </summary>
        protected abstract void Copy(int src, int dest);

        /// <summary>
        /// Save all elements between slots <paramref name="i"/> and <paramref name="i"/>+<paramref name="len"/>
        /// into the temporary storage.
        /// </summary>
        protected abstract void Save(int i, int len);

        /// <summary>
        /// Restore element <paramref name="j"/> from the temporary storage into slot <paramref name="i"/>. </summary>
        protected abstract void Restore(int i, int j);

        /// <summary>
        /// Compare element <paramref name="i"/> from the temporary storage with element
        /// <paramref name="j"/> from the slice to sort, similarly to
        /// <see cref="Sorter.Compare(int, int)"/>.
        /// </summary>
        protected abstract int CompareSaved(int i, int j);
    }
}