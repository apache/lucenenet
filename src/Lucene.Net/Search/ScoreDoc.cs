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
    /// Holds one hit in <see cref="TopDocs"/>. </summary>
    public class ScoreDoc
    {
        /// <summary>
        /// The score of this document for the query. </summary>
        public float Score { get; set; } // LUCENENET NOTE: For some reason, this was not readonly - should it be?

        /// <summary>
        /// A hit document's number. </summary>
        /// <seealso cref="IndexSearcher.Doc(int)"/>
        public int Doc { get; set; } // LUCENENET NOTE: For some reason, this was not readonly - should it be?

        /// <summary>
        /// Only set by <see cref="TopDocs.Merge(Sort, int, int, TopDocs[])"/> </summary>
        public int ShardIndex { get; set; } // LUCENENET NOTE: For some reason, this was not readonly - should it be?

        /// <summary>
        /// Constructs a <see cref="ScoreDoc"/>. </summary>
        public ScoreDoc(int doc, float score)
            : this(doc, score, -1)
        {
        }

        /// <summary>
        /// Constructs a <see cref="ScoreDoc"/>. </summary>
        public ScoreDoc(int doc, float score, int shardIndex)
        {
            this.Doc = doc;
            this.Score = score;
            this.ShardIndex = shardIndex;
        }

        /// <summary>
        /// A convenience method for debugging.
        /// </summary>
        public override string ToString()
        {
            return "doc=" + Doc + " score=" + Score + " shardIndex=" + ShardIndex;
        }
    }
}