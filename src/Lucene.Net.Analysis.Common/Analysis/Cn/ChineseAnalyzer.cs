// Lucene version compatibility level 4.8.1
using System;
using System.IO;

namespace Lucene.Net.Analysis.Cn
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
    /// An <see cref="Analyzer"/> that tokenizes text with <see cref="ChineseTokenizer"/> and
    /// filters with <see cref="ChineseFilter"/>
    /// </summary>
    /// @deprecated (3.1) Use <see cref="Standard.StandardAnalyzer"/> instead, which has the same functionality.
    /// This analyzer will be removed in Lucene 5.0 
    [Obsolete("(3.1) Use StandardAnalyzer instead, which has the same functionality.")]
    public sealed class ChineseAnalyzer : Analyzer
    {
        /// <summary>
        /// Creates
        /// <see cref="TokenStreamComponents"/>
        /// used to tokenize all the text in the provided <see cref="TextReader"/>.
        /// </summary>
        /// <returns> <see cref="TokenStreamComponents"/>
        ///         built from a <see cref="ChineseTokenizer"/> filtered with
        ///         <see cref="ChineseFilter"/> </returns>
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer source = new ChineseTokenizer(reader);
            return new TokenStreamComponents(source, new ChineseFilter(source));
        }
    }
}