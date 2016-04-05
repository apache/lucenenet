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

    using Constants = Lucene.Net.Util.Constants;

    /// <summary>
    /// this <seealso cref="MergePolicy"/> is used for upgrading all existing segments of
    /// an index when calling <seealso cref="IndexWriter#forceMerge(int)"/>.
    /// All other methods delegate to the base {@code MergePolicy} given to the constructor.
    /// this allows for an as-cheap-as possible upgrade of an older index by only upgrading segments that
    /// are created by previous Lucene versions. forceMerge does no longer really merge;
    /// it is just used to &quot;forceMerge&quot; older segment versions away.
    /// <p>In general one would use <seealso cref="IndexUpgrader"/>, but for a fully customizeable upgrade,
    /// you can use this like any other {@code MergePolicy} and call <seealso cref="IndexWriter#forceMerge(int)"/>:
    /// <pre class="prettyprint lang-java">
    ///  IndexWriterConfig iwc = new IndexWriterConfig(Version.LUCENE_XX, new KeywordAnalyzer());
    ///  iwc.setMergePolicy(new UpgradeIndexMergePolicy(iwc.getMergePolicy()));
    ///  IndexWriter w = new IndexWriter(dir, iwc);
    ///  w.forceMerge(1);
    ///  w.Dispose();
    /// </pre>
    /// <p><b>Warning:</b> this merge policy may reorder documents if the index was partially
    /// upgraded before calling forceMerge (e.g., documents were added). If your application relies
    /// on &quot;monotonicity&quot; of doc IDs (which means that the order in which the documents
    /// were added to the index is preserved), do a forceMerge(1) instead. Please note, the
    /// delegate {@code MergePolicy} may also reorder documents.
    /// @lucene.experimental </summary>
    /// <seealso cref= IndexUpgrader </seealso>
    public class UpgradeIndexMergePolicy : MergePolicy
    {
        /// <summary>
        /// Wrapped <seealso cref="MergePolicy"/>. </summary>
        protected internal readonly MergePolicy @base;

        /// <summary>
        /// Wrap the given <seealso cref="MergePolicy"/> and intercept forceMerge requests to
        /// only upgrade segments written with previous Lucene versions.
        /// </summary>
        public UpgradeIndexMergePolicy(MergePolicy @base)
        {
            this.@base = @base;
        }

        /// <summary>
        /// Returns if the given segment should be upgraded. The default implementation
        /// will return {@code !Constants.LUCENE_MAIN_VERSION.equals(si.getVersion())},
        /// so all segments created with a different version number than this Lucene version will
        /// get upgraded.
        /// </summary>
        protected internal virtual bool ShouldUpgradeSegment(SegmentCommitInfo si)
        {
            return !Constants.LUCENE_MAIN_VERSION.Equals(si.Info.Version);
        }

        public override IndexWriter IndexWriter
        {
            set
            {
                base.IndexWriter = value;
                @base.IndexWriter = value;
            }
        }

        public override MergeSpecification FindMerges(MergeTrigger? mergeTrigger, SegmentInfos segmentInfos)
        {
            return @base.FindMerges(null, segmentInfos);
        }

        public override MergeSpecification FindForcedMerges(SegmentInfos segmentInfos, int maxSegmentCount, IDictionary<SegmentCommitInfo, bool?> segmentsToMerge)
        {
            // first find all old segments
            IDictionary<SegmentCommitInfo, bool?> oldSegments = new Dictionary<SegmentCommitInfo, bool?>();
            foreach (SegmentCommitInfo si in segmentInfos.Segments)
            {
                bool? v = segmentsToMerge[si];
                if (v != null && ShouldUpgradeSegment(si))
                {
                    oldSegments[si] = v;
                }
            }

            if (Verbose())
            {
                Message("findForcedMerges: segmentsToUpgrade=" + oldSegments);
            }

            if (oldSegments.Count == 0)
            {
                return null;
            }

            MergeSpecification spec = @base.FindForcedMerges(segmentInfos, maxSegmentCount, oldSegments);

            if (spec != null)
            {
                // remove all segments that are in merge specification from oldSegments,
                // the resulting set contains all segments that are left over
                // and will be merged to one additional segment:
                foreach (OneMerge om in spec.Merges)
                {
                    foreach (SegmentCommitInfo sipc in om.Segments)
                    {
                        oldSegments.Remove(sipc);
                    }
                }
            }

            if (oldSegments.Count > 0)
            {
                if (Verbose())
                {
                    Message("findForcedMerges: " + @base.GetType().Name + " does not want to merge all old segments, merge remaining ones into new segment: " + oldSegments);
                }
                IList<SegmentCommitInfo> newInfos = new List<SegmentCommitInfo>();
                foreach (SegmentCommitInfo si in segmentInfos.Segments)
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
            return @base.FindForcedDeletesMerges(segmentInfos);
        }

        public override bool UseCompoundFile(SegmentInfos segments, SegmentCommitInfo newSegment)
        {
            return @base.UseCompoundFile(segments, newSegment);
        }

        public override void Dispose()
        {
            @base.Dispose();
        }

        public override string ToString()
        {
            return "[" + this.GetType().Name + "->" + @base + "]";
        }

        private bool Verbose()
        {
            IndexWriter w = Writer.Get();
            return w != null && w.infoStream.IsEnabled("UPGMP");
        }

        private void Message(string message)
        {
            Writer.Get().infoStream.Message("UPGMP", message);
        }
    }
}