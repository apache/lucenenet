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

    using IBits = Lucene.Net.Util.IBits;
    using DataInput = Lucene.Net.Store.DataInput;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IndexInput = Lucene.Net.Store.IndexInput;

    /// <summary>
    /// The core terms dictionaries (BlockTermsReader,
    ///  BlockTreeTermsReader) interact with a single instance
    ///  of this class to manage creation of <seealso cref="DocsEnum"/> and
    ///  <seealso cref="DocsAndPositionsEnum"/> instances.  It provides an
    ///  IndexInput (termsIn) where this class may read any
    ///  previously stored data that it had written in its
    ///  corresponding <seealso cref="PostingsWriterBase"/> at indexing
    ///  time.
    ///  @lucene.experimental
    /// </summary>

    // TODO: find a better name; this defines the API that the
    // terms dict impls use to talk to a postings impl.
    // TermsDict + PostingsReader/WriterBase == PostingsConsumer/Producer
    public abstract class PostingsReaderBase : IDisposable
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        ///  constructors, typically implicit.)
        /// </summary>
        protected internal PostingsReaderBase()
        {
        }

        /// <summary>
        /// Performs any initialization, such as reading and
        ///  verifying the header from the provided terms
        ///  dictionary <seealso cref="IndexInput"/>.
        /// </summary>
        public abstract void Init(IndexInput termsIn);

        /// <summary>
        /// Return a newly created empty TermState </summary>
        public abstract BlockTermState NewTermState();

        /// <summary>
        /// Actually decode metadata for next term </summary>
        ///  <seealso cref= PostingsWriterBase#encodeTerm  </seealso>
        public abstract void DecodeTerm(long[] longs, DataInput @in, FieldInfo fieldInfo, BlockTermState state, bool absolute);

        /// <summary>
        /// Must fully consume state, since after this call that
        ///  TermState may be reused.
        /// </summary>
        public abstract DocsEnum Docs(FieldInfo fieldInfo, BlockTermState state, IBits skipDocs, DocsEnum reuse, int flags);

        /// <summary>
        /// Must fully consume state, since after this call that
        ///  TermState may be reused.
        /// </summary>
        public abstract DocsAndPositionsEnum DocsAndPositions(FieldInfo fieldInfo, BlockTermState state, IBits skipDocs, DocsAndPositionsEnum reuse, int flags);

        /// <summary>
        /// Returns approximate RAM bytes used </summary>
        public abstract long RamBytesUsed();

        /// <summary>
        /// Checks consistency of this reader.
        /// <p>
        /// Note that this may be costly in terms of I/O, e.g.
        /// may involve computing a checksum value against large data files.
        /// @lucene.internal
        /// </summary>
        public abstract void CheckIntegrity();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}