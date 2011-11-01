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
using Lucene.Net.Analysis;

namespace Lucene.Net.Analysis.Ru
{
	/// <summary>
	/// A filter that stems Russian words. The implementation was inspired by GermanStemFilter.
	/// The input should be filtered by RussianLowerCaseFilter before passing it to RussianStemFilter,
	/// because RussianStemFilter only works  with lowercase part of any "russian" charset.
	/// </summary>
	public sealed class RussianStemFilter : TokenFilter
	{
		/// <summary>
		/// The actual token in the input stream.
		/// </summary>
		private Token token = null;
		private RussianStemmer stemmer = null;

		public RussianStemFilter(TokenStream _in, char[] charset) : base(_in)
		{
			stemmer = new RussianStemmer(charset);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>Returns the next token in the stream, or null at EOS</returns>
		public override Token Next() 
		{
			if ((token = input.Next()) == null)
			{
				return null;
			}
			else
			{
				String s = stemmer.Stem(token.TermText());
				if (!s.Equals(token.TermText()))
				{
					return new Token(s, token.StartOffset(), token.EndOffset(),
						token.Type());
				}
				return token;
			}
		}

		/// <summary>
		/// Set a alternative/custom RussianStemmer for this filter.
		/// </summary>
		/// <param name="stemmer"></param>
		public void SetStemmer(RussianStemmer stemmer)
		{
			if (stemmer != null)
			{
				this.stemmer = stemmer;
			}
		}
	}
}