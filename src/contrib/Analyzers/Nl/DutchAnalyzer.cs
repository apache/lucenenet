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
using Lucene.Net.Analysis.Standard;

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
	/// Analyzer for Dutch language. Supports an external list of stopwords (words that
	/// will not be indexed at all), an external list of exclusions (word that will
	/// not be stemmed, but indexed) and an external list of word-stem pairs that overrule
	/// the algorithm (dictionary stemming).
	/// A default set of stopwords is used unless an alternative list is specified, the
	/// exclusion list is empty by default. 
	/// <version>$Id: DutchAnalyzer.java,v 1.1 2004/03/09 14:55:08 otis Exp $</version>
	/// </summary>
	/// <author>Edwin de Jonge</author>
	public class DutchAnalyzer : Analyzer
	{
		/// <summary>
		/// List of typical german stopwords.
		/// </summary>
		public static string[] DUTCH_STOP_WORDS = 
		{
       "de","en","van","ik","te","dat","die","in","een",
       "hij","het","niet","zijn","is","was","op","aan","met","als","voor","had",
       "er","maar","om","hem","dan","zou","of","wat","mijn","men","dit","zo",
       "door","over","ze","zich","bij","ook","tot","je","mij","uit","der","daar",
       "haar","naar","heb","hoe","heeft","hebben","deze","u","want","nog","zal",
       "me","zij","nu","ge","geen","omdat","iets","worden","toch","al","waren",
       "veel","meer","doen","toen","moet","ben","zonder","kan","hun","dus",
       "alles","onder","ja","eens","hier","wie","werd","altijd","doch","wordt",
       "wezen","kunnen","ons","zelf","tegen","na","reeds","wil","kon","niets",
       "uw","iemand","geweest","andere"		
		};
		/// <summary>
		/// Contains the stopwords used with the StopFilter. 
		/// </summary>
		private Hashtable stoptable = new Hashtable();

		/// <summary>
		/// Contains words that should be indexed but not stemmed. 
		/// </summary>
		private Hashtable excltable = new Hashtable();

		private Hashtable _stemdict = new Hashtable();

		/// <summary>
		/// Builds an analyzer. 
		/// </summary>
		public DutchAnalyzer()
		{
			stoptable = StopFilter.MakeStopSet( DUTCH_STOP_WORDS );
			_stemdict.Add("fiets","fiets"); //otherwise fiet
			_stemdict.Add("bromfiets","bromfiets"); //otherwise bromfiet
			_stemdict.Add("ei","eier"); 
			_stemdict.Add("kind","kinder");
		}

		/// <summary>
		/// Builds an analyzer with the given stop words. 
		/// </summary>
		/// <param name="stopwords"></param>
		public DutchAnalyzer( String[] stopwords )
		{
			stoptable = StopFilter.MakeStopSet( stopwords );
		}

		/// <summary>
		/// Builds an analyzer with the given stop words. 
		/// </summary>
		/// <param name="stopwords"></param>
		public DutchAnalyzer( Hashtable stopwords )
		{
			stoptable = stopwords;
		}

		/// <summary>
		/// Builds an analyzer with the given stop words. 
		/// </summary>
		/// <param name="stopwords"></param>
		public DutchAnalyzer( FileInfo stopwords )
		{
			stoptable = WordlistLoader.GetWordtable( stopwords );
		}

		/// <summary>
		/// Builds an exclusionlist from an array of Strings. 
		/// </summary>
		/// <param name="exclusionlist"></param>
		public void SetStemExclusionTable( String[] exclusionlist )
		{
			excltable = StopFilter.MakeStopSet( exclusionlist );
		}

		/// <summary>
		/// Builds an exclusionlist from a Hashtable. 
		/// </summary>
		/// <param name="exclusionlist"></param>
		public void SetStemExclusionTable( Hashtable exclusionlist )
		{
			excltable = exclusionlist;
		}

		/// <summary>
		/// Builds an exclusionlist from the words contained in the given file. 
		/// </summary>
		/// <param name="exclusionlist"></param>
		public void SetStemExclusionTable(FileInfo exclusionlist)
		{
			excltable = WordlistLoader.GetWordtable(exclusionlist);
		}

		/// <summary>
		/// Reads a stemdictionary file , that overrules the stemming algorithm
		/// This is a textfile that contains per line
		/// word\tstem
		/// i.e: tabseperated
		/// </summary>
		/// <param name="stemdict"></param>
		public void SetStemDictionary(FileInfo stemdict)
		{
			_stemdict = WordlistLoader.GetStemDict(stemdict);
		}

		/// <summary>
		/// Creates a TokenStream which tokenizes all the text in the provided TextReader. 
		/// </summary>
		/// <param name="fieldName"></param>
		/// <param name="reader"></param>
		/// <returns>A TokenStream build from a StandardTokenizer filtered with StandardFilter, StopFilter, GermanStemFilter</returns>
		public override TokenStream TokenStream(String fieldName, TextReader reader)
		{
			TokenStream result = new StandardTokenizer( reader );
			result = new StandardFilter( result );
			result = new StopFilter( result, stoptable );
			result = new DutchStemFilter( result, excltable, _stemdict);
			return result;
		}
	}
}