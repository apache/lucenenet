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
		
		private static bool ENABLE_POSITION_INCREMENTS_DEFAULT = false;
		
		private CharArraySet stopWords;
		private bool enablePositionIncrements = ENABLE_POSITION_INCREMENTS_DEFAULT;
		
        /// <summary> Construct a token stream filtering the given input.</summary>
        public StopFilter(TokenStream input, System.String[] stopWords) : this(input, stopWords, false)
        {
        }

        /// <summary> Constructs a filter which removes words from the input
        /// TokenStream that are named in the array of words.
        /// </summary>
		public StopFilter(TokenStream in_Renamed, System.String[] stopWords, bool ignoreCase) : base(in_Renamed)
		{
			this.stopWords = (CharArraySet) MakeStopSet(stopWords, ignoreCase);
		}
		
		
		/// <summary> Construct a token stream filtering the given input.
		/// If <code>stopWords</code> is an instance of {@link CharArraySet} (true if
		/// <code>makeStopSet()</code> was used to construct the set) it will be directly used
		/// and <code>ignoreCase</code> will be ignored since <code>CharArraySet</code>
		/// directly controls case sensitivity.
		/// <p/>
		/// If <code>stopWords</code> is not an instance of {@link CharArraySet},
		/// a new CharArraySet will be constructed and <code>ignoreCase</code> will be
		/// used to specify the case sensitivity of that set.
		/// 
		/// </summary>
		/// <param name="input">
		/// </param>
		/// <param name="stopWords">The set of Stop Words.
		/// </param>
		/// <param name="ignoreCase">-Ignore case when stopping.
		/// </param>
		public StopFilter(TokenStream input, System.Collections.Hashtable stopWords, bool ignoreCase) : base(input)
		{
			if (stopWords is CharArraySet)
			{
				this.stopWords = (CharArraySet) stopWords;
			}
			else
			{
				this.stopWords = new CharArraySet(stopWords.Count, ignoreCase);
				foreach (System.String sw in stopWords.Values)
				{
					this.stopWords.Add(sw);
				}
			}
		}
		
		/// <summary> Constructs a filter which removes words from the input
		/// TokenStream that are named in the Set.
		/// 
		/// </summary>
		/// <seealso cref="MakeStopSet(java.lang.String[])">
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
		/// <seealso cref="MakeStopSet(java.lang.String[], boolean) passing false to ignoreCase">
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
			CharArraySet stopSet = new CharArraySet(stopWords.Length, ignoreCase);
			for (int i = 0; i < stopWords.Length; i++)
			{
				stopSet.Add(stopWords[i]);
			}
			return stopSet;
		}
		
		/// <summary> Returns the next input Token whose termText() is not a stop word.</summary>
		public override Token Next(Token result)
		{
			// return the first non-stop word found
			int skippedPositions = 0;
			while ((result = input.Next(result)) != null)
			{
				if (!stopWords.Contains(result.TermBuffer(), 0, result.termLength))
				{
					if (enablePositionIncrements)
					{
						result.SetPositionIncrement(result.GetPositionIncrement() + skippedPositions);
					}
					return result;
				}
				skippedPositions += result.GetPositionIncrement();
			}
			// reached EOS -- return null
			return null;
		}
		
		/// <seealso cref="setEnablePositionIncrementsDefault(boolean).">
		/// </seealso>
		public static bool GetEnablePositionIncrementsDefault()
		{
			return ENABLE_POSITION_INCREMENTS_DEFAULT;
		}
		
		/// <summary> Set the default position increments behavior of every StopFilter created from now on.
		/// <p>
		/// Note: behavior of a single StopFilter instance can be modified 
		/// with {@link #SetEnablePositionIncrements(boolean)}.
		/// This static method allows control over behavior of classes using StopFilters internally, 
		/// for example {@link Lucene.Net.Analysis.Standard.StandardAnalyzer StandardAnalyzer}. 
		/// <p>
		/// Default : false.
		/// </summary>
		/// <seealso cref="setEnablePositionIncrements(boolean).">
		/// </seealso>
		public static void  SetEnablePositionIncrementsDefault(bool defaultValue)
		{
			ENABLE_POSITION_INCREMENTS_DEFAULT = defaultValue;
		}
		
		/// <seealso cref="setEnablePositionIncrements(boolean).">
		/// </seealso>
		public bool GetEnablePositionIncrements()
		{
			return enablePositionIncrements;
		}
		
		/// <summary> Set to <code>true</code> to make <b>this</b> StopFilter enable position increments to result tokens.
		/// <p>
		/// When set, when a token is stopped (omitted), the position increment of 
		/// the following token is incremented.  
		/// <p>
		/// Default: see {@link #SetEnablePositionIncrementsDefault(boolean)}.
		/// </summary>
		public void  SetEnablePositionIncrements(bool enable)
		{
			this.enablePositionIncrements = enable;
		}
	}
}