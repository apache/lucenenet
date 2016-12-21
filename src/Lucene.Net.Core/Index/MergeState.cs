using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
{
    using Bits = Lucene.Net.Util.Bits;

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
    using InfoStream = Lucene.Net.Util.InfoStream;
    using MonotonicAppendingLongBuffer = Lucene.Net.Util.Packed.MonotonicAppendingLongBuffer;

    /// <summary>
    /// Holds common state used during segment merging.
    ///
    /// @lucene.experimental
    /// </summary>
    public class MergeState
    {
        /// <summary>
        /// Remaps docids around deletes during merge
        /// </summary>
        public abstract class DocMap
        {
            internal DocMap()
            {
            }

            /// <summary>
            /// Returns the mapped docID corresponding to the provided one. </summary>
            public abstract int Get(int docID);

            /// <summary>
            /// Returns the total number of documents, ignoring
            ///  deletions.
            /// </summary>
            public abstract int MaxDoc { get; }

            /// <summary>
            /// Returns the number of not-deleted documents. </summary>
            public int NumDocs
            {
                get { return MaxDoc - NumDeletedDocs; }
            }

            /// <summary>
            /// Returns the number of deleted documents. </summary>
            public abstract int NumDeletedDocs { get; }

            /// <summary>
            /// Returns true if there are any deletions. </summary>
            public virtual bool HasDeletions
            {
                get { return NumDeletedDocs > 0; }
            }

            /// <summary>
            /// Creates a <seealso cref="DocMap"/> instance appropriate for
            ///  this reader.
            /// </summary>
            public static DocMap Build(AtomicReader reader)
            {
                int maxDoc = reader.MaxDoc;
                if (!reader.HasDeletions)
                {
                    return new NoDelDocMap(maxDoc);
                }
                Bits liveDocs = reader.LiveDocs;
                return Build(maxDoc, liveDocs);
            }

            internal static DocMap Build(int maxDoc, Bits liveDocs)
            {
                Debug.Assert(liveDocs != null);
                MonotonicAppendingLongBuffer docMap = new MonotonicAppendingLongBuffer();
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
                Debug.Assert(docMap.Size() == maxDoc);
                return new DocMapAnonymousInnerClassHelper(maxDoc, liveDocs, docMap, numDeletedDocs);
            }

            private class DocMapAnonymousInnerClassHelper : DocMap
            {
                private int maxDoc;
                private Bits LiveDocs;
                private MonotonicAppendingLongBuffer docMap;
                private int numDeletedDocs;

                public DocMapAnonymousInnerClassHelper(int maxDoc, Bits liveDocs, MonotonicAppendingLongBuffer docMap, int numDeletedDocs)
                {
                    this.maxDoc = maxDoc;
                    this.LiveDocs = liveDocs;
                    this.docMap = docMap;
                    this.numDeletedDocs = numDeletedDocs;
                }

                public override int Get(int docID)
                {
                    if (!LiveDocs.Get(docID))
                    {
                        return -1;
                    }
                    return (int)docMap.Get(docID);
                }

                public override int MaxDoc
                {
                    get { return maxDoc; }
                }

                public override int NumDeletedDocs
                {
                    get { return numDeletedDocs; }
                }
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

            public override int MaxDoc
            {
                get { return maxDoc; }
            }

            public override int NumDeletedDocs
            {
                get { return 0; }
            }
        }

        /// <summary>
        /// <seealso cref="SegmentInfo"/> of the newly merged segment. </summary>
        public SegmentInfo SegmentInfo { get; private set; }

        /// <summary>
        /// <seealso cref="FieldInfos"/> of the newly merged segment. </summary>
        public FieldInfos FieldInfos { get; set; }

        /// <summary>
        /// Readers being merged. </summary>
        public IList<AtomicReader> Readers { get; private set; }

        /// <summary>
        /// Maps docIDs around deletions. </summary>
        public DocMap[] DocMaps; // LUCENENET TODO: Make property ? arrays shouldn't be properties - perhaps leave a field?

        /// <summary>
        /// New docID base per reader. </summary>
        public int[] DocBase; // LUCENENET TODO: Make property ?

        /// <summary>
        /// Holds the CheckAbort instance, which is invoked
        ///  periodically to see if the merge has been aborted.
        /// </summary>
        public CheckAbort CheckAbort { get; private set; }

        /// <summary>
        /// InfoStream for debugging messages. </summary>
        public InfoStream InfoStream { get; private set; }

        // TODO: get rid of this? it tells you which segments are 'aligned' (e.g. for bulk merging)
        // but is this really so expensive to compute again in different components, versus once in SM?

        /// <summary>
        /// <seealso cref="SegmentReader"/>s that have identical field
        /// name/number mapping, so their stored fields and term
        /// vectors may be bulk merged.
        /// </summary>
        public SegmentReader[] MatchingSegmentReaders; // LUCENENET TODO: Make property ?

        /// <summary>
        /// How many <seealso cref="#matchingSegmentReaders"/> are set. </summary>
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
        /// Creates a #CheckAbort instance. </summary>
        public CheckAbort(MergePolicy.OneMerge merge, Directory dir)
        {
            this.merge = merge;
            this.dir = dir;
        }

        /// <summary>
        /// Records the fact that roughly units amount of work
        /// have been done since this method was last called.
        /// When adding time-consuming code into SegmentMerger,
        /// you should test different values for units to ensure
        /// that the time in between calls to merge.checkAborted
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
        /// If you use this: IW.close(false) cannot abort your merge!
        /// @lucene.internal
        /// </summary>
        public static readonly CheckAbort NONE = new CheckAbortAnonymousInnerClassHelper();

        private class CheckAbortAnonymousInnerClassHelper : CheckAbort
        {
            public CheckAbortAnonymousInnerClassHelper()
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