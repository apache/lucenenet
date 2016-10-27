using Lucene.Net.Search;

namespace Lucene.Net.Search.Grouping
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
    /// Represents one group in the results.
    /// 
    /// @lucene.experimental 
    /// </summary>
    public class GroupDocs<TGroupValue>
    {
        /// <summary>
        /// The groupField value for all docs in this group; this
        /// may be null if hits did not have the groupField. 
        /// </summary>
        public readonly TGroupValue GroupValue;

        /// <summary>
        /// Max score in this group
        /// </summary>
        public readonly float MaxScore;

        /// <summary>
        /// Overall aggregated score of this group (currently only set by join queries). 
        /// </summary>
        public readonly float Score;

        /// <summary>
        /// Hits; this may be {@link org.apache.lucene.search.FieldDoc} instances if the
        /// withinGroupSort sorted by fields. 
        /// </summary>
        public readonly ScoreDoc[] ScoreDocs;

        /// <summary>
        /// Total hits within this group
        /// </summary>
        public readonly int TotalHits;

        /// <summary>
        /// Matches the groupSort passed to {@link AbstractFirstPassGroupingCollector}. 
        /// </summary>
        public readonly object[] GroupSortValues;

        public GroupDocs(float score, float maxScore, int totalHits, ScoreDoc[] scoreDocs, TGroupValue groupValue, object[] groupSortValues)
        {
            Score = score;
            MaxScore = maxScore;
            TotalHits = totalHits;
            ScoreDocs = scoreDocs;
            GroupValue = groupValue;
            GroupSortValues = groupSortValues;
        }
    }
}