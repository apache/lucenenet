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
	
    /// <summary>Filters LetterTokenizer with LowerCaseFilter and StopFilter. </summary>
	
    public sealed class StopAnalyzer : Analyzer
    {
        private System.Collections.Hashtable stopWords;
		
        /// <summary>An array containing some common English words that are not usually useful
        /// for searching. 
        /// </summary>
        public static readonly System.String[] ENGLISH_STOP_WORDS = new System.String[]{"a", "an", "and", "are", "as", "at", "be", "but", "by", "for", "if", "in", "into", "is", "it", "no", "not", "of", "on", "or", "such", "that", "the", "their", "then", "there", "these", "they", "this", "to", "was", "will", "with"};
		
        /// <summary>Builds an analyzer which removes words in ENGLISH_STOP_WORDS. </summary>
        public StopAnalyzer()
        {
            stopWords = StopFilter.MakeStopSet(ENGLISH_STOP_WORDS);
        }
		
        /// <summary>Builds an analyzer with the stop words from the given set.</summary>
        public StopAnalyzer(System.Collections.Hashtable stopWords)
        {
            this.stopWords = stopWords;
        }
		
        /// <summary>Builds an analyzer which removes words in the provided array. </summary>
        public StopAnalyzer(System.String[] stopWords)
        {
            this.stopWords = StopFilter.MakeStopSet(stopWords);
        }
		
        /// <summary>Builds an analyzer with the stop words from the given file.</summary>
        /// <seealso cref="WordlistLoader.GetWordSet(File)">
        /// </seealso>
        public StopAnalyzer(System.IO.FileInfo stopwordsFile)
        {
            stopWords = WordlistLoader.GetWordSet(stopwordsFile);
        }
		
        /// <summary>Builds an analyzer with the stop words from the given reader.</summary>
        /// <seealso cref="WordlistLoader.GetWordSet(Reader)">
        /// </seealso>
        public StopAnalyzer(System.IO.TextReader stopwords)
        {
            stopWords = WordlistLoader.GetWordSet(stopwords);
        }
		
        /// <summary>Filters LowerCaseTokenizer with StopFilter. </summary>
        public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
        {
            return new StopFilter(new LowerCaseTokenizer(reader), stopWords);
        }

		/// <summary>Filters LowerCaseTokenizer with StopFilter. </summary>
		private class SavedStreams
		{
			public SavedStreams(StopAnalyzer enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			private void  InitBlock(StopAnalyzer enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private StopAnalyzer enclosingInstance;
			public StopAnalyzer Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal Tokenizer source;
			internal TokenStream result;
		}
		
		public override TokenStream ReusableTokenStream(System.String fieldName, System.IO.TextReader reader)
		{
			SavedStreams streams = (SavedStreams) GetPreviousTokenStream();
			if (streams == null)
			{
				streams = new SavedStreams(this);
				streams.source = new LowerCaseTokenizer(reader);
				streams.result = new StopFilter(streams.source, stopWords);
				SetPreviousTokenStream(streams);
			}
			else
				streams.source.Reset(reader);
			return streams.result;
		}
    }
}