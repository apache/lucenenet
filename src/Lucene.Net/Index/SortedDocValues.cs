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
    /// A per-document <see cref="T:byte[]"/> with presorted values.
    /// <para/>
    /// Per-Document values in a <see cref="SortedDocValues"/> are deduplicated, dereferenced,
    /// and sorted into a dictionary of unique values. A pointer to the
    /// dictionary value (ordinal) can be retrieved for each document. Ordinals
    /// are dense and in increasing sorted order.
    /// </summary>
    public abstract class SortedDocValues : BinaryDocValues
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected SortedDocValues()
        {
        }

        /// <summary>
        /// Returns the ordinal for the specified docID. </summary>
        /// <param name="docID"> document ID to lookup </param>
        /// <returns> ordinal for the document: this is dense, starts at 0, then
        ///         increments by 1 for the next value in sorted order. Note that
        ///         missing values are indicated by -1. </returns>
        public abstract int GetOrd(int docID);

        /// <summary>
        /// Retrieves the value for the specified ordinal. </summary>
        /// <param name="ord"> ordinal to lookup (must be &gt;= 0 and &lt; <see cref="ValueCount"/>) </param>
        /// <param name="result"> will be populated with the ordinal's value </param>
        /// <seealso cref="GetOrd(int)"/>
        public abstract void LookupOrd(int ord, BytesRef result);

        /// <summary>
        /// Returns the number of unique values. </summary>
        /// <returns> Number of unique values in this <see cref="SortedDocValues"/>. This is
        ///         also equivalent to one plus the maximum ordinal. </returns>
        public abstract int ValueCount { get; }

        public override void Get(int docID, BytesRef result)
        {
            int ord = GetOrd(docID);
            if (ord == -1)
            {
                result.Bytes = BytesRef.EMPTY_BYTES;
                result.Length = 0;
                result.Offset = 0;
            }
            else
            {
                LookupOrd(ord, result);
            }
        }

        /// <summary>
        /// If <paramref name="key"/> exists, returns its ordinal, else
        /// returns <c>-insertionPoint-1</c>, like 
        /// <see cref="System.Array.BinarySearch(System.Array, int, int, object)"/>
        /// </summary>
        /// <param name="key"> Key to look up</param>
        public virtual int LookupTerm(BytesRef key)
        {
            BytesRef spare = new BytesRef();
            int low = 0;
            int high = ValueCount - 1;

            while (low <= high)
            {
                int mid = (low + high).TripleShift(1);
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
            return new SortedDocValuesTermsEnum(this);
        }
    }
}