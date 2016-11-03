using System;

namespace Lucene.Net.Codecs
{
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum; // javadocs
    using Fields = Lucene.Net.Index.Fields;

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

    using OffsetAttribute = Lucene.Net.Analysis.Tokenattributes.OffsetAttribute; // javadocs

    /// <summary>
    /// Codec API for reading term vectors:
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class TermVectorsReader : IDisposable
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        ///  constructors, typically implicit.)
        /// </summary>
        protected internal TermVectorsReader()
        {
        }

        /// <summary>
        /// Returns term vectors for this document, or null if
        ///  term vectors were not indexed. If offsets are
        ///  available they are in an <seealso cref="OffsetAttribute"/>
        ///  available from the <seealso cref="DocsAndPositionsEnum"/>.
        /// </summary>
        public abstract Fields Get(int doc);

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

        /// <summary>
        /// Create a clone that one caller at a time may use to
        ///  read term vectors.
        /// </summary>
        public abstract object Clone();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}