using Lucene.Net.Util;
using System;
#nullable enable

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

    internal sealed class HitQueue : PriorityQueue<ScoreDoc>
    {
        /// <summary>
        /// Creates a new instance with <paramref name="size"/> elements. If
        /// <paramref name="prePopulate"/> is set to <c>true</c>, the queue will pre-populate itself
        /// with sentinel objects and set its <see cref="PriorityQueue{T}.Count"/> to <paramref name="size"/>. In
        /// that case, you should not rely on <see cref="PriorityQueue{T}.Count"/> to get the number of
        /// actual elements that were added to the queue, but keep track yourself.
        /// <para/>
        /// <b>NOTE:</b> in case <paramref name="prePopulate"/> is <c>true</c>, you should pop
        /// elements from the queue using the following code example:
        ///
        /// <code>
        /// PriorityQueue&lt;ScoreDoc&gt; pq = new HitQueue(10, true); // pre-populate.
        /// ScoreDoc top = pq.Top;
        ///
        /// // Add/Update one element.
        /// top.Score = 1.0f;
        /// top.Soc = 0;
        /// top = (ScoreDoc) pq.UpdateTop();
        /// int totalHits = 1;
        ///
        /// // Now pop only the elements that were *truly* inserted.
        /// // First, pop all the sentinel elements (there are pq.Count - totalHits).
        /// for (int i = pq.Count - totalHits; i &gt; 0; i--) pq.Pop();
        ///
        /// // Now pop the truly added elements.
        /// ScoreDoc[] results = new ScoreDoc[totalHits];
        /// for (int i = totalHits - 1; i &gt;= 0; i--) 
        /// {
        ///     results[i] = (ScoreDoc)pq.Pop();
        /// }
        /// </code>
        ///
        /// <para/><b>NOTE</b>: this overload will pre-allocate a full array of
        /// length <paramref name="size"/>.
        /// </summary>
        /// <param name="size">
        ///          The requested size of this queue. </param>
        /// <param name="prePopulate">
        ///          Specifies whether to pre-populate the queue with sentinel values. </param>
        /// <seealso cref="SentinelFactory"/>
        internal HitQueue(int size, bool prePopulate)
            : base(size, prePopulate ? SentinelFactory.Default : null)
        {
        }

        // LUCENENET specific - Rather than having a GetSentinelObject() method on PriorityQueue<T>,
        // and a "prePopulate" boolean value, population is controlled by whether ISentinelFactory<T>
        // has an instance or is null. This is the singleton instance that is injected when "prePopulate"
        // is true.
        internal sealed class SentinelFactory : ISentinelFactory<ScoreDoc>
        {
            public static SentinelFactory Default { get; } = new SentinelFactory();

            public ScoreDoc Create()
            {
                // Always set the doc Id to MAX_VALUE so that it won't be favored by
                // lessThan. this generally should not happen since if score is not NEG_INF,
                // TopScoreDocCollector will always add the object to the queue.
                return new ScoreDoc(int.MaxValue, float.NegativeInfinity);
            }
        }

        protected internal override sealed bool LessThan(ScoreDoc hitA, ScoreDoc hitB)
        {
            // LUCENENET: Added guard clauses
            if (hitA is null)
                throw new ArgumentNullException(nameof(hitA));
            if (hitB is null)
                throw new ArgumentNullException(nameof(hitB));

            // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
            if (NumericUtils.SingleToSortableInt32(hitA.Score) == NumericUtils.SingleToSortableInt32(hitB.Score))
            {
                return hitA.Doc > hitB.Doc;
            }
            else
            {
                return NumericUtils.SingleToSortableInt32(hitA.Score) < NumericUtils.SingleToSortableInt32(hitB.Score);
            }
        }
    }
}