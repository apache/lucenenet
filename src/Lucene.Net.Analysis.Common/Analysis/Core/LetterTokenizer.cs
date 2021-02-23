// Lucene version compatibility level 4.8.1
using J2N;
using Lucene.Net.Analysis.Util;
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
    /// A <see cref="LetterTokenizer"/> is a tokenizer that divides text at non-letters. That's to
    /// say, it defines tokens as maximal strings of adjacent letters, as defined by
    /// <see cref="Character.IsLetter(int)"/> predicate.
    /// <para>
    /// Note: this does a decent job for most European languages, but does a terrible
    /// job for some Asian languages, where words are not separated by spaces.
    /// </para>
    /// <para>
    /// You must specify the required <see cref="LuceneVersion"/> compatibility when creating
    /// <see cref="LetterTokenizer"/>:
    /// <list type="bullet">
    ///     <item><description>As of 3.1, <see cref="CharTokenizer"/> uses an <see cref="int"/> based API to normalize and
    ///     detect token characters. See <see cref="CharTokenizer.IsTokenChar(int)"/> and
    ///     <see cref="CharTokenizer.Normalize(int)"/> for details.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class LetterTokenizer : CharTokenizer
    {
        /// <summary>
        /// Construct a new <see cref="LetterTokenizer"/>.
        /// </summary>
        /// <param name="matchVersion">
        ///          <see cref="LuceneVersion"/> to match. </param>
        /// <param name="in">
        ///          the input to split up into tokens </param>
        public LetterTokenizer(LuceneVersion matchVersion, TextReader @in)
            : base(matchVersion, @in)
        {
        }

        /// <summary>
        /// Construct a new <see cref="LetterTokenizer"/> using a given
        /// <see cref="AttributeSource.AttributeFactory"/>.
        /// </summary>
        /// <param name="matchVersion">
        ///          <see cref="LuceneVersion"/> to match</param>
        /// <param name="factory">
        ///          the attribute factory to use for this <see cref="Tokenizer"/> </param>
        /// <param name="in">
        ///          the input to split up into tokens </param>
        public LetterTokenizer(LuceneVersion matchVersion, AttributeSource.AttributeFactory factory, TextReader @in)
            : base(matchVersion, factory, @in)
        {
        }

        /// <summary>
        /// Collects only characters which satisfy
        /// <see cref="Character.IsLetter(int)"/>.
        /// </summary>
        protected override bool IsTokenChar(int c)
        {
            return Character.IsLetter(c);
        }
    }
}