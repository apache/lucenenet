using Lucene.Net.Facet.Search;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Util
{
    public static class ScoredDocIdsUtils
    {
        public static IScoredDocIDs GetComplementSet(IScoredDocIDs docids, IndexReader reader)
        {
            int maxDoc = reader.MaxDoc;
            DocIdSet docIdSet = docids.DocIDs;
            FixedBitSet complement;
            if (docIdSet is FixedBitSet)
            {
                complement = ((FixedBitSet)docIdSet).Clone();
            }
            else
            {
                complement = new FixedBitSet(maxDoc);
                DocIdSetIterator iter = docIdSet.Iterator();
                int doc;
                while ((doc = iter.NextDoc()) < maxDoc)
                {
                    complement.Set(doc);
                }
            }

            complement.Flip(0, maxDoc);
            ClearDeleted(reader, complement);
            return CreateScoredDocIds(complement, maxDoc);
        }

        private static void ClearDeleted(IndexReader reader, FixedBitSet set)
        {
            if (!reader.HasDeletions)
            {
                return;
            }

            DocIdSetIterator it = set.Iterator();
            int doc = it.NextDoc();
            foreach (AtomicReaderContext context in reader.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                int maxDoc = r.MaxDoc + context.docBase;
                if (doc >= maxDoc)
                {
                    continue;
                }

                if (!r.HasDeletions)
                {
                    while ((doc = it.NextDoc()) < maxDoc)
                    {
                    }

                    continue;
                }

                IBits liveDocs = r.LiveDocs;
                do
                {
                    if (!liveDocs[doc - context.docBase])
                    {
                        set.Clear(doc);
                    }
                }
                while ((doc = it.NextDoc()) < maxDoc);
            }
        }

        public static IScoredDocIDs CreateScoredDocIDsSubset(IScoredDocIDs allDocIds, int[] sampleSet)
        {
            int[] docids = sampleSet;
            Array.Sort(docids);
            float[] scores = new float[docids.Length];
            IScoredDocIDsIterator it = allDocIds.Iterator();
            int n = 0;
            while (it.Next() && n < docids.Length)
            {
                int doc = it.DocID;
                if (doc == docids[n])
                {
                    scores[n] = it.Score;
                    ++n;
                }
            }

            int size = n;
            return new AnonymousScoredDocIDs(size, docids, scores);
        }

        private sealed class AnonymousDocIdSetIterator : DocIdSetIterator
        {
            private int next = -1;

            private readonly int size;
            private readonly int[] docids;

            public AnonymousDocIdSetIterator(int size, int[] docids)
            {
                this.size = size;
                this.docids = docids;
            }

            public override int Advance(int target)
            {
                while (next < size && docids[next++] < target)
                {
                }

                return next == size ? NO_MORE_DOCS : docids[next];
            }

            public override int DocID
            {
                get
                {
                    return docids[next];
                }
            }

            public override int NextDoc()
            {
                if (++next >= size)
                {
                    return NO_MORE_DOCS;
                }

                return docids[next];
            }

            public override long Cost
            {
                get
                {
                    return size;
                }
            }
        }

        private sealed class AnonymousDocIdSet : DocIdSet
        {
            private readonly int size;
            private readonly int[] docids;

            public AnonymousDocIdSet(int size, int[] docids)
            {
                this.size = size;
                this.docids = docids;
            }

            public override bool IsCacheable
            {
                get
                {
                    return true;
                }
            }

            public override DocIdSetIterator Iterator()
            {
                return new AnonymousDocIdSetIterator(size, docids);
            }
        }

        private sealed class AnonymousScoredDocIDsIterator : IScoredDocIDsIterator
        {
            int next = -1;

            private readonly int size;
            private readonly int[] docids;
            private readonly float[] scores;

            public AnonymousScoredDocIDsIterator(int size, int[] docids, float[] scores)
            {
                this.size = size;
                this.docids = docids;
                this.scores = scores;
            }

            public bool Next()
            {
                return ++next < size;
            }

            public float Score
            {
                get
                {
                    return scores[next];
                }
            }

            public int DocID
            {
                get
                {
                    return docids[next];
                }
            }
        }

        private sealed class AnonymousScoredDocIDs : IScoredDocIDs
        {
            private readonly int size;
            private readonly int[] docids;
            private readonly float[] scores;

            public AnonymousScoredDocIDs(int size, int[] docids, float[] scores)
            {
                this.size = size;
                this.docids = docids;
                this.scores = scores;
            }

            public DocIdSet DocIDs
            {
                get
                {
                    return new AnonymousDocIdSet(size, docids);
                }
            }

            public IScoredDocIDsIterator Iterator()
            {
                return new AnonymousScoredDocIDsIterator(size, docids, scores);
            }

            public int Size
            {
                get
                {
                    return size;
                }
            }
        }

        public static IScoredDocIDs CreateAllDocsScoredDocIDs(IndexReader reader)
        {
            if (reader.HasDeletions)
            {
                return new AllLiveDocsScoredDocIDs(reader);
            }

            return new AllDocsScoredDocIDs(reader);
        }

        public static IScoredDocIDs CreateScoredDocIds(DocIdSet docIdSet, int maxDoc)
        {
            return new AnonymousScoredDocIDs1(docIdSet, maxDoc);
        }

        private sealed class AnonymousScoredDocIDsIterator1 : IScoredDocIDsIterator
        {
            public AnonymousScoredDocIDsIterator1(DocIdSetIterator docIterator)
            {
                this.docIterator = docIterator;
            }

            private readonly DocIdSetIterator docIterator;

            public bool Next()
            {
                try
                {
                    return docIterator.NextDoc() != DocIdSetIterator.NO_MORE_DOCS;
                }
                catch (IOException e)
                {
                    throw;
                }
            }

            public float Score
            {
                get
                {
                    return ScoredDocIDsIterator.DEFAULT_SCORE;
                }
            }

            public int DocID
            {
                get
                {
                    return docIterator.DocID;
                }
            }
        }

        private sealed class AnonymousScoredDocIDs1 : IScoredDocIDs
        {
            public AnonymousScoredDocIDs1(DocIdSet docIdSet, int maxDoc)
            {
                this.docIdSet = docIdSet;
                this.maxDoc = maxDoc;
            }

            private int size = -1;
            private readonly DocIdSet docIdSet;
            private readonly int maxDoc;

            public DocIdSet DocIDs
            {
                get
                {
                    return docIdSet;
                }
            }

            public IScoredDocIDsIterator Iterator()
            {
                DocIdSetIterator docIterator = docIdSet.Iterator();
                return new AnonymousScoredDocIDsIterator1(docIterator);
            }

            public int Size
            {
                get
                {
                    if (size < 0)
                    {
                        OpenBitSetDISI openBitSetDISI;
                        try
                        {
                            openBitSetDISI = new OpenBitSetDISI(docIdSet.Iterator(), maxDoc);
                        }
                        catch (IOException)
                        {
                            throw;
                        }

                        size = (int)openBitSetDISI.Cardinality;
                    }

                    return size;
                }
            }
        }

        private class AllDocsScoredDocIDs : IScoredDocIDs
        {
            readonly int maxDoc;
            public AllDocsScoredDocIDs(IndexReader reader)
            {
                this.maxDoc = reader.MaxDoc;
            }

            public int Size
            {
                get
                {
                    return maxDoc;
                }
            }

            public DocIdSet DocIDs
            {
                get
                {
                    return new AnonymousDocIdSet1(this);
                }
            }

            private sealed class AnonymousDocIdSetIterator1 : DocIdSetIterator
            {
                public AnonymousDocIdSetIterator1(AllDocsScoredDocIDs parent)
                {
                    this.parent = parent;
                }

                private readonly AllDocsScoredDocIDs parent;
                private int next = -1;

                public override int Advance(int target)
                {
                    if (target <= next)
                    {
                        target = next + 1;
                    }

                    return next = target >= parent.maxDoc ? NO_MORE_DOCS : target;
                }

                public override int DocID
                {
                    get
                    {
                        return next;
                    }
                }

                public override int NextDoc()
                {
                    return ++next < parent.maxDoc ? next : NO_MORE_DOCS;
                }

                public override long Cost
                {
                    get
                    {
                        return parent.maxDoc;
                    }
                }
            }

            private sealed class AnonymousDocIdSet1 : DocIdSet
            {
                public AnonymousDocIdSet1(AllDocsScoredDocIDs parent)
                {
                    this.parent = parent;
                }

                private readonly AllDocsScoredDocIDs parent;

                public override bool IsCacheable
                {
                    get
                    {
                        return true;
                    }
                }

                public override DocIdSetIterator Iterator()
                {
                    return new AnonymousDocIdSetIterator1(parent);
                }
            }

            public IScoredDocIDsIterator Iterator()
            {
                try
                {
                    DocIdSetIterator iter = DocIDs.Iterator();
                    return new AnonymousScoredDocIDsIterator2(iter);
                }
                catch (IOException)
                {
                    throw;
                }
            }

            private sealed class AnonymousScoredDocIDsIterator2 : IScoredDocIDsIterator
            {
                public AnonymousScoredDocIDsIterator2(DocIdSetIterator iter)
                {
                    this.iter = iter;
                }

                private readonly DocIdSetIterator iter;

                public bool Next()
                {
                    try
                    {
                        return iter.NextDoc() != DocIdSetIterator.NO_MORE_DOCS;
                    }
                    catch (IOException)
                    {
                        return false;
                    }
                }

                public float Score
                {
                    get
                    {
                        return ScoredDocIDsIterator.DEFAULT_SCORE;
                    }
                }

                public int DocID
                {
                    get
                    {
                        return iter.DocID;
                    }
                }
            }
        }

        private sealed class AllLiveDocsScoredDocIDs : IScoredDocIDs
        {
            readonly int maxDoc;
            readonly IndexReader reader;

            internal AllLiveDocsScoredDocIDs(IndexReader reader)
            {
                this.maxDoc = reader.MaxDoc;
                this.reader = reader;
            }

            public int Size
            {
                get
                {
                    return reader.NumDocs;
                }
            }

            public DocIdSet DocIDs
            {
                get
                {
                    return new AnonymousDocIdSet2(this);
                }
            }

            private sealed class AnonymousDocIdSetIterator2 : DocIdSetIterator
            {
                public AnonymousDocIdSetIterator2(AllLiveDocsScoredDocIDs parent)
                {
                    this.parent = parent;
                    liveDocs = MultiFields.GetLiveDocs(parent.reader);
                }

                private readonly AllLiveDocsScoredDocIDs parent;
                readonly IBits liveDocs; // = MultiFields.GetLiveDocs(parent.reader);
                private int next = -1;

                public override int Advance(int target)
                {
                    if (target > next)
                    {
                        next = target - 1;
                    }

                    return NextDoc();
                }

                public override int DocID
                {
                    get
                    {
                        return next;
                    }
                }

                public override int NextDoc()
                {
                    do
                    {
                        ++next;
                    }
                    while (next < parent.maxDoc && liveDocs != null && !liveDocs[next]);
                    return next < parent.maxDoc ? next : NO_MORE_DOCS;
                }

                public override long Cost
                {
                    get
                    {
                        return parent.maxDoc;
                    }
                }
            }

            private sealed class AnonymousDocIdSet2 : DocIdSet
            {
                public AnonymousDocIdSet2(AllLiveDocsScoredDocIDs parent)
                {
                    this.parent = parent;
                }

                private readonly AllLiveDocsScoredDocIDs parent;

                public override bool IsCacheable
                {
                    get
                    {
                        return true;
                    }
                }

                public override DocIdSetIterator Iterator()
                {
                    return new AnonymousDocIdSetIterator2(parent);
                }
            }

            public IScoredDocIDsIterator Iterator()
            {
                try
                {
                    DocIdSetIterator iter = DocIDs.Iterator();
                    return new AnonymousScoredDocIDsIterator3(iter);
                }
                catch (IOException e)
                {
                    throw;
                }
            }

            private sealed class AnonymousScoredDocIDsIterator3 : IScoredDocIDsIterator
            {
                public AnonymousScoredDocIDsIterator3(DocIdSetIterator iter)
                {
                    this.iter = iter;
                }

                private readonly DocIdSetIterator iter;

                public bool Next()
                {
                    try
                    {
                        return iter.NextDoc() != DocIdSetIterator.NO_MORE_DOCS;
                    }
                    catch (IOException)
                    {
                        return false;
                    }
                }

                public float Score
                {
                    get
                    {
                        return ScoredDocIDsIterator.DEFAULT_SCORE;
                    }
                }

                public int DocID
                {
                    get
                    {
                        return iter.DocID;
                    }
                }
            }
        }
    }
}
