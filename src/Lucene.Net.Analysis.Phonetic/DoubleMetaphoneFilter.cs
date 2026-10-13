// lucene version compatibility level: 4.8.1
using Lucene.Net.Analysis.Phonetic.Language;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.TokenAttributes.Extensions;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
    /// Filter for DoubleMetaphone (supporting secondary codes)
    /// </summary>
    public sealed class DoubleMetaphoneFilter : TokenFilter
    {
        //private static readonly string TOKEN_TYPE = "DoubleMetaphone"; // LUCENENET: Not used

        private readonly Queue<State> remainingTokens = new Queue<State>();
        private readonly DoubleMetaphone encoder = new DoubleMetaphone();
        private readonly bool inject;
        private readonly ICharTermAttribute termAtt;
        private readonly IPositionIncrementAttribute posAtt;

        /// <summary>
        /// Creates a <see cref="DoubleMetaphoneFilter"/> with the specified maximum code length,
        /// and either adding encoded forms as synonyms (<c>inject=true</c>) or
        /// replacing them.
        /// </summary>
        public DoubleMetaphoneFilter(TokenStream input, int maxCodeLength, bool inject)
            : base(input)
        {
            this.encoder.MaxCodeLen = maxCodeLength; // LUCENENET: MaxCodeLen ensures the value is non-negative
            this.inject = inject;
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.posAtt = AddAttribute<IPositionIncrementAttribute>();
        }

        public override bool IncrementToken()
        {
            for (; ; )
            {
                if (remainingTokens.TryDequeue(out State first))
                {
                    // clearAttributes();  // not currently necessary
                    RestoreState(first);
                    return true;
                }

                if (!m_input.IncrementToken()) return false;

                int len = termAtt.Length;
                if (len == 0) return true; // pass through zero length terms

                int firstAlternativeIncrement = inject ? 0 : posAtt.PositionIncrement;

                string v = termAtt.ToString();
                string primaryPhoneticValue = encoder.GetDoubleMetaphone(v);
                string alternatePhoneticValue = encoder.GetDoubleMetaphone(v, true);

                // a flag to lazily save state if needed... this avoids a save/restore when only
                // one token will be generated.
                bool saveState = inject;

                if (primaryPhoneticValue != null && primaryPhoneticValue.Length > 0 && !primaryPhoneticValue.Equals(v, StringComparison.Ordinal))
                {
                    if (saveState)
                    {
                        remainingTokens.Enqueue(CaptureState());
                    }
                    posAtt.PositionIncrement = firstAlternativeIncrement;
                    firstAlternativeIncrement = 0;
                    termAtt.SetEmpty().Append(primaryPhoneticValue);
                    saveState = true;
                }

                if (alternatePhoneticValue != null && alternatePhoneticValue.Length > 0
                        && !alternatePhoneticValue.Equals(primaryPhoneticValue, StringComparison.Ordinal)
                        && !primaryPhoneticValue.Equals(v, StringComparison.Ordinal))
                {
                    if (saveState)
                    {
                        remainingTokens.Enqueue(CaptureState());
                        //saveState = false; // LUCENENET: IDE0059: Remove unnecessary value assignment
                    }
                    posAtt.PositionIncrement = firstAlternativeIncrement;
                    termAtt.SetEmpty().Append(alternatePhoneticValue);
                    saveState = true;
                }

                // Just one token to return, so no need to capture/restore
                // any state, simply return it.
                if (remainingTokens.Count == 0)
                {
                    return true;
                }

                if (saveState)
                {
                    remainingTokens.Enqueue(CaptureState());
                }
            }
        }

        // LUCENENET specific: matches the upstream Java implementation, which calls input.reset()
        // directly rather than super.reset(). Behavior is identical.
        [SuppressMessage("Design", "Lucene1001:TokenStream override of End()/Reset()/Close() must call the corresponding base method.",
            Justification = "Matches upstream Lucene Java behavior; TokenFilter.Reset() chains to m_input.Reset() so this is equivalent.")]
        public override void Reset()
        {
            m_input.Reset();
            remainingTokens.Clear();
        }
    }
}
