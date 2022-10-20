// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tr;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Snowball
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
    /// Filters <see cref="StandardTokenizer"/> with <see cref="StandardFilter"/>, 
    /// <see cref="LowerCaseFilter"/>, <see cref="StopFilter"/> and <see cref="SnowballFilter"/>.
    /// 
    /// Available stemmers are listed in org.tartarus.snowball.ext.  The name of a
    /// stemmer is the part of the class name before "Stemmer", e.g., the stemmer in
    /// <see cref="Tartarus.Snowball.Ext.EnglishStemmer"/> is named "English".
    /// 
    /// <para><b>NOTE</b>: This class uses the same <see cref="LuceneVersion"/>
    /// dependent settings as <see cref="StandardAnalyzer"/>, with the following addition:
    /// <list type="bullet">
    ///   <item><description> As of 3.1, uses <see cref="TurkishLowerCaseFilter"/> for Turkish language.</description></item>
    /// </list>
    /// </para> </summary>
    /// @deprecated (3.1) Use the language-specific analyzer in modules/analysis instead. 
    /// This analyzer will be removed in Lucene 5.0 
    [Obsolete("(3.1) Use the language-specific analyzer in modules/analysis instead. This analyzer will be removed in Lucene 5.0.")]
    public sealed class SnowballAnalyzer : Analyzer
    {
        private string name;
        private CharArraySet stopSet;
        private readonly LuceneVersion matchVersion;

        /// <summary>
        /// Builds the named analyzer with no stop words. </summary>
        public SnowballAnalyzer(LuceneVersion matchVersion, string name)
        {
            this.name = name;
            this.matchVersion = matchVersion;
        }

        /// <summary>
        /// Builds the named analyzer with the given stop words. </summary>
        public SnowballAnalyzer(LuceneVersion matchVersion, string name, CharArraySet stopWords) : this(matchVersion, name)
        {
            stopSet = CharArraySet.Copy(matchVersion, stopWords).AsReadOnly();
        }

        /// <summary>
        /// Constructs a <see cref="StandardTokenizer"/> filtered by a 
        ///    <see cref="StandardFilter"/>, a <see cref="LowerCaseFilter"/>, a <see cref="StopFilter"/>,
        ///    and a <see cref="SnowballFilter"/> 
        /// </summary>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer tokenizer = new StandardTokenizer(matchVersion, reader);
            TokenStream result = new StandardFilter(matchVersion, tokenizer);
            // remove the possessive 's for english stemmers
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_31) && (name.Equals("English", StringComparison.Ordinal) || name.Equals("Porter", StringComparison.Ordinal) || name.Equals("Lovins", StringComparison.Ordinal)))
            {
                result = new EnglishPossessiveFilter(result);
            }
            // Use a special lowercase filter for turkish, the stemmer expects it.
            if (matchVersion.OnOrAfter(LuceneVersion.LUCENE_31) && name.Equals("Turkish", StringComparison.Ordinal))
            {
                result = new TurkishLowerCaseFilter(result);
            }
            else
            {
                result = new LowerCaseFilter(matchVersion, result);
            }
            if (stopSet != null)
            {
                result = new StopFilter(matchVersion, result, stopSet);
            }
            result = new SnowballFilter(result, name);
            return new TokenStreamComponents(tokenizer, result);
        }
    }
}