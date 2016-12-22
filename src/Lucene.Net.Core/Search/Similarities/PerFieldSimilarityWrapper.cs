namespace Lucene.Net.Search.Similarities
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
    using FieldInvertState = Lucene.Net.Index.FieldInvertState;

    /// <summary>
    /// Provides the ability to use a different <seealso cref="Similarity"/> for different fields.
    /// <p>
    /// Subclasses should implement <seealso cref="#get(String)"/> to return an appropriate
    /// Similarity (for example, using field-specific parameter values) for the field.
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class PerFieldSimilarityWrapper : Similarity
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        public PerFieldSimilarityWrapper()
        {
        }

        public override sealed long ComputeNorm(FieldInvertState state)
        {
            return Get(state.Name).ComputeNorm(state);
        }

        public override sealed SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
        {
            PerFieldSimWeight weight = new PerFieldSimWeight();
            weight.@delegate = Get(collectionStats.Field());
            weight.delegateWeight = weight.@delegate.ComputeWeight(queryBoost, collectionStats, termStats);
            return weight;
        }

        public override sealed SimScorer DoSimScorer(SimWeight weight, AtomicReaderContext context)
        {
            PerFieldSimWeight perFieldWeight = (PerFieldSimWeight)weight;
            return perFieldWeight.@delegate.DoSimScorer(perFieldWeight.delegateWeight, context);
        }

        /// <summary>
        /// Returns a <seealso cref="Similarity"/> for scoring a field.
        /// </summary>
        public abstract Similarity Get(string name);

        internal class PerFieldSimWeight : SimWeight
        {
            internal Similarity @delegate;
            internal SimWeight delegateWeight;

            public override float ValueForNormalization
            {
                get
                {
                    return delegateWeight.ValueForNormalization;
                }
            }

            public override void Normalize(float queryNorm, float topLevelBoost)
            {
                delegateWeight.Normalize(queryNorm, topLevelBoost);
            }
        }
    }
}