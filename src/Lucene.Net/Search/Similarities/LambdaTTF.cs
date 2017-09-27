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
    /// Computes lambda as <c>totalTermFreq+1 / numberOfDocuments+1</c>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class LambdaTTF : Lambda
    {
        /// <summary>
        /// Sole constructor: parameter-free </summary>
        public LambdaTTF()
        {
        }

        public override sealed float CalculateLambda(BasicStats stats)
        {
            return (stats.TotalTermFreq + 1F) / (stats.NumberOfDocuments + 1F);
        }

        public override sealed Explanation Explain(BasicStats stats)
        {
            Explanation result = new Explanation();
            result.Description = this.GetType().Name + ", computed from: ";
            result.Value = CalculateLambda(stats);
            result.AddDetail(new Explanation(stats.TotalTermFreq, "totalTermFreq"));
            result.AddDetail(new Explanation(stats.NumberOfDocuments, "numberOfDocuments"));
            return result;
        }

        public override string ToString()
        {
            return "L";
        }
    }
}