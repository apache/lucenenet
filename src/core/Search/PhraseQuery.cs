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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{

    /// <summary>A Query that matches documents containing a particular sequence of terms.
    /// A PhraseQuery is built by QueryParser for input like <c>"new york"</c>.
    /// 
    /// <p/>This query may be combined with other terms or queries with a <see cref="BooleanQuery" />.
    /// </summary>
    [Serializable]
    public class PhraseQuery : Query
    {
        private string field;
        private IList<Term> terms = new EquatableList<Term>(4);
        private IList<int> positions = new EquatableList<int>(4);
        private int maxPosition = 0;
        private int slop = 0;

        /// <summary>Constructs an empty phrase query. </summary>
        public PhraseQuery()
        {
        }

        /// <summary>Sets the number of other words permitted between words in query phrase.
        /// If zero, then this is an exact phrase search.  For larger values this works
        /// like a <c>WITHIN</c> or <c>NEAR</c> operator.
        /// <p/>The slop is in fact an edit-distance, where the units correspond to
        /// moves of terms in the query phrase out of position.  For example, to switch
        /// the order of two words requires two moves (the first move places the words
        /// atop one another), so to permit re-orderings of phrases, the slop must be
        /// at least two.
        /// <p/>More exact matches are scored higher than sloppier matches, thus search
        /// results are sorted by exactness.
        /// <p/>The slop is zero by default, requiring exact matches.
        /// </summary>
        public virtual int Slop
        {
            get { return slop; }
            set { slop = value; }
        }

        /// <summary> Adds a term to the end of the query phrase.
        /// The relative position of the term is the one immediately after the last term added.
        /// </summary>
        public virtual void Add(Term term)
        {
            int position = 0;
            if (positions.Count > 0)
                position = positions[positions.Count - 1] + 1;

            Add(term, position);
        }

        /// <summary> Adds a term to the end of the query phrase.
        /// The relative position of the term within the phrase is specified explicitly.
        /// This allows e.g. phrases with more than one term at the same position
        /// or phrases with gaps (e.g. in connection with stopwords).
        /// 
        /// </summary>
        /// <param name="term">
        /// </param>
        /// <param name="position">
        /// </param>
        public virtual void Add(Term term, int position)
        {
            if (terms.Count == 0)
                field = term.Field;
            else if (term.Field != field)
            {
                throw new ArgumentException("All phrase terms must be in the same field: " + term);
            }

            terms.Add(term);
            positions.Add(position);
            if (position > maxPosition)
                maxPosition = position;
        }

        /// <summary>Returns the set of terms in this phrase. </summary>
        public virtual Term[] GetTerms()
        {
            return terms.ToArray();
        }

        /// <summary> Returns the relative positions of terms in this phrase.</summary>
        public virtual int[] GetPositions()
        {
            int[] result = new int[positions.Count];
            for (int i = 0; i < positions.Count; i++)
                result[i] = positions[i];
            return result;
        }

        public override Query Rewrite(IndexReader reader)
        {
            if (!terms.Any())
            {
                var bq = new BooleanQuery();
                bq.Boost = Boost;
                return bq;
            }
            else if (terms.Count == 1)
            {
                var tq = new TermQuery(terms[0]);
                tq.Boost = Boost;
                return tq;
            }
            else
            {
                return base.Rewrite(reader);
            }
        }

        [Serializable]
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
                        var terms2 = new Term[terms.Length];
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

            public int CompareTo(PostingsAndFreq other)
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
                for (var i = 0; i < terms.Length; i++)
                {
                    var res = terms[i].CompareTo(other.terms[i]);
                    if (res != 0) return res;
                }
                return 0;
            }

            public override int GetHashCode()
            {
                var prime = 31;
                var result = 1;
                result = prime * result + docFreq;
                result = prime * result + position;
                for (var i = 0; i < nTerms; i++)
                {
                    result = prime * result + terms[i].GetHashCode();
                }
                return result;
            }

            public override bool Equals(object obj)
            {
                if (this == obj) return true;
                if (obj == null) return false;
                if (GetType() != obj.GetType()) return false;
                var other = (PostingsAndFreq)obj;
                if (docFreq != other.docFreq) return false;
                if (position != other.position) return false;
                if (terms == null) return other.terms == null;
                return Arrays.Equals(terms, other.terms);
            }
        }

        [Serializable]
        private class PhraseWeight : Weight
        {
            private PhraseQuery parent;
            private readonly Similarity similarity;
            private readonly Similarity.SimWeight stats;
            [NonSerialized]
            private TermContext[] states;

            public PhraseWeight(PhraseQuery parent, IndexSearcher searcher)
            {
                this.parent = parent;

                this.similarity = searcher.Similarity;
                var context = searcher.TopReaderContext;
                states = new TermContext[parent.terms.Count];
                TermStatistics[] termStats = new TermStatistics[parent.terms.Count];
                for (var i = 0; i < parent.terms.Count; i++)
                {
                    var term = parent.terms[i];
                    states[i] = TermContext.Build(context, term, true);
                    termStats[i] = searcher.TermStatistics(term, states[i]);
                }
                stats = similarity.ComputeWeight(parent.Boost, searcher.CollectionStatistics(parent.field),
                                                     termStats);
            }

            public override string ToString()
            {
                return "weight(" + parent + ")";
            }

            public override Query Query
            {
                get { return parent; }
            }

            public override float ValueForNormalization
            {
                get
                {
                    return stats.ValueForNormalization;
                }
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                stats.Normalize(queryNorm, topLevelBoost);
            }

            public override Scorer Scorer(AtomicReaderContext context, bool scoreDocsInOrder, bool topScorer, IBits acceptDocs)
            {
                // assert !terms.isEmpty()

                var reader = context.AtomicReader;
                var liveDocs = acceptDocs;
                var postingsFreqs = new PostingsAndFreq[parent.terms.Count];

                var fieldTerms = reader.Terms(parent.field);
                if (fieldTerms == null)
                {
                    return null;
                }

                var te = fieldTerms.Iterator(null);

                for (var i = 0; i < parent.terms.Count; i++)
                {
                    var t = parent.terms[i];
                    var state = states[i].Get(context.ord);
                    if (state == null)
                    {
                        // assert termNotInReader(reader, t);
                        return null;
                    }
                    te.SeekExact(t.Bytes, state);
                    var postingsEnum = te.DocsAndPositions(liveDocs, null, DocsEnum.FLAG_NONE);

                    if (postingsEnum == null)
                    {
                        // assert te.seekExact(t.bytes(), false) : "termstate found but no term exists in reader";

                        throw new InvalidOperationException("field \"" + t.Field + "\" was indexed without position data; cannot run PhraseQuery (term=" + t.Text + ")");
                    }
                    postingsFreqs[i] = new PostingsAndFreq(postingsEnum, te.DocFreq, Convert.ToInt32(parent.positions[i]), t);
                }

                if (parent.slop == 0)
                {
                    ArrayUtil.MergeSort(postingsFreqs);
                }

                if (parent.slop == 0)
                {
                    var s = new ExactPhraseScorer(this, postingsFreqs, similarity.GetExactSimScorer(stats, context));
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
                    return new SloppyPhraseScorer(this, postingsFreqs, parent.slop, similarity.GetSloppySimScorer(stats, context));
                }
            }

            // only called from assert
            private bool TermNotInReader(AtomicReader reader, Term term)
            {
                return reader.DocFreq(term) == 0;
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                var scorer = Scorer(context, true, false, ((AtomicReader)context.Reader).LiveDocs);
                if (scorer != null)
                {
                    var newDoc = scorer.Advance(doc);
                    if (newDoc == doc)
                    {
                        var freq = parent.slop == 0 ? scorer.Freq : ((SloppyPhraseScorer) scorer).SloppyFreq;
                        var docScorer = similarity.GetSloppySimScorer(stats, context);
                        var result = new ComplexExplanation();
                        result.Description = "weight(" + Query + " in " + doc + ") [" + similarity.GetType().Name + "], result of:";
                        var scoreExplanation = docScorer.Explain(doc, new Explanation(freq, "phraseFreq=" + freq));
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

        /// <seealso cref="Lucene.Net.Search.Query.ExtractTerms(System.Collections.Generic.ISet{Term})">
        /// </seealso>
        public override void ExtractTerms(ISet<Term> queryTerms)
        {
            queryTerms.UnionWith(terms);
        }

        /// <summary>Prints a user-readable version of this query. </summary>
        public override string ToString(string f)
        {
            var buffer = new StringBuilder();
            if (field != null && !field.Equals(f))
            {
                buffer.Append(field);
                buffer.Append(":");
            }

            buffer.Append("\"");
            var pieces = new string[maxPosition + 1];
            for (int i = 0; i < terms.Count; i++)
            {
                int pos = positions[i];
                string s = pieces[pos];
                if (s == null)
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

        /// <summary>Returns true iff <c>o</c> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (!(o is PhraseQuery))
                return false;
            var other = (PhraseQuery)o;
            return (Boost == other.Boost) 
                && (slop == other.slop) 
                && terms.Equals(other.terms) 
                && positions.Equals(other.positions);
        }

        /// <summary>Returns a hash code value for this object.</summary>
        public override int GetHashCode()
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(Boost), 0)
                ^ slop 
                ^ terms.GetHashCode()
                ^ positions.GetHashCode();
        }
    }
}