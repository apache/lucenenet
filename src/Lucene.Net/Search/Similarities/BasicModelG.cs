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
    /// Geometric as limiting form of the Bose-Einstein model.  The formula used in Lucene differs
    /// slightly from the one in the original paper: <c>F</c> is increased by <c>1</c>
    /// and <c>N</c> is increased by <c>F</c>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class BasicModelG : BasicModel
    {
        /// <summary>
        /// Sole constructor: parameter-free </summary>
        public BasicModelG()
        {
        }

        public override sealed float Score(BasicStats stats, float tfn)
        {
            // just like in BE, approximation only holds true when F << N, so we use lambda = F / (N + F)
            double F = stats.TotalTermFreq + 1;
            double N = stats.NumberOfDocuments;
            double lambda = F / (N + F);
            // -log(1 / (lambda + 1)) -> log(lambda + 1)
            return (float)(Log2(lambda + 1) + tfn * Log2((1 + lambda) / lambda));
        }

        public override string ToString()
        {
            return "G";
        }
    }
}