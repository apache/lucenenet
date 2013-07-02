using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    public sealed class DocTermOrdsRewriteMethod : MultiTermQuery.RewriteMethod
    {
        public override Query Rewrite(IndexReader reader, MultiTermQuery query)
        {
            Query result = new ConstantScoreQuery(new MultiTermQueryDocTermOrdsWrapperFilter(query));
            result.Boost = query.Boost;
            return result;
        }

        internal class MultiTermQueryDocTermOrdsWrapperFilter : Filter
        {
            protected readonly MultiTermQuery query;

            internal MultiTermQueryDocTermOrdsWrapperFilter(MultiTermQuery query)
            {
                this.query = query;
            }

            public override string ToString()
            {
                // query.toString should be ok for the filter, too, if the query boost is 1.0f
                return query.ToString();
            }

            public override bool Equals(object o)
            {
                if (o == this) return true;
                if (o == null) return false;
                if (this.GetType().Equals(o.GetType()))
                {
                    return this.query.Equals(((MultiTermQueryDocTermOrdsWrapperFilter)o).query);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return query.GetHashCode();
            }

            public string Field
            {
                get
                {
                    return query.Field;
                }
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                SortedSetDocValues docTermOrds = FieldCache.DEFAULT.GetDocTermOrds(context.Reader, query.Field);
                // Cannot use FixedBitSet because we require long index (ord):
                OpenBitSet termSet = new OpenBitSet(docTermOrds.ValueCount);

                TermsEnum termsEnum = query.GetTermsEnum(new AnonymousGetDocIdSetTerms(docTermOrds));

                //assert termsEnum != null;
                if (termsEnum.Next() != null)
                {
                    // fill into a OpenBitSet
                    do
                    {
                        termSet.Set(termsEnum.Ord);
                    } while (termsEnum.Next() != null);
                }
                else
                {
                    return DocIdSet.EMPTY_DOCIDSET;
                }

                return new AnonymousFieldCacheDocIdSet(context.Reader.MaxDoc, acceptDocs, docTermOrds, termSet);
            }

            private sealed class AnonymousGetDocIdSetTerms : Terms
            {
                private readonly SortedSetDocValues docTermOrds;

                public AnonymousGetDocIdSetTerms(SortedSetDocValues docTermOrds)
                {
                    this.docTermOrds = docTermOrds;
                }

                public override IComparer<BytesRef> Comparator
                {
                    get { return BytesRef.UTF8SortedAsUnicodeComparer; }
                }

                public override TermsEnum Iterator(TermsEnum reuse)
                {
                    return docTermOrds.TermsEnum;
                }

                public override long SumTotalTermFreq
                {
                    get { return -1; }
                }

                public override long SumDocFreq
                {
                    get { return -1; }
                }

                public override int DocCount
                {
                    get { return -1; }
                }

                public override long Size
                {
                    get { return -1; }
                }

                public override bool HasOffsets
                {
                    get { return false; }
                }

                public override bool HasPositions
                {
                    get { return false; }
                }

                public override bool HasPayloads
                {
                    get { return false; }
                }
            }

            private sealed class AnonymousFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private readonly SortedSetDocValues docTermOrds;
                private readonly OpenBitSet termSet;

                public AnonymousFieldCacheDocIdSet(int maxDoc, IBits acceptDocs, SortedSetDocValues docTermOrds, OpenBitSet termSet)
                    : base(maxDoc, acceptDocs)
                {
                    this.docTermOrds = docTermOrds;
                    this.termSet = termSet;
                }

                protected override bool MatchDoc(int doc)
                {
                    docTermOrds.SetDocument(doc);
                    long ord;
                    // TODO: we could track max bit set and early terminate (since they come in sorted order)
                    while ((ord = docTermOrds.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        if (termSet[(int)ord])
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return 877;
        }
    }
}
