// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Miscellaneous
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
    /// This filter normalize use of the interchangeable Scandinavian characters æÆäÄöÖøØ
    /// and folded variants (aa, ao, ae, oe and oo) by transforming them to åÅæÆøØ.
    /// <para/>
    /// It's a semantically less destructive solution than <see cref="ScandinavianFoldingFilter"/>,
    /// most useful when a person with a Norwegian or Danish keyboard queries a Swedish index
    /// and vice versa. This filter does <b>not</b>  the common Swedish folds of å and ä to a nor ö to o.
    /// <para/>
    /// blåbærsyltetøj == blåbärsyltetöj == blaabaarsyltetoej but not blabarsyltetoj
    /// räksmörgås == ræksmørgås == ræksmörgaos == raeksmoergaas but not raksmorgas
    /// </summary>
    /// <seealso cref="ScandinavianFoldingFilter"/>
    public sealed class ScandinavianNormalizationFilter : TokenFilter
    {
        public ScandinavianNormalizationFilter(TokenStream input) 
            : base(input)
        {
            charTermAttribute = AddAttribute<ICharTermAttribute>();
        }

        private readonly ICharTermAttribute charTermAttribute;

        private const char AA = '\u00C5'; // Å
        private const char aa = '\u00E5'; // å
        private const char AE = '\u00C6'; // Æ
        private const char ae = '\u00E6'; // æ
        private const char AE_se = '\u00C4'; // Ä
        private const char ae_se = '\u00E4'; // ä
        private const char OE = '\u00D8'; // Ø
        private const char oe = '\u00F8'; // ø
        private const char OE_se = '\u00D6'; // Ö
        private const char oe_se = '\u00F6'; //ö


        public override bool IncrementToken()
        {
            if (!m_input.IncrementToken())
            {
                return false;
            }

            char[] buffer = charTermAttribute.Buffer;
            int length = charTermAttribute.Length;


            int i;
            for (i = 0; i < length; i++)
            {

                if (buffer[i] == ae_se)
                {
                    buffer[i] = ae;

                }
                else if (buffer[i] == AE_se)
                {
                    buffer[i] = AE;

                }
                else if (buffer[i] == oe_se)
                {
                    buffer[i] = oe;

                }
                else if (buffer[i] == OE_se)
                {
                    buffer[i] = OE;

                }
                else if (length - 1 > i)
                {

                    if (buffer[i] == 'a' && (buffer[i + 1] == 'a' || buffer[i + 1] == 'o' || buffer[i + 1] == 'A' || buffer[i + 1] == 'O'))
                    {
                        length = StemmerUtil.Delete(buffer, i + 1, length);
                        buffer[i] = aa;

                    }
                    else if (buffer[i] == 'A' && (buffer[i + 1] == 'a' || buffer[i + 1] == 'A' || buffer[i + 1] == 'o' || buffer[i + 1] == 'O'))
                    {
                        length = StemmerUtil.Delete(buffer, i + 1, length);
                        buffer[i] = AA;

                    }
                    else if (buffer[i] == 'a' && (buffer[i + 1] == 'e' || buffer[i + 1] == 'E'))
                    {
                        length = StemmerUtil.Delete(buffer, i + 1, length);
                        buffer[i] = ae;

                    }
                    else if (buffer[i] == 'A' && (buffer[i + 1] == 'e' || buffer[i + 1] == 'E'))
                    {
                        length = StemmerUtil.Delete(buffer, i + 1, length);
                        buffer[i] = AE;

                    }
                    else if (buffer[i] == 'o' && (buffer[i + 1] == 'e' || buffer[i + 1] == 'E' || buffer[i + 1] == 'o' || buffer[i + 1] == 'O'))
                    {
                        length = StemmerUtil.Delete(buffer, i + 1, length);
                        buffer[i] = oe;

                    }
                    else if (buffer[i] == 'O' && (buffer[i + 1] == 'e' || buffer[i + 1] == 'E' || buffer[i + 1] == 'o' || buffer[i + 1] == 'O'))
                    {
                        length = StemmerUtil.Delete(buffer, i + 1, length);
                        buffer[i] = OE;

                    }

                }
            }

            charTermAttribute.Length = length;

            return true;
        }
    }
}