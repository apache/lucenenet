using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.De
{
	/// <summary>
	/// A filter that stems German words. It supports a table of words that should
	/// not be stemmed at all. The stemmer used can be changed at runtime after the
	/// filter object is created (as long as it is a GermanStemmer).
	/// </summary>
	public sealed class GermanStemFilter : TokenFilter
	{
		/// <summary>
		/// The actual token in the input stream.
		/// </summary>
		private Token token = null;
		private GermanStemmer stemmer = null;
        private ICollection<string> exclusions = null;
    
		public GermanStemFilter( TokenStream _in ) : base(_in)
		{
			stemmer = new GermanStemmer();
		}
    
		/// <summary>
		/// Builds a GermanStemFilter that uses an exclusiontable. 
		/// </summary>
		/// <param name="_in"></param>
		/// <param name="exclusiontable"></param>
        public GermanStemFilter(TokenStream _in, ICollection<string> exclusiontable) : this(_in)
		{
			exclusions = exclusiontable;
		}
    
		/// <summary>
		/// </summary>
		/// <returns>Returns the next token in the stream, or null at EOS</returns>
		public override Token Next()
	
		{
			if ( ( token = input.Next() ) == null ) 
			{
				return null;
			}
				// Check the exclusiontable
			else if ( exclusions != null && exclusions.Contains( token.TermText() ) ) 
			{
				return token;
			}
			else 
			{
				String s = stemmer.Stem( token.TermText() );
				// If not stemmed, dont waste the time creating a new token
				if ( !s.Equals( token.TermText() ) ) 
				{
					return new Token( s, token.StartOffset(),
						token.EndOffset(), token.Type() );
				}
				return token;
			}
		}

		/// <summary>
		/// Set a alternative/custom GermanStemmer for this filter. 
		/// </summary>
		/// <param name="stemmer"></param>
		public void SetStemmer( GermanStemmer stemmer )
		{
			if ( stemmer != null ) 
			{
				this.stemmer = stemmer;
			}
		}

		/// <summary>
		/// Set an alternative exclusion list for this filter. 
		/// </summary>
		/// <param name="exclusiontable"></param>
        public void SetExclusionTable(ICollection<string> exclusiontable)
		{
			exclusions = exclusiontable;
		}
	}
}