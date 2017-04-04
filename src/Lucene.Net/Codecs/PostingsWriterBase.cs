using System;

namespace Lucene.Net.Codecs
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

    using DataOutput = Lucene.Net.Store.DataOutput;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IndexOutput = Lucene.Net.Store.IndexOutput;

    /// <summary>
    /// Extension of <seealso cref="PostingsConsumer"/> to support pluggable term dictionaries.
    /// <p>
    /// this class contains additional hooks to interact with the provided
    /// term dictionaries such as <seealso cref="BlockTreeTermsWriter"/>. If you want
    /// to re-use an existing implementation and are only interested in
    /// customizing the format of the postings list, extend this class
    /// instead.
    /// </summary>
    /// <seealso cref= PostingsReaderBase
    /// @lucene.experimental </seealso>
    // TODO: find a better name; this defines the API that the
    // terms dict impls use to talk to a postings impl.
    // TermsDict + PostingsReader/WriterBase == PostingsConsumer/Producer
    public abstract class PostingsWriterBase : PostingsConsumer, IDisposable
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        ///  constructors, typically implicit.)
        /// </summary>
        protected internal PostingsWriterBase()
        {
        }

        /// <summary>
        /// Called once after startup, before any terms have been
        ///  added.  Implementations typically write a header to
        ///  the provided {@code termsOut}.
        /// </summary>
        public abstract void Init(IndexOutput termsOut);

        /// <summary>
        /// Return a newly created empty TermState </summary>
        public abstract BlockTermState NewTermState();

        /// <summary>
        /// Start a new term.  Note that a matching call to {@link
        ///  #finishTerm(BlockTermState)} is done, only if the term has at least one
        ///  document.
        /// </summary>
        public abstract void StartTerm();

        /// <summary>
        /// Finishes the current term.  The provided {@link
        ///  BlockTermState} contains the term's summary statistics,
        ///  and will holds metadata from PBF when returned
        /// </summary>
        public abstract void FinishTerm(BlockTermState state);

        /// <summary>
        /// Encode metadata as long[] and byte[]. {@code absolute} controls whether
        /// current term is delta encoded according to latest term.
        /// Usually elements in {@code longs} are file pointers, so each one always
        /// increases when a new term is consumed. {@code out} is used to write generic
        /// bytes, which are not monotonic.
        ///
        /// NOTE: sometimes long[] might contain "don't care" values that are unused, e.g.
        /// the pointer to postings list may not be defined for some terms but is defined
        /// for others, if it is designed to inline  some postings data in term dictionary.
        /// In this case, the postings writer should always use the last value, so that each
        /// element in metadata long[] remains monotonic.
        /// </summary>
        public abstract void EncodeTerm(long[] longs, DataOutput @out, FieldInfo fieldInfo, BlockTermState state, bool absolute);

        /// <summary>
        /// Sets the current field for writing, and returns the
        /// fixed length of long[] metadata (which is fixed per
        /// field), called when the writing switches to another field.
        /// </summary>
        // TODO: better name?
        public abstract int SetField(FieldInfo fieldInfo);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}