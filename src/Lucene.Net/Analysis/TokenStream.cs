using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
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
    /// to store the information of a <see cref="Token"/> is to use <see cref="Util.Attribute"/>s.
    /// <para/>
    /// <see cref="TokenStream"/> now extends <see cref="AttributeSource"/>, which provides
    /// access to all of the token <see cref="Util.IAttribute"/>s for the <see cref="TokenStream"/>.
    /// Note that only one instance per <see cref="Util.Attribute"/> is created and reused
    /// for every token. This approach reduces object creation and allows local
    /// caching of references to the <see cref="Util.Attribute"/>s. See
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
    ///     <item><description>The consumer calls <see cref="Close()"/> to release any resource when finished
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
    /// <para/>
    /// LUCENENET: <see cref="Close()"/> may be called multiple times upon reuse of the <see cref="Analyzer"/>.
    /// If a <see cref="TokenStream"/> subclass implements <see cref="IDisposable"/>,
    /// a call to <see cref="Analyzer.Dispose()"/> will be cascaded to the <see cref="TokenStream"/>
    /// instance through <see cref="TokenStreamComponents"/>. This allows for final teardown of components
    /// that are only designed to be disposed once.
    /// </summary>
    public abstract class TokenStream : AttributeSource, ICloseable
    {
        // LUCENENET specific - track disposable TokenStreams that are part of the current chain
        private readonly DisposableTracker disposableTracker;

        /// <summary>
        /// A <see cref="TokenStream"/> using the default attribute factory.
        /// </summary>
        protected TokenStream()
        {
            // LUCENENET: Rather than using AssertFinal() to run Reflection code at runtime,
            // we are using a Roslyn code analyzer to ensure the rules are followed at compile time.

            // LUCENENET specific - track disposable TokenStreams that are part of the current chain
            disposableTracker = new DisposableTracker(This);
        }

        /// <summary>
        /// A <see cref="TokenStream"/> that uses the same attributes as the supplied one.
        /// </summary>
        protected TokenStream(AttributeSource input)
            : base(input)
        {
            // LUCENENET: Rather than using AssertFinal() to run Reflection code at runtime,
            // we are using a Roslyn code analyzer to ensure the rules are followed at compile time.

            // LUCENENET specific - track disposable TokenStreams that are part of the current chain
            if (input is TokenStream ts)
            {
                disposableTracker = ts.disposableTracker;
                disposableTracker.MaybeRegisterForDisposal(This);
            }
            else
            {
                disposableTracker = new DisposableTracker(This);
            }
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

            // LUCENENET specific - track disposable TokenStreams that are part of the current chain
            disposableTracker = new DisposableTracker(This);
        }

        /// <summary>
        /// LUCENENET: We cannot access <c>this</c> from the constructor, so this property is used
        /// to cheat the compiler.
        /// </summary>
        private TokenStream This => this;

        /// <summary>
        /// Consumers (i.e., <see cref="IndexWriter"/>) use this method to advance the stream to
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

        /// <summary>
        /// Releases resources associated with this stream.
        /// <para />
        /// If you override this method, always call <c>base.Close()</c>, otherwise
        /// some internal state will not be correctly reset (e.g., <see cref="Tokenizer"/> will
        /// throw <see cref="InvalidOperationException"/> on reuse).
        /// </summary>
        /// <remarks>
        /// LUCENENET NOTE: This is intended to release resources in a way that allows the
        /// instance to be reused, so it is not the same as <see cref="IDisposable.Dispose()"/>.
        /// Implementing <see cref="IDisposable"/> on your <see cref="TokenStream"/> subclass will
        /// cascade the call from <see cref="Analyzer.Dispose()"/> to your <see cref="TokenStream"/>
        /// automatically.
        /// </remarks>
        public virtual void Close()
        {
        }

        /// <summary>
        /// LUCENENET: Disposes all tracked wrapped <see cref="TokenStream"/>s that implement <see cref="IDisposable"/>.
        /// </summary>
        internal void DoDispose() => disposableTracker.Dispose();

        /// <summary>
        /// LUCENENET: <see cref="IDisposable"/> tracker class to act as a shared instance
        /// between <see cref="TokenStream"/> instances.
        /// This allows us to set <see cref="disposables"/> to <c>null</c> in all cases
        /// where there are no <see cref="IDisposable"/> <see cref="TokenStream"/>s in the
        /// decorator chain while still sharing a common reference so the <see cref="LinkedList{T}"/>
        /// can be instantiated by any participant and still referenced by all of them.
        /// </summary>
        private sealed class DisposableTracker : IDisposable
        {
            private LinkedList<IDisposable> disposables;

            /// <summary>
            /// Initializes a new instance of <see cref="DisposableTracker"/> and registers the
            /// supplied <paramref name="obj"/> if it implements <see cref="IDisposable"/>.
            /// </summary>
            /// <param name="obj">An object that may or may not implement <see cref="IDisposable"/>.</param>
            public DisposableTracker(object obj)
            {
                MaybeRegisterForDisposal(obj);
            }

            /// <summary>
            /// Registers an object for disposal if it implements <see cref="IDisposable"/>.
            /// </summary>
            /// <param name="obj">An object that may or may not implement <see cref="IDisposable"/>.</param>
            public void MaybeRegisterForDisposal(object obj)
            {
                // Register in reverse order (ensures correct teardown order)
                if (obj is IDisposable disposable)
                {
                    // Initialize disposables only if needed
                    disposables ??= new LinkedList<IDisposable>();
                    disposables.AddFirst(disposable);
                }
            }

            /// <summary>
            /// Disposes all registered objects.
            /// </summary>
            public void Dispose()
            {
                if (disposables is not null)
                {
                    IOUtils.Dispose(disposables);
                    disposables = null;
                }
            }
        }
    }
}
