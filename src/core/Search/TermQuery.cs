/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using IndexReader = Lucene.Net.Index.IndexReader;
using Term = Lucene.Net.Index.Term;
using TermDocs = Lucene.Net.Index.TermDocs;
using ToStringUtils = Lucene.Net.Util.ToStringUtils;
using IDFExplanation = Lucene.Net.Search.Explanation.IDFExplanation;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Search
{

    /// <summary>A Query that matches documents containing a term.
    /// This may be combined with other terms with a <see cref="BooleanQuery" />.
    /// </summary>
    [Serializable]
    public class TermQuery : Query
    {
        private readonly Term term;
        private readonly int docFreq;
        private readonly TermContext perReaderTermState;

        [Serializable]
        private class TermWeight : Weight
        {
            private TermQuery parent;

            private readonly Similarity similarity;
            private readonly Similarity.SimWeight stats;
            private readonly TermContext termStates;

            public TermWeight(TermQuery parent, IndexSearcher searcher, TermContext termStates)
            {
                this.parent = parent;

                this.termStates = termStates;
                this.similarity = searcher.Similarity;
                this.stats = similarity.ComputeWeight(
                    Boost,
                    searcher.CollectionStatistics(term.Field),
                    searcher.TermStatistics(term, termStates));
            }

            public override String ToString()
            {
                return "weight(" + parent + ")";
            }

            public override Query Query
            {
                get { return parent; }
            }

            public override float ValueForNormalization
            {
                get { return stats.ValueForNormalization; }
            }

            public override void Normalize(float queryNorm)
            {
                stats.Normalize(queryNorm, topLevelBoost);
            }

            public override Scorer Scorer(AtomicReaderContext context, bool scoreDocsInOrder, bool topScorer, Bits acceptDocs)
            {
                // assert termStates.topReaderContext == ReaderUtil.getTopLevelContext(context) : "The top-reader used to create Weight (" + termStates.topReaderContext + ") is not the same as the current reader's top-reader (" + ReaderUtil.getTopLevelContext(context);
                var termsEnum = GetTermsEnum(context);
                if (termsEnum == null)
                {
                    return null;
                }
                var docs = termsEnum.Docs(acceptDocs, null);
                // assert docs != null
                return new TermScorer(this, docs, similarity.GetExactSimScorer(stats, context));
            }

            private TermsEnum GetTermsEnum(AtomicReaderContext context)
            {
                var state = termStates.Get(context.ord);
                if (state == null)
                {
                    // assert termNotInReader(context.reader(), term) : "no termstate found but term exists in reader term=" + term;
                    return null;
                }
                var termsEnum = context.Reader.Terms(term.Field).Iterator(null);
                termsEnum.SeekExact(term.Bytes, state);
                return termsEnum;
            }

            private bool TermNotInReader(AtomicReader reader, Term term)
            {
                // only called from assert
                return reader.docFreq(term) == 0;
            }

            public override Explanation Explain(IndexReader reader, int doc)
            {
                var scorer = scorer(context, true, false, context.reader().getLiveDocs());
                if (scorer != null)
                {
                    int newDoc = scorer.advance(doc);
                    if (newDoc == doc)
                    {
                        float freq = scorer.freq();
                        var docScorer = similarity.GetExactSimScorer(stats, context);
                        var result = new ComplexExplanation();
                        result.Description = "weight(" + Query + " in " + doc + ") [" + similarity.GetType().Name + "], result of:";
                        var scoreExplanation = docScorer.Explain(doc, new Explanation(freq, "termFreq=" + freq));
                        result.AddDetail(scoreExplanation);
                        result.Value = scoreExplanation.Value;
                        result.Match = true;
                        return result;
                    }
                }
                return new ComplexExplanation(false, 0.0f, "no matching term");
            }
        }

        /// <summary>Constructs a query for the term <c>t</c>. </summary>
        public TermQuery(Term t) : this(t, -1) { }

        public TermQuery(Term t, int docFreq)
        {
            term = t;
            this.docFreq = docFreq;
            perReaderTermState = null;
        }

        public TermQuery(Term t, TermContext states)
        {
            if (states == null) throw new ArgumentNullException("states");
            term = t;
            docFreq = states.DocFreq;
            perReaderTermState = states;
        }

        /// <summary>Returns the term of this query. </summary>
        public virtual Term Term
        {
            get { return term; }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            var context = searcher.TopReaderContext;
            TermContext termState;
            if (perReaderTermState == null || perReaderTermState.TopReaderContext != context)
            {
                // make TermQuery single-pass if we don't have a PRTS or if the context differs!
                termState = TermContext.Build(context, term, true); // cache term lookups!
            }
            else
            {
                // PRTS was pre-build for this IS
                termState = this.perReaderTermState;
            }

            // we must not ignore the given docFreq - if set use the given value (lie)
            if (docFreq != -1)
                termState.DocFreq = docFreq;

            return new TermWeight(searcher, termState);
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            terms.Add(Term);
        }

        /// <summary>Prints a user-readable version of this query. </summary>
        public override String ToString(String field)
        {
            StringBuilder buffer = new StringBuilder();
            if (!term.Field.Equals(field))
            {
                buffer.Append(term.Field);
                buffer.Append(":");
            }
            buffer.Append(term.Text);
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        /// <summary>Returns true iff <c>o</c> is equal to this. </summary>
        public override bool Equals(System.Object o)
        {
            if (!(o is TermQuery))
                return false;
            TermQuery other = (TermQuery)o;
            return (this.Boost == other.Boost) && this.term.Equals(other.term);
        }

        /// <summary>Returns a hash code value for this object.</summary>
        public override int GetHashCode()
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(Boost), 0) ^ term.GetHashCode();
        }
    }
}