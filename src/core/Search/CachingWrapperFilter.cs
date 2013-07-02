/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Support;
using IndexReader = Lucene.Net.Index.IndexReader;
using OpenBitSetDISI = Lucene.Net.Util.OpenBitSetDISI;
using Lucene.Net.Util;
using Lucene.Net.Index;

namespace Lucene.Net.Search
{

    /// <summary> Wraps another filter's result and caches it.  The purpose is to allow
    /// filters to simply filter, and then wrap with this class to add caching.
    /// </summary>
    [Serializable]
    public class CachingWrapperFilter : Filter
    {
        // TODO: make this filter aware of ReaderContext. a cached filter could 
        // specify the actual readers key or something similar to indicate on which
        // level of the readers hierarchy it should be cached.
        private readonly Filter filter;
        private readonly IDictionary<object, DocIdSet> cache = new ConcurrentHashMapWrapper<object, DocIdSet>(new WeakDictionary<object, DocIdSet>());

        public CachingWrapperFilter(Filter filter)
        {
            this.filter = filter;
        }

        /// <summary>Provide the DocIdSet to be cached, using the DocIdSet provided
        /// by the wrapped Filter.
        /// This implementation returns the given DocIdSet.
        /// </summary>
        protected internal virtual DocIdSet DocIdSetToCache(DocIdSet docIdSet, AtomicReader reader)
        {
            if (docIdSet == null)
            {
                // this is better than returning null, as the nonnull result can be cached
                return DocIdSet.EMPTY_DOCIDSET;
            }
            else if (docIdSet.IsCacheable)
            {
                return docIdSet;
            }
            else
            {
                DocIdSetIterator it = docIdSet.Iterator();
                // null is allowed to be returned by iterator(),
                // in this case we wrap with the empty set,
                // which is cacheable.
                if (it == null)
                {
                    return DocIdSet.EMPTY_DOCIDSET;
                }
                else
                {
                    FixedBitSet bits = new FixedBitSet(reader.MaxDoc);
                    bits.Or(it);
                    return bits;
                }
            }
        }

        // for testing
        public int hitCount, missCount;

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            AtomicReader reader = context.Reader;
            Object key = reader.CoreCacheKey;

            DocIdSet docIdSet = cache[key];
            if (docIdSet != null)
            {
                hitCount++;
            }
            else
            {
                missCount++;
                docIdSet = DocIdSetToCache(filter.GetDocIdSet(context, null), reader);
                cache[key] = docIdSet;
            }

            return BitsFilteredDocIdSet.Wrap(docIdSet, acceptDocs);
        }

        public override string ToString()
        {
            return "CachingWrapperFilter(" + filter + ")";
        }

        public override bool Equals(object o)
        {
            if (!(o is CachingWrapperFilter))
                return false;
            return this.filter.Equals(((CachingWrapperFilter)o).filter);
        }

        public override int GetHashCode()
        {
            return filter.GetHashCode() ^ 0x1117BF25;
        }
    }
}