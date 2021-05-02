namespace Lucene.Net.Index
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
    /// Extension of <see cref="SortedSetDocValues"/> that supports random access
    /// to the ordinals of a document.
    /// <para/>
    /// Operations via this API are independent of the iterator api (<see cref="SortedSetDocValues.NextOrd()"/>)
    /// and do not impact its state.
    /// <para/>
    /// Codecs can optionally extend this API if they support constant-time access
    /// to ordinals for the document.
    /// </summary>
    public abstract class RandomAccessOrds : SortedSetDocValues
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected RandomAccessOrds()
        {
        }

        /// <summary>
        /// Retrieve the ordinal for the current document (previously
        /// set by <see cref="SortedSetDocValues.SetDocument(int)"/> at the specified index.
        /// <para/>
        /// An index ranges from <c>0</c> to <c>Cardinality-1</c>.
        /// The first ordinal value is at index <c>0</c>, the next at index <c>1</c>,
        /// and so on, as for array indexing. </summary>
        /// <param name="index"> index of the ordinal for the document. </param>
        /// <returns> ordinal for the document at the specified index. </returns>
        public abstract long OrdAt(int index);

        /// <summary>
        /// Gets the cardinality for the current document (previously
        /// set by <see cref="SortedSetDocValues.SetDocument(int)"/>.
        /// </summary>
        public abstract int Cardinality { get; }
    }
}