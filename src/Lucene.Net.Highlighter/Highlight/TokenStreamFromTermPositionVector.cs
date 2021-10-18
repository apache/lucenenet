using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Highlight
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
    /// <see cref="TokenStream"/> created from a term vector field.
    /// </summary>
    public sealed class TokenStreamFromTermPositionVector : TokenStream
    {
        private readonly IList<Token> positionedTokens = new JCG.List<Token>();

        private IEnumerator<Token> tokensAtCurrentPosition;

        private readonly ICharTermAttribute termAttribute; // LUCENENET: marked readonly

        private readonly IPositionIncrementAttribute positionIncrementAttribute; // LUCENENET: marked readonly

        private readonly IOffsetAttribute offsetAttribute; // LUCENENET: marked readonly

        private readonly IPayloadAttribute payloadAttribute; // LUCENENET: marked readonly

        ///<summary>Constructor</summary>
        /// <param name="vector">
        /// Terms that contains the data for
        /// creating the <see cref="TokenStream"/>. Must have positions and offsets.
        /// </param>
        public TokenStreamFromTermPositionVector(Terms vector)
        {
            termAttribute = AddAttribute<ICharTermAttribute>();
            positionIncrementAttribute = AddAttribute<IPositionIncrementAttribute>();
            offsetAttribute = AddAttribute<IOffsetAttribute>();
            payloadAttribute = AddAttribute<IPayloadAttribute>();

            bool hasOffsets = vector.HasOffsets;
            bool hasPayloads = vector.HasPayloads;
            TermsEnum termsEnum = vector.GetEnumerator();
            BytesRef text;
            DocsAndPositionsEnum dpEnum = null;

            while (termsEnum.MoveNext())
            {
                text = termsEnum.Term;
                dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
                dpEnum.NextDoc();
                int freq = dpEnum.Freq;
                for (int j = 0; j < freq; j++)
                {
                    int pos = dpEnum.NextPosition();
                    Token token;
                    if (hasOffsets)
                    {
                        token = new Token(text.Utf8ToString(),
                            dpEnum.StartOffset,
                            dpEnum.EndOffset);
                    }
                    else
                    {
                        token = new Token();
                        token.SetEmpty().Append(text.Utf8ToString());
                    }
                    if (hasPayloads)
                    {
                        // Must make a deep copy of the returned payload,
                        // since D&PEnum API is allowed to re-use on every
                        // call:
                        token.Payload = BytesRef.DeepCopyOf(dpEnum.GetPayload());
                    }

                    // Yes - this is the position, not the increment! This is for
                    // sorting. This value
                    // will be corrected before use.
                    token.PositionIncrement = pos;
                    this.positionedTokens.Add(token);
                }
            }

            CollectionUtil.TimSort(this.positionedTokens, tokenComparer);

            int lastPosition = -1;
            foreach (Token token in this.positionedTokens)
            {
                int thisPosition = token.PositionIncrement;
                token.PositionIncrement = thisPosition - lastPosition;
                lastPosition = thisPosition;
            }
            this.tokensAtCurrentPosition = this.positionedTokens.GetEnumerator();
        }

        private static readonly IComparer<Token> tokenComparer = new TokenComparer();

        public override bool IncrementToken()
        {
            if (this.tokensAtCurrentPosition.MoveNext())
            {
                Token next = this.tokensAtCurrentPosition.Current;
                ClearAttributes();
                termAttribute.SetEmpty().Append(next);
                positionIncrementAttribute.PositionIncrement = next.PositionIncrement;
                offsetAttribute.SetOffset(next.StartOffset, next.EndOffset);
                payloadAttribute.Payload = next.Payload;
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            this.tokensAtCurrentPosition = this.positionedTokens.GetEnumerator();
        }

        private class TokenComparer : IComparer<Token>
        {
            public int Compare(Token o1, Token o2)
            {
                return o1.PositionIncrement - o2.PositionIncrement;
            }
        }
    }
}
