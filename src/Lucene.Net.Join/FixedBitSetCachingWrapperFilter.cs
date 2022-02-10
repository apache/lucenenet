// Lucene version compatibility level 4.8.1
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Join
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

    /// <summary>
    /// A <see cref="CachingWrapperFilter"/> that caches sets using a <see cref="FixedBitSet"/>,
    /// as required for joins. 
    /// </summary>
    public sealed class FixedBitSetCachingWrapperFilter : CachingWrapperFilter
    {
        /// <summary>
        /// Sole constructor, see <see cref="CachingWrapperFilter.CachingWrapperFilter(Filter)"/>.
        /// </summary>
        public FixedBitSetCachingWrapperFilter(Filter filter) 
            : base(filter)
        {
        }

        protected override DocIdSet DocIdSetToCache(DocIdSet docIdSet, AtomicReader reader)
        {
            if (docIdSet is null)
            {
                return EMPTY_DOCIDSET;
            }

            if (docIdSet is FixedBitSet)
            {
                // this is different from CachingWrapperFilter: even when the DocIdSet is
                // cacheable, we convert it to a FixedBitSet since we require all the
                // cached filters to be FixedBitSets
                return docIdSet;
            }

            DocIdSetIterator it = docIdSet.GetIterator();
            if (it is null)
            {
                return EMPTY_DOCIDSET;
            }
            FixedBitSet copy = new FixedBitSet(reader.MaxDoc);
            copy.Or(it);
            return copy;
        }
    }
}