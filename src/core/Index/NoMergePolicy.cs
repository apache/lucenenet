using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MergeTrigger = Lucene.Net.Index.MergePolicy.MergeTrigger;
using MergeSpecification = Lucene.Net.Index.MergePolicy.MergeSpecification;

namespace Lucene.Net.Index
{
    public sealed class NoMergePolicy : MergePolicy
    {
        public static readonly MergePolicy NO_COMPOUND_FILES = new NoMergePolicy(false);

        public static readonly MergePolicy COMPOUND_FILES = new NoMergePolicy(true);

        private readonly bool useCompoundFile;

        private NoMergePolicy(bool useCompoundFile)
        {
            // prevent instantiation
            this.useCompoundFile = useCompoundFile;
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override MergeSpecification FindMerges(MergeTrigger? mergeTrigger, SegmentInfos segmentInfos)
        {
            return null;
        }

        public override MergeSpecification FindForcedMerges(SegmentInfos segmentInfos, int maxSegmentCount, IDictionary<SegmentInfoPerCommit, bool> segmentsToMerge)
        {
            return null;
        }

        public override MergeSpecification FindForcedDeletesMerges(SegmentInfos segmentInfos)
        {
            return null;
        }

        public override bool UseCompoundFile(SegmentInfos segments, SegmentInfoPerCommit newSegment)
        {
            return useCompoundFile;
        }

        public override void SetIndexWriter(IndexWriter writer)
        {
        }

        public override string ToString()
        {
            return "NoMergePolicy";
        }
    }
}
