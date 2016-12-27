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

    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using IBits = Lucene.Net.Util.IBits;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;

    /// <summary>
    /// Abstract API that produces numeric, binary and
    /// sorted docvalues.
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class DocValuesProducer : IDisposable
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        ///  constructors, typically implicit.)
        /// </summary>
        protected internal DocValuesProducer()
        {
        }

        /// <summary>
        /// Returns <seealso cref="NumericDocValues"/> for this field.
        ///  The returned instance need not be thread-safe: it will only be
        ///  used by a single thread.
        /// </summary>
        public abstract NumericDocValues GetNumeric(FieldInfo field);

        /// <summary>
        /// Returns <seealso cref="BinaryDocValues"/> for this field.
        ///  The returned instance need not be thread-safe: it will only be
        ///  used by a single thread.
        /// </summary>
        public abstract BinaryDocValues GetBinary(FieldInfo field);

        /// <summary>
        /// Returns <seealso cref="SortedDocValues"/> for this field.
        ///  The returned instance need not be thread-safe: it will only be
        ///  used by a single thread.
        /// </summary>
        public abstract SortedDocValues GetSorted(FieldInfo field);

        /// <summary>
        /// Returns <seealso cref="SortedSetDocValues"/> for this field.
        ///  The returned instance need not be thread-safe: it will only be
        ///  used by a single thread.
        /// </summary>
        public abstract SortedSetDocValues GetSortedSet(FieldInfo field);

        /// <summary>
        /// Returns a <seealso cref="IBits"/> at the size of <code>reader.maxDoc()</code>,
        ///  with turned on bits for each docid that does have a value for this field.
        ///  The returned instance need not be thread-safe: it will only be
        ///  used by a single thread.
        /// </summary>
        public abstract IBits GetDocsWithField(FieldInfo field);

        /// <summary>
        /// Returns approximate RAM bytes used </summary>
        public abstract long RamBytesUsed();

        /// <summary>
        /// Checks consistency of this producer
        /// <p>
        /// Note that this may be costly in terms of I/O, e.g.
        /// may involve computing a checksum value against large data files.
        /// @lucene.internal
        /// </summary>
        public abstract void CheckIntegrity();

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}