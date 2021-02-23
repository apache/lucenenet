// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Pt;

namespace Lucene.Net.Analysis.Gl
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
    /// Minimal Stemmer for Galician
    /// <para>
    /// This follows the "RSLP-S" algorithm, but modified for Galician.
    /// Hence this stemmer only applies the plural reduction step of:
    /// "Regras do lematizador para o galego"
    /// </para>
    /// </summary>
    /// <seealso cref="RSLPStemmerBase"/>
    public class GalicianMinimalStemmer : RSLPStemmerBase
    {
        private static readonly Step pluralStep = Parse(typeof(GalicianMinimalStemmer), "galician.rslp")["Plural"];

        public virtual int Stem(char[] s, int len)
        {
            return pluralStep.Apply(s, len);
        }
    }
}