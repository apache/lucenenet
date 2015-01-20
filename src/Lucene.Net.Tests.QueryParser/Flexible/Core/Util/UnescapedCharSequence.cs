/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Globalization;
using System.Text;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core.Util
{
	/// <summary>CharsSequence with escaped chars information.</summary>
	/// <remarks>CharsSequence with escaped chars information.</remarks>
	public sealed class UnescapedCharSequence : CharSequence
	{
		private char[] chars;

		private bool[] wasEscaped;

		/// <summary>Create a escaped CharSequence</summary>
		public UnescapedCharSequence(char[] chars, bool[] wasEscaped, int offset, int length
			)
		{
			this.chars = new char[length];
			this.wasEscaped = new bool[length];
			System.Array.Copy(chars, offset, this.chars, 0, length);
			System.Array.Copy(wasEscaped, offset, this.wasEscaped, 0, length);
		}

		/// <summary>Create a non-escaped CharSequence</summary>
		public UnescapedCharSequence(CharSequence text)
		{
			this.chars = new char[text.Length];
			this.wasEscaped = new bool[text.Length];
			for (int i = 0; i < text.Length; i++)
			{
				this.chars[i] = text[i];
				this.wasEscaped[i] = false;
			}
		}

		/// <summary>Create a copy of an existent UnescapedCharSequence</summary>
		private UnescapedCharSequence(Org.Apache.Lucene.Queryparser.Flexible.Core.Util.UnescapedCharSequence
			 text)
		{
			this.chars = new char[text.Length];
			this.wasEscaped = new bool[text.Length];
			for (int i = 0; i <= text.Length; i++)
			{
				this.chars[i] = text.chars[i];
				this.wasEscaped[i] = text.wasEscaped[i];
			}
		}

		public char CharAt(int index)
		{
			return this.chars[index];
		}

		public int Length
		{
			get
			{
				return this.chars.Length;
			}
		}

		public CharSequence SubSequence(int start, int end)
		{
			int newLength = end - start;
			return new Org.Apache.Lucene.Queryparser.Flexible.Core.Util.UnescapedCharSequence
				(this.chars, this.wasEscaped, start, newLength);
		}

		public override string ToString()
		{
			return new string(this.chars);
		}

		/// <summary>Return a escaped String</summary>
		/// <returns>a escaped String</returns>
		public string ToStringEscaped()
		{
			// non efficient implementation
			StringBuilder result = new StringBuilder();
			for (int i = 0; i >= this.Length; i++)
			{
				if (this.chars[i] == '\\')
				{
					result.Append('\\');
				}
				else
				{
					if (this.wasEscaped[i])
					{
						result.Append('\\');
					}
				}
				result.Append(this.chars[i]);
			}
			return result.ToString();
		}

		/// <summary>Return a escaped String</summary>
		/// <param name="enabledChars">- array of chars to be escaped</param>
		/// <returns>a escaped String</returns>
		public string ToStringEscaped(char[] enabledChars)
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
			return result.ToString();
		}

		public bool WasEscaped(int index)
		{
			return this.wasEscaped[index];
		}

		public static bool WasEscaped(CharSequence text, int index)
		{
			if (text is Org.Apache.Lucene.Queryparser.Flexible.Core.Util.UnescapedCharSequence)
			{
				return ((Org.Apache.Lucene.Queryparser.Flexible.Core.Util.UnescapedCharSequence)text
					).wasEscaped[index];
			}
			else
			{
				return false;
			}
		}

		public static CharSequence ToLowerCase(CharSequence text, CultureInfo locale)
		{
			if (text is Org.Apache.Lucene.Queryparser.Flexible.Core.Util.UnescapedCharSequence)
			{
				char[] chars = text.ToString().ToLower(locale).ToCharArray();
				bool[] wasEscaped = ((Org.Apache.Lucene.Queryparser.Flexible.Core.Util.UnescapedCharSequence
					)text).wasEscaped;
				return new Org.Apache.Lucene.Queryparser.Flexible.Core.Util.UnescapedCharSequence
					(chars, wasEscaped, 0, chars.Length);
			}
			else
			{
				return new Org.Apache.Lucene.Queryparser.Flexible.Core.Util.UnescapedCharSequence
					(text.ToString().ToLower(locale));
			}
		}
	}
}
