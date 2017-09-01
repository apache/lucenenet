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

    /// <summary>
    /// This class is used to score a range of documents at
    /// once, and is returned by <see cref="Weight.GetBulkScorer(Index.AtomicReaderContext, bool, Util.IBits)"/>.  Only
    /// queries that have a more optimized means of scoring
    /// across a range of documents need to override this.
    /// Otherwise, a default implementation is wrapped around
    /// the <see cref="Scorer"/> returned by <see cref="Weight.GetScorer(Index.AtomicReaderContext, Util.IBits)"/>.
    /// </summary>

    public abstract class BulkScorer
    {
        /// <summary>
        /// Scores and collects all matching documents. </summary>
        /// <param name="collector"> The collector to which all matching documents are passed. </param>
        public virtual void Score(ICollector collector)
        {
            Score(collector, int.MaxValue);
        }

        /// <summary>
        /// Collects matching documents in a range.
        /// </summary>
        /// <param name="collector"> The collector to which all matching documents are passed. </param>
        /// <param name="max"> Score up to, but not including, this doc </param>
        /// <returns> <c>true</c> if more matching documents may remain. </returns>
        public abstract bool Score(ICollector collector, int max);
    }
}