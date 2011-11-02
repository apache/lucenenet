/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.IO;
using System.Collections;

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
		private Hashtable exclusions = null;
    
		public GermanStemFilter( TokenStream _in ) : base(_in)
		{
			stemmer = new GermanStemmer();
		}
    
		/// <summary>
		/// Builds a GermanStemFilter that uses an exclusiontable. 
		/// </summary>
		/// <param name="_in"></param>
		/// <param name="exclusiontable"></param>
		public GermanStemFilter( TokenStream _in, Hashtable exclusiontable ): this(_in)
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
		public void SetExclusionTable( Hashtable exclusiontable )
		{
			exclusions = exclusiontable;
		}
	}
}