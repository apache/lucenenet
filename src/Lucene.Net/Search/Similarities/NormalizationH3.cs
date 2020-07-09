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
    /// Dirichlet Priors normalization
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class NormalizationH3 : Normalization
    {
        private readonly float mu;

        /// <summary>
        /// Calls <see cref="T:NormalizationH3(800)"/>
        /// </summary>
        public NormalizationH3()
            : this(800F)
        {
        }

        /// <summary>
        /// Creates <see cref="NormalizationH3"/> with the supplied parameter <c>&#956;</c>. </summary>
        /// <param name="mu"> smoothing parameter <c>&#956;</c> </param>
        public NormalizationH3(float mu)
        {
            this.mu = mu;
        }

        public override float Tfn(BasicStats stats, float tf, float len)
        {
            return (tf + mu * ((stats.TotalTermFreq + 1F) / (stats.NumberOfFieldTokens + 1F))) / (len + mu) * mu;
        }

        public override string ToString()
        {
            return "3(" + mu + ")";
        }

        /// <summary>
        /// Returns the parameter <c>&#956;</c> </summary>
        /// <seealso cref="NormalizationH3(float)"/>
        public virtual float Mu => mu;
    }
}