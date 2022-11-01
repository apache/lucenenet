using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Index.Sorter
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
    /// A <see cref="MergePolicy"/> that reorders documents according to a <see cref="Sort"/>
    /// before merging them. As a consequence, all segments resulting from a merge
    /// will be sorted while segments resulting from a flush will be in the order
    /// in which documents have been added.
    /// <para><b>NOTE</b>: Never use this policy if you rely on
    /// <see cref="Index.IndexWriter.AddDocuments(IEnumerable{IEnumerable{IIndexableField}}, Analysis.Analyzer)">IndexWriter.AddDocuments</see> 
    /// to have sequentially-assigned doc IDs, this policy will scatter doc IDs.
    /// </para>
    /// <para><b>NOTE</b>: This policy should only be used with idempotent <see cref="Sort"/>s 
    /// so that the order of segments is predictable. For example, using 
    /// <see cref="Sort.INDEXORDER"/> in reverse (which is not idempotent) will make 
    /// the order of documents in a segment depend on the number of times the segment 
    /// has been merged.
    /// @lucene.experimental 
    /// </para>
    /// </summary>
    public sealed class SortingMergePolicy : MergePolicy
    {

        /// <summary>
        /// Put in the <see cref="SegmentInfo.Diagnostics">diagnostics</see> to denote that
        /// this segment is sorted.
        /// </summary>
        public static readonly string SORTER_ID_PROP = "sorter";

        internal class SortingOneMerge : OneMerge
        {
            private readonly SortingMergePolicy outerInstance;


            internal IList<AtomicReader> unsortedReaders;
            internal Sorter.DocMap docMap;
            internal AtomicReader sortedView;

            internal SortingOneMerge(SortingMergePolicy outerInstance, IList<SegmentCommitInfo> segments)
                    : base(segments)
            {
                this.outerInstance = outerInstance;
            }

            public override IList<AtomicReader> GetMergeReaders()
            {
                if (unsortedReaders is null)
                {
                    unsortedReaders = base.GetMergeReaders();
                    AtomicReader atomicView;
                    if (unsortedReaders.Count == 1)
                    {
                        atomicView = unsortedReaders[0];
                    }
                    else
                    {
                        IndexReader multiReader = new MultiReader(unsortedReaders.ToArray());
                        atomicView = SlowCompositeReaderWrapper.Wrap(multiReader);
                    }
                    docMap = outerInstance.sorter.Sort(atomicView);
                    sortedView = SortingAtomicReader.Wrap(atomicView, docMap);
                }
                // a null doc map means that the readers are already sorted
                return docMap is null ? unsortedReaders : new JCG.List<AtomicReader>(new AtomicReader[] { sortedView });
            }

            public override SegmentCommitInfo Info
            {
                get => base.info; // LUCENENET specific: added getter
                set
                {
                    IDictionary<string, string> diagnostics = value.Info.Diagnostics;
                    diagnostics[SORTER_ID_PROP] = outerInstance.sorter.ID;
                    base.Info = value;
                }
            }

            internal virtual MonotonicAppendingInt64Buffer GetDeletes(IList<AtomicReader> readers)
            {
                MonotonicAppendingInt64Buffer deletes = new MonotonicAppendingInt64Buffer();
                int deleteCount = 0;
                foreach (AtomicReader reader in readers)
                {
                    int maxDoc = reader.MaxDoc;
                    IBits liveDocs = reader.LiveDocs;
                    for (int i = 0; i < maxDoc; ++i)
                    {
                        if (liveDocs != null && !liveDocs.Get(i))
                        {
                            ++deleteCount;
                        }
                        else
                        {
                            deletes.Add(deleteCount);
                        }
                    }
                }
                deletes.Freeze();
                return deletes;
            }

            public override MergePolicy.DocMap GetDocMap(MergeState mergeState)
            {
                if (unsortedReaders is null)
                {
                    throw IllegalStateException.Create();
                }
                if (docMap is null)
                {
                    return base.GetDocMap(mergeState);
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(mergeState.DocMaps.Length == 1); // we returned a singleton reader
                MonotonicAppendingInt64Buffer deletes = GetDeletes(unsortedReaders);
                return new DocMapAnonymousClass(this, mergeState, deletes);
            }

            private sealed class DocMapAnonymousClass : MergePolicy.DocMap
            {
                private readonly SortingOneMerge outerInstance;

                private readonly MergeState mergeState;
                private readonly MonotonicAppendingInt64Buffer deletes;

                public DocMapAnonymousClass(SortingOneMerge outerInstance, MergeState mergeState, MonotonicAppendingInt64Buffer deletes)
                {
                    this.outerInstance = outerInstance;
                    this.mergeState = mergeState;
                    this.deletes = deletes;
                }

                public override int Map(int old)
                {
                    int oldWithDeletes = old + (int)deletes.Get(old);
                    int newWithDeletes = outerInstance.docMap.OldToNew(oldWithDeletes);
                    return mergeState.DocMaps[0].Get(newWithDeletes);
                }
            }
        }

        internal class SortingMergeSpecification : MergeSpecification
        {
            private readonly SortingMergePolicy outerInstance;

            public SortingMergeSpecification(SortingMergePolicy outerInstance)
            {
                this.outerInstance = outerInstance;
            }


            public override void Add(OneMerge merge)
            {
                base.Add(new SortingOneMerge(outerInstance, merge.Segments));
            }

            public override string SegString(Directory dir)
            {
                return "SortingMergeSpecification(" + base.SegString(dir) + ", sorter=" + outerInstance.sorter + ")";
            }

        }

        /// <summary>
        /// Returns <c>true</c> if the given <paramref name="reader"/> is sorted by the specified <paramref name="sort"/>.
        /// </summary>
        public static bool IsSorted(AtomicReader reader, Sort sort)
        {
            if (reader is SegmentReader segReader)
            {
                IDictionary<string, string> diagnostics = segReader.SegmentInfo.Info.Diagnostics;
                if (diagnostics != null
                    && diagnostics.TryGetValue(SORTER_ID_PROP, out string diagnosticsSort)
                    && sort.ToString().Equals(diagnosticsSort, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private MergeSpecification SortedMergeSpecification(MergeSpecification specification)
        {
            if (specification is null)
            {
                return null;
            }
            MergeSpecification sortingSpec = new SortingMergeSpecification(this);
            foreach (OneMerge merge in specification.Merges)
            {
                sortingSpec.Add(merge);
            }
            return sortingSpec;
        }

        internal readonly MergePolicy @in;
        internal readonly Sorter sorter;
        internal readonly Sort sort;

        /// <summary>
        /// Create a new <see cref="MergePolicy"/> that sorts documents with the given <paramref name="sort"/>.
        /// </summary>
        public SortingMergePolicy(MergePolicy @in, Sort sort)
        {
            this.@in = @in;
            this.sorter = new Sorter(sort);
            this.sort = sort;
        }

        public override MergeSpecification FindMerges(MergeTrigger mergeTrigger, SegmentInfos segmentInfos)
        {
            return SortedMergeSpecification(@in.FindMerges(mergeTrigger, segmentInfos));
        }

        public override MergeSpecification FindForcedMerges(SegmentInfos segmentInfos, int maxSegmentCount, IDictionary<SegmentCommitInfo, bool> segmentsToMerge)
        {
            return SortedMergeSpecification(@in.FindForcedMerges(segmentInfos, maxSegmentCount, segmentsToMerge));
        }

        public override MergeSpecification FindForcedDeletesMerges(SegmentInfos segmentInfos)
        {
            return SortedMergeSpecification(@in.FindForcedDeletesMerges(segmentInfos));
        }

        public override object Clone()
        {
            return new SortingMergePolicy((MergePolicy)@in.Clone(), sort);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                @in.Dispose();
            }
        }

        public override bool UseCompoundFile(SegmentInfos segments, SegmentCommitInfo newSegment)
        {
            return @in.UseCompoundFile(segments, newSegment);
        }

        public override void SetIndexWriter(IndexWriter writer)
        {
            @in.SetIndexWriter(writer);
        }

        public override string ToString()
        {
            return "SortingMergePolicy(" + @in + ", sorter=" + sorter + ")";
        }
    }
}