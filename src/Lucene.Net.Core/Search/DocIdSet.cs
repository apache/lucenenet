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
    /// A DocIdSet contains a set of doc ids. Implementing classes must
    /// only implement <seealso cref="#iterator"/> to provide access to the set.
    /// </summary>
    public abstract class DocIdSet
    {
        /// <summary>
        /// Provides a <seealso cref="DocIdSetIterator"/> to access the set.
        /// this implementation can return <code>null</code> if there
        /// are no docs that match.
        /// </summary>
        public abstract DocIdSetIterator GetIterator(); // LUCENENET TODO: Consistency GetIterator() vs Iterator()

        // TODO: somehow this class should express the cost of
        // iteration vs the cost of random access Bits; for
        // expensive Filters (e.g. distance < 1 km) we should use
        // bits() after all other Query/Filters have matched, but
        // this is the opposite of what bits() is for now
        // (down-low filtering using e.g. FixedBitSet)

        /// <summary>
        /// Optionally provides a <seealso cref="GetBits"/> interface for random access
        /// to matching documents. </summary>
        /// <returns> {@code null}, if this {@code DocIdSet} does not support random access.
        /// In contrast to <seealso cref="#iterator()"/>, a return value of {@code null}
        /// <b>does not</b> imply that no documents match the filter!
        /// The default implementation does not provide random access, so you
        /// only need to implement this method if your DocIdSet can
        /// guarantee random access to every docid in O(1) time without
        /// external disk access (as <seealso cref="GetBits"/> interface cannot throw
        /// <seealso cref="IOException"/>). this is generally true for bit sets
        /// like <seealso cref="Lucene.Net.Util.FixedBitSet"/>, which return
        /// itself if they are used as {@code DocIdSet}. </returns>
        public virtual Bits GetBits()
        {
            return null;
        }

        /// <summary>
        /// this method is a hint for <seealso cref="CachingWrapperFilter"/>, if this <code>DocIdSet</code>
        /// should be cached without copying it. The default is to return
        /// <code>false</code>. If you have an own <code>DocIdSet</code> implementation
        /// that does its iteration very effective and fast without doing disk I/O,
        /// override this method and return <code>true</code>.
        /// </summary>
        public virtual bool Cacheable // LUCENENET TODO: Rename IsCacheable
        {
            get
            {
                return false;
            }
        }
    }
}