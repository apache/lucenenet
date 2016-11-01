using System.IO;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
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
    /// A WhitespaceTokenizer is a tokenizer that divides text at whitespace.
    /// Adjacent sequences of non-Whitespace characters form tokens. <a
    /// name="version"/>
    /// <para>
    /// You must specify the required <seealso cref="LuceneVersion"/> compatibility when creating
    /// <seealso cref="WhitespaceTokenizer"/>:
    /// <ul>
    /// <li>As of 3.1, <seealso cref="CharTokenizer"/> uses an int based API to normalize and
    /// detect token characters. See <seealso cref="CharTokenizer#isTokenChar(int)"/> and
    /// <seealso cref="CharTokenizer#normalize(int)"/> for details.</li>
    /// </ul>
    /// </para>
    /// </summary>
    public sealed class WhitespaceTokenizer : CharTokenizer
    {

        /// Construct a new WhitespaceTokenizer. * <param name="matchVersion"> Lucene version
        /// to match See <seealso cref="<a href="#version">above</a>"/>
        /// </param>
        /// <param name="in">
        ///          the input to split up into tokens </param>
        public WhitespaceTokenizer(LuceneVersion matchVersion, TextReader @in)
            : base(matchVersion, @in)
        {
        }

        /// <summary>
        /// Construct a new WhitespaceTokenizer using a given
        /// <seealso cref="org.apache.lucene.util.AttributeSource.AttributeFactory"/>.
        /// 
        /// @param
        ///          matchVersion Lucene version to match See
        ///          <seealso cref="<a href="#version">above</a>"/> </summary>
        /// <param name="factory">
        ///          the attribute factory to use for this <seealso cref="Tokenizer"/> </param>
        /// <param name="in">
        ///          the input to split up into tokens </param>
        public WhitespaceTokenizer(LuceneVersion matchVersion, AttributeFactory factory, TextReader @in)
            : base(matchVersion, factory, @in)
        {
        }

        /// <summary>
        /// Collects only characters which do not satisfy
        /// <seealso cref="Character#isWhitespace(int)"/>.
        /// </summary>
        protected override bool IsTokenChar(int c)
        {
            return !char.IsWhiteSpace((char)c);
        }
    }
}