// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Fa
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
    /// Normalizer for Persian.
    /// <para>
    /// Normalization is done in-place for efficiency, operating on a termbuffer.
    /// </para>
    /// <para>
    /// Normalization is defined as:
    /// <list type="bullet">
    ///     <item><description>Normalization of various heh + hamza forms and heh goal to heh.</description></item>
    ///     <item><description>Normalization of farsi yeh and yeh barree to arabic yeh</description></item>
    ///     <item><description>Normalization of persian keheh to arabic kaf</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class PersianNormalizer
    {
        public const char YEH = '\u064A';

        public const char FARSI_YEH = '\u06CC';

        public const char YEH_BARREE = '\u06D2';

        public const char KEHEH = '\u06A9';

        public const char KAF = '\u0643';

        public const char HAMZA_ABOVE = '\u0654';

        public const char HEH_YEH = '\u06C0';

        public const char HEH_GOAL = '\u06C1';

        public const char HEH = '\u0647';

        /// <summary>
        /// Normalize an input buffer of Persian text
        /// </summary>
        /// <param name="s"> input buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> length of input buffer after normalization </returns>
        public virtual int Normalize(char[] s, int len)
        {

            for (int i = 0; i < len; i++)
            {
                switch (s[i])
                {
                    case FARSI_YEH:
                    case YEH_BARREE:
                        s[i] = YEH;
                        break;
                    case KEHEH:
                        s[i] = KAF;
                        break;
                    case HEH_YEH:
                    case HEH_GOAL:
                        s[i] = HEH;
                        break;
                    case HAMZA_ABOVE: // necessary for HEH + HAMZA
                        len = StemmerUtil.Delete(s, i, len);
                        i--;
                        break;
                    default:
                        break;
                }
            }

            return len;
        }
    }
}