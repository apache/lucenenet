// Lucene version compatibility level 4.8.1
using J2N;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Core
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
    /// Compares MockTokenizer (which is simple with no optimizations) with equivalent 
    /// core tokenizers (that have optimizations like buffering).
    /// 
    /// Any tests here need to probably consider unicode version of the JRE (it could
    /// cause false fails).
    /// </summary>
    public class TestDuelingAnalyzers : LuceneTestCase
    {
        private CharacterRunAutomaton jvmLetter;

        public override void SetUp()
        {
            base.SetUp();
            // build an automaton matching this jvm's letter definition
            State initial = new State();
            State accept = new State();
            accept.Accept = true;
            for (int i = 0; i <= 0x10FFFF; i++)
            {
                if (Character.IsLetter(i))
                {
                    initial.AddTransition(new Transition(i, i, accept));
                }
            }
            Automaton single = new Automaton(initial);
            single.Reduce();
            Automaton repeat = BasicOperations.Repeat(single);
            jvmLetter = new CharacterRunAutomaton(repeat);
        }

        [Test]
        public virtual void TestLetterAscii()
        {
            Random random = Random;
            Analyzer left = new MockAnalyzer(random, jvmLetter, false);
            Analyzer right = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new LetterTokenizer(TEST_VERSION_CURRENT, reader);
                return new TokenStreamComponents(tokenizer, tokenizer);
            });
            for (int i = 0; i < 1000; i++)
            {
                string s = TestUtil.RandomSimpleString(random);
                assertEquals(s, left.GetTokenStream("foo", newStringReader(s)), right.GetTokenStream("foo", newStringReader(s)));
            }
        }

        // not so useful since its all one token?!
        [Test]
        public virtual void TestLetterAsciiHuge()
        {
            Random random = Random;
            int maxLength = 8192; // CharTokenizer.IO_BUFFER_SIZE*2
            MockAnalyzer left = new MockAnalyzer(random, jvmLetter, false);
            left.MaxTokenLength = 255; // match CharTokenizer's max token length
            Analyzer right = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new LetterTokenizer(TEST_VERSION_CURRENT, reader);
                return new TokenStreamComponents(tokenizer, tokenizer);
            });
            int numIterations = AtLeast(50);
            for (int i = 0; i < numIterations; i++)
            {
                string s = TestUtil.RandomSimpleString(random, maxLength);
                assertEquals(s, left.GetTokenStream("foo", newStringReader(s)), right.GetTokenStream("foo", newStringReader(s)));
            }
        }

        [Test]
        public virtual void TestLetterHtmlish()
        {
            Random random = Random;
            Analyzer left = new MockAnalyzer(random, jvmLetter, false);
            Analyzer right = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new LetterTokenizer(TEST_VERSION_CURRENT, reader);
                return new TokenStreamComponents(tokenizer, tokenizer);
            });
            for (int i = 0; i < 1000; i++)
            {
                string s = TestUtil.RandomHtmlishString(random, 20);
                assertEquals(s, left.GetTokenStream("foo", newStringReader(s)), right.GetTokenStream("foo", newStringReader(s)));
            }
        }

        [Test]
        public virtual void TestLetterHtmlishHuge()
        {
            Random random = Random;
            int maxLength = 1024; // this is number of elements, not chars!
            MockAnalyzer left = new MockAnalyzer(random, jvmLetter, false);
            left.MaxTokenLength = 255; // match CharTokenizer's max token length
            Analyzer right = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new LetterTokenizer(TEST_VERSION_CURRENT, reader);
                return new TokenStreamComponents(tokenizer, tokenizer);
            });
            int numIterations = AtLeast(50);
            for (int i = 0; i < numIterations; i++)
            {
                string s = TestUtil.RandomHtmlishString(random, maxLength);
                assertEquals(s, left.GetTokenStream("foo", newStringReader(s)), right.GetTokenStream("foo", newStringReader(s)));
            }
        }

        [Test]
        public virtual void TestLetterUnicode()
        {
            Random random = Random;
            Analyzer left = new MockAnalyzer(LuceneTestCase.Random, jvmLetter, false);
            Analyzer right = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new LetterTokenizer(TEST_VERSION_CURRENT, reader);
                return new TokenStreamComponents(tokenizer, tokenizer);
            });
            for (int i = 0; i < 1000; i++)
            {
                string s = TestUtil.RandomUnicodeString(random);
                assertEquals(s, left.GetTokenStream("foo", newStringReader(s)), right.GetTokenStream("foo", newStringReader(s)));
            }
        }

        [Test]
        public virtual void TestLetterUnicodeHuge()
        {
            Random random = Random;
            int maxLength = 4300; // CharTokenizer.IO_BUFFER_SIZE + fudge
            MockAnalyzer left = new MockAnalyzer(random, jvmLetter, false);
            left.MaxTokenLength = 255; // match CharTokenizer's max token length
            Analyzer right = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new LetterTokenizer(TEST_VERSION_CURRENT, reader);
                return new TokenStreamComponents(tokenizer, tokenizer);
            });
            int numIterations = AtLeast(50);
            for (int i = 0; i < numIterations; i++)
            {
                string s = TestUtil.RandomUnicodeString(random, maxLength);
                assertEquals(s, left.GetTokenStream("foo", newStringReader(s)), right.GetTokenStream("foo", newStringReader(s)));
            }
        }

        // we only check a few core attributes here.
        // TODO: test other things
        public virtual void assertEquals(string s, TokenStream left, TokenStream right)
        {
            left.Reset();
            right.Reset();
            ICharTermAttribute leftTerm = left.AddAttribute<ICharTermAttribute>();
            ICharTermAttribute rightTerm = right.AddAttribute<ICharTermAttribute>();
            IOffsetAttribute leftOffset = left.AddAttribute<IOffsetAttribute>();
            IOffsetAttribute rightOffset = right.AddAttribute<IOffsetAttribute>();
            IPositionIncrementAttribute leftPos = left.AddAttribute<IPositionIncrementAttribute>();
            IPositionIncrementAttribute rightPos = right.AddAttribute<IPositionIncrementAttribute>();

            while (left.IncrementToken())
            {
                assertTrue("wrong number of tokens for input: " + s, right.IncrementToken());
                assertEquals("wrong term text for input: " + s, leftTerm.ToString(), rightTerm.ToString());
                assertEquals("wrong position for input: " + s, leftPos.PositionIncrement, rightPos.PositionIncrement);
                assertEquals("wrong start offset for input: " + s, leftOffset.StartOffset, rightOffset.StartOffset);
                assertEquals("wrong end offset for input: " + s, leftOffset.EndOffset, rightOffset.EndOffset);
            };
            assertFalse("wrong number of tokens for input: " + s, right.IncrementToken());
            left.End();
            right.End();
            assertEquals("wrong final offset for input: " + s, leftOffset.EndOffset, rightOffset.EndOffset);
            left.Dispose();
            right.Dispose();
        }

        // TODO: maybe push this out to TestUtil or LuceneTestCase and always use it instead?
        private static TextReader newStringReader(string s)
        {
            Random random = Random;
            TextReader r = new StringReader(s);
            if (random.NextBoolean())
            {
                r = new MockReaderWrapper(random, r);
            }
            return r;
        }
    }
}