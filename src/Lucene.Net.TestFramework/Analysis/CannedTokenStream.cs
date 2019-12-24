using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Analysis
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
    /// <see cref="TokenStream"/> from a canned list of <see cref="Token"/>s.
    /// </summary>
    public sealed class CannedTokenStream : TokenStream
    {
        private readonly Token[] tokens;
        private int upto = 0;
        private readonly ICharTermAttribute termAtt;
        private readonly IPositionIncrementAttribute posIncrAtt;
        private readonly IPositionLengthAttribute posLengthAtt;
        private readonly IOffsetAttribute offsetAtt;
        private readonly IPayloadAttribute payloadAtt;
        private readonly int finalOffset;
        private readonly int finalPosInc;

        public CannedTokenStream(params Token[] tokens)
            : this(0, 0, tokens)
        { }

        /// <summary>
        /// If you want trailing holes, pass a non-zero
        /// <paramref name="finalPosInc"/>.
        /// </summary>
        public CannedTokenStream(int finalPosInc, int finalOffset, params Token[] tokens)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            posLengthAtt = AddAttribute<IPositionLengthAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            payloadAtt = AddAttribute<IPayloadAttribute>();

            this.tokens = tokens;
            this.finalOffset = finalOffset;
            this.finalPosInc = finalPosInc;
        }

        public override void End()
        {
            base.End();
            posIncrAtt.PositionIncrement = finalPosInc;
            offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        public override bool IncrementToken()
        {
            if (upto < tokens.Length)
            {
                Token token = tokens[upto++];
                // TODO: can we just capture/restoreState so
                // we get all attrs...?
                ClearAttributes();
                termAtt.SetEmpty();
                termAtt.Append(token.ToString());
                posIncrAtt.PositionIncrement = token.PositionIncrement;
                posLengthAtt.PositionLength = token.PositionLength;
                offsetAtt.SetOffset(token.StartOffset, token.EndOffset);
                payloadAtt.Payload = token.Payload;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}