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

    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum; // javadocs
    using Fields = Lucene.Net.Index.Fields;
    using IOffsetAttribute = Lucene.Net.Analysis.TokenAttributes.IOffsetAttribute; // javadocs

    /// <summary>
    /// Codec API for reading term vectors:
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class TermVectorsReader : IDisposable // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected TermVectorsReader()
        {
        }

        /// <summary>
        /// Returns term vectors for this document, or <c>null</c> if
        /// term vectors were not indexed. If offsets are
        /// available they are in an <see cref="IOffsetAttribute"/>
        /// available from the <see cref="DocsAndPositionsEnum"/>.
        /// </summary>
        public abstract Fields Get(int doc);

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
        /// Create a clone that one caller at a time may use to
        /// read term vectors.
        /// </summary>
        public abstract object Clone();

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