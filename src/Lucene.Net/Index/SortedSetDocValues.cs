using J2N.Numerics;

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

    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// A per-document set of presorted <see cref="T:byte[]"/> values.
    /// <para/>
    /// Per-Document values in a <see cref="SortedDocValues"/> are deduplicated, dereferenced,
    /// and sorted into a dictionary of unique values. A pointer to the
    /// dictionary value (ordinal) can be retrieved for each document. Ordinals
    /// are dense and in increasing sorted order.
    /// </summary>
    public abstract class SortedSetDocValues
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected SortedSetDocValues()
        {
        }

        /// <summary>
        /// When returned by <see cref="NextOrd()"/> it means there are no more
        /// ordinals for the document.
        /// </summary>
        public static readonly long NO_MORE_ORDS = -1;

        /// <summary>
        /// Returns the next ordinal for the current document (previously
        /// set by <see cref="SetDocument(int)"/>. </summary>
        /// <returns> Next ordinal for the document, or <see cref="NO_MORE_ORDS"/>.
        ///         ordinals are dense, start at 0, then increment by 1 for
        ///         the next value in sorted order.  </returns>
        public abstract long NextOrd();

        /// <summary>
        /// Sets iteration to the specified docID </summary>
        /// <param name="docID"> document ID  </param>
        public abstract void SetDocument(int docID);

        /// <summary>
        /// Retrieves the value for the specified ordinal. </summary>
        /// <param name="ord"> ordinal to lookup </param>
        /// <param name="result"> will be populated with the ordinal's value </param>
        /// <seealso cref="NextOrd()"/>
        public abstract void LookupOrd(long ord, BytesRef result);

        /// <summary>
        /// Returns the number of unique values. </summary>
        /// <returns> Number of unique values in this <see cref="SortedDocValues"/>. This is
        ///         also equivalent to one plus the maximum ordinal. </returns>
        public abstract long ValueCount { get; }

        /// <summary>
        /// If <paramref name="key"/> exists, returns its ordinal, else
        /// returns <c>-insertionPoint-1</c>, like
        /// <see cref="System.Array.BinarySearch(System.Array, int, int, object)"/>.
        /// </summary>
        /// <param name="key"> Key to look up</param>
        public virtual long LookupTerm(BytesRef key)
        {
            BytesRef spare = new BytesRef();
            long low = 0;
            long high = ValueCount - 1;

            while (low <= high)
            {
                long mid = (low + high).TripleShift(1);
                LookupOrd(mid, spare);
                int cmp = spare.CompareTo(key);

                if (cmp < 0)
                {
                    low = mid + 1;
                }
                else if (cmp > 0)
                {
                    high = mid - 1;
                }
                else
                {
                    return mid; // key found
                }
            }

            return -(low + 1); // key not found.
        }

        /// <summary>
        /// Returns a <see cref="TermsEnum"/> over the values.
        /// The enum supports <see cref="TermsEnum.Ord"/> and <see cref="TermsEnum.SeekExact(long)"/>.
        /// </summary>
        public virtual TermsEnum GetTermsEnum()
        {
            return new SortedSetDocValuesTermsEnum(this);
        }
    }
}