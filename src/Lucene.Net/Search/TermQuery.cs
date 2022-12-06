using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
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
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using IBits = Lucene.Net.Util.IBits;
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
    /// A <see cref="Query"/> that matches documents containing a term.
    /// this may be combined with other terms with a <see cref="BooleanQuery"/>.
    /// </summary>
    public class TermQuery : Query
    {
        private readonly Term term;
        private readonly int docFreq;
        private readonly TermContext perReaderTermState;

        internal sealed class TermWeight : Weight
        {
            private readonly TermQuery outerInstance;

            internal readonly Similarity similarity;
            internal readonly Similarity.SimWeight stats;
            internal readonly TermContext termStates;

            public TermWeight(TermQuery outerInstance, IndexSearcher searcher, TermContext termStates)
            {
                this.outerInstance = outerInstance;
                if (Debugging.AssertsEnabled) Debugging.Assert(termStates != null, "TermContext must not be null");
                this.termStates = termStates;
                this.similarity = searcher.Similarity;
                this.stats = similarity.ComputeWeight(outerInstance.Boost, searcher.CollectionStatistics(outerInstance.term.Field), searcher.TermStatistics(outerInstance.term, termStates));
            }

            public override string ToString()
            {
                return "weight(" + outerInstance + ")";
            }

            public override Query Query => outerInstance;

            public override float GetValueForNormalization()
            {
                return stats.GetValueForNormalization();
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                stats.Normalize(queryNorm, topLevelBoost);
            }

            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(termStates.TopReaderContext == ReaderUtil.GetTopLevelContext(context),"The top-reader used to create Weight ({0}) is not the same as the current reader's top-reader ({1})", termStates.TopReaderContext, ReaderUtil.GetTopLevelContext(context));
                TermsEnum termsEnum = GetTermsEnum(context);
                if (termsEnum is null)
                {
                    return null;
                }
                DocsEnum docs = termsEnum.Docs(acceptDocs, null);
                if (Debugging.AssertsEnabled) Debugging.Assert(docs != null);
                return new TermScorer(this, docs, similarity.GetSimScorer(stats, context));
            }

            /// <summary>
            /// Returns a <see cref="TermsEnum"/> positioned at this weights <see cref="Index.Term"/> or <c>null</c> if
            /// the term does not exist in the given context.
            /// </summary>
            private TermsEnum GetTermsEnum(AtomicReaderContext context)
            {
                TermState state = termStates.Get(context.Ord);
                if (state is null) // term is not present in that reader
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(TermNotInReader(context.AtomicReader, outerInstance.term), "no termstate found but term exists in reader term={0}", outerInstance.term);
                    return null;
                }
                //System.out.println("LD=" + reader.getLiveDocs() + " set?=" + (reader.getLiveDocs() != null ? reader.getLiveDocs().get(0) : "null"));
                TermsEnum termsEnum = context.AtomicReader.GetTerms(outerInstance.term.Field).GetEnumerator();
                termsEnum.SeekExact(outerInstance.term.Bytes, state);
                return termsEnum;
            }

            private static bool TermNotInReader(AtomicReader reader, Term term) // LUCENENET: CA1822: Mark members as static
            {
                // only called from assert
                //System.out.println("TQ.termNotInReader reader=" + reader + " term=" + field + ":" + bytes.utf8ToString());
                return reader.DocFreq(term) == 0;
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                Scorer scorer = GetScorer(context, context.AtomicReader.LiveDocs);
                if (scorer != null)
                {
                    int newDoc = scorer.Advance(doc);
                    if (newDoc == doc)
                    {
                        float freq = scorer.Freq;
                        SimScorer docScorer = similarity.GetSimScorer(stats, context);
                        ComplexExplanation result = new ComplexExplanation();
                        result.Description = "weight(" + Query + " in " + doc + ") [" + similarity.GetType().Name + "], result of:";
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
        /// Constructs a query for the term <paramref name="t"/>. </summary>
        public TermQuery(Term t)
            : this(t, -1)
        {
        }

        /// <summary>
        /// Expert: constructs a <see cref="TermQuery"/> that will use the
        /// provided <paramref name="docFreq"/> instead of looking up the docFreq
        /// against the searcher.
        /// </summary>
        public TermQuery(Term t, int docFreq)
        {
            term = t;
            this.docFreq = docFreq;
            perReaderTermState = null;
        }

        /// <summary>
        /// Expert: constructs a <see cref="TermQuery"/> that will use the
        /// provided docFreq instead of looking up the docFreq
        /// against the searcher.
        /// </summary>
        public TermQuery(Term t, TermContext states)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(states != null);
            term = t;
            docFreq = states.DocFreq;
            perReaderTermState = states;
        }

        /// <summary>
        /// Returns the term of this query. </summary>
        public virtual Term Term => term;

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            IndexReaderContext context = searcher.TopReaderContext;
            TermContext termState;
            if (perReaderTermState is null || perReaderTermState.TopReaderContext != context)
            {
                // make TermQuery single-pass if we don't have a PRTS or if the context differs!
                termState = TermContext.Build(context, term);
            }
            else
            {
                // PRTS was pre-build for this IS
                termState = this.perReaderTermState;
            }

            // we must not ignore the given docFreq - if set use the given value (lie)
            if (docFreq != -1)
            {
                termState.DocFreq = docFreq;
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
            if (!term.Field.Equals(field, StringComparison.Ordinal))
            {
                buffer.Append(term.Field);
                buffer.Append(':');
            }
            buffer.Append(term.Text);
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="o"/> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (!(o is TermQuery))
            {
                return false;
            }
            TermQuery other = (TermQuery)o;
            // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
            return (NumericUtils.SingleToSortableInt32(this.Boost) == NumericUtils.SingleToSortableInt32(other.Boost))
                && this.term.Equals(other.term);
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            return J2N.BitConversion.SingleToInt32Bits(Boost) ^ term.GetHashCode();
        }
    }
}