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

using Lucene.Net.Analysis;

namespace Lucene.Net.Analysis.Standard
{
	
    /// <summary> Filters {@link StandardTokenizer} with {@link StandardFilter}, {@link
    /// LowerCaseFilter} and {@link StopFilter}, using a list of English stop words.
    /// 
    /// </summary>
    /// <version>  $Id: StandardAnalyzer.java 613280 2008-01-18 21:27:10Z gsingers $
    /// </version>
    public class StandardAnalyzer : Analyzer
    {
        private System.Collections.Hashtable stopSet;
		
		/// <summary> Specifies whether deprecated acronyms should be replaced with HOST type.
		/// This is false by default to support backward compatibility.
		/// 
		/// </summary>
		/// <deprecated> this should be removed in the next release (3.0).
		/// 
		/// See https://issues.apache.org/jira/browse/LUCENE-1068
		/// </deprecated>
		private bool replaceInvalidAcronym = false;
		
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
		
		/// <summary> </summary>
		/// <param name="replaceInvalidAcronym">Set to true if this analyzer should replace mischaracterized acronyms in the StandardTokenizer
		/// 
		/// See https://issues.apache.org/jira/browse/LUCENE-1068
		/// 
		/// </param>
		/// <deprecated> Remove in 3.X and make true the only valid value
		/// </deprecated>
		public StandardAnalyzer(bool replaceInvalidAcronym):this(STOP_WORDS)
		{
			this.replaceInvalidAcronym = replaceInvalidAcronym;
		}
		
		/// <param name="stopwords">The stopwords to use
		/// </param>
		/// <param name="replaceInvalidAcronym">Set to true if this analyzer should replace mischaracterized acronyms in the StandardTokenizer
		/// 
		/// See https://issues.apache.org/jira/browse/LUCENE-1068
		/// 
		/// </param>
		/// <deprecated> Remove in 3.X and make true the only valid value
		/// </deprecated>
		public StandardAnalyzer(System.IO.TextReader stopwords, bool replaceInvalidAcronym):this(stopwords)
		{
			this.replaceInvalidAcronym = replaceInvalidAcronym;
		}
		
		/// <param name="stopwords">The stopwords to use
		/// </param>
		/// <param name="replaceInvalidAcronym">Set to true if this analyzer should replace mischaracterized acronyms in the StandardTokenizer
		/// 
		/// See https://issues.apache.org/jira/browse/LUCENE-1068
		/// 
		/// </param>
		/// <deprecated> Remove in 3.X and make true the only valid value
		/// </deprecated>
		public StandardAnalyzer(System.IO.FileInfo stopwords, bool replaceInvalidAcronym):this(stopwords)
		{
			this.replaceInvalidAcronym = replaceInvalidAcronym;
		}
		
		/// <summary> </summary>
		/// <param name="stopwords">The stopwords to use
		/// </param>
		/// <param name="replaceInvalidAcronym">Set to true if this analyzer should replace mischaracterized acronyms in the StandardTokenizer
		/// 
		/// See https://issues.apache.org/jira/browse/LUCENE-1068
		/// 
		/// </param>
		/// <deprecated> Remove in 3.X and make true the only valid value
		/// </deprecated>
		public StandardAnalyzer(System.String[] stopwords, bool replaceInvalidAcronym):this(stopwords)
		{
			this.replaceInvalidAcronym = replaceInvalidAcronym;
		}
		
		/// <param name="stopwords">The stopwords to use
		/// </param>
		/// <param name="replaceInvalidAcronym">Set to true if this analyzer should replace mischaracterized acronyms in the StandardTokenizer
		/// 
		/// See https://issues.apache.org/jira/browse/LUCENE-1068
		/// 
		/// </param>
		/// <deprecated> Remove in 3.X and make true the only valid value
		/// </deprecated>
		public StandardAnalyzer(System.Collections.Hashtable stopwords, bool replaceInvalidAcronym) : this(stopwords)
		{
			this.replaceInvalidAcronym = replaceInvalidAcronym;
		}
		
		/// <summary>Constructs a {@link StandardTokenizer} filtered by a {@link
		/// StandardFilter}, a {@link LowerCaseFilter} and a {@link StopFilter}. 
		/// </summary>
		public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
		{
			StandardTokenizer tokenStream = new StandardTokenizer(reader, replaceInvalidAcronym);
			tokenStream.SetMaxTokenLength(maxTokenLength);
			TokenStream result = new StandardFilter(tokenStream);
			result = new LowerCaseFilter(result);
			result = new StopFilter(result, stopSet);
			return result;
		}
		
		private sealed class SavedStreams
		{
			internal StandardTokenizer tokenStream;
			internal TokenStream filteredTokenStream;
		}
		
		/// <summary>Default maximum allowed token length </summary>
		public const int DEFAULT_MAX_TOKEN_LENGTH = 255;
		
		private int maxTokenLength = DEFAULT_MAX_TOKEN_LENGTH;
		
		/// <summary> Set maximum allowed token length.  If a token is seen
		/// that exceeds this length then it is discarded.  This
		/// setting only takes effect the next time tokenStream or
		/// reusableTokenStream is called.
		/// </summary>
		public virtual void  SetMaxTokenLength(int length)
		{
			maxTokenLength = length;
		}
		
		/// <seealso cref="setMaxTokenLength">
		/// </seealso>
		public virtual int GetMaxTokenLength()
		{
			return maxTokenLength;
		}
		
		public override TokenStream ReusableTokenStream(System.String fieldName, System.IO.TextReader reader)
		{
			SavedStreams streams = (SavedStreams) GetPreviousTokenStream();
			if (streams == null)
			{
				streams = new SavedStreams();
				SetPreviousTokenStream(streams);
				streams.tokenStream = new StandardTokenizer(reader);
				streams.filteredTokenStream = new StandardFilter(streams.tokenStream);
				streams.filteredTokenStream = new LowerCaseFilter(streams.filteredTokenStream);
				streams.filteredTokenStream = new StopFilter(streams.filteredTokenStream, stopSet);
			}
			else
			{
				streams.tokenStream.Reset(reader);
			}
			streams.tokenStream.SetMaxTokenLength(maxTokenLength);
			
			streams.tokenStream.SetReplaceInvalidAcronym(replaceInvalidAcronym);
			
			return streams.filteredTokenStream;
		}
		
		/// <summary> </summary>
		/// <returns> true if this Analyzer is replacing mischaracterized acronyms in the StandardTokenizer
		/// 
		/// See https://issues.apache.org/jira/browse/LUCENE-1068
		/// </returns>
		public virtual bool IsReplaceInvalidAcronym()
		{
			return replaceInvalidAcronym;
		}
		
		/// <summary> </summary>
		/// <param name="replaceInvalidAcronym">Set to true if this Analyzer is replacing mischaracterized acronyms in the StandardTokenizer
		/// 
		/// See https://issues.apache.org/jira/browse/LUCENE-1068
		/// </param>
		public virtual void  SetReplaceInvalidAcronym(bool replaceInvalidAcronym)
		{
			this.replaceInvalidAcronym = replaceInvalidAcronym;
		}
		static StandardAnalyzer()
		{
			STOP_WORDS = StopAnalyzer.ENGLISH_STOP_WORDS;
		}
	}
}