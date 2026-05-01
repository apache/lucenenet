// Lucene version compatibility level 4.8.1
using System.Diagnostics.CodeAnalysis;

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

        // LUCENENET specific: matches the upstream Java implementation, which does not call super.reset().
        [SuppressMessage("Design", "Lucene1001:TokenStream override of End()/Reset()/Close() must call the corresponding base method.",
            Justification = "Matches upstream Lucene Java behavior; TokenStream.Reset() is a no-op so the omission is equivalent.")]
        public override void Reset()
        {
            suffix.Reset();
        }

        // LUCENENET specific: matches the upstream Java implementation, which does not call super.close().
        [SuppressMessage("Design", "Lucene1001:TokenStream override of End()/Reset()/Close() must call the corresponding base method.",
            Justification = "Matches upstream Lucene Java behavior; TokenStream.Close() is a no-op so the omission is equivalent.")]
        public override void Close()
        {
            suffix.Close();
        }

        // LUCENENET TODO: the upstream Java does not call super.end() here, so neither do we — but
        // TokenStream.End() does ClearAttributes() and resets PositionIncrementAttribute, which the
        // documented contract says End() overrides should do. This may be a latent bug in upstream
        // Lucene that the .NET port should investigate; see Lucene.NET issue tracker if reproducing
        // a problem with end-of-stream attribute state on this filter.
        [SuppressMessage("Design", "Lucene1001:TokenStream override of End()/Reset()/Close() must call the corresponding base method.",
            Justification = "Matches upstream Lucene Java behavior; see LUCENENET TODO above for why this may need to be revisited.")]
        public override void End()
        {
            suffix.End();
        }
    }
}
