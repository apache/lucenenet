using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Sandbox.Queries
{
    public class FuzzyLikeThisQuery : Query
    {
        static TFIDFSimilarity sim = new DefaultSimilarity();
        Query rewrittenQuery = null;
        List<FieldVals> fieldVals = new List<FieldVals>();
        Analyzer analyzer;
        ScoreTermQueue q;
        int MAX_VARIANTS_PER_TERM = 50;
        bool ignoreTF = false;
        private int maxNumTerms;

        public override int GetHashCode()
        {
            int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((analyzer == null) ? 0 : analyzer.GetHashCode());
            result = prime * result + ((fieldVals == null) ? 0 : fieldVals.GetHashCode());
            result = prime * result + (ignoreTF ? 1231 : 1237);
            result = prime * result + maxNumTerms;
            return result;
        }

        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            if (!base.Equals(obj))
            {
                return false;
            }

            FuzzyLikeThisQuery other = (FuzzyLikeThisQuery) obj;
            if (analyzer == null)
            {
                if (other.analyzer != null)
                    return false;
            }
            else if (!analyzer.Equals(other.analyzer))
                return false;
            if (fieldVals == null)
            {
                if (other.fieldVals != null)
                    return false;
            }
            else if (!fieldVals.Equals(other.fieldVals))
                return false;
            if (ignoreTF != other.ignoreTF)
                return false;
            if (maxNumTerms != other.maxNumTerms)
                return false;
            return true;
        }

        public FuzzyLikeThisQuery(int maxNumTerms, Analyzer analyzer)
        {
            q = new ScoreTermQueue(maxNumTerms);
            this.analyzer = analyzer;
            this.maxNumTerms = maxNumTerms;
        }

        class FieldVals
        {
            internal string queryString;
            internal string fieldName;
            internal float minSimilarity;
            internal int prefixLength;

            public FieldVals(string name, float similarity, int length, string queryString)
            {
                fieldName = name;
                minSimilarity = similarity;
                prefixLength = length;
                this.queryString = queryString;
            }

            public override int GetHashCode()
            {
                int prime = 31;
                int result = 1;
                result = prime * result + ((fieldName == null) ? 0 : fieldName.GetHashCode());
                result = prime * result + Number.FloatToIntBits(minSimilarity);
                result = prime * result + prefixLength;
                result = prime * result + ((queryString == null) ? 0 : queryString.GetHashCode());
                return result;
            }

            public override bool Equals(Object obj)
            {
                if (this == obj)
                    return true;
                if (obj == null)
                    return false;
                if (GetType() != obj.GetType())
                    return false;
                FieldVals other = (FieldVals) obj;
                if (fieldName == null)
                {
                    if (other.fieldName != null)
                        return false;
                }
                else if (!fieldName.Equals(other.fieldName))
                    return false;
                if (Number.FloatToIntBits(minSimilarity) != Number.FloatToIntBits(other.minSimilarity))
                    return false;
                if (prefixLength != other.prefixLength)
                    return false;
                if (queryString == null)
                {
                    if (other.queryString != null)
                        return false;
                }
                else if (!queryString.Equals(other.queryString))
                    return false;
                return true;
            }
        }

        public virtual void AddTerms(string queryString, string fieldName, float minSimilarity, int prefixLength)
        {
            fieldVals.Add(new FieldVals(fieldName, minSimilarity, prefixLength, queryString));
        }

        private void AddTerms(IndexReader reader, FieldVals f)
        {
            if (f.queryString == null)
                return;
            TokenStream ts = analyzer.TokenStream(f.fieldName, new StringReader(f.queryString));
            ICharTermAttribute termAtt = ts.AddAttribute<ICharTermAttribute>();
            int corpusNumDocs = reader.NumDocs;
            HashSet<String> processedTerms = new HashSet<String>();
            ts.Reset();
            Terms terms = MultiFields.GetTerms(reader, f.fieldName);
            if (terms == null)
            {
                return;
            }

            while (ts.IncrementToken())
            {
                string term = termAtt.ToString();
                if (!processedTerms.Contains(term))
                {
                    processedTerms.Add(term);
                    ScoreTermQueue variantsQ = new ScoreTermQueue(MAX_VARIANTS_PER_TERM);
                    float minScore = 0;
                    Term startTerm = new Term(f.fieldName, term);
                    AttributeSource atts = new AttributeSource();
                    IMaxNonCompetitiveBoostAttribute maxBoostAtt = atts.AddAttribute<IMaxNonCompetitiveBoostAttribute>();
                    SlowFuzzyTermsEnum fe = new SlowFuzzyTermsEnum(terms, atts, startTerm, f.minSimilarity, f.prefixLength);
                    int df = reader.DocFreq(startTerm);
                    int numVariants = 0;
                    int totalVariantDocFreqs = 0;
                    BytesRef possibleMatch;
                    IBoostAttribute boostAtt = fe.Attributes.AddAttribute<IBoostAttribute>();
                    while ((possibleMatch = fe.Next()) != null)
                    {
                        numVariants++;
                        totalVariantDocFreqs = fe.DocFreq;
                        float score = boostAtt.Boost;
                        if (variantsQ.Size < MAX_VARIANTS_PER_TERM || score > minScore)
                        {
                            ScoreTerm st = new ScoreTerm(new Term(startTerm.Field, BytesRef.DeepCopyOf(possibleMatch)), score, startTerm);
                            variantsQ.InsertWithOverflow(st);
                            minScore = variantsQ.Top().score;
                        }

                        maxBoostAtt.MaxNonCompetitiveBoost = variantsQ.Size >= MAX_VARIANTS_PER_TERM ? minScore : float.NegativeInfinity;
                    }

                    if (numVariants > 0)
                    {
                        int avgDf = totalVariantDocFreqs / numVariants;
                        if (df == 0)
                        {
                            df = avgDf;
                        }

                        int size = variantsQ.Size;
                        for (int i = 0; i < size; i++)
                        {
                            ScoreTerm st = variantsQ.Pop();
                            st.score = (st.score * st.score) * sim.Idf(df, corpusNumDocs);
                            q.InsertWithOverflow(st);
                        }
                    }
                }
            }

            ts.End();
            ts.Dispose();
        }

        public override Query Rewrite(IndexReader reader)
        {
            if (rewrittenQuery != null)
            {
                return rewrittenQuery;
            }

            for (IEnumerator<FieldVals> iter = fieldVals.GetEnumerator(); iter.MoveNext();)
            {
                FieldVals f = iter.Current;
                AddTerms(reader, f);
            }

            fieldVals.Clear();
            BooleanQuery bq = new BooleanQuery();
            HashMap<Term, List<ScoreTerm>> variantQueries = new HashMap<Term, List<ScoreTerm>>();
            int size = q.Size;
            for (int i = 0; i < size; i++)
            {
                ScoreTerm st = q.Pop();
                List<ScoreTerm> l = variantQueries[st.fuzziedSourceTerm];
                if (l == null)
                {
                    l = new List<ScoreTerm>();
                    variantQueries[st.fuzziedSourceTerm] = l;
                }

                l.Add(st);
            }

            for (IEnumerator<List<ScoreTerm>> iter = variantQueries.Values.GetEnumerator(); iter.MoveNext();)
            {
                List<ScoreTerm> variants = iter.Current;
                if (variants.Count == 1)
                {
                    ScoreTerm st = variants[0];
                    Query tq = ignoreTF ? (Query)new ConstantScoreQuery(new TermQuery(st.term)) : new TermQuery(st.term, 1);
                    tq.Boost = st.score;
                    bq.Add(tq, Occur.SHOULD);
                }
                else
                {
                    BooleanQuery termVariants = new BooleanQuery(true);
                    for (IEnumerator<ScoreTerm> iterator2 = variants.GetEnumerator(); iterator2.MoveNext();)
                    {
                        ScoreTerm st = iterator2.Current;
                        Query tq = ignoreTF ? (Query)new ConstantScoreQuery(new TermQuery(st.term)) : new TermQuery(st.term, 1);
                        tq.Boost = st.score;
                        termVariants.Add(tq, Occur.SHOULD);
                    }

                    bq.Add(termVariants, Occur.SHOULD);
                }
            }

            bq.Boost = Boost;
            this.rewrittenQuery = bq;
            return bq;
        }

        private class ScoreTerm
        {
            public Term term;
            public float score;
            internal Term fuzziedSourceTerm;
            public ScoreTerm(Term term, float score, Term fuzziedSourceTerm)
            {
                this.term = term;
                this.score = score;
                this.fuzziedSourceTerm = fuzziedSourceTerm;
            }
        }

        private class ScoreTermQueue : Lucene.Net.Util.PriorityQueue<ScoreTerm>
        {
            public ScoreTermQueue(int size): base (size)
            {
            }

            public override bool LessThan(ScoreTerm termA, ScoreTerm termB)
            {
                if (termA.score == termB.score)
                    return termA.term.CompareTo(termB.term) > 0;
                else
                    return termA.score < termB.score;
            }
        }

        public override string ToString(string field)
        {
            return null;
        }

        public virtual bool IsIgnoreTF()
        {
            return ignoreTF;
        }

        public virtual void SetIgnoreTF(bool ignoreTF)
        {
            this.ignoreTF = ignoreTF;
        }
    }
}
