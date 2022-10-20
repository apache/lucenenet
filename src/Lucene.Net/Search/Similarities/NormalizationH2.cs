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
    /// Normalization model in which the term frequency is inversely related to the
    /// length.
    /// <para>While this model is parameterless in the
    /// <a href="http://citeseer.ist.psu.edu/viewdoc/summary?doi=10.1.1.101.742">
    /// original article</a>, the <a href="http://theses.gla.ac.uk/1570/">thesis</a>
    /// introduces the parameterized variant.
    /// The default value for the <c>c</c> parameter is <c>1</c>.</para>
    /// @lucene.experimental
    /// </summary>
    public class NormalizationH2 : Normalization
    {
        private readonly float c;

        /// <summary>
        /// Creates <see cref="NormalizationH2"/> with the supplied parameter <paramref name="c"/>. </summary>
        /// <param name="c"> Hyper-parameter that controls the term frequency
        /// normalization with respect to the document length. </param>
        public NormalizationH2(float c)
        {
            this.c = c;
        }

        /// <summary>
        /// Calls <see cref="T:NormalizationH2(1)"/>
        /// </summary>
        public NormalizationH2()
            : this(1)
        {
        }

        public override sealed float Tfn(BasicStats stats, float tf, float len)
        {
            return (float)(tf * Log2(1 + c * stats.AvgFieldLength / len));
        }

        public override string ToString()
        {
            return "2";
        }

        /// <summary>
        /// Returns the <c>c</c> parameter. </summary>
        /// <seealso cref="NormalizationH2(float)"/>
        public virtual float C => c;
    }
}