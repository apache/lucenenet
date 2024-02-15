// Lucene version compatibility level 8.2.0
// LUCENENET NOTE: Ported because Lucene.Net.Analysis.OpenNLP requires this to be useful.
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
#nullable enable

namespace Lucene.Net.Analysis.Miscellaneous
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
    /// Adds the <see cref="ITypeAttribute.Type"/> as a synonym,
    /// i.e. another token at the same position, optionally with a specified prefix prepended.
    /// </summary>
    public sealed class TypeAsSynonymFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAtt;
        private readonly ITypeAttribute typeAtt;
        private readonly IPositionIncrementAttribute posIncrAtt;
        private readonly string? prefix;

        private State? savedToken = null;

        /// <summary>
        /// Initializes a new instance of <see cref="TypeAsSynonymFilter"/> with
        /// the specified token stream.
        /// </summary>
        /// <param name="input">Input token stream.</param>
        public TypeAsSynonymFilter(TokenStream input)
            : this(input, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TypeAsSynonymFilter"/> with
        /// the specified token stream and prefix.
        /// </summary>
        /// <param name="input">Input token stream.</param>
        /// <param name="prefix">Prepend this string to every token type emitted as token text.
        /// If <c>null</c>, nothing will be prepended.</param>
        public TypeAsSynonymFilter(TokenStream input, string? prefix)
            : base(input)
        {
            this.prefix = prefix;
            termAtt = AddAttribute<ICharTermAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
        }


        public override bool IncrementToken()
        {
            if (savedToken != null)
            {
                // Emit last token's type at the same position
                RestoreState(savedToken);
                savedToken = null;
                termAtt.SetEmpty();
                if (prefix != null)
                {
                    termAtt.Append(prefix);
                }
                termAtt.Append(typeAtt.Type);
                posIncrAtt.PositionIncrement = 0;
                return true;
            }
            else if (m_input.IncrementToken())
            {
                // Ho pending token type to emit
                savedToken = CaptureState();
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            savedToken = null;
        }
    }
}
