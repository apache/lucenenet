using System;

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
    /// Base class for <see cref="DocIdSet"/> to be used with <see cref="IFieldCache"/>. The implementation
    /// of its iterator is very stupid and slow if the implementation of the
    /// <see cref="MatchDoc(int)"/> method is not optimized, as iterators simply increment
    /// the document id until <see cref="MatchDoc(int)"/> returns <c>true</c>. Because of this
    /// <see cref="MatchDoc(int)"/> must be as fast as possible and in no case do any
    /// I/O.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public class FieldCacheDocIdSet : DocIdSet
    {
        protected readonly int m_maxDoc;
        protected readonly IBits m_acceptDocs;
        private readonly Predicate<int> matchDoc;
        private readonly bool hasMatchDoc;

        // LUCENENET specific - added constructor to allow the class to be used without hand-coding
        // a subclass by passing a predicate.
        public FieldCacheDocIdSet(int maxDoc, IBits acceptDocs, Predicate<int> matchDoc)
        {
            this.matchDoc = matchDoc ?? throw new ArgumentNullException(nameof(matchDoc));
            this.hasMatchDoc = true;
            this.m_maxDoc = maxDoc;
            this.m_acceptDocs = acceptDocs;
        }

        protected FieldCacheDocIdSet(int maxDoc, IBits acceptDocs)
        {
            this.m_maxDoc = maxDoc;
            this.m_acceptDocs = acceptDocs;
        }

        /// <summary>
        /// This method checks, if a doc is a hit
        /// </summary>
        protected internal virtual bool MatchDoc(int doc) => hasMatchDoc && matchDoc(doc);

        /// <summary>
        /// This DocIdSet is always cacheable (does not go back
        /// to the reader for iteration)
        /// </summary>
        public override sealed bool IsCacheable => true;

        public override sealed IBits Bits => (m_acceptDocs is null) ? (IBits)new BitsAnonymousClass(this) : new BitsAnonymousClass2(this);

        private sealed class BitsAnonymousClass : IBits
        {
            private readonly FieldCacheDocIdSet outerInstance;

            public BitsAnonymousClass(FieldCacheDocIdSet outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public bool Get(int docid)
            {
                return outerInstance.MatchDoc(docid);
            }

            public int Length => outerInstance.m_maxDoc;
        }

        private sealed class BitsAnonymousClass2 : IBits
        {
            private readonly FieldCacheDocIdSet outerInstance;

            public BitsAnonymousClass2(FieldCacheDocIdSet outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public bool Get(int docid)
            {
                return outerInstance.MatchDoc(docid) && outerInstance.m_acceptDocs.Get(docid);
            }

            public int Length => outerInstance.m_maxDoc;
        }

        public override sealed DocIdSetIterator GetIterator()
        {
            if (m_acceptDocs is null)
            {
                // Specialization optimization disregard acceptDocs
                return new DocIdSetIteratorAnonymousClass(this);
            }
            else if (m_acceptDocs is FixedBitSet || m_acceptDocs is OpenBitSet)
            {
                // special case for FixedBitSet / OpenBitSet: use the iterator and filter it
                // (used e.g. when Filters are chained by FilteredQuery)
                return new FilteredDocIdSetIteratorAnonymousClass(this, ((DocIdSet)m_acceptDocs).GetIterator());
            }
            else
            {
                // Stupid consultation of acceptDocs and matchDoc()
                return new DocIdSetIteratorAnonymousClass2(this);
            }
        }

        private sealed class DocIdSetIteratorAnonymousClass : DocIdSetIterator
        {
            private readonly FieldCacheDocIdSet outerInstance;

            public DocIdSetIteratorAnonymousClass(FieldCacheDocIdSet outerInstance)
            {
                this.outerInstance = outerInstance;
                doc = -1;
            }

            private int doc;

            public override int DocID => doc;

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

            public override long GetCost()
            {
                return outerInstance.m_maxDoc;
            }
        }

        private sealed class FilteredDocIdSetIteratorAnonymousClass : FilteredDocIdSetIterator
        {
            private readonly FieldCacheDocIdSet outerInstance;

            public FilteredDocIdSetIteratorAnonymousClass(FieldCacheDocIdSet outerInstance, Lucene.Net.Search.DocIdSetIterator iterator)
                : base(iterator)
            {
                this.outerInstance = outerInstance;
            }

            protected override bool Match(int doc)
            {
                return outerInstance.MatchDoc(doc);
            }
        }

        private sealed class DocIdSetIteratorAnonymousClass2 : DocIdSetIterator
        {
            private readonly FieldCacheDocIdSet outerInstance;

            public DocIdSetIteratorAnonymousClass2(FieldCacheDocIdSet outerInstance)
            {
                this.outerInstance = outerInstance;
                doc = -1;
            }

            private int doc;

            public override int DocID => doc;

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

            public override long GetCost()
            {
                return outerInstance.m_maxDoc;
            }
        }
    }
}