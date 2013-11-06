using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class MatchingDocsAsScoredDocIDs : IScoredDocIDs
    {
        readonly List<FacetsCollector.MatchingDocs> matchingDocs;
        readonly int size;

        public MatchingDocsAsScoredDocIDs(List<FacetsCollector.MatchingDocs> matchingDocs)
        {
            this.matchingDocs = matchingDocs;
            int totalSize = 0;
            foreach (FacetsCollector.MatchingDocs md in matchingDocs)
            {
                totalSize += md.totalHits;
            }

            this.size = totalSize;
        }

        public IScoredDocIDsIterator Iterator()
        {
            return new AnonymousScoredDocIDsIterator(this);
        }

        private sealed class AnonymousScoredDocIDsIterator : IScoredDocIDsIterator
        {
            public AnonymousScoredDocIDsIterator(MatchingDocsAsScoredDocIDs parent)
            {
                this.parent = parent;
                mdIter = parent.matchingDocs.GetEnumerator();
            }

            private readonly MatchingDocsAsScoredDocIDs parent;
            readonly IEnumerator<FacetsCollector.MatchingDocs> mdIter; // = parent.matchingDocs.GetEnumerator();
            int scoresIdx = 0;
            int doc = 0;
            FacetsCollector.MatchingDocs current;
            int currentLength;
            bool done = false;

            public bool Next()
            {
                if (done)
                {
                    return false;
                }

                while (current == null)
                {
                    if (!mdIter.MoveNext())
                    {
                        done = true;
                        return false;
                    }

                    current = mdIter.Current;
                    currentLength = current.bits.Length;
                    doc = 0;
                    scoresIdx = 0;
                    if (doc >= currentLength || (doc = current.bits.NextSetBit(doc)) == -1)
                    {
                        current = null;
                    }
                    else
                    {
                        doc = -1;
                    }
                }

                ++doc;
                if (doc >= currentLength || (doc = current.bits.NextSetBit(doc)) == -1)
                {
                    current = null;
                    return Next();
                }

                return true;
            }

            public float Score
            {
                get
                {
                    return current.scores == null ? ScoredDocIDsIterator.DEFAULT_SCORE : current.scores[scoresIdx++];
                }
            }

            public int DocID
            {
                get
                {
                    return done ? DocIdSetIterator.NO_MORE_DOCS : doc + current.context.docBase;
                }
            }
        }

        public DocIdSet DocIDs
        {
            get
            {
                return new AnonymousDocIdSet(this);
            }
        }

        private sealed class AnonymousDocIdSetIterator : DocIdSetIterator
        {
            public AnonymousDocIdSetIterator(AnonymousDocIdSet parent)
            {
                this.parent = parent;
            }

            private readonly AnonymousDocIdSet parent;

            public override int NextDoc()
            {
                if (parent.done)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

                while (parent.current == null)
                {
                    if (!parent.mdIter.MoveNext())
                    {
                        parent.done = true;
                        return DocIdSetIterator.NO_MORE_DOCS;
                    }

                    parent.current = parent.mdIter.Current;
                    parent.currentLength = parent.current.bits.Length;
                    parent.doc = 0;
                    if (parent.doc >= parent.currentLength || (parent.doc = parent.current.bits.NextSetBit(parent.doc)) == -1)
                    {
                        parent.current = null;
                    }
                    else
                    {
                        parent.doc = -1;
                    }
                }

                ++parent.doc;
                if (parent.doc >= parent.currentLength || (parent.doc = parent.current.bits.NextSetBit(parent.doc)) == -1)
                {
                    parent.current = null;
                    return NextDoc();
                }

                return parent.doc + parent.current.context.docBase;
            }

            public override int DocID
            {
                get
                {
                    return parent.doc + parent.current.context.docBase;
                }
            }

            public override long Cost
            {
                get
                {
                    return parent.parent.size;
                }
            }

            public override int Advance(int target)
            {
                throw new NotSupportedException(@"not supported");
            }
        }

        private sealed class AnonymousDocIdSet : DocIdSet
        {
            public AnonymousDocIdSet(MatchingDocsAsScoredDocIDs parent)
            {
                this.parent = parent;
                mdIter = parent.matchingDocs.GetEnumerator();
            }

            internal readonly MatchingDocsAsScoredDocIDs parent;
            internal readonly IEnumerator<FacetsCollector.MatchingDocs> mdIter; // = matchingDocs.GetEnumerator();
            internal int doc = 0;
            internal FacetsCollector.MatchingDocs current;
            internal int currentLength;
            internal bool done = false;
            
            public override DocIdSetIterator Iterator()
            {
                return new AnonymousDocIdSetIterator(this);
            }
        }

        public int Size
        {
            get
            {
                return size;
            }
        }
    }
}
