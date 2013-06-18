using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Index
{
    public class MergeState
    {
        public abstract class DocMap
        {
            public DocMap()
            {
            }

            public abstract int this[int docID] { get; }

            public abstract int MaxDoc { get; }

            public int NumDocs
            {
                get { return MaxDoc - NumDeletedDocs; }
            }

            public abstract int NumDeletedDocs { get; }

            public bool HasDeletions
            {
                get { return NumDeletedDocs > 0; }
            }

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

            private sealed class AnonymousBuildDocMap : DocMap
            {
                private readonly IBits liveDocs;
                private readonly MonotonicAppendingLongBuffer docMap;
                private readonly int maxDoc;
                private readonly int numDeletedDocs;

                public AnonymousBuildDocMap(IBits liveDocs, MonotonicAppendingLongBuffer docMap, int maxDoc, int numDeletedDocs)
                {
                    this.liveDocs = liveDocs;
                    this.docMap = docMap;
                    this.maxDoc = maxDoc;
                    this.numDeletedDocs = numDeletedDocs;
                }

                public override int this[int docID]
                {
                    get
                    {
                        if (!liveDocs[docID])
                        {
                            return -1;
                        }
                        return (int)docMap.Get(docID);
                    }
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

            internal static DocMap Build(int maxDoc, IBits liveDocs)
            {
                //assert liveDocs != null;
                MonotonicAppendingLongBuffer docMap = new MonotonicAppendingLongBuffer();
                int del = 0;
                for (int i = 0; i < maxDoc; ++i)
                {
                    docMap.Add(i - del);
                    if (!liveDocs[i])
                    {
                        ++del;
                    }
                }
                int numDeletedDocs = del;
                //assert docMap.size() == maxDoc;

                return new AnonymousBuildDocMap(liveDocs, docMap, maxDoc, numDeletedDocs);
            }
        }

        private sealed class NoDelDocMap : DocMap
        {
            private readonly int maxDoc;

            public NoDelDocMap(int maxDoc)
            {
                this.maxDoc = maxDoc;
            }

            public override int this[int docID]
            {
                get { return docID; }
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

        public readonly SegmentInfo segmentInfo;

        public FieldInfos fieldInfos;

        public readonly IList<AtomicReader> readers;

        public DocMap[] docMaps;

        public int[] docBase;

        public readonly CheckAbort checkAbort;

        public readonly InfoStream infoStream;

        public SegmentReader[] matchingSegmentReaders;

        public int matchedCount;

        internal MergeState(IList<AtomicReader> readers, SegmentInfo segmentInfo, InfoStream infoStream, CheckAbort checkAbort)
        {
            this.readers = readers;
            this.segmentInfo = segmentInfo;
            this.infoStream = infoStream;
            this.checkAbort = checkAbort;
        }

        public class CheckAbort
        {
            private double workCount;
            private readonly MergePolicy.OneMerge merge;
            private readonly Directory dir;

            /** Creates a #CheckAbort instance. */
            public CheckAbort(MergePolicy.OneMerge merge, Directory dir)
            {
                this.merge = merge;
                this.dir = dir;
            }

            public virtual void Work(double units)
            {
                workCount += units;
                if (workCount >= 10000.0)
                {
                    merge.CheckAborted(dir);
                    workCount = 0;
                }
            }

            private sealed class AnonymousNoneMergeStateCheckAbort : MergeState.CheckAbort
            {
                public AnonymousNoneMergeStateCheckAbort()
                    : base(null, null)
                {
                }

                public override void Work(double units)
                {
                    // do nothing
                }
            }

            internal static readonly MergeState.CheckAbort NONE = new AnonymousNoneMergeStateCheckAbort();
        }
    }
}
