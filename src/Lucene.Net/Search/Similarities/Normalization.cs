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
    /// This class acts as the base class for the implementations of the term
    /// frequency normalization methods in the DFR framework.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="DFRSimilarity"/>
    public abstract class Normalization
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected Normalization() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
        }

        /// <summary>
        /// Returns the normalized term frequency. </summary>
        /// <param name="len"> the field length.  </param>
        public abstract float Tfn(BasicStats stats, float tf, float len);

        /// <summary>
        /// Returns an explanation for the normalized term frequency.
        /// <para>The default normalization methods use the field length of the document
        /// and the average field length to compute the normalized term frequency.
        /// This method provides a generic explanation for such methods.
        /// Subclasses that use other statistics must override this method.</para>
        /// </summary>
        public virtual Explanation Explain(BasicStats stats, float tf, float len)
        {
            Explanation result = new Explanation();
            result.Description = this.GetType().Name + ", computed from: ";
            result.Value = Tfn(stats, tf, len);
            result.AddDetail(new Explanation(tf, "tf"));
            result.AddDetail(new Explanation(stats.AvgFieldLength, "avgFieldLength"));
            result.AddDetail(new Explanation(len, "len"));
            return result;
        }

        /// <summary>
        /// Implementation used when there is no normalization. </summary>
        public sealed class NoNormalization : Normalization
        {
            /// <summary>
            /// Sole constructor: parameter-free </summary>
            public NoNormalization()
            {
            }

            public override float Tfn(BasicStats stats, float tf, float len)
            {
                return tf;
            }

            public override Explanation Explain(BasicStats stats, float tf, float len)
            {
                return new Explanation(1, "no normalization");
            }

            public override string ToString()
            {
                return "";
            }
        }

        /// <summary>
        /// Subclasses must override this method to return the code of the
        /// normalization formula. Refer to the original paper for the list.
        /// </summary>
        public override abstract string ToString();
    }
}