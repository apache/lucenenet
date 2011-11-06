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
using System.Diagnostics;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;

namespace Lucene.Net.Analyzers.Miscellaneous
{
    /// <summary>
    /// A TokenStream containing a single token.
    /// </summary>
    public class SingleTokenTokenStream : TokenStream
    {
        private readonly AttributeImpl _tokenAtt;
        private bool _exhausted;

        // The token needs to be immutable, so work with clones!
        private Token _singleToken;

        public SingleTokenTokenStream(Token token)
        {
            Debug.Assert(token != null, "Token was null!");
            _singleToken = (Token) token.Clone();

            // ReSharper disable DoNotCallOverridableMethodsInConstructor
            _tokenAtt = (AttributeImpl) AddAttribute(typeof (TermAttribute));
            // ReSharper restore DoNotCallOverridableMethodsInConstructor

            Debug.Assert(_tokenAtt is Token || _tokenAtt.GetType().Name.Equals(typeof (TokenWrapper).Name),
                         "Token Attribute is the wrong type! Type was: " + _tokenAtt.GetType().Name + " but expected " +
                         typeof (TokenWrapper).Name);
        }

        public override sealed bool IncrementToken()
        {
            if (_exhausted)
                return false;

            ClearAttributes();
            _singleToken.CopyTo(_tokenAtt);
            _exhausted = true;

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
        [Obsolete(
            "The returned Token is a \"full private copy\" (not re-used across calls to Next()) but will be slower than calling Next(Token) or using the new IncrementToken() method with the new AttributeSource API."
            )]
        public override sealed Token Next()
        {
            return base.Next();
        }

        public override void Reset()
        {
            _exhausted = false;
        }

        public Token GetToken()
        {
            return (Token) _singleToken.Clone();
        }

        public void SetToken(Token token)
        {
            _singleToken = (Token) token.Clone();
        }
    }
}