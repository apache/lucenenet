using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public class UpgradeIndexMergePolicy : MergePolicy
    {
        protected readonly MergePolicy basepolicy;

        public UpgradeIndexMergePolicy(MergePolicy basepolicy)
        {
            this.basepolicy = basepolicy;
        }

        protected bool ShouldUpgradeSegment(SegmentInfoPerCommit si)
        {
            return !Constants.LUCENE_MAIN_VERSION.Equals(si.info.Version);
        }

        public override void SetIndexWriter(IndexWriter writer)
        {
            base.SetIndexWriter(writer);
            basepolicy.SetIndexWriter(writer);
        }

        public override MergeSpecification FindMerges(MergeTrigger? mergeTrigger, SegmentInfos segmentInfos)
        {
            return basepolicy.FindMerges(null, segmentInfos);
        }

        public override MergeSpecification FindForcedMerges(SegmentInfos segmentInfos, int maxSegmentCount, IDictionary<SegmentInfoPerCommit, bool> segmentsToMerge)
        {
            // first find all old segments
            IDictionary<SegmentInfoPerCommit, Boolean> oldSegments = new HashMap<SegmentInfoPerCommit, Boolean>();
            foreach (SegmentInfoPerCommit si in segmentInfos)
            {
                bool v = segmentsToMerge[si];
                if (v != null && ShouldUpgradeSegment(si))
                {
                    oldSegments[si] = v;
                }
            }

            if (Verbose)
            {
                Message("findForcedMerges: segmentsToUpgrade=" + oldSegments);
            }

            if (oldSegments.Count == 0)
                return null;

            MergeSpecification spec = basepolicy.FindForcedMerges(segmentInfos, maxSegmentCount, oldSegments);

            if (spec != null)
            {
                // remove all segments that are in merge specification from oldSegments,
                // the resulting set contains all segments that are left over
                // and will be merged to one additional segment:
                foreach (OneMerge om in spec.merges)
                {
                    foreach (SegmentInfoPerCommit sipc in om.segments)
                    {
                        oldSegments.Remove(sipc);
                    }
                }
            }

            if (oldSegments.Count > 0)
            {
                if (Verbose)
                {
                    Message("findForcedMerges: " + basepolicy.GetType().Name +
                    " does not want to merge all old segments, merge remaining ones into new segment: " + oldSegments);
                }
                IList<SegmentInfoPerCommit> newInfos = new List<SegmentInfoPerCommit>();
                foreach (SegmentInfoPerCommit si in segmentInfos)
                {
                    if (oldSegments.ContainsKey(si))
                    {
                        newInfos.Add(si);
                    }
                }
                // add the final merge
                if (spec == null)
                {
                    spec = new MergeSpecification();
                }
                spec.Add(new OneMerge(newInfos));
            }

            return spec;
        }

        public override MergeSpecification FindForcedDeletesMerges(SegmentInfos segmentInfos)
        {
            return basepolicy.FindForcedDeletesMerges(segmentInfos);
        }

        public override bool UseCompoundFile(SegmentInfos segments, SegmentInfoPerCommit newSegment)
        {
            return basepolicy.UseCompoundFile(segments, newSegment);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                basepolicy.Dispose();
            }
        }

        public override string ToString()
        {
            return "[" + GetType().Name + "->" + basepolicy + "]";
        }

        private bool Verbose
        {
            get
            {
                IndexWriter w = writer.Get();
                return w != null && w.infoStream.IsEnabled("UPGMP");
            }
        }

        private void Message(string message)
        {
            writer.Get().infoStream.Message("UPGMP", message);
        }
    }
}
