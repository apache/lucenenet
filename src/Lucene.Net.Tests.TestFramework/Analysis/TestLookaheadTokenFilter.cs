// Lucene version compatibility level 8.2.0
using System;
using Lucene.Net.TestFramework;

#if TESTFRAMEWORK_MSTEST
using Test = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
using Assert = Lucene.Net.TestFramework.Assert;
#elif TESTFRAMEWORK_NUNIT
using Test = NUnit.Framework.TestAttribute;
using Assert = NUnit.Framework.Assert;
#elif TESTFRAMEWORK_XUNIT
using Test = Lucene.Net.TestFramework.SkippableFactAttribute;
using Assert = Lucene.Net.TestFramework.Assert;
#endif

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

#if TESTFRAMEWORK_MSTEST
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute]
#endif
    public class TestLookaheadTokenFilter : BaseTokenStreamTestCase
#if TESTFRAMEWORK_XUNIT
        , Xunit.IClassFixture<BeforeAfterClass>
    {
        public TestLookaheadTokenFilter(BeforeAfterClass beforeAfter)
            : base(beforeAfter)
        {
        }
#else
    {
#endif
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
            int maxLength = TEST_NIGHTLY ? 8192 : 1024;
            CheckRandomData(Random, a, 50 * RANDOM_MULTIPLIER, maxLength);
        }

        private sealed class NeverPeeksLookaheadTokenFilter : LookaheadTokenFilter<LookaheadTokenFilter.Position>
        {
            public NeverPeeksLookaheadTokenFilter(TokenStream input)
                : base(input)
            {
            }

            protected override Position NewPosition()
            {
                return new Position();
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
            int maxLength = TEST_NIGHTLY ? 8192 : 1024;
            CheckRandomData(Random, a, 50 * RANDOM_MULTIPLIER, maxLength);
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
