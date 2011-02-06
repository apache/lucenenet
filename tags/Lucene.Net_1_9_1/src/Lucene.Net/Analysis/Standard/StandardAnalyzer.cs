/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Analysis;

namespace Lucene.Net.Analysis.Standard
{
	
	/// <summary> Filters {@link StandardTokenizer} with {@link StandardFilter}, {@link
	/// LowerCaseFilter} and {@link StopFilter}, using a list of English stop words.
	/// 
	/// </summary>
	/// <version>  $Id: StandardAnalyzer.java 219090 2005-07-14 20:36:28Z dnaber $
	/// </version>
	public class StandardAnalyzer : Analyzer
	{
		private System.Collections.Hashtable stopSet;
		
		/// <summary>An array containing some common English words that are usually not
		/// useful for searching. 
		/// </summary>
		public static readonly System.String[] STOP_WORDS;
		
		/// <summary>Builds an analyzer with the default stop words ({@link #STOP_WORDS}). </summary>
		public StandardAnalyzer() : this(STOP_WORDS)
		{
		}
		
		/// <summary>Builds an analyzer with the given stop words. </summary>
		public StandardAnalyzer(System.Collections.Hashtable stopWords)
		{
			stopSet = stopWords;
		}
		
		/// <summary>Builds an analyzer with the given stop words. </summary>
		public StandardAnalyzer(System.String[] stopWords)
		{
			stopSet = StopFilter.MakeStopSet(stopWords);
		}
		
		/// <summary>Builds an analyzer with the stop words from the given file.</summary>
		/// <seealso cref="WordlistLoader.GetWordSet(File)">
		/// </seealso>
		public StandardAnalyzer(System.IO.FileInfo stopwords)
		{
			stopSet = WordlistLoader.GetWordSet(stopwords);
		}
		
		/// <summary>Builds an analyzer with the stop words from the given reader.</summary>
		/// <seealso cref="WordlistLoader.GetWordSet(Reader)">
		/// </seealso>
		public StandardAnalyzer(System.IO.TextReader stopwords)
		{
			stopSet = WordlistLoader.GetWordSet(stopwords);
		}
		
		/// <summary>Constructs a {@link StandardTokenizer} filtered by a {@link
		/// StandardFilter}, a {@link LowerCaseFilter} and a {@link StopFilter}. 
		/// </summary>
		public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
		{
			TokenStream result = new StandardTokenizer(reader);
			result = new StandardFilter(result);
			result = new LowerCaseFilter(result);
			result = new StopFilter(result, stopSet);
			return result;
		}
		static StandardAnalyzer()
		{
			STOP_WORDS = StopAnalyzer.ENGLISH_STOP_WORDS;
		}
	}
}