using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    public class ConstantScoreAutoRewrite : TermCollectingRewrite<BooleanQuery>
    {
        public static int DEFAULT_TERM_COUNT_CUTOFF = 350;

        public static double DEFAULT_DOC_COUNT_PERCENT = 0.1;

        private int termCountCutoff = DEFAULT_TERM_COUNT_CUTOFF;
        private double docCountPercent = DEFAULT_DOC_COUNT_PERCENT;

        public virtual int TermCountCutoff
        {
            get { return termCountCutoff; }
            set { termCountCutoff = value; }
        }

        public virtual double DocCountPercent
        {
            get { return docCountPercent; }
            set { docCountPercent = value; }
        }

        protected override BooleanQuery GetTopLevelQuery()
        {
            return new BooleanQuery(true);
        }

        protected override void AddClause(BooleanQuery topLevel, Index.Term term, int docCount, float boost, TermContext states)
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
            int size = col.pendingTerms.Size;
            if (col.hasCutOff)
            {
                return MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE.Rewrite(reader, query);
            }
            else if (size == 0)
            {
                return GetTopLevelQuery();
            }
            else
            {
                BooleanQuery bq = GetTopLevelQuery();
                BytesRefHash pendingTerms = col.pendingTerms;
                int[] sort = pendingTerms.Sort(col.termsEnum.Comparator);
                for (int i = 0; i < size; i++)
                {
                    int pos = sort[i];
                    // docFreq is not used for constant score here, we pass 1
                    // to explicitely set a fake value, so it's not calculated
                    AddClause(bq, new Term(query.Field, pendingTerms.Get(pos, new BytesRef())), 1, 1.0f, col.array.termState[pos]);
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
                if (pendingTerms.Size >= termCountLimit || docVisitCount >= docCountCutoff)
                {
                    hasCutOff = true;
                    return false;
                }

                TermState termState = termsEnum.TermState;
                //assert termState != null;
                if (pos < 0)
                {
                    pos = (-pos) - 1;
                    array.termState[pos].register(termState, readerContext.ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                }
                else
                {
                    array.termState[pos] = new TermContext(topReaderContext, termState, readerContext.ord, termsEnum.DocFreq, termsEnum.TotalTermFreq);
                }
                return true;
            }

            internal int docVisitCount = 0;
            internal bool hasCutOff = false;
            internal TermsEnum termsEnum;

            internal int docCountCutoff, termCountLimit;
            internal TermStateByteStart array = new TermStateByteStart(16);
            internal BytesRefHash pendingTerms = new BytesRefHash(new ByteBlockPool(new ByteBlockPool.DirectAllocator()), 16, array);
        }

        public override int GetHashCode()
        {
            int prime = 1279;
            return (int)(prime * termCountCutoff + BitConverter.DoubleToInt64Bits(docCountPercent));
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;

            ConstantScoreAutoRewrite other = (ConstantScoreAutoRewrite)obj;
            if (other.termCountCutoff != termCountCutoff)
            {
                return false;
            }

            if (BitConverter.DoubleToInt64Bits(other.docCountPercent) != BitConverter.DoubleToInt64Bits(docCountPercent))
            {
                return false;
            }

            return true;
        }

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
                //assert termState.length >= ord.length;
                return ord;
            }

            public override int[] Grow()
            {
                int[] ord = base.Grow();
                if (termState.Length < ord.Length)
                {
                    TermContext[] tmpTermState = new TermContext[ArrayUtil.Oversize(ord.Length, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Array.Copy(termState, 0, tmpTermState, 0, termState.Length);
                    termState = tmpTermState;
                }
                //assert termState.length >= ord.length;
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
