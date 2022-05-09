// lucene version compatibility level: 4.8.1
using Lucene.Net.Analysis.Phonetic.Language;
using Lucene.Net.Analysis.TokenAttributes;
using System;

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
    /// Create tokens for phonetic matches.
    /// See the Language namespace.
    /// </summary>
    public sealed class PhoneticFilter : TokenFilter
    {
        /// <summary>true if encoded tokens should be added as synonyms</summary>
        private readonly bool inject = true; // LUCENENET: marked readonly
        /// <summary>phonetic encoder</summary>
        private readonly IStringEncoder encoder = null; // LUCENENET: marked readonly
        /// <summary>captured state, non-null when <c>inject=true</c> and a token is buffered</summary>
        private State save = null;
        private readonly ICharTermAttribute termAtt;
        private readonly IPositionIncrementAttribute posAtt;

        /// <summary>
        /// Creates a <see cref="PhoneticFilter"/> with the specified encoder, and either
        /// adding encoded forms as synonyms (<c>inject=true</c>) or
        /// replacing them.
        /// </summary>
        public PhoneticFilter(TokenStream input, IStringEncoder encoder, bool inject)
            : base(input)
        {
            this.encoder = encoder;
            this.inject = inject;
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.posAtt = AddAttribute<IPositionIncrementAttribute>();
        }

        public override bool IncrementToken()
        {
            if (save != null)
            {
                // clearAttributes();  // not currently necessary
                RestoreState(save);
                save = null;
                return true;
            }

            if (!m_input.IncrementToken()) return false;

            // pass through zero-length terms
            if (termAtt.Length == 0) return true;

            string value = termAtt.ToString();
            string phonetic = null;
            try
            {
                string v = encoder.Encode(value);
                if (v.Length > 0 && !value.Equals(v, StringComparison.Ordinal))
                {
                    phonetic = v;
                }
            }
            catch (Exception ignored) when (ignored.IsException()) { } // just use the direct text

                if (phonetic is null) return true;

            if (!inject)
            {
                // just modify this token
                termAtt.SetEmpty().Append(phonetic);
                return true;
            }

            // We need to return both the original and the phonetic tokens.
            // to avoid a orig=captureState() change_to_phonetic() saved=captureState()  restoreState(orig)
            // we return the phonetic alternative first

            int origOffset = posAtt.PositionIncrement;
            posAtt.PositionIncrement = 0;
            save = CaptureState();

            posAtt.PositionIncrement = origOffset;
            termAtt.SetEmpty().Append(phonetic);
            return true;
        }

        public override void Reset()
        {
            m_input.Reset();
            save = null;
        }
    }
}
