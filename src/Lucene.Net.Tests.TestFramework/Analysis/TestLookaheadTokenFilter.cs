// Lucene version compatibility level 8.2.0
using Lucene.Net.Util;
using RandomizedTesting.Generators;
using System;
using Test = NUnit.Framework.TestAttribute;

namespace Lucene.Net.Analysis
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

    public class TestLookaheadTokenFilter : BaseTokenStreamTestCase
    {
        [Test]
        public void TestRandomStrings()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Random random = Random;
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, random.nextBoolean());
                TokenStream output = new MockRandomLookaheadTokenFilter(random, tokenizer);
                return new TokenStreamComponents(tokenizer, output);
            });
            //int maxLength = TestNightly ? 8192 : 1024;
            // LUCENENET specific - reduced Nightly iterations from 8192 to 4096
            // to keep it under the 1 hour free limit of Azure DevOps
            int maxLength = TestNightly ? 4096 : 1024;
            CheckRandomData(Random, a, 50 * RandomMultiplier, maxLength);
        }

        private sealed class NeverPeeksLookaheadTokenFilter : LookaheadTokenFilter<LookaheadTokenFilter.Position>
        {
            // LUCENENET specific - removed NewPosition override and using factory instead
            public NeverPeeksLookaheadTokenFilter(TokenStream input)
                : base(input, RollingBufferItemFactory<LookaheadTokenFilter.Position>.Default)
            {
            }

            public override bool IncrementToken()
            {
                return NextToken();
            }
        }

        [Test]
        public void TestNeverCallingPeek()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, Random.NextBoolean());
                TokenStream output = new NeverPeeksLookaheadTokenFilter(tokenizer);
                return new TokenStreamComponents(tokenizer, output);
            });
            //int maxLength = TestNightly ? 8192 : 1024;
            // LUCENENET specific - reduced Nightly iterations from 8192 to 4096
            // to keep it under the 1 hour free limit of Azure DevOps
            int maxLength = TestNightly ? 4096 : 1024;
            CheckRandomData(Random, a, 50 * RandomMultiplier, maxLength);
        }

        [Test]
        public void TestMissedFirstToken()
        {
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TrivialLookaheadFilter filter = new TrivialLookaheadFilter(source);
                return new TokenStreamComponents(source, filter);
            });

            AssertAnalyzesTo(analyzer,
                    "Only he who is running knows .",
                    new String[]{
            "Only",
            "Only-huh?",
            "he",
            "he-huh?",
            "who",
            "who-huh?",
            "is",
            "is-huh?",
            "running",
            "running-huh?",
            "knows",
            "knows-huh?",
            ".",
            ".-huh?"
                    });
        }
    }
}
