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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;

    /// <summary>
    /// A <seealso cref="Collector"/> implementation which wraps another
    /// <seealso cref="Collector"/> and makes sure only documents with
    /// scores &gt; 0 are collected.
    /// </summary>
    public class PositiveScoresOnlyCollector : Collector
    {
        private readonly Collector c;
        private Scorer Scorer_Renamed; // LUCENENET TODO: Rename (private)

        public PositiveScoresOnlyCollector(Collector c)
        {
            this.c = c;
        }

        public override void Collect(int doc)
        {
            if (Scorer_Renamed.Score() > 0)
            {
                c.Collect(doc);
            }
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                c.NextReader = value;
            }
        }
        
        public override void SetScorer(Scorer scorer)
        {
            // Set a ScoreCachingWrappingScorer in case the wrapped Collector will call
            // score() also.
            this.Scorer_Renamed = new ScoreCachingWrappingScorer(scorer);
            c.SetScorer(this.Scorer_Renamed);
        }

        public override bool AcceptsDocsOutOfOrder()
        {
            return c.AcceptsDocsOutOfOrder();
        }
    }
}