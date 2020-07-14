using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Ja
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

    //[Slow] // LUCENENET specific - not slow in .NET
    public class TestExtendedMode : BaseTokenStreamTestCase
    {
        private readonly Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new JapaneseTokenizer(reader, null, true, JapaneseTokenizerMode.EXTENDED);
            return new TokenStreamComponents(tokenizer, tokenizer);
        });

        /** simple test for supplementary characters */
        [Test]
        public void TestSurrogates()
        {
            AssertAnalyzesTo(analyzer, "𩬅艱鍟䇹愯瀛",
          new String[] { "𩬅", "艱", "鍟", "䇹", "愯", "瀛" });
        }

        /** random test ensuring we don't ever split supplementaries */
        [Test]
        public void TestSurrogates2()
        {
            int numIterations = AtLeast(1000);
            for (int i = 0; i < numIterations; i++)
            {
                String s = TestUtil.RandomUnicodeString(Random, 100);
                TokenStream ts = analyzer.GetTokenStream("foo", s);
                try
                {
                    ICharTermAttribute termAtt = ts.AddAttribute<ICharTermAttribute>();
                    ts.Reset();
                    while (ts.IncrementToken())
                    {
                        assertTrue(UnicodeUtil.ValidUTF16String(termAtt));
                    }
                    ts.End();
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(ts);
                }
            }
        }

        /** blast some random strings through the analyzer */
        [Test]
        public void TestRandomStrings()
        {
            Random random = Random;
            CheckRandomData(random, analyzer, 1000 * RandomMultiplier);
        }

        /** blast some random large strings through the analyzer */
        public void TestRandomHugeStrings()
        {
            Random random = Random;
            CheckRandomData(random, analyzer, 100 * RandomMultiplier, 8192);
        }
    }
}
