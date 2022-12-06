namespace Lucene.Net.Search
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

    using IBits = Lucene.Net.Util.IBits;

    /// <summary>
    /// Abstract decorator class for a <see cref="DocIdSet"/> implementation
    /// that provides on-demand filtering/validation
    /// mechanism on a given <see cref="DocIdSet"/>.
    ///
    /// <para/>
    ///
    /// Technically, this same functionality could be achieved
    /// with ChainedFilter (under queries/), however the
    /// benefit of this class is it never materializes the full
    /// bitset for the filter.  Instead, the <see cref="Match(int)"/>
    /// method is invoked on-demand, per docID visited during
    /// searching.  If you know few docIDs will be visited, and
    /// the logic behind <see cref="Match(int)"/> is relatively costly,
    /// this may be a better way to filter than ChainedFilter.
    /// </summary>
    /// <seealso cref="DocIdSet"/>
    public abstract class FilteredDocIdSet : DocIdSet
    {
        private readonly DocIdSet innerSet;

        /// <summary>
        /// Constructor. </summary>
        /// <param name="innerSet"> Underlying <see cref="DocIdSet"/> </param>
        protected FilteredDocIdSet(DocIdSet innerSet) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.innerSet = innerSet;
        }

        /// <summary>
        /// This <see cref="DocIdSet"/> implementation is cacheable if the inner set is cacheable. </summary>
        public override bool IsCacheable => innerSet.IsCacheable;

        public override IBits Bits
        {
            get
            {
                IBits bits = innerSet.Bits;
                return (bits is null) ? null : new BitsAnonymousClass(this, bits);
            }
        }

        private sealed class BitsAnonymousClass : IBits
        {
            private readonly FilteredDocIdSet outerInstance;

            private readonly IBits bits;

            public BitsAnonymousClass(FilteredDocIdSet outerInstance, IBits bits)
            {
                this.outerInstance = outerInstance;
                this.bits = bits;
            }

            public bool Get(int docid)
            {
                return bits.Get(docid) && outerInstance.Match(docid);
            }

            public int Length => bits.Length;
        }

        /// <summary>
        /// Validation method to determine whether a docid should be in the result set. </summary>
        /// <param name="docid"> docid to be tested </param>
        /// <returns> <c>true</c> if input docid should be in the result set, false otherwise. </returns>
        protected abstract bool Match(int docid);

        /// <summary>
        /// Implementation of the contract to build a <see cref="DocIdSetIterator"/>. </summary>
        /// <seealso cref="DocIdSetIterator"/>
        /// <seealso cref="FilteredDocIdSetIterator"/>
        public override DocIdSetIterator GetIterator()
        {
            DocIdSetIterator iterator = innerSet.GetIterator();
            if (iterator is null)
            {
                return null;
            }
            return new FilteredDocIdSetIteratorAnonymousClass(this, iterator);
        }

        private sealed class FilteredDocIdSetIteratorAnonymousClass : FilteredDocIdSetIterator
        {
            private readonly FilteredDocIdSet outerInstance;

            public FilteredDocIdSetIteratorAnonymousClass(FilteredDocIdSet outerInstance, Lucene.Net.Search.DocIdSetIterator iterator)
                : base(iterator)
            {
                this.outerInstance = outerInstance;
            }

            protected override bool Match(int docid)
            {
                return outerInstance.Match(docid);
            }
        }
    }
}