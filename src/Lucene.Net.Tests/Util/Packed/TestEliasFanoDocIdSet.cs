using Lucene.Net.Support;
using NUnit.Framework;
using System.Collections;
using System.Diagnostics;

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
        public override EliasFanoDocIdSet CopyOf(BitArray bs, int numBits)
        {
            EliasFanoDocIdSet set = new EliasFanoDocIdSet(bs.Cardinality(), numBits - 1);
            set.EncodeFromDisi(new DocIdSetIteratorAnonymousInnerClassHelper(this, bs, numBits));
            return set;
        }

        private class DocIdSetIteratorAnonymousInnerClassHelper : DocIdSetIterator
        {
            private readonly TestEliasFanoDocIdSet OuterInstance;

            private BitArray Bs;
            private int NumBits;

            public DocIdSetIteratorAnonymousInnerClassHelper(TestEliasFanoDocIdSet outerInstance, BitArray bs, int numBits)
            {
                this.OuterInstance = outerInstance;
                this.Bs = bs;
                this.NumBits = numBits;
                doc = -1;
            }

            internal int doc;

            public override int NextDoc()
            {
                doc = Bs.NextSetBit(doc + 1);
                if (doc == -1)
                {
                    doc = NO_MORE_DOCS;
                }
                Debug.Assert(doc < NumBits);
                return doc;
            }

            public override int DocID
            {
                get { return doc; }
            }

            public override long GetCost()
            {
                return Bs.Cardinality();
            }

            public override int Advance(int target)
            {
                return SlowAdvance(target);
            }
        }
    }
}