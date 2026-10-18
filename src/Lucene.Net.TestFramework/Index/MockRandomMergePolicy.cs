using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Index
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
    /// <see cref="MergePolicy"/> that makes random decisions for testing.
    /// </summary>
    public class MockRandomMergePolicy : MergePolicy
    {
        private readonly Random random;
        private bool doNonBulkMerges = true;

        public MockRandomMergePolicy(Random random)
        {
            // fork a private random, since we are called
            // unpredictably from threads:
            this.random = new J2N.Randomizer(random.NextInt64());
        }

        /// <summary>
        /// Set to <c>true</c> if sometimes readers to be merged should be wrapped in a FilterReader
        /// to mixup bulk merging.
        /// </summary>
        public bool DoNonBulkMerges
        {
            get => doNonBulkMerges;
            set => doNonBulkMerges = value;
        }

        public override MergeSpecification FindMerges(MergeTrigger mergeTrigger, SegmentInfos segmentInfos)
        {
            MergeSpecification mergeSpec = null;
            //System.out.println("MRMP: findMerges sis=" + segmentInfos);

            int numSegments/* = segmentInfos.Count*/; // LUCENENET: IDE0059: Remove unnecessary value assignment

            JCG.List<SegmentCommitInfo> segments = new JCG.List<SegmentCommitInfo>();
            ICollection<SegmentCommitInfo> merging = base.m_writer.Value?.MergingSegments
                ?? throw new InvalidOperationException("The writer has not been initialized"); // LUCENENET specific - throw exception if writer is null

            foreach (SegmentCommitInfo sipc in segmentInfos.Segments)
            {
                if (!merging.Contains(sipc))
                {
                    segments.Add(sipc);
                }
            }

            numSegments = segments.Count;

            if (numSegments > 1 && (numSegments > 30 || random.Next(5) == 3))
            {
                segments.Shuffle(random);

                // TODO: sometimes make more than 1 merge?
                mergeSpec = new MergeSpecification();
                int segsToMerge = TestUtil.NextInt32(random, 1, numSegments);
                if (doNonBulkMerges)
                {
                    mergeSpec.Add(new MockRandomOneMerge(segments.GetView(0, segsToMerge), random.NextInt64())); // LUCENENET: Checked length for correctness
                }
                else
                {
                    mergeSpec.Add(new OneMerge(segments.GetView(0, segsToMerge))); // LUCENENET: Checked length for correctness
                }
            }

            return mergeSpec;
        }

        public override MergeSpecification FindForcedMerges(SegmentInfos segmentInfos, int maxSegmentCount, IDictionary<SegmentCommitInfo, bool> segmentsToMerge)
        {
            JCG.List<SegmentCommitInfo> eligibleSegments = new JCG.List<SegmentCommitInfo>();
            foreach (SegmentCommitInfo info in segmentInfos.Segments)
            {
                if (segmentsToMerge.ContainsKey(info))
                {
                    eligibleSegments.Add(info);
                }
            }

            //System.out.println("MRMP: findMerges sis=" + segmentInfos + " eligible=" + eligibleSegments);
            MergeSpecification mergeSpec = null;
            if (eligibleSegments.Count > 1 || (eligibleSegments.Count == 1 && eligibleSegments[0].HasDeletions))
            {
                mergeSpec = new MergeSpecification();
                // Already shuffled having come out of a set but
                // shuffle again for good measure:
                eligibleSegments.Shuffle(random);
                int upto = 0;
                while (upto < eligibleSegments.Count)
                {
                    int max = Math.Min(10, eligibleSegments.Count - upto);
                    int inc = max <= 2 ? max : TestUtil.NextInt32(random, 2, max);
                    if (doNonBulkMerges)
                    {
                        mergeSpec.Add(new MockRandomOneMerge(eligibleSegments.GetView(upto, inc), random.NextInt64())); // LUCENENET: Converted end index to length
                    }
                    else
                    {
                        mergeSpec.Add(new OneMerge(eligibleSegments.GetView(upto, inc))); // LUCENENET: Converted end index to length
                    }
                    upto += inc;
                }
            }

            if (mergeSpec != null)
            {
                foreach (OneMerge merge in mergeSpec.Merges)
                {
                    foreach (SegmentCommitInfo info in merge.Segments)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(segmentsToMerge.ContainsKey(info));
                    }
                }
            }
            return mergeSpec;
        }

        public override MergeSpecification FindForcedDeletesMerges(SegmentInfos segmentInfos)
        {
            // LUCENENET specific - use NONE instead of null
            return FindMerges(MergeTrigger.NONE, segmentInfos);
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override bool UseCompoundFile(SegmentInfos infos, SegmentCommitInfo mergedInfo)
        {
            // 80% of the time we create CFS:
            return random.Next(5) != 1;
        }

        private class MockRandomOneMerge : OneMerge
        {
            private readonly J2N.Randomizer r;
            private JCG.List<AtomicReader> readers;

            public MockRandomOneMerge(JCG.List<SegmentCommitInfo> segments, long seed)
                : base(segments)
            {
                r = new J2N.Randomizer(seed); // LUCENENET: using J2N.Randomizer for long seed support
            }

            public override IList<AtomicReader> GetMergeReaders()
            {
                if (readers == null)
                {
                    readers = new JCG.List<AtomicReader>(base.GetMergeReaders());
                    for (int i = 0; i < readers.Count; i++)
                    {
                        // wrap it (e.g. prevent bulk merge etc)
                        if (r.Next(4) == 0)
                        {
                            readers[i] = new FilterAtomicReader(readers[i]);
                        }
                    }
                }

                return readers;
            }
        }
    }
}
