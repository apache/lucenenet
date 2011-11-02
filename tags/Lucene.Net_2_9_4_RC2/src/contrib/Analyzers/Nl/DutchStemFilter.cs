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

namespace Lucene.Net.Analysis.Nl
{

	/* ====================================================================
	 * The Apache Software License, Version 1.1
	 *
	 * Copyright (c) 2001 The Apache Software Foundation.  All rights
	 * reserved.
	 *
	 * Redistribution and use in source and binary forms, with or without
	 * modification, are permitted provided that the following conditions
	 * are met:
	 *
	 * 1. Redistributions of source code must retain the above copyright
	 *    notice, this list of conditions and the following disclaimer.
	 *
	 * 2. Redistributions in binary form must reproduce the above copyright
	 *    notice, this list of conditions and the following disclaimer in
	 *    the documentation and/or other materials provided with the
	 *    distribution.
	 *
	 * 3. The end-user documentation included with the redistribution,
	 *    if any, must include the following acknowledgment:
	 *       "This product includes software developed by the
	 *        Apache Software Foundation (http://www.apache.org/)."
	 *    Alternately, this acknowledgment may appear in the software itself,
	 *    if and wherever such third-party acknowledgments normally appear.
	 *
	 * 4. The names "Apache" and "Apache Software Foundation" and
	 *    "Apache Lucene" must not be used to endorse or promote products
	 *    derived from this software without prior written permission. For
	 *    written permission, please contact apache@apache.org.
	 *
	 * 5. Products derived from this software may not be called "Apache",
	 *    "Apache Lucene", nor may "Apache" appear in their name, without
	 *    prior written permission of the Apache Software Foundation.
	 *
	 * THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED
	 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
	 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
	 * DISCLAIMED.  IN NO EVENT SHALL THE APACHE SOFTWARE FOUNDATION OR
	 * ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
	 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
	 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
	 * USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
	 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
	 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
	 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
	 * SUCH DAMAGE.
	 * ====================================================================
	 *
	 * This software consists of voluntary contributions made by many
	 * individuals on behalf of the Apache Software Foundation.  For more
	 * information on the Apache Software Foundation, please see
	 * <http://www.apache.org/>.
	 */

	/// <summary>
	/// A filter that stems Dutch words. It supports a table of words that should
	/// not be stemmed at all. The stemmer used can be changed at runtime after the
	/// filter object is created (as long as it is a DutchStemmer).
	/// 
	/// <version>$Id: DutchStemFilter.java,v 1.1 2004/03/09 14:55:08 otis Exp $</version>
	/// </summary>
	/// <author>Edwin de Jonge</author>
	public sealed class DutchStemFilter : TokenFilter
	{
		/// <summary>
		/// The actual token in the input stream.
		/// </summary>
		private Token token = null;
		private DutchStemmer stemmer = null;
		private Hashtable exclusions = null;
    
		public DutchStemFilter( TokenStream _in ) : base(_in)
		{
			stemmer = new DutchStemmer();
		}
    
		/// <summary>
		/// Builds a DutchStemFilter that uses an exclusiontable. 
		/// </summary>
		/// <param name="_in"></param>
		/// <param name="exclusiontable"></param>
		public DutchStemFilter( TokenStream _in, Hashtable exclusiontable ): this(_in)
		{
			exclusions = exclusiontable;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="_in"></param>
		/// <param name="exclusiontable"></param>
		/// <param name="stemdictionary">Dictionary of word stem pairs, that overrule the algorithm</param>
		public DutchStemFilter( TokenStream _in, Hashtable exclusiontable , Hashtable stemdictionary): this(_in, exclusiontable)
		{
			stemmer.SetStemDictionary(stemdictionary);
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
		/// Set a alternative/custom DutchStemmer for this filter. 
		/// </summary>
		/// <param name="stemmer"></param>
		public void SetStemmer( DutchStemmer stemmer )
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

		/// <summary>
		/// Set dictionary for stemming, this dictionary overrules the algorithm,
		/// so you can correct for a particular unwanted word-stem pair.
		/// </summary>
		/// <param name="dict"></param>
		public void SetStemDictionary(Hashtable dict)
		{
			if (stemmer != null)
				stemmer.SetStemDictionary(dict);
		}
	}
}