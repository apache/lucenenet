/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Globalization;
using System.Text;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Messages;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Parser;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Util;
using Org.Apache.Lucene.Queryparser.Flexible.Messages;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Parser;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Parser
{
	/// <summary>
	/// Implementation of
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.EscapeQuerySyntax">Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.EscapeQuerySyntax
	/// 	</see>
	/// for the standard lucene
	/// syntax.
	/// </summary>
	public class EscapeQuerySyntaxImpl : EscapeQuerySyntax
	{
		private static readonly char[] wildcardChars = new char[] { '*', '?' };

		private static readonly string[] escapableTermExtraFirstChars = new string[] { "+"
			, "-", "@" };

		private static readonly string[] escapableTermChars = new string[] { "\"", "<", ">"
			, "=", "!", "(", ")", "^", "[", "{", ":", "]", "}", "~", "/" };

		private static readonly string[] escapableQuotedChars = new string[] { "\"" };

		private static readonly string[] escapableWhiteChars = new string[] { " ", "\t", 
			"\n", "\r", "\f", "\b", "\u3000" };

		private static readonly string[] escapableWordTokens = new string[] { "AND", "OR"
			, "NOT", "TO", "WITHIN", "SENTENCE", "PARAGRAPH", "INORDER" };

		// TODO: check what to do with these "*", "?", "\\"
		private static CharSequence EscapeChar(CharSequence str, CultureInfo locale)
		{
			if (str == null || str.Length == 0)
			{
				return str;
			}
			CharSequence buffer = str;
			// regular escapable Char for terms
			for (int i = 0; i < escapableTermChars.Length; i++)
			{
				buffer = ReplaceIgnoreCase(buffer, escapableTermChars[i].ToLower(locale), "\\", locale
					);
			}
			// First Character of a term as more escaping chars
			for (int i_1 = 0; i_1 < escapableTermExtraFirstChars.Length; i_1++)
			{
				if (buffer[0] == escapableTermExtraFirstChars[i_1][0])
				{
					buffer = "\\" + buffer[0] + buffer.SubSequence(1, buffer.Length);
					break;
				}
			}
			return buffer;
		}

		private CharSequence EscapeQuoted(CharSequence str, CultureInfo locale)
		{
			if (str == null || str.Length == 0)
			{
				return str;
			}
			CharSequence buffer = str;
			for (int i = 0; i < escapableQuotedChars.Length; i++)
			{
				buffer = ReplaceIgnoreCase(buffer, escapableTermChars[i].ToLower(locale), "\\", locale
					);
			}
			return buffer;
		}

		private static CharSequence EscapeTerm(CharSequence term, CultureInfo locale)
		{
			if (term == null)
			{
				return term;
			}
			// Escape single Chars
			term = EscapeChar(term, locale);
			term = EscapeWhiteChar(term, locale);
			// Escape Parser Words
			for (int i = 0; i < escapableWordTokens.Length; i++)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(escapableWordTokens[i], term.ToString()))
				{
					return "\\" + term;
				}
			}
			return term;
		}

		/// <summary>replace with ignore case</summary>
		/// <param name="string">string to get replaced</param>
		/// <param name="sequence1">the old character sequence in lowercase</param>
		/// <param name="escapeChar">the new character to prefix sequence1 in return string.</param>
		/// <returns>the new String</returns>
		private static CharSequence ReplaceIgnoreCase(CharSequence @string, CharSequence 
			sequence1, CharSequence escapeChar, CultureInfo locale)
		{
			if (escapeChar == null || sequence1 == null || @string == null)
			{
				throw new ArgumentNullException();
			}
			// empty string case
			int count = @string.Length;
			int sequence1Length = sequence1.Length;
			if (sequence1Length == 0)
			{
				StringBuilder result = new StringBuilder((count + 1) * escapeChar.Length);
				result.Append(escapeChar);
				for (int i = 0; i < count; i++)
				{
					result.Append(@string[i]);
					result.Append(escapeChar);
				}
				return result.ToString();
			}
			// normal case
			StringBuilder result_1 = new StringBuilder();
			char first = sequence1[0];
			int start = 0;
			int copyStart = 0;
			int firstIndex;
			while (start < count)
			{
				if ((firstIndex = @string.ToString().ToLower(locale).IndexOf(first, start)) == -1)
				{
					break;
				}
				bool found = true;
				if (sequence1.Length > 1)
				{
					if (firstIndex + sequence1Length > count)
					{
						break;
					}
					for (int i = 1; i < sequence1Length; i++)
					{
						if (@string.ToString().ToLower(locale)[firstIndex + i] != sequence1[i])
						{
							found = false;
							break;
						}
					}
				}
				if (found)
				{
					result_1.Append(Sharpen.Runtime.Substring(@string.ToString(), copyStart, firstIndex
						));
					result_1.Append(escapeChar);
					result_1.Append(Sharpen.Runtime.Substring(@string.ToString(), firstIndex, firstIndex
						 + sequence1Length));
					copyStart = start = firstIndex + sequence1Length;
				}
				else
				{
					start = firstIndex + 1;
				}
			}
			if (result_1.Length == 0 && copyStart == 0)
			{
				return @string;
			}
			result_1.Append(Sharpen.Runtime.Substring(@string.ToString(), copyStart));
			return result_1.ToString();
		}

		/// <summary>escape all tokens that are part of the parser syntax on a given string</summary>
		/// <param name="str">string to get replaced</param>
		/// <param name="locale">locale to be used when performing string compares</param>
		/// <returns>the new String</returns>
		private static CharSequence EscapeWhiteChar(CharSequence str, CultureInfo locale)
		{
			if (str == null || str.Length == 0)
			{
				return str;
			}
			CharSequence buffer = str;
			for (int i = 0; i < escapableWhiteChars.Length; i++)
			{
				buffer = ReplaceIgnoreCase(buffer, escapableWhiteChars[i].ToLower(locale), "\\", 
					locale);
			}
			return buffer;
		}

		public override CharSequence Escape(CharSequence text, CultureInfo locale, EscapeQuerySyntax.Type
			 type)
		{
			if (text == null || text.Length == 0)
			{
				return text;
			}
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
			if (type == EscapeQuerySyntax.Type.STRING)
			{
				return EscapeQuoted(text, locale);
			}
			else
			{
				return EscapeTerm(text, locale);
			}
		}

		/// <summary>
		/// Returns a String where the escape char has been removed, or kept only once
		/// if there was a double escape.
		/// </summary>
		/// <remarks>
		/// Returns a String where the escape char has been removed, or kept only once
		/// if there was a double escape.
		/// Supports escaped unicode characters, e. g. translates <code>A</code> to
		/// <code>A</code>.
		/// </remarks>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Parser.ParseException
		/// 	"></exception>
		public static UnescapedCharSequence DiscardEscapeChar(CharSequence input)
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
					codePointMultiplier = (int)(((uint)codePointMultiplier) >> 4);
					if (codePointMultiplier == 0)
					{
						output[length++] = (char)codePoint;
						codePoint = 0;
					}
				}
				else
				{
					if (lastCharWasEscapeChar)
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
			}
			if (codePointMultiplier > 0)
			{
				throw new ParseException(new MessageImpl(QueryParserMessages.INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION
					));
			}
			if (lastCharWasEscapeChar)
			{
				throw new ParseException(new MessageImpl(QueryParserMessages.INVALID_SYNTAX_ESCAPE_CHARACTER
					));
			}
			return new UnescapedCharSequence(output, wasEscaped, 0, length);
		}

		/// <summary>Returns the numeric value of the hexadecimal character</summary>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Parser.ParseException
		/// 	"></exception>
		private static int HexToInt(char c)
		{
			if ('0' <= c && c <= '9')
			{
				return c - '0';
			}
			else
			{
				if ('a' <= c && c <= 'f')
				{
					return c - 'a' + 10;
				}
				else
				{
					if ('A' <= c && c <= 'F')
					{
						return c - 'A' + 10;
					}
					else
					{
						throw new ParseException(new MessageImpl(QueryParserMessages.INVALID_SYNTAX_ESCAPE_NONE_HEX_UNICODE
							, c));
					}
				}
			}
		}
	}
}
