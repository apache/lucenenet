// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;

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
    /// A <see cref="TokenFilter"/> which filters out <see cref="Token"/>s at the same position and Term text as the previous token in the stream.
    /// </summary>
    public sealed class RemoveDuplicatesTokenFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAttribute;
        private readonly IPositionIncrementAttribute posIncAttribute;

        // use a fixed version, as we don't care about case sensitivity.
        private readonly CharArraySet previous = new CharArraySet(
#pragma warning disable 612, 618
            LuceneVersion.LUCENE_31, 8, false);
#pragma warning restore 612, 618

        /// <summary>
        /// Creates a new RemoveDuplicatesTokenFilter
        /// </summary>
        /// <param name="in"> TokenStream that will be filtered </param>
        public RemoveDuplicatesTokenFilter(TokenStream @in)
            : base(@in)
        {
            termAttribute = AddAttribute<ICharTermAttribute>();
            posIncAttribute = AddAttribute<IPositionIncrementAttribute>();
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
        public override sealed bool IncrementToken()
        {
            while (m_input.IncrementToken())
            {
                char[] term = termAttribute.Buffer;
                int length = termAttribute.Length;
                int posIncrement = posIncAttribute.PositionIncrement;

                if (posIncrement > 0)
                {
                    previous.Clear();
                }

                bool duplicate = (posIncrement == 0 && previous.Contains(term, 0, length));

                // clone the term, and add to the set of seen terms.
                char[] saved = new char[length];
                Arrays.Copy(term, 0, saved, 0, length);
                previous.Add(saved);

                if (!duplicate)
                {
                    return true;
                }
            }
            return false;
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
        public override void Reset()
        {
            base.Reset();
            previous.Clear();
        }
    }
}