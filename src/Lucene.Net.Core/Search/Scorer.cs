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

    using DocsEnum = Lucene.Net.Index.DocsEnum;

    /// <summary>
    /// Expert: Common scoring functionality for different types of queries.
    ///
    /// <p>
    /// A <code>Scorer</code> iterates over documents matching a
    /// query in increasing order of doc Id.
    /// </p>
    /// <p>
    /// Document scores are computed using a given <code>Similarity</code>
    /// implementation.
    /// </p>
    ///
    /// <p><b>NOTE</b>: The values Float.Nan,
    /// Float.NEGATIVE_INFINITY and Float.POSITIVE_INFINITY are
    /// not valid scores.  Certain collectors (eg {@link
    /// TopScoreDocCollector}) will not properly collect hits
    /// with these scores.
    /// </summary>
    public abstract class Scorer : DocsEnum
    {
        /// <summary>
        /// the Scorer's parent Weight. in some cases this may be null </summary>
        // TODO can we clean this up?
        protected internal readonly Weight weight; // LUCENENET TODO: rename (CLS)

        /// <summary>
        /// Constructs a Scorer </summary>
        /// <param name="weight"> The scorers <code>Weight</code>. </param>
        protected Scorer(Weight weight)
        {
            this.weight = weight;
        }

        /// <summary>
        /// Returns the score of the current document matching the query.
        /// Initially invalid, until <seealso cref="#nextDoc()"/> or <seealso cref="#advance(int)"/>
        /// is called the first time, or when called from within
        /// <seealso cref="Collector#collect"/>.
        /// </summary>
        public abstract float Score(); // LUCENENET NOTE: Often makes a calculation, so not a good candidate for a property, change to GetScore() to make this more clear

        /// <summary>
        /// returns parent Weight
        /// @lucene.experimental
        /// </summary>
        public virtual Weight Weight
        {
            get
            {
                return weight;
            }
        }

        /// <summary>
        /// Returns child sub-scorers
        /// @lucene.experimental
        /// </summary>
        public virtual ICollection<ChildScorer> Children // LUCENENET TODO: Make GetChildren() (conversion) (check consistency across API)
        {
            get
            {
                return new List<ChildScorer>();
            }
        }

        /// <summary>
        /// A child Scorer and its relationship to its parent.
        /// the meaning of the relationship depends upon the parent query.
        /// @lucene.experimental
        /// </summary>
        public class ChildScorer
        {
            /// <summary>
            /// Child Scorer. (note this is typically a direct child, and may
            /// itself also have children).
            /// </summary>
            public Scorer Child { get; private set; }

            /// <summary>
            /// An arbitrary string relating this scorer to the parent.
            /// </summary>
            public string Relationship { get; private set; }

            /// <summary>
            /// Creates a new ChildScorer node with the specified relationship.
            /// <p>
            /// The relationship can be any be any string that makes sense to
            /// the parent Scorer.
            /// </summary>
            public ChildScorer(Scorer child, string relationship)
            {
                this.Child = child;
                this.Relationship = relationship;
            }
        }
    }
}