// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;

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
    /// Joins two token streams and leaves the last token of the first stream available
    /// to be used when updating the token values in the second stream based on that token.
    /// 
    /// The default implementation adds last prefix token end offset to the suffix token start and end offsets.
    /// <para/>
    /// <b>NOTE:</b> This filter might not behave correctly if used with custom 
    /// <see cref="Lucene.Net.Util.IAttribute"/>s, i.e. <see cref="Lucene.Net.Util.IAttribute"/>s other than
    /// the ones located in Lucene.Net.Analysis.TokenAttributes.
    /// </summary>
    public class PrefixAwareTokenFilter : TokenStream
    {
        private TokenStream prefix;
        private TokenStream suffix;

        private readonly ICharTermAttribute termAtt;
        private readonly IPositionIncrementAttribute posIncrAtt;
        private readonly IPayloadAttribute payloadAtt;
        private readonly IOffsetAttribute offsetAtt;
        private readonly ITypeAttribute typeAtt;
        private readonly IFlagsAttribute flagsAtt;

        private readonly ICharTermAttribute p_termAtt;
        private readonly IPositionIncrementAttribute p_posIncrAtt;
        private readonly IPayloadAttribute p_payloadAtt;
        private readonly IOffsetAttribute p_offsetAtt;
        private readonly ITypeAttribute p_typeAtt;
        private readonly IFlagsAttribute p_flagsAtt;

        public PrefixAwareTokenFilter(TokenStream prefix, TokenStream suffix)
            : base(suffix)
        {
            this.suffix = suffix;
            this.prefix = prefix;
            prefixExhausted = false;

            termAtt = AddAttribute<ICharTermAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            payloadAtt = AddAttribute<IPayloadAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();
            flagsAtt = AddAttribute<IFlagsAttribute>();

            p_termAtt = prefix.AddAttribute<ICharTermAttribute>();
            p_posIncrAtt = prefix.AddAttribute<IPositionIncrementAttribute>();
            p_payloadAtt = prefix.AddAttribute<IPayloadAttribute>();
            p_offsetAtt = prefix.AddAttribute<IOffsetAttribute>();
            p_typeAtt = prefix.AddAttribute<ITypeAttribute>();
            p_flagsAtt = prefix.AddAttribute<IFlagsAttribute>();
        }

        private readonly Token previousPrefixToken = new Token();
        private readonly Token reusableToken = new Token();

        private bool prefixExhausted;

        public override sealed bool IncrementToken()
        {
            Token nextToken; // LUCENENET: IDE0059: Remove unnecessary value assignment
            if (!prefixExhausted)
            {
                nextToken = GetNextPrefixInputToken(reusableToken);
                if (nextToken is null)
                {
                    prefixExhausted = true;
                }
                else
                {
                    previousPrefixToken.Reinit(nextToken);
                    // Make it a deep copy
                    BytesRef p = previousPrefixToken.Payload;
                    if (p != null)
                    {
                        previousPrefixToken.Payload = (BytesRef)p.Clone();
                    }
                    SetCurrentToken(nextToken);
                    return true;
                }
            }

            nextToken = GetNextSuffixInputToken(reusableToken);
            if (nextToken is null)
            {
                return false;
            }

            nextToken = UpdateSuffixToken(nextToken, previousPrefixToken);
            SetCurrentToken(nextToken);
            return true;
        }

        private void SetCurrentToken(Token token)
        {
            if (token is null)
            {
                return;
            }
            ClearAttributes();
            termAtt.CopyBuffer(token.Buffer, 0, token.Length);
            posIncrAtt.PositionIncrement = token.PositionIncrement;
            flagsAtt.Flags = token.Flags;
            offsetAtt.SetOffset(token.StartOffset, token.EndOffset);
            typeAtt.Type = token.Type;
            payloadAtt.Payload = token.Payload;
        }

        private Token GetNextPrefixInputToken(Token token)
        {
            if (!prefix.IncrementToken())
            {
                return null;
            }
            token.CopyBuffer(p_termAtt.Buffer, 0, p_termAtt.Length);
            token.PositionIncrement = p_posIncrAtt.PositionIncrement;
            token.Flags = p_flagsAtt.Flags;
            token.SetOffset(p_offsetAtt.StartOffset, p_offsetAtt.EndOffset);
            token.Type = p_typeAtt.Type;
            token.Payload = p_payloadAtt.Payload;
            return token;
        }

        private Token GetNextSuffixInputToken(Token token)
        {
            if (!suffix.IncrementToken())
            {
                return null;
            }
            token.CopyBuffer(termAtt.Buffer, 0, termAtt.Length);
            token.PositionIncrement = posIncrAtt.PositionIncrement;
            token.Flags = flagsAtt.Flags;
            token.SetOffset(offsetAtt.StartOffset, offsetAtt.EndOffset);
            token.Type = typeAtt.Type;
            token.Payload = payloadAtt.Payload;
            return token;
        }

        /// <summary>
        /// The default implementation adds last prefix token end offset to the suffix token start and end offsets.
        /// </summary>
        /// <param name="suffixToken"> a token from the suffix stream </param>
        /// <param name="lastPrefixToken"> the last token from the prefix stream </param>
        /// <returns> consumer token </returns>
        public virtual Token UpdateSuffixToken(Token suffixToken, Token lastPrefixToken)
        {
            suffixToken.SetOffset(lastPrefixToken.EndOffset + suffixToken.StartOffset, lastPrefixToken.EndOffset + suffixToken.EndOffset);
            return suffixToken;
        }

        public override void End()
        {
            prefix.End();
            suffix.End();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                prefix.Dispose();
                suffix.Dispose();
            }
            base.Dispose(disposing); // LUCENENET specific - disposable pattern requires calling the base class implementation
        }

        public override void Reset()
        {
            base.Reset();
            if (prefix != null)
            {
                prefixExhausted = false;
                prefix.Reset();
            }
            if (suffix != null)
            {
                suffix.Reset();
            }
        }

        public virtual TokenStream Prefix
        {
            get => prefix;
            set => this.prefix = value;
        }

        public virtual TokenStream Suffix
        {
            get => suffix;
            set => this.suffix = value;
        }
    }
}