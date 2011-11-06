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

namespace Lucene.Net.Analyzers.Miscellaneous
{
    /// <summary>
    /// Links two PrefixAwareTokenFilter.
    /// <p/>
    /// <b>NOTE:</b> This filter might not behave correctly if used with custom Attributes, i.e. Attributes other than
    /// the ones located in Lucene.Net.Analysis.Tokenattributes.  
    /// </summary>
    public class PrefixAndSuffixAwareTokenFilter : TokenStream
    {
        private readonly PrefixAwareTokenFilter _suffix;

        public PrefixAndSuffixAwareTokenFilter(TokenStream prefix, TokenStream input, TokenStream suffix) : base(suffix)
        {
            _suffix =
                new InjectablePrefixAwareTokenFilter(
                    new InjectablePrefixAwareTokenFilter(prefix, input)
                        {
                            UpdateAction = UpdateInputToken
                        },
                    suffix)
                    {
                        UpdateAction = UpdateSuffixToken
                    };
        }

        public Token UpdateInputToken(Token inputToken, Token lastPrefixToken)
        {
            inputToken.SetStartOffset(lastPrefixToken.EndOffset() + inputToken.StartOffset());
            inputToken.SetEndOffset(lastPrefixToken.EndOffset() + inputToken.EndOffset());
            return inputToken;
        }

        public Token UpdateSuffixToken(Token suffixToken, Token lastInputToken)
        {
            suffixToken.SetStartOffset(lastInputToken.EndOffset() + suffixToken.StartOffset());
            suffixToken.SetEndOffset(lastInputToken.EndOffset() + suffixToken.EndOffset());
            return suffixToken;
        }


        public override sealed bool IncrementToken()
        {
            return _suffix.IncrementToken();
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
        [Obsolete("The returned Token is a \"full private copy\" (not re-used across calls to Next()) but will be slower than calling Next(Token) or using the new IncrementToken() method with the new AttributeSource API.")]
        public override sealed Token Next()
        {
            return base.Next();
        }

        public override void Reset()
        {
            _suffix.Reset();
        }


        public override void Close()
        {
            _suffix.Close();
        }
    }
}