// Lucene version compatibility level 4.8.1
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
    /// This <see cref="TokenFilter"/> limits the number of tokens while indexing. It is
    /// a replacement for the maximum field length setting inside <see cref="Index.IndexWriter"/>.
    /// <para>
    /// By default, this filter ignores any tokens in the wrapped <see cref="TokenStream"/>
    /// once the limit has been reached, which can result in <see cref="Reset"/> being 
    /// called prior to <see cref="IncrementToken"/> returning <c>false</c>.  For most 
    /// <see cref="TokenStream"/> implementations this should be acceptable, and faster 
    /// then consuming the full stream. If you are wrapping a <see cref="TokenStream"/> 
    /// which requires that the full stream of tokens be exhausted in order to 
    /// function properly, use the 
    /// <see cref="LimitTokenCountFilter.LimitTokenCountFilter(TokenStream,int,bool)"/> consumeAllTokens
    /// option.
    /// </para>
    /// </summary>
    public sealed class LimitTokenCountFilter : TokenFilter
    {
        private readonly int maxTokenCount;
        private readonly bool consumeAllTokens;
        private int tokenCount = 0;
        private bool exhausted = false;

        /// <summary>
        /// Build a filter that only accepts tokens up to a maximum number.
        /// This filter will not consume any tokens beyond the <paramref name="maxTokenCount"/> limit
        /// </summary>
        /// <param name="in"> the stream to wrap </param>
        /// <param name="maxTokenCount"> max number of tokens to produce </param>
        /// <seealso cref="LimitTokenCountFilter(TokenStream,int,bool)"/>
        public LimitTokenCountFilter(TokenStream @in, int maxTokenCount)
            : this(@in, maxTokenCount, false)
        {
        }

        /// <summary>
        /// Build an filter that limits the maximum number of tokens per field. </summary>
        /// <param name="in"> the stream to wrap </param>
        /// <param name="maxTokenCount"> max number of tokens to produce </param>
        /// <param name="consumeAllTokens"> whether all tokens from the input must be consumed even if <paramref name="maxTokenCount"/> is reached. </param>
        public LimitTokenCountFilter(TokenStream @in, int maxTokenCount, bool consumeAllTokens)
            : base(@in)
        {
            if (maxTokenCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTokenCount), "maxTokenCount must be greater than zero"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.maxTokenCount = maxTokenCount;
            this.consumeAllTokens = consumeAllTokens;
        }

        public override bool IncrementToken()
        {
            if (exhausted)
            {
                return false;
            }
            else if (tokenCount < maxTokenCount)
            {
                if (m_input.IncrementToken())
                {
                    tokenCount++;
                    return true;
                }
                else
                {
                    exhausted = true;
                    return false;
                }
            }
            else
            {
                while (consumeAllTokens && m_input.IncrementToken()) // NOOP
                {
                }
                return false;
            }
        }

        public override void Reset()
        {
            base.Reset();
            tokenCount = 0;
            exhausted = false;
        }
    }
}