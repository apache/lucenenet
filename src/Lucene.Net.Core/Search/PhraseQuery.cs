using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IBits = Lucene.Net.Util.IBits;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
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
    /// A Query that matches documents containing a particular sequence of terms.
    /// A PhraseQuery is built by QueryParser for input like <code>"new york"</code>.
    ///
    /// <p>this query may be combined with other terms or queries with a <seealso cref="BooleanQuery"/>.
    /// </summary>
    public class PhraseQuery : Query
    {
        private string field;
        private List<Term> terms = new ValueList<Term>(4);
        private List<int?> positions = new ValueList<int?>(4);
        private int maxPosition = 0;
        private int slop = 0;

        /// <summary>
        /// Constructs an empty phrase query. </summary>
        public PhraseQuery()
        {
        }

        /// <summary>
        /// Sets the number of other words permitted between words in query phrase.
        ///  If zero, then this is an exact phrase search.  For larger values this works
        ///  like a <code>WITHIN</code> or <code>NEAR</code> operator.
        ///
        ///  <p>The slop is in fact an edit-distance, where the units correspond to
        ///  moves of terms in the query phrase out of position.  For example, to switch
        ///  the order of two words requires two moves (the first move places the words
        ///  atop one another), so to permit re-orderings of phrases, the slop must be
        ///  at least two.
        ///
        ///  <p>More exact matches are scored higher than sloppier matches, thus search
        ///  results are sorted by exactness.
        ///
        ///  <p>The slop is zero by default, requiring exact matches.
        /// </summary>
        public virtual int Slop
        {
            set
            {
                if (value < 0)
                {
                    throw new System.ArgumentException("slop value cannot be negative");
                }
                slop = value;
            }
            get
            {
                return slop;
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
                position = (int)positions[positions.Count - 1] + 1;
            }

            Add(term, position);
        }

        /// <summary>
        /// Adds a term to the end of the query phrase.
        /// The relative position of the term within the phrase is specified explicitly.
        /// this allows e.g. phrases with more than one term at the same position
        /// or phrases with gaps (e.g. in connection with stopwords).
        ///
        /// </summary>
        public virtual void Add(Term term, int position)
        {
            if (terms.Count == 0)
            {
                field = term.Field;
            }
            else if (!term.Field.Equals(field))
            {
                throw new System.ArgumentException("All phrase terms must be in the same field: " + term);
            }

            terms.Add(term);
            positions.Add(Convert.ToInt32(position));
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
                result[i] = (int)positions[i];
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
                nTerms = terms == null ? 0 : terms.Length;
                if (nTerms > 0)
                {
                    if (terms.Length == 1)
                    {
                        this.terms = terms;
                    }
                    else
                    {
                        Term[] terms2 = new Term[terms.Length];
                        Array.Copy(terms, 0, terms2, 0, terms.Length);
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
                if (obj == null)
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
                if (terms == null)
                {
                    return other.terms == null;
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

            public override Query Query
            {
                get
                {
                    return outerInstance;
                }
            }

            public override float GetValueForNormalization()
            {
                return stats.GetValueForNormalization();
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                stats.Normalize(queryNorm, topLevelBoost);
            }

            public override Scorer Scorer(AtomicReaderContext context, IBits acceptDocs)
            {
                Debug.Assert(outerInstance.terms.Count > 0);
                AtomicReader reader = context.AtomicReader;
                IBits liveDocs = acceptDocs;
                PostingsAndFreq[] postingsFreqs = new PostingsAndFreq[outerInstance.terms.Count];

                Terms fieldTerms = reader.Terms(outerInstance.field);
                if (fieldTerms == null)
                {
                    return null;
                }

                // Reuse single TermsEnum below:
                TermsEnum te = fieldTerms.Iterator(null);

                for (int i = 0; i < outerInstance.terms.Count; i++)
                {
                    Term t = outerInstance.terms[i];
                    TermState state = states[i].Get(context.Ord);
                    if (state == null) // term doesnt exist in this segment
                    {
                        Debug.Assert(TermNotInReader(reader, t), "no termstate found but term exists in reader");
                        return null;
                    }
                    te.SeekExact(t.Bytes, state);
                    DocsAndPositionsEnum postingsEnum = te.DocsAndPositions(liveDocs, null, DocsEnum.FLAG_NONE);

                    // PhraseQuery on a field that did not index
                    // positions.
                    if (postingsEnum == null)
                    {
                        Debug.Assert(te.SeekExact(t.Bytes), "termstate found but no term exists in reader");
                        // term does exist, but has no positions
                        throw new InvalidOperationException("field \"" + t.Field + "\" was indexed without position data; cannot run PhraseQuery (term=" + t.Text() + ")");
                    }
                    postingsFreqs[i] = new PostingsAndFreq(postingsEnum, te.DocFreq, (int)outerInstance.positions[i], t);
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
            private bool TermNotInReader(AtomicReader reader, Term term)
            {
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

        /// <seealso cref= Lucene.Net.Search.Query#extractTerms(Set) </seealso>
        public override void ExtractTerms(ISet<Term> queryTerms)
        {
            //LUCENE TO-DO Normal conundrum
            queryTerms.UnionWith(terms);
        }

        /// <summary>
        /// Prints a user-readable version of this query. </summary>
        public override string ToString(string f)
        {
            StringBuilder buffer = new StringBuilder();
            if (field != null && !field.Equals(f))
            {
                buffer.Append(field);
                buffer.Append(":");
            }

            buffer.Append("\"");
            string[] pieces = new string[maxPosition + 1];
            for (int i = 0; i < terms.Count; i++)
            {
                int pos = (int)positions[i];
                string s = pieces[pos];
                if (s == null)
                {
                    s = (terms[i]).Text();
                }
                else
                {
                    s = s + "|" + (terms[i]).Text();
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
                if (s == null)
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
                buffer.Append("~");
                buffer.Append(slop);
            }

            buffer.Append(ToStringUtils.Boost(Boost));

            return buffer.ToString();
        }

        /// <summary>
        /// Returns true iff <code>o</code> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (!(o is PhraseQuery))
            {
                return false;
            }
            PhraseQuery other = (PhraseQuery)o;
            return (this.Boost == other.Boost) 
                && (this.slop == other.slop) 
                && this.terms.SequenceEqual(other.terms) 
                && this.positions.SequenceEqual(other.positions);
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            return Number.FloatToIntBits(Boost) 
                ^ slop 
                ^ terms.GetHashCode() 
                ^ positions.GetHashCode();
        }
    }
}