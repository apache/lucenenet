using System;
using System.IO;

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
    /// A <see cref="TokenFilter"/> is a <see cref="TokenStream"/> whose input is another <see cref="TokenStream"/>.
    /// <para/>
    /// This is an abstract class; subclasses must override <see cref="TokenStream.IncrementToken()"/>.
    /// </summary>
    /// <seealso cref="TokenStream"/>
    public abstract class TokenFilter : TokenStream
    {
        /// <summary>
        /// The source of tokens for this filter. </summary>
        protected readonly TokenStream m_input;

        /// <summary>
        /// Construct a token stream filtering the given input. </summary>
        protected TokenFilter(TokenStream input)
            : base(input)
        {
            this.m_input = input;
        }

        /// <summary>
        /// This method is called by the consumer after the last token has been
        /// consumed, after <see cref="TokenStream.IncrementToken()"/> returned <c>false</c>
        /// (using the new <see cref="TokenStream"/> API). Streams implementing the old API
        /// should upgrade to use this feature.
        /// <para/>
        /// This method can be used to perform any end-of-stream operations, such as
        /// setting the final offset of a stream. The final offset of a stream might
        /// differ from the offset of the last token eg in case one or more whitespaces
        /// followed after the last token, but a WhitespaceTokenizer was used.
        /// <para/>
        /// Additionally any skipped positions (such as those removed by a stopfilter)
        /// can be applied to the position increment, or any adjustment of other
        /// attributes where the end-of-stream value may be important.
        /// <para/>
        /// <b>NOTE:</b>
        /// The default implementation chains the call to the input TokenStream, so
        /// be sure to call <c>base.End()</c> first when overriding this method.
        /// </summary>
        /// <exception cref="IOException"> If an I/O error occurs </exception>
        public override void End()
        {
            m_input.End();
        }

        /// <summary>
        /// Releases resources associated with this stream.
        /// <para/>
        /// If you override this method, always call <c>base.Dispose(disposing)</c>, otherwise
        /// some internal state will not be correctly reset (e.g., <see cref="Tokenizer"/> will
        /// throw <see cref="InvalidOperationException"/> on reuse).
        /// <para/>
        /// <b>NOTE:</b>
        /// The default implementation chains the call to the input TokenStream, so
        /// be sure to call <c>base.Dispose(disposing)</c> when overriding this method.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_input.Dispose();
            }
            base.Dispose(disposing); // LUCENENET specific - disposable pattern requires calling the base class implementation
        }

        /// <summary>
        /// This method is called by a consumer before it begins consumption using
        /// <see cref="TokenStream.IncrementToken()"/>.
        /// <para/>
        /// Resets this stream to a clean state. Stateful implementations must implement
        /// this method so that they can be reused, just as if they had been created fresh.
        /// <para/>
        /// If you override this method, always call <c>base.Reset()</c>, otherwise
        /// some internal state will not be correctly reset (e.g., <see cref="Tokenizer"/> will
        /// throw <see cref="InvalidOperationException"/> on further usage).
        /// </summary>
        /// <remarks>
        /// <b>NOTE:</b>
        /// The default implementation chains the call to the input <see cref="TokenStream"/>, so
        /// be sure to call <c>base.Reset()</c> when overriding this method.
        /// </remarks>
        public override void Reset()
        {
            m_input.Reset();
        }
    }
}