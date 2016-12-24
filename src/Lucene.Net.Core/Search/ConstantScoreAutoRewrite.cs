using System;
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

    // LUCENENET NOTE: made this class public, since a derived class with the same name is public
    public class ConstantScoreAutoRewrite : TermCollectingRewrite<BooleanQuery>
    {
        // LUCENENET specific - making constructor internal since the class was meant to be internal
        internal ConstantScoreAutoRewrite() { }

        // Defaults derived from rough tests with a 20.0 million
        // doc Wikipedia index.  With more than 350 terms in the
        // query, the filter method is fastest:
        public static int DEFAULT_TERM_COUNT_CUTOFF = 350;

        // If the query will hit more than 1 in 1000 of the docs
        // in the index (0.1%), the filter method is fastest:
        public static double DEFAULT_DOC_COUNT_PERCENT = 0.1;

        private int TermCountCutoff_Renamed = DEFAULT_TERM_COUNT_CUTOFF; // LUCENENET TODO: rename (private)
        private double DocCountPercent_Renamed = DEFAULT_DOC_COUNT_PERCENT; // LUCENENET TODO: rename (private)

        /// <summary>
        /// If the number of terms in this query is equal to or
        ///  larger than this setting then {@link
        ///  MultiTermQuery#CONSTANT_SCORE_FILTER_REWRITE} is used.
        /// </summary>
        public virtual int TermCountCutoff
        {
            set
            {
                TermCountCutoff_Renamed = value;
            }
            get
            {
                return TermCountCutoff_Renamed;
            }
        }

        /// <summary>
        /// If the number of documents to be visited in the
        ///  postings exceeds this specified percentage of the
        ///  maxDoc() for the index, then {@link
        ///  MultiTermQuery#CONSTANT_SCORE_FILTER_REWRITE} is used. </summary>
        ///  <param name="percent"> 0.0 to 100.0  </param>
        public virtual double DocCountPercent
        {
            set
            {
                DocCountPercent_Renamed = value;
            }
            get
            {
                return DocCountPercent_Renamed;
            }
        }

        protected override BooleanQuery TopLevelQuery
        {
            get
            {
                return new BooleanQuery(true);
            }
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
            int docCountCutoff = (int)((DocCountPercent_Renamed / 100.0) * reader.MaxDoc);
            int termCountLimit = Math.Min(BooleanQuery.MaxClauseCount, TermCountCutoff_Renamed);

            CutOffTermCollector col = new CutOffTermCollector(docCountCutoff, termCountLimit);
            CollectTerms(reader, query, col);
            int size = col.PendingTerms.Size();
            if (col.HasCutOff)
            {
                return MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE.Rewrite(reader, query);
            }
            else
            {
                BooleanQuery bq = TopLevelQuery;
                if (size > 0)
                {
                    BytesRefHash pendingTerms = col.PendingTerms;
                    int[] sort = pendingTerms.Sort(col.TermsEnum.Comparator);
                    for (int i = 0; i < size; i++)
                    {
                        int pos = sort[i];
                        // docFreq is not used for constant score here, we pass 1
                        // to explicitely set a fake value, so it's not calculated
                        AddClause(bq, new Term(query.field, pendingTerms.Get(pos, new BytesRef())), 1, 1.0f, col.Array.TermState[pos]);
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
            private void InitializeInstanceFields()
            {
                PendingTerms = new BytesRefHash(new ByteBlockPool(new ByteBlockPool.DirectAllocator()), 16, Array);
            }

            internal CutOffTermCollector(int docCountCutoff, int termCountLimit)
            {
                InitializeInstanceFields();
                this.DocCountCutoff = docCountCutoff;
                this.TermCountLimit = termCountLimit;
            }

            public override TermsEnum NextEnum
            {
                set
                {
                    this.TermsEnum = value;
                }
            }

            public override bool Collect(BytesRef bytes)
            {
                int pos = PendingTerms.Add(bytes);
                DocVisitCount += TermsEnum.DocFreq();
                if (PendingTerms.Size() >= TermCountLimit || DocVisitCount >= DocCountCutoff)
                {
                    HasCutOff = true;
                    return false;
                }

                TermState termState = TermsEnum.TermState();
                Debug.Assert(termState != null);
                if (pos < 0)
                {
                    pos = (-pos) - 1;
                    Array.TermState[pos].Register(termState, ReaderContext.Ord, TermsEnum.DocFreq(), TermsEnum.TotalTermFreq());
                }
                else
                {
                    Array.TermState[pos] = new TermContext(TopReaderContext, termState, ReaderContext.Ord, TermsEnum.DocFreq(), TermsEnum.TotalTermFreq());
                }
                return true;
            }

            internal int DocVisitCount = 0;
            internal bool HasCutOff = false;
            internal TermsEnum TermsEnum;

            internal readonly int DocCountCutoff, TermCountLimit;
            internal readonly TermStateByteStart Array = new TermStateByteStart(16);
            internal BytesRefHash PendingTerms;
        }

        public override int GetHashCode()
        {
            const int prime = 1279;
            return (int)(prime * TermCountCutoff_Renamed + BitConverter.DoubleToInt64Bits(DocCountPercent_Renamed));
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

            ConstantScoreAutoRewrite other = (ConstantScoreAutoRewrite)obj;
            if (other.TermCountCutoff_Renamed != TermCountCutoff_Renamed)
            {
                return false;
            }

            if (BitConverter.DoubleToInt64Bits(other.DocCountPercent_Renamed) != BitConverter.DoubleToInt64Bits(DocCountPercent_Renamed))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Special implementation of BytesStartArray that keeps parallel arrays for <seealso cref="TermContext"/> </summary>
        internal sealed class TermStateByteStart : BytesRefHash.DirectBytesStartArray
        {
            internal TermContext[] TermState; // LUCENENET TODO: rename (private)

            public TermStateByteStart(int initSize)
                : base(initSize)
            {
            }

            public override int[] Init()
            {
                int[] ord = base.Init();
                TermState = new TermContext[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                Debug.Assert(TermState.Length >= ord.Length);
                return ord;
            }

            public override int[] Grow()
            {
                int[] ord = base.Grow();
                if (TermState.Length < ord.Length)
                {
                    TermContext[] tmpTermState = new TermContext[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Array.Copy(TermState, 0, tmpTermState, 0, TermState.Length);
                    TermState = tmpTermState;
                }
                Debug.Assert(TermState.Length >= ord.Length);
                return ord;
            }

            public override int[] Clear()
            {
                TermState = null;
                return base.Clear();
            }
        }
    }
}