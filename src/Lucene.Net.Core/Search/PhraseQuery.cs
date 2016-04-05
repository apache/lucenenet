using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search
{
    using Lucene.Net.Support;
    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using AtomicReader = Lucene.Net.Index.AtomicReader;

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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Bits = Lucene.Net.Util.Bits;
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
        private string Field;
        private List<Term> Terms_Renamed = new List<Term>(4);
        private List<int?> Positions_Renamed = new List<int?>(4);
        private int MaxPosition = 0;
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
            if (Positions_Renamed.Count > 0)
            {
                position = (int)Positions_Renamed[Positions_Renamed.Count - 1] + 1;
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
            if (Terms_Renamed.Count == 0)
            {
                Field = term.Field;
            }
            else if (!term.Field.Equals(Field))
            {
                throw new System.ArgumentException("All phrase terms must be in the same field: " + term);
            }

            Terms_Renamed.Add(term);
            Positions_Renamed.Add(Convert.ToInt32(position));
            if (position > MaxPosition)
            {
                MaxPosition = position;
            }
        }

        /// <summary>
        /// Returns the set of terms in this phrase. </summary>
        public virtual Term[] Terms
        {
            get
            {
                return Terms_Renamed.ToArray();
            }
        }

        /// <summary>
        /// Returns the relative positions of terms in this phrase.
        /// </summary>
        public virtual int[] Positions
        {
            get
            {
                int[] result = new int[Positions_Renamed.Count];
                for (int i = 0; i < Positions_Renamed.Count; i++)
                {
                    result[i] = (int)Positions_Renamed[i];
                }
                return result;
            }
        }

        public override Query Rewrite(IndexReader reader)
        {
            if (Terms_Renamed.Count == 0)
            {
                BooleanQuery bq = new BooleanQuery();
                bq.Boost = Boost;
                return bq;
            }
            else if (Terms_Renamed.Count == 1)
            {
                TermQuery tq = new TermQuery(Terms_Renamed[0]);
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
            internal readonly DocsAndPositionsEnum Postings;
            internal readonly int DocFreq;
            internal readonly int Position;
            internal readonly Term[] Terms;
            internal readonly int NTerms; // for faster comparisons

            public PostingsAndFreq(DocsAndPositionsEnum postings, int docFreq, int position, params Term[] terms)
            {
                this.Postings = postings;
                this.DocFreq = docFreq;
                this.Position = position;
                NTerms = terms == null ? 0 : terms.Length;
                if (NTerms > 0)
                {
                    if (terms.Length == 1)
                    {
                        this.Terms = terms;
                    }
                    else
                    {
                        Term[] terms2 = new Term[terms.Length];
                        Array.Copy(terms, 0, terms2, 0, terms.Length);
                        Array.Sort(terms2);
                        this.Terms = terms2;
                    }
                }
                else
                {
                    this.Terms = null;
                }
            }

            public virtual int CompareTo(PostingsAndFreq other)
            {
                if (DocFreq != other.DocFreq)
                {
                    return DocFreq - other.DocFreq;
                }
                if (Position != other.Position)
                {
                    return Position - other.Position;
                }
                if (NTerms != other.NTerms)
                {
                    return NTerms - other.NTerms;
                }
                if (NTerms == 0)
                {
                    return 0;
                }
                for (int i = 0; i < Terms.Length; i++)
                {
                    int res = Terms[i].CompareTo(other.Terms[i]);
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
                result = prime * result + DocFreq;
                result = prime * result + Position;
                for (int i = 0; i < NTerms; i++)
                {
                    result = prime * result + Terms[i].GetHashCode();
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
                if (DocFreq != other.DocFreq)
                {
                    return false;
                }
                if (Position != other.Position)
                {
                    return false;
                }
                if (Terms == null)
                {
                    return other.Terms == null;
                }
                return Arrays.Equals(Terms, other.Terms);
            }
        }

        private class PhraseWeight : Weight
        {
            private readonly PhraseQuery OuterInstance;

            internal readonly Similarity Similarity;
            internal readonly Similarity.SimWeight Stats;

            
            internal TermContext[] States;

            public PhraseWeight(PhraseQuery outerInstance, IndexSearcher searcher)
            {
                this.OuterInstance = outerInstance;
                this.Similarity = searcher.Similarity;
                IndexReaderContext context = searcher.TopReaderContext;
                States = new TermContext[outerInstance.Terms_Renamed.Count];
                TermStatistics[] termStats = new TermStatistics[outerInstance.Terms_Renamed.Count];
                for (int i = 0; i < outerInstance.Terms_Renamed.Count; i++)
                {
                    Term term = outerInstance.Terms_Renamed[i];
                    States[i] = TermContext.Build(context, term);
                    termStats[i] = searcher.TermStatistics(term, States[i]);
                }
                Stats = Similarity.ComputeWeight(outerInstance.Boost, searcher.CollectionStatistics(outerInstance.Field), termStats);
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
                Debug.Assert(OuterInstance.Terms_Renamed.Count > 0);
                AtomicReader reader = context.AtomicReader;
                Bits liveDocs = acceptDocs;
                PostingsAndFreq[] postingsFreqs = new PostingsAndFreq[OuterInstance.Terms_Renamed.Count];

                Terms fieldTerms = reader.Terms(OuterInstance.Field);
                if (fieldTerms == null)
                {
                    return null;
                }

                // Reuse single TermsEnum below:
                TermsEnum te = fieldTerms.Iterator(null);

                for (int i = 0; i < OuterInstance.Terms_Renamed.Count; i++)
                {
                    Term t = OuterInstance.Terms_Renamed[i];
                    TermState state = States[i].Get(context.Ord);
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
                    postingsFreqs[i] = new PostingsAndFreq(postingsEnum, te.DocFreq(), (int)OuterInstance.Positions_Renamed[i], t);
                }

                // sort by increasing docFreq order
                if (OuterInstance.slop == 0)
                {
                    ArrayUtil.TimSort(postingsFreqs);
                }

                if (OuterInstance.slop == 0) // optimize exact case
                {
                    ExactPhraseScorer s = new ExactPhraseScorer(this, postingsFreqs, Similarity.DoSimScorer(Stats, context));
                    if (s.NoDocs)
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
                    return new SloppyPhraseScorer(this, postingsFreqs, OuterInstance.slop, Similarity.DoSimScorer(Stats, context));
                }
            }

            // only called from assert
            internal virtual bool TermNotInReader(AtomicReader reader, Term term)
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
                        float freq = OuterInstance.slop == 0 ? scorer.Freq() : ((SloppyPhraseScorer)scorer).SloppyFreq();
                        SimScorer docScorer = Similarity.DoSimScorer(Stats, context);
                        ComplexExplanation result = new ComplexExplanation();
                        result.Description = "weight(" + Query + " in " + doc + ") [" + Similarity.GetType().Name + "], result of:";
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
            queryTerms.UnionWith(Terms_Renamed);
        }

        /// <summary>
        /// Prints a user-readable version of this query. </summary>
        public override string ToString(string f)
        {
            StringBuilder buffer = new StringBuilder();
            if (Field != null && !Field.Equals(f))
            {
                buffer.Append(Field);
                buffer.Append(":");
            }

            buffer.Append("\"");
            string[] pieces = new string[MaxPosition + 1];
            for (int i = 0; i < Terms_Renamed.Count; i++)
            {
                int pos = (int)Positions_Renamed[i];
                string s = pieces[pos];
                if (s == null)
                {
                    s = (Terms_Renamed[i]).Text();
                }
                else
                {
                    s = s + "|" + (Terms_Renamed[i]).Text();
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
            return (this.Boost == other.Boost) && (this.slop == other.slop) && this.Terms_Renamed.SequenceEqual(other.Terms_Renamed) && this.Positions_Renamed.SequenceEqual(other.Positions_Renamed);
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            return Number.FloatToIntBits(Boost) ^ slop ^ Terms_Renamed.GetHashCode() ^ Positions_Renamed.GetHashCode();
        }
    }
}