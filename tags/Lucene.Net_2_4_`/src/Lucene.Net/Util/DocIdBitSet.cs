/**
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

using BitArray = System.Collections.BitArray;
using DocIdSet = Lucene.Net.Search.DocIdSet;
using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

namespace Lucene.Net.Util
{
    /// <summary>Simple DocIdSet and DocIdSetIterator backed by a BitArray</summary>
    public class DocIdBitSet : DocIdSet
    {
        private BitArray bitArray;

        public DocIdBitSet(BitArray bitArray)
        {
            this.bitArray = bitArray;
        }

        public override DocIdSetIterator Iterator()
        {
            return new DocIdBitSetIterator(bitArray);
        }

        /// <summary>Returns the underlying BitArray.</summary>
        public BitArray GetBitSet()
        {
            return this.bitArray;
        }

        private class DocIdBitSetIterator : DocIdSetIterator
        {
            private int docId;
            private BitArray bitArray;

            internal DocIdBitSetIterator(BitArray bitArray)
            {
                this.bitArray = bitArray;
                this.docId = -1;
            }

            public override int Doc()
            {
                System.Diagnostics.Debug.Assert(docId != -1);
                return docId;
            }

            public override bool Next()
            {
                // (docId + 1) on next line requires -1 initial value for docNr:
                return CheckNextDocId(SupportClass.BitSetSupport.NextSetBit(bitArray, docId + 1));
            }

            public override bool SkipTo(int skipDocNr)
            {
                return CheckNextDocId(SupportClass.BitSetSupport.NextSetBit(bitArray, skipDocNr));
            }

            private bool CheckNextDocId(int d)
            {
                if (d == -1)
                { // -1 returned by BitArray.nextSetBit() when exhausted
                    docId = int.MaxValue;
                    return false;
                }
                else
                {
                    docId = d;
                    return true;
                }
            }
        }
    }
}
