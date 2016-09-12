using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Search.Spell
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
    /// Simple automaton-based spellchecker.
    /// <para>
    /// Candidates are presented directly from the term dictionary, based on
    /// Levenshtein distance. This is an alternative to <seealso cref="SpellChecker"/>
    /// if you are using an edit-distance-like metric such as Levenshtein
    /// or <seealso cref="JaroWinklerDistance"/>.
    /// </para>
    /// <para>
    /// A practical benefit of this spellchecker is that it requires no additional
    /// datastructures (neither in RAM nor on disk) to do its work.
    /// 
    /// </para>
    /// </summary>
    /// <seealso cref= LevenshteinAutomata </seealso>
    /// <seealso cref= FuzzyTermsEnum
    /// 
    /// @lucene.experimental </seealso>
    public class DirectSpellChecker
    {
        /// <summary>
        /// The default StringDistance, Damerau-Levenshtein distance implemented internally
        ///  via <seealso cref="LevenshteinAutomata"/>.
        ///  <para>
        ///  Note: this is the fastest distance metric, because Damerau-Levenshtein is used
        ///  to draw candidates from the term dictionary: this just re-uses the scoring.
        /// </para>
        /// </summary>
        public static readonly IStringDistance INTERNAL_LEVENSHTEIN = new LuceneLevenshteinDistance();

        /// <summary>
        /// maximum edit distance for candidate terms </summary>
        private int maxEdits = LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE;
        /// <summary>
        /// minimum prefix for candidate terms </summary>
        private int minPrefix = 1;
        /// <summary>
        /// maximum number of top-N inspections per suggestion </summary>
        private int maxInspections = 5;
        /// <summary>
        /// minimum accuracy for a term to match </summary>
        private float accuracy = SpellChecker.DEFAULT_ACCURACY;
        /// <summary>
        /// value in [0..1] (or absolute number >=1) representing the minimum
        /// number of documents (of the total) where a term should appear. 
        /// </summary>
        private float thresholdFrequency = 0f;
        /// <summary>
        /// minimum length of a query word to return suggestions </summary>
        private int minQueryLength = 4;
        /// <summary>
        /// value in [0..1] (or absolute number >=1) representing the maximum
        ///  number of documents (of the total) a query term can appear in to
        ///  be corrected. 
        /// </summary>
        private float maxQueryFrequency = 0.01f;
        /// <summary>
        /// true if the spellchecker should lowercase terms </summary>
        private bool lowerCaseTerms = true;
        /// <summary>
        /// the comparator to use </summary>
        private IComparer<SuggestWord> comparator = SuggestWordQueue.DEFAULT_COMPARATOR;
        /// <summary>
        /// the string distance to use </summary>
        private IStringDistance distance = INTERNAL_LEVENSHTEIN;

        /// <summary>
        /// Creates a DirectSpellChecker with default configuration values </summary>
        public DirectSpellChecker()
        {
        }

        /// <summary>
        /// Get the maximum number of Levenshtein edit-distances to draw
        ///  candidate terms from. 
        /// </summary>
        public virtual int MaxEdits
        {
            get
            {
                return maxEdits;
            }
            set
            {
                if (value < 1 || value > LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE)
                {
                    throw new NotSupportedException("Invalid maxEdits");
                }
                this.maxEdits = value;
            }
        }


        /// <summary>
        /// Get the minimal number of characters that must match exactly
        /// </summary>
        public virtual int MinPrefix
        {
            get
            {
                return minPrefix;
            }
            set
            {
                this.minPrefix = value;
            }
        }


        /// <summary>
        /// Get the maximum number of top-N inspections per suggestion
        /// </summary>
        public virtual int MaxInspections
        {
            get
            {
                return maxInspections;
            }
            set
            {
                this.maxInspections = value;
            }
        }


        /// <summary>
        /// Get the minimal accuracy from the StringDistance for a match
        /// </summary>
        public virtual float Accuracy
        {
            get
            {
                return accuracy;
            }
            set
            {
                this.accuracy = value;
            }
        }


        /// <summary>
        /// Get the minimal threshold of documents a term must appear for a match
        /// </summary>
        public virtual float ThresholdFrequency
        {
            get
            {
                return thresholdFrequency;
            }
            set
            {
                if (value >= 1f && value != (int)value)
                {
                    throw new System.ArgumentException("Fractional absolute document frequencies are not allowed");
                }
                this.thresholdFrequency = value;
            }
        }


        /// <summary>
        /// Get the minimum length of a query term needed to return suggestions </summary>
        public virtual int MinQueryLength
        {
            get
            {
                return minQueryLength;
            }
            set
            {
                this.minQueryLength = value;
            }
        }


        /// <summary>
        /// Get the maximum threshold of documents a query term can appear in order
        /// to provide suggestions.
        /// </summary>
        public virtual float MaxQueryFrequency
        {
            get
            {
                return maxQueryFrequency;
            }
            set
            {
                if (value >= 1f && value != (int)value)
                {
                    throw new System.ArgumentException("Fractional absolute document frequencies are not allowed");
                }
                this.maxQueryFrequency = value;
            }
        }


        /// <summary>
        /// true if the spellchecker should lowercase terms </summary>
        public virtual bool LowerCaseTerms
        {
            get
            {
                return lowerCaseTerms;
            }
            set
            {
                this.lowerCaseTerms = value;
            }
        }


        /// <summary>
        /// Get the current comparator in use.
        /// </summary>
        public virtual IComparer<SuggestWord> Comparator
        {
            get
            {
                return comparator;
            }
            set
            {
                this.comparator = value;
            }
        }


        /// <summary>
        /// Get the string distance metric in use.
        /// </summary>
        public virtual IStringDistance Distance
        {
            get
            {
                return distance;
            }
            set
            {
                this.distance = value;
            }
        }


        /// <summary>
        /// Calls {@link #suggestSimilar(Term, int, IndexReader, SuggestMode) 
        ///       suggestSimilar(term, numSug, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX)}
        /// </summary>
        public virtual SuggestWord[] SuggestSimilar(Term term, int numSug, IndexReader ir)
        {
            return SuggestSimilar(term, numSug, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
        }

        /// <summary>
        /// Calls {@link #suggestSimilar(Term, int, IndexReader, SuggestMode, float) 
        ///       suggestSimilar(term, numSug, ir, suggestMode, this.accuracy)}
        /// 
        /// </summary>
        public virtual SuggestWord[] SuggestSimilar(Term term, int numSug, IndexReader ir, SuggestMode suggestMode)
        {
            return SuggestSimilar(term, numSug, ir, suggestMode, this.accuracy);
        }

        /// <summary>
        /// Suggest similar words.
        /// 
        /// <para>Unlike <seealso cref="SpellChecker"/>, the similarity used to fetch the most
        /// relevant terms is an edit distance, therefore typically a low value
        /// for numSug will work very well.
        /// 
        /// </para>
        /// </summary>
        /// <param name="term"> Term you want to spell check on </param>
        /// <param name="numSug"> the maximum number of suggested words </param>
        /// <param name="ir"> IndexReader to find terms from </param>
        /// <param name="suggestMode"> specifies when to return suggested words </param>
        /// <param name="accuracy"> return only suggested words that match with this similarity </param>
        /// <returns> sorted list of the suggested words according to the comparator </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        public virtual SuggestWord[] SuggestSimilar(Term term, int numSug, IndexReader ir, SuggestMode suggestMode, float accuracy)
        {
            CharsRef spare = new CharsRef();
            string text = term.Text();
            if (minQueryLength > 0 && text.CodePointCount(0, text.Length) < minQueryLength)
            {
                return new SuggestWord[0];
            }

            if (lowerCaseTerms)
            {
                term = new Term(term.Field, text.ToLower()); // LUCENENET TODO: Culture - from the current thread? Or perhaps another overload where it can be passed?
            }

            int docfreq = ir.DocFreq(term);

            if (suggestMode == SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX && docfreq > 0)
            {
                return new SuggestWord[0];
            }

            int maxDoc = ir.MaxDoc;

            if (maxQueryFrequency >= 1f && docfreq > maxQueryFrequency)
            {
                return new SuggestWord[0];
            }
            else if (docfreq > (int)Math.Ceiling(maxQueryFrequency * maxDoc))
            {
                return new SuggestWord[0];
            }

            if (suggestMode != SuggestMode.SUGGEST_MORE_POPULAR)
            {
                docfreq = 0;
            }

            if (thresholdFrequency >= 1f)
            {
                docfreq = Math.Max(docfreq, (int)thresholdFrequency);
            }
            else if (thresholdFrequency > 0f)
            {
                docfreq = Math.Max(docfreq, (int)(thresholdFrequency * maxDoc) - 1);
            }

            IEnumerable<ScoreTerm> terms = null;
            int inspections = numSug * maxInspections;

            // try ed=1 first, in case we get lucky
            terms = SuggestSimilar(term, inspections, ir, docfreq, 1, accuracy, spare);
            if (maxEdits > 1 && terms.Count() < inspections)
            {
                var moreTerms = new HashSet<ScoreTerm>();
                moreTerms.AddAll(terms);
                moreTerms.AddAll(SuggestSimilar(term, inspections, ir, docfreq, maxEdits, accuracy, spare));
                terms = moreTerms;
            }

            // create the suggestword response, sort it, and trim it to size.

            var suggestions = new SuggestWord[terms.Count()];
            int index = suggestions.Length - 1;
            foreach (ScoreTerm s in terms)
            {
                SuggestWord suggestion = new SuggestWord();
                if (s.termAsString == null)
                {
                    UnicodeUtil.UTF8toUTF16(s.term, spare);
                    s.termAsString = spare.ToString();
                }
                suggestion.String = s.termAsString;
                suggestion.Score = s.score;
                suggestion.Freq = s.docfreq;
                suggestions[index--] = suggestion;
            }

            ArrayUtil.TimSort(suggestions, Collections.ReverseOrder(comparator));
            if (numSug < suggestions.Length)
            {
                SuggestWord[] trimmed = new SuggestWord[numSug];
                Array.Copy(suggestions, 0, trimmed, 0, numSug);
                suggestions = trimmed;
            }
            return suggestions;
        }

        /// <summary>
        /// Provide spelling corrections based on several parameters.
        /// </summary>
        /// <param name="term"> The term to suggest spelling corrections for </param>
        /// <param name="numSug"> The maximum number of spelling corrections </param>
        /// <param name="ir"> The index reader to fetch the candidate spelling corrections from </param>
        /// <param name="docfreq"> The minimum document frequency a potential suggestion need to have in order to be included </param>
        /// <param name="editDistance"> The maximum edit distance candidates are allowed to have </param>
        /// <param name="accuracy"> The minimum accuracy a suggested spelling correction needs to have in order to be included </param>
        /// <param name="spare"> a chars scratch </param>
        /// <returns> a collection of spelling corrections sorted by <code>ScoreTerm</code>'s natural order. </returns>
        /// <exception cref="IOException"> If I/O related errors occur </exception>
        protected internal virtual IEnumerable<ScoreTerm> SuggestSimilar(Term term, int numSug, IndexReader ir, int docfreq, int editDistance, float accuracy, CharsRef spare)
        {

            var atts = new AttributeSource();
            IMaxNonCompetitiveBoostAttribute maxBoostAtt = atts.AddAttribute<IMaxNonCompetitiveBoostAttribute>();
            Terms terms = MultiFields.GetTerms(ir, term.Field);
            if (terms == null)
            {
                return new List<ScoreTerm>();
            }
            FuzzyTermsEnum e = new FuzzyTermsEnum(terms, atts, term, editDistance, Math.Max(minPrefix, editDistance - 1), true);

            var stQueue = new Support.PriorityQueue<ScoreTerm>();

            BytesRef queryTerm = new BytesRef(term.Text());
            BytesRef candidateTerm;
            ScoreTerm st = new ScoreTerm();
            IBoostAttribute boostAtt = e.Attributes().AddAttribute<IBoostAttribute>();
            while ((candidateTerm = e.Next()) != null)
            {
                float boost = boostAtt.Boost;
                // ignore uncompetitive hits
                if (stQueue.Count >= numSug && boost <= stQueue.Peek().boost)
                {
                    continue;
                }

                // ignore exact match of the same term
                if (queryTerm.BytesEquals(candidateTerm))
                {
                    continue;
                }

                int df = e.DocFreq();

                // check docFreq if required
                if (df <= docfreq)
                {
                    continue;
                }

                float score;
                string termAsString;
                if (distance == INTERNAL_LEVENSHTEIN)
                {
                    // delay creating strings until the end
                    termAsString = null;
                    // undo FuzzyTermsEnum's scale factor for a real scaled lev score
                    score = boost / e.ScaleFactor + e.MinSimilarity;
                }
                else
                {
                    UnicodeUtil.UTF8toUTF16(candidateTerm, spare);
                    termAsString = spare.ToString();
                    score = distance.GetDistance(term.Text(), termAsString);
                }

                if (score < accuracy)
                {
                    continue;
                }

                // add new entry in PQ
                st.term = BytesRef.DeepCopyOf(candidateTerm);
                st.boost = boost;
                st.docfreq = df;
                st.termAsString = termAsString;
                st.score = score;
                stQueue.Offer(st);
                // possibly drop entries from queue
                st = (stQueue.Count > numSug) ? stQueue.Poll() : new ScoreTerm();
                maxBoostAtt.MaxNonCompetitiveBoost = (stQueue.Count >= numSug) ? stQueue.Peek().boost : float.NegativeInfinity;
            }

            return stQueue;
        }

        /// <summary>
        /// Holds a spelling correction for internal usage inside <seealso cref="DirectSpellChecker"/>.
        /// </summary>
        protected internal class ScoreTerm : IComparable<ScoreTerm>
        {

            /// <summary>
            /// The actual spellcheck correction.
            /// </summary>
            public BytesRef term;

            /// <summary>
            /// The boost representing the similarity from the FuzzyTermsEnum (internal similarity score)
            /// </summary>
            public float boost;

            /// <summary>
            /// The df of the spellcheck correction.
            /// </summary>
            public int docfreq;

            /// <summary>
            /// The spellcheck correction represented as string, can be <code>null</code>.
            /// </summary>
            public string termAsString;

            /// <summary>
            /// The similarity score.
            /// </summary>
            public float score;

            /// <summary>
            /// Constructor.
            /// </summary>
            public ScoreTerm()
            {
            }

            public virtual int CompareTo(ScoreTerm other)
            {
                if (term.BytesEquals(other.term))
                {
                    return 0; // consistent with equals
                }
                if (this.boost == other.boost)
                {
                    return other.term.CompareTo(this.term);
                }
                else
                {
                    return this.boost.CompareTo(other.boost);
                }
            }

            public override int GetHashCode()
            {
                const int prime = 31;
                int result = 1;
                result = prime * result + ((term == null) ? 0 : term.GetHashCode());
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
                ScoreTerm other = (ScoreTerm)obj;
                if (term == null)
                {
                    if (other.term != null)
                    {
                        return false;
                    }
                }
                else if (!term.BytesEquals(other.term))
                {
                    return false;
                }
                return true;
            }
        }
    }
}