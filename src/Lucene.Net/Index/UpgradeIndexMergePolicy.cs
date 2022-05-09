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

    using Constants = Lucene.Net.Util.Constants;

    /// <summary>
    /// This <see cref="MergePolicy"/> is used for upgrading all existing segments of
    /// an index when calling <see cref="IndexWriter.ForceMerge(int)"/>.
    /// All other methods delegate to the base <see cref="MergePolicy"/> given to the constructor.
    /// This allows for an as-cheap-as possible upgrade of an older index by only upgrading segments that
    /// are created by previous Lucene versions. ForceMerge does no longer really merge;
    /// it is just used to &quot;ForceMerge&quot; older segment versions away.
    /// <para/>In general one would use <see cref="IndexUpgrader"/>, but for a fully customizeable upgrade,
    /// you can use this like any other <see cref="MergePolicy"/> and call <see cref="IndexWriter.ForceMerge(int)"/>:
    /// <code>
    ///     IndexWriterConfig iwc = new IndexWriterConfig(LuceneVersion.LUCENE_XX, new KeywordAnalyzer());
    ///     iwc.MergePolicy = new UpgradeIndexMergePolicy(iwc.MergePolicy);
    ///     using (IndexWriter w = new IndexWriter(dir, iwc))
    ///     {
    ///         w.ForceMerge(1);
    ///     }
    /// </code>
    /// <para/><b>Warning:</b> this merge policy may reorder documents if the index was partially
    /// upgraded before calling <see cref="IndexWriter.ForceMerge(int)"/> (e.g., documents were added). If your application relies
    /// on &quot;monotonicity&quot; of doc IDs (which means that the order in which the documents
    /// were added to the index is preserved), do a <c>ForceMerge(1)</c> instead. Please note, the
    /// delegate <see cref="MergePolicy"/> may also reorder documents.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="IndexUpgrader"/>
    public class UpgradeIndexMergePolicy : MergePolicy
    {
        /// <summary>
        /// Wrapped <see cref="MergePolicy"/>. </summary>
        protected readonly MergePolicy m_base;

        /// <summary>
        /// Wrap the given <see cref="MergePolicy"/> and intercept <see cref="IndexWriter.ForceMerge(int)"/> requests to
        /// only upgrade segments written with previous Lucene versions.
        /// </summary>
        public UpgradeIndexMergePolicy(MergePolicy @base)
        {
            this.m_base = @base;
        }

        /// <summary>
        /// Returns <c>true</c> if the given segment should be upgraded. The default implementation
        /// will return <c>!Constants.LUCENE_MAIN_VERSION.Equals(si.Info.Version, StringComparison.Ordinal)</c>,
        /// so all segments created with a different version number than this Lucene version will
        /// get upgraded.
        /// </summary>
        protected virtual bool ShouldUpgradeSegment(SegmentCommitInfo si)
        {
            return !Constants.LUCENE_MAIN_VERSION.Equals(si.Info.Version, StringComparison.Ordinal);
        }

        public override void SetIndexWriter(IndexWriter writer)
        {
            base.SetIndexWriter(writer);
            m_base.SetIndexWriter(writer);
        }

        public override MergeSpecification FindMerges(MergeTrigger mergeTrigger, SegmentInfos segmentInfos)
        {
            // LUCENENET specific - just use min value to indicate "null" for merge trigger
            return m_base.FindMerges((MergeTrigger)int.MinValue, segmentInfos);
        }

        public override MergeSpecification FindForcedMerges(SegmentInfos segmentInfos, int maxSegmentCount, IDictionary<SegmentCommitInfo, bool> segmentsToMerge)
        {
            // first find all old segments
            IDictionary<SegmentCommitInfo, bool> oldSegments = new Dictionary<SegmentCommitInfo, bool>();
            foreach (SegmentCommitInfo si in segmentInfos.Segments)
            {
                if (segmentsToMerge.TryGetValue(si, out bool v) && ShouldUpgradeSegment(si))
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

            MergeSpecification spec = m_base.FindForcedMerges(segmentInfos, maxSegmentCount, oldSegments);

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
                    Message("findForcedMerges: " + m_base.GetType().Name + " does not want to merge all old segments, merge remaining ones into new segment: " + oldSegments);
                }
                IList<SegmentCommitInfo> newInfos = new JCG.List<SegmentCommitInfo>();
                foreach (SegmentCommitInfo si in segmentInfos.Segments)
                {
                    if (oldSegments.ContainsKey(si))
                    {
                        newInfos.Add(si);
                    }
                }
                // add the final merge
                if (spec is null)
                {
                    spec = new MergeSpecification();
                }
                spec.Add(new OneMerge(newInfos));
            }

            return spec;
        }

        public override MergeSpecification FindForcedDeletesMerges(SegmentInfos segmentInfos)
        {
            return m_base.FindForcedDeletesMerges(segmentInfos);
        }

        public override bool UseCompoundFile(SegmentInfos segments, SegmentCommitInfo newSegment)
        {
            return m_base.UseCompoundFile(segments, newSegment);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            { 
                m_base.Dispose();
            }
        }

        public override string ToString()
        {
            return "[" + this.GetType().Name + "->" + m_base + "]";
        }

        private bool Verbose()
        {
            IndexWriter w = m_writer.Get();
            return w != null && w.infoStream.IsEnabled("UPGMP");
        }

        private void Message(string message)
        {
            m_writer.Get().infoStream.Message("UPGMP", message);
        }
    }
}