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
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using OpenBitSet = Lucene.Net.Util.OpenBitSet;

    /// <summary>
    /// Base class for DocIdSet to be used with FieldCache. The implementation
    /// of its iterator is very stupid and slow if the implementation of the
    /// <seealso cref="#matchDoc"/> method is not optimized, as iterators simply increment
    /// the document id until {@code matchDoc(int)} returns true. Because of this
    /// {@code matchDoc(int)} must be as fast as possible and in no case do any
    /// I/O.
    /// @lucene.internal
    /// </summary>
    public abstract class FieldCacheDocIdSet : DocIdSet
    {
        protected readonly int m_maxDoc;
        protected readonly IBits m_acceptDocs;

        public FieldCacheDocIdSet(int maxDoc, IBits acceptDocs)
        {
            this.m_maxDoc = maxDoc;
            this.m_acceptDocs = acceptDocs;
        }

        /// <summary>
        /// this method checks, if a doc is a hit
        /// </summary>
        protected internal abstract bool MatchDoc(int doc);

        /// <summary>
        /// this DocIdSet is always cacheable (does not go back
        /// to the reader for iteration)
        /// </summary>
        public override sealed bool IsCacheable
        {
            get
            {
                return true;
            }
        }

        public override sealed IBits GetBits()
        {
            return (m_acceptDocs == null) ? (IBits)new BitsAnonymousInnerClassHelper(this) : new BitsAnonymousInnerClassHelper2(this);
        }

        private class BitsAnonymousInnerClassHelper : IBits
        {
            private readonly FieldCacheDocIdSet outerInstance;

            public BitsAnonymousInnerClassHelper(FieldCacheDocIdSet outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public virtual bool Get(int docid)
            {
                return outerInstance.MatchDoc(docid);
            }

            public virtual int Length()
            {
                return outerInstance.m_maxDoc;
            }
        }

        private class BitsAnonymousInnerClassHelper2 : IBits
        {
            private readonly FieldCacheDocIdSet outerInstance;

            public BitsAnonymousInnerClassHelper2(FieldCacheDocIdSet outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public virtual bool Get(int docid)
            {
                return outerInstance.MatchDoc(docid) && outerInstance.m_acceptDocs.Get(docid);
            }

            public virtual int Length()
            {
                return outerInstance.m_maxDoc;
            }
        }

        public override sealed DocIdSetIterator GetIterator()
        {
            if (m_acceptDocs == null)
            {
                // Specialization optimization disregard acceptDocs
                return new DocIdSetIteratorAnonymousInnerClassHelper(this);
            }
            else if (m_acceptDocs is FixedBitSet || m_acceptDocs is OpenBitSet)
            {
                // special case for FixedBitSet / OpenBitSet: use the iterator and filter it
                // (used e.g. when Filters are chained by FilteredQuery)
                return new FilteredDocIdSetIteratorAnonymousInnerClassHelper(this, ((DocIdSet)m_acceptDocs).GetIterator());
            }
            else
            {
                // Stupid consultation of acceptDocs and matchDoc()
                return new DocIdSetIteratorAnonymousInnerClassHelper2(this);
            }
        }

        private class DocIdSetIteratorAnonymousInnerClassHelper : DocIdSetIterator
        {
            private readonly FieldCacheDocIdSet outerInstance;

            public DocIdSetIteratorAnonymousInnerClassHelper(FieldCacheDocIdSet outerInstance)
            {
                this.outerInstance = outerInstance;
                doc = -1;
            }

            private int doc;

            public override int DocID
            {
                get { return doc; }
            }

            public override int NextDoc()
            {
                do
                {
                    doc++;
                    if (doc >= outerInstance.m_maxDoc)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                } while (!outerInstance.MatchDoc(doc));
                return doc;
            }

            public override int Advance(int target)
            {
                for (doc = target; doc < outerInstance.m_maxDoc; doc++)
                {
                    if (outerInstance.MatchDoc(doc))
                    {
                        return doc;
                    }
                }
                return doc = NO_MORE_DOCS;
            }

            public override long Cost()
            {
                return outerInstance.m_maxDoc;
            }
        }

        private class FilteredDocIdSetIteratorAnonymousInnerClassHelper : FilteredDocIdSetIterator
        {
            private readonly FieldCacheDocIdSet outerInstance;

            public FilteredDocIdSetIteratorAnonymousInnerClassHelper(FieldCacheDocIdSet outerInstance, Lucene.Net.Search.DocIdSetIterator iterator)
                : base(iterator)
            {
                this.outerInstance = outerInstance;
            }

            protected override bool Match(int doc)
            {
                return outerInstance.MatchDoc(doc);
            }
        }

        private class DocIdSetIteratorAnonymousInnerClassHelper2 : DocIdSetIterator
        {
            private readonly FieldCacheDocIdSet outerInstance;

            public DocIdSetIteratorAnonymousInnerClassHelper2(FieldCacheDocIdSet outerInstance)
            {
                this.outerInstance = outerInstance;
                doc = -1;
            }

            private int doc;

            public override int DocID
            {
                get { return doc; }
            }

            public override int NextDoc()
            {
                do
                {
                    doc++;
                    if (doc >= outerInstance.m_maxDoc)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                } while (!(outerInstance.MatchDoc(doc) && outerInstance.m_acceptDocs.Get(doc)));
                return doc;
            }

            public override int Advance(int target)
            {
                for (doc = target; doc < outerInstance.m_maxDoc; doc++)
                {
                    if (outerInstance.MatchDoc(doc) && outerInstance.m_acceptDocs.Get(doc))
                    {
                        return doc;
                    }
                }
                return doc = NO_MORE_DOCS;
            }

            public override long Cost()
            {
                return outerInstance.m_maxDoc;
            }
        }
    }
}