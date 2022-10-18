using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;

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
    using BytesRef = Lucene.Net.Util.BytesRef;
    using IBits = Lucene.Net.Util.IBits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Int64BitSet = Lucene.Net.Util.Int64BitSet;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// Rewrites <see cref="MultiTermQuery"/>s into a filter, using the <see cref="IFieldCache"/> for term enumeration.
    /// <para/>
    /// This can be used to perform these queries against an unindexed docvalues field.
    /// <para/>
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
            protected internal readonly MultiTermQuery m_query;

            /// <summary>
            /// Wrap a <see cref="MultiTermQuery"/> as a Filter.
            /// </summary>
            protected internal MultiTermQueryFieldCacheWrapperFilter(MultiTermQuery query)
            {
                this.m_query = query;
            }

            public override string ToString()
            {
                // query.toString should be ok for the filter, too, if the query boost is 1.0f
                return m_query.ToString();
            }

            public override sealed bool Equals(object o)
            {
                if (o == this)
                {
                    return true;
                }
                if (o is null)
                {
                    return false;
                }
                if (this.GetType().Equals(o.GetType()))
                {
                    return this.m_query.Equals(((MultiTermQueryFieldCacheWrapperFilter)o).m_query);
                }
                return false;
            }

            public override sealed int GetHashCode()
            {
                return m_query.GetHashCode();
            }

            /// <summary>
            /// Returns the field name for this query </summary>
            public string Field => m_query.Field;

            /// <summary>
            /// Returns a DocIdSet with documents that should be permitted in search
            /// results.
            /// </summary>
            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                SortedDocValues fcsi = FieldCache.DEFAULT.GetTermsIndex((context.AtomicReader), m_query.m_field);
                // Cannot use FixedBitSet because we require long index (ord):
                Int64BitSet termSet = new Int64BitSet(fcsi.ValueCount);
                TermsEnum termsEnum = m_query.GetTermsEnum(new TermsAnonymousClass(fcsi));

                if (Debugging.AssertsEnabled) Debugging.Assert(termsEnum != null);
                if (termsEnum.MoveNext())
                {
                    // fill into a bitset
                    do
                    {
                        long ord = termsEnum.Ord;
                        if (ord >= 0)
                        {
                            termSet.Set(ord);
                        }
                    } while (termsEnum.MoveNext());
                }
                else
                {
                    return null;
                }

                return new FieldCacheDocIdSet(context.Reader.MaxDoc, acceptDocs, (doc) =>
                {
                    int ord = fcsi.GetOrd(doc);
                    if (ord == -1)
                    {
                        return false;
                    }
                    return termSet.Get(ord);
                });
            }

            private sealed class TermsAnonymousClass : Terms
            {
                private readonly SortedDocValues fcsi;

                public TermsAnonymousClass(SortedDocValues fcsi)
                {
                    this.fcsi = fcsi;
                }

                public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

                public override TermsEnum GetEnumerator() => fcsi.GetTermsEnum();

                public override long SumTotalTermFreq => -1;

                public override long SumDocFreq => -1;

                public override int DocCount => -1;

                public override long Count => -1;

                public override bool HasFreqs => false;

                public override bool HasOffsets => false;

                public override bool HasPositions => false;

                public override bool HasPayloads => false;
            }
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj is null)
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