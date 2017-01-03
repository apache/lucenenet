using System;
using System.Diagnostics;

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
    /// <seealso cref="Sorter"/> implementation based on the
    /// <a href="http://svn.python.org/projects/python/trunk/Objects/listsort.txt">TimSort</a>
    /// algorithm.
    /// <p>this implementation is especially good at sorting partially-sorted
    /// arrays and sorts small arrays with binary sort.
    /// <p><b>NOTE</b>:There are a few differences with the original implementation:<ul>
    /// <li><a name="maxTempSlots"/>The extra amount of memory to perform merges is
    /// configurable. this allows small merges to be very fast while large merges
    /// will be performed in-place (slightly slower). You can make sure that the
    /// fast merge routine will always be used by having <code>maxTempSlots</code>
    /// equal to half of the length of the slice of data to sort.
    /// <li>Only the fast merge routine can gallop (the one that doesn't run
    /// in-place) and it only gallops on the longest slice.
    /// </ul>
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
        /// Create a new <seealso cref="TimSorter"/>. </summary>
        /// <param name="maxTempSlots"> the <a href="#maxTempSlots">maximum amount of extra memory to run merges</a> </param>
        protected TimSorter(int maxTempSlots)
            : base()
        {
            runEnds = new int[1 + STACKSIZE];
            this.maxTempSlots = maxTempSlots;
        }

        /// <summary>
        /// Minimum run length for an array of length <code>length</code>. </summary>
        internal static int MinRun(int length)
        {
            Debug.Assert(length >= MINRUN);
            int n = length;
            int r = 0;
            while (n >= 64)
            {
                r |= n & 1;
                n = (int)((uint)n >> 1);
            }
            int minRun = n + r;
            Debug.Assert(minRun >= MINRUN && minRun <= THRESHOLD);
            return minRun;
        }

        internal virtual int RunLen(int i)
        {
            int off = stackSize - i;
            return runEnds[off] - runEnds[off - 1];
        }

        internal virtual int RunBase(int i)
        {
            return runEnds[stackSize - i - 1];
        }

        internal virtual int RunEnd(int i)
        {
            return runEnds[stackSize - i];
        }

        internal virtual void SetRunEnd(int i, int runEnd)
        {
            runEnds[stackSize - i] = runEnd;
        }

        internal virtual void PushRunLen(int len)
        {
            runEnds[stackSize + 1] = runEnds[stackSize] + len;
            ++stackSize;
        }

        /// <summary>
        /// Compute the length of the next run, make the run sorted and return its
        ///  length.
        /// </summary>
        internal virtual int NextRun()
        {
            int runBase = RunEnd(0);
            Debug.Assert(runBase < to);
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

        internal virtual void ExhaustStack()
        {
            while (stackSize > 1)
            {
                MergeAt(0);
            }
        }

        internal virtual void Reset(int from, int to)
        {
            stackSize = 0;
            Array.Clear(runEnds, 0, runEnds.Length);
            runEnds[0] = from;
            this.to = to;
            int length = to - from;
            this.minRun = length <= THRESHOLD ? length : MinRun(length);
        }

        internal virtual void MergeAt(int n)
        {
            Debug.Assert(stackSize >= 2);
            Merge(RunBase(n + 1), RunBase(n), RunEnd(n));
            for (int j = n + 1; j > 0; --j)
            {
                SetRunEnd(j, RunEnd(j - 1));
            }
            --stackSize;
        }

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
            Debug.Assert(RunEnd(0) == to);
        }

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

        internal virtual void MergeLo(int lo, int mid, int hi)
        {
            Debug.Assert(Compare(lo, mid) > 0);
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
            outerContinue: ;
            }
        outerBreak:
            for (; i < len1; ++dest)
            {
                Restore(i++, dest);
            }
            Debug.Assert(j == dest);
        }

        internal virtual void MergeHi(int lo, int mid, int hi)
        {
            Debug.Assert(Compare(mid - 1, hi - 1) > 0);
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
            outerContinue: ;
            }
        outerBreak:
            for (; j >= 0; --dest)
            {
                Restore(j--, dest);
            }
            Debug.Assert(i == dest);
        }

        internal virtual int LowerSaved(int from, int to, int val)
        {
            int len = to - from;
            while (len > 0)
            {
                int half = (int)((uint)len >> 1);
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

        internal virtual int UpperSaved(int from, int to, int val)
        {
            int len = to - from;
            while (len > 0)
            {
                int half = (int)((uint)len >> 1);
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
        /// Copy data from slot <code>src</code> to slot <code>dest</code>. </summary>
        protected abstract void Copy(int src, int dest);

        /// <summary>
        /// Save all elements between slots <code>i</code> and <code>i+len</code>
        ///  into the temporary storage.
        /// </summary>
        protected abstract void Save(int i, int len);

        /// <summary>
        /// Restore element <code>j</code> from the temporary storage into slot <code>i</code>. </summary>
        protected abstract void Restore(int i, int j);

        /// <summary>
        /// Compare element <code>i</code> from the temporary storage with element
        ///  <code>j</code> from the slice to sort, similarly to
        ///  <seealso cref="#compare(int, int)"/>.
        /// </summary>
        protected abstract int CompareSaved(int i, int j);
    }
}