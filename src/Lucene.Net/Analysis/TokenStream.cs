using Lucene.Net.Analysis.TokenAttributes;
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

    using AttributeSource = Lucene.Net.Util.AttributeSource;

    /// <summary>
    /// A <see cref="TokenStream"/> enumerates the sequence of tokens, either from
    /// <see cref="Documents.Field"/>s of a <see cref="Documents.Document"/> or from query text.
    /// <para/>
    /// this is an abstract class; concrete subclasses are:
    /// <list type="bullet">
    ///     <item><description><see cref="Tokenizer"/>, a <see cref="TokenStream"/> whose input is a <see cref="TextReader"/>; and</description></item>
    ///     <item><description><see cref="TokenFilter"/>, a <see cref="TokenStream"/> whose input is another
    ///         <see cref="TokenStream"/>.</description></item>
    /// </list>
    /// A new <see cref="TokenStream"/> API has been introduced with Lucene 2.9. this API
    /// has moved from being <see cref="Token"/>-based to <see cref="Util.IAttribute"/>-based. While
    /// <see cref="Token"/> still exists in 2.9 as a convenience class, the preferred way
    /// to store the information of a <see cref="Token"/> is to use <see cref="Attribute"/>s.
    /// <para/>
    /// <see cref="TokenStream"/> now extends <see cref="AttributeSource"/>, which provides
    /// access to all of the token <see cref="Util.IAttribute"/>s for the <see cref="TokenStream"/>.
    /// Note that only one instance per <see cref="Attribute"/> is created and reused
    /// for every token. This approach reduces object creation and allows local
    /// caching of references to the <see cref="Attribute"/>s. See
    /// <see cref="IncrementToken()"/> for further details.
    /// <para/>
    /// <b>The workflow of the new <see cref="TokenStream"/> API is as follows:</b>
    /// <list type="number">
    ///     <item><description>Instantiation of <see cref="TokenStream"/>/<see cref="TokenFilter"/>s which add/get
    ///         attributes to/from the <see cref="AttributeSource"/>.</description></item>
    ///     <item><description>The consumer calls <see cref="TokenStream.Reset()"/>.</description></item>
    ///     <item><description>The consumer retrieves attributes from the stream and stores local
    ///         references to all attributes it wants to access.</description></item>
    ///     <item><description>The consumer calls <see cref="IncrementToken()"/> until it returns false
    ///         consuming the attributes after each call.</description></item>
    ///     <item><description>The consumer calls <see cref="End()"/> so that any end-of-stream operations
    ///         can be performed.</description></item>
    ///     <item><description>The consumer calls <see cref="Dispose()"/> to release any resource when finished
    ///         using the <see cref="TokenStream"/>.</description></item>
    /// </list>
    /// To make sure that filters and consumers know which attributes are available,
    /// the attributes must be added during instantiation. Filters and consumers are
    /// not required to check for availability of attributes in
    /// <see cref="IncrementToken()"/>.
    /// <para/>
    /// You can find some example code for the new API in the analysis 
    /// documentation.
    /// <para/>
    /// Sometimes it is desirable to capture a current state of a <see cref="TokenStream"/>,
    /// e.g., for buffering purposes (see <see cref="CachingTokenFilter"/>,
    /// TeeSinkTokenFilter). For this usecase
    /// <see cref="AttributeSource.CaptureState"/> and <see cref="AttributeSource.RestoreState"/>
    /// can be used.
    /// <para/>The <see cref="TokenStream"/>-API in Lucene is based on the decorator pattern.
    /// Therefore all non-abstract subclasses must be sealed or have at least a sealed
    /// implementation of <see cref="IncrementToken()"/>! This is checked when assertions are enabled.
    /// </summary>
    public abstract class TokenStream : AttributeSource, IDisposable
    {
        /// <summary>
        /// A <see cref="TokenStream"/> using the default attribute factory.
        /// </summary>
        protected TokenStream()
        {
            // LUCENENET: Rather than using AssertFinal() to run Reflection code at runtime,
            // we are using a Roslyn code analyzer to ensure the rules are followed at compile time.
        }

        /// <summary>
        /// A <see cref="TokenStream"/> that uses the same attributes as the supplied one.
        /// </summary>
        protected TokenStream(AttributeSource input)
            : base(input)
        {
            // LUCENENET: Rather than using AssertFinal() to run Reflection code at runtime,
            // we are using a Roslyn code analyzer to ensure the rules are followed at compile time.
        }

        /// <summary>
        /// A <see cref="TokenStream"/> using the supplied <see cref="AttributeSource.AttributeFactory"/> 
        /// for creating new <see cref="Util.IAttribute"/> instances.
        /// </summary>
        protected TokenStream(AttributeFactory factory)
            : base(factory)
        {
            // LUCENENET: Rather than using AssertFinal() to run Reflection code at runtime,
            // we are using a Roslyn code analyzer to ensure the rules are followed at compile time.
        }

        /// <summary>
        /// Consumers (i.e., <see cref="Index.IndexWriter"/>) use this method to advance the stream to
        /// the next token. Implementing classes must implement this method and update
        /// the appropriate <see cref="Lucene.Net.Util.IAttribute"/>s with the attributes of the next
        /// token.
        /// <para/>
        /// The producer must make no assumptions about the attributes after the method
        /// has been returned: the caller may arbitrarily change it. If the producer
        /// needs to preserve the state for subsequent calls, it can use
        /// <see cref="AttributeSource.CaptureState"/> to create a copy of the current attribute state.
        /// <para/>
        /// this method is called for every token of a document, so an efficient
        /// implementation is crucial for good performance. To avoid calls to
        /// <see cref="AttributeSource.AddAttribute{T}"/> and <see cref="AttributeSource.GetAttribute{T}"/>,
        /// references to all <see cref="Lucene.Net.Util.IAttribute"/>s that this stream uses should be
        /// retrieved during instantiation.
        /// <para/>
        /// To ensure that filters and consumers know which attributes are available,
        /// the attributes must be added during instantiation. Filters and consumers
        /// are not required to check for availability of attributes in
        /// <see cref="IncrementToken()"/>.
        /// </summary>
        /// <returns> false for end of stream; true otherwise </returns>
        public abstract bool IncrementToken();

        /// <summary>
        /// This method is called by the consumer after the last token has been
        /// consumed, after <see cref="IncrementToken()"/> returned <c>false</c>
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
        /// If you override this method, always call <c>base.End();</c>.
        /// </summary>
        /// <exception cref="IOException"> If an I/O error occurs </exception>
        public virtual void End()
        {
            ClearAttributes(); // LUCENE-3849: don't consume dirty atts

            if (HasAttribute<IPositionIncrementAttribute>())
            {
                var attr = GetAttribute<IPositionIncrementAttribute>();
                attr.PositionIncrement = 0;
            }
        }

        /// <summary>
        /// This method is called by a consumer before it begins consumption using
        /// <see cref="IncrementToken()"/>.
        /// <para/>
        /// Resets this stream to a clean state. Stateful implementations must implement
        /// this method so that they can be reused, just as if they had been created fresh.
        /// <para/>
        /// If you override this method, always call <c>base.Reset()</c>, otherwise
        /// some internal state will not be correctly reset (e.g., <see cref="Tokenizer"/> will
        /// throw <see cref="InvalidOperationException"/> on further usage).
        /// </summary>
        public virtual void Reset()
        {
        }

        // LUCENENET specific - implementing proper dispose pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources associated with this stream.
        /// <para/>
        /// If you override this method, always call <c>base.Dispose(disposing)</c>, otherwise
        /// some internal state will not be correctly reset (e.g., <see cref="Tokenizer"/> will
        /// throw <see cref="InvalidOperationException"/> on reuse).
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}