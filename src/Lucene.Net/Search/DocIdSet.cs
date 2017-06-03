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

    /// <summary>
    /// A <see cref="DocIdSet"/> contains a set of doc ids. Implementing classes must
    /// only implement <see cref="GetIterator()"/> to provide access to the set.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class DocIdSet
    {
        /// <summary>
        /// Provides a <see cref="DocIdSetIterator"/> to access the set.
        /// This implementation can return <c>null</c> if there
        /// are no docs that match.
        /// </summary>
        public abstract DocIdSetIterator GetIterator();

        // TODO: somehow this class should express the cost of
        // iteration vs the cost of random access Bits; for
        // expensive Filters (e.g. distance < 1 km) we should use
        // bits() after all other Query/Filters have matched, but
        // this is the opposite of what bits() is for now
        // (down-low filtering using e.g. FixedBitSet)

        /// <summary>
        /// Optionally provides a <see cref="IBits"/> interface for random access
        /// to matching documents. </summary>
        /// <returns> <c>null</c>, if this <see cref="DocIdSet"/> does not support random access.
        /// In contrast to <see cref="GetIterator()"/>, a return value of <c>null</c>
        /// <b>does not</b> imply that no documents match the filter!
        /// The default implementation does not provide random access, so you
        /// only need to implement this method if your <see cref="DocIdSet"/> can
        /// guarantee random access to every docid in O(1) time without
        /// external disk access (as <see cref="IBits"/> interface cannot throw
        /// <see cref="System.IO.IOException"/>). This is generally true for bit sets
        /// like <see cref="Lucene.Net.Util.FixedBitSet"/>, which return
        /// itself if they are used as <see cref="DocIdSet"/>. </returns>
        public virtual IBits Bits // LUCENENET NOTE: This isn't a great candidate for a property, but it makes more sense to call this Bits than Bits(). GetBits() was already taken in the same context.
        {
            get { return null; }
        }

        /// <summary>
        /// This method is a hint for <see cref="CachingWrapperFilter"/>, if this <see cref="DocIdSet"/>
        /// should be cached without copying it. The default is to return
        /// <c>false</c>. If you have an own <see cref="DocIdSet"/> implementation
        /// that does its iteration very effective and fast without doing disk I/O,
        /// override this property and return <c>true</c>.
        /// </summary>
        public virtual bool IsCacheable
        {
            get
            {
                return false;
            }
        }
    }
}