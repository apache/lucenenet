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
using PriorityQueue = Lucene.Net.Util.PriorityQueue;

namespace Lucene.Net.Search
{
	
    /// <summary>A {@link HitCollector} implementation that collects the top-scoring
    /// documents, returning them as a {@link TopDocs}.  This is used by {@link
    /// IndexSearcher} to implement {@link TopDocs}-based search.
    /// 
    /// <p>This may be extended, overriding the collect method to, e.g.,
    /// conditionally invoke <code>super()</code> in order to filter which
    /// documents are collected.
    /// 
    /// </summary>
    public class TopDocCollector : HitCollector
    {
        private int numHits;
        private float minScore = 0.0f;
		
        internal int totalHits;
        internal PriorityQueue hq;
		
        /// <summary>Construct to collect a given number of hits.</summary>
        /// <param name="numHits">the maximum number of hits to collect
        /// </param>
        public TopDocCollector(int numHits) : this(numHits, new HitQueue(numHits))
        {
        }
		
        internal TopDocCollector(int numHits, PriorityQueue hq)
        {
            this.numHits = numHits;
            this.hq = hq;
        }
		
        // javadoc inherited
        public override void  Collect(int doc, float score)
        {
            if (score > 0.0f)
            {
                totalHits++;
                if (hq.Size() < numHits || score >= minScore)
                {
                    hq.Insert(new ScoreDoc(doc, score));
                    minScore = ((ScoreDoc) hq.Top()).score; // maintain minScore
                }
            }
        }
		
        /// <summary>The total number of documents that matched this query. </summary>
        public virtual int GetTotalHits()
        {
            return totalHits;
        }
		
        /// <summary>The top-scoring hits. </summary>
        public virtual TopDocs TopDocs()
        {
            ScoreDoc[] scoreDocs = new ScoreDoc[hq.Size()];
            for (int i = hq.Size() - 1; i >= 0; i--)
                // put docs in array
                scoreDocs[i] = (ScoreDoc) hq.Pop();
			
            float maxScore = (totalHits == 0) ? System.Single.NegativeInfinity : scoreDocs[0].score;
			
            return new TopDocs(totalHits, scoreDocs, maxScore);
        }
    }
}