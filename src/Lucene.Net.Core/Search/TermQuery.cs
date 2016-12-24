using Lucene.Net.Support;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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

    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Bits = Lucene.Net.Util.Bits;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
    using ReaderUtil = Lucene.Net.Index.ReaderUtil;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using SimScorer = Lucene.Net.Search.Similarities.Similarity.SimScorer;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TermState = Lucene.Net.Index.TermState;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// A Query that matches documents containing a term.
    ///  this may be combined with other terms with a <seealso cref="BooleanQuery"/>.
    /// </summary>
    public class TermQuery : Query
    {
        private readonly Term _term;
        private readonly int DocFreq; // LUCENENET TODO: Rename (private)
        private readonly TermContext PerReaderTermState; // LUCENENET TODO: Rename (private)

        internal sealed class TermWeight : Weight
        {
            private readonly TermQuery OuterInstance; // LUCENENET TODO: Rename (private)

            internal readonly Similarity Similarity; // LUCENENET TODO: Rename (private)
            internal readonly Similarity.SimWeight Stats; // LUCENENET TODO: Rename (private)
            internal readonly TermContext TermStates; // LUCENENET TODO: Rename (private)

            public TermWeight(TermQuery outerInstance, IndexSearcher searcher, TermContext termStates)
            {
                this.OuterInstance = outerInstance;
                Debug.Assert(termStates != null, "TermContext must not be null");
                this.TermStates = termStates;
                this.Similarity = searcher.Similarity;
                this.Stats = Similarity.ComputeWeight(outerInstance.Boost, searcher.CollectionStatistics(outerInstance._term.Field), searcher.TermStatistics(outerInstance._term, termStates));
            }

            public override string ToString()
            {
                return "weight(" + OuterInstance + ")";
            }

            public override Query Query
            {
                get
                {
                    return OuterInstance;
                }
            }

            public override float ValueForNormalization
            {
                get
                {
                    return Stats.ValueForNormalization;
                }
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                Stats.Normalize(queryNorm, topLevelBoost);
            }

            public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
            {
                Debug.Assert(TermStates.TopReaderContext == ReaderUtil.GetTopLevelContext(context), "The top-reader used to create Weight (" + TermStates.TopReaderContext + ") is not the same as the current reader's top-reader (" + ReaderUtil.GetTopLevelContext(context));
                TermsEnum termsEnum = GetTermsEnum(context);
                if (termsEnum == null)
                {
                    return null;
                }
                DocsEnum docs = termsEnum.Docs(acceptDocs, null);
                Debug.Assert(docs != null);
                return new TermScorer(this, docs, Similarity.DoSimScorer(Stats, context));
            }

            /// <summary>
            /// Returns a <seealso cref="TermsEnum"/> positioned at this weights Term or null if
            /// the term does not exist in the given context
            /// </summary>
            private TermsEnum GetTermsEnum(AtomicReaderContext context)
            {
                TermState state = TermStates.Get(context.Ord);
                if (state == null) // term is not present in that reader
                {
                    Debug.Assert(TermNotInReader(context.AtomicReader, OuterInstance._term), "no termstate found but term exists in reader term=" + OuterInstance._term);
                    return null;
                }
                //System.out.println("LD=" + reader.getLiveDocs() + " set?=" + (reader.getLiveDocs() != null ? reader.getLiveDocs().get(0) : "null"));
                TermsEnum termsEnum = context.AtomicReader.Terms(OuterInstance._term.Field).Iterator(null);
                termsEnum.SeekExact(OuterInstance._term.Bytes, state);
                return termsEnum;
            }

            private bool TermNotInReader(AtomicReader reader, Term term)
            {
                // only called from assert
                //System.out.println("TQ.termNotInReader reader=" + reader + " term=" + field + ":" + bytes.utf8ToString());
                return reader.DocFreq(term) == 0;
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                Scorer scorer = Scorer(context, context.AtomicReader.LiveDocs);
                if (scorer != null)
                {
                    int newDoc = scorer.Advance(doc);
                    if (newDoc == doc)
                    {
                        float freq = scorer.Freq;
                        SimScorer docScorer = Similarity.DoSimScorer(Stats, context);
                        ComplexExplanation result = new ComplexExplanation();
                        result.Description = "weight(" + Query + " in " + doc + ") [" + Similarity.GetType().Name + "], result of:";
                        Explanation scoreExplanation = docScorer.Explain(doc, new Explanation(freq, "termFreq=" + freq));
                        result.AddDetail(scoreExplanation);
                        result.Value = scoreExplanation.Value;
                        result.Match = true;
                        return result;
                    }
                }
                return new ComplexExplanation(false, 0.0f, "no matching term");
            }
        }

        /// <summary>
        /// Constructs a query for the term <code>t</code>. </summary>
        public TermQuery(Term t)
            : this(t, -1)
        {
        }

        /// <summary>
        /// Expert: constructs a TermQuery that will use the
        ///  provided docFreq instead of looking up the docFreq
        ///  against the searcher.
        /// </summary>
        public TermQuery(Term t, int docFreq)
        {
            _term = t;
            this.DocFreq = docFreq;
            PerReaderTermState = null;
        }

        /// <summary>
        /// Expert: constructs a TermQuery that will use the
        ///  provided docFreq instead of looking up the docFreq
        ///  against the searcher.
        /// </summary>
        public TermQuery(Term t, TermContext states)
        {
            Debug.Assert(states != null);
            _term = t;
            DocFreq = states.DocFreq;
            PerReaderTermState = states;
        }

        /// <summary>
        /// Returns the term of this query. </summary>
        public virtual Term Term
        {
            get
            {
                return _term;
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            IndexReaderContext context = searcher.TopReaderContext;
            TermContext termState;
            if (PerReaderTermState == null || PerReaderTermState.TopReaderContext != context)
            {
                // make TermQuery single-pass if we don't have a PRTS or if the context differs!
                termState = TermContext.Build(context, _term);
            }
            else
            {
                // PRTS was pre-build for this IS
                termState = this.PerReaderTermState;
            }

            // we must not ignore the given docFreq - if set use the given value (lie)
            if (DocFreq != -1)
            {
                termState.DocFreq = DocFreq;
            }

            return new TermWeight(this, searcher, termState);
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            terms.Add(Term);
        }

        /// <summary>
        /// Prints a user-readable version of this query. </summary>
        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            if (!_term.Field.Equals(field))
            {
                buffer.Append(_term.Field);
                buffer.Append(":");
            }
            buffer.Append(_term.Text());
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        /// <summary>
        /// Returns true iff <code>o</code> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (!(o is TermQuery))
            {
                return false;
            }
            TermQuery other = (TermQuery)o;
            return (this.Boost == other.Boost) && this._term.Equals(other._term);
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            return Number.FloatToIntBits(Boost) ^ _term.GetHashCode();
        }
    }
}