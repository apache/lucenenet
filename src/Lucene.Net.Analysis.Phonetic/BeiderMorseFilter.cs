// lucene version compatibility level: 4.8.1
using Lucene.Net.Analysis.Phonetic.Language.Bm;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Phonetic
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
    /// TokenFilter for Beider-Morse phonetic encoding.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="BeiderMorseEncoder"/>
    public sealed class BeiderMorseFilter : TokenFilter
    {
        private readonly PhoneticEngine engine;
        private readonly LanguageSet languages;

        // output is a string such as ab|ac|...
        // in complex cases like d'angelo its (anZelo|andZelo|...)-(danZelo|...)
        // if there are multiple 's, it starts to nest...
        private static readonly Regex pattern = new Regex("([^()|-]+)", RegexOptions.Compiled);

        private bool isReset = false;
        // matcher over any buffered output
        private Match matcher = pattern.Match("");
        // encoded representation
        private string encoded;
        // preserves all attributes for any buffered outputs
        private State state;

        private readonly ICharTermAttribute termAtt;
        private readonly IPositionIncrementAttribute posIncAtt;

        /// <summary>
        /// Calls <see cref="BeiderMorseFilter(TokenStream, PhoneticEngine, LanguageSet)"/>
        /// </summary>
        /// <param name="input"><see cref="TokenStream"/> to filter</param>
        /// <param name="engine">Configured <see cref="PhoneticEngine"/> with BM settings.</param>
        public BeiderMorseFilter(TokenStream input, PhoneticEngine engine)
            : this(input, engine, null)
        {
        }

        /// <summary>
        /// Create a new <see cref="BeiderMorseFilter"/>
        /// </summary>
        /// <param name="input"><see cref="TokenStream"/> to filter</param>
        /// <param name="engine">Configured <see cref="PhoneticEngine"/> with BM settings.</param>
        /// <param name="languages">Optional Set of original languages. Can be <c>null</c> (which means it will be guessed).</param>
        public BeiderMorseFilter(TokenStream input, PhoneticEngine engine, LanguageSet languages)
            : base(input)
        {
            this.engine = engine;
            this.languages = languages;
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.posIncAtt = AddAttribute<IPositionIncrementAttribute>();
        }

        public override bool IncrementToken()
        {
            if (!isReset)
            {
                matcher = matcher.NextMatch();
            }
            isReset = false;

            if (matcher.Success)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(state != null && encoded != null);
                RestoreState(state);

                int start = matcher.Index;
                //int end = start + matcher.Length;
                termAtt.SetEmpty().Append(encoded, start, matcher.Length); // LUCENENET: Corrected 3rd parameter
                posIncAtt.PositionIncrement = 0;
                return true;
            }

            if (m_input.IncrementToken())
            {
                encoded = (languages is null)
                    ? engine.Encode(termAtt.ToString())
                    : engine.Encode(termAtt.ToString(), languages);
                state = CaptureState();

                matcher = pattern.Match(encoded);
                if (matcher.Success)
                {
                    int start = matcher.Index;
                    //int end = start + matcher.Length;
                    termAtt.SetEmpty().Append(encoded, start, matcher.Length); // LUCENENET: Corrected 3rd parameter
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void Reset()
        {
            base.Reset();

            // LUCENENET: Since we need to "reset" the Match
            // object, we also need an "isReset" flag to indicate
            // whether we are at the head of the match and to 
            // take the appropriate measures to ensure we don't 
            // overwrite our matcher variable with 
            // matcher = matcher.NextMatch();
            // before it is time. A string could potentially
            // match on index 0, so we need another variable to
            // manage this state.
            matcher = pattern.Match("");
            isReset = true;
        }
    }
}
