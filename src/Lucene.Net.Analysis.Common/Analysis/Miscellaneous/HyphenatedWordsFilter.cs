// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Text;

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
    /// When the plain text is extracted from documents, we will often have many words hyphenated and broken into
    /// two lines. This is often the case with documents where narrow text columns are used, such as newsletters.
    /// In order to increase search efficiency, this filter puts hyphenated words broken into two lines back together.
    /// This filter should be used on indexing time only.
    /// Example field definition in schema.xml:
    /// <code>
    /// &lt;fieldtype name="text" class="solr.TextField" positionIncrementGap="100"&gt;
    ///  &lt;analyzer type="index"&gt;
    ///    &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///      &lt;filter class="solr.SynonymFilterFactory" synonyms="index_synonyms.txt" ignoreCase="true" expand="false"/&gt;
    ///      &lt;filter class="solr.StopFilterFactory" ignoreCase="true"/&gt;
    ///      &lt;filter class="solr.HyphenatedWordsFilterFactory"/&gt;
    ///      &lt;filter class="solr.WordDelimiterFilterFactory" generateWordParts="1" generateNumberParts="1" catenateWords="1" catenateNumbers="1" catenateAll="0"/&gt;
    ///      &lt;filter class="solr.LowerCaseFilterFactory"/&gt;
    ///      &lt;filter class="solr.RemoveDuplicatesTokenFilterFactory"/&gt;
    ///  &lt;/analyzer&gt;
    ///  &lt;analyzer type="query"&gt;
    ///      &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///      &lt;filter class="solr.SynonymFilterFactory" synonyms="synonyms.txt" ignoreCase="true" expand="true"/&gt;
    ///      &lt;filter class="solr.StopFilterFactory" ignoreCase="true"/&gt;
    ///      &lt;filter class="solr.WordDelimiterFilterFactory" generateWordParts="1" generateNumberParts="1" catenateWords="0" catenateNumbers="0" catenateAll="0"/&gt;
    ///      &lt;filter class="solr.LowerCaseFilterFactory"/&gt;
    ///      &lt;filter class="solr.RemoveDuplicatesTokenFilterFactory"/&gt;
    ///  &lt;/analyzer&gt;
    /// &lt;/fieldtype&gt;
    /// </code>
    /// </summary>
    public sealed class HyphenatedWordsFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAttribute;
        private readonly IOffsetAttribute offsetAttribute;

        private readonly StringBuilder hyphenated = new StringBuilder();
        private State savedState;
        private bool exhausted = false;
        private int lastEndOffset = 0;

        /// <summary>
        /// Creates a new <see cref="HyphenatedWordsFilter"/>
        /// </summary>
        /// <param name="in"> <see cref="TokenStream"/> that will be filtered </param>
        public HyphenatedWordsFilter(TokenStream @in)
            : base(@in)
        {
            termAttribute = AddAttribute<ICharTermAttribute>();
            offsetAttribute = AddAttribute<IOffsetAttribute>();
        }

        /// <summary>
        /// Consumers (i.e., <see cref="Index.IndexWriter"/>) use this method to advance the stream to
        /// the next token. Implementing classes must implement this method and update
        /// the appropriate <see cref="Lucene.Net.Util.Attribute"/>s with the attributes of the next
        /// token.
        /// <para/>
        /// The producer must make no assumptions about the attributes after the method
        /// has been returned: the caller may arbitrarily change it. If the producer
        /// needs to preserve the state for subsequent calls, it can use
        /// <see cref="Lucene.Net.Util.AttributeSource.CaptureState"/> to create a copy of the current attribute state.
        /// <para/>
        /// this method is called for every token of a document, so an efficient
        /// implementation is crucial for good performance. To avoid calls to
        /// <see cref="Lucene.Net.Util.AttributeSource.AddAttribute{T}"/> and <see cref="Lucene.Net.Util.AttributeSource.GetAttribute{T}"/>,
        /// references to all <see cref="Lucene.Net.Util.Attribute"/>s that this stream uses should be
        /// retrieved during instantiation.
        /// <para/>
        /// To ensure that filters and consumers know which attributes are available,
        /// the attributes must be added during instantiation. Filters and consumers
        /// are not required to check for availability of attributes in
        /// <see cref="IncrementToken()"/>.
        /// </summary>
        /// <returns> false for end of stream; true otherwise </returns>
        public override bool IncrementToken()
        {
            while (!exhausted && m_input.IncrementToken())
            {
                char[] term = termAttribute.Buffer;
                int termLength = termAttribute.Length;
                lastEndOffset = offsetAttribute.EndOffset;

                if (termLength > 0 && term[termLength - 1] == '-')
                {
                    // a hyphenated word
                    // capture the state of the first token only
                    if (savedState is null)
                    {
                        savedState = CaptureState();
                    }
                    hyphenated.Append(term, 0, termLength - 1);
                }
                else if (savedState is null)
                {
                    // not part of a hyphenated word.
                    return true;
                }
                else
                {
                    // the final portion of a hyphenated word
                    hyphenated.Append(term, 0, termLength);
                    Unhyphenate();
                    return true;
                }
            }

            exhausted = true;

            if (savedState != null)
            {
                // the final term ends with a hyphen
                // add back the hyphen, for backwards compatibility.
                hyphenated.Append('-');
                Unhyphenate();
                return true;
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
        /// <remarks>
        /// <b>NOTE:</b>
        /// The default implementation chains the call to the input <see cref="TokenStream"/>, so
        /// be sure to call <c>base.Reset()</c> when overriding this method.
        /// </remarks>
        public override void Reset()
        {
            base.Reset();
            hyphenated.Length = 0;
            savedState = null;
            exhausted = false;
            lastEndOffset = 0;
        }

        // ================================================= Helper Methods ================================================

        /// <summary>
        /// Writes the joined unhyphenated term
        /// </summary>
        private void Unhyphenate()
        {
            RestoreState(savedState);
            savedState = null;

            char[] term = termAttribute.Buffer;
            int length = hyphenated.Length;
            if (length > termAttribute.Length)
            {
                term = termAttribute.ResizeBuffer(length);
            }

            hyphenated.CopyTo(0, term, 0, length);
            termAttribute.Length = length;
            offsetAttribute.SetOffset(offsetAttribute.StartOffset, lastEndOffset);
            hyphenated.Length = 0;
        }
    }
}