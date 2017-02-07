using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.Support;
using System;
using System.Globalization;
using System.Text;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Parser
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
    /// Implementation of <see cref="IEscapeQuerySyntax"/> for the standard lucene
    /// syntax.
    /// </summary>
    public class EscapeQuerySyntax : IEscapeQuerySyntax
    {
        private static readonly char[] wildcardChars = { '*', '?' };

        private static readonly string[] escapableTermExtraFirstChars = { "+", "-", "@" };

        private static readonly string[] escapableTermChars = { "\"", "<", ">", "=",
            "!", "(", ")", "^", "[", "{", ":", "]", "}", "~", "/" };

        // TODO: check what to do with these "*", "?", "\\"
        private static readonly string[] escapableQuotedChars = { "\"" };
        private static readonly string[] escapableWhiteChars = { " ", "\t", "\n", "\r",
            "\f", "\b", "\u3000" };
        private static readonly string[] escapableWordTokens = { "AND", "OR", "NOT",
            "TO", "WITHIN", "SENTENCE", "PARAGRAPH", "INORDER" };

        private static ICharSequence EscapeChar(ICharSequence str, CultureInfo locale)
        {
            if (str == null || str.Length == 0)
                return str;

            ICharSequence buffer = str;

            // regular escapable Char for terms
            for (int i = 0; i < escapableTermChars.Length; i++)
            {
                buffer = ReplaceIgnoreCase(buffer, locale.TextInfo.ToLower(escapableTermChars[i]),
                    "\\", locale);
            }

            // First Character of a term as more escaping chars
            for (int i = 0; i < escapableTermExtraFirstChars.Length; i++)
            {
                if (buffer[0] == escapableTermExtraFirstChars[i][0])
                {
                    buffer = new StringCharSequenceWrapper("\\" + buffer[0]
                        + buffer.SubSequence(1, buffer.Length).ToString());
                    break;
                }
            }

            return buffer;
        }

        private ICharSequence EscapeQuoted(ICharSequence str, CultureInfo locale)
        {
            if (str == null || str.Length == 0)
                return str;

            ICharSequence buffer = str;

            for (int i = 0; i < escapableQuotedChars.Length; i++)
            {
                buffer = ReplaceIgnoreCase(buffer, locale.TextInfo.ToLower(escapableTermChars[i]),
                    "\\", locale);
            }
            return buffer;
        }

        private static ICharSequence EscapeTerm(ICharSequence term, CultureInfo locale)
        {
            if (term == null)
                return term;

            // Escape single Chars
            term = EscapeChar(term, locale);
            term = EscapeWhiteChar(term, locale);

            // Escape Parser Words
            for (int i = 0; i < escapableWordTokens.Length; i++)
            {
                if (escapableWordTokens[i].Equals(term.ToString(), StringComparison.OrdinalIgnoreCase))
                    return new StringCharSequenceWrapper("\\" + term);
            }
            return term;
        }

        /// <summary>
        /// replace with ignore case
        /// </summary>
        /// <param name="string">string to get replaced</param>
        /// <param name="sequence1">the old character sequence in lowercase</param>
        /// <param name="escapeChar">the new character to prefix sequence1 in return string.</param>
        /// <param name="locale"></param>
        /// <returns>the new <see cref="ICharSequence"/></returns>
        private static ICharSequence ReplaceIgnoreCase(ICharSequence @string,
            string sequence1, string escapeChar, CultureInfo locale)
        {
            if (escapeChar == null || sequence1 == null || @string == null)
                throw new NullReferenceException(); // LUCNENET TODO: ArgumentException...

            // empty string case
            int count = @string.Length;
            int sequence1Length = sequence1.Length;
            if (sequence1Length == 0)
            {
                StringBuilder result2 = new StringBuilder((count + 1)
                    * escapeChar.Length);
                result2.Append(escapeChar);
                for (int i = 0; i < count; i++)
                {
                    result2.Append(@string[i]);
                    result2.Append(escapeChar);
                }
                return result2.ToString().ToCharSequence();
            }

            // normal case
            StringBuilder result = new StringBuilder();
            char first = sequence1[0];
            int start = 0, copyStart = 0, firstIndex;
            while (start < count)
            {
                if ((firstIndex = locale.TextInfo.ToLower(@string.ToString()).IndexOf(first,
                    start)) == -1)
                    break;
                bool found = true;
                if (sequence1.Length > 1)
                {
                    if (firstIndex + sequence1Length > count)
                        break;
                    for (int i = 1; i < sequence1Length; i++)
                    {
                        if (locale.TextInfo.ToLower(@string.ToString())[firstIndex + i] != sequence1[i])
                        {
                            found = false;
                            break;
                        }
                    }
                }
                if (found)
                {
                    result.Append(@string.ToString().Substring(copyStart, firstIndex - copyStart));
                    result.Append(escapeChar);
                    result.Append(@string.ToString().Substring(firstIndex,
                        (firstIndex + sequence1Length) - firstIndex));
                    copyStart = start = firstIndex + sequence1Length;
                }
                else
                {
                    start = firstIndex + 1;
                }
            }
            
            if (result.Length == 0 && copyStart == 0)
                return @string;
            result.Append(@string.ToString().Substring(copyStart));
            return result.ToString().ToCharSequence();
        }

        /// <summary>
        /// escape all tokens that are part of the parser syntax on a given string
        /// </summary>
        /// <param name="str">string to get replaced</param>
        /// <param name="locale">locale to be used when performing string compares</param>
        /// <returns>the new <see cref="ICharSequence"/></returns>
        private static ICharSequence EscapeWhiteChar(ICharSequence str,
            CultureInfo locale)
        {
            if (str == null || str.Length == 0)
                return str;

            ICharSequence buffer = str;

            for (int i = 0; i < escapableWhiteChars.Length; i++)
            {
                buffer = ReplaceIgnoreCase(buffer, locale.TextInfo.ToLower(escapableWhiteChars[i]),
                    "\\", locale);
            }
            return buffer;
        }

        // LUCENENET specific overload for text as string
        public virtual string Escape(string text, CultureInfo locale, EscapeQuerySyntaxType type)
        {
            if (text == null || text.Length == 0)
                return text;

            return Escape(text.ToCharSequence(), locale, type).ToString();
        }

        public virtual ICharSequence Escape(ICharSequence text, CultureInfo locale, EscapeQuerySyntaxType type)  
        {
            if (text == null || text.Length == 0)
                return text;

            // escape wildcards and the escape char (this has to be perform before
            // anything else)
            // since we need to preserve the UnescapedCharSequence and escape the
            // original escape chars
            if (text is UnescapedCharSequence)
            {
                text = ((UnescapedCharSequence)text).ToStringEscaped(wildcardChars);
            }
            else
            {
                text = new UnescapedCharSequence(text).ToStringEscaped(wildcardChars);
            }

            if (type == EscapeQuerySyntaxType.STRING)
            {
                return EscapeQuoted(text, locale);
            }
            else
            {
                return EscapeTerm(text, locale);
            }
        }

        /// <summary>
        /// Returns a string where the escape char has been removed, or kept only once
        /// if there was a double escape.
        /// <para/>
        /// Supports escaped unicode characters, e. g. translates <c>A</c> to
        /// <c>A</c>.
        /// </summary>
        public static UnescapedCharSequence DiscardEscapeChar(string input)
        {
            // Create char array to hold unescaped char sequence
            char[] output = new char[input.Length];
            bool[] wasEscaped = new bool[input.Length];

            // The length of the output can be less than the input
            // due to discarded escape chars. This variable holds
            // the actual length of the output
            int length = 0;

            // We remember whether the last processed character was
            // an escape character
            bool lastCharWasEscapeChar = false;

            // The multiplier the current unicode digit must be multiplied with.
            // E. g. the first digit must be multiplied with 16^3, the second with
            // 16^2...
            int codePointMultiplier = 0;

            // Used to calculate the codepoint of the escaped unicode character
            int codePoint = 0;

            for (int i = 0; i < input.Length; i++)
            {
                char curChar = input[i];
                if (codePointMultiplier > 0)
                {
                    codePoint += HexToInt32(curChar) * codePointMultiplier;
                    codePointMultiplier = (int)((uint)codePointMultiplier >> 4);
                    if (codePointMultiplier == 0)
                    {
                        output[length++] = (char)codePoint;
                        codePoint = 0;
                    }
                }
                else if (lastCharWasEscapeChar)
                {
                    if (curChar == 'u')
                    {
                        // found an escaped unicode character
                        codePointMultiplier = 16 * 16 * 16;
                    }
                    else
                    {
                        // this character was escaped
                        output[length] = curChar;
                        wasEscaped[length] = true;
                        length++;
                    }
                    lastCharWasEscapeChar = false;
                }
                else
                {
                    if (curChar == '\\')
                    {
                        lastCharWasEscapeChar = true;
                    }
                    else
                    {
                        output[length] = curChar;
                        length++;
                    }
                }
            }

            if (codePointMultiplier > 0)
            {
                throw new ParseException(new Message(
                    QueryParserMessages.INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION));
            }

            if (lastCharWasEscapeChar)
            {
                throw new ParseException(new Message(
                    QueryParserMessages.INVALID_SYNTAX_ESCAPE_CHARACTER));
            }

            return new UnescapedCharSequence(output, wasEscaped, 0, length);
        }

        /// <summary>
        /// Returns the numeric value of the hexadecimal character
        /// <para/>
        /// NOTE: This was hexToInt() in Lucene
        /// </summary>
        private static int HexToInt32(char c)
        {
            if ('0' <= c && c <= '9')
            {
                return c - '0';
            }
            else if ('a' <= c && c <= 'f')
            {
                return c - 'a' + 10;
            }
            else if ('A' <= c && c <= 'F')
            {
                return c - 'A' + 10;
            }
            else
            {
                throw new ParseException(new Message(
                    QueryParserMessages.INVALID_SYNTAX_ESCAPE_NONE_HEX_UNICODE, c));
            }
        }
    }
}
