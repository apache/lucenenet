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

    /// <summary>
    /// This class acts as the base class for the implementations of the <em>first
    /// normalization of the informative content</em> in the DFR framework. This
    /// component is also called the <em>after effect</em> and is defined by the
    /// formula <em>Inf<sub>2</sub> = 1 - Prob<sub>2</sub></em>, where
    /// <em>Prob<sub>2</sub></em> measures the <em>information gain</em>.
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    /// <seealso cref="DFRSimilarity"/>
    public abstract class AfterEffect
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected AfterEffect() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
        }

        /// <summary>
        /// Returns the aftereffect score. </summary>
        public abstract float Score(BasicStats stats, float tfn);

        /// <summary>
        /// Returns an explanation for the score. </summary>
        public abstract Explanation Explain(BasicStats stats, float tfn);

        /// <summary>
        /// Implementation used when there is no aftereffect. </summary>
        public sealed class NoAfterEffect : AfterEffect
        {
            /// <summary>
            /// Sole constructor: parameter-free </summary>
            public NoAfterEffect()
            {
            }

            public override float Score(BasicStats stats, float tfn)
            {
                return 1f;
            }

            public override Explanation Explain(BasicStats stats, float tfn)
            {
                return new Explanation(1, "no aftereffect");
            }

            public override string ToString()
            {
                return "";
            }
        }

        /// <summary>
        /// Subclasses must override this method to return the code of the
        /// after effect formula. Refer to the original paper for the list.
        /// </summary>
        public override abstract string ToString();
    }
}