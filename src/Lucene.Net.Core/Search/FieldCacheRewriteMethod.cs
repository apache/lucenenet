using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Search
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LongBitSet = Lucene.Net.Util.LongBitSet;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Rewrites MultiTermQueries into a filter, using the FieldCache for term enumeration.
    /// <p>
    /// this can be used to perform these queries against an unindexed docvalues field.
    /// @lucene.experimental
    /// </summary>
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
            protected internal readonly MultiTermQuery Query;// LUCENENET TODO: Rename (private)

            /// <summary>
            /// Wrap a <seealso cref="MultiTermQuery"/> as a Filter.
            /// </summary>
            protected internal MultiTermQueryFieldCacheWrapperFilter(MultiTermQuery query)
            {
                this.Query = query;
            }

            public override string ToString()
            {
                // query.toString should be ok for the filter, too, if the query boost is 1.0f
                return Query.ToString();
            }

            public override sealed bool Equals(object o)
            {
                if (o == this)
                {
                    return true;
                }
                if (o == null)
                {
                    return false;
                }
                if (this.GetType().Equals(o.GetType()))
                {
                    return this.Query.Equals(((MultiTermQueryFieldCacheWrapperFilter)o).Query);
                }
                return false;
            }

            public override sealed int GetHashCode()
            {
                return Query.GetHashCode();
            }

            /// <summary>
            /// Returns the field name for this query </summary>
            public string Field
            {
                get
                {
                    return Query.Field;
                }
            }

            /// <summary>
            /// Returns a DocIdSet with documents that should be permitted in search
            /// results.
            /// </summary>
            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                SortedDocValues fcsi = FieldCache.DEFAULT.GetTermsIndex((context.AtomicReader), Query.field);
                // Cannot use FixedBitSet because we require long index (ord):
                LongBitSet termSet = new LongBitSet(fcsi.ValueCount);
                TermsEnum termsEnum = Query.GetTermsEnum(new TermsAnonymousInnerClassHelper(this, fcsi));

                Debug.Assert(termsEnum != null);
                if (termsEnum.Next() != null)
                {
                    // fill into a bitset
                    do
                    {
                        long ord = termsEnum.Ord();
                        if (ord >= 0)
                        {
                            termSet.Set(ord);
                        }
                    } while (termsEnum.Next() != null);
                }
                else
                {
                    return null;
                }

                return new FieldCacheDocIdSetAnonymousInnerClassHelper(this, context.Reader.MaxDoc, acceptDocs, fcsi, termSet);
            }

            private class TermsAnonymousInnerClassHelper : Terms
            {
                private readonly MultiTermQueryFieldCacheWrapperFilter OuterInstance;

                private SortedDocValues Fcsi;

                public TermsAnonymousInnerClassHelper(MultiTermQueryFieldCacheWrapperFilter outerInstance, SortedDocValues fcsi)
                {
                    this.OuterInstance = outerInstance;
                    this.Fcsi = fcsi;
                }

                public override IComparer<BytesRef> Comparator
                {
                    get
                    {
                        return BytesRef.UTF8SortedAsUnicodeComparer;
                    }
                }

                public override TermsEnum Iterator(TermsEnum reuse)
                {
                    return Fcsi.TermsEnum();
                }

                public override long SumTotalTermFreq
                {
                    get
                    {
                        return -1;
                    }
                }

                public override long SumDocFreq
                {
                    get
                    {
                        return -1;
                    }
                }

                public override int DocCount
                {
                    get
                    {
                        return -1;
                    }
                }

                public override long Size
                {
                    get { return -1; }
                }

                public override bool HasFreqs
                {
                    get { return false; }
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

            private class FieldCacheDocIdSetAnonymousInnerClassHelper : FieldCacheDocIdSet
            {
                private readonly MultiTermQueryFieldCacheWrapperFilter OuterInstance;

                private SortedDocValues Fcsi;
                private LongBitSet TermSet;

                public FieldCacheDocIdSetAnonymousInnerClassHelper(MultiTermQueryFieldCacheWrapperFilter outerInstance, int maxDoc, Bits acceptDocs, SortedDocValues fcsi, LongBitSet termSet)
                    : base(maxDoc, acceptDocs)
                {
                    this.OuterInstance = outerInstance;
                    this.Fcsi = fcsi;
                    this.TermSet = termSet;
                }

                protected internal override sealed bool MatchDoc(int doc)
                {
                    int ord = Fcsi.GetOrd(doc);
                    if (ord == -1)
                    {
                        return false;
                    }
                    return TermSet.Get(ord);
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj == null)
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return 641;
        }
    }
}