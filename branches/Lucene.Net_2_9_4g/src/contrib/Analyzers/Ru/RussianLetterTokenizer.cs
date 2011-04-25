using System;
using System.IO;
using Lucene.Net.Analysis;

namespace Lucene.Net.Analysis.Ru
{
	/// <summary>
	/// A RussianLetterTokenizer is a tokenizer that extends LetterTokenizer by additionally looking up letters
	/// in a given "russian charset". The problem with LeterTokenizer is that it uses Character.isLetter() method,
	/// which doesn't know how to detect letters in encodings like CP1252 and KOI8
	/// (well-known problems with 0xD7 and 0xF7 chars)
	/// </summary>
	public class RussianLetterTokenizer : CharTokenizer
	{
		/// <summary>
		/// Construct a new LetterTokenizer.
		/// </summary>
		private char[] charset;

		public RussianLetterTokenizer(TextReader _in, char[] charset) : base(_in)
		{
			this.charset = charset;
		}

		/// <summary>
		/// Collects only characters which satisfy Char.IsLetter(char).
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		protected override bool IsTokenChar(char c)
		{
			if (Char.IsLetter(c))
				return true;
			for (int i = 0; i < charset.Length; i++)
			{
				if (c == charset[i])
					return true;
			}
			return false;
		}
	}
}