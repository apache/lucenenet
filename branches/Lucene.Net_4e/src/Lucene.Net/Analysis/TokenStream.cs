// -----------------------------------------------------------------------
// <copyright company="Apache" file="TokenStream.cs">
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
    using System.Diagnostics.CodeAnalysis;
    using Lucene.Net.Util;
    

    /// <summary>
    /// TODO: update summary when Field, Document, TokenFilter, CachingTokenFilter, and IndexWriter have been created or ported.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     All subclasses of Token stream must seal <see cref="IncrementToken"/>
    ///     </para>
    /// </remarks>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix",
        Justification = "The name is valid even if it does not derived from Stream.")]
    public abstract class TokenStream : AttributeSource, IDisposable
    {
        //// TODO: create fxcop or nunit/mbunit tests that assert subclasses of TokenStream to seal constructors or IncrementToken().
        //// https://cwiki.apache.org/confluence/display/LUCENENET/Lucene+Concepts

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenStream"/> class.
        /// </summary>
        protected TokenStream()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenStream"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        protected TokenStream(AttributeSource source)
            : base(source)
        {            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenStream"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        protected TokenStream(AttributeFactory factory)
            : base(factory)
        {  
        }

        /// <summary>
        /// Closes the <see cref="TokenStream"/>.
        /// </summary>
        public virtual void Close()
        {
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
           this.Dispose(true);
        }

        /// <summary>
        /// End invokes end-of-stream operations, such as setting the final offset of a stream. 
        /// The default implementation does nothing.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method is called by the consumer after the last token has been
        ///         consumed. i.e. after <see cref="IncrementToken"/> returns <c>false</c>.
        ///     </para>
        ///     <para>
        ///         This method can be used to perform any end-of-stream operations. 
        ///         An example would be setting the final offset of a stream. 
        ///         The final offset of a stream might differ from the offset of the last token.
        ///         This could be in case one or more whitespaces followed after the last 
        ///         token, but a WhitespaceTokenizer was used.
        ///     </para>
        /// </remarks>
        public virtual void End()
        {
        }


        /// <summary>
        /// Advances the stream to the next token. 
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     TODO: update remarks for IncrementToken when class summary is updated.
        ///     </para>
        /// </remarks>
        /// <returns>An instance of <see cref="Boolean"/>.</returns>
        public abstract bool IncrementToken();

        /// <summary>
        /// Resets this stream to the beginning. The default implementation does nothing.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         <see cref="Reset"/>is not needed for the standard indexing 
        ///         process. However, if the tokens of<see cref="TokenStream"/> are 
        ///         intended to be consumed more than once, it is necessary to 
        ///         implement <see cref="Reset"/>.
        ///     </para>
        ///     <note>
        ///         If a <see cref="TokenStream" /> caches tokens and feeds them back again after a 
        ///         reset, it is imperative that you clone the tokens when you store them on the first pass.
        ///         It is also imperative to clone the tokens when you return them on future passes after 
        ///         <see cref="Reset"/> is invoked.
        ///     </note>
        /// </remarks>
        public virtual void Reset()
        {
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="release"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool release)
        {
            this.Close();
        }
    }
}