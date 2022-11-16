using J2N;
using Lucene.Net.Index;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using JCG = J2N.Collections.Generic;
using WritableArrayAttribute = Lucene.Net.Support.WritableArrayAttribute;

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
        /// <seealso cref="MaxChanges"/>
        /// <seealso cref="MaxCombineWordLength"/>
        /// <seealso cref="MaxEvaluations"/>
        /// <seealso cref="MinBreakWordLength"/>
        /// <seealso cref="MinSuggestionFrequency"/>
        public WordBreakSpellChecker()
        {
        }

        /// <summary>
        /// Determines the order to list word break suggestions
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
        /// Generate suggestions by breaking the passed-in term into multiple words.
        /// The scores returned are equal to the number of word breaks needed so a
        /// lower score is generally preferred over a higher score.
        /// </summary>
        /// <param name="suggestMode">
        ///          - default = <see cref="SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX"/> </param>
        /// <param name="sortMethod">
        ///          - default = <see cref="BreakSuggestionSortMethod.NUM_CHANGES_THEN_MAX_FREQUENCY"/> </param>
        /// <returns> one or more arrays of words formed by breaking up the original term </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        public virtual SuggestWord[][] SuggestWordBreaks(Term term, int maxSuggestions, IndexReader ir, 
            SuggestMode suggestMode = SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX, 
            BreakSuggestionSortMethod sortMethod = BreakSuggestionSortMethod.NUM_CHANGES_THEN_MAX_FREQUENCY)
        {
            if (maxSuggestions < 1)
            {
                return Arrays.Empty<SuggestWord[]>();
            }

            int queueInitialCapacity = maxSuggestions > 10 ? 10 : maxSuggestions;
            IComparer<SuggestWordArrayWrapper> queueComparer = sortMethod == BreakSuggestionSortMethod.NUM_CHANGES_THEN_MAX_FREQUENCY 
                ? (IComparer<SuggestWordArrayWrapper>)LengthThenMaxFreqComparer.Default 
                : LengthThenSumFreqComparer.Default;
            JCG.PriorityQueue<SuggestWordArrayWrapper> suggestions = new JCG.PriorityQueue<SuggestWordArrayWrapper>(queueInitialCapacity, queueComparer);

            int origFreq = ir.DocFreq(term);
            if (origFreq > 0 && suggestMode == SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX)
            {
                return Arrays.Empty<SuggestWord[]>();
            }

            int useMinSuggestionFrequency = minSuggestionFrequency;
            if (suggestMode == SuggestMode.SUGGEST_MORE_POPULAR)
            {
                useMinSuggestionFrequency = (origFreq == 0 ? 1 : origFreq);
            }

            GenerateBreakUpSuggestions(term, ir, 1, maxSuggestions, useMinSuggestionFrequency, Arrays.Empty<SuggestWord>(), suggestions, 0, sortMethod);

            SuggestWord[][] suggestionArray = new SuggestWord[suggestions.Count][];
            for (int i = suggestions.Count - 1; i >= 0; i--)
            {
                suggestionArray[i] = suggestions.Dequeue().SuggestWords;
            }

            return suggestionArray;
        }

        /// <summary>
        /// <para>
        /// Generate suggestions by combining one or more of the passed-in terms into
        /// single words. The returned <see cref="CombineSuggestion"/> contains both a
        /// <see cref="SuggestWord"/> and also an array detailing which passed-in terms were
        /// involved in creating this combination. The scores returned are equal to the
        /// number of word combinations needed, also one less than the length of the
        /// array <see cref="CombineSuggestion.OriginalTermIndexes"/>. Generally, a
        /// suggestion with a lower score is preferred over a higher score.
        /// </para>
        /// <para>
        /// To prevent two adjacent terms from being combined (for instance, if one is
        /// mandatory and the other is prohibited), separate the two terms with
        /// <see cref="WordBreakSpellChecker.SEPARATOR_TERM"/>
        /// </para>
        /// <para>
        /// When suggestMode equals <see cref="SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX"/>, each
        /// suggestion will include at least one term not in the index.
        /// </para>
        /// <para>
        /// When suggestMode equals <see cref="SuggestMode.SUGGEST_MORE_POPULAR"/>, each
        /// suggestion will have the same, or better frequency than the most-popular
        /// included term.
        /// </para>
        /// </summary>
        /// <returns> an array of words generated by combining original terms </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        public virtual CombineSuggestion[] SuggestWordCombinations(Term[] terms, int maxSuggestions, 
            IndexReader ir, SuggestMode suggestMode = SuggestMode.SUGGEST_WHEN_NOT_IN_INDEX)
        {
            if (maxSuggestions < 1)
            {
                return Arrays.Empty<CombineSuggestion>();
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
            IComparer<CombineSuggestionWrapper> queueComparer = CombinationsThenFreqComparer.Default;
            JCG.PriorityQueue<CombineSuggestionWrapper> suggestions = new JCG.PriorityQueue<CombineSuggestionWrapper>(queueInitialCapacity, queueComparer);

            int thisTimeEvaluations = 0;
            for (int i = 0; i < terms.Length - 1; i++)
            {
                if (terms[i].Equals(SEPARATOR_TERM))
                {
                    continue;
                }
                string leftTermText = terms[i].Text;
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
                    string rightTermText = terms[j].Text;
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

                    Term combinedTerm = new Term(terms[0].Field, combinedTermText);
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
                                word.Freq = combinedTermFreq;
                                word.Score = origIndexes.Length - 1;
                                word.String = combinedTerm.Text;
                                CombineSuggestionWrapper suggestion = new CombineSuggestionWrapper(new CombineSuggestion(word, origIndexes), (origIndexes.Length - 1));
                                suggestions.Enqueue(suggestion);
                                if (suggestions.Count > maxSuggestions)
                                {
                                    suggestions.TryDequeue(out CombineSuggestionWrapper _);
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
                combineSuggestions[i] = suggestions.Dequeue().CombineSuggestion;
            }
            return combineSuggestions;
        }

        private int GenerateBreakUpSuggestions(Term term, IndexReader ir, 
            int numberBreaks, int maxSuggestions, int useMinSuggestionFrequency, 
            SuggestWord[] prefix, JCG.PriorityQueue<SuggestWordArrayWrapper> suggestions, 
            int totalEvaluations, BreakSuggestionSortMethod sortMethod)
        {
            string termText = term.Text;
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
                SuggestWord leftWord = GenerateSuggestWord(ir, term.Field, leftText);

                if (leftWord.Freq >= useMinSuggestionFrequency)
                {
                    SuggestWord rightWord = GenerateSuggestWord(ir, term.Field, rightText);
                    if (rightWord.Freq >= useMinSuggestionFrequency)
                    {
                        SuggestWordArrayWrapper suggestion = new SuggestWordArrayWrapper(NewSuggestion(prefix, leftWord, rightWord));
                        suggestions.Enqueue(suggestion);
                        if (suggestions.Count > maxSuggestions)
                        {
                            suggestions.Dequeue();
                        }
                    }
                    int newNumberBreaks = numberBreaks + 1;
                    if (newNumberBreaks <= maxChanges)
                    {
                        int evaluations = GenerateBreakUpSuggestions(new Term(term.Field, rightWord.String), 
                            ir, newNumberBreaks, maxSuggestions, 
                            useMinSuggestionFrequency, NewPrefix(prefix, leftWord), 
                            suggestions, totalEvaluations, sortMethod);
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
            Arrays.Copy(oldPrefix, 0, newPrefix, 0, oldPrefix.Length);
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
                word.String = prefix[i].String;
                word.Freq = prefix[i].Freq;
                word.Score = score;
                newSuggestion[i] = word;
            }
            append1.Score = score;
            append2.Score = score;
            newSuggestion[newSuggestion.Length - 2] = append1;
            newSuggestion[newSuggestion.Length - 1] = append2;
            return newSuggestion;
        }

        private static SuggestWord GenerateSuggestWord(IndexReader ir, string fieldname, string text) // LUCENENET: CA1822: Mark members as static
        {
            Term term = new Term(fieldname, text);
            int freq = ir.DocFreq(term);
            SuggestWord word = new SuggestWord();
            word.Freq = freq;
            word.Score = 1;
            word.String = text;
            return word;
        }

        /// <summary>
        /// Gets or sets the minimum frequency a term must have to be 
        /// included as part of a suggestion. Default=1 Not applicable when used with
        /// <see cref="SuggestMode.SUGGEST_MORE_POPULAR"/>
        /// </summary>
        public virtual int MinSuggestionFrequency
        {
            get => minSuggestionFrequency;
            set => this.minSuggestionFrequency = value;
        }

        /// <summary>
        /// Gets or sets the maximum length of a suggestion made 
        /// by combining 1 or more original terms. Default=20.
        /// </summary>
        public virtual int MaxCombineWordLength
        {
            get => maxCombineWordLength;
            set => this.maxCombineWordLength = value;
        }

        /// <summary>
        /// Gets or sets the minimum length to break words down to. Default=1.
        /// </summary>
        public virtual int MinBreakWordLength
        {
            get => minBreakWordLength;
            set => this.minBreakWordLength = value;
        }

        /// <summary>
        /// Gets or sets the maximum numbers of changes (word breaks or combinations) to make 
        /// on the original term(s). Default=1.
        /// </summary>
        public virtual int MaxChanges
        {
            get => maxChanges;
            set => this.maxChanges = value;
        }

        /// <summary>
        /// Gets or sets the maximum number of word combinations to evaluate. Default=1000. A higher
        /// value might improve result quality. A lower value might improve performance.
        /// </summary>
        public virtual int MaxEvaluations
        {
            get => maxEvaluations;
            set => this.maxEvaluations = value;
        }

        private sealed class LengthThenMaxFreqComparer : IComparer<SuggestWordArrayWrapper>
        {
            private LengthThenMaxFreqComparer() { } // LUCENENET: Made into singleton

            public static IComparer<SuggestWordArrayWrapper> Default { get; } = new LengthThenMaxFreqComparer();

            public int Compare(SuggestWordArrayWrapper o1, SuggestWordArrayWrapper o2)
            {
                if (o1.SuggestWords.Length != o2.SuggestWords.Length)
                {
                    return o2.SuggestWords.Length - o1.SuggestWords.Length;
                }
                if (o1.FreqMax != o2.FreqMax)
                {
                    return o1.FreqMax - o2.FreqMax;
                }
                return 0;
            }
        }

        private sealed class LengthThenSumFreqComparer : IComparer<SuggestWordArrayWrapper>
        {
            private LengthThenSumFreqComparer() { } // LUCENENET: Made into singleton

            public static IComparer<SuggestWordArrayWrapper> Default { get; } = new LengthThenSumFreqComparer();

            public int Compare(SuggestWordArrayWrapper o1, SuggestWordArrayWrapper o2)
            {
                if (o1.SuggestWords.Length != o2.SuggestWords.Length)
                {
                    return o2.SuggestWords.Length - o1.SuggestWords.Length;
                }
                if (o1.FreqSum != o2.FreqSum)
                {
                    return o1.FreqSum - o2.FreqSum;
                }
                return 0;
            }
        }

        private sealed class CombinationsThenFreqComparer : IComparer<CombineSuggestionWrapper>
        {
            private CombinationsThenFreqComparer() { } // LUCENENET: Made into singleton

            public static IComparer<CombineSuggestionWrapper> Default { get; } = new CombinationsThenFreqComparer();

            public int Compare(CombineSuggestionWrapper o1, CombineSuggestionWrapper o2)
            {
                if (o1.NumCombinations != o2.NumCombinations)
                {
                    return o2.NumCombinations - o1.NumCombinations;
                }
                if (o1.CombineSuggestion.Suggestion.Freq != o2.CombineSuggestion.Suggestion.Freq)
                {
                    return o1.CombineSuggestion.Suggestion.Freq - o2.CombineSuggestion.Suggestion.Freq;
                }
                return 0;
            }
        }

        private class SuggestWordArrayWrapper : IComparable<SuggestWordArrayWrapper>
        {
            private readonly SuggestWord[] suggestWords;
            private readonly int freqMax;
            private readonly int freqSum;

            internal SuggestWordArrayWrapper(SuggestWord[] suggestWords)
            {
                this.suggestWords = suggestWords;
                int aFreqSum = 0;
                int aFreqMax = 0;
                foreach (SuggestWord sw in suggestWords)
                {
                    aFreqSum += sw.Freq;
                    aFreqMax = Math.Max(aFreqMax, sw.Freq);
                }
                this.freqSum = aFreqSum;
                this.freqMax = aFreqMax;
            }

            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public SuggestWord[] SuggestWords => suggestWords;

            public int FreqMax => freqMax;

            public int FreqSum => freqSum;

            // LUCENENET: Required by the PriorityQueue's generic constraint, but we are using
            // IComparer<T> here rather than IComparable<T>
            public int CompareTo(SuggestWordArrayWrapper other)
            {
                throw UnsupportedOperationException.Create();
            }
        }

        private class CombineSuggestionWrapper : IComparable<CombineSuggestionWrapper>
        {
            private readonly CombineSuggestion combineSuggestion;
            private readonly int numCombinations;

            internal CombineSuggestionWrapper(CombineSuggestion combineSuggestion, int numCombinations)
            {
                this.combineSuggestion = combineSuggestion;
                this.numCombinations = numCombinations;
            }

            public CombineSuggestion CombineSuggestion => combineSuggestion;

            public int NumCombinations => numCombinations;

            // LUCENENET: Required by the PriorityQueue's generic constraint, but we are using
            // IComparer<T> here rather than IComparable<T>
            public int CompareTo(CombineSuggestionWrapper other)
            {
                throw UnsupportedOperationException.Create();
            }
        }
    }
}