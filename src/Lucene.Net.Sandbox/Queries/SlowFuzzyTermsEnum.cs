using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Sandbox.Queries
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
    /// Potentially slow fuzzy <see cref="TermsEnum"/> for enumerating all terms that are similar
    /// to the specified filter term.
    /// <para/>
    /// If the minSimilarity or maxEdits is greater than the Automaton's
    /// allowable range, this backs off to the classic (brute force)
    /// fuzzy terms enum method by calling <see cref="FuzzyTermsEnum.GetAutomatonEnum(int, BytesRef)"/>.
    /// <para/>
    /// Term enumerations are always ordered by
    /// <see cref="FuzzyTermsEnum.Comparer"/>. Each term in the enumeration is
    /// greater than all that precede it.
    /// </summary>
    [Obsolete("Use FuzzyTermsEnum instead.")]
    public class SlowFuzzyTermsEnum : FuzzyTermsEnum
    {
        public SlowFuzzyTermsEnum(Terms terms, AttributeSource atts, Term term,
            float minSimilarity, int prefixLength)
            : base(terms, atts, term, minSimilarity, prefixLength, false)
        {
        }

        protected override void MaxEditDistanceChanged(BytesRef lastTerm, int maxEdits, bool init)
        {
            TermsEnum newEnum = GetAutomatonEnum(maxEdits, lastTerm);
            if (newEnum != null)
            {
                SetEnum(newEnum);
            }
            else if (init)
            {
                SetEnum(new LinearFuzzyTermsEnum(this));
            }
        }

        /// <summary>
        /// Implement fuzzy enumeration with linear brute force.
        /// </summary>
        private class LinearFuzzyTermsEnum : FilteredTermsEnum
        {
            private readonly SlowFuzzyTermsEnum outerInstance;

            /// <summary>
            /// Allows us save time required to create a new array
            /// every time similarity is called.
            /// </summary>
            private int[] d;
            private int[] p;

            /// <summary>this is the text, minus the prefix</summary>
            private readonly int[] text;

            private readonly IBoostAttribute boostAtt;

            /// <summary>
            /// Constructor for enumeration of all terms from specified <c>reader</c> which share a prefix of
            /// length <c>prefixLength</c> with <c>term</c> and which have a fuzzy similarity &gt;
            /// <c>minSimilarity</c>.
            /// <para/>
            /// After calling the constructor the enumeration is already pointing to the first 
            /// valid term if such a term exists.
            /// </summary>
            /// <exception cref="IOException">If there is a low-level I/O error.</exception>
            public LinearFuzzyTermsEnum(SlowFuzzyTermsEnum outerInstance)
                : base(outerInstance.m_terms.GetEnumerator())
            {
                this.outerInstance = outerInstance;
                this.boostAtt = Attributes.AddAttribute<IBoostAttribute>();

                this.text = new int[outerInstance.m_termLength - outerInstance.m_realPrefixLength];
                Arrays.Copy(outerInstance.m_termText, outerInstance.m_realPrefixLength, text, 0, text.Length);
                string prefix = UnicodeUtil.NewString(outerInstance.m_termText, 0, outerInstance.m_realPrefixLength);
                prefixBytesRef = new BytesRef(prefix);
                this.d = new int[this.text.Length + 1];
                this.p = new int[this.text.Length + 1];


                SetInitialSeekTerm(prefixBytesRef);
            }

            private readonly BytesRef prefixBytesRef;
            /// <summary>used for unicode conversion from BytesRef byte[] to int[]</summary>
            private readonly Int32sRef utf32 = new Int32sRef(20);

            /// <summary>
            /// <para>
            /// The termCompare method in FuzzyTermEnum uses Levenshtein distance to
            /// calculate the distance between the given term and the comparing term.
            /// </para>
            /// <para>
            /// If the minSimilarity is >= 1.0, this uses the maxEdits as the comparison.
            /// Otherwise, this method uses the following logic to calculate similarity.
            /// <code>
            ///     similarity = 1 - ((float)distance / (float) (prefixLength + Math.min(textlen, targetlen)));
            /// </code>
            /// where distance is the Levenshtein distance for the two words.
            /// </para>
            /// </summary>
            protected override sealed AcceptStatus Accept(BytesRef term)
            {
                if (StringHelper.StartsWith(term, prefixBytesRef))
                {
                    UnicodeUtil.UTF8toUTF32(term, utf32);
                    int distance = CalcDistance(utf32.Int32s, outerInstance.m_realPrefixLength, utf32.Length - outerInstance.m_realPrefixLength);

                    //Integer.MIN_VALUE is the sentinel that Levenshtein stopped early
                    if (distance == int.MinValue)
                    {
                        return AcceptStatus.NO;
                    }
                    //no need to calc similarity, if raw is true and distance > maxEdits
                    if (outerInstance.m_raw == true && distance > outerInstance.m_maxEdits)
                    {
                        return AcceptStatus.NO;
                    }
                    float similarity = CalcSimilarity(distance, (utf32.Length - outerInstance.m_realPrefixLength), text.Length);

                    //if raw is true, then distance must also be <= maxEdits by now
                    //given the previous if statement
                    if (outerInstance.m_raw == true ||
                          (outerInstance.m_raw == false && NumericUtils.SingleToSortableInt32(similarity) > NumericUtils.SingleToSortableInt32(outerInstance.MinSimilarity)))
                    {
                        boostAtt.Boost = (similarity - outerInstance.MinSimilarity) * outerInstance.m_scaleFactor;
                        return AcceptStatus.YES;
                    }
                    else
                    {
                        return AcceptStatus.NO;
                    }
                }
                else
                {
                    return AcceptStatus.END;
                }
            }

            /******************************
             * Compute Levenshtein distance
             ******************************/

            /// <summary>
            /// <para>
            /// <see cref="CalcDistance(int[], int, int)"/> returns the Levenshtein distance between the query term
            /// and the target term.
            /// </para>
            /// <para>
            /// Embedded within this algorithm is a fail-fast Levenshtein distance
            /// algorithm.  The fail-fast algorithm differs from the standard Levenshtein
            /// distance algorithm in that it is aborted if it is discovered that the
            /// minimum distance between the words is greater than some threshold.
            /// </para>
            /// <para>
            /// Levenshtein distance (also known as edit distance) is a measure of similarity
            /// between two strings where the distance is measured as the number of character
            /// deletions, insertions or substitutions required to transform one string to
            /// the other string.
            /// </para>
            /// </summary>
            /// <param name="target">the target word or phrase</param>
            /// <param name="offset">the offset at which to start the comparison</param>
            /// <param name="length">the length of what's left of the string to compare</param>
            /// <returns>
            /// the number of edits or <see cref="int.MaxValue"/> if the edit distance is
            /// greater than maxDistance.
            /// </returns>
            private int CalcDistance(int[] target, int offset, int length)
            {
                int m = length;
                int n = text.Length;
                if (n == 0)
                {
                    //we don't have anything to compare.  That means if we just add
                    //the letters for m we get the new word
                    return m;
                }
                if (m == 0)
                {
                    return n;
                }

                int maxDistance = CalculateMaxDistance(m);

                if (maxDistance < Math.Abs(m - n))
                {
                    //just adding the characters of m to n or vice-versa results in
                    //too many edits
                    //for example "pre" length is 3 and "prefixes" length is 8.  We can see that
                    //given this optimal circumstance, the edit distance cannot be less than 5.
                    //which is 8-3 or more precisely Math.abs(3-8).
                    //if our maximum edit distance is 4, then we can discard this word
                    //without looking at it.
                    return int.MinValue;
                }

                // init matrix d
                for (int i = 0; i <= n; ++i)
                {
                    p[i] = i;
                }

                // start computing edit distance
                for (int j = 1; j <= m; ++j)
                { // iterates through target
                    int bestPossibleEditDistance = m;
                    int t_j = target[offset + j - 1]; // jth character of t
                    d[0] = j;

                    for (int i = 1; i <= n; ++i)
                    { // iterates through text
                      // minimum of cell to the left+1, to the top+1, diagonally left and up +(0|1)
                        if (t_j != text[i - 1])
                        {
                            d[i] = Math.Min(Math.Min(d[i - 1], p[i]), p[i - 1]) + 1;
                        }
                        else
                        {
                            d[i] = Math.Min(Math.Min(d[i - 1] + 1, p[i] + 1), p[i - 1]);
                        }
                        bestPossibleEditDistance = Math.Min(bestPossibleEditDistance, d[i]);
                    }

                    //After calculating row i, the best possible edit distance
                    //can be found by found by finding the smallest value in a given column.
                    //If the bestPossibleEditDistance is greater than the max distance, abort.

                    if (j > maxDistance && bestPossibleEditDistance > maxDistance)
                    {  //equal is okay, but not greater
                       //the closest the target can be to the text is just too far away.
                       //this target is leaving the party early.
                        return int.MinValue;
                    }

                    // copy current distance counts to 'previous row' distance counts: swap p and d
                    int[] _d = p;
                    p = d;
                    d = _d;
                }

                // our last action in the above loop was to switch d and p, so p now
                // actually has the most recent cost counts

                return p[n];
            }

            private float CalcSimilarity(int edits, int m, int n)
            {
                // this will return less than 0.0 when the edit distance is
                // greater than the number of characters in the shorter word.
                // but this was the formula that was previously used in FuzzyTermEnum,
                // so it has not been changed (even though minimumSimilarity must be
                // greater than 0.0)

                return 1.0f - ((float)edits / (float)(outerInstance.m_realPrefixLength + Math.Min(n, m)));
            }

            /// <summary>
            /// The max Distance is the maximum Levenshtein distance for the text
            /// compared to some other value that results in score that is
            /// better than the minimum similarity.
            /// </summary>
            /// <param name="m">the length of the "other value"</param>
            /// <returns>the maximum levenshtein distance that we care about</returns>
            private int CalculateMaxDistance(int m)
            {
                return outerInstance.m_raw ? outerInstance.m_maxEdits : Math.Min(outerInstance.m_maxEdits,
                    (int)((1 - outerInstance.MinSimilarity) * (Math.Min(text.Length, m) + outerInstance.m_realPrefixLength)));
            }
        }
    }
}
