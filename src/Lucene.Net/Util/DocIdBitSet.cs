using System.Runtime.CompilerServices;
using BitSet = J2N.Collections.BitSet;

namespace Lucene.Net.Util
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
    /// Simple <see cref="DocIdSet"/> and <see cref="DocIdSetIterator"/> backed by a <see cref="BitSet"/> 
    /// </summary>
    public class DocIdBitSet : DocIdSet, IBits
    {
        private readonly BitSet bitSet;

        public DocIdBitSet(BitSet bitSet)
        {
            this.bitSet = bitSet;
        }

        public override DocIdSetIterator GetIterator()
        {
            return new DocIdBitSetIterator(bitSet);
        }

        public override IBits Bits => this;

        /// <summary>
        /// This DocIdSet implementation is cacheable. </summary>
        public override bool IsCacheable => true;

        /// <summary>
        /// Returns the underlying <see cref="BitSet"/>.
        /// </summary>
        public virtual BitSet BitSet => this.bitSet;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int index)
        {
            return bitSet.Get(index);
        }

        public int Length =>
            // the size may not be correct...
            bitSet.Length;

        private class DocIdBitSetIterator : DocIdSetIterator
        {
            private int docId;
            private readonly BitSet bitSet;

            internal DocIdBitSetIterator(BitSet bitSet)
            {
                this.bitSet = bitSet;
                this.docId = -1;
            }

            public override int DocID => docId;

            public override int NextDoc()
            {
                // (docId + 1) on next line requires -1 initial value for docNr:
                var d = bitSet.NextSetBit(docId + 1);
                // -1 returned by BitSet.nextSetBit() when exhausted
                docId = d == -1 ? NO_MORE_DOCS : d;
                return docId;
            }

            public override int Advance(int target)
            {
                int d = bitSet.NextSetBit(target);
                // -1 returned by BitSet.nextSetBit() when exhausted
                docId = d == -1 ? NO_MORE_DOCS : d;
                return docId;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long GetCost()
            {
                // upper bound
                return bitSet.Length;
            }
        }
    }
}