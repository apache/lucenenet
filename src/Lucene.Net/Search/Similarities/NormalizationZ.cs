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
    /// Pareto-Zipf Normalization
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class NormalizationZ : Normalization
    {
        internal readonly float z;

        /// <summary>
        /// Calls <see cref="T:NormalizationZ(0.3)"/>
        /// </summary>
        public NormalizationZ()
            : this(0.30F)
        {
        }

        /// <summary>
        /// Creates <see cref="NormalizationZ"/> with the supplied parameter <paramref name="z"/>. </summary>
        /// <param name="z"> represents <c>A/(A+1)</c> where <c>A</c>
        ///          measures the specificity of the language. </param>
        public NormalizationZ(float z)
        {
            this.z = z;
        }

        public override float Tfn(BasicStats stats, float tf, float len)
        {
            return (float)(tf * Math.Pow(stats.m_avgFieldLength / len, z));
        }

        public override string ToString()
        {
            return "Z(" + z + ")";
        }

        /// <summary>
        /// Returns the parameter <c>z</c> </summary>
        /// <seealso cref="NormalizationZ(float)"/>
        public virtual float Z => z;
    }
}