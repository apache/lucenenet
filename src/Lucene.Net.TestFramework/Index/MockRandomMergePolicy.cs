using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// MergePolicy that makes random decisions for testing.
    /// </summary>
    public class MockRandomMergePolicy : MergePolicy
    {
        private readonly Random Random;

        public MockRandomMergePolicy(Random random)
        {
            // fork a private random, since we are called
            // unpredictably from threads:
            this.Random = new Random(random.Next());
        }

        public override MergeSpecification FindMerges(MergeTrigger? mergeTrigger, SegmentInfos segmentInfos)
        {
            MergeSpecification mergeSpec = null;
            //System.out.println("MRMP: findMerges sis=" + segmentInfos);

            int numSegments = segmentInfos.Size;

            IList<SegmentCommitInfo> segments = new List<SegmentCommitInfo>();
            ICollection<SegmentCommitInfo> merging = base.writer.Get().MergingSegments;

            foreach (SegmentCommitInfo sipc in segmentInfos.Segments)
            {
                if (!merging.Contains(sipc))
                {
                    segments.Add(sipc);
                }
            }

            numSegments = segments.Count;

            if (numSegments > 1 && (numSegments > 30 || Random.Next(5) == 3))
            {
                segments = CollectionsHelper.Shuffle(segments);

                // TODO: sometimes make more than 1 merge?
                mergeSpec = new MergeSpecification();
                int segsToMerge = TestUtil.NextInt(Random, 1, numSegments);
                mergeSpec.Add(new OneMerge(segments.SubList(0, segsToMerge)));
            }

            return mergeSpec;
        }

        public override MergeSpecification FindForcedMerges(SegmentInfos segmentInfos, int maxSegmentCount, IDictionary<SegmentCommitInfo, bool?> segmentsToMerge)
        {
            IList<SegmentCommitInfo> eligibleSegments = new List<SegmentCommitInfo>();
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
                eligibleSegments = CollectionsHelper.Shuffle(eligibleSegments);
                int upto = 0;
                while (upto < eligibleSegments.Count)
                {
                    int max = Math.Min(10, eligibleSegments.Count - upto);
                    int inc = max <= 2 ? max : TestUtil.NextInt(Random, 2, max);
                    mergeSpec.Add(new OneMerge(eligibleSegments.SubList(upto, upto + inc)));
                    upto += inc;
                }
            }

            if (mergeSpec != null)
            {
                foreach (OneMerge merge in mergeSpec.Merges)
                {
                    foreach (SegmentCommitInfo info in merge.Segments)
                    {
                        Debug.Assert(segmentsToMerge.ContainsKey(info));
                    }
                }
            }
            return mergeSpec;
        }

        public override MergeSpecification FindForcedDeletesMerges(SegmentInfos segmentInfos)
        {
            return FindMerges(null, segmentInfos);
        }

        public override void Dispose()
        {
        }

        public override bool UseCompoundFile(SegmentInfos infos, SegmentCommitInfo mergedInfo)
        {
            // 80% of the time we create CFS:
            return Random.Next(5) != 1;
        }
    }
}