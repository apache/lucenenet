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
    /// <remarks>
    /// If <see cref="IDisposable"/> is implemented on a <see cref="TokenFilter"/> subclass,
    /// a call to <see cref="Analyzer.Dispose()"/> will cascade the call to the <see cref="TokenFilter"/>
    /// automatically. This allows for final teardown of components that are only designed to be disposed
    /// once, since <see cref="Close()"/> may be called multiple times during a <see cref="TokenFilter"/>
    /// instance lifetime.
    /// </remarks>
    /// <seealso cref="TokenStream"/>
    public abstract class TokenFilter : TokenStream
    {
        /// <summary>
        /// The source of tokens for this filter.
        /// </summary>
        protected readonly TokenStream m_input;

        /// <summary>
        /// Construct a token stream filtering the given input.
        /// </summary>
        protected TokenFilter(TokenStream input)
            : base(input)
        {
            this.m_input = input;
        }

        /// <summary>
        /// <inheritdoc cref="TokenStream.End()"/>
        /// </summary>
        /// <remarks>
        /// <b>NOTE:</b>
        /// The default implementation chains the call to the input TokenStream, so
        /// be sure to call <c>base.End()</c> first when overriding this method.
        /// </remarks>
        /// <exception cref="IOException"> If an I/O error occurs </exception>
        public override void End()
        {
            m_input.End();
        }

        /// <summary>
        /// <inheritdoc cref="TokenStream.Close()"/>
        /// </summary>
        public override void Close()
        {
            m_input.Close();
            base.Close();
        }

        /// <summary>
        /// <inheritdoc cref="TokenStream.Reset()"/>
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
