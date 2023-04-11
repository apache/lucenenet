using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Sandbox.Queries
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

    /// <summary>
    /// Fuzzifies ALL terms provided as strings and then picks the best n differentiating terms.
    /// In effect this mixes the behaviour of <see cref="FuzzyQuery"/> and MoreLikeThis but with special consideration
    /// of fuzzy scoring factors.
    /// This generally produces good results for queries where users may provide details in a number of 
    /// fields and have no knowledge of boolean query syntax and also want a degree of fuzzy matching and
    /// a fast query.
    /// <para/>
    /// For each source term the fuzzy variants are held in a <see cref="BooleanQuery"/> with no coord factor (because
    /// we are not looking for matches on multiple variants in any one doc). Additionally, a specialized
    /// <see cref="TermQuery"/> is used for variants and does not use that variant term's IDF because this would favour rarer 
    /// terms eg misspellings. Instead, all variants use the same IDF ranking (the one for the source query 
    /// term) and this is factored into the variant's boost. If the source query term does not exist in the
    /// index the average IDF of the variants is used.
    /// </summary>
    public class FuzzyLikeThisQuery : Query
    {
        // TODO: generalize this query (at least it should not reuse this static sim!
        // a better way might be to convert this into multitermquery rewrite methods.
        // the rewrite method can 'average' the TermContext's term statistics (docfreq,totalTermFreq) 
        // provided to TermQuery, so that the general idea is agnostic to any scoring system...
        internal static TFIDFSimilarity sim = new DefaultSimilarity();
        private Query rewrittenQuery = null;
        private readonly IList<FieldVals> fieldVals = new JCG.List<FieldVals>();
        private readonly Analyzer analyzer;

        private readonly ScoreTermQueue q;
        private const int MAX_VARIANTS_PER_TERM = 50;
        private bool ignoreTF = false;
        private readonly int maxNumTerms;

        public override int GetHashCode()
        {
            int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((analyzer is null) ? 0 : analyzer.GetHashCode());
            result = prime * result
                + ((fieldVals is null) ? 0 : fieldVals.GetHashCode());
            result = prime * result + (ignoreTF ? 1231 : 1237);
            result = prime * result + maxNumTerms;
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj is null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            if (!base.Equals(obj))
            {
                return false;
            }
            FuzzyLikeThisQuery other = (FuzzyLikeThisQuery)obj;
            if (analyzer is null)
            {
                if (other.analyzer != null)
                    return false;
            }
            else if (!analyzer.Equals(other.analyzer))
                return false;
            if (fieldVals is null)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxNumTerms">The total number of terms clauses that will appear once rewritten as a <see cref="BooleanQuery"/></param>
        /// <param name="analyzer"></param>
        public FuzzyLikeThisQuery(int maxNumTerms, Analyzer analyzer)
        {
            q = new ScoreTermQueue(maxNumTerms);
            this.analyzer = analyzer;
            this.maxNumTerms = maxNumTerms;
        }

        internal class FieldVals
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
                result = prime * result
                    + ((fieldName is null) ? 0 : fieldName.GetHashCode());
                result = prime * result + J2N.BitConversion.SingleToInt32Bits(minSimilarity);
                result = prime * result + prefixLength;
                result = prime * result
                    + ((queryString is null) ? 0 : queryString.GetHashCode());
                return result;
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                    return true;
                if (obj is null)
                    return false;
                if (GetType() != obj.GetType())
                    return false;
                FieldVals other = (FieldVals)obj;
                if (fieldName is null)
                {
                    if (other.fieldName != null)
                        return false;
                }
                else if (!fieldName.Equals(other.fieldName, StringComparison.Ordinal))
                    return false;
                if (J2N.BitConversion.SingleToInt32Bits(minSimilarity) != J2N.BitConversion
                    .SingleToInt32Bits(other.minSimilarity))
                    return false;
                if (prefixLength != other.prefixLength)
                    return false;
                if (queryString is null)
                {
                    if (other.queryString != null)
                        return false;
                }
                else if (!queryString.Equals(other.queryString, StringComparison.Ordinal))
                    return false;
                return true;
            }
        }

        /// <summary>
        /// Adds user input for "fuzzification" 
        /// </summary>
        /// <param name="queryString">The string which will be parsed by the analyzer and for which fuzzy variants will be parsed</param>
        /// <param name="fieldName">The minimum similarity of the term variants (see <see cref="FuzzyTermsEnum"/>)</param>
        /// <param name="minSimilarity">Length of required common prefix on variant terms (see <see cref="FuzzyTermsEnum"/>)</param>
        /// <param name="prefixLength"></param>
        public virtual void AddTerms(string queryString, string fieldName, float minSimilarity, int prefixLength)
        {
            fieldVals.Add(new FieldVals(fieldName, minSimilarity, prefixLength, queryString));
        }


        private void AddTerms(IndexReader reader, FieldVals f)
        {
            if (f.queryString is null) return;
            Terms terms = MultiFields.GetTerms(reader, f.fieldName);
            if (terms is null)
            {
                return;
            }
            TokenStream ts = analyzer.GetTokenStream(f.fieldName, f.queryString);
            try
            {
                ICharTermAttribute termAtt = ts.AddAttribute<ICharTermAttribute>();

                int corpusNumDocs = reader.NumDocs;
                ISet<string> processedTerms = new JCG.HashSet<string>();
                ts.Reset();
                while (ts.IncrementToken())
                {
                    string term = termAtt.ToString();
                    if (!processedTerms.Contains(term))
                    {
                        processedTerms.Add(term);
                        ScoreTermQueue variantsQ = new ScoreTermQueue(MAX_VARIANTS_PER_TERM); //maxNum variants considered for any one term
                        float minScore = 0;
                        Term startTerm = new Term(f.fieldName, term);
                        AttributeSource atts = new AttributeSource();
                        IMaxNonCompetitiveBoostAttribute maxBoostAtt =
                            atts.AddAttribute<IMaxNonCompetitiveBoostAttribute>();
#pragma warning disable 612, 618
                        SlowFuzzyTermsEnum fe = new SlowFuzzyTermsEnum(terms, atts, startTerm, f.minSimilarity, f.prefixLength);
#pragma warning restore 612, 618
                        //store the df so all variants use same idf
                        int df = reader.DocFreq(startTerm);
                        int numVariants = 0;
                        int totalVariantDocFreqs = 0;
                        BytesRef possibleMatch;
                        IBoostAttribute boostAtt =
                          fe.Attributes.AddAttribute<IBoostAttribute>();
                        while (fe.MoveNext())
                        {
                            possibleMatch = fe.Term;
                            numVariants++;
                            totalVariantDocFreqs += fe.DocFreq;
                            float score = boostAtt.Boost;
                            if (variantsQ.Count < MAX_VARIANTS_PER_TERM || score > minScore)
                            {
                                ScoreTerm st = new ScoreTerm(new Term(startTerm.Field, BytesRef.DeepCopyOf(possibleMatch)), score, startTerm);
                                variantsQ.InsertWithOverflow(st);
                                minScore = variantsQ.Top.Score; // maintain minScore
                            }
                            maxBoostAtt.MaxNonCompetitiveBoost = variantsQ.Count >= MAX_VARIANTS_PER_TERM ? minScore : float.NegativeInfinity;
                        }

                        if (numVariants > 0)
                        {
                            int avgDf = totalVariantDocFreqs / numVariants;
                            if (df == 0)//no direct match we can use as df for all variants
                            {
                                df = avgDf; //use avg df of all variants
                            }

                            // take the top variants (scored by edit distance) and reset the score
                            // to include an IDF factor then add to the global queue for ranking
                            // overall top query terms
                            int size = variantsQ.Count;
                            for (int i = 0; i < size; i++)
                            {
                                ScoreTerm st = variantsQ.Pop();
                                st.Score = (st.Score * st.Score) * sim.Idf(df, corpusNumDocs);
                                q.InsertWithOverflow(st);
                            }
                        }
                    }
                }
                ts.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }
        }

        public override Query Rewrite(IndexReader reader)
        {
            if (rewrittenQuery != null)
            {
                return rewrittenQuery;
            }
            //load up the list of possible terms
            foreach (var f in fieldVals)
            {
                AddTerms(reader, f);
            }
            //clear the list of fields
            fieldVals.Clear();

            BooleanQuery bq = new BooleanQuery();


            //create BooleanQueries to hold the variants for each token/field pair and ensure it
            // has no coord factor
            //Step 1: sort the termqueries by term/field
            IDictionary<Term, IList<ScoreTerm>> variantQueries = new Dictionary<Term, IList<ScoreTerm>>();
            int size = q.Count;
            for (int i = 0; i < size; i++)
            {
                ScoreTerm st = q.Pop();
                if (!variantQueries.TryGetValue(st.FuzziedSourceTerm, out IList<ScoreTerm> l) || l is null)
                {
                    l = new JCG.List<ScoreTerm>();
                    variantQueries[st.FuzziedSourceTerm] = l;
                }
                l.Add(st);
            }
            //Step 2: Organize the sorted termqueries into zero-coord scoring boolean queries
            foreach (IList<ScoreTerm> variants in variantQueries.Values)
            {
                if (variants.Count == 1)
                {
                    //optimize where only one selected variant
                    ScoreTerm st = variants[0];
                    Query tq = ignoreTF ? (Query)new ConstantScoreQuery(new TermQuery(st.Term)) : new TermQuery(st.Term, 1);
                    tq.Boost = st.Score; // set the boost to a mix of IDF and score
                    bq.Add(tq, Occur.SHOULD);
                }
                else
                {
                    BooleanQuery termVariants = new BooleanQuery(true); //disable coord and IDF for these term variants
                    foreach (ScoreTerm st in variants)
                    {
                        // found a match
                        Query tq = ignoreTF ? (Query)new ConstantScoreQuery(new TermQuery(st.Term)) : new TermQuery(st.Term, 1);
                        tq.Boost = st.Score; // set the boost using the ScoreTerm's score
                        termVariants.Add(tq, Occur.SHOULD);          // add to query                    
                    }
                    bq.Add(termVariants, Occur.SHOULD);          // add to query
                }
            }
            //TODO possible alternative step 3 - organize above booleans into a new layer of field-based
            // booleans with a minimum-should-match of NumFields-1?
            bq.Boost = Boost;
            this.rewrittenQuery = bq;
            return bq;
        }

        //Holds info for a fuzzy term variant - initially score is set to edit distance (for ranking best
        // term variants) then is reset with IDF for use in ranking against all other
        // terms/fields
        internal class ScoreTerm
        {
            public Term Term { get; set; }
            public float Score { get; set; }
            internal Term FuzziedSourceTerm { get; set; }

            public ScoreTerm(Term term, float score, Term fuzziedSourceTerm)
            {
                this.Term = term;
                this.Score = score;
                this.FuzziedSourceTerm = fuzziedSourceTerm;
            }
        }

        internal class ScoreTermQueue : PriorityQueue<ScoreTerm>
        {
            public ScoreTermQueue(int size)
                : base(size)
            {
            }

            /// <summary>
            /// (non-Javadoc)
            /// <see cref="PriorityQueue{T}.LessThan(T, T)"/>
            /// </summary>
            protected internal override bool LessThan(ScoreTerm termA, ScoreTerm termB)
            {
                // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
                if (NumericUtils.SingleToSortableInt32(termA.Score) == NumericUtils.SingleToSortableInt32(termB.Score))
                    return termA.Term.CompareTo(termB.Term) > 0;
                else
                    return NumericUtils.SingleToSortableInt32(termA.Score) < NumericUtils.SingleToSortableInt32(termB.Score);
            }

        }

        /// <summary>
        /// (non-Javadoc)
        /// <see cref="Query.ToString(string)"/>
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public override string ToString(string field)
        {
            return null;
        }

        public virtual bool IgnoreTF
        {
            get => ignoreTF;
            set => ignoreTF = value;
        }
    }
}
