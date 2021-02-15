namespace Lucene.Net.Classification
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
    /// The result of a call to <see cref="IClassifier{T}.AssignClass(string)"/> holding an assigned class of type <typeparam name="T"/> and a score.
    /// @lucene.experimental
    /// </summary>
    public class ClassificationResult<T>
    {

        private readonly T assignedClass;
        private readonly double score;

        /// <summary>
        /// Constructor
        /// <param name="assignedClass">the class <typeparamref name="T"/> assigned by a <see cref="IClassifier{T}"/></param>
        /// <param name="score">score the score for the <paramref name="assignedClass"/> as a <see cref="double"/></param>
        /// </summary>
        public ClassificationResult(T assignedClass, double score) 
        {
            this.assignedClass = assignedClass;
            this.score = score;
        }

        /// <summary>
        /// retrieve the result class
        /// @return a <typeparamref name="T"/> representing an assigned class
        /// </summary>
        public virtual T AssignedClass => assignedClass;

        /// <summary>
        /// Gets a <see cref="double"/> representing a result score.
        /// </summary>
        public virtual double Score => score;
    }
}