/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
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

namespace Lucene.Net.Analysis
{
	
	/// <summary> Removes stop words from a token stream.</summary>
	
	public sealed class StopFilter : TokenFilter
	{
		
		private System.Collections.Hashtable stopWords;
		private bool ignoreCase;
		
		/// <summary> Construct a token stream filtering the given input.</summary>
		public StopFilter(TokenStream input, System.String[] stopWords) : this(input, stopWords, false)
		{
		}
		
		/// <summary> Constructs a filter which removes words from the input
		/// TokenStream that are named in the array of words.
		/// </summary>
		public StopFilter(TokenStream in_Renamed, System.String[] stopWords, bool ignoreCase) : base(in_Renamed)
		{
			this.ignoreCase = ignoreCase;
			this.stopWords = MakeStopSet(stopWords, ignoreCase);
		}
		

		/// <summary> Construct a token stream filtering the given input.</summary>
		/// <param name="input">
		/// </param>
		/// <param name="stopWords">The set of Stop Words, as Strings.  If ignoreCase is true, all strings should be lower cased
		/// </param>
		/// <param name="ignoreCase">-Ignore case when stopping.  The stopWords set must be setup to contain only lower case words 
		/// </param>
		public StopFilter(TokenStream input, System.Collections.Hashtable stopWords, bool ignoreCase) : base(input)
		{
			this.ignoreCase = ignoreCase;
			this.stopWords = stopWords;
		}
		
		/// <summary> Constructs a filter which removes words from the input
		/// TokenStream that are named in the Set.
		/// It is crucial that an efficient Set implementation is used
		/// for maximum performance.
		/// 
		/// </summary>
		/// <seealso cref="MakeStopSet(String[])">
		/// </seealso>
		public StopFilter(TokenStream in_Renamed, System.Collections.Hashtable stopWords) : this(in_Renamed, stopWords, false)
		{
		}
		
		/// <summary> Builds a Set from an array of stop words,
		/// appropriate for passing into the StopFilter constructor.
		/// This permits this stopWords construction to be cached once when
		/// an Analyzer is constructed.
		/// 
		/// </summary>
		/// <seealso cref="MakeStopSet(String[], boolean) passing false to ignoreCase">
		/// </seealso>
		public static System.Collections.Hashtable MakeStopSet(System.String[] stopWords)
		{
			return MakeStopSet(stopWords, false);
		}
		
		/// <summary> </summary>
		/// <param name="stopWords">
		/// </param>
		/// <param name="ignoreCase">If true, all words are lower cased first.  
		/// </param>
		/// <returns> a Set containing the words
		/// </returns>
		public static System.Collections.Hashtable MakeStopSet(System.String[] stopWords, bool ignoreCase)
		{
			System.Collections.Hashtable stopTable = new System.Collections.Hashtable(stopWords.Length);
            for (int i = 0; i < stopWords.Length; i++)
            {
                System.String tmp = ignoreCase ? stopWords[i].ToLower() : stopWords[i];
                stopTable.Add(tmp, tmp);
            }
			return stopTable;
		}
		
		/// <summary> Returns the next input Token whose termText() is not a stop word.</summary>
		public override Token Next()
		{
			// return the first non-stop word found
			for (Token token = input.Next(); token != null; token = input.Next())
			{
				System.String termText = ignoreCase ? token.termText.ToLower() : token.termText;
				if (!stopWords.Contains(termText))
					return token;
			}
			// reached EOS -- return null
			return null;
		}
	}
}