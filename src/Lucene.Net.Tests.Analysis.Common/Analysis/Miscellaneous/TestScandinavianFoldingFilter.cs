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

    public class TestScandinavianFoldingFilter : BaseTokenStreamTestCase
    {
        private static readonly Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            TokenStream stream = new ScandinavianFoldingFilter(tokenizer);
            return new TokenStreamComponents(tokenizer, stream);
        });

        [Test]
        public virtual void Test()
        {

            CheckOneTerm(analyzer, "aeäaeeea", "aaaeea"); // should not cause ArrayOutOfBoundsException

            CheckOneTerm(analyzer, "aeäaeeeae", "aaaeea");
            CheckOneTerm(analyzer, "aeaeeeae", "aaeea");

            CheckOneTerm(analyzer, "bøen", "boen");
            CheckOneTerm(analyzer, "åene", "aene");


            CheckOneTerm(analyzer, "blåbærsyltetøj", "blabarsyltetoj");
            CheckOneTerm(analyzer, "blaabaarsyltetoej", "blabarsyltetoj");
            CheckOneTerm(analyzer, "blåbärsyltetöj", "blabarsyltetoj");

            CheckOneTerm(analyzer, "raksmorgas", "raksmorgas");
            CheckOneTerm(analyzer, "räksmörgås", "raksmorgas");
            CheckOneTerm(analyzer, "ræksmørgås", "raksmorgas");
            CheckOneTerm(analyzer, "raeksmoergaas", "raksmorgas");
            CheckOneTerm(analyzer, "ræksmörgaos", "raksmorgas");


            CheckOneTerm(analyzer, "ab", "ab");
            CheckOneTerm(analyzer, "ob", "ob");
            CheckOneTerm(analyzer, "Ab", "Ab");
            CheckOneTerm(analyzer, "Ob", "Ob");

            CheckOneTerm(analyzer, "å", "a");

            CheckOneTerm(analyzer, "aa", "a");
            CheckOneTerm(analyzer, "aA", "a");
            CheckOneTerm(analyzer, "ao", "a");
            CheckOneTerm(analyzer, "aO", "a");

            CheckOneTerm(analyzer, "AA", "A");
            CheckOneTerm(analyzer, "Aa", "A");
            CheckOneTerm(analyzer, "Ao", "A");
            CheckOneTerm(analyzer, "AO", "A");

            CheckOneTerm(analyzer, "æ", "a");
            CheckOneTerm(analyzer, "ä", "a");

            CheckOneTerm(analyzer, "Æ", "A");
            CheckOneTerm(analyzer, "Ä", "A");

            CheckOneTerm(analyzer, "ae", "a");
            CheckOneTerm(analyzer, "aE", "a");

            CheckOneTerm(analyzer, "Ae", "A");
            CheckOneTerm(analyzer, "AE", "A");


            CheckOneTerm(analyzer, "ö", "o");
            CheckOneTerm(analyzer, "ø", "o");
            CheckOneTerm(analyzer, "Ö", "O");
            CheckOneTerm(analyzer, "Ø", "O");


            CheckOneTerm(analyzer, "oo", "o");
            CheckOneTerm(analyzer, "oe", "o");
            CheckOneTerm(analyzer, "oO", "o");
            CheckOneTerm(analyzer, "oE", "o");

            CheckOneTerm(analyzer, "Oo", "O");
            CheckOneTerm(analyzer, "Oe", "O");
            CheckOneTerm(analyzer, "OO", "O");
            CheckOneTerm(analyzer, "OE", "O");
        }

        /// <summary>
        /// check that the empty string doesn't cause issues </summary>
        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new ScandinavianFoldingFilter(tokenizer));
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