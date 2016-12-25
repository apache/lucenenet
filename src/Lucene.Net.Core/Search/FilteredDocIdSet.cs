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

    using Bits = Lucene.Net.Util.Bits;

    /// <summary>
    /// Abstract decorator class for a DocIdSet implementation
    /// that provides on-demand filtering/validation
    /// mechanism on a given DocIdSet.
    ///
    /// <p/>
    ///
    /// Technically, this same functionality could be achieved
    /// with ChainedFilter (under queries/), however the
    /// benefit of this class is it never materializes the full
    /// bitset for the filter.  Instead, the <seealso cref="#match"/>
    /// method is invoked on-demand, per docID visited during
    /// searching.  If you know few docIDs will be visited, and
    /// the logic behind <seealso cref="#match"/> is relatively costly,
    /// this may be a better way to filter than ChainedFilter.
    /// </summary>
    /// <seealso cref= DocIdSet </seealso>

    public abstract class FilteredDocIdSet : DocIdSet
    {
        private readonly DocIdSet innerSet;

        /// <summary>
        /// Constructor. </summary>
        /// <param name="innerSet"> Underlying DocIdSet </param>
        public FilteredDocIdSet(DocIdSet innerSet)
        {
            this.innerSet = innerSet;
        }

        /// <summary>
        /// this DocIdSet implementation is cacheable if the inner set is cacheable. </summary>
        public override bool IsCacheable
        {
            get
            {
                return innerSet.IsCacheable;
            }
        }

        public override Bits GetBits()
        {
            Bits bits = innerSet.GetBits();
            return (bits == null) ? null : new BitsAnonymousInnerClassHelper(this, bits);
        }

        private class BitsAnonymousInnerClassHelper : Bits
        {
            private readonly FilteredDocIdSet outerInstance;

            private Bits bits;

            public BitsAnonymousInnerClassHelper(FilteredDocIdSet outerInstance, Bits bits)
            {
                this.outerInstance = outerInstance;
                this.bits = bits;
            }

            public virtual bool Get(int docid)
            {
                return bits.Get(docid) && outerInstance.Match(docid);
            }

            public virtual int Length()
            {
                return bits.Length();
            }
        }

        /// <summary>
        /// Validation method to determine whether a docid should be in the result set. </summary>
        /// <param name="docid"> docid to be tested </param>
        /// <returns> true if input docid should be in the result set, false otherwise. </returns>
        protected abstract bool Match(int docid);

        /// <summary>
        /// Implementation of the contract to build a DocIdSetIterator. </summary>
        /// <seealso cref= DocIdSetIterator </seealso>
        /// <seealso cref= FilteredDocIdSetIterator </seealso>
        public override DocIdSetIterator GetIterator()
        {
            DocIdSetIterator iterator = innerSet.GetIterator();
            if (iterator == null)
            {
                return null;
            }
            return new FilteredDocIdSetIteratorAnonymousInnerClassHelper(this, iterator);
        }

        private class FilteredDocIdSetIteratorAnonymousInnerClassHelper : FilteredDocIdSetIterator
        {
            private readonly FilteredDocIdSet outerInstance;

            public FilteredDocIdSetIteratorAnonymousInnerClassHelper(FilteredDocIdSet outerInstance, Lucene.Net.Search.DocIdSetIterator iterator)
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