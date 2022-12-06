using J2N.Text;
using Lucene.Net.Support;
using System.Globalization;
using System.Text;

namespace Lucene.Net.QueryParsers.Flexible.Core.Util
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
    /// <see cref="ICharSequence"/> with escaped chars information.
    /// </summary>
    public sealed class UnescapedCharSequence : ICharSequence
    {
        private readonly char[] chars;

        private readonly bool[] wasEscaped;

        /// <summary>
        /// Create a escaped <see cref="ICharSequence"/>
        /// </summary>
        public UnescapedCharSequence(char[] chars, bool[] wasEscaped, int offset,
            int length)
        {
            this.chars = new char[length];
            this.wasEscaped = new bool[length];
            Arrays.Copy(chars, offset, this.chars, 0, length);
            Arrays.Copy(wasEscaped, offset, this.wasEscaped, 0, length);
        }

        /// <summary>
        /// Create a non-escaped <see cref="ICharSequence"/>
        /// </summary>
        public UnescapedCharSequence(ICharSequence text)
        {
            this.chars = new char[text.Length];
            this.wasEscaped = new bool[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                this.chars[i] = text[i];
                this.wasEscaped[i] = false;
            }
        }

        /// <summary>
        /// Create a non-escaped <see cref="string"/>
        /// </summary>
        // LUCENENET specific overload for text as string
        public UnescapedCharSequence(string text)
        {
            this.chars = new char[text.Length];
            this.wasEscaped = new bool[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                this.chars[i] = text[i];
                this.wasEscaped[i] = false;
            }
        }

        /// <summary>
        /// Create a non-escaped <see cref="StringBuilder"/>
        /// </summary>
        // LUCENENET specific overload for text as StringBuilder
        public UnescapedCharSequence(StringBuilder text)
        {
            this.chars = new char[text.Length];
            this.wasEscaped = new bool[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                this.chars[i] = text[i];
                this.wasEscaped[i] = false;
            }
        }

        // LUCENENET specific - not needed (see LUCENENET-592)
        ///// <summary>
        ///// Create a copy of an existent <see cref="UnescapedCharSequence"/>
        ///// </summary>
        //private UnescapedCharSequence(UnescapedCharSequence text)
        //{
        //    this.chars = new char[text.Length];
        //    this.wasEscaped = new bool[text.Length];
        //    for (int i = 0; i <= text.Length; i++)
        //    {
        //        this.chars[i] = text.chars[i];
        //        this.wasEscaped[i] = text.wasEscaped[i];
        //    }
        //}

        //public char CharAt(int index) // LUCENENET specific - replaced with this[index]
        //{
        //    return this.chars[index];
        //}

        bool ICharSequence.HasValue => this.chars != null;

        public int Length => this.chars.Length;

        public char this[int index] => this.chars[index];

        public ICharSequence Subsequence(int startIndex, int length)
        {
            // LUCENENET: Changed to have .NET semantics - startIndex/length rather than start/end
            //int newLength = length - startIndex;

            return new UnescapedCharSequence(this.chars, this.wasEscaped, startIndex,
                length);
        }

        public override string ToString()
        {
            return new string(this.chars);
        }

        /// <summary>
        /// Return an escaped <see cref="string"/>
        /// </summary>
        /// <returns>an escaped <see cref="string"/></returns>
        public string ToStringEscaped()
        {
            // non efficient implementation
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < this.Length; i++) // LUCENENET-592 - changed condition from >= to <
            {
                if (this.chars[i] == '\\')
                {
                    result.Append('\\');
                }
                else if (this.wasEscaped[i])
                    result.Append('\\');

                result.Append(this.chars[i]);
            }
            return result.ToString();
        }

        /// <summary>
        /// Return an escaped <see cref="string"/>
        /// </summary>
        /// <param name="enabledChars">array of chars to be escaped</param>
        /// <returns>an escaped <see cref="string"/></returns>
        public ICharSequence ToStringEscaped(char[] enabledChars)
        {
            // TODO: non efficient implementation, refactor this code
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < this.Length; i++)
            {
                if (this.chars[i] == '\\')
                {
                    result.Append('\\');
                }
                else
                {
                    foreach (char character in enabledChars)
                    {
                        if (this.chars[i] == character && this.wasEscaped[i])
                        {
                            result.Append('\\');
                            break;
                        }
                    }
                }

                result.Append(this.chars[i]);
            }
            return new StringCharSequence(result.ToString());
        }

        public bool WasEscaped(int index)
        {
            return this.wasEscaped[index];
        }

        public static bool WasEscaped(ICharSequence text, int index)
        {
            if (text is UnescapedCharSequence unescapedCharSequence)
                return unescapedCharSequence.wasEscaped[index];
            else return false;
        }

        public static ICharSequence ToLower(ICharSequence text, CultureInfo locale)
        {
            var lowercaseText = locale.TextInfo.ToLower(text.ToString());

            if (text is UnescapedCharSequence unescapedCharSequence)
            {
                char[] chars = lowercaseText.ToCharArray();
                bool[] wasEscaped = unescapedCharSequence.wasEscaped;
                return new UnescapedCharSequence(chars, wasEscaped, 0, chars.Length);
            }
            else
                return new UnescapedCharSequence(lowercaseText);
        }
    }
}
