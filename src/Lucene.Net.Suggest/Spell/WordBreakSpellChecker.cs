using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Support;

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
    /// <para>
    /// A spell checker whose sole function is to offer suggestions by combining
    /// multiple terms into one word and/or breaking terms into multiple words.
    /// </para>
    /// </summary>
    public class WordBreakSpellChecker
    {
        private int minSuggestionFrequency = 1;
        private int minBreakWordLength = 1;
        private int maxCombineWordLength = 20;
        private int maxChanges = 1;
        private int maxEvaluations = 1000;

        /// <summary>
        /// Term that can be used to prohibit adjacent terms from being combined </summary>
        public static readonly Term SEPARATOR_TERM = new Term("", "");

        /// <summary>
        /// Creates a new spellchecker with default configuration values </summary>
        /// <seealso cref= #setMaxChanges(int) </seealso>
        /// <seealso cref= #setMaxCombineWordLength(int) </seealso>
        /// <seealso cref= #setMaxEvaluations(int) </seealso>
        /// <seealso cref= #setMinBreakWordLength(int) </seealso>
        /// <seealso cref= #setMinSuggestionFrequency(int) </seealso>
        public WordBreakSpellChecker()
        {
        }

        /// <summary>
        /// <para>
        /// Determines the order to list word break suggestions
        /// </para>
        /// </summary>
        public enum BreakSuggestionSortMethod
        {
            /// <summary>
            /// <para>
            /// Sort by Number of word breaks, then by the Sum of all the component
            /// term's frequencies
            /// </para>
            /// </summary>
            NUM_CHANGES_THEN_SUMMED_FREQUENCY,
            /// <summary>
            /// <para>
            /// Sort by Number of word breaks, then by the Maximum of all the component
            /// term's frequencies
            /// </para>
            /// </summary>
            NUM_CHANGES_THEN_MAX_FREQUENCY
        }

        /// <summary>
        /// <para>
        /// Generate suggestions by breaking the passed-in term into multiple words.
        /// The scores returned are equal to the number of word breaks needed so a
        /// lower score is generally preferred over a higher score.
        /// </para>
        /// </summary>
        /// <param name="suggestMode">
        ///          - default = <seealso cref="SuggestMode#SUGGEST_WHEN_NOT_IN_INDEX"/> </param>
        /// <param name="sortMethod">
        ///          - default =
        ///          <seealso cref="BreakSuggestionSortMethod#NUM_CHANGES_THEN_MAX_FREQUENCY"/> </param>
        /// <returns> one or more arrays of words formed by breaking up the original term </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        public virtual SuggestWord[][] SuggestWordBreaks(Term term, int maxSuggestions, IndexReader ir, SuggestMode suggestMode, BreakSuggestionSortMethod sortMethod)
        {
            if (maxSuggestions < 1)
            {
                return new SuggestWord[0][];
            }
            if (suggestMode == null)
            {
                suggestMode = SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX;
            }
            if (sortMethod == null)
            {
                sortMethod = BreakSuggestionSortMethod.NUM_CHANGES_THEN_MAX_FREQUENCY;
            }

            int queueInitialCapacity = maxSuggestions > 10 ? 10 : maxSuggestions;
            IComparer<SuggestWordArrayWrapper> queueComparator = sortMethod == BreakSuggestionSortMethod.NUM_CHANGES_THEN_MAX_FREQUENCY ? new LengthThenMaxFreqComparator(this) : new LengthThenSumFreqComparator(this);
            LinkedList<SuggestWordArrayWrapper> suggestions = new PriorityQueue<SuggestWordArrayWrapper>(queueInitialCapacity, queueComparator);

            int origFreq = ir.DocFreq(term);
            if (origFreq > 0 && suggestMode == SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX)
            {
                return new SuggestWord[0][];
            }

            int useMinSuggestionFrequency = minSuggestionFrequency;
            if (suggestMode == SuggestMode.SUGGEST_MORE_POPULAR)
            {
                useMinSuggestionFrequency = (origFreq == 0 ? 1 : origFreq);
            }

            GenerateBreakUpSuggestions(term, ir, 1, maxSuggestions, useMinSuggestionFrequency, new SuggestWord[0], suggestions, 0, sortMethod);

            SuggestWord[][] suggestionArray = new SuggestWord[suggestions.Count][];
            for (int i = suggestions.Count - 1; i >= 0; i--)
            {
                suggestionArray[i] = suggestions.RemoveFirst().SuggestWords;
            }

            return suggestionArray;
        }

        /// <summary>
        /// <para>
        /// Generate suggestions by combining one or more of the passed-in terms into
        /// single words. The returned <seealso cref="CombineSuggestion"/> contains both a
        /// <seealso cref="SuggestWord"/> and also an array detailing which passed-in terms were
        /// involved in creating this combination. The scores returned are equal to the
        /// number of word combinations needed, also one less than the length of the
        /// array <seealso cref="CombineSuggestion#originalTermIndexes"/>. Generally, a
        /// suggestion with a lower score is preferred over a higher score.
        /// </para>
        /// <para>
        /// To prevent two adjacent terms from being combined (for instance, if one is
        /// mandatory and the other is prohibited), separate the two terms with
        /// <seealso cref="WordBreakSpellChecker#SEPARATOR_TERM"/>
        /// </para>
        /// <para>
        /// When suggestMode equals <seealso cref="SuggestMode#SUGGEST_WHEN_NOT_IN_INDEX"/>, each
        /// suggestion will include at least one term not in the index.
        /// </para>
        /// <para>
        /// When suggestMode equals <seealso cref="SuggestMode#SUGGEST_MORE_POPULAR"/>, each
        /// suggestion will have the same, or better frequency than the most-popular
        /// included term.
        /// </para>
        /// </summary>
        /// <returns> an array of words generated by combining original terms </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        public virtual CombineSuggestion[] SuggestWordCombinations(Term[] terms, int maxSuggestions, IndexReader ir, SuggestMode suggestMode)
        {
            if (maxSuggestions < 1)
            {
                return new CombineSuggestion[0];
            }

            int[] origFreqs = null;
            if (suggestMode != SuggestMode.SUGGEST_ALWAYS)
            {
                origFreqs = new int[terms.Length];
                for (int i = 0; i < terms.Length; i++)
                {
                    origFreqs[i] = ir.DocFreq(terms[i]);
                }
            }

            int queueInitialCapacity = maxSuggestions > 10 ? 10 : maxSuggestions;
            IComparer<CombineSuggestionWrapper> queueComparator = new CombinationsThenFreqComparator(this);
            LinkedList<CombineSuggestionWrapper> suggestions = new PriorityQueue<CombineSuggestionWrapper>(queueInitialCapacity, queueComparator);

            int thisTimeEvaluations = 0;
            for (int i = 0; i < terms.Length - 1; i++)
            {
                if (terms[i].Equals(SEPARATOR_TERM))
                {
                    continue;
                }
                string leftTermText = terms[i].Text();
                int leftTermLength = leftTermText.CodePointCount(0, leftTermText.Length);
                if (leftTermLength > maxCombineWordLength)
                {
                    continue;
                }
                int maxFreq = 0;
                int minFreq = int.MaxValue;
                if (origFreqs != null)
                {
                    maxFreq = origFreqs[i];
                    minFreq = origFreqs[i];
                }
                string combinedTermText = leftTermText;
                int combinedLength = leftTermLength;
                for (int j = i + 1; j < terms.Length && j - i <= maxChanges; j++)
                {
                    if (terms[j].Equals(SEPARATOR_TERM))
                    {
                        break;
                    }
                    string rightTermText = terms[j].Text();
                    int rightTermLength = rightTermText.CodePointCount(0, rightTermText.Length);
                    combinedTermText += rightTermText;
                    combinedLength += rightTermLength;
                    if (combinedLength > maxCombineWordLength)
                    {
                        break;
                    }

                    if (origFreqs != null)
                    {
                        maxFreq = Math.Max(maxFreq, origFreqs[j]);
                        minFreq = Math.Min(minFreq, origFreqs[j]);
                    }

                    Term combinedTerm = new Term(terms[0].Field(), combinedTermText);
                    int combinedTermFreq = ir.DocFreq(combinedTerm);

                    if (suggestMode != SuggestMode.SUGGEST_MORE_POPULAR || combinedTermFreq >= maxFreq)
                    {
                        if (suggestMode != SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX || minFreq == 0)
                        {
                            if (combinedTermFreq >= minSuggestionFrequency)
                            {
                                int[] origIndexes = new int[j - i + 1];
                                origIndexes[0] = i;
                                for (int k = 1; k < origIndexes.Length; k++)
                                {
                                    origIndexes[k] = i + k;
                                }
                                SuggestWord word = new SuggestWord();
                                word.freq = combinedTermFreq;
                                word.score = origIndexes.Length - 1;
                                word.@string = combinedTerm.Text();
                                CombineSuggestionWrapper suggestion = new CombineSuggestionWrapper(this, new CombineSuggestion(word, origIndexes), (origIndexes.Length - 1));
                                suggestions.AddLast(suggestion);
                                if (suggestions.Count > maxSuggestions)
                                {
                                    suggestions.RemoveFirst();
                                }
                            }
                        }
                    }
                    thisTimeEvaluations++;
                    if (thisTimeEvaluations == maxEvaluations)
                    {
                        break;
                    }
                }
            }
            CombineSuggestion[] combineSuggestions = new CombineSuggestion[suggestions.Count];
            for (int i = suggestions.Count - 1; i >= 0; i--)
            {
                combineSuggestions[i] = suggestions.RemoveFirst().CombineSuggestion;
            }
            return combineSuggestions;
        }

        private int GenerateBreakUpSuggestions(Term term, IndexReader ir, int numberBreaks, int maxSuggestions, int useMinSuggestionFrequency, SuggestWord[] prefix, LinkedList<SuggestWordArrayWrapper> suggestions, int totalEvaluations, BreakSuggestionSortMethod sortMethod)
        {
            string termText = term.Text();
            int termLength = termText.CodePointCount(0, termText.Length);
            int useMinBreakWordLength = minBreakWordLength;
            if (useMinBreakWordLength < 1)
            {
                useMinBreakWordLength = 1;
            }
            if (termLength < (useMinBreakWordLength * 2))
            {
                return 0;
            }

            int thisTimeEvaluations = 0;
            for (int i = useMinBreakWordLength; i <= (termLength - useMinBreakWordLength); i++)
            {
                int end = termText.OffsetByCodePoints(0, i);
                string leftText = termText.Substring(0, end);
                string rightText = termText.Substring(end);
                SuggestWord leftWord = GenerateSuggestWord(ir, term.Field(), leftText);

                if (leftWord.freq >= useMinSuggestionFrequency)
                {
                    SuggestWord rightWord = GenerateSuggestWord(ir, term.Field(), rightText);
                    if (rightWord.freq >= useMinSuggestionFrequency)
                    {
                        SuggestWordArrayWrapper suggestion = new SuggestWordArrayWrapper(this, NewSuggestion(prefix, leftWord, rightWord));
                        suggestions.AddLast(suggestion);
                        if (suggestions.Count > maxSuggestions)
                        {
                            suggestions.RemoveFirst();
                        }
                    }
                    int newNumberBreaks = numberBreaks + 1;
                    if (newNumberBreaks <= maxChanges)
                    {
                        int evaluations = GenerateBreakUpSuggestions(new Term(term.Field(), rightWord.@string), ir, newNumberBreaks, maxSuggestions, useMinSuggestionFrequency, NewPrefix(prefix, leftWord), suggestions, totalEvaluations, sortMethod);
                        totalEvaluations += evaluations;
                    }
                }

                thisTimeEvaluations++;
                totalEvaluations++;
                if (totalEvaluations >= maxEvaluations)
                {
                    break;
                }
            }
            return thisTimeEvaluations;
        }

        private static SuggestWord[] NewPrefix(SuggestWord[] oldPrefix, SuggestWord append)
        {
            SuggestWord[] newPrefix = new SuggestWord[oldPrefix.Length + 1];
            Array.Copy(oldPrefix, 0, newPrefix, 0, oldPrefix.Length);
            newPrefix[newPrefix.Length - 1] = append;
            return newPrefix;
        }

        private static SuggestWord[] NewSuggestion(SuggestWord[] prefix, SuggestWord append1, SuggestWord append2)
        {
            SuggestWord[] newSuggestion = new SuggestWord[prefix.Length + 2];
            int score = prefix.Length + 1;
            for (int i = 0; i < prefix.Length; i++)
            {
                SuggestWord word = new SuggestWord();
                word.@string = prefix[i].@string;
                word.freq = prefix[i].freq;
                word.score = score;
                newSuggestion[i] = word;
            }
            append1.score = score;
            append2.score = score;
            newSuggestion[newSuggestion.Length - 2] = append1;
            newSuggestion[newSuggestion.Length - 1] = append2;
            return newSuggestion;
        }

        private SuggestWord GenerateSuggestWord(IndexReader ir, string fieldname, string text)
        {
            Term term = new Term(fieldname, text);
            int freq = ir.DocFreq(term);
            SuggestWord word = new SuggestWord();
            word.freq = freq;
            word.score = 1;
            word.@string = text;
            return word;
        }

        /// <summary>
        /// Returns the minimum frequency a term must have
        /// to be part of a suggestion. </summary>
        /// <seealso cref= #setMinSuggestionFrequency(int) </seealso>
        public virtual int MinSuggestionFrequency
        {
            get
            {
                return minSuggestionFrequency;
            }
            set
            {
                this.minSuggestionFrequency = value;
            }
        }

        /// <summary>
        /// Returns the maximum length of a combined suggestion </summary>
        /// <seealso cref= #setMaxCombineWordLength(int) </seealso>
        public virtual int MaxCombineWordLength
        {
            get
            {
                return maxCombineWordLength;
            }
            set
            {
                this.maxCombineWordLength = value;
            }
        }

        /// <summary>
        /// Returns the minimum size of a broken word </summary>
        /// <seealso cref= #setMinBreakWordLength(int) </seealso>
        public virtual int MinBreakWordLength
        {
            get
            {
                return minBreakWordLength;
            }
            set
            {
                this.minBreakWordLength = value;
            }
        }

        /// <summary>
        /// Returns the maximum number of changes to perform on the input </summary>
        /// <seealso cref= #setMaxChanges(int) </seealso>
        public virtual int MaxChanges
        {
            get
            {
                return maxChanges;
            }
            set
            {
                this.maxChanges = value;
            }
        }

        /// <summary>
        /// Returns the maximum number of word combinations to evaluate. </summary>
        /// <seealso cref= #setMaxEvaluations(int) </seealso>
        public virtual int MaxEvaluations
        {
            get
            {
                return maxEvaluations;
            }
            set
            {
                this.maxEvaluations = value;
            }
        }

        private sealed class LengthThenMaxFreqComparator : IComparer<SuggestWordArrayWrapper>
        {
            private readonly WordBreakSpellChecker outerInstance;

            public LengthThenMaxFreqComparator(WordBreakSpellChecker outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public int Compare(SuggestWordArrayWrapper o1, SuggestWordArrayWrapper o2)
            {
                if (o1.suggestWords.Length != o2.suggestWords.Length)
                {
                    return o2.suggestWords.Length - o1.suggestWords.Length;
                }
                if (o1.freqMax != o2.freqMax)
                {
                    return o1.freqMax - o2.freqMax;
                }
                return 0;
            }
        }

        private sealed class LengthThenSumFreqComparator : IComparer<SuggestWordArrayWrapper>
        {
            private readonly WordBreakSpellChecker outerInstance;

            public LengthThenSumFreqComparator(WordBreakSpellChecker outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public int Compare(SuggestWordArrayWrapper o1, SuggestWordArrayWrapper o2)
            {
                if (o1.suggestWords.Length != o2.suggestWords.Length)
                {
                    return o2.suggestWords.Length - o1.suggestWords.Length;
                }
                if (o1.freqSum != o2.freqSum)
                {
                    return o1.freqSum - o2.freqSum;
                }
                return 0;
            }
        }

        private sealed class CombinationsThenFreqComparator : IComparer<CombineSuggestionWrapper>
        {
            private readonly WordBreakSpellChecker outerInstance;

            public CombinationsThenFreqComparator(WordBreakSpellChecker outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public int Compare(CombineSuggestionWrapper o1, CombineSuggestionWrapper o2)
            {
                if (o1.numCombinations != o2.numCombinations)
                {
                    return o2.numCombinations - o1.numCombinations;
                }
                if (o1.combineSuggestion.suggestion.freq != o2.combineSuggestion.suggestion.freq)
                {
                    return o1.combineSuggestion.suggestion.freq - o2.combineSuggestion.suggestion.freq;
                }
                return 0;
            }
        }

        private class SuggestWordArrayWrapper
        {
            private readonly WordBreakSpellChecker outerInstance;

            internal readonly SuggestWord[] suggestWords;
            internal readonly int freqMax;
            internal readonly int freqSum;

            internal SuggestWordArrayWrapper(WordBreakSpellChecker outerInstance, SuggestWord[] suggestWords)
            {
                this.outerInstance = outerInstance;
                this.suggestWords = suggestWords;
                int aFreqSum = 0;
                int aFreqMax = 0;
                foreach (SuggestWord sw in suggestWords)
                {
                    aFreqSum += sw.freq;
                    aFreqMax = Math.Max(aFreqMax, sw.freq);
                }
                this.freqSum = aFreqSum;
                this.freqMax = aFreqMax;
            }
        }

        private class CombineSuggestionWrapper
        {
            private readonly WordBreakSpellChecker outerInstance;

            internal readonly CombineSuggestion combineSuggestion;
            internal readonly int numCombinations;

            internal CombineSuggestionWrapper(WordBreakSpellChecker outerInstance, CombineSuggestion combineSuggestion, int numCombinations)
            {
                this.outerInstance = outerInstance;
                this.combineSuggestion = combineSuggestion;
                this.numCombinations = numCombinations;
            }
        }
    }

}