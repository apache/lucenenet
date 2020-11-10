using J2N;
using Lucene.Net.Util;
using System;
using RectangularArrays = Lucene.Net.Support.RectangularArrays;

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
    ///  Damerau-Levenshtein (optimal string alignment) implemented in a consistent 
    ///  way as Lucene's FuzzyTermsEnum with the transpositions option enabled.
    ///  
    ///  Notes:
    ///  <list type="bullet">
    ///    <item><description> This metric treats full unicode codepoints as characters</description></item>
    ///    <item><description> This metric scales raw edit distances into a floating point score
    ///         based upon the shortest of the two terms</description></item>
    ///    <item><description> Transpositions of two adjacent codepoints are treated as primitive 
    ///         edits.</description></item>
    ///    <item><description> Edits are applied in parallel: for example, "ab" and "bca" have 
    ///         distance 3.</description></item>
    ///  </list>
    ///  
    ///  NOTE: this class is not particularly efficient. It is only intended
    ///  for merging results from multiple DirectSpellCheckers.
    /// </summary>
    public sealed class LuceneLevenshteinDistance : IStringDistance
    {

        /// <summary>
        /// Creates a new comparer, mimicing the behavior of Lucene's internal
        /// edit distance.
        /// </summary>
        public LuceneLevenshteinDistance()
        {
        }

        public float GetDistance(string target, string other)
        {
            Int32sRef targetPoints;
            Int32sRef otherPoints;
            int n;
            int[][] d; // cost array

            // NOTE: if we cared, we could 3*m space instead of m*n space, similar to 
            // what LevenshteinDistance does, except cycling thru a ring of three 
            // horizontal cost arrays... but this comparer is never actually used by 
            // DirectSpellChecker, its only used for merging results from multiple shards 
            // in "distributed spellcheck", and its inefficient in other ways too...

            // cheaper to do this up front once
            targetPoints = ToInt32sRef(target);
            otherPoints = ToInt32sRef(other);
            n = targetPoints.Length;
            int m = otherPoints.Length;

            d = RectangularArrays.ReturnRectangularArray<int>(n + 1, m + 1);

            if (n == 0 || m == 0)
            {
                if (n == m)
                {
                    return 0;
                }
                else
                {
                    return Math.Max(n, m);
                }
            }

            // indexes into strings s and t
            int i; // iterates through s
            int j; // iterates through t

            int t_j; // jth character of t

            int cost; // cost

            for (i = 0; i <= n; i++)
            {
                d[i][0] = i;
            }

            for (j = 0; j <= m; j++)
            {
                d[0][j] = j;
            }

            for (j = 1; j <= m; j++)
            {
                t_j = otherPoints.Int32s[j - 1];

                for (i = 1; i <= n; i++)
                {
                    cost = targetPoints.Int32s[i - 1] == t_j ? 0 : 1;
                    // minimum of cell to the left+1, to the top+1, diagonally left and up +cost
                    d[i][j] = Math.Min(Math.Min(d[i - 1][j] + 1, d[i][j - 1] + 1), d[i - 1][j - 1] + cost);
                    // transposition
                    if (i > 1 && j > 1 && targetPoints.Int32s[i - 1] == otherPoints.Int32s[j - 2] && targetPoints.Int32s[i - 2] == otherPoints.Int32s[j - 1])
                    {
                        d[i][j] = Math.Min(d[i][j], d[i - 2][j - 2] + cost);
                    }
                }
            }

            return 1.0f - ((float)d[n][m] / Math.Min(m, n));
        }

        /// <summary>
        /// NOTE: This was toIntsRef() in Lucene
        /// </summary>
        private static Int32sRef ToInt32sRef(string s)
        {
            var @ref = new Int32sRef(s.Length); // worst case
            int utf16Len = s.Length;
            for (int i = 0, cp; i < utf16Len; i += Character.CharCount(cp)) // LUCENENET: IDE0059: Remove unnecessary value assignment to cp
            {
                cp = @ref.Int32s[@ref.Length++] = Character.CodePointAt(s, i);
            }
            return @ref;
        }
    }
}