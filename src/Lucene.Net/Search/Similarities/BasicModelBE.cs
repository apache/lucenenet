using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
    /// Limiting form of the Bose-Einstein model. The formula used in Lucene differs
    /// slightly from the one in the original paper: <c>F</c> is increased by <c>tfn+1</c>
    /// and <c>N</c> is increased by <c>F</c>
    /// <para/>
    /// @lucene.experimental
    /// <para/>
    /// NOTE: in some corner cases this model may give poor performance with Normalizations that
    /// return large values for <c>tfn</c> such as <see cref="NormalizationH3"/>. Consider using the
    /// geometric approximation (<see cref="BasicModelG"/>) instead, which provides the same relevance
    /// but with less practical problems.
    /// </summary>
    public class BasicModelBE : BasicModel
    {
        /// <summary>
        /// Sole constructor: parameter-free </summary>
        public BasicModelBE()
        {
        }

        public override sealed float Score(BasicStats stats, float tfn)
        {
            double F = stats.TotalTermFreq + 1 + tfn;
            // approximation only holds true when F << N, so we use N += F
            double N = F + stats.NumberOfDocuments;
            return (float)(-Log2((N - 1) * Math.E) + this.F(N + F - 1, N + F - tfn - 2) - this.F(F, F - tfn));
        }

        /// <summary>
        /// The <em>f</em> helper function defined for <em>B<sub>E</sub></em>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        private double F(double n, double m)
        {
            return (m + 0.5) * Log2(n / m) + (n - m) * Log2(n);
        }

        public override string ToString()
        {
            return "Be";
        }
    }
}