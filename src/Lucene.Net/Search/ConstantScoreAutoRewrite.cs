using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;

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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using ByteBlockPool = Lucene.Net.Util.ByteBlockPool;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using BytesRefHash = Lucene.Net.Util.BytesRefHash;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TermState = Lucene.Net.Index.TermState;

    /// <summary>
    /// A rewrite method that tries to pick the best
    /// constant-score rewrite method based on term and
    /// document counts from the query.  If both the number of
    /// terms and documents is small enough, then 
    /// <see cref="MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE"/> is used.
    /// Otherwise, <see cref="MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE"/> is
    /// used.
    /// </summary>
    // LUCENENET specific: made this class public. In Lucene there was a derived class 
    // with the same name that was nested within MultiTermQuery, but in .NET it is 
    // more intuitive if our classes are not nested.
    public class ConstantScoreAutoRewrite : TermCollectingRewrite<BooleanQuery>
    {
        /// <summary>
        /// Defaults derived from rough tests with a 20.0 million
        /// doc Wikipedia index.  With more than 350 terms in the
        /// query, the filter method is fastest:
        /// </summary>
        public static int DEFAULT_TERM_COUNT_CUTOFF = 350;

        /// <summary>
        /// If the query will hit more than 1 in 1000 of the docs
        /// in the index (0.1%), the filter method is fastest:
        /// </summary>
        public static double DEFAULT_DOC_COUNT_PERCENT = 0.1;

        private int termCountCutoff = DEFAULT_TERM_COUNT_CUTOFF;
        private double docCountPercent = DEFAULT_DOC_COUNT_PERCENT;

        /// <summary>
        /// If the number of terms in this query is equal to or
        /// larger than this setting then 
        /// <see cref="MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE"/> is used.
        /// </summary>
        public virtual int TermCountCutoff
        {
            get => termCountCutoff;
            set => termCountCutoff = value;
        }

        /// <summary>
        /// If the number of documents to be visited in the
        /// postings exceeds this specified percentage of the
        /// <see cref="Index.IndexReader.MaxDoc"/> for the index, then
        /// <see cref="MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE"/> is used. 
        /// Value may be 0.0 to 100.0.
        /// </summary>
        public virtual double DocCountPercent
        {
            get => docCountPercent;
            set => docCountPercent = value;
        }

        protected override BooleanQuery GetTopLevelQuery()
        {
            return new BooleanQuery(true);
        }

        protected override void AddClause(BooleanQuery topLevel, Term term, int docFreq, float boost, TermContext states) //ignored
        {
            topLevel.Add(new TermQuery(term, states), Occur.SHOULD);
        }

        public override Query Rewrite(IndexReader reader, MultiTermQuery query)
        {
            // Get the enum and start visiting terms.  If we
            // exhaust the enum before hitting either of the
            // cutoffs, we use ConstantBooleanQueryRewrite; else,
            // ConstantFilterRewrite:
            int docCountCutoff = (int)((docCountPercent / 100.0) * reader.MaxDoc);
            int termCountLimit = Math.Min(BooleanQuery.MaxClauseCount, termCountCutoff);

            CutOffTermCollector col = new CutOffTermCollector(docCountCutoff, termCountLimit);
            CollectTerms(reader, query, col);
            int size = col.pendingTerms.Count;
            if (col.hasCutOff)
            {
                return MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE.Rewrite(reader, query);
            }
            else
            {
                BooleanQuery bq = GetTopLevelQuery();
                if (size > 0)
                {
                    BytesRefHash pendingTerms = col.pendingTerms;
                    int[] sort = pendingTerms.Sort(col.termsEnum.Comparer);
                    for (int i = 0; i < size; i++)
                    {
                        int pos = sort[i];
                        // docFreq is not used for constant score here, we pass 1
                        // to explicitely set a fake value, so it's not calculated
                        AddClause(bq, new Term(query.m_field, pendingTerms.Get(pos, new BytesRef())), 1, 1.0f, col.array.termState[pos]);
                    }
                }
                // Strip scores
                Query result = new ConstantScoreQuery(bq);
                result.Boost = query.Boost;
                return result;
            }
        }

        internal sealed class CutOffTermCollector : TermCollector
        {
            internal CutOffTermCollector(int docCountCutoff, int termCountLimit)
            {
                pendingTerms = new BytesRefHash(new ByteBlockPool(new ByteBlockPool.DirectAllocator()), 16, array);
                this.docCountCutoff = docCountCutoff;
                this.termCountLimit = termCountLimit;
            }

            public override void SetNextEnum(TermsEnum termsEnum)
            {
                this.termsEnum = termsEnum;
            }

            public override bool Collect(BytesRef bytes)
            {
                int pos = pendingTerms.Add(bytes);
                docVisitCount += termsEnum.DocFreq;
                if (pendingTerms.Count >= termCountLimit || docVisitCount >= docCountCutoff)
                {
                    hasCutOff = true;
                    return false;
                }

                TermState termState = termsEnum.GetTermState();
                if (Debugging.AssertsEnabled) Debugging.Assert(termState != null);
                if (pos < 0)
                {
                    pos = (-pos) - 1;
                    array.termState[pos].Register(termState, m_readerContext.Ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                }
                else
                {
                    array.termState[pos] = new TermContext(m_topReaderContext, termState, m_readerContext.Ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                }
                return true;
            }

            internal int docVisitCount = 0;
            internal bool hasCutOff = false;
            internal TermsEnum termsEnum;

            internal readonly int docCountCutoff, termCountLimit;
            internal readonly TermStateByteStart array = new TermStateByteStart(16);
            internal BytesRefHash pendingTerms;
        }

        public override int GetHashCode()
        {
            const int prime = 1279;
            return (int)(prime * termCountCutoff + J2N.BitConversion.DoubleToInt64Bits(docCountPercent));
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

            ConstantScoreAutoRewrite other = (ConstantScoreAutoRewrite)obj;
            if (other.termCountCutoff != termCountCutoff)
            {
                return false;
            }

            if (J2N.BitConversion.DoubleToInt64Bits(other.docCountPercent) != J2N.BitConversion.DoubleToInt64Bits(docCountPercent))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Special implementation of <see cref="BytesRefHash.BytesStartArray"/> that keeps parallel arrays for <see cref="TermContext"/> </summary>
        internal sealed class TermStateByteStart : BytesRefHash.DirectBytesStartArray
        {
            internal TermContext[] termState;

            public TermStateByteStart(int initSize)
                : base(initSize)
            {
            }

            public override int[] Init()
            {
                int[] ord = base.Init();
                termState = new TermContext[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                if (Debugging.AssertsEnabled) Debugging.Assert(termState.Length >= ord.Length);
                return ord;
            }

            public override int[] Grow()
            {
                int[] ord = base.Grow();
                if (termState.Length < ord.Length)
                {
                    TermContext[] tmpTermState = new TermContext[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Arrays.Copy(termState, 0, tmpTermState, 0, termState.Length);
                    termState = tmpTermState;
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(termState.Length >= ord.Length);
                return ord;
            }

            public override int[] Clear()
            {
                termState = null;
                return base.Clear();
            }
        }
    }
}