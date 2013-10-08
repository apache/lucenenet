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

using System;
using System.Globalization;
using System.IO;
using Lucene.Net.Analysis.Core;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.AR
{
    /// <summary>
    /// Tokenizer that breaks text into runs of letters and diacritics.
    /// <p>
    /// The problem with the standard Letter tokenizer is that it fails on diacritics.
    /// Handling similar to this is necessary for Indic Scripts, Hebrew, Thaana, etc.
    /// </p>
    /// <see cref="Version"/>
    /// You must specify the required Version compatibility when creating ArabicLetterTokenizer:
    /// As of 3.1, CharTokenizer uses an int based API to normalize and detect token characters.
    /// </summary>
    [Obsolete("(3.1) Use StandardTokenizer instead.")]
    public class ArabicLetterTokenizer : LetterTokenizer
    {
        /// <summary>
        /// Contstruct a new ArabicLetterTokenizer.
        /// </summary>
        /// <param name="matchVersion">Lucene version to match</param>
        /// <param name="input">the input to split up into tokens</param>
        public ArabicLetterTokenizer(Version matchVersion, TextReader input) : base(matchVersion, input) { }

        /// <summary>
        /// Construct a new ArabicLetterTokenizer using a given
        /// <see cref="Lucene.Net.Util.AttributeSource.AttributeFactory"/>.
        /// </summary>
        /// <param name="matchVersion">Lucene version to match</param>
        /// <param name="factory">the attribute factory to use for thsi Tokenizer</param>
        /// <param name="input">the input to split up into tokens</param>
        public ArabicLetterTokenizer(Version matchVersion, AttributeFactory factory, TextReader input) : base(matchVersion, factory, input) { }

        /// <summary>
        /// Allows for Letter category or NonspacingMark category
        /// <seealso cref="Lucene.Net.Analysis.Core.LetterTokenizer#IsTokenChar(int)"/>
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        protected override bool IsTokenChar(int c)
        {
            return base.IsTokenChar(c) || char.GetUnicodeCategory((char)c) == UnicodeCategory.NonSpacingMark;
        }
    }
}