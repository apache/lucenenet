using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    public sealed class FieldCacheRewriteMethod : MultiTermQuery.RewriteMethod
    {
        public override Query Rewrite(IndexReader reader, MultiTermQuery query)
        {
            Query result = new ConstantScoreQuery(new MultiTermQueryFieldCacheWrapperFilter(query));
            result.Boost = query.Boost;
            return result;
        }

        internal class MultiTermQueryFieldCacheWrapperFilter : Filter
        {
            protected readonly MultiTermQuery query;

            public MultiTermQueryFieldCacheWrapperFilter(MultiTermQuery query)
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
                    return this.query.Equals(((MultiTermQueryFieldCacheWrapperFilter)o).query);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return query.GetHashCode();
            }

            public string Field
            {
                get { return query.Field; }
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                SortedDocValues fcsi = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, query.Field);
                // Cannot use FixedBitSet because we require long index (ord):
                OpenBitSet termSet = new OpenBitSet(fcsi.ValueCount);

                TermsEnum termsEnum = query.GetTermsEnum(new AnonymousTerms(fcsi));

                //assert termsEnum != null;
                if (termsEnum.Next() != null)
                {
                    // fill into a OpenBitSet
                    do
                    {
                        long ord = termsEnum.Ord;
                        if (ord >= 0)
                        {
                            termSet.Set(ord);
                        }
                    } while (termsEnum.Next() != null);
                }
                else
                {
                    return DocIdSet.EMPTY_DOCIDSET;
                }

                return new AnonymousFieldCacheDocIdSet(fcsi, termSet, context.Reader.MaxDoc, acceptDocs);
            }

            private sealed class AnonymousFieldCacheDocIdSet : FieldCacheDocIdSet
            {
                private readonly SortedDocValues fcsi;
                private readonly OpenBitSet termSet;

                public AnonymousFieldCacheDocIdSet(SortedDocValues fcsi, OpenBitSet termSet, int maxDoc, IBits acceptDocs)
                    : base(maxDoc, acceptDocs)
                {
                    this.fcsi = fcsi;
                    this.termSet = termSet;
                }

                protected override bool MatchDoc(int doc)
                {
                    int ord = fcsi.GetOrd(doc);
                    if (ord == -1)
                    {
                        return false;
                    }
                    return termSet.Get(ord);
                }
            }

            private sealed class AnonymousTerms : Terms
            {
                private readonly SortedDocValues fcsi;

                public AnonymousTerms(SortedDocValues fcsi)
                {
                    this.fcsi = fcsi;
                }

                public override IComparer<BytesRef> Comparator
                {
                    get { return BytesRef.UTF8SortedAsUnicodeComparer; }
                }

                public override TermsEnum Iterator(TermsEnum reuse)
                {
                    return fcsi.TermsEnum;
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
            return 641;
        }
    }
}
