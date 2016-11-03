using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Lucene.Net.Analysis.Tokenattributes;
using System;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Util;

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

    using Attribute = Lucene.Net.Util.Attribute;
    using AttributeSource = Lucene.Net.Util.AttributeSource;

    /// <summary>
    /// A <code>TokenStream</code> enumerates the sequence of tokens, either from
    /// <seealso cref="Field"/>s of a <seealso cref="Document"/> or from query text.
    /// <p>
    /// this is an abstract class; concrete subclasses are:
    /// <ul>
    /// <li><seealso cref="Tokenizer"/>, a <code>TokenStream</code> whose input is a Reader; and
    /// <li><seealso cref="TokenFilter"/>, a <code>TokenStream</code> whose input is another
    /// <code>TokenStream</code>.
    /// </ul>
    /// A new <code>TokenStream</code> API has been introduced with Lucene 2.9. this API
    /// has moved from being <seealso cref="Token"/>-based to <seealso cref="Attribute"/>-based. While
    /// <seealso cref="Token"/> still exists in 2.9 as a convenience class, the preferred way
    /// to store the information of a <seealso cref="Token"/> is to use <seealso cref="AttributeImpl"/>s.
    /// <p>
    /// <code>TokenStream</code> now extends <seealso cref="AttributeSource"/>, which provides
    /// access to all of the token <seealso cref="Attribute"/>s for the <code>TokenStream</code>.
    /// Note that only one instance per <seealso cref="AttributeImpl"/> is created and reused
    /// for every token. this approach reduces object creation and allows local
    /// caching of references to the <seealso cref="AttributeImpl"/>s. See
    /// <seealso cref="#IncrementToken()"/> for further details.
    /// <p>
    /// <b>The workflow of the new <code>TokenStream</code> API is as follows:</b>
    /// <ol>
    /// <li>Instantiation of <code>TokenStream</code>/<seealso cref="TokenFilter"/>s which add/get
    /// attributes to/from the <seealso cref="AttributeSource"/>.
    /// <li>The consumer calls <seealso cref="TokenStream#reset()"/>.
    /// <li>The consumer retrieves attributes from the stream and stores local
    /// references to all attributes it wants to access.
    /// <li>The consumer calls <seealso cref="#IncrementToken()"/> until it returns false
    /// consuming the attributes after each call.
    /// <li>The consumer calls <seealso cref="#end()"/> so that any end-of-stream operations
    /// can be performed.
    /// <li>The consumer calls <seealso cref="#close()"/> to release any resource when finished
    /// using the <code>TokenStream</code>.
    /// </ol>
    /// To make sure that filters and consumers know which attributes are available,
    /// the attributes must be added during instantiation. Filters and consumers are
    /// not required to check for availability of attributes in
    /// <seealso cref="#IncrementToken()"/>.
    /// <p>
    /// You can find some example code for the new API in the analysis package level
    /// Javadoc.
    /// <p>
    /// Sometimes it is desirable to capture a current state of a <code>TokenStream</code>,
    /// e.g., for buffering purposes (see <seealso cref="CachingTokenFilter"/>,
    /// TeeSinkTokenFilter). For this usecase
    /// <seealso cref="AttributeSource#captureState"/> and <seealso cref="AttributeSource#restoreState"/>
    /// can be used.
    /// <p>The {@code TokenStream}-API in Lucene is based on the decorator pattern.
    /// Therefore all non-abstract subclasses must be final or have at least a final
    /// implementation of <seealso cref="#incrementToken"/>! this is checked when Java
    /// assertions are enabled.
    /// </summary>
    public abstract class TokenStream : AttributeSource, IDisposable
    {
        /// <summary>
        /// A TokenStream using the default attribute factory.
        /// </summary>
        protected TokenStream()
        {
            AssertFinal();
        }

        /// <summary>
        /// A TokenStream that uses the same attributes as the supplied one.
        /// </summary>
        protected TokenStream(AttributeSource input)
            : base(input)
        {
            AssertFinal();
        }

        /// <summary>
        /// A TokenStream using the supplied AttributeFactory for creating new <seealso cref="Attribute"/> instances.
        /// </summary>
        protected TokenStream(AttributeFactory factory)
            : base(factory)
        {
            AssertFinal();
        }

        private bool AssertFinal()
        {
            var type = this.GetType();

            //if (!type.desiredAssertionStatus()) return true; // not supported in .NET

            var hasCompilerGeneratedAttribute =
                type.GetTypeInfo().GetCustomAttributes(typeof (CompilerGeneratedAttribute), false).Any();
            var isAnonymousType = hasCompilerGeneratedAttribute && type.FullName.Contains("AnonymousType");

            var method = type.GetMethod("IncrementToken", BindingFlags.Public | BindingFlags.Instance);

            if (!(isAnonymousType || type.GetTypeInfo().IsSealed || (method != null && method.IsFinal)))            
            {
                // Original Java code throws an AssertException via Java's assert, we can't do this here
                throw new InvalidOperationException("TokenStream implementation classes or at least their IncrementToken() implementation must be marked sealed");
            }

            return true;
        }

        /// <summary>
        /// Consumers (i.e., <seealso cref="IndexWriter"/>) use this method to advance the stream to
        /// the next token. Implementing classes must implement this method and update
        /// the appropriate <seealso cref="AttributeImpl"/>s with the attributes of the next
        /// token.
        /// <P>
        /// The producer must make no assumptions about the attributes after the method
        /// has been returned: the caller may arbitrarily change it. If the producer
        /// needs to preserve the state for subsequent calls, it can use
        /// <seealso cref="#captureState"/> to create a copy of the current attribute state.
        /// <p>
        /// this method is called for every token of a document, so an efficient
        /// implementation is crucial for good performance. To avoid calls to
        /// <seealso cref="#addAttribute(Class)"/> and <seealso cref="#getAttribute(Class)"/>,
        /// references to all <seealso cref="AttributeImpl"/>s that this stream uses should be
        /// retrieved during instantiation.
        /// <p>
        /// To ensure that filters and consumers know which attributes are available,
        /// the attributes must be added during instantiation. Filters and consumers
        /// are not required to check for availability of attributes in
        /// <seealso cref="#IncrementToken()"/>.
        /// </summary>
        /// <returns> false for end of stream; true otherwise </returns>
        public abstract bool IncrementToken();

        /// <summary>
        /// this method is called by the consumer after the last token has been
        /// consumed, after <seealso cref="#IncrementToken()"/> returned <code>false</code>
        /// (using the new <code>TokenStream</code> API). Streams implementing the old API
        /// should upgrade to use this feature.
        /// <p/>
        /// this method can be used to perform any end-of-stream operations, such as
        /// setting the final offset of a stream. The final offset of a stream might
        /// differ from the offset of the last token eg in case one or more whitespaces
        /// followed after the last token, but a WhitespaceTokenizer was used.
        /// <p>
        /// Additionally any skipped positions (such as those removed by a stopfilter)
        /// can be applied to the position increment, or any adjustment of other
        /// attributes where the end-of-stream value may be important.
        /// <p>
        /// If you override this method, always call {@code super.end()}.
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
        /// this method is called by a consumer before it begins consumption using
        /// <seealso cref="#IncrementToken()"/>.
        /// <p>
        /// Resets this stream to a clean state. Stateful implementations must implement
        /// this method so that they can be reused, just as if they had been created fresh.
        /// <p>
        /// If you override this method, always call {@code super.reset()}, otherwise
        /// some internal state will not be correctly reset (e.g., <seealso cref="Tokenizer"/> will
        /// throw <seealso cref="IllegalStateException"/> on further usage).
        /// </summary>
        public virtual void Reset()
        {
        }

        /// <summary>
        /// Releases resources associated with this stream.
        /// <p>
        /// If you override this method, always call {@code super.Dispose()}, otherwise
        /// some internal state will not be correctly reset (e.g., <seealso cref="Tokenizer"/> will
        /// throw <seealso cref="IllegalStateException"/> on reuse).
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}