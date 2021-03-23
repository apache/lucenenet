// Lucene version compatibility level 4.8.1
using Lucene.Net.Util;
using System.IO;

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
    /// An <see cref="Analyzer"/> that filters <see cref="LetterTokenizer"/> 
    ///  with <see cref="LowerCaseFilter"/> 
    /// <para>
    /// You must specify the required <see cref="LuceneVersion"/> compatibility
    /// when creating <see cref="Util.CharTokenizer"/>:
    /// <list type="bullet">
    ///     <item><description>As of 3.1, <see cref="LowerCaseTokenizer"/> uses an int based API to normalize and
    ///     detect token codepoints. See <see cref="Util.CharTokenizer.IsTokenChar(int)"/> and
    ///     <see cref="Util.CharTokenizer.Normalize(int)"/> for details.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class SimpleAnalyzer : Analyzer
    {
        private readonly LuceneVersion matchVersion;

        /// <summary>
        /// Creates a new <see cref="SimpleAnalyzer"/> </summary>
        /// <param name="matchVersion"> <see cref="LuceneVersion"/> to match </param>
        public SimpleAnalyzer(LuceneVersion matchVersion)
        {
            this.matchVersion = matchVersion;
        }

        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            return new TokenStreamComponents(new LowerCaseTokenizer(matchVersion, reader));
        }
    }
}