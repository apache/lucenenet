using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

    using Directory = Lucene.Net.Store.Directory;
    using IBits = Lucene.Net.Util.IBits;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using MonotonicAppendingInt64Buffer = Lucene.Net.Util.Packed.MonotonicAppendingInt64Buffer;

    /// <summary>
    /// Holds common state used during segment merging.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class MergeState
    {
        /// <summary>
        /// Remaps docids around deletes during merge
        /// </summary>
        public abstract class DocMap
        {
            private protected DocMap() // LUCENENET: Changed from internal to private protected
            {
            }

            /// <summary>
            /// Returns the mapped docID corresponding to the provided one. </summary>
            public abstract int Get(int docID);

            /// <summary>
            /// Returns the total number of documents, ignoring
            /// deletions.
            /// </summary>
            public abstract int MaxDoc { get; }

            /// <summary>
            /// Returns the number of not-deleted documents. </summary>
            public int NumDocs => MaxDoc - NumDeletedDocs;

            /// <summary>
            /// Returns the number of deleted documents. </summary>
            public abstract int NumDeletedDocs { get; }

            /// <summary>
            /// Returns <c>true</c> if there are any deletions. </summary>
            public virtual bool HasDeletions => NumDeletedDocs > 0;

            /// <summary>
            /// Creates a <see cref="DocMap"/> instance appropriate for
            /// this reader.
            /// </summary>
            public static DocMap Build(AtomicReader reader)
            {
                int maxDoc = reader.MaxDoc;
                if (!reader.HasDeletions)
                {
                    return new NoDelDocMap(maxDoc);
                }
                IBits liveDocs = reader.LiveDocs;
                return Build(maxDoc, liveDocs);
            }

            internal static DocMap Build(int maxDoc, IBits liveDocs)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(liveDocs != null);
                MonotonicAppendingInt64Buffer docMap = new MonotonicAppendingInt64Buffer();
                int del = 0;
                for (int i = 0; i < maxDoc; ++i)
                {
                    docMap.Add(i - del);
                    if (!liveDocs.Get(i))
                    {
                        ++del;
                    }
                }
                docMap.Freeze();
                int numDeletedDocs = del;
                if (Debugging.AssertsEnabled) Debugging.Assert(docMap.Count == maxDoc);
                return new DocMapAnonymousClass(maxDoc, liveDocs, docMap, numDeletedDocs);
            }

            private sealed class DocMapAnonymousClass : DocMap
            {
                private readonly int maxDoc;
                private readonly IBits liveDocs;
                private readonly MonotonicAppendingInt64Buffer docMap;
                private readonly int numDeletedDocs;

                public DocMapAnonymousClass(int maxDoc, IBits liveDocs, MonotonicAppendingInt64Buffer docMap, int numDeletedDocs)
                {
                    this.maxDoc = maxDoc;
                    this.liveDocs = liveDocs;
                    this.docMap = docMap;
                    this.numDeletedDocs = numDeletedDocs;
                }

                public override int Get(int docID)
                {
                    if (!liveDocs.Get(docID))
                    {
                        return -1;
                    }
                    return (int)docMap.Get(docID);
                }

                public override int MaxDoc => maxDoc;

                public override int NumDeletedDocs => numDeletedDocs;
            }
        }

        private sealed class NoDelDocMap : DocMap
        {
            private readonly int maxDoc;

            internal NoDelDocMap(int maxDoc)
            {
                this.maxDoc = maxDoc;
            }

            public override int Get(int docID)
            {
                return docID;
            }

            public override int MaxDoc => maxDoc;

            public override int NumDeletedDocs => 0;
        }

        /// <summary>
        /// <see cref="Index.SegmentInfo"/> of the newly merged segment. </summary>
        public SegmentInfo SegmentInfo { get; private set; }

        /// <summary>
        /// <see cref="Index.FieldInfos"/> of the newly merged segment. </summary>
        public FieldInfos FieldInfos { get; set; }

        /// <summary>
        /// Readers being merged. </summary>
        public IList<AtomicReader> Readers { get; private set; }

        /// <summary>
        /// Maps docIDs around deletions. </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public DocMap[] DocMaps { get; set; }

        /// <summary>
        /// New docID base per reader. </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public int[] DocBase { get; set; }

        /// <summary>
        /// Holds the <see cref="Index.CheckAbort"/> instance, which is invoked
        /// periodically to see if the merge has been aborted.
        /// </summary>
        public CheckAbort CheckAbort { get; private set; }

        /// <summary>
        /// <see cref="Util.InfoStream"/> for debugging messages. </summary>
        public InfoStream InfoStream { get; private set; }

        // TODO: get rid of this? it tells you which segments are 'aligned' (e.g. for bulk merging)
        // but is this really so expensive to compute again in different components, versus once in SM?

        /// <summary>
        /// <see cref="SegmentReader"/>s that have identical field
        /// name/number mapping, so their stored fields and term
        /// vectors may be bulk merged.
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public SegmentReader[] MatchingSegmentReaders { get; set; }

        /// <summary>
        /// How many <see cref="MatchingSegmentReaders"/> are set. </summary>
        public int MatchedCount { get; set; }

        /// <summary>
        /// Sole constructor. </summary>
        internal MergeState(IList<AtomicReader> readers, SegmentInfo segmentInfo, InfoStream infoStream, CheckAbort checkAbort)
        {
            this.Readers = readers;
            this.SegmentInfo = segmentInfo;
            this.InfoStream = infoStream;
            this.CheckAbort = checkAbort;
        }
    }

    /// <summary>
    /// Class for recording units of work when merging segments.
    /// </summary>
    public class CheckAbort // LUCENENET Specific: De-nested this class to fix CLS naming issue
    {
        private double workCount;
        private readonly MergePolicy.OneMerge merge;
        private readonly Directory dir;

        /// <summary>
        /// Creates a <see cref="CheckAbort"/> instance. </summary>
        public CheckAbort(MergePolicy.OneMerge merge, Directory dir)
        {
            this.merge = merge;
            this.dir = dir;
        }

        /// <summary>
        /// Records the fact that roughly units amount of work
        /// have been done since this method was last called.
        /// When adding time-consuming code into <see cref="SegmentMerger"/>,
        /// you should test different values for units to ensure
        /// that the time in between calls to merge.CheckAborted
        /// is up to ~ 1 second.
        /// </summary>
        public virtual void Work(double units)
        {
            workCount += units;
            if (workCount >= 10000.0)
            {
                merge.CheckAborted(dir);
                workCount = 0;
            }
        }

        /// <summary>
        /// If you use this: IW.Dispose(false) cannot abort your merge!
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public static readonly CheckAbort NONE = new CheckAbortAnonymousClass();

        private sealed class CheckAbortAnonymousClass : CheckAbort
        {
            public CheckAbortAnonymousClass()
                : base(null, null)
            {
            }

            public override void Work(double units)
            {
                // do nothing
            }
        }
    }
}