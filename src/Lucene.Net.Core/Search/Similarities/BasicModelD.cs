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
    /// Implements the approximation of the binomial model with the divergence
    /// for DFR. The formula used in Lucene differs slightly from the one in the
    /// original paper: to avoid underflow for small values of {@code N} and
    /// {@code F}, {@code N} is increased by {@code 1} and
    /// {@code F} is always increased by {@code tfn+1}.
    /// <p>
    /// WARNING: for terms that do not meet the expected random distribution
    /// (e.g. stopwords), this model may give poor performance, such as
    /// abnormally high scores for low tf values.
    /// @lucene.experimental
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class BasicModelD : BasicModel
    {
        /// <summary>
        /// Sole constructor: parameter-free </summary>
        public BasicModelD()
        {
        }

        public override sealed float Score(BasicStats stats, float tfn)
        {
            // we have to ensure phi is always < 1 for tiny TTF values, otherwise nphi can go negative,
            // resulting in NaN. cleanest way is to unconditionally always add tfn to totalTermFreq
            // to create a 'normalized' F.
            double F = stats.TotalTermFreq + 1 + tfn;
            double phi = (double)tfn / F;
            double nphi = 1 - phi;
            double p = 1.0 / (stats.NumberOfDocuments + 1);
            double D = phi * SimilarityBase.Log2(phi / p) + nphi * SimilarityBase.Log2(nphi / (1 - p));
            return (float)(D * F + 0.5 * SimilarityBase.Log2(1 + 2 * Math.PI * tfn * nphi));
        }

        public override string ToString()
        {
            return "D";
        }
    }
}