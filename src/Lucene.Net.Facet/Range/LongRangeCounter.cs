// Lucene version compatibility level 4.8.1
using J2N.Numerics;
using Lucene.Net.Diagnostics;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Facet.Range
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
    /// Counts how many times each range was seen;
    /// per-hit it's just a binary search (<see cref="Add"/>)
    /// against the elementary intervals, and in the end we
    /// rollup back to the original ranges.
    /// <para/>
    /// NOTE: This was LongRangeCounter in Lucene
    /// </summary>
    internal sealed class Int64RangeCounter
    {
        internal readonly Int64RangeNode root;
        internal readonly long[] boundaries;
        internal readonly int[] leafCounts;

        // Used during rollup
        private int leafUpto;
        private int missingCount;

        public Int64RangeCounter(Int64Range[] ranges)
        {
            // Maps all range inclusive endpoints to int flags; 1
            // = start of interval, 2 = end of interval.  We need to
            // track the start vs end case separately because if a
            // given point is both, then it must be its own
            // elementary interval:
            IDictionary<long, int> endsMap = new Dictionary<long, int>
            {
                [long.MinValue] = 1,
                [long.MaxValue] = 2
            };

            foreach (Int64Range range in ranges)
            {
                if (!endsMap.TryGetValue(range.minIncl, out int cur))
                {
                    endsMap[range.minIncl] = 1;
                }
                else
                {
                    endsMap[range.minIncl] = cur | 1;
                }

                if (!endsMap.TryGetValue(range.maxIncl, out cur))
                {
                    endsMap[range.maxIncl] = 2;
                }
                else
                {
                    endsMap[range.maxIncl] = cur | 2;
                }
            }

            var endsList = new JCG.List<long>(endsMap.Keys);
            endsList.Sort();

            // Build elementaryIntervals (a 1D Venn diagram):
            IList<InclusiveRange> elementaryIntervals = new JCG.List<InclusiveRange>();
            int upto0 = 1;
            long v = endsList[0];
            long prev;
            if (endsMap[v] == 3)
            {
                elementaryIntervals.Add(new InclusiveRange(v, v));
                prev = v + 1;
            }
            else
            {
                prev = v;
            }

            while (upto0 < endsList.Count)
            {
                v = endsList[upto0];
                int flags = endsMap[v];
                //System.out.println("  v=" + v + " flags=" + flags);
                if (flags == 3)
                {
                    // This point is both an end and a start; we need to
                    // separate it:
                    if (v > prev)
                    {
                        elementaryIntervals.Add(new InclusiveRange(prev, v - 1));
                    }
                    elementaryIntervals.Add(new InclusiveRange(v, v));
                    prev = v + 1;
                }
                else if (flags == 1)
                {
                    // This point is only the start of an interval;
                    // attach it to next interval:
                    if (v > prev)
                    {
                        elementaryIntervals.Add(new InclusiveRange(prev, v - 1));
                    }
                    prev = v;
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(flags == 2);
                    // This point is only the end of an interval; attach
                    // it to last interval:
                    elementaryIntervals.Add(new InclusiveRange(prev, v));
                    prev = v + 1;
                }
                //System.out.println("    ints=" + elementaryIntervals);
                upto0++;
            }

            // Build binary tree on top of intervals:
            root = Split(0, elementaryIntervals.Count, elementaryIntervals);

            // Set outputs, so we know which range to output for
            // each node in the tree:
            for (int i = 0; i < ranges.Length; i++)
            {
                root.AddOutputs(i, ranges[i]);
            }

            // Set boundaries (ends of each elementary interval):
            boundaries = new long[elementaryIntervals.Count];
            for (int i = 0; i < boundaries.Length; i++)
            {
                boundaries[i] = elementaryIntervals[i].End;
            }

            leafCounts = new int[boundaries.Length];

            //System.out.println("ranges: " + Arrays.toString(ranges));
            //System.out.println("intervals: " + elementaryIntervals);
            //System.out.println("boundaries: " + Arrays.toString(boundaries));
            //System.out.println("root:\n" + root);
        }

        public void Add(long v)
        {
            //System.out.println("add v=" + v);

            // NOTE: this works too, but it's ~6% slower on a simple
            // test with a high-freq TermQuery w/ range faceting on
            // wikimediumall:
            /*
            int index = Arrays.binarySearch(boundaries, v);
            if (index < 0) {
              index = -index-1;
            }
            leafCounts[index]++;
            */

            // Binary search to find matched elementary range; we
            // are guaranteed to find a match because the last
            // boundary is Long.MAX_VALUE:

            int lo = 0;
            int hi = boundaries.Length - 1;
            while (true)
            {
                int mid = (lo + hi).TripleShift(1);
                //System.out.println("  cycle lo=" + lo + " hi=" + hi + " mid=" + mid + " boundary=" + boundaries[mid] + " to " + boundaries[mid+1]);
                if (v <= boundaries[mid])
                {
                    if (mid == 0)
                    {
                        leafCounts[0]++;
                        return;
                    }
                    else
                    {
                        hi = mid - 1;
                    }
                }
                else if (v > boundaries[mid + 1])
                {
                    lo = mid + 1;
                }
                else
                {
                    leafCounts[mid + 1]++;
                    //System.out.println("  incr @ " + (mid+1) + "; now " + leafCounts[mid+1]);
                    return;
                }
            }
        }

        /// <summary>
        /// Fills counts corresponding to the original input
        /// ranges, returning the missing count (how many hits
        /// didn't match any ranges). 
        /// </summary>
        public int FillCounts(int[] counts)
        {
            //System.out.println("  rollup");
            missingCount = 0;
            leafUpto = 0;
            Rollup(root, counts, false);
            return missingCount;
        }

        private int Rollup(Int64RangeNode node, int[] counts, bool sawOutputs)
        {
            int count;
            sawOutputs |= node.outputs != null;
            if (node.left != null)
            {
                count = Rollup(node.left, counts, sawOutputs);
                count += Rollup(node.right, counts, sawOutputs);
            }
            else
            {
                // Leaf:
                count = leafCounts[leafUpto];
                leafUpto++;
                if (!sawOutputs)
                {
                    // This is a missing count (no output ranges were
                    // seen "above" us):
                    missingCount += count;
                }
            }
            if (node.outputs != null)
            {
                foreach (int rangeIndex in node.outputs)
                {
                    counts[rangeIndex] += count;
                }
            }
            //System.out.println("  rollup node=" + node.start + " to " + node.end + ": count=" + count);
            return count;
        }

        private static Int64RangeNode Split(int start, int end, IList<InclusiveRange> elementaryIntervals)
        {
            if (start == end - 1)
            {
                // leaf
                InclusiveRange range = elementaryIntervals[start];
                return new Int64RangeNode(range.Start, range.End, null, null, start);
            }
            else
            {
                int mid = (start + end).TripleShift(1);
                Int64RangeNode left = Split(start, mid, elementaryIntervals);
                Int64RangeNode right = Split(mid, end, elementaryIntervals);
                return new Int64RangeNode(left.start, right.end, left, right, -1);
            }
        }

        private sealed class InclusiveRange
        {
            public long Start { get; private set; }
            public long End { get; private set; }

            public InclusiveRange(long start, long end)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(end >= start);
                this.Start = start;
                this.End = end;
            }

            public override string ToString()
            {
                return Start + " to " + End;
            }
        }

        /// <summary>
        /// Holds one node of the segment tree.
        /// <para/>
        /// NOTE: This was LongRangeNode in Lucene
        /// </summary>
        public sealed class Int64RangeNode
        {
            internal readonly Int64RangeNode left;
            internal readonly Int64RangeNode right;

            // Our range, inclusive:
            internal readonly long start;
            internal readonly long end;

            // If we are a leaf, the index into elementary ranges that
            // we point to:
            internal readonly int leafIndex;

            // Which range indices to output when a query goes
            // through this node:
            internal IList<int> outputs;

            public Int64RangeNode(long start, long end, Int64RangeNode left, Int64RangeNode right, int leafIndex)
            {
                this.start = start;
                this.end = end;
                this.left = left;
                this.right = right;
                this.leafIndex = leafIndex;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                ToString(sb, 0);
                return sb.ToString();
            }

            internal static void Indent(StringBuilder sb, int depth)
            {
                for (int i = 0; i < depth; i++)
                {
                    sb.Append("  ");
                }
            }

            /// <summary>
            /// Recursively assigns range outputs to each node.
            /// </summary>
            internal void AddOutputs(int index, Int64Range range)
            {
                if (start >= range.minIncl && end <= range.maxIncl)
                {
                    // Our range is fully included in the incoming
                    // range; add to our output list:
                    if (outputs is null)
                    {
                        outputs = new JCG.List<int>();
                    }
                    outputs.Add(index);
                }
                else if (left != null)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(right != null);
                    // Recurse:
                    left.AddOutputs(index, range);
                    right.AddOutputs(index, range);
                }
            }

            internal void ToString(StringBuilder sb, int depth)
            {
                Indent(sb, depth);
                if (left is null)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(right is null);
                    sb.Append("leaf: " + start + " to " + end);
                }
                else
                {
                    sb.Append("node: " + start + " to " + end);
                }
                if (outputs != null)
                {
                    sb.Append(" outputs=");
                    sb.Append(outputs);
                }
                sb.Append('\n');

                if (left != null)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(right != null);
                    left.ToString(sb, depth + 1);
                    right.ToString(sb, depth + 1);
                }
            }
        }
    }
}