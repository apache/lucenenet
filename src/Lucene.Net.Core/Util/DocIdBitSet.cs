using System.Collections;

namespace Lucene.Net.Util
{
    using Lucene.Net.Support;

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
    /// Simple DocIdSet and DocIdSetIterator backed by a BitSet </summary>
    public class DocIdBitSet : DocIdSet, Bits
    {
        private readonly BitArray bitSet;

        public DocIdBitSet(BitArray bitSet)
        {
            this.bitSet = bitSet;
        }

        public override DocIdSetIterator GetIterator()
        {
            return new DocIdBitSetIterator(bitSet);
        }

        public override Bits GetBits()
        {
            return this;
        }

        /// <summary>
        /// this DocIdSet implementation is cacheable. </summary>
        public override bool Cacheable
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Returns the underlying BitSet.
        /// </summary>
        public virtual BitArray BitSet
        {
            get
            {
                return this.bitSet;
            }
        }

        public bool Get(int index)
        {
            return bitSet.SafeGet(index);
        }

        public int Length()
        {
            // the size may not be correct...
            return bitSet.Length;
        }

        private class DocIdBitSetIterator : DocIdSetIterator
        {
            private int DocId;
            private readonly BitArray bitSet;

            internal DocIdBitSetIterator(BitArray bitSet)
            {
                this.bitSet = bitSet;
                this.DocId = -1;
            }

            public override int DocID()
            {
                return DocId;
            }

            public override int NextDoc()
            {
                // (docId + 1) on next line requires -1 initial value for docNr:
                var d = bitSet.NextSetBit(DocId + 1);
                // -1 returned by BitSet.nextSetBit() when exhausted
                DocId = d == -1 ? NO_MORE_DOCS : d;
                return DocId;
            }

            public override int Advance(int target)
            {
                int d = bitSet.NextSetBit(target);
                // -1 returned by BitSet.nextSetBit() when exhausted
                DocId = d == -1 ? NO_MORE_DOCS : d;
                return DocId;
            }

            public override long Cost()
            {
                // upper bound
                return bitSet.Length;
            }
        }
    }
}