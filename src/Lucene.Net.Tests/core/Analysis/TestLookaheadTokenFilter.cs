using Lucene.Net.Attributes;
using Lucene.Net.Randomized.Generators;
using NUnit.Framework;
using System;
using System.IO;

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

    [TestFixture]
    public class TestLookaheadTokenFilter : BaseTokenStreamTestCase
    {
        [Test, LongRunningTest, MaxTime(int.MaxValue)]
        public virtual void TestRandomStrings()
        {
            Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);
            CheckRandomData(Random(), a, 200 * RANDOM_MULTIPLIER, 8192);
        }

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestLookaheadTokenFilter OuterInstance;

            public AnalyzerAnonymousInnerClassHelper(TestLookaheadTokenFilter outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Random random = Random();
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, random.NextBoolean());
                TokenStream output = new MockRandomLookaheadTokenFilter(random, tokenizer);
                return new TokenStreamComponents(tokenizer, output);
            }
        }

        private class NeverPeeksLookaheadTokenFilter : LookaheadTokenFilter<LookaheadTokenFilter.Position>
        {
            public NeverPeeksLookaheadTokenFilter(TokenStream input)
                : base(input)
            {
            }

            protected internal override LookaheadTokenFilter.Position NewPosition()
            {
                return new LookaheadTokenFilter.Position();
            }

            public sealed override bool IncrementToken()
            {
                return NextToken();
            }
        }

        [Test, LongRunningTest, MaxTime(int.MaxValue)]
        public virtual void TestNeverCallingPeek()
        {
            Analyzer a = new NCPAnalyzerAnonymousInnerClassHelper(this);
            CheckRandomData(Random(), a, 200 * RANDOM_MULTIPLIER, 8192);
        }

        private class NCPAnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestLookaheadTokenFilter OuterInstance;

            public NCPAnalyzerAnonymousInnerClassHelper(TestLookaheadTokenFilter outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, Random().NextBoolean());
                TokenStream output = new NeverPeeksLookaheadTokenFilter(tokenizer);
                return new TokenStreamComponents(tokenizer, output);
            }
        }

        [Test]
        public virtual void TestMissedFirstToken()
        {
            Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper2(this);

            AssertAnalyzesTo(analyzer, "Only he who is running knows .", new string[] { "Only", "Only-huh?", "he", "he-huh?", "who", "who-huh?", "is", "is-huh?", "running", "running-huh?", "knows", "knows-huh?", ".", ".-huh?" });
        }

        private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
        {
            private readonly TestLookaheadTokenFilter OuterInstance;

            public AnalyzerAnonymousInnerClassHelper2(TestLookaheadTokenFilter outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TrivialLookaheadFilter filter = new TrivialLookaheadFilter(source);
                return new TokenStreamComponents(source, filter);
            }
        }
    }
}