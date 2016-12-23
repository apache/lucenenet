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
        protected readonly int MaxDoc; // LUCENENET TODO: Rename
        protected readonly Bits AcceptDocs; // LUCENENET TODO: Rename

        public FieldCacheDocIdSet(int maxDoc, Bits acceptDocs)
        {
            this.MaxDoc = maxDoc;
            this.AcceptDocs = acceptDocs;
        }

        /// <summary>
        /// this method checks, if a doc is a hit
        /// </summary>
        protected internal abstract bool MatchDoc(int doc);

        /// <summary>
        /// this DocIdSet is always cacheable (does not go back
        /// to the reader for iteration)
        /// </summary>
        public override sealed bool Cacheable
        {
            get
            {
                return true;
            }
        }

        public override sealed Bits GetBits()
        {
            return (AcceptDocs == null) ? (Bits)new BitsAnonymousInnerClassHelper(this) : new BitsAnonymousInnerClassHelper2(this);
        }

        private class BitsAnonymousInnerClassHelper : Bits
        {
            private readonly FieldCacheDocIdSet OuterInstance;

            public BitsAnonymousInnerClassHelper(FieldCacheDocIdSet outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public virtual bool Get(int docid)
            {
                return OuterInstance.MatchDoc(docid);
            }

            public virtual int Length()
            {
                return OuterInstance.MaxDoc;
            }
        }

        private class BitsAnonymousInnerClassHelper2 : Bits
        {
            private readonly FieldCacheDocIdSet OuterInstance;

            public BitsAnonymousInnerClassHelper2(FieldCacheDocIdSet outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public virtual bool Get(int docid)
            {
                return OuterInstance.MatchDoc(docid) && OuterInstance.AcceptDocs.Get(docid);
            }

            public virtual int Length()
            {
                return OuterInstance.MaxDoc;
            }
        }

        public override sealed DocIdSetIterator GetIterator()
        {
            if (AcceptDocs == null)
            {
                // Specialization optimization disregard acceptDocs
                return new DocIdSetIteratorAnonymousInnerClassHelper(this);
            }
            else if (AcceptDocs is FixedBitSet || AcceptDocs is OpenBitSet)
            {
                // special case for FixedBitSet / OpenBitSet: use the iterator and filter it
                // (used e.g. when Filters are chained by FilteredQuery)
                return new FilteredDocIdSetIteratorAnonymousInnerClassHelper(this, ((DocIdSet)AcceptDocs).GetIterator());
            }
            else
            {
                // Stupid consultation of acceptDocs and matchDoc()
                return new DocIdSetIteratorAnonymousInnerClassHelper2(this);
            }
        }

        private class DocIdSetIteratorAnonymousInnerClassHelper : DocIdSetIterator
        {
            private readonly FieldCacheDocIdSet OuterInstance;

            public DocIdSetIteratorAnonymousInnerClassHelper(FieldCacheDocIdSet outerInstance)
            {
                this.OuterInstance = outerInstance;
                doc = -1;
            }

            private int doc;

            public override int DocID()
            {
                return doc;
            }

            public override int NextDoc()
            {
                do
                {
                    doc++;
                    if (doc >= OuterInstance.MaxDoc)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                } while (!OuterInstance.MatchDoc(doc));
                return doc;
            }

            public override int Advance(int target)
            {
                for (doc = target; doc < OuterInstance.MaxDoc; doc++)
                {
                    if (OuterInstance.MatchDoc(doc))
                    {
                        return doc;
                    }
                }
                return doc = NO_MORE_DOCS;
            }

            public override long Cost()
            {
                return OuterInstance.MaxDoc;
            }
        }

        private class FilteredDocIdSetIteratorAnonymousInnerClassHelper : FilteredDocIdSetIterator
        {
            private readonly FieldCacheDocIdSet OuterInstance;

            public FilteredDocIdSetIteratorAnonymousInnerClassHelper(FieldCacheDocIdSet outerInstance, Lucene.Net.Search.DocIdSetIterator iterator)
                : base(iterator)
            {
                this.OuterInstance = outerInstance;
            }

            protected override bool Match(int doc)
            {
                return OuterInstance.MatchDoc(doc);
            }
        }

        private class DocIdSetIteratorAnonymousInnerClassHelper2 : DocIdSetIterator
        {
            private readonly FieldCacheDocIdSet OuterInstance;

            public DocIdSetIteratorAnonymousInnerClassHelper2(FieldCacheDocIdSet outerInstance)
            {
                this.OuterInstance = outerInstance;
                doc = -1;
            }

            private int doc;

            public override int DocID()
            {
                return doc;
            }

            public override int NextDoc()
            {
                do
                {
                    doc++;
                    if (doc >= OuterInstance.MaxDoc)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                } while (!(OuterInstance.MatchDoc(doc) && OuterInstance.AcceptDocs.Get(doc)));
                return doc;
            }

            public override int Advance(int target)
            {
                for (doc = target; doc < OuterInstance.MaxDoc; doc++)
                {
                    if (OuterInstance.MatchDoc(doc) && OuterInstance.AcceptDocs.Get(doc))
                    {
                        return doc;
                    }
                }
                return doc = NO_MORE_DOCS;
            }

            public override long Cost()
            {
                return OuterInstance.MaxDoc;
            }
        }
    }
}