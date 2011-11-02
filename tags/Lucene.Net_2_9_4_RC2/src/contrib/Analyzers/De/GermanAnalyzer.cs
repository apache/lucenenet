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
using Lucene.Net.Analysis;

namespace Lucene.Net.Analysis.De
{
	/// <summary>
	/// Analyzer for German language. Supports an external list of stopwords (words that
	/// will not be indexed at all) and an external list of exclusions (word that will
	/// not be stemmed, but indexed).
	/// A default set of stopwords is used unless an alternative list is specified, the
	/// exclusion list is empty by default.
	/// </summary>
	public class GermanAnalyzer : Analyzer
	{
		/// <summary>
		/// List of typical german stopwords.
		/// </summary>
		private String[] GERMAN_STOP_WORDS = 
		{
			"einer", "eine", "eines", "einem", "einen",
			"der", "die", "das", "dass", "daß",
			"du", "er", "sie", "es",
			"was", "wer", "wie", "wir",
			"und", "oder", "ohne", "mit",
			"am", "im", "in", "aus", "auf",
			"ist", "sein", "war", "wird",
			"ihr", "ihre", "ihres",
			"als", "für", "von",
			"dich", "dir", "mich", "mir",
			"mein", "kein",
			"durch", "wegen"
		};

		/// <summary>
		/// Contains the stopwords used with the StopFilter. 
		/// </summary>
		private Hashtable stoptable = new Hashtable();

		/// <summary>
		/// Contains words that should be indexed but not stemmed. 
		/// </summary>
		private Hashtable excltable = new Hashtable();

		/// <summary>
		/// Builds an analyzer. 
		/// </summary>
		public GermanAnalyzer()
		{
			stoptable = StopFilter.MakeStopSet( GERMAN_STOP_WORDS );
		}

		/// <summary>
		/// Builds an analyzer with the given stop words. 
		/// </summary>
		/// <param name="stopwords"></param>
		public GermanAnalyzer( String[] stopwords )
		{
			stoptable = StopFilter.MakeStopSet( stopwords );
		}

		/// <summary>
		/// Builds an analyzer with the given stop words. 
		/// </summary>
		/// <param name="stopwords"></param>
		public GermanAnalyzer( Hashtable stopwords )
		{
			stoptable = stopwords;
		}

		/// <summary>
		/// Builds an analyzer with the given stop words. 
		/// </summary>
		/// <param name="stopwords"></param>
		public GermanAnalyzer( FileInfo stopwords )
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
		/// Creates a TokenStream which tokenizes all the text in the provided TextReader. 
		/// </summary>
		/// <param name="fieldName"></param>
		/// <param name="reader"></param>
		/// <returns>A TokenStream build from a StandardTokenizer filtered with StandardFilter, StopFilter, GermanStemFilter</returns>
		public override TokenStream TokenStream(String fieldName, TextReader reader)
		{
			TokenStream result = new StandardTokenizer( reader );
			result = new StandardFilter( result );
			result = new LowerCaseFilter(result);
			result = new StopFilter( result, stoptable );
			result = new GermanStemFilter( result, excltable );
			return result;
		}
	}
}