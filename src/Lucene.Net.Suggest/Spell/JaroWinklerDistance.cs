using Lucene.Net.Support;
using System;

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
    /// Similarity measure for short strings such as person names.
    /// See <a href="http://en.wikipedia.org/wiki/Jaro%E2%80%93Winkler_distance">http://en.wikipedia.org/wiki/Jaro%E2%80%93Winkler_distance</a>
    /// </summary>
    public class JaroWinklerDistance : IStringDistance
    {

        private float threshold = 0.7f;

        /// <summary>
        /// Creates a new distance metric with the default threshold
        /// for the Jaro Winkler bonus (0.7) </summary>
        /// <seealso cref="Threshold"/>
        public JaroWinklerDistance()
        {
        }

        private static int[] Matches(string s1, string s2) // LUCENENET: CA1822: Mark members as static
        {
            string max, min;
            if (s1.Length > s2.Length)
            {
                max = s1;
                min = s2;
            }
            else
            {
                max = s2;
                min = s1;
            }
            int range = Math.Max(max.Length / 2 - 1, 0);
            int[] matchIndexes = new int[min.Length];
            Arrays.Fill(matchIndexes, -1);
            bool[] matchFlags = new bool[max.Length];
            int matches = 0;
            for (int mi = 0; mi < min.Length; mi++)
            {
                char c1 = min[mi];
                for (int xi = Math.Max(mi - range, 0), xn = Math.Min(mi + range + 1, max.Length); xi < xn; xi++)
                {
                    if (!matchFlags[xi] && c1 == max[xi])
                    {
                        matchIndexes[mi] = xi;
                        matchFlags[xi] = true;
                        matches++;
                        break;
                    }
                }
            }
            char[] ms1 = new char[matches];
            char[] ms2 = new char[matches];
            for (int i = 0, si = 0; i < min.Length; i++)
            {
                if (matchIndexes[i] != -1)
                {
                    ms1[si] = min[i];
                    si++;
                }
            }
            for (int i = 0, si = 0; i < max.Length; i++)
            {
                if (matchFlags[i])
                {
                    ms2[si] = max[i];
                    si++;
                }
            }
            int transpositions = 0;
            for (int mi = 0; mi < ms1.Length; mi++)
            {
                if (ms1[mi] != ms2[mi])
                {
                    transpositions++;
                }
            }
            int prefix = 0;
            for (int mi = 0; mi < min.Length; mi++)
            {
                if (s1[mi] == s2[mi])
                {
                    prefix++;
                }
                else
                {
                    break;
                }
            }
            return new int[] { matches, transpositions / 2, prefix, max.Length };
        }

        public virtual float GetDistance(string s1, string s2)
        {
            int[] mtp = Matches(s1, s2);
            float m = mtp[0];
            if (m == 0)
            {
                return 0f;
            }
            float j = ((m / s1.Length + m / s2.Length + (m - mtp[1]) / m)) / 3;
            float jw = j < Threshold ? j : j + Math.Min(0.1f, 1f / mtp[3]) * mtp[2] * (1 - j);
            return jw;
        }

        /// <summary>
        /// Gets or sets the threshold used to determine when Winkler bonus should be used.
        /// The default value is 0.7. Set to a negative value to get the Jaro distance. 
        /// </summary>
        public virtual float Threshold
        {
            set => this.threshold = value;
            get => threshold;
        }


        public override int GetHashCode()
        {
            return 113 * J2N.BitConversion.SingleToInt32Bits(threshold) * this.GetType().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (null == obj || this.GetType() != obj.GetType())
            {
                return false;
            }

            JaroWinklerDistance o = (JaroWinklerDistance)obj;
            return (J2N.BitConversion.SingleToInt32Bits(o.threshold) == J2N.BitConversion.SingleToInt32Bits(this.threshold));
        }

        public override string ToString()
        {
            return "jarowinkler(" + threshold + ")";
        }
    }
}