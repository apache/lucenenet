using J2N;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using JCG = J2N.Collections.Generic;

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
    /// Levenshtein distance. This is an alternative to <see cref="SpellChecker"/>
    /// if you are using an edit-distance-like metric such as Levenshtein
    /// or <see cref="JaroWinklerDistance"/>.
    /// </para>
    /// <para>
    /// A practical benefit of this spellchecker is that it requires no additional
    /// datastructures (neither in RAM nor on disk) to do its work.
    /// 
    /// </para>
    /// </summary>
    /// <seealso cref="LevenshteinAutomata"/>
    /// <seealso cref="FuzzyTermsEnum"/>
    /// 
    /// @lucene.experimental
    public class DirectSpellChecker
    {
        /// <summary>
        /// The default StringDistance, Damerau-Levenshtein distance implemented internally
        ///  via <see cref="LevenshteinAutomata"/>.
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
        /// the comparer to use </summary>
        private IComparer<SuggestWord> comparer = SuggestWordQueue.DEFAULT_COMPARER;
        /// <summary>
        /// the string distance to use </summary>
        private IStringDistance distance = INTERNAL_LEVENSHTEIN;

        /// <summary>
        /// The culture to use for lowercasing terms.
        /// </summary>
        private CultureInfo lowerCaseTermsCulture = null; // LUCENENET specific

        /// <summary>
        /// Creates a DirectSpellChecker with default configuration values 
        /// </summary>
        public DirectSpellChecker()
        {
        }

        /// <summary>
        /// Gets or sets the maximum number of Levenshtein edit-distances to draw
        /// candidate terms from.This value can be 1 or 2. The default is 2.
        /// 
        /// Note: a large number of spelling errors occur with an edit distance
        /// of 1, by setting this value to 1 you can increase both performance
        /// and precision at the cost of recall.
        /// </summary>
        public virtual int MaxEdits
        {
            get => maxEdits;
            set
            {
                if (value < 1 || value > LevenshteinAutomata.MAXIMUM_SUPPORTED_DISTANCE)
                {
                    throw UnsupportedOperationException.Create("Invalid maxEdits");
                }
                maxEdits = value;
            }
        }


        /// <summary>
        /// Gets or sets the minimal number of characters that must match exactly.
        /// 
        /// This can improve both performance and accuracy of results, 
        /// as misspellings are commonly not the first character.
        /// </summary>
        public virtual int MinPrefix
        {
            get => minPrefix;
            set => minPrefix = value;
        }


        /// <summary>
        /// Get the maximum number of top-N inspections per suggestion.
        /// 
        /// Increasing this number can improve the accuracy of results, at the cost 
        /// of performance.
        /// </summary>
        public virtual int MaxInspections
        {
            get => maxInspections;
            set => maxInspections = value;
        }


        /// <summary>
        /// Gets or sets the minimal accuracy required (default: 0.5f) from a StringDistance 
        /// for a suggestion match.
        /// </summary>
        public virtual float Accuracy
        {
            get => accuracy;
            set => accuracy = value;
        }


        /// <summary>
        /// Gets or sets the minimal threshold of documents a term must appear for a match.
        /// <para/>
        /// This can improve quality by only suggesting high-frequency terms. Note that
        /// very high values might decrease performance slightly, by forcing the spellchecker
        /// to draw more candidates from the term dictionary, but a practical value such
        /// as <c>1</c> can be very useful towards improving quality.
        /// <para/>
        /// This can be specified as a relative percentage of documents such as 0.5f,
        /// or it can be specified as an absolute whole document frequency, such as 4f.
        /// Absolute document frequencies may not be fractional.
        /// </summary>
        public virtual float ThresholdFrequency
        {
            get => thresholdFrequency;
            set
            {
                if (value >= 1f && value != (int)value)
                {
                    throw new ArgumentException("Fractional absolute document frequencies are not allowed");
                }
                thresholdFrequency = value;
            }
        }


        /// <summary>
        /// Gets or sets the minimum length of a query term (default: 4) needed to return suggestions.
        /// <para/>
        /// Very short query terms will often cause only bad suggestions with any distance
        /// metric.
        /// </summary>
        public virtual int MinQueryLength
        {
            get => minQueryLength;
            set => minQueryLength = value;
        }


        /// <summary>
        /// Gets or sets the maximum threshold (default: 0.01f) of documents a query term can 
        /// appear in order to provide suggestions.
        /// <para/>
        /// Very high-frequency terms are typically spelled correctly. Additionally,
        /// this can increase performance as it will do no work for the common case
        /// of correctly-spelled input terms.
        /// <para/>
        /// This can be specified as a relative percentage of documents such as 0.5f,
        /// or it can be specified as an absolute whole document frequency, such as 4f.
        /// Absolute document frequencies may not be fractional.
        /// </summary>
        public virtual float MaxQueryFrequency
        {
            get => maxQueryFrequency;
            set
            {
                if (value >= 1f && value != (int)value)
                {
                    throw new ArgumentException("Fractional absolute document frequencies are not allowed");
                }
                maxQueryFrequency = value;
            }
        }


        /// <summary>
        /// True if the spellchecker should lowercase terms (default: true)
        /// <para/>
        /// This is a convenience method, if your index field has more complicated
        /// analysis (such as StandardTokenizer removing punctuation), its probably
        /// better to turn this off, and instead run your query terms through your
        /// Analyzer first.
        /// <para/>
        /// If this option is not on, case differences count as an edit!
        /// </summary>
        public virtual bool LowerCaseTerms
        {
            get => lowerCaseTerms;
            set => lowerCaseTerms = value;
        }

        /// <summary>
        /// Gets or sets the culture to use for lowercasing terms.
        /// Set to <c>null</c> (the default) to use <see cref="CultureInfo.CurrentCulture"/>.
        /// </summary>
        public virtual CultureInfo LowerCaseTermsCulture // LUCENENET specific
        {
            get => lowerCaseTermsCulture ?? CultureInfo.CurrentCulture;
            set => lowerCaseTermsCulture = value;
        }

        /// <summary>
        /// Gets or sets the comparer for sorting suggestions.
        /// The default is <see cref="SuggestWordQueue.DEFAULT_COMPARER"/> 
        /// </summary>
        public virtual IComparer<SuggestWord> Comparer
        {
            get => comparer;
            set => comparer = value;
        }


        /// <summary>
        /// Gets or sets the string distance metric.
        /// The default is <see cref="INTERNAL_LEVENSHTEIN"/>.
        /// <para/>
        /// Note: because this spellchecker draws its candidates from the term
        /// dictionary using Damerau-Levenshtein, it works best with an edit-distance-like
        /// string metric. If you use a different metric than the default,
        /// you might want to consider increasing <see cref="MaxInspections"/>
        /// to draw more candidates for your metric to rank. 
        /// </summary>
        public virtual IStringDistance Distance
        {
            get => distance;
            set => distance = value;
        }


        /// <summary>
        /// Calls <see cref="SuggestSimilar(Term, int, IndexReader, SuggestMode)"/>
        ///       SuggestSimilar(term, numSug, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX)
        /// </summary>
        public virtual SuggestWord[] SuggestSimilar(Term term, int numSug, IndexReader ir)
        {
            return SuggestSimilar(term, numSug, ir, SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX);
        }

        /// <summary>
        /// Calls <see cref="SuggestSimilar(Term, int, IndexReader, SuggestMode, float)"/>
        ///       SuggestSimilar(term, numSug, ir, suggestMode, this.accuracy)
        /// </summary>
        public virtual SuggestWord[] SuggestSimilar(Term term, int numSug, IndexReader ir, SuggestMode suggestMode)
        {
            return SuggestSimilar(term, numSug, ir, suggestMode, this.accuracy);
        }

        /// <summary>
        /// Suggest similar words.
        /// 
        /// <para>
        /// Unlike <see cref="SpellChecker"/>, the similarity used to fetch the most
        /// relevant terms is an edit distance, therefore typically a low value
        /// for numSug will work very well.
        /// </para>
        /// </summary>
        /// <param name="term"> Term you want to spell check on </param>
        /// <param name="numSug"> the maximum number of suggested words </param>
        /// <param name="ir"> IndexReader to find terms from </param>
        /// <param name="suggestMode"> specifies when to return suggested words </param>
        /// <param name="accuracy"> return only suggested words that match with this similarity </param>
        /// <returns> sorted list of the suggested words according to the comparer </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        public virtual SuggestWord[] SuggestSimilar(Term term, int numSug, IndexReader ir, 
            SuggestMode suggestMode, float accuracy)
        {
            CharsRef spare = new CharsRef();
            string text = term.Text;
            if (minQueryLength > 0 && text.CodePointCount(0, text.Length) < minQueryLength)
            {
                return Arrays.Empty<SuggestWord>();
            }

            if (lowerCaseTerms)
            {
                term = new Term(term.Field, LowerCaseTermsCulture.TextInfo.ToLower(text));
            }

            int docfreq = ir.DocFreq(term);

            if (suggestMode == SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX && docfreq > 0)
            {
                return Arrays.Empty<SuggestWord>();
            }

            int maxDoc = ir.MaxDoc;

            if (maxQueryFrequency >= 1f && docfreq > maxQueryFrequency)
            {
                return Arrays.Empty<SuggestWord>();
            }
            else if (docfreq > (int)Math.Ceiling(maxQueryFrequency * maxDoc))
            {
                return Arrays.Empty<SuggestWord>();
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

            ICollection<ScoreTerm> terms; // LUCENENET: IDE0059: Remove unnecessary value assignment
            int inspections = numSug * maxInspections;

            // try ed=1 first, in case we get lucky
            terms = SuggestSimilar(term, inspections, ir, docfreq, 1, accuracy, spare);
            if (maxEdits > 1 && terms.Count < inspections)
            {
                var moreTerms = new JCG.HashSet<ScoreTerm>();
                moreTerms.UnionWith(terms);
                moreTerms.UnionWith(SuggestSimilar(term, inspections, ir, docfreq, maxEdits, accuracy, spare));
                terms = moreTerms;
            }

            // create the suggestword response, sort it, and trim it to size.

            var suggestions = new SuggestWord[terms.Count];
            int index = suggestions.Length - 1;
            foreach (ScoreTerm s in terms)
            {
                SuggestWord suggestion = new SuggestWord();
                if (s.TermAsString is null)
                {
                    UnicodeUtil.UTF8toUTF16(s.Term, spare);
                    s.TermAsString = spare.ToString();
                }
                suggestion.String = s.TermAsString;
                suggestion.Score = s.Score;
                suggestion.Freq = s.Docfreq;
                suggestions[index--] = suggestion;
            }

            ArrayUtil.TimSort(suggestions, Collections.ReverseOrder(comparer));
            if (numSug < suggestions.Length)
            {
                SuggestWord[] trimmed = new SuggestWord[numSug];
                Arrays.Copy(suggestions, 0, trimmed, 0, numSug);
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
        protected internal virtual ICollection<ScoreTerm> SuggestSimilar(Term term, int numSug, IndexReader ir, 
            int docfreq, int editDistance, float accuracy, CharsRef spare)
        {

            var atts = new AttributeSource();
            IMaxNonCompetitiveBoostAttribute maxBoostAtt = atts.AddAttribute<IMaxNonCompetitiveBoostAttribute>();
            Terms terms = MultiFields.GetTerms(ir, term.Field);
            if (terms is null)
            {
                return Collections.EmptyList<ScoreTerm>();
            }
            FuzzyTermsEnum e = new FuzzyTermsEnum(terms, atts, term, editDistance, Math.Max(minPrefix, editDistance - 1), true);

            var stQueue = new JCG.PriorityQueue<ScoreTerm>();

            BytesRef queryTerm = new BytesRef(term.Text);
            BytesRef candidateTerm;
            ScoreTerm st = new ScoreTerm();
            IBoostAttribute boostAtt = e.Attributes.AddAttribute<IBoostAttribute>();
            while (e.MoveNext())
            {
                candidateTerm = e.Term;
                float boost = boostAtt.Boost;
                // ignore uncompetitive hits
                if (stQueue.Count >= numSug && boost <= stQueue.Peek().Boost)
                {
                    continue;
                }

                // ignore exact match of the same term
                if (queryTerm.BytesEquals(candidateTerm))
                {
                    continue;
                }

                int df = e.DocFreq;

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
                    score = distance.GetDistance(term.Text, termAsString);
                }

                if (score < accuracy)
                {
                    continue;
                }

                // add new entry in PQ
                st.Term = BytesRef.DeepCopyOf(candidateTerm);
                st.Boost = boost;
                st.Docfreq = df;
                st.TermAsString = termAsString;
                st.Score = score;
                stQueue.Enqueue(st);
                // possibly drop entries from queue
                st = (stQueue.Count > numSug) ? stQueue.Dequeue() : new ScoreTerm();
                maxBoostAtt.MaxNonCompetitiveBoost = (stQueue.Count >= numSug) ? stQueue.Peek().Boost : float.NegativeInfinity;
            }

            return stQueue;
        }

        /// <summary>
        /// Holds a spelling correction for internal usage inside <see cref="DirectSpellChecker"/>.
        /// </summary>
        protected internal class ScoreTerm : IComparable<ScoreTerm>
        {

            /// <summary>
            /// The actual spellcheck correction.
            /// </summary>
            public BytesRef Term { get; set; }

            /// <summary>
            /// The boost representing the similarity from the FuzzyTermsEnum (internal similarity score)
            /// </summary>
            public float Boost { get; set; }

            /// <summary>
            /// The df of the spellcheck correction.
            /// </summary>
            public int Docfreq { get; set; }

            /// <summary>
            /// The spellcheck correction represented as string, can be <code>null</code>.
            /// </summary>
            public string TermAsString { get; set; }

            /// <summary>
            /// The similarity score.
            /// </summary>
            public float Score { get; set; }

            /// <summary>
            /// Constructor.
            /// </summary>
            public ScoreTerm()
            {
            }

            public virtual int CompareTo(ScoreTerm other)
            {
                if (Term.BytesEquals(other.Term))
                {
                    return 0; // consistent with equals
                }
                if (this.Boost == other.Boost)
                {
                    return other.Term.CompareTo(this.Term);
                }
                else
                {
                    return this.Boost.CompareTo(other.Boost);
                }
            }

            public override int GetHashCode()
            {
                const int prime = 31;
                int result = 1;
                result = prime * result + ((Term is null) ? 0 : Term.GetHashCode());
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
                ScoreTerm other = (ScoreTerm)obj;
                if (Term is null)
                {
                    if (other.Term != null)
                    {
                        return false;
                    }
                }
                else if (!Term.BytesEquals(other.Term))
                {
                    return false;
                }
                return true;
            }
        }
    }
}