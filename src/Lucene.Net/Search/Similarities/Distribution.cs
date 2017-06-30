using System;

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
    /// The probabilistic distribution used to model term occurrence
    /// in information-based models. 
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="IBSimilarity"/>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class Distribution
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        public Distribution()
        {
        }

        /// <summary>
        /// Computes the score. </summary>
        public abstract float Score(BasicStats stats, float tfn, float lambda);

        /// <summary>
        /// Explains the score. Returns the name of the model only, since
        /// both <c>tfn</c> and <c>lambda</c> are explained elsewhere.
        /// </summary>
        public virtual Explanation Explain(BasicStats stats, float tfn, float lambda)
        {
            return new Explanation(Score(stats, tfn, lambda), this.GetType().Name);
        }

        /// <summary>
        /// Subclasses must override this method to return the name of the
        /// distribution.
        /// </summary>
        public override abstract string ToString();
    }
}