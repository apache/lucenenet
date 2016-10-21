using System;
using System.Collections.Generic;
using System.Globalization;
using Lucene.Net.Analysis.Tokenattributes;
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
    /// A filter to apply normal capitalization rules to Tokens.  It will make the first letter
    /// capital and the rest lower case.
    /// <p/>
    /// This filter is particularly useful to build nice looking facet parameters.  This filter
    /// is not appropriate if you intend to use a prefix query.
    /// </summary>
    public sealed class CapitalizationFilter : TokenFilter
    {
        public static readonly int DEFAULT_MAX_WORD_COUNT = int.MaxValue;
        public static readonly int DEFAULT_MAX_TOKEN_LENGTH = int.MaxValue;

        private readonly bool onlyFirstWord;
        private readonly CharArraySet keep;
        private readonly bool forceFirstLetter;
        private readonly ICollection<char[]> okPrefix;

        private readonly int minWordLength;
        private readonly int maxWordCount;
        private readonly int maxTokenLength;

        private readonly ICharTermAttribute termAtt;

        /// <summary>
        /// Creates a CapitalizationFilter with the default parameters.
        /// <para>
        /// Calls {@link #CapitalizationFilter(TokenStream, boolean, CharArraySet, boolean, Collection, int, int, int)
        ///   CapitalizationFilter(in, true, null, true, null, 0, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH)}
        /// </para>
        /// </summary>
        public CapitalizationFilter(TokenStream @in)
            : this(@in, true, null, true, null, 0, DEFAULT_MAX_WORD_COUNT, DEFAULT_MAX_TOKEN_LENGTH)
        {
        }

        /// <summary>
        /// Creates a CapitalizationFilter with the specified parameters. </summary>
        /// <param name="in"> input tokenstream </param>
        /// <param name="onlyFirstWord"> should each word be capitalized or all of the words? </param>
        /// <param name="keep"> a keep word list.  Each word that should be kept separated by whitespace. </param>
        /// <param name="forceFirstLetter"> Force the first letter to be capitalized even if it is in the keep list. </param>
        /// <param name="okPrefix"> do not change word capitalization if a word begins with something in this list. </param>
        /// <param name="minWordLength"> how long the word needs to be to get capitalization applied.  If the
        ///                      minWordLength is 3, "and" > "And" but "or" stays "or". </param>
        /// <param name="maxWordCount"> if the token contains more then maxWordCount words, the capitalization is
        ///                     assumed to be correct. </param>
        /// <param name="maxTokenLength"> ??? </param>
        public CapitalizationFilter(TokenStream @in, bool onlyFirstWord, CharArraySet keep, bool forceFirstLetter, ICollection<char[]> okPrefix, int minWordLength, int maxWordCount, int maxTokenLength)
            : base(@in)
        {
            // LUCENENET: The guard clauses were copied here from the version of Lucene.
            // Apparently, the tests were not ported from 4.8.0 because they expected this and the
            // original tests did not. Adding them anyway because there is no downside to this.
            if (minWordLength < 0)
            {
                throw new ArgumentOutOfRangeException("minWordLength must be greater than or equal to zero");
            }
            if (maxWordCount < 1)
            {
                throw new ArgumentOutOfRangeException("maxWordCount must be greater than zero");
            }
            if (maxTokenLength < 1)
            {
                throw new ArgumentOutOfRangeException("maxTokenLength must be greater than zero");
            }

            this.onlyFirstWord = onlyFirstWord;
            this.keep = keep;
            this.forceFirstLetter = forceFirstLetter;
            this.okPrefix = okPrefix;
            this.minWordLength = minWordLength;
            this.maxWordCount = maxWordCount;
            this.maxTokenLength = maxTokenLength;
            termAtt = AddAttribute<ICharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            if (!input.IncrementToken())
            {
                return false;
            }

            char[] termBuffer = termAtt.Buffer();
            int termBufferLength = termAtt.Length;
            char[] backup = null;

            if (maxWordCount < DEFAULT_MAX_WORD_COUNT)
            {
                //make a backup in case we exceed the word count
                backup = new char[termBufferLength];
                Array.Copy(termBuffer, 0, backup, 0, termBufferLength);
            }

            if (termBufferLength < maxTokenLength)
            {
                int wordCount = 0;

                int lastWordStart = 0;
                for (int i = 0; i < termBufferLength; i++)
                {
                    char c = termBuffer[i];
                    if (c <= ' ' || c == '.')
                    {
                        int len = i - lastWordStart;
                        if (len > 0)
                        {
                            ProcessWord(termBuffer, lastWordStart, len, wordCount++);
                            lastWordStart = i + 1;
                            i++;
                        }
                    }
                }

                // process the last word
                if (lastWordStart < termBufferLength)
                {
                    ProcessWord(termBuffer, lastWordStart, termBufferLength - lastWordStart, wordCount++);
                }

                if (wordCount > maxWordCount)
                {
                    termAtt.CopyBuffer(backup, 0, termBufferLength);
                }
            }

            return true;
        }

        private void ProcessWord(char[] buffer, int offset, int length, int wordCount)
        {
            if (length < 1)
            {
                return;
            }

            if (onlyFirstWord && wordCount > 0)
            {
                for (int i = 0; i < length; i++)
                {
                    buffer[offset + i] = char.ToLower(buffer[offset + i]);

                }
                return;
            }

            if (keep != null && keep.Contains(buffer, offset, length))
            {
                if (wordCount == 0 && forceFirstLetter)
                {
                    buffer[offset] = CultureInfo.InvariantCulture.TextInfo.ToUpper(buffer[offset]);
                }
                return;
            }

            if (length < minWordLength)
            {
                return;
            }

            if (okPrefix != null)
            {
                foreach (char[] prefix in okPrefix)
                {
                    if (length >= prefix.Length) //don't bother checking if the buffer length is less than the prefix
                    {
                        bool match = true;
                        for (int i = 0; i < prefix.Length; i++)
                        {
                            if (prefix[i] != buffer[offset + i])
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match)
                        {
                            return;
                        }
                    }
                }
            }

            // We know it has at least one character
            /*char[] chars = w.toCharArray();
            StringBuilder word = new StringBuilder( w.length() );
            word.append( Character.toUpperCase( chars[0] ) );*/
            buffer[offset] = char.ToUpper(buffer[offset]);

            for (int i = 1; i < length; i++)
            {
                buffer[offset + i] = CultureInfo.InvariantCulture.TextInfo.ToLower(buffer[offset + i]);
            }
            //return word.toString();
        }
    }
}