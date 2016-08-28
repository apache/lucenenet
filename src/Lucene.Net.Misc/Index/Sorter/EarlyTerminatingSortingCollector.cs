using Lucene.Net.Search;
using System;

namespace Lucene.Net.Index.Sorter
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
    /// A <seealso cref="Collector"/> that early terminates collection of documents on a
    /// per-segment basis, if the segment was sorted according to the given
    /// <seealso cref="Sort"/>.
    /// 
    /// <para>
    /// <b>NOTE:</b> the {@code Collector} detects sorted segments according to
    /// <seealso cref="SortingMergePolicy"/>, so it's best used in conjunction with it. Also,
    /// it collects up to a specified {@code numDocsToCollect} from each segment, 
    /// and therefore is mostly suitable for use in conjunction with collectors such as
    /// <seealso cref="TopDocsCollector"/>, and not e.g. <seealso cref="TotalHitCountCollector"/>.
    /// </para>
    /// <para>
    /// <b>NOTE</b>: If you wrap a {@code TopDocsCollector} that sorts in the same
    /// order as the index order, the returned <seealso cref="TopDocsCollector#topDocs() TopDocs"/>
    /// will be correct. However the total of {@link TopDocsCollector#getTotalHits()
    /// hit count} will be underestimated since not all matching documents will have
    /// been collected.
    /// </para>
    /// <para>
    /// <b>NOTE</b>: This {@code Collector} uses <seealso cref="Sort#toString()"/> to detect
    /// whether a segment was sorted with the same {@code Sort}. This has
    /// two implications:
    /// <ul>
    /// <li>if a custom comparator is not implemented correctly and returns
    /// different identifiers for equivalent instances, this collector will not
    /// detect sorted segments,</li>
    /// <li>if you suddenly change the <seealso cref="IndexWriter"/>'s
    /// {@code SortingMergePolicy} to sort according to another criterion and if both
    /// the old and the new {@code Sort}s have the same identifier, this
    /// {@code Collector} will incorrectly detect sorted segments.</li>
    /// </ul>
    /// 
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class EarlyTerminatingSortingCollector : Collector
    {
        /// <summary>
        /// The wrapped Collector </summary>
        protected internal readonly Collector @in;
        /// <summary>
        /// Sort used to sort the search results </summary>
        protected internal readonly Sort sort;
        /// <summary>
        /// Number of documents to collect in each segment </summary>
        protected internal readonly int numDocsToCollect;
        /// <summary>
        /// Number of documents to collect in the current segment being processed </summary>
        protected internal int segmentTotalCollect;
        /// <summary>
        /// True if the current segment being processed is sorted by <seealso cref="#sort"/> </summary>
        protected internal bool segmentSorted;

        private int numCollected;

        /// <summary>
        /// Create a new <seealso cref="EarlyTerminatingSortingCollector"/> instance.
        /// </summary>
        /// <param name="in">
        ///          the collector to wrap </param>
        /// <param name="sort">
        ///          the sort you are sorting the search results on </param>
        /// <param name="numDocsToCollect">
        ///          the number of documents to collect on each segment. When wrapping
        ///          a <seealso cref="TopDocsCollector"/>, this number should be the number of
        ///          hits. </param>
        public EarlyTerminatingSortingCollector(Collector @in, Sort sort, int numDocsToCollect)
        {
            if (numDocsToCollect <= 0)
            {
                throw new InvalidOperationException("numDocsToCollect must always be > 0, got " + segmentTotalCollect);
            }
            this.@in = @in;
            this.sort = sort;
            this.numDocsToCollect = numDocsToCollect;
        }

        public override Scorer Scorer
        {
            set
            {
                @in.Scorer = value;
            }
        }

        public override void Collect(int doc)
        {
            @in.Collect(doc);
            if (++numCollected >= segmentTotalCollect)
            {
                throw new CollectionTerminatedException();
            }
        }
        // LUCENENET TODO: This should be a method because it changes the state of the object
        public override AtomicReaderContext NextReader
        {
            set
            {
                @in.NextReader = value;
                segmentSorted = SortingMergePolicy.IsSorted(value.AtomicReader, sort);
                segmentTotalCollect = segmentSorted ? numDocsToCollect : int.MaxValue;
                numCollected = 0;
            }
        }

        public override bool AcceptsDocsOutOfOrder()
        {
            return !segmentSorted && @in.AcceptsDocsOutOfOrder();
        }
    }
}