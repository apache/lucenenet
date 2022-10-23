// Lucene version compatibility level 4.8.1
using J2N;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Lucene.Net.Analysis.Util
{
    /*
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

    /// <summary>
    /// Base class for <see cref="Analyzer"/>s that need to make use of stopword sets. 
    /// </summary>
    public abstract class StopwordAnalyzerBase : Analyzer
    {
        /// <summary>
        /// An immutable stopword set
        /// </summary>
        protected readonly CharArraySet m_stopwords;

        protected readonly LuceneVersion m_matchVersion;

        /// <summary>
        /// Returns the analyzer's stopword set or an empty set if the analyzer has no
        /// stopwords
        /// </summary>
        /// <returns> the analyzer's stopword set or an empty set if the analyzer has no
        ///         stopwords </returns>
        public virtual CharArraySet StopwordSet => m_stopwords;

        /// <summary>
        /// Creates a new instance initialized with the given stopword set
        /// </summary>
        /// <param name="version">
        ///          the Lucene version for cross version compatibility </param>
        /// <param name="stopwords">
        ///          the analyzer's stopword set </param>
        protected StopwordAnalyzerBase(LuceneVersion version, CharArraySet stopwords)
        {
            m_matchVersion = version;
            // analyzers should use char array set for stopwords!
            this.m_stopwords = stopwords is null ? CharArraySet.Empty : CharArraySet.Copy(version, stopwords).AsReadOnly();
        }

        /// <summary>
        /// Creates a new <see cref="Analyzer"/> with an empty stopword set
        /// </summary>
        /// <param name="version">
        ///          the Lucene version for cross version compatibility </param>
        protected StopwordAnalyzerBase(LuceneVersion version)
            : this(version, null)
        {
        }

        /// <summary>
        /// Creates a <see cref="CharArraySet"/> from an embedded resource associated with a class. (See
        /// <see cref="Assembly.GetManifestResourceStream(string)"/>).
        /// </summary>
        /// <param name="ignoreCase">
        ///          <c>true</c> if the set should ignore the case of the
        ///          stopwords, otherwise <c>false</c> </param>
        /// <param name="aClass">
        ///          a class that is associated with the given stopwordResource </param>
        /// <param name="resource">
        ///          name of the resource file associated with the given class </param>
        /// <param name="comment">
        ///          comment string to ignore in the stopword file </param>
        /// <returns> a <see cref="CharArraySet"/> containing the distinct stopwords from the given
        ///         file </returns>
        /// <exception cref="IOException">
        ///           if loading the stopwords throws an <see cref="IOException"/> </exception>
        protected static CharArraySet LoadStopwordSet(bool ignoreCase, Type aClass, string resource, string comment)
        {
            TextReader reader = null;
            try
            {
                var resourceStream = aClass.FindAndGetManifestResourceStream(resource);
                reader = IOUtils.GetDecodingReader(resourceStream, Encoding.UTF8);
                return WordlistLoader.GetWordSet(reader, comment, new CharArraySet(
#pragma warning disable 612, 618
                    LuceneVersion.LUCENE_CURRENT, 16, ignoreCase));
#pragma warning restore 612, 618
            }
            finally
            {
                IOUtils.Dispose(reader);
            }
        }

        /// <summary>
        /// Creates a <see cref="CharArraySet"/> from a file.
        /// </summary>
        /// <param name="stopwords">
        ///          the stopwords file to load
        /// </param>
        /// <param name="matchVersion">
        ///          the Lucene version for cross version compatibility </param>
        /// <returns> a <see cref="CharArraySet"/> containing the distinct stopwords from the given
        ///         file </returns>
        /// <exception cref="IOException">
        ///           if loading the stopwords throws an <see cref="IOException"/> </exception>
        protected static CharArraySet LoadStopwordSet(FileInfo stopwords, LuceneVersion matchVersion)
        {
            TextReader reader = null;
            try
            {
                reader = IOUtils.GetDecodingReader(stopwords, Encoding.UTF8);
                return WordlistLoader.GetWordSet(reader, matchVersion);
            }
            finally
            {
                IOUtils.Dispose(reader);
            }
        }

        /// <summary>
        /// Creates a <see cref="CharArraySet"/> from a file.
        /// </summary>
        /// <param name="stopwords">
        ///          the stopwords reader to load
        /// </param>
        /// <param name="matchVersion">
        ///          the Lucene version for cross version compatibility </param>
        /// <returns> a <see cref="CharArraySet"/> containing the distinct stopwords from the given
        ///         reader </returns>
        /// <exception cref="IOException">
        ///           if loading the stopwords throws an <see cref="IOException"/> </exception>
        protected static CharArraySet LoadStopwordSet(TextReader stopwords, LuceneVersion matchVersion)
        {
            try
            {
                return WordlistLoader.GetWordSet(stopwords, matchVersion);
            }
            finally
            {
                IOUtils.Dispose(stopwords);
            }
        }
    }
}