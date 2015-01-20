/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using Lucene.Net.Queryparser.Classic;
using Sharpen;

namespace Lucene.Net.Queryparser.Classic
{
	/// <summary>This exception is thrown when parse errors are encountered.</summary>
	/// <remarks>
	/// This exception is thrown when parse errors are encountered.
	/// You can explicitly create objects of this exception type by
	/// calling the method generateParseException in the generated
	/// parser.
	/// You can modify this class to customize your error reporting
	/// mechanisms so long as you retain the public fields.
	/// </remarks>
	[System.Serializable]
	public class ParseException : Exception
	{
		/// <summary>The version identifier for this Serializable class.</summary>
		/// <remarks>
		/// The version identifier for this Serializable class.
		/// Increment only if the <i>serialized</i> form of the
		/// class changes.
		/// </remarks>
		private const long serialVersionUID = 1L;

		/// <summary>
		/// This constructor is used by the method "generateParseException"
		/// in the generated parser.
		/// </summary>
		/// <remarks>
		/// This constructor is used by the method "generateParseException"
		/// in the generated parser.  Calling this constructor generates
		/// a new object of this type with the fields "currentToken",
		/// "expectedTokenSequences", and "tokenImage" set.
		/// </remarks>
		public ParseException(Token currentTokenVal, int[][] expectedTokenSequencesVal, string
			[] tokenImageVal) : base(Initialise(currentTokenVal, expectedTokenSequencesVal, 
			tokenImageVal))
		{
			currentToken = currentTokenVal;
			expectedTokenSequences = expectedTokenSequencesVal;
			tokenImage = tokenImageVal;
		}

		/// <summary>
		/// The following constructors are for use by you for whatever
		/// purpose you can think of.
		/// </summary>
		/// <remarks>
		/// The following constructors are for use by you for whatever
		/// purpose you can think of.  Constructing the exception in this
		/// manner makes the exception behave in the normal way - i.e., as
		/// documented in the class "Throwable".  The fields "errorToken",
		/// "expectedTokenSequences", and "tokenImage" do not contain
		/// relevant information.  The JavaCC generated code does not use
		/// these constructors.
		/// </remarks>
		public ParseException() : base()
		{
		}

		/// <summary>Constructor with message.</summary>
		/// <remarks>Constructor with message.</remarks>
		public ParseException(string message) : base(message)
		{
		}

		/// <summary>This is the last token that has been consumed successfully.</summary>
		/// <remarks>
		/// This is the last token that has been consumed successfully.  If
		/// this object has been created due to a parse error, the token
		/// followng this token will (therefore) be the first error token.
		/// </remarks>
		public Token currentToken;

		/// <summary>Each entry in this array is an array of integers.</summary>
		/// <remarks>
		/// Each entry in this array is an array of integers.  Each array
		/// of integers represents a sequence of tokens (by their ordinal
		/// values) that is expected at this point of the parse.
		/// </remarks>
		public int[][] expectedTokenSequences;

		/// <summary>
		/// This is a reference to the "tokenImage" array of the generated
		/// parser within which the parse error occurred.
		/// </summary>
		/// <remarks>
		/// This is a reference to the "tokenImage" array of the generated
		/// parser within which the parse error occurred.  This array is
		/// defined in the generated ...Constants interface.
		/// </remarks>
		public string[] tokenImage;

		/// <summary>
		/// It uses "currentToken" and "expectedTokenSequences" to generate a parse
		/// error message and returns it.
		/// </summary>
		/// <remarks>
		/// It uses "currentToken" and "expectedTokenSequences" to generate a parse
		/// error message and returns it.  If this object has been created
		/// due to a parse error, and you do not catch it (it gets thrown
		/// from the parser) the correct error message
		/// gets displayed.
		/// </remarks>
		private static string Initialise(Token currentToken, int[][] expectedTokenSequences
			, string[] tokenImage)
		{
			string eol = Runtime.GetProperty("line.separator", "\n");
			StringBuilder expected = new StringBuilder();
			int maxSize = 0;
			for (int i = 0; i < expectedTokenSequences.Length; i++)
			{
				if (maxSize < expectedTokenSequences[i].Length)
				{
					maxSize = expectedTokenSequences[i].Length;
				}
				for (int j = 0; j < expectedTokenSequences[i].Length; j++)
				{
					expected.Append(tokenImage[expectedTokenSequences[i][j]]).Append(' ');
				}
				if (expectedTokenSequences[i][expectedTokenSequences[i].Length - 1] != 0)
				{
					expected.Append("...");
				}
				expected.Append(eol).Append("    ");
			}
			string retval = "Encountered \"";
			Token tok = currentToken.next;
			for (int i_1 = 0; i_1 < maxSize; i_1++)
			{
				if (i_1 != 0)
				{
					retval += " ";
				}
				if (tok.kind == 0)
				{
					retval += tokenImage[0];
					break;
				}
				retval += " " + tokenImage[tok.kind];
				retval += " \"";
				retval += Add_escapes(tok.image);
				retval += " \"";
				tok = tok.next;
			}
			retval += "\" at line " + currentToken.next.beginLine + ", column " + currentToken
				.next.beginColumn;
			retval += "." + eol;
			if (expectedTokenSequences.Length == 1)
			{
				retval += "Was expecting:" + eol + "    ";
			}
			else
			{
				retval += "Was expecting one of:" + eol + "    ";
			}
			retval += expected.ToString();
			return retval;
		}

		/// <summary>The end of line string for this machine.</summary>
		/// <remarks>The end of line string for this machine.</remarks>
		protected internal string eol = Runtime.GetProperty("line.separator", "\n");

		/// <summary>
		/// Used to convert raw characters to their escaped version
		/// when these raw version cannot be used as part of an ASCII
		/// string literal.
		/// </summary>
		/// <remarks>
		/// Used to convert raw characters to their escaped version
		/// when these raw version cannot be used as part of an ASCII
		/// string literal.
		/// </remarks>
		internal static string Add_escapes(string str)
		{
			StringBuilder retval = new StringBuilder();
			char ch;
			for (int i = 0; i < str.Length; i++)
			{
				switch (str[i])
				{
					case 0:
					{
						continue;
						goto case '\b';
					}

					case '\b':
					{
						retval.Append("\\b");
						continue;
						goto case '\t';
					}

					case '\t':
					{
						retval.Append("\\t");
						continue;
						goto case '\n';
					}

					case '\n':
					{
						retval.Append("\\n");
						continue;
						goto case '\f';
					}

					case '\f':
					{
						retval.Append("\\f");
						continue;
						goto case '\r';
					}

					case '\r':
					{
						retval.Append("\\r");
						continue;
						goto case '\"';
					}

					case '\"':
					{
						retval.Append("\\\"");
						continue;
						goto case '\'';
					}

					case '\'':
					{
						retval.Append("\\\'");
						continue;
						goto case '\\';
					}

					case '\\':
					{
						retval.Append("\\\\");
						continue;
						goto default;
					}

					default:
					{
						if ((ch = str[i]) < unchecked((int)(0x20)) || ch > unchecked((int)(0x7e)))
						{
							string s = "0000" + Sharpen.Extensions.ToString(ch, 16);
							retval.Append("\\u" + Sharpen.Runtime.Substring(s, s.Length - 4, s.Length));
						}
						else
						{
							retval.Append(ch);
						}
						continue;
						break;
					}
				}
			}
			return retval.ToString();
		}
	}
}
