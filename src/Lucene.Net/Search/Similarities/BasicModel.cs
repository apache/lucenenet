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
    /// This class acts as the base class for the specific <em>basic model</em>
    /// implementations in the DFR framework. Basic models compute the
    /// <em>informative content Inf<sub>1</sub> = -log<sub>2</sub>Prob<sub>1</sub>
    /// </em>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="DFRSimilarity"/>
    public abstract class BasicModel
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected BasicModel() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
        }

        /// <summary>
        /// Returns the informative content score. </summary>
        public abstract float Score(BasicStats stats, float tfn);

        /// <summary>
        /// Returns an explanation for the score.
        /// <para>Most basic models use the number of documents and the total term
        /// frequency to compute Inf<sub>1</sub>. this method provides a generic
        /// explanation for such models. Subclasses that use other statistics must
        /// override this method.</para>
        /// </summary>
        public virtual Explanation Explain(BasicStats stats, float tfn)
        {
            Explanation result = new Explanation();
            result.Description = this.GetType().Name + ", computed from: ";
            result.Value = Score(stats, tfn);
            result.AddDetail(new Explanation(tfn, "tfn"));
            result.AddDetail(new Explanation(stats.NumberOfDocuments, "numberOfDocuments"));
            result.AddDetail(new Explanation(stats.TotalTermFreq, "totalTermFreq"));
            return result;
        }

        /// <summary>
        /// Subclasses must override this method to return the code of the
        /// basic model formula. Refer to the original paper for the list.
        /// </summary>
        public override abstract string ToString();
    }
}