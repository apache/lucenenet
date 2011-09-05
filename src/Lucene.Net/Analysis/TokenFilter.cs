// -----------------------------------------------------------------------
// <copyright company="Apache" file="TokenFilter.cs">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Analysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A <c>TokenFilter</c> is a <see cref="TokenStream"/> that wraps an inner <see cref="TokenStream"/> which
    /// serves as the input source for the stream.
    /// </summary>
    public abstract class TokenFilter : TokenStream
    {
        private readonly TokenStream tokenStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenFilter"/> class.
        /// </summary>
        /// <param name="tokenStream">The token stream.</param>
        protected TokenFilter(TokenStream tokenStream)
        {
            this.tokenStream = tokenStream;
        }

        /// <summary>
        /// Gets the inner token stream which is the input source for this stream.
        /// </summary>
        /// <value>The token stream.</value>
        protected virtual TokenStream TokenStream
        {
            get { return this.tokenStream; }
        }

        /// <summary>
        /// Closes the <see cref="TokenStream"/>.
        /// </summary>
        public override void Close()
        {
           this.TokenStream.Close();
        }

        /// <summary>
        /// End invokes end-of-stream operations, such as setting the final offset of a stream.
        /// Calls <see cref="Analysis.TokenStream.End"/> on the inner <see cref="TokenStream"/>
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method is called by the consumer after the last token has been
        ///         consumed. i.e. after <see cref="Analysis.TokenStream.IncrementToken"/> returns <c>false</c>.
        ///     </para>
        ///     <para>
        ///         This method can be used to perform any end-of-stream operations.
        ///         An example would be setting the final offset of a stream.
        ///         The final offset of a stream might differ from the offset of the last token.
        ///         This could be in case one or more whitespaces followed after the last
        ///         token, but a WhitespaceTokenizer was used.
        /// </para>
        /// </remarks>
        public override void End()
        {
            this.TokenStream.End();
        }

        /// <summary>
        /// Resets this stream to the beginning. Calls <see cref="Lucene.Net.Analysis.TokenStream.Reset()"/> on 
        /// the <see cref="TokenStream"/>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         <see cref="Reset"/>is not needed for the standard indexing
        ///         process. However, if the tokens of<see cref="TokenStream"/> are
        ///         intended to be consumed more than once, it is necessary to
        ///         implement <see cref="Reset"/>.
        ///     </para>
        ///     <note>
        ///         If a <see cref="TokenStream"/> caches tokens and feeds them back again after a
        ///         reset, it is imperative that you clone the tokens when you store them on the first pass.
        ///         It is also imperative to clone the tokens when you return them on future passes after
        ///         <see cref="Reset"/> is invoked.
        ///     </note>
        /// </remarks>
        public override void Reset()
        {
            this.TokenStream.Reset();
        }
    }
}