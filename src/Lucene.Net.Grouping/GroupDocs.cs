using Lucene.Net.Support;
using System.Diagnostics.CodeAnalysis;

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
    public class GroupDocs<TGroupValue> : IGroupDocs<TGroupValue>
    {
        /// <summary>
        /// The groupField value for all docs in this group; this
        /// may be null if hits did not have the groupField. 
        /// </summary>
        public TGroupValue GroupValue { get; private set; }

        /// <summary>
        /// Max score in this group
        /// </summary>
        public float MaxScore { get; private set; }

        /// <summary>
        /// Overall aggregated score of this group (currently only set by join queries). 
        /// </summary>
        public float Score { get; private set; }

        /// <summary>
        /// Hits; this may be <see cref="FieldDoc"/> instances if the
        /// withinGroupSort sorted by fields. 
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public ScoreDoc[] ScoreDocs { get; private set; }

        /// <summary>
        /// Total hits within this group
        /// </summary>
        public int TotalHits { get; private set; }

        /// <summary>
        /// Matches the groupSort passed to <see cref="AbstractFirstPassGroupingCollector{TGroupValue}"/>. 
        /// </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public object[] GroupSortValues { get; private set; }

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

    /// <summary>
    /// LUCENENET specific interface used to apply covariance to TGroupValue
    /// to simulate Java's wildcard generics.
    /// </summary>
    /// <typeparam name="TGroupValue"></typeparam>
    public interface IGroupDocs<out TGroupValue>
    {
        /// <summary>
        /// The groupField value for all docs in this group; this
        /// may be null if hits did not have the groupField. 
        /// </summary>
        TGroupValue GroupValue { get; }

        /// <summary>
        /// Max score in this group
        /// </summary>
        float MaxScore { get; }

        /// <summary>
        /// Overall aggregated score of this group (currently only set by join queries). 
        /// </summary>
        float Score { get; }

        /// <summary>
        /// Hits; this may be <see cref="FieldDoc"/> instances if the
        /// withinGroupSort sorted by fields. 
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        ScoreDoc[] ScoreDocs { get; }

        /// <summary>
        /// Total hits within this group
        /// </summary>
        int TotalHits { get; }

        /// <summary>
        /// Matches the groupSort passed to <see cref="AbstractFirstPassGroupingCollector{TGroupValue}"/>. 
        /// </summary>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Lucene's design requires some array properties")]
        object[] GroupSortValues { get; }
    }
}