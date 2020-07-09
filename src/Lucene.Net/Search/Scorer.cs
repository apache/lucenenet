using Lucene.Net.Support;
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
    /// <para>
    /// A <see cref="Scorer"/> iterates over documents matching a
    /// query in increasing order of doc Id.
    /// </para>
    /// <para>
    /// Document scores are computed using a given <see cref="Similarities.Similarity"/>
    /// implementation.
    /// </para>
    ///
    /// <para><b>NOTE</b>: The values <see cref="float.NaN"/>,
    /// <see cref="float.NegativeInfinity"/> and <see cref="float.PositiveInfinity"/> are
    /// not valid scores.  Certain collectors (eg 
    /// <see cref="TopScoreDocCollector"/>) will not properly collect hits
    /// with these scores.
    /// </para>
    /// </summary>
    public abstract class Scorer : DocsEnum
    {
        /// <summary>
        /// The <see cref="Scorer"/>'s parent <see cref="Weight"/>. In some cases this may be <c>null</c>. </summary>
        // TODO can we clean this up?
        protected internal readonly Weight m_weight;

        /// <summary>
        /// Constructs a <see cref="Scorer"/> </summary>
        /// <param name="weight"> The scorers <see cref="Weight"/>. </param>
        protected Scorer(Weight weight)
        {
            this.m_weight = weight;
        }

        /// <summary>
        /// Returns the score of the current document matching the query.
        /// Initially invalid, until <see cref="DocIdSetIterator.NextDoc()"/> or <see cref="DocIdSetIterator.Advance(int)"/>
        /// is called the first time, or when called from within
        /// <see cref="ICollector.Collect(int)"/>.
        /// </summary>
        public abstract float GetScore();

        /// <summary>
        /// returns parent <see cref="Weight"/>
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public virtual Weight Weight => m_weight;

        /// <summary>
        /// Returns child sub-scorers
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public virtual ICollection<ChildScorer> GetChildren()
        {
            return Collections.EmptyList<ChildScorer>();
        }

        /// <summary>
        /// A child <see cref="Scorer"/> and its relationship to its parent.
        /// The meaning of the relationship depends upon the parent query.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public class ChildScorer
        {
            /// <summary>
            /// Child <see cref="Scorer"/>. (note this is typically a direct child, and may
            /// itself also have children).
            /// </summary>
            public Scorer Child { get; private set; }

            /// <summary>
            /// An arbitrary string relating this scorer to the parent.
            /// </summary>
            public string Relationship { get; private set; }

            /// <summary>
            /// Creates a new <see cref="ChildScorer"/> node with the specified relationship.
            /// <para/>
            /// The relationship can be any be any string that makes sense to
            /// the parent <see cref="Scorer"/>.
            /// </summary>
            public ChildScorer(Scorer child, string relationship)
            {
                this.Child = child;
                this.Relationship = relationship;
            }
        }
    }
}