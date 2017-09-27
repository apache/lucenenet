// lucene version compatibility level: 4.8.1
namespace Lucene.Net.Analysis.Cn.Smart.Hhmm
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
    /// Filters a <see cref="SegToken"/> by converting full-width latin to half-width, then lowercasing latin.
    /// Additionally, all punctuation is converted into <see cref="Utility.COMMON_DELIMITER"/>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class SegTokenFilter
    {
        /// <summary>
        /// Filter an input <see cref="SegToken"/>
        /// <para>
        /// Full-width latin will be converted to half-width, then all latin will be lowercased.
        /// All punctuation is converted into <see cref="Utility.COMMON_DELIMITER"/>
        /// </para>
        /// </summary>
        /// <param name="token">Input <see cref="SegToken"/>.</param>
        /// <returns>Normalized <see cref="SegToken"/>.</returns>
        public virtual SegToken Filter(SegToken token)
        {
            switch (token.WordType)
            {
                case WordType.FULLWIDTH_NUMBER:
                case WordType.FULLWIDTH_STRING: /* first convert full-width -> half-width */
                    for (int i = 0; i < token.CharArray.Length; i++)
                    {
                        if (token.CharArray[i] >= 0xFF10)
                        {
                            token.CharArray[i] = (char)(token.CharArray[i] - 0xFEE0);
                        }

                        if (token.CharArray[i] >= 0x0041 && token.CharArray[i] <= 0x005A) /* lowercase latin */
                        {
                            token.CharArray[i] = (char)(token.CharArray[i] + 0x0020);
                        }
                    }
                    break;
                case WordType.STRING:
                    for (int i = 0; i < token.CharArray.Length; i++)
                    {
                        if (token.CharArray[i] >= 0x0041 && token.CharArray[i] <= 0x005A) /* lowercase latin */
                        {
                            token.CharArray[i] = (char)(token.CharArray[i] + 0x0020);
                        }
                    }
                    break;
                case WordType.DELIMITER: /* convert all punctuation to Utility.COMMON_DELIMITER */
                    token.CharArray = Utility.COMMON_DELIMITER;
                    break;
                default:
                    break;
            }
            return token;
        }
    }
}
