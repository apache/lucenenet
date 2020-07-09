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
    /// Normalization model that assumes a uniform distribution of the term frequency.
    /// <para>While this model is parameterless in the
    /// <a href="http://citeseer.ist.psu.edu/viewdoc/summary?doi=10.1.1.101.742">
    /// original article</a>, <a href="http://dl.acm.org/citation.cfm?id=1835490">
    /// information-based models</a> (see <see cref="IBSimilarity"/>) introduced a
    /// multiplying factor.
    /// The default value for the <c>c</c> parameter is <c>1</c>.</para>
    /// @lucene.experimental
    /// </summary>
    public class NormalizationH1 : Normalization
    {
        private readonly float c;

        /// <summary>
        /// Creates <see cref="NormalizationH1"/> with the supplied parameter <paramref name="c"/>. </summary>
        /// <param name="c"> Hyper-parameter that controls the term frequency
        /// normalization with respect to the document length. </param>
        public NormalizationH1(float c)
        {
            this.c = c;
        }

        /// <summary>
        /// Calls <see cref="T:NormalizationH1(1)"/>
        /// </summary>
        public NormalizationH1()
            : this(1)
        {
        }

        public override sealed float Tfn(BasicStats stats, float tf, float len)
        {
            return tf * stats.AvgFieldLength / len;
        }

        public override string ToString()
        {
            return "1";
        }

        /// <summary>
        /// Returns the <c>c</c> parameter. </summary>
        /// <seealso cref="NormalizationH1(float)"/>
        public virtual float C => c;
    }
}