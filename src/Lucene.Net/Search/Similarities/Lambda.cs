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
    /// The <em>lambda (&#955;<sub>w</sub>)</em> parameter in information-based
    /// models. 
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="IBSimilarity"/> 
    public abstract class Lambda
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected Lambda() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
        }

        /// <summary>
        /// Computes the lambda parameter. </summary>
        public abstract float CalculateLambda(BasicStats stats);

        /// <summary>
        /// Explains the lambda parameter. </summary>
        public abstract Explanation Explain(BasicStats stats);

        /// <summary>
        /// Subclasses must override this method to return the code of the lambda
        /// formula. Since the original paper is not very clear on this matter, and
        /// also uses the DFR naming scheme incorrectly, the codes here were chosen
        /// arbitrarily.
        /// </summary>
        public override abstract string ToString();
    }
}