using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using Reader = System.IO.TextReader;
using Version = Lucene.Net.Util.LuceneVersion;

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
    /// <p/>
    /// <b>NOTE:</b> This filter might not behave correctly if used with custom Attributes, i.e. Attributes other than
    /// the ones located in org.apache.lucene.analysis.tokenattributes. 
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
            posIncrAtt = AddAttribute(typeof(PositionIncrementAttribute));
            payloadAtt = AddAttribute(typeof(PayloadAttribute));
            offsetAtt = AddAttribute(typeof(OffsetAttribute));
            typeAtt = AddAttribute(typeof(TypeAttribute));
            flagsAtt = AddAttribute(typeof(FlagsAttribute));

            p_termAtt = prefix.AddAttribute(typeof(CharTermAttribute));
            p_posIncrAtt = prefix.AddAttribute(typeof(PositionIncrementAttribute));
            p_payloadAtt = prefix.AddAttribute(typeof(PayloadAttribute));
            p_offsetAtt = prefix.AddAttribute(typeof(OffsetAttribute));
            p_typeAtt = prefix.AddAttribute(typeof(TypeAttribute));
            p_flagsAtt = prefix.AddAttribute(typeof(FlagsAttribute));
        }

        private readonly Token previousPrefixToken = new Token();
        private readonly Token reusableToken = new Token();

        private bool prefixExhausted;

        public override bool IncrementToken()
        {
            if (!prefixExhausted)
            {
                Token nextToken = getNextPrefixInputToken(reusableToken);
                if (nextToken == null)
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
                        previousPrefixToken.Payload = p.Clone();
                    }
                    CurrentToken = nextToken;
                    return true;
                }
            }

            Token nextToken = getNextSuffixInputToken(reusableToken);
            if (nextToken == null)
            {
                return false;
            }

            nextToken = updateSuffixToken(nextToken, previousPrefixToken);
            CurrentToken = nextToken;
            return true;
        }

        private Token CurrentToken
        {
            set
            {
                if (value == null)
                {
                    return;
                }
                ClearAttributes();
                termAtt.CopyBuffer(value.buffer(), 0, value.length());
                posIncrAtt.PositionIncrement = value.PositionIncrement;
                flagsAtt.Flags = value.Flags;
                offsetAtt.setOffset(value.startOffset(), value.endOffset());
                typeAtt.Type = value.type();
                payloadAtt.Payload = value.Payload;
            }
        }

        private Token getNextPrefixInputToken(Token token)
        {
            if (!prefix.IncrementToken())
            {
                return null;
            }
            token.CopyBuffer(p_termAtt.buffer(), 0, p_termAtt.length());
            token.PositionIncrement = p_posIncrAtt.PositionIncrement;
            token.Flags = p_flagsAtt.Flags;
            token.SetOffset(p_offsetAtt.startOffset(), p_offsetAtt.endOffset());
            token.Type = p_typeAtt.type();
            token.Payload = p_payloadAtt.Payload;
            return token;
        }

        private Token getNextSuffixInputToken(Token token)
        {
            if (!suffix.IncrementToken())
            {
                return null;
            }
            token.CopyBuffer(termAtt.buffer(), 0, termAtt.length());
            token.PositionIncrement = posIncrAtt.PositionIncrement;
            token.Flags = flagsAtt.Flags;
            token.SetOffset(offsetAtt.StartOffset(), offsetAtt.EndOffset());
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
        public virtual Token updateSuffixToken(Token suffixToken, Token lastPrefixToken)
        {
            suffixToken.setOffset(lastPrefixToken.endOffset() + suffixToken.startOffset(), lastPrefixToken.endOffset() + suffixToken.endOffset());
            return suffixToken;
        }

        public override void End()
        {
            prefix.End();
            suffix.End();
        }

        public override void Dispose()
        {
            prefix.Dispose();
            suffix.Dispose();
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
            get
            {
                return prefix;
            }
            set
            {
                this.prefix = value;
            }
        }


        public virtual TokenStream Suffix
        {
            get
            {
                return suffix;
            }
            set
            {
                this.suffix = value;
            }
        }
    }
}