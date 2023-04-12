using Lucene.Net.Index;
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
    /// <see cref="BlockTreeTermsReader{TSubclassState}"/>) interact with a single instance
    /// of this class to manage creation of <see cref="DocsEnum"/> and
    /// <see cref="DocsAndPositionsEnum"/> instances.  It provides an
    /// <see cref="IndexInput"/> (termsIn) where this class may read any
    /// previously stored data that it had written in its
    /// corresponding <see cref="PostingsWriterBase"/> at indexing
    /// time.
    /// <para/>
    /// @lucene.experimental
    /// </summary>

    // TODO: find a better name; this defines the API that the
    // terms dict impls use to talk to a postings impl.
    // TermsDict + PostingsReader/WriterBase == PostingsConsumer/Producer
    public abstract class PostingsReaderBase : IDisposable
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected PostingsReaderBase()
        {
        }

        /// <summary>
        /// Performs any initialization, such as reading and
        /// verifying the header from the provided terms
        /// dictionary <see cref="IndexInput"/>.
        /// </summary>
        public abstract void Init(IndexInput termsIn);

        /// <summary>
        /// Return a newly created empty <see cref="TermState"/>. </summary>
        public abstract BlockTermState NewTermState();

        /// <summary>
        /// Actually decode metadata for next term. </summary>
        /// <seealso cref="PostingsWriterBase.EncodeTerm(long[], Store.DataOutput, FieldInfo, BlockTermState, bool)"/>
        public abstract void DecodeTerm(long[] longs, DataInput @in, FieldInfo fieldInfo, BlockTermState state, bool absolute);

        /// <summary>
        /// Must fully consume state, since after this call that
        /// <see cref="TermState"/> may be reused.
        /// </summary>
        public abstract DocsEnum Docs(FieldInfo fieldInfo, BlockTermState state, IBits skipDocs, DocsEnum reuse, DocsFlags flags);

        /// <summary>
        /// Must fully consume state, since after this call that
        /// <see cref="TermState"/> may be reused.
        /// </summary>
        public abstract DocsAndPositionsEnum DocsAndPositions(FieldInfo fieldInfo, BlockTermState state, IBits skipDocs, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags);

        /// <summary>
        /// Returns approximate RAM bytes used. </summary>
        public abstract long RamBytesUsed();

        /// <summary>
        /// Checks consistency of this reader.
        /// <para/>
        /// Note that this may be costly in terms of I/O, e.g.
        /// may involve computing a checksum value against large data files.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public abstract void CheckIntegrity();

        /// <summary>
        /// Disposes all resources used by this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implementations must override and should dispose all resources used by this instance.
        /// </summary>
        protected abstract void Dispose(bool disposing);
    }
}