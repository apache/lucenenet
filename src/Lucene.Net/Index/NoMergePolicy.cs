using System.Collections.Generic;

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
    /// A <see cref="MergePolicy"/> which never returns merges to execute (hence it's
    /// name). It is also a singleton and can be accessed through
    /// <see cref="NoMergePolicy.NO_COMPOUND_FILES"/> if you want to indicate the index
    /// does not use compound files, or through <see cref="NoMergePolicy.COMPOUND_FILES"/>
    /// otherwise. Use it if you want to prevent an <see cref="IndexWriter"/> from ever
    /// executing merges, without going through the hassle of tweaking a merge
    /// policy's settings to achieve that, such as changing its merge factor.
    /// </summary>
    public sealed class NoMergePolicy : MergePolicy
    {
        /// <summary>
        /// A singleton <see cref="NoMergePolicy"/> which indicates the index does not use
        /// compound files.
        /// </summary>
        public static readonly MergePolicy NO_COMPOUND_FILES = new NoMergePolicy(false);

        /// <summary>
        /// A singleton <see cref="NoMergePolicy"/> which indicates the index uses compound
        /// files.
        /// </summary>
        public static readonly MergePolicy COMPOUND_FILES = new NoMergePolicy(true);

        private readonly bool useCompoundFile;

        private NoMergePolicy(bool useCompoundFile)
            : base(useCompoundFile ? 1.0 : 0.0, 0)
        {
            // prevent instantiation
            this.useCompoundFile = useCompoundFile;
        }

        protected override void Dispose(bool disposing)
        {
            // LUCENENET: Intentionally blank
        }

        public override MergeSpecification FindMerges(MergeTrigger mergeTrigger, SegmentInfos segmentInfos)
        {
            return null;
        }

        public override MergeSpecification FindForcedMerges(SegmentInfos segmentInfos, int maxSegmentCount, IDictionary<SegmentCommitInfo, bool> segmentsToMerge)
        {
            return null;
        }

        public override MergeSpecification FindForcedDeletesMerges(SegmentInfos segmentInfos)
        {
            return null;
        }

        public override bool UseCompoundFile(SegmentInfos segments, SegmentCommitInfo newSegment)
        {
            return useCompoundFile;
        }

        public override void SetIndexWriter(IndexWriter writer)
        {
            // LUCENENET: Intentionally blank
        }

        protected override long Size(SegmentCommitInfo info)
        {
            return long.MaxValue;
        }

        public override string ToString()
        {
            return "NoMergePolicy";
        }
    }
}