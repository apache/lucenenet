// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Miscellaneous
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

    public class TestScandinavianNormalizationFilter : BaseTokenStreamTestCase
    {
        private static readonly Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            TokenStream stream = new ScandinavianNormalizationFilter(tokenizer);
            return new TokenStreamComponents(tokenizer, stream);
        });

        [Test]
        public virtual void Test()
        {

            CheckOneTerm(analyzer, "aeäaeeea", "æææeea"); // should not cause ArrayIndexOutOfBoundsException

            CheckOneTerm(analyzer, "aeäaeeeae", "æææeeæ");
            CheckOneTerm(analyzer, "aeaeeeae", "ææeeæ");

            CheckOneTerm(analyzer, "bøen", "bøen");
            CheckOneTerm(analyzer, "bOEen", "bØen");
            CheckOneTerm(analyzer, "åene", "åene");


            CheckOneTerm(analyzer, "blåbærsyltetøj", "blåbærsyltetøj");
            CheckOneTerm(analyzer, "blaabaersyltetöj", "blåbærsyltetøj");
            CheckOneTerm(analyzer, "räksmörgås", "ræksmørgås");
            CheckOneTerm(analyzer, "raeksmörgaos", "ræksmørgås");
            CheckOneTerm(analyzer, "raeksmörgaas", "ræksmørgås");
            CheckOneTerm(analyzer, "raeksmoergås", "ræksmørgås");


            CheckOneTerm(analyzer, "ab", "ab");
            CheckOneTerm(analyzer, "ob", "ob");
            CheckOneTerm(analyzer, "Ab", "Ab");
            CheckOneTerm(analyzer, "Ob", "Ob");

            CheckOneTerm(analyzer, "å", "å");

            CheckOneTerm(analyzer, "aa", "å");
            CheckOneTerm(analyzer, "aA", "å");
            CheckOneTerm(analyzer, "ao", "å");
            CheckOneTerm(analyzer, "aO", "å");

            CheckOneTerm(analyzer, "AA", "Å");
            CheckOneTerm(analyzer, "Aa", "Å");
            CheckOneTerm(analyzer, "Ao", "Å");
            CheckOneTerm(analyzer, "AO", "Å");

            CheckOneTerm(analyzer, "æ", "æ");
            CheckOneTerm(analyzer, "ä", "æ");

            CheckOneTerm(analyzer, "Æ", "Æ");
            CheckOneTerm(analyzer, "Ä", "Æ");

            CheckOneTerm(analyzer, "ae", "æ");
            CheckOneTerm(analyzer, "aE", "æ");

            CheckOneTerm(analyzer, "Ae", "Æ");
            CheckOneTerm(analyzer, "AE", "Æ");


            CheckOneTerm(analyzer, "ö", "ø");
            CheckOneTerm(analyzer, "ø", "ø");
            CheckOneTerm(analyzer, "Ö", "Ø");
            CheckOneTerm(analyzer, "Ø", "Ø");


            CheckOneTerm(analyzer, "oo", "ø");
            CheckOneTerm(analyzer, "oe", "ø");
            CheckOneTerm(analyzer, "oO", "ø");
            CheckOneTerm(analyzer, "oE", "ø");

            CheckOneTerm(analyzer, "Oo", "Ø");
            CheckOneTerm(analyzer, "Oe", "Ø");
            CheckOneTerm(analyzer, "OO", "Ø");
            CheckOneTerm(analyzer, "OE", "Ø");
        }

        /// <summary>
        /// check that the empty string doesn't cause issues </summary>
        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new ScandinavianNormalizationFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomData()
        {
            CheckRandomData(Random, analyzer, 1000 * RandomMultiplier);
        }
    }
}