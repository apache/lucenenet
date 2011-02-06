/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace Lucene.Net.Analysis
{
	
    /// <summary> A filter that replaces accented characters in the ISO Latin 1 character set 
    /// (ISO-8859-1) by their unaccented equivalent. The case will not be altered.
    /// <p>
    /// For instance, '&agrave;' will be replaced by 'a'.
    /// <p>
    /// </summary>
    public class ISOLatin1AccentFilter : TokenFilter
    {
        public ISOLatin1AccentFilter(TokenStream input) : base(input)
        {
        }

        private char[] output = new char[256];
        private int outputPos;

        public override Token Next(Token result)
		{
            result = input.Next(result);
            if (result != null)
            {
                char[] buffer = result.TermBuffer();
                int length = result.TermLength();
                // If no characters actually require rewriting then we
                // just return token as-is:
                for (int i = 0; i < length; i++)
                {
                    char c = buffer[i];
                    if (c >= '\u00c0' && c <= '\u0178')
                    {
                        RemoveAccents(buffer, length);
                        result.SetTermBuffer(output, 0, outputPos);
                        break;
                    }
                }
                return result;
            }
            else
                return null;
        }
		
        /// <summary> To replace accented characters in a String by unaccented equivalents.</summary>
        public void RemoveAccents(char[] input, int length)
        {

            // Worst-case length required:
            int maxSizeNeeded = 2 * length;

            int size = output.Length;
            while (size < maxSizeNeeded)
                size *= 2;

            if (size != output.Length)
                output = new char[size];

            outputPos = 0;

            int pos = 0;

            for (int i = 0; i < length; i++, pos++)
            {
                char c = input[pos];

                // Quick test: if it's not in range then just keep
                // current character
                if (c < '\u00c0')
                    output[outputPos++] = c;
                else
                {
                    switch (c)
                    {

                        case '\u00C0':
                        // Ã€
                        case '\u00C1':
                        // Ã
                        case '\u00C2':
                        // Ã‚
                        case '\u00C3':
                        // Ãƒ
                        case '\u00C4':
                        // Ã„
                        case '\u00C5':  // Ã…
                            output[outputPos++] = 'A';
                            break;

                        case '\u00C6':  // Ã†
                            output[outputPos++] = 'A';
                            output[outputPos++] = 'E';
                            break;

                        case '\u00C7':  // Ã‡
                            output[outputPos++] = 'C';
                            break;

                        case '\u00C8':
                        // Ãˆ
                        case '\u00C9':
                        // Ã‰
                        case '\u00CA':
                        // ÃŠ
                        case '\u00CB':  // Ã‹
                            output[outputPos++] = 'E';
                            break;

                        case '\u00CC':
                        // ÃŒ
                        case '\u00CD':
                        // Ã
                        case '\u00CE':
                        // ÃŽ
                        case '\u00CF':  // Ã
                            output[outputPos++] = 'I';
                            break;

                        case '\u00D0':  // Ã
                            output[outputPos++] = 'D';
                            break;

                        case '\u00D1':  // Ã‘
                            output[outputPos++] = 'N';
                            break;

                        case '\u00D2':
                        // Ã’
                        case '\u00D3':
                        // Ã“
                        case '\u00D4':
                        // Ã”
                        case '\u00D5':
                        // Ã•
                        case '\u00D6':
                        // Ã–
                        case '\u00D8':  // Ã˜
                            output[outputPos++] = 'O';
                            break;

                        case '\u0152':  // Å’
                            output[outputPos++] = 'O';
                            output[outputPos++] = 'E';
                            break;

                        case '\u00DE':  // Ãž
                            output[outputPos++] = 'T';
                            output[outputPos++] = 'H';
                            break;

                        case '\u00D9':
                        // Ã™
                        case '\u00DA':
                        // Ãš
                        case '\u00DB':
                        // Ã›
                        case '\u00DC':  // Ãœ
                            output[outputPos++] = 'U';
                            break;

                        case '\u00DD':
                        // Ã
                        case '\u0178':  // Å¸
                            output[outputPos++] = 'Y';
                            break;

                        case '\u00E0':
                        // Ã 
                        case '\u00E1':
                        // Ã¡
                        case '\u00E2':
                        // Ã¢
                        case '\u00E3':
                        // Ã£
                        case '\u00E4':
                        // Ã¤
                        case '\u00E5':  // Ã¥
                            output[outputPos++] = 'a';
                            break;

                        case '\u00E6':  // Ã¦
                            output[outputPos++] = 'a';
                            output[outputPos++] = 'e';
                            break;

                        case '\u00E7':  // Ã§
                            output[outputPos++] = 'c';
                            break;

                        case '\u00E8':
                        // Ã¨
                        case '\u00E9':
                        // Ã©
                        case '\u00EA':
                        // Ãª
                        case '\u00EB':  // Ã«
                            output[outputPos++] = 'e';
                            break;

                        case '\u00EC':
                        // Ã¬
                        case '\u00ED':
                        // Ã­
                        case '\u00EE':
                        // Ã®
                        case '\u00EF':  // Ã¯
                            output[outputPos++] = 'i';
                            break;

                        case '\u00F0':  // Ã°
                            output[outputPos++] = 'd';
                            break;

                        case '\u00F1':  // Ã±
                            output[outputPos++] = 'n';
                            break;

                        case '\u00F2':
                        // Ã²
                        case '\u00F3':
                        // Ã³
                        case '\u00F4':
                        // Ã´
                        case '\u00F5':
                        // Ãµ
                        case '\u00F6':
                        // Ã¶
                        case '\u00F8':  // Ã¸
                            output[outputPos++] = 'o';
                            break;

                        case '\u0153':  // Å“
                            output[outputPos++] = 'o';
                            output[outputPos++] = 'e';
                            break;

                        case '\u00DF':  // ÃŸ
                            output[outputPos++] = 's';
                            output[outputPos++] = 's';
                            break;

                        case '\u00FE':  // Ã¾
                            output[outputPos++] = 't';
                            output[outputPos++] = 'h';
                            break;

                        case '\u00F9':
                        // Ã¹
                        case '\u00FA':
                        // Ãº
                        case '\u00FB':
                        // Ã»
                        case '\u00FC':  // Ã¼
                            output[outputPos++] = 'u';
                            break;

                        case '\u00FD':
                        // Ã½
                        case '\u00FF':  // Ã¿
                            output[outputPos++] = 'y';
                            break;

                        default:
                            output[outputPos++] = c;
                            break;

                    }
                }
            }
        }
    }
}