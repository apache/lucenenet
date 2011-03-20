using System;
using Lucene.Net.Analysis;

namespace Lucene.Net.Analysis.Ru
{
	/// <summary>
	/// Normalizes token text to lower case, analyzing given ("russian") charset.
	/// </summary>
	public sealed class RussianLowerCaseFilter : TokenFilter
	{
		char[] charset;

		public RussianLowerCaseFilter(TokenStream _in, char[] charset) : base(_in)
		{
			this.charset = charset;
		}

		public override Token Next() 
		{
			Token t = input.Next();

			if (t == null)
				return null;

			String txt = t.TermText();

			char[] chArray = txt.ToCharArray();
			for (int i = 0; i < chArray.Length; i++)
			{
				chArray[i] = RussianCharsets.ToLowerCase(chArray[i], charset);
			}

			String newTxt = new String(chArray);
			// create new token
			Token newToken = new Token(newTxt, t.StartOffset(), t.EndOffset());

			return newToken;
		}
	}
}