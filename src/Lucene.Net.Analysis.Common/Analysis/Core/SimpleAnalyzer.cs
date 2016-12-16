using System.IO;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Core
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
    /// An <seealso cref="Analyzer"/> that filters <seealso cref="LetterTokenizer"/> 
    ///  with <seealso cref="LowerCaseFilter"/> 
    /// <para>
    /// <a name="version">You must specify the required <seealso cref="LuceneVersion"/> compatibility
    /// when creating <seealso cref="CharTokenizer"/>:
    /// <ul>
    /// <li>As of 3.1, <seealso cref="LowerCaseTokenizer"/> uses an int based API to normalize and
    /// detect token codepoints. See <seealso cref="CharTokenizer#isTokenChar(int)"/> and
    /// <seealso cref="CharTokenizer#normalize(int)"/> for details.</li>
    /// </ul>
    /// </para>
    /// <para>
    /// 
    /// </para>
    /// </summary>
    public sealed class SimpleAnalyzer : Analyzer
    {

        private readonly LuceneVersion matchVersion;

        /// <summary>
        /// Creates a new <seealso cref="SimpleAnalyzer"/> </summary>
        /// <param name="matchVersion"> Lucene version to match See <seealso cref="<a href="#version">above</a>"/> </param>
        public SimpleAnalyzer(LuceneVersion matchVersion)
        {
            this.matchVersion = matchVersion;
        }

        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            return new TokenStreamComponents(new LowerCaseTokenizer(matchVersion, reader));
        }
    }
}