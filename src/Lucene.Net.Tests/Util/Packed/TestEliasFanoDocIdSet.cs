using Lucene.Net.Diagnostics;
using BitSet = J2N.Collections.BitSet;

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

    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

    public class TestEliasFanoDocIdSet : BaseDocIdSetTestCase<EliasFanoDocIdSet>
    {
        public override EliasFanoDocIdSet CopyOf(BitSet bs, int numBits)
        {
            EliasFanoDocIdSet set = new EliasFanoDocIdSet((int)bs.Cardinality, numBits - 1);
            set.EncodeFromDisi(new DocIdSetIteratorAnonymousClass(bs, numBits));
            return set;
        }

        private sealed class DocIdSetIteratorAnonymousClass : DocIdSetIterator
        {
            private readonly BitSet bs;
            private readonly int numBits;

            public DocIdSetIteratorAnonymousClass(BitSet bs, int numBits)
            {
                this.bs = bs;
                this.numBits = numBits;
                doc = -1;
            }

            internal int doc;

            public override int NextDoc()
            {
                doc = bs.NextSetBit(doc + 1);
                if (doc == -1)
                {
                    doc = NO_MORE_DOCS;
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(doc < numBits);
                return doc;
            }

            public override int DocID => doc;

            public override long GetCost()
            {
                return bs.Cardinality;
            }

            public override int Advance(int target)
            {
                return SlowAdvance(target);
            }
        }
    }
}