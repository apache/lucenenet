// Lucene version compatibility level 4.8.1
using J2N;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;
using System;
using System.Globalization;
using System.IO;

namespace Lucene.Net.Analysis.Ar
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
    /// Tokenizer that breaks text into runs of letters and diacritics.
    /// <para>
    /// The problem with the standard Letter tokenizer is that it fails on diacritics.
    /// Handling similar to this is necessary for Indic Scripts, Hebrew, Thaana, etc.
    /// </para>
    /// <para>
    /// You must specify the required <see cref="LuceneVersion"/> compatibility when creating
    /// <see cref="ArabicLetterTokenizer"/>:
    /// <list type="bullet">
    /// <item><description>As of 3.1, <see cref="Util.CharTokenizer"/> uses an int based API to normalize and
    /// detect token characters. See <see cref="IsTokenChar(int)"/> and
    /// <see cref="Util.CharTokenizer.Normalize(int)"/> for details.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// @deprecated (3.1) Use <see cref="Standard.StandardTokenizer"/> instead. 
    [Obsolete("(3.1) Use StandardTokenizer instead.")]
    public class ArabicLetterTokenizer : LetterTokenizer
    {
        /// <summary>
        /// Construct a new ArabicLetterTokenizer. </summary>
        /// <param name="matchVersion"> <see cref="LuceneVersion"/>
        /// to match 
        /// </param>
        /// <param name="in">
        ///          the input to split up into tokens </param>
        public ArabicLetterTokenizer(LuceneVersion matchVersion, TextReader @in)
              : base(matchVersion, @in)
        {
        }

        /// <summary>
        /// Construct a new <see cref="ArabicLetterTokenizer"/> using a given
        /// <see cref="AttributeSource.AttributeFactory"/>. 
        /// </summary>
        /// <param name="matchVersion">
        ///         Lucene version to match - See
        ///         <see cref="LuceneVersion"/>.
        /// </param>
        /// <param name="factory">
        ///          the attribute factory to use for this Tokenizer </param>
        /// <param name="in">
        ///          the input to split up into tokens </param>
        public ArabicLetterTokenizer(LuceneVersion matchVersion, AttributeFactory factory, TextReader @in)
            : base(matchVersion, factory, @in)
        {
        }

        /// <summary>
        /// Allows for Letter category or NonspacingMark category </summary>
        /// <see cref="LetterTokenizer.IsTokenChar(int)"/>
        protected override bool IsTokenChar(int c)
        {
            return base.IsTokenChar(c) || Character.GetType(c) == UnicodeCategory.NonSpacingMark;
        }
    }
}