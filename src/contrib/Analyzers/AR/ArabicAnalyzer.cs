/**
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.IO;
using System.Collections;
using System.Collections.Generic;

using Lucene.Net.Analysis;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.AR
{
    /**
     * {@link Analyzer} for Arabic. 
     * <p>
     * This analyzer implements light-stemming as specified by:
     * <i>
     * Light Stemming for Arabic Information Retrieval
     * </i>    
     * http://www.mtholyoke.edu/~lballest/Pubs/arab_stem05.pdf
     * <p>
     * The analysis package contains three primary components:
     * <ul>
     *  <li>{@link ArabicNormalizationFilter}: Arabic orthographic normalization.
     *  <li>{@link ArabicStemFilter}: Arabic light stemming
     *  <li>Arabic stop words file: a set of default Arabic stop words.
     * </ul>
     * 
     */
    public class ArabicAnalyzer : Analyzer
    {

        /**
         * File containing default Arabic stopwords.
         * 
         * Default stopword list is from http://members.unine.ch/jacques.savoy/clef/index.html
         * The stopword list is BSD-Licensed.
         */
        public static string DEFAULT_STOPWORD_FILE = "ArabicStopWords.txt";

        /**
         * Contains the stopwords used with the StopFilter.
         */
        private ICollection<string> stoptable = new List<string>();
        /**
         * The comment character in the stopwords file.  All lines prefixed with this will be ignored  
         */
        public static string STOPWORDS_COMMENT = "#";

        private Version matchVersion;

        /**
         * Builds an analyzer with the default stop words: {@link #DEFAULT_STOPWORD_FILE}.
         *
         * @deprecated Use {@link #ArabicAnalyzer(Version)} instead
         */
        public ArabicAnalyzer() : this(Version.LUCENE_24)
        {
            
        }

        /**
         * Builds an analyzer with the default stop words: {@link #DEFAULT_STOPWORD_FILE}.
         */
        public ArabicAnalyzer(Version matchVersion)
        {
            this.matchVersion = matchVersion;

            using (StreamReader reader = new StreamReader(System.Reflection.Assembly.GetAssembly(this.GetType()).GetManifestResourceStream("Lucene.Net.Analyzers.AR." + DEFAULT_STOPWORD_FILE)))
            {
                while (!reader.EndOfStream)
                {
                    string word = reader.ReadLine();
                    stoptable.Add(word);
                }
            }
        }

        /**
         * Builds an analyzer with the given stop words.
         *
         * @deprecated Use {@link #ArabicAnalyzer(Version, String[])} instead
         */
        public ArabicAnalyzer(string[] stopwords): this(Version.LUCENE_24, stopwords)
        {
        }

        /**
         * Builds an analyzer with the given stop words.
         */
        public ArabicAnalyzer(Version matchVersion, string[] stopwords)
        {
            stoptable = StopFilter.MakeStopSet(stopwords);
            this.matchVersion = matchVersion;
        }

        /**
         * Builds an analyzer with the given stop words.
         *
         * @deprecated Use {@link #ArabicAnalyzer(Version, Hashtable)} instead
         */
        public ArabicAnalyzer(ICollection<string> stopwords) : this(Version.LUCENE_24, stopwords)
        {
        }

        /**
         * Builds an analyzer with the given stop words.
         */
        public ArabicAnalyzer(Version matchVersion, ICollection<string> stopwords)
        {
            stoptable = new List<string>(stopwords);
            this.matchVersion = matchVersion;
        }

        //DIGY
        ///**
        // * Builds an analyzer with the given stop words.  Lines can be commented out using {@link #STOPWORDS_COMMENT}
        // *
        // * @deprecated Use {@link #ArabicAnalyzer(Version, File)} instead
        // */
        //public ArabicAnalyzer(File stopwords)
        //{
        //    this(Version.LUCENE_24, stopwords);
        //}

        ///**
        // * Builds an analyzer with the given stop words.  Lines can be commented out using {@link #STOPWORDS_COMMENT}
        // */
        //public ArabicAnalyzer(Version matchVersion, File stopwords)
        //{
        //    stoptable = WordlistLoader.getWordSet(stopwords, STOPWORDS_COMMENT);
        //    this.matchVersion = matchVersion;
        //}


        /**
         * Creates a {@link TokenStream} which tokenizes all the text in the provided {@link Reader}.
         *
         * @return  A {@link TokenStream} built from an {@link ArabicLetterTokenizer} filtered with
         * 			{@link LowerCaseFilter}, {@link StopFilter}, {@link ArabicNormalizationFilter}
         *            and {@link ArabicStemFilter}.
         */
        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            TokenStream result = new ArabicLetterTokenizer(reader);
            result = new LowerCaseFilter(result);
            result = new StopFilter(StopFilter.GetEnablePositionIncrementsVersionDefault(matchVersion), result, stoptable);
            result = new ArabicNormalizationFilter(result);
            result = new ArabicStemFilter(result);

            return result;
        }

        private class SavedStreams
        {
            internal Tokenizer Source;
            internal TokenStream Result;
        };

        /**
         * Returns a (possibly reused) {@link TokenStream} which tokenizes all the text 
         * in the provided {@link Reader}.
         *
         * @return  A {@link TokenStream} built from an {@link ArabicLetterTokenizer} filtered with
         *            {@link LowerCaseFilter}, {@link StopFilter}, {@link ArabicNormalizationFilter}
         *            and {@link ArabicStemFilter}.
         */
        public override TokenStream ReusableTokenStream(string fieldName, TextReader reader)
        {
            SavedStreams streams = (SavedStreams)GetPreviousTokenStream();
            if (streams == null)
            {
                streams = new SavedStreams();
                streams.Source = new ArabicLetterTokenizer(reader);
                streams.Result = new LowerCaseFilter(streams.Source);
                streams.Result = new StopFilter(StopFilter.GetEnablePositionIncrementsVersionDefault(matchVersion),
                                                streams.Result, stoptable);
                streams.Result = new ArabicNormalizationFilter(streams.Result);
                streams.Result = new ArabicStemFilter(streams.Result);
                SetPreviousTokenStream(streams);
            }
            else
            {
                streams.Source.Reset(reader);
            }
            return streams.Result;
        }
    }
}