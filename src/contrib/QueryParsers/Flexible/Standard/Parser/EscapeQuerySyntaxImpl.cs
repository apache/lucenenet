using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.QueryParsers.Support;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Parser
{
    public class EscapeQuerySyntaxImpl : IEscapeQuerySyntax
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
                buffer = ReplaceIgnoreCase(buffer, escapableTermChars[i].ToLower(locale),
                    "\\", locale);
            }

            // First Character of a term as more escaping chars
            for (int i = 0; i < escapableTermExtraFirstChars.Length; i++)
            {
                if (buffer.CharAt(0) == escapableTermExtraFirstChars[i][0])
                {
                    buffer = new StringCharSequenceWrapper("\\" + buffer.CharAt(0)
                        + buffer.SubSequence(1, buffer.Length));
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
                buffer = ReplaceIgnoreCase(buffer, escapableTermChars[i].ToLower(locale), "\\", locale);
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
                if (escapableWordTokens[i].EqualsIgnoreCase(term.ToString()))
                    return new StringCharSequenceWrapper("\\" + term);
            }
            return term;
        }

        private static ICharSequence ReplaceIgnoreCase(ICharSequence str, string sequence1, string escapeChar, CultureInfo locale)
        {
            if (escapeChar == null || sequence1 == null || str == null)
                throw new NullReferenceException();

            // empty string case
            int count = str.Length;
            int sequence1Length = sequence1.Length;
            if (sequence1Length == 0)
            {
                StringBuilder result = new StringBuilder((count + 1)
                    * escapeChar.Length);
                result.Append(escapeChar);
                for (int i = 0; i < count; i++)
                {
                    result.Append(str.CharAt(i));
                    result.Append(escapeChar);
                }
                return new StringCharSequenceWrapper(result.ToString());
            }

            // normal case
            StringBuilder result2 = new StringBuilder();
            char first = sequence1[0];
            int start = 0, copyStart = 0, firstIndex;
            while (start < count)
            {
                if ((firstIndex = str.ToString().ToLower(locale).IndexOf(first, start)) == -1)
                    break;
                bool found = true;
                if (sequence1.Length > 1)
                {
                    if (firstIndex + sequence1Length > count)
                        break;
                    for (int i = 1; i < sequence1Length; i++)
                    {
                        if (str.ToString().ToLower(locale)[firstIndex + i] != sequence1[i])
                        {
                            found = false;
                            break;
                        }
                    }
                }
                if (found)
                {
                    result2.Append(str.ToString().Substring(copyStart, firstIndex));
                    result2.Append(escapeChar);
                    result2.Append(str.ToString().Substring(firstIndex,
                        firstIndex + sequence1Length));
                    copyStart = start = firstIndex + sequence1Length;
                }
                else
                {
                    start = firstIndex + 1;
                }
            }
            if (result2.Length == 0 && copyStart == 0)
                return str;
            result2.Append(str.ToString().Substring(copyStart));
            return new StringCharSequenceWrapper(result2.ToString());
        }

        private static ICharSequence EscapeWhiteChar(ICharSequence str, CultureInfo locale)
        {
            if (str == null || str.Length == 0)
                return str;

            ICharSequence buffer = str;

            for (int i = 0; i < escapableWhiteChars.Length; i++)
            {
                buffer = ReplaceIgnoreCase(buffer, escapableWhiteChars[i].ToLower(locale), "\\", locale);
            }
            return buffer;
        }

        public ICharSequence Escape(ICharSequence text, CultureInfo locale, EscapeQuerySyntax.Type type)
        {
            if (text == null || text.Length == 0)
                return text;

            // escape wildcards and the escape char (this has to be perform before
            // anything else)
            // since we need to preserve the UnescapedCharSequence and escape the
            // original escape chars
            if (text is UnescapedCharSequence)
            {
                text = new StringCharSequenceWrapper(((UnescapedCharSequence)text).ToStringEscaped(wildcardChars));
            }
            else
            {
                text = new StringCharSequenceWrapper(new UnescapedCharSequence(text).ToStringEscaped(wildcardChars));
            }

            if (type == EscapeQuerySyntax.Type.STRING)
            {
                return EscapeQuoted(text, locale);
            }
            else
            {
                return EscapeTerm(text, locale);
            }
        }

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
                    codePoint += HexToInt(curChar) * codePointMultiplier;
                    codePointMultiplier = Number.URShift(codePointMultiplier, 4);
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
                throw new ParseException(new Message(QueryParserMessages.INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION));
            }

            if (lastCharWasEscapeChar)
            {
                throw new ParseException(new Message(QueryParserMessages.INVALID_SYNTAX_ESCAPE_CHARACTER));
            }

            return new UnescapedCharSequence(output, wasEscaped, 0, length);
        }

        public static UnescapedCharSequence DiscardEscapeChar(ICharSequence input)
        {
            return DiscardEscapeChar(input.ToString());
        }

        private static int HexToInt(char c)
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
                throw new ParseException(new Message(QueryParserMessages.INVALID_SYNTAX_ESCAPE_NONE_HEX_UNICODE, c));
            }
        }
    }
}
