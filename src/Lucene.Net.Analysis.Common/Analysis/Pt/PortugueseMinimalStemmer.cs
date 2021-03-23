// Lucene version compatibility level 4.8.1
namespace Lucene.Net.Analysis.Pt
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
    /// Minimal Stemmer for Portuguese
    /// <para>
    /// This follows the "RSLP-S" algorithm presented in:
    /// <c>A study on the Use of Stemming for Monolingual Ad-Hoc Portuguese
    /// Information Retrieval</c> (Orengo, et al)
    /// which is just the plural reduction step of the RSLP
    /// algorithm from <c>A Stemming Algorithm for the Portuguese Language</c>,
    /// Orengo et al.
    /// </para>
    /// </summary>
    /// <seealso cref="RSLPStemmerBase"/>
    public class PortugueseMinimalStemmer : RSLPStemmerBase
    {
        private static readonly Step pluralStep = Parse(typeof(PortugueseMinimalStemmer), "portuguese.rslp")["Plural"];

        public virtual int Stem(char[] s, int len)
        {
            return pluralStep.Apply(s, len);
        }
    }
}