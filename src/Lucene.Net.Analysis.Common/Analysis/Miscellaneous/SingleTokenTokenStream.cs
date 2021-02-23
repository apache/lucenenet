// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using System.Diagnostics;

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
    /// A <see cref="TokenStream"/> containing a single token.
    /// </summary>
    public sealed class SingleTokenTokenStream : TokenStream
    {
        private bool exhausted = false;

        // The token needs to be immutable, so work with clones!
        private Token singleToken;
        private readonly ICharTermAttribute tokenAtt;

        public SingleTokenTokenStream(Token token) 
            : base(Token.TOKEN_ATTRIBUTE_FACTORY)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(token != null);
            this.singleToken = (Token)token.Clone();

            tokenAtt = AddAttribute<ICharTermAttribute>();
            if (Debugging.AssertsEnabled) Debugging.Assert(tokenAtt is Token);
        }

        public override sealed bool IncrementToken()
        {
            if (exhausted)
            {
                return false;
            }
            else
            {
                ClearAttributes();
                singleToken.CopyTo(tokenAtt);
                exhausted = true;
                return true;
            }
        }

        public override void Reset()
        {
            exhausted = false;
        }

        public Token GetToken() // LUCENENET NOTE: These remain methods because they make a conversion of the value
        {
            return (Token)singleToken.Clone();
        }

        public void SetToken(Token token)
        {
            this.singleToken = (Token)token.Clone();
        }
    }
}