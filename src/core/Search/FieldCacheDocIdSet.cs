using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    public abstract class FieldCacheDocIdSet : DocIdSet
    {
        protected readonly int maxDoc;
        protected readonly IBits acceptDocs;

        public FieldCacheDocIdSet(int maxDoc, IBits acceptDocs)
        {
            this.maxDoc = maxDoc;
            this.acceptDocs = acceptDocs;
        }

        protected abstract bool MatchDoc(int doc);

        public override bool IsCacheable
        {
            get
            {
                return true;
            }
        }

        public override IBits Bits
        {
            get
            {
                return (acceptDocs == null) ? (IBits)new AnonymousNoAcceptDocsBits(this) : new AnonymousAcceptDocsBits(this);
            }
        }

        private sealed class AnonymousNoAcceptDocsBits : IBits
        {
            private readonly FieldCacheDocIdSet parent;

            public AnonymousNoAcceptDocsBits(FieldCacheDocIdSet parent)
            {
                this.parent = parent;
            }

            public bool this[int docid]
            {
                get { return parent.MatchDoc(docid); }
            }

            public int Length
            {
                get { return parent.maxDoc; }
            }
        }

        private sealed class AnonymousAcceptDocsBits : IBits
        {
            private readonly FieldCacheDocIdSet parent;

            public AnonymousAcceptDocsBits(FieldCacheDocIdSet parent)
            {
                this.parent = parent;
            }

            public bool this[int docid]
            {
                get { return parent.MatchDoc(docid) && parent.acceptDocs[docid]; }
            }

            public int Length
            {
                get { return parent.maxDoc; }
            }
        }

        public override DocIdSetIterator Iterator()
        {
            if (acceptDocs == null)
            {
                // Specialization optimization disregard acceptDocs
                return new AnonymousNoAcceptDocsIterator(this);
            }
            else if (acceptDocs is FixedBitSet || acceptDocs is OpenBitSet)
            {
                // special case for FixedBitSet / OpenBitSet: use the iterator and filter it
                // (used e.g. when Filters are chained by FilteredQuery)
                return new AnonymousFilteredDocIdSetIterator(this, ((DocIdSet)acceptDocs).Iterator());
            }
            else
            {
                // Stupid consultation of acceptDocs and matchDoc()
                return new AnonymousAcceptDocsIterator(this);
            }
        }

        private sealed class AnonymousNoAcceptDocsIterator : DocIdSetIterator
        {
            private readonly FieldCacheDocIdSet parent;
            private int doc = -1;

            public AnonymousNoAcceptDocsIterator(FieldCacheDocIdSet parent)
            {
                this.parent = parent;
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int NextDoc()
            {
                do
                {
                    doc++;
                    if (doc >= parent.maxDoc)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                } while (!parent.MatchDoc(doc));
                return doc;
            }

            public override int Advance(int target)
            {
                for (doc = target; doc < parent.maxDoc; doc++)
                {
                    if (parent.MatchDoc(doc))
                    {
                        return doc;
                    }
                }
                return doc = NO_MORE_DOCS;
            }

            public override long Cost
            {
                get { return parent.maxDoc; }
            }
        }

        private sealed class AnonymousFilteredDocIdSetIterator : FilteredDocIdSetIterator
        {
            private readonly FieldCacheDocIdSet parent;

            public AnonymousFilteredDocIdSetIterator(FieldCacheDocIdSet parent, DocIdSetIterator innerIter)
                : base(innerIter)
            {
                this.parent = parent;
            }

            public override bool Match(int doc)
            {
                return parent.MatchDoc(doc);
            }
        }

        private sealed class AnonymousAcceptDocsIterator : DocIdSetIterator
        {
            private readonly FieldCacheDocIdSet parent;
            private int doc = -1;

            public AnonymousAcceptDocsIterator(FieldCacheDocIdSet parent)
            {
                this.parent = parent;
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override int NextDoc()
            {
                do
                {
                    doc++;
                    if (doc >= parent.maxDoc)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                } while (!(parent.MatchDoc(doc) && parent.acceptDocs[doc]));
                return doc;
            }

            public override int Advance(int target)
            {
                for (doc = target; doc < parent.maxDoc; doc++)
                {
                    if (parent.MatchDoc(doc) && parent.acceptDocs[doc])
                    {
                        return doc;
                    }
                }
                return doc = NO_MORE_DOCS;
            }

            public override long Cost
            {
                get { return parent.maxDoc; }
            }
        }
    }
}
