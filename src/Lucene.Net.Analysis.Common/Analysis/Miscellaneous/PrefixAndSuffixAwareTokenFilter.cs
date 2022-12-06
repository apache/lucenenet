// Lucene version compatibility level 4.8.1
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
    /// Links two <see cref="PrefixAwareTokenFilter"/>.
    /// <para/>
    /// <b>NOTE:</b> This filter might not behave correctly if used with custom 
    /// <see cref="Lucene.Net.Util.IAttribute"/>s, i.e. <see cref="Lucene.Net.Util.IAttribute"/>s other than
    /// the ones located in Lucene.Net.Analysis.TokenAttributes. 
    /// </summary>
    public class PrefixAndSuffixAwareTokenFilter : TokenStream
    {
        private readonly PrefixAwareTokenFilter suffix;

        public PrefixAndSuffixAwareTokenFilter(TokenStream prefix, TokenStream input, TokenStream suffix) 
            : base(suffix)
        {
            prefix = new PrefixAwareTokenFilterAnonymousClass(this, prefix, input);
            this.suffix = new PrefixAwareTokenFilterAnonymousClass2(this, prefix, suffix);
        }

        private sealed class PrefixAwareTokenFilterAnonymousClass : PrefixAwareTokenFilter
        {
            private readonly PrefixAndSuffixAwareTokenFilter outerInstance;

            public PrefixAwareTokenFilterAnonymousClass(PrefixAndSuffixAwareTokenFilter outerInstance, TokenStream prefix, TokenStream input) 
                : base(prefix, input)
            {
                this.outerInstance = outerInstance;
            }

            public override Token UpdateSuffixToken(Token suffixToken, Token lastInputToken)
            {
                return outerInstance.UpdateInputToken(suffixToken, lastInputToken);
            }
        }

        private sealed class PrefixAwareTokenFilterAnonymousClass2 : PrefixAwareTokenFilter
        {
            private readonly PrefixAndSuffixAwareTokenFilter outerInstance;

            public PrefixAwareTokenFilterAnonymousClass2(PrefixAndSuffixAwareTokenFilter outerInstance, TokenStream prefix, TokenStream suffix) 
                : base(prefix, suffix)
            {
                this.outerInstance = outerInstance;
            }

            public override Token UpdateSuffixToken(Token suffixToken, Token lastInputToken)
            {
                return outerInstance.UpdateSuffixToken(suffixToken, lastInputToken);
            }
        }

        public virtual Token UpdateInputToken(Token inputToken, Token lastPrefixToken)
        {
            inputToken.SetOffset(lastPrefixToken.EndOffset + inputToken.StartOffset, lastPrefixToken.EndOffset + inputToken.EndOffset);
            return inputToken;
        }

        public virtual Token UpdateSuffixToken(Token suffixToken, Token lastInputToken)
        {
            suffixToken.SetOffset(lastInputToken.EndOffset + suffixToken.StartOffset, lastInputToken.EndOffset + suffixToken.EndOffset);
            return suffixToken;
        }

        public sealed override bool IncrementToken()
        {
            return suffix.IncrementToken();
        }

        public override void Reset()
        {
            suffix.Reset();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                suffix.Dispose();
            }
            base.Dispose(disposing); // LUCENENET specific - disposable pattern requires calling the base class implementation
        }

        public override void End()
        {
            suffix.End();
        }
    }
}