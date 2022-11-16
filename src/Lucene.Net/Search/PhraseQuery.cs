using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

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
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using IBits = Lucene.Net.Util.IBits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using SimScorer = Lucene.Net.Search.Similarities.Similarity.SimScorer;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TermState = Lucene.Net.Index.TermState;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// A <see cref="Query"/> that matches documents containing a particular sequence of terms.
    /// A <see cref="PhraseQuery"/> is built by QueryParser for input like <c>"new york"</c>.
    ///
    /// <para/>This query may be combined with other terms or queries with a <see cref="BooleanQuery"/>.
    /// <para/>
    /// Collection initializer note: To create and populate a <see cref="PhraseQuery"/>
    /// in a single statement, you can use the following example as a guide:
    /// 
    /// <code>
    /// var phraseQuery = new PhraseQuery() {
    ///     new Term("field", "microsoft"), 
    ///     new Term("field", "office")
    /// };
    /// </code>
    /// Note that as long as you specify all of the parameters, you can use either
    /// <see cref="Add(Term)"/> or <see cref="Add(Term, int)"/>
    /// as the method to use to initialize. If there are multiple parameters, each parameter set
    /// must be surrounded by curly braces.
    /// </summary>
    public class PhraseQuery : Query, IEnumerable<Term> // LUCENENET specific - implemented IEnumerable<Term>, which allows for use of collection initializer. See: https://stackoverflow.com/a/9195144
    {
        private string field;
        private readonly IList<Term> terms = new JCG.List<Term>(4); // LUCENENET: marked readonly
        private readonly IList<int> positions = new JCG.List<int>(4); // LUCENENET: marked readonly
        private int maxPosition = 0;
        private int slop = 0;

        /// <summary>
        /// Constructs an empty phrase query. </summary>
        public PhraseQuery()
        {
        }

        /// <summary>
        /// Sets the number of other words permitted between words in query phrase.
        /// If zero, then this is an exact phrase search.  For larger values this works
        /// like a <c>WITHIN</c> or <c>NEAR</c> operator.
        ///
        /// <para/>The slop is in fact an edit-distance, where the units correspond to
        /// moves of terms in the query phrase out of position.  For example, to switch
        /// the order of two words requires two moves (the first move places the words
        /// atop one another), so to permit re-orderings of phrases, the slop must be
        /// at least two.
        ///
        /// <para/>More exact matches are scored higher than sloppier matches, thus search
        /// results are sorted by exactness.
        ///
        /// <para/>The slop is zero by default, requiring exact matches.
        /// </summary>
        public virtual int Slop
        {
            get => slop;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Slop), "slop value cannot be negative"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)

                slop = value;
            }
        }

        /// <summary>
        /// Adds a term to the end of the query phrase.
        /// The relative position of the term is the one immediately after the last term added.
        /// </summary>
        public virtual void Add(Term term)
        {
            int position = 0;
            if (positions.Count > 0)
            {
                position = positions[positions.Count - 1] + 1;
            }

            Add(term, position);
        }

        /// <summary>
        /// Adds a term to the end of the query phrase.
        /// The relative position of the term within the phrase is specified explicitly.
        /// this allows e.g. phrases with more than one term at the same position
        /// or phrases with gaps (e.g. in connection with stopwords).
        /// </summary>
        public virtual void Add(Term term, int position)
        {
            if (terms.Count == 0)
            {
                field = term.Field;
            }
            else if (!term.Field.Equals(field, StringComparison.Ordinal))
            {
                throw new ArgumentException("All phrase terms must be in the same field: " + term);
            }

            terms.Add(term);
            positions.Add(position);
            if (position > maxPosition)
            {
                maxPosition = position;
            }
        }

        /// <summary>
        /// Returns the set of terms in this phrase. </summary>
        public virtual Term[] GetTerms()
        {
            return terms.ToArray();
        }

        /// <summary>
        /// Returns the relative positions of terms in this phrase.
        /// </summary>
        public virtual int[] GetPositions()
        {
            int[] result = new int[positions.Count];
            for (int i = 0; i < positions.Count; i++)
            {
                result[i] = positions[i];
            }
            return result;
        }

        public override Query Rewrite(IndexReader reader)
        {
            if (terms.Count == 0)
            {
                BooleanQuery bq = new BooleanQuery();
                bq.Boost = Boost;
                return bq;
            }
            else if (terms.Count == 1)
            {
                TermQuery tq = new TermQuery(terms[0]);
                tq.Boost = Boost;
                return tq;
            }
            else
            {
                return base.Rewrite(reader);
            }
        }

        internal class PostingsAndFreq : IComparable<PostingsAndFreq>
        {
            internal readonly DocsAndPositionsEnum postings;
            internal readonly int docFreq;
            internal readonly int position;
            internal readonly Term[] terms;
            internal readonly int nTerms; // for faster comparisons

            public PostingsAndFreq(DocsAndPositionsEnum postings, int docFreq, int position, params Term[] terms)
            {
                this.postings = postings;
                this.docFreq = docFreq;
                this.position = position;
                nTerms = terms is null ? 0 : terms.Length;
                if (nTerms > 0)
                {
                    if (terms.Length == 1)
                    {
                        this.terms = terms;
                    }
                    else
                    {
                        Term[] terms2 = new Term[terms.Length];
                        Arrays.Copy(terms, 0, terms2, 0, terms.Length);
                        Array.Sort(terms2);
                        this.terms = terms2;
                    }
                }
                else
                {
                    this.terms = null;
                }
            }

            public virtual int CompareTo(PostingsAndFreq other)
            {
                if (docFreq != other.docFreq)
                {
                    return docFreq - other.docFreq;
                }
                if (position != other.position)
                {
                    return position - other.position;
                }
                if (nTerms != other.nTerms)
                {
                    return nTerms - other.nTerms;
                }
                if (nTerms == 0)
                {
                    return 0;
                }
                for (int i = 0; i < terms.Length; i++)
                {
                    int res = terms[i].CompareTo(other.terms[i]);
                    if (res != 0)
                    {
                        return res;
                    }
                }
                return 0;
            }

            public override int GetHashCode()
            {
                const int prime = 31;
                int result = 1;
                result = prime * result + docFreq;
                result = prime * result + position;
                for (int i = 0; i < nTerms; i++)
                {
                    result = prime * result + terms[i].GetHashCode();
                }
                return result;
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
                PostingsAndFreq other = (PostingsAndFreq)obj;
                if (docFreq != other.docFreq)
                {
                    return false;
                }
                if (position != other.position)
                {
                    return false;
                }
                if (terms is null)
                {
                    return other.terms is null;
                }
                return Arrays.Equals(terms, other.terms);
            }
        }

        private class PhraseWeight : Weight
        {
            private readonly PhraseQuery outerInstance;

            internal readonly Similarity similarity;
            internal readonly Similarity.SimWeight stats;

            
            internal TermContext[] states;

            public PhraseWeight(PhraseQuery outerInstance, IndexSearcher searcher)
            {
                this.outerInstance = outerInstance;
                this.similarity = searcher.Similarity;
                IndexReaderContext context = searcher.TopReaderContext;
                states = new TermContext[outerInstance.terms.Count];
                TermStatistics[] termStats = new TermStatistics[outerInstance.terms.Count];
                for (int i = 0; i < outerInstance.terms.Count; i++)
                {
                    Term term = outerInstance.terms[i];
                    states[i] = TermContext.Build(context, term);
                    termStats[i] = searcher.TermStatistics(term, states[i]);
                }
                stats = similarity.ComputeWeight(outerInstance.Boost, searcher.CollectionStatistics(outerInstance.field), termStats);
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
                if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.terms.Count > 0);
                AtomicReader reader = context.AtomicReader;
                IBits liveDocs = acceptDocs;
                PostingsAndFreq[] postingsFreqs = new PostingsAndFreq[outerInstance.terms.Count];

                Terms fieldTerms = reader.GetTerms(outerInstance.field);
                if (fieldTerms is null)
                {
                    return null;
                }

                // Reuse single TermsEnum below:
                TermsEnum te = fieldTerms.GetEnumerator();

                for (int i = 0; i < outerInstance.terms.Count; i++)
                {
                    Term t = outerInstance.terms[i];
                    TermState state = states[i].Get(context.Ord);
                    if (state is null) // term doesnt exist in this segment
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(TermNotInReader(reader, t), "no termstate found but term exists in reader");
                        return null;
                    }
                    te.SeekExact(t.Bytes, state);
                    DocsAndPositionsEnum postingsEnum = te.DocsAndPositions(liveDocs, null, DocsAndPositionsFlags.NONE);

                    // PhraseQuery on a field that did not index
                    // positions.
                    if (postingsEnum is null)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(te.SeekExact(t.Bytes), "termstate found but no term exists in reader");
                        // term does exist, but has no positions
                        throw IllegalStateException.Create("field \"" + t.Field + "\" was indexed without position data; cannot run PhraseQuery (term=" + t.Text + ")");
                    }
                    postingsFreqs[i] = new PostingsAndFreq(postingsEnum, te.DocFreq, outerInstance.positions[i], t);
                }

                // sort by increasing docFreq order
                if (outerInstance.slop == 0)
                {
                    ArrayUtil.TimSort(postingsFreqs);
                }

                if (outerInstance.slop == 0) // optimize exact case
                {
                    ExactPhraseScorer s = new ExactPhraseScorer(this, postingsFreqs, similarity.GetSimScorer(stats, context));
                    if (s.noDocs)
                    {
                        return null;
                    }
                    else
                    {
                        return s;
                    }
                }
                else
                {
                    return new SloppyPhraseScorer(this, postingsFreqs, outerInstance.slop, similarity.GetSimScorer(stats, context));
                }
            }

            // only called from assert
            private static bool TermNotInReader(AtomicReader reader, Term term) // LUCENENET: CA1822: Mark members as static
            {
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
                        float freq = outerInstance.slop == 0 ? scorer.Freq : ((SloppyPhraseScorer)scorer).SloppyFreq;
                        SimScorer docScorer = similarity.GetSimScorer(stats, context);
                        ComplexExplanation result = new ComplexExplanation();
                        result.Description = "weight(" + Query + " in " + doc + ") [" + similarity.GetType().Name + "], result of:";
                        Explanation scoreExplanation = docScorer.Explain(doc, new Explanation(freq, "phraseFreq=" + freq));
                        result.AddDetail(scoreExplanation);
                        result.Value = scoreExplanation.Value;
                        result.Match = true;
                        return result;
                    }
                }

                return new ComplexExplanation(false, 0.0f, "no matching term");
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new PhraseWeight(this, searcher);
        }

        /// <seealso cref="Lucene.Net.Search.Query.ExtractTerms(ISet{Term})"/>
        public override void ExtractTerms(ISet<Term> queryTerms)
        {
            queryTerms.UnionWith(terms);
        }

        /// <summary>
        /// Prints a user-readable version of this query. </summary>
        public override string ToString(string f)
        {
            StringBuilder buffer = new StringBuilder();
            if (field != null && !field.Equals(f, StringComparison.Ordinal))
            {
                buffer.Append(field);
                buffer.Append(':');
            }

            buffer.Append("\"");
            string[] pieces = new string[maxPosition + 1];
            for (int i = 0; i < terms.Count; i++)
            {
                int pos = positions[i];
                string s = pieces[pos];
                if (s is null)
                {
                    s = terms[i].Text;
                }
                else
                {
                    s = s + "|" + terms[i].Text;
                }
                pieces[pos] = s;
            }
            for (int i = 0; i < pieces.Length; i++)
            {
                if (i > 0)
                {
                    buffer.Append(' ');
                }
                string s = pieces[i];
                if (s is null)
                {
                    buffer.Append('?');
                }
                else
                {
                    buffer.Append(s);
                }
            }
            buffer.Append("\"");

            if (slop != 0)
            {
                buffer.Append('~');
                buffer.Append(slop);
            }

            buffer.Append(ToStringUtils.Boost(Boost));

            return buffer.ToString();
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="o"/> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (!(o is PhraseQuery))
            {
                return false;
            }
            PhraseQuery other = (PhraseQuery)o;
            // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
            return (NumericUtils.SingleToSortableInt32(this.Boost) == NumericUtils.SingleToSortableInt32(other.Boost)) 
                && (this.slop == other.slop) 
                && this.terms.Equals(other.terms) 
                && this.positions.Equals(other.positions);
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            return J2N.BitConversion.SingleToInt32Bits(Boost) 
                ^ slop 
                ^ terms.GetHashCode() 
                ^ positions.GetHashCode();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="terms"/> collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the <see cref="terms"/> collection.</returns>
        // LUCENENET specific
        public IEnumerator<Term> GetEnumerator()
        {
            return this.terms.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="terms"/> collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the <see cref="terms"/> collection.</returns>
        // LUCENENET specific
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}