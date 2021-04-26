// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using System;

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
    /// This <see cref="TokenFilter"/> limits its emitted tokens to those with positions that
    /// are not greater than the configured limit.
    /// <para>
    /// By default, this filter ignores any tokens in the wrapped <see cref="TokenStream"/>
    /// once the limit has been exceeded, which can result in <see cref="Reset"/> being 
    /// called prior to <see cref="IncrementToken"/> returning <c>false</c>.  For most 
    /// <see cref="TokenStream"/> implementations this should be acceptable, and faster 
    /// then consuming the full stream. If you are wrapping a <see cref="TokenStream"/>
    /// which requires that the full stream of tokens be exhausted in order to 
    /// function properly, use the 
    /// <see cref="LimitTokenPositionFilter(TokenStream,int,bool)"/> consumeAllTokens
    /// option.
    /// </para>
    /// </summary>
    public sealed class LimitTokenPositionFilter : TokenFilter
    {
        private readonly int maxTokenPosition;
        private readonly bool consumeAllTokens;
        private int tokenPosition = 0;
        private bool exhausted = false;
        private readonly IPositionIncrementAttribute posIncAtt;

        /// <summary>
        /// Build a filter that only accepts tokens up to and including the given maximum position.
        /// This filter will not consume any tokens with position greater than the <paramref name="maxTokenPosition"/> limit.
        /// </summary>
        /// <param name="in"> the stream to wrap </param>
        /// <param name="maxTokenPosition"> max position of tokens to produce (1st token always has position 1)
        /// </param>
        /// <seealso cref="LimitTokenPositionFilter(TokenStream,int,bool)"/>
        public LimitTokenPositionFilter(TokenStream @in, int maxTokenPosition)
            : this(@in, maxTokenPosition, false)
        {
        }

        /// <summary>
        /// Build a filter that limits the maximum position of tokens to emit.
        /// </summary>
        /// <param name="in"> the stream to wrap </param>
        /// <param name="maxTokenPosition"> max position of tokens to produce (1st token always has position 1) </param>
        /// <param name="consumeAllTokens"> whether all tokens from the wrapped input stream must be consumed
        ///                         even if maxTokenPosition is exceeded. </param>
        public LimitTokenPositionFilter(TokenStream @in, int maxTokenPosition, bool consumeAllTokens)
            : base(@in)
        {
            if (maxTokenPosition < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTokenPosition), "maxTokenPosition must be greater than zero"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.maxTokenPosition = maxTokenPosition;
            this.consumeAllTokens = consumeAllTokens;
            posIncAtt = AddAttribute<IPositionIncrementAttribute>();
        }

        public override bool IncrementToken()
        {
            if (exhausted)
            {
                return false;
            }
            if (m_input.IncrementToken())
            {
                tokenPosition += posIncAtt.PositionIncrement;
                if (tokenPosition <= maxTokenPosition)
                {
                    return true;
                }
                else
                {
                    while (consumeAllTokens && m_input.IncrementToken()) // NOOP
                    {
                    }
                    exhausted = true;
                    return false;
                }
            }
            else
            {
                exhausted = true;
                return false;
            }
        }

        public override void Reset()
        {
            base.Reset();
            tokenPosition = 0;
            exhausted = false;
        }
    }
}