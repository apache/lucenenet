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

using System;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using FlagsAttribute = Lucene.Net.Analysis.Tokenattributes.FlagsAttribute;

namespace Lucene.Net.Analyzers.Miscellaneous
{
    /// <summary>
    /// Joins two token streams and leaves the last token of the first stream available
    /// to be used when updating the token values in the second stream based on that token.
    /// 
    /// The default implementation adds last prefix token end offset to the suffix token start and end offsets.
    /// <p/>
    /// <b>NOTE:</b> This filter might not behave correctly if used with custom Attributes, i.e. Attributes other than
    /// the ones located in Lucene.Net.Analysis.TokenAttributes. 
    /// </summary>
    public class PrefixAwareTokenFilter : TokenStream
    {
        private readonly FlagsAttribute _flagsAtt;
        private readonly OffsetAttribute _offsetAtt;
        private readonly FlagsAttribute _pFlagsAtt;

        private readonly OffsetAttribute _pOffsetAtt;
        private readonly PayloadAttribute _pPayloadAtt;
        private readonly PositionIncrementAttribute _pPosIncrAtt;
        private readonly TermAttribute _pTermAtt;
        private readonly TypeAttribute _pTypeAtt;
        private readonly PayloadAttribute _payloadAtt;
        private readonly PositionIncrementAttribute _posIncrAtt;

        private readonly Token _previousPrefixToken = new Token();
        private readonly Token _reusableToken = new Token();
        private readonly TermAttribute _termAtt;
        private readonly TypeAttribute _typeAtt;

        private bool _prefixExhausted;

        public PrefixAwareTokenFilter(TokenStream prefix, TokenStream suffix) : base(suffix)
        {
            Suffix = suffix;
            Prefix = prefix;
            _prefixExhausted = false;

            // ReSharper disable DoNotCallOverridableMethodsInConstructor
            _termAtt = (TermAttribute) AddAttribute(typeof (TermAttribute));
            _posIncrAtt = (PositionIncrementAttribute) AddAttribute(typeof (PositionIncrementAttribute));
            _payloadAtt = (PayloadAttribute) AddAttribute(typeof (PayloadAttribute));
            _offsetAtt = (OffsetAttribute) AddAttribute(typeof (OffsetAttribute));
            _typeAtt = (TypeAttribute) AddAttribute(typeof (TypeAttribute));
            _flagsAtt = (FlagsAttribute) AddAttribute(typeof (FlagsAttribute));
            // ReSharper restore DoNotCallOverridableMethodsInConstructor

            _pTermAtt = (TermAttribute) prefix.AddAttribute(typeof (TermAttribute));
            _pPosIncrAtt = (PositionIncrementAttribute) prefix.AddAttribute(typeof (PositionIncrementAttribute));
            _pPayloadAtt = (PayloadAttribute) prefix.AddAttribute(typeof (PayloadAttribute));
            _pOffsetAtt = (OffsetAttribute) prefix.AddAttribute(typeof (OffsetAttribute));
            _pTypeAtt = (TypeAttribute) prefix.AddAttribute(typeof (TypeAttribute));
            _pFlagsAtt = (FlagsAttribute) prefix.AddAttribute(typeof (FlagsAttribute));
        }

        public TokenStream Prefix { get; set; }

        public TokenStream Suffix { get; set; }

        public override sealed bool IncrementToken()
        {
            if (!_prefixExhausted)
            {
                Token nextToken = GetNextPrefixInputToken(_reusableToken);
                if (nextToken == null)
                {
                    _prefixExhausted = true;
                }
                else
                {
                    _previousPrefixToken.Reinit(nextToken);
                    // Make it a deep copy
                    Payload p = _previousPrefixToken.GetPayload();
                    if (p != null)
                    {
                        _previousPrefixToken.SetPayload((Payload) p.Clone());
                    }
                    SetCurrentToken(nextToken);
                    return true;
                }
            }

            Token nextSuffixToken = GetNextSuffixInputToken(_reusableToken);
            if (nextSuffixToken == null)
            {
                return false;
            }

            nextSuffixToken = UpdateSuffixToken(nextSuffixToken, _previousPrefixToken);
            SetCurrentToken(nextSuffixToken);
            return true;
        }

        /// <summary>
        /// @deprecated Will be removed in Lucene 3.0. This method is final, as it should not be overridden. Delegates to the backwards compatibility layer.
        /// </summary>
        /// <param name="reusableToken"></param>
        /// <returns></returns>
        [Obsolete("The new IncrementToken() and AttributeSource APIs should be used instead.")]
        public override sealed Token Next(Token reusableToken)
        {
            return base.Next(reusableToken);
        }

        /// <summary>
        /// @deprecated Will be removed in Lucene 3.0. This method is final, as it should not be overridden. Delegates to the backwards compatibility layer.
        /// </summary>
        /// <returns></returns>
        [Obsolete("The returned Token is a \"full private copy\" (not re-used across calls to Next()) but will be slower than calling Next(Token) or using the new IncrementToken() method with the new AttributeSource API.")]
        public override sealed Token Next()
        {
            return base.Next();
        }

        private void SetCurrentToken(Token token)
        {
            if (token == null) return;
            ClearAttributes();
            _termAtt.SetTermBuffer(token.TermBuffer(), 0, token.TermLength());
            _posIncrAtt.SetPositionIncrement(token.GetPositionIncrement());
            _flagsAtt.SetFlags(token.GetFlags());
            _offsetAtt.SetOffset(token.StartOffset(), token.EndOffset());
            _typeAtt.SetType(token.Type());
            _payloadAtt.SetPayload(token.GetPayload());
        }

        private Token GetNextPrefixInputToken(Token token)
        {
            if (!Prefix.IncrementToken()) return null;
            token.SetTermBuffer(_pTermAtt.TermBuffer(), 0, _pTermAtt.TermLength());
            token.SetPositionIncrement(_pPosIncrAtt.GetPositionIncrement());
            token.SetFlags(_pFlagsAtt.GetFlags());
            token.SetOffset(_pOffsetAtt.StartOffset(), _pOffsetAtt.EndOffset());
            token.SetType(_pTypeAtt.Type());
            token.SetPayload(_pPayloadAtt.GetPayload());
            return token;
        }

        private Token GetNextSuffixInputToken(Token token)
        {
            if (!Suffix.IncrementToken()) return null;
            token.SetTermBuffer(_termAtt.TermBuffer(), 0, _termAtt.TermLength());
            token.SetPositionIncrement(_posIncrAtt.GetPositionIncrement());
            token.SetFlags(_flagsAtt.GetFlags());
            token.SetOffset(_offsetAtt.StartOffset(), _offsetAtt.EndOffset());
            token.SetType(_typeAtt.Type());
            token.SetPayload(_payloadAtt.GetPayload());
            return token;
        }

        /// <summary>
        /// The default implementation adds last prefix token end offset to the suffix token start and end offsets.
        /// </summary>
        /// <param name="suffixToken">a token from the suffix stream</param>
        /// <param name="lastPrefixToken">the last token from the prefix stream</param>
        /// <returns>consumer token</returns>
        public virtual Token UpdateSuffixToken(Token suffixToken, Token lastPrefixToken)
        {
            suffixToken.SetStartOffset(lastPrefixToken.EndOffset() + suffixToken.StartOffset());
            suffixToken.SetEndOffset(lastPrefixToken.EndOffset() + suffixToken.EndOffset());
            return suffixToken;
        }

        public override void Close()
        {
            Prefix.Close();
            Suffix.Close();
        }

        public override void Reset()
        {
            base.Reset();

            if (Prefix != null)
            {
                _prefixExhausted = false;
                Prefix.Reset();
            }

            if (Suffix != null)
                Suffix.Reset();
        }
    }
}