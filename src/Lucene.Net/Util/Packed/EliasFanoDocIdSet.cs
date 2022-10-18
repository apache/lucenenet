using System;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Util.Packed
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

    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

    /// <summary>
    /// A DocIdSet in Elias-Fano encoding.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public class EliasFanoDocIdSet : DocIdSet
    {
        internal readonly EliasFanoEncoder efEncoder;

        /// <summary>
        /// Construct an EliasFanoDocIdSet. For efficient encoding, the parameters should be chosen as low as possible. </summary>
        /// <param name="numValues"> At least the number of document ids that will be encoded. </param>
        /// <param name="upperBound">  At least the highest document id that will be encoded. </param>
        public EliasFanoDocIdSet(int numValues, int upperBound)
        {
            efEncoder = new EliasFanoEncoder(numValues, upperBound);
        }

        /// <summary>
        /// Provide an indication that is better to use an <see cref="EliasFanoDocIdSet"/> than a <see cref="FixedBitSet"/>
        /// to encode document identifiers. </summary>
        /// <param name="numValues"> The number of document identifiers that is to be encoded. Should be non negative. </param>
        /// <param name="upperBound"> The maximum possible value for a document identifier. Should be at least <paramref name="numValues"/>. </param>
        /// <returns> See <see cref="EliasFanoEncoder.SufficientlySmallerThanBitSet(long, long)"/> </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SufficientlySmallerThanBitSet(long numValues, long upperBound)
        {
            return EliasFanoEncoder.SufficientlySmallerThanBitSet(numValues, upperBound);
        }

        /// <summary>
        /// Encode the document ids from a DocIdSetIterator. </summary>
        /// <param name="disi"> This DocIdSetIterator should provide document ids that are consistent
        ///              with <c>numValues</c> and <c>upperBound</c> as provided to the constructor.   </param>
        public virtual void EncodeFromDisi(DocIdSetIterator disi)
        {
            while (efEncoder.numEncoded < efEncoder.numValues)
            {
                int x = disi.NextDoc();
                if (x == DocIdSetIterator.NO_MORE_DOCS)
                {
                    throw new ArgumentException("disi: " + disi.ToString() + "\nhas " + efEncoder.numEncoded + " docs, but at least " + efEncoder.numValues + " are required.");
                }
                efEncoder.EncodeNext(x);
            }
        }

        /// <summary>
        /// Provides a <see cref="DocIdSetIterator"/> to access encoded document ids.
        /// </summary>
        public override DocIdSetIterator GetIterator()
        {
            if (efEncoder.lastEncoded >= DocIdSetIterator.NO_MORE_DOCS)
            {
                throw UnsupportedOperationException.Create("Highest encoded value too high for DocIdSetIterator.NO_MORE_DOCS: " + efEncoder.lastEncoded);
            }
            return new DocIdSetIteratorAnonymousClass(this);
        }

        private sealed class DocIdSetIteratorAnonymousClass : DocIdSetIterator
        {
            public DocIdSetIteratorAnonymousClass(EliasFanoDocIdSet outerInstance)
            {
                curDocId = -1;
                efDecoder = outerInstance.efEncoder.GetDecoder();
            }

            private int curDocId;
            private readonly EliasFanoDecoder efDecoder;

            public override int DocID => curDocId;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int SetCurDocID(long value)
            {
                curDocId = (value == EliasFanoDecoder.NO_MORE_VALUES) ? NO_MORE_DOCS : (int)value;
                return curDocId;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int NextDoc()
            {
                return SetCurDocID(efDecoder.NextValue());
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int Advance(int target)
            {
                return SetCurDocID(efDecoder.AdvanceToValue(target));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long GetCost()
            {
                return efDecoder.NumEncoded;
            }
        }

        /// <summary>
        /// This DocIdSet implementation is cacheable. </summary>
        /// <returns> <c>true</c> </returns>
        public override bool IsCacheable => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            return (other is EliasFanoDocIdSet otherEncoder) && efEncoder.Equals(otherEncoder.efEncoder);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return efEncoder.GetHashCode() ^ this.GetType().GetHashCode();
        }
    }
}