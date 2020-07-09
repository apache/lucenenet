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
    /// A <see cref="ICollector"/> implementation which wraps another
    /// <see cref="ICollector"/> and makes sure only documents with
    /// scores &gt; 0 are collected.
    /// </summary>
    public class PositiveScoresOnlyCollector : ICollector
    {
        private readonly ICollector c;
        private Scorer scorer;

        public PositiveScoresOnlyCollector(ICollector c)
        {
            this.c = c;
        }

        public virtual void Collect(int doc)
        {
            if (scorer.GetScore() > 0)
            {
                c.Collect(doc);
            }
        }

        public virtual void SetNextReader(AtomicReaderContext context)
        {
            c.SetNextReader(context);
        }
        
        public virtual void SetScorer(Scorer scorer)
        {
            // Set a ScoreCachingWrappingScorer in case the wrapped Collector will call
            // score() also.
            this.scorer = new ScoreCachingWrappingScorer(scorer);
            c.SetScorer(this.scorer);
        }

        public virtual bool AcceptsDocsOutOfOrder => c.AcceptsDocsOutOfOrder;
    }
}