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
    /// A TokenFilter is a TokenStream whose input is another TokenStream.
    ///  <p>
    ///  this is an abstract class; subclasses must override <seealso cref="#IncrementToken()"/>. </summary>
    ///  <seealso cref= TokenStream </seealso>
    public abstract class TokenFilter : TokenStream
    {
        /// <summary>
        /// The source of tokens for this filter. </summary>
        protected readonly TokenStream m_input;

        /// <summary>
        /// Construct a token stream filtering the given input. </summary>
        protected internal TokenFilter(TokenStream input)
            : base(input)
        {
            this.m_input = input;
        }

        /// <summary>
        /// {@inheritDoc}
        /// <p>
        /// <b>NOTE:</b>
        /// The default implementation chains the call to the input TokenStream, so
        /// be sure to call <code>super.end()</code> first when overriding this method.
        /// </summary>
        public override void End()
        {
            m_input.End();
        }

        /// <summary>
        /// {@inheritDoc}
        /// <p>
        /// <b>NOTE:</b>
        /// The default implementation chains the call to the input TokenStream, so
        /// be sure to call <code>super.Dispose()</code> when overriding this method.
        /// </summary>
        public override void Dispose()
        {
            m_input.Dispose();
        }

        /// <summary>
        /// {@inheritDoc}
        /// <p>
        /// <b>NOTE:</b>
        /// The default implementation chains the call to the input TokenStream, so
        /// be sure to call <code>super.reset()</code> when overriding this method.
        /// </summary>
        public override void Reset()
        {
            m_input.Reset();
        }
    }
}