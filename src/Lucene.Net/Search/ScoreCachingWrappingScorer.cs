using System.Collections.Generic;

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
    /// A <see cref="Scorer"/> which wraps another scorer and caches the score of the
    /// current document. Successive calls to <see cref="GetScore()"/> will return the same
    /// result and will not invoke the wrapped Scorer's GetScore() method, unless the
    /// current document has changed.
    /// <para/>
    /// This class might be useful due to the changes done to the <see cref="ICollector"/>
    /// interface, in which the score is not computed for a document by default, only
    /// if the collector requests it. Some collectors may need to use the score in
    /// several places, however all they have in hand is a <see cref="Scorer"/> object, and
    /// might end up computing the score of a document more than once.
    /// </summary>
    public class ScoreCachingWrappingScorer : Scorer
    {
        private readonly Scorer scorer;
        private int curDoc = -1;
        private float curScore;

        /// <summary>
        /// Creates a new instance by wrapping the given scorer. </summary>
        public ScoreCachingWrappingScorer(Scorer scorer)
            : base(scorer.m_weight)
        {
            this.scorer = scorer;
        }

        public override float GetScore()
        {
            int doc = scorer.DocID;
            if (doc != curDoc)
            {
                curScore = scorer.GetScore();
                curDoc = doc;
            }

            return curScore;
        }

        public override int Freq => scorer.Freq;

        public override int DocID => scorer.DocID;

        public override int NextDoc()
        {
            return scorer.NextDoc();
        }

        public override int Advance(int target)
        {
            return scorer.Advance(target);
        }

        public override ICollection<ChildScorer> GetChildren()
        {
            return new[] { new ChildScorer(scorer, "CACHED") };
        }

        public override long GetCost()
        {
            return scorer.GetCost();
        }
    }
}