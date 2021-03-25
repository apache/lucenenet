// Lucene version compatibility level 4.8.1
using J2N;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Ru
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
    /// A <see cref="RussianLetterTokenizer"/> is a <see cref="Tokenizer"/> that extends <see cref="Core.LetterTokenizer"/>
    /// by also allowing the basic Latin digits 0-9.
    /// <para>
    /// <a name="version"/>
    /// You must specify the required <see cref="LuceneVersion"/> compatibility when creating
    /// <see cref="RussianLetterTokenizer"/>:
    /// <ul>
    /// <li>As of 3.1, <see cref="CharTokenizer"/> uses an int based API to normalize and
    /// detect token characters. See <see cref="CharTokenizer.IsTokenChar(int)"/> and
    /// <see cref="CharTokenizer.Normalize(int)"/> for details.</li>
    /// </ul>
    /// </para>
    /// </summary>
    /// @deprecated (3.1) Use <see cref="Standard.StandardTokenizer"/> instead, which has the same functionality.
    /// This filter will be removed in Lucene 5.0  
    [Obsolete("(3.1) Use StandardTokenizer instead, which has the same functionality.")]
    public class RussianLetterTokenizer : CharTokenizer
    {
        private const int DIGIT_0 = '0';
        private const int DIGIT_9 = '9';

        /// <summary>
        /// Construct a new <see cref="RussianLetterTokenizer"/>.
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="in">
        ///          the input to split up into tokens </param>
        public RussianLetterTokenizer(LuceneVersion matchVersion, TextReader @in)
            : base(matchVersion, @in)
        {
        }

        /// <summary>
        /// Construct a new RussianLetterTokenizer using a given
        /// <see cref="AttributeSource.AttributeFactory"/>.
        /// </summary>
        /// <param name="matchVersion"> lucene compatibility version </param>
        /// <param name="factory">
        ///          the attribute factory to use for this <see cref="Tokenizer"/> </param>
        /// <param name="in">
        ///          the input to split up into tokens </param>
        public RussianLetterTokenizer(LuceneVersion matchVersion, AttributeFactory factory, TextReader @in)
            : base(matchVersion, factory, @in)
        {
        }

        /// <summary>
        /// Collects only characters which satisfy
        /// <see cref="Character.IsLetter(int)"/>.
        /// </summary>
        protected override bool IsTokenChar(int c)
        {
            return Character.IsLetter(c) || (c >= DIGIT_0 && c <= DIGIT_9);
        }
    }
}