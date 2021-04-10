// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;

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

    public class TestLimitTokenPositionFilter : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestMaxPosition2()
        {
            foreach (bool consumeAll in new bool[] { true, false })
            {
                Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                    // if we are consuming all tokens, we can use the checks, otherwise we can't
                    tokenizer.EnableChecks = consumeAll;
                    return new TokenStreamComponents(tokenizer, new LimitTokenPositionFilter(tokenizer, 2, consumeAll));
                });

                // don't use assertAnalyzesTo here, as the end offset is not the end of the string (unless consumeAll is true, in which case its correct)!
                AssertTokenStreamContents(a.GetTokenStream("dummy", "1  2     3  4  5"), new string[] { "1", "2" }, new int[] { 0, 3 }, new int[] { 1, 4 }, consumeAll ? 16 : (int?)null);
                AssertTokenStreamContents(a.GetTokenStream("dummy", new StringReader("1 2 3 4 5")), new string[] { "1", "2" }, new int[] { 0, 2 }, new int[] { 1, 3 }, consumeAll ? 9 : (int?)null);

                // less than the limit, ensure we behave correctly
                AssertTokenStreamContents(a.GetTokenStream("dummy", "1  "), new string[] { "1" }, new int[] { 0 }, new int[] { 1 }, consumeAll ? 3 : (int?)null);

                // equal to limit
                AssertTokenStreamContents(a.GetTokenStream("dummy", "1  2  "), new string[] { "1", "2" }, new int[] { 0, 3 }, new int[] { 1, 4 }, consumeAll ? 6 : (int?)null);
            }
        }

        [Test]
        public virtual void TestMaxPosition3WithSynomyms()
        {
            foreach (bool consumeAll in new bool[] { true, false })
            {
                MockTokenizer tokenizer = new MockTokenizer(new StringReader("one two three four five"), MockTokenizer.WHITESPACE, false);
                // if we are consuming all tokens, we can use the checks, otherwise we can't
                tokenizer.EnableChecks = consumeAll;

                SynonymMap.Builder builder = new SynonymMap.Builder(true);
                builder.Add(new CharsRef("one"), new CharsRef("first"), true);
                builder.Add(new CharsRef("one"), new CharsRef("alpha"), true);
                builder.Add(new CharsRef("one"), new CharsRef("beguine"), true);
                CharsRef multiWordCharsRef = new CharsRef();
                SynonymMap.Builder.Join(new string[] { "and", "indubitably", "single", "only" }, multiWordCharsRef);
                builder.Add(new CharsRef("one"), multiWordCharsRef, true);
                SynonymMap.Builder.Join(new string[] { "dopple", "ganger" }, multiWordCharsRef);
                builder.Add(new CharsRef("two"), multiWordCharsRef, true);
                SynonymMap synonymMap = builder.Build();
                TokenStream stream = new SynonymFilter(tokenizer, synonymMap, true);
                stream = new LimitTokenPositionFilter(stream, 3, consumeAll);

                // "only", the 4th word of multi-word synonym "and indubitably single only" is not emitted, since its position is greater than 3.
                AssertTokenStreamContents(stream, new string[] { "one", "first", "alpha", "beguine", "and", "two", "indubitably", "dopple", "three", "single", "ganger" }, new int[] { 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0 });
            }
        }

        [Test]
        public virtual void TestIllegalArguments()
        {
            // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            Assert.Throws<ArgumentOutOfRangeException>(() => new LimitTokenPositionFilter(new MockTokenizer(new StringReader("one two three four five")), 0));
        }
    }
}