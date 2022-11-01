using System;
using static Lucene.Net.Search.Similarities.SimilarityBase;

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
    /// Implements the Poisson approximation for the binomial model for DFR.
    /// <para/>
    /// @lucene.experimental
    /// <para/>
    /// WARNING: for terms that do not meet the expected random distribution
    /// (e.g. stopwords), this model may give poor performance, such as
    /// abnormally high scores for low tf values.
    /// </summary>
    public class BasicModelP : BasicModel
    {
        /// <summary>
        /// <c>log2(Math.E)</c>, precomputed. </summary>
        protected internal static double LOG2_E = Log2(Math.E);

        /// <summary>
        /// Sole constructor: parameter-free </summary>
        public BasicModelP()
        {
        }

        public override sealed float Score(BasicStats stats, float tfn)
        {
            float lambda = (float)(stats.TotalTermFreq + 1) / (stats.NumberOfDocuments + 1);
            return (float)(tfn * Log2(tfn / lambda) + (lambda + 1 / (12 * tfn) - tfn) * LOG2_E + 0.5 * Log2(2 * Math.PI * tfn));
        }

        public override string ToString()
        {
            return "P";
        }
    }
}