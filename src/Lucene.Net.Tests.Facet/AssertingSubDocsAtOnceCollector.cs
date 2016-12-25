using System.Collections.Generic;

namespace Lucene.Net.Facet
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
    using Collector = Lucene.Net.Search.Collector;
    using ChildScorer = Lucene.Net.Search.Scorer.ChildScorer;
    using Scorer = Lucene.Net.Search.Scorer;
    using System;
    /// <summary>
    /// Verifies in collect() that all child subScorers are on
    ///  the collected doc. 
    /// </summary>
    internal class AssertingSubDocsAtOnceCollector : Collector
    {

        // TODO: allow wrapping another Collector

        internal IList<Scorer> allScorers;

        public override void SetScorer(Scorer scorer)
        {
            // Gathers all scorers, including value and "under":
            allScorers = new List<Scorer>();
            allScorers.Add(scorer);
            int upto = 0;
            while (upto < allScorers.Count)
            {
                scorer = allScorers[upto++];
                foreach (ChildScorer sub in scorer.Children)
                {
                    allScorers.Add(sub.Child);
                }
            }
        }

        public override void Collect(int docID)
        {
            foreach (Scorer s in allScorers)
            {
                if (docID != s.DocID)
                {
                    throw new InvalidOperationException("subScorer=" + s + " has docID=" + s.DocID + " != collected docID=" + docID);
                }
            }
        }

        public override void SetNextReader(AtomicReaderContext context)
        {
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get { return false; }
        }

    }

}