// Lucene version compatibility level 8.2.0
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using Morfologik.Stemming;
using Morfologik.Stemming.Polish;
using System.IO;

namespace Lucene.Net.Analysis.Morfologik
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
    /// <see cref="Analyzer"/> using Morfologik library.
    /// <para/>
    /// See: <a href="http://morfologik.blogspot.com/">Morfologik project page</a>
    /// </summary>
    /// <since>4.0.0</since>
    public class MorfologikAnalyzer : Analyzer
    {
        private readonly Dictionary dictionary;
        private readonly LuceneVersion version;

        /// <summary>
        /// Builds an analyzer with an explicit <see cref="Dictionary"/> resource.
        /// <para/>
        /// See: <a href="https://github.com/morfologik/">https://github.com/morfologik/</a>
        /// </summary>
        /// <param name="version">Lucene compatibility version</param>
        /// <param name="dictionary">A prebuilt automaton with inflected and base word forms.</param>
        public MorfologikAnalyzer(LuceneVersion version, Dictionary dictionary)
        {
            this.version = version;
            this.dictionary = dictionary;
        }

        /// <summary>
        /// Builds an analyzer with the default Morfologik's Polish dictionary.
        /// </summary>
        /// <param name="version">Lucene compatibility version</param>
        public MorfologikAnalyzer(LuceneVersion version)
            : this(version, new PolishStemmer().Dictionary)
        {
        }

        /// <summary>
        /// Creates a <see cref="TokenStreamComponents"/>
        /// which tokenizes all the text in the provided <paramref name="reader"/>.
        /// </summary>
        /// <param name="fieldName">Ignored field name.</param>
        /// <param name="reader">Source of tokens.</param>
        /// <returns>A <see cref="TokenStreamComponents"/>
        /// built from a <see cref="StandardTokenizer"/> filtered with
        /// <see cref="MorfologikFilter"/>.</returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer src = new StandardTokenizer(this.version, reader);

            return new TokenStreamComponents(
                src,
                new MorfologikFilter(src, dictionary));
        }

    }
}
