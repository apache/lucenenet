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
    /// Model of the information gain based on Laplace's law of succession.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class AfterEffectL : AfterEffect
    {
        /// <summary>
        /// Sole constructor: parameter-free </summary>
        public AfterEffectL()
        {
        }

        public override sealed float Score(BasicStats stats, float tfn)
        {
            return 1 / (tfn + 1);
        }

        public override sealed Explanation Explain(BasicStats stats, float tfn)
        {
            Explanation result = new Explanation();
            result.Description = this.GetType().Name + ", computed from: ";
            result.Value = Score(stats, tfn);
            result.AddDetail(new Explanation(tfn, "tfn"));
            return result;
        }

        public override string ToString()
        {
            return "L";
        }
    }
}