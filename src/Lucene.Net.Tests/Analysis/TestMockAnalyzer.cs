using Lucene.Net.Documents;
using Lucene.Net.Index;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

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

    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using AutomatonTestUtil = Lucene.Net.Util.Automaton.AutomatonTestUtil;
    using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;
    using BasicOperations = Lucene.Net.Util.Automaton.BasicOperations;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CharacterRunAutomaton = Lucene.Net.Util.Automaton.CharacterRunAutomaton;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using Document = Documents.Document;
    using Field = Field;
    using Fields = Lucene.Net.Index.Fields;
    using FieldType = FieldType;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using RegExp = Lucene.Net.Util.Automaton.RegExp;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestMockAnalyzer : BaseTokenStreamTestCase
    {
        /// <summary>
        /// Test a configuration that behaves a lot like WhitespaceAnalyzer </summary>
        [Test]
        public virtual void TestWhitespace()
        {
            Analyzer a = new MockAnalyzer(Random);
            AssertAnalyzesTo(a, "A bc defg hiJklmn opqrstuv wxy z ", new string[] { "a", "bc", "defg", "hijklmn", "opqrstuv", "wxy", "z" });
            AssertAnalyzesTo(a, "aba cadaba shazam", new string[] { "aba", "cadaba", "shazam" });
            AssertAnalyzesTo(a, "break on whitespace", new string[] { "break", "on", "whitespace" });
        }

        /// <summary>
        /// Test a configuration that behaves a lot like SimpleAnalyzer </summary>
        [Test]
        public virtual void TestSimple()
        {
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
            AssertAnalyzesTo(a, "a-bc123 defg+hijklmn567opqrstuv78wxy_z ", new string[] { "a", "bc", "defg", "hijklmn", "opqrstuv", "wxy", "z" });
            AssertAnalyzesTo(a, "aba4cadaba-Shazam", new string[] { "aba", "cadaba", "shazam" });
            AssertAnalyzesTo(a, "break+on/Letters", new string[] { "break", "on", "letters" });
        }

        /// <summary>
        /// Test a configuration that behaves a lot like KeywordAnalyzer </summary>
        [Test]
        public virtual void TestKeyword()
        {
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.KEYWORD, false);
            AssertAnalyzesTo(a, "a-bc123 defg+hijklmn567opqrstuv78wxy_z ", new string[] { "a-bc123 defg+hijklmn567opqrstuv78wxy_z " });
            AssertAnalyzesTo(a, "aba4cadaba-Shazam", new string[] { "aba4cadaba-Shazam" });
            AssertAnalyzesTo(a, "break+on/Nothing", new string[] { "break+on/Nothing" });
            // currently though emits no tokens for empty string: maybe we can do it,
            // but we don't want to emit tokens infinitely...
            AssertAnalyzesTo(a, "", new string[0]);
        }

        // Test some regular expressions as tokenization patterns
        /// <summary>
        /// Test a configuration where each character is a term </summary>
        [Test]
        public virtual void TestSingleChar()
        {
            var single = new CharacterRunAutomaton((new RegExp(".")).ToAutomaton());
            Analyzer a = new MockAnalyzer(Random, single, false);
            AssertAnalyzesTo(a, "foobar", new[] { "f", "o", "o", "b", "a", "r" }, new[] { 0, 1, 2, 3, 4, 5 }, new[] { 1, 2, 3, 4, 5, 6 });
            CheckRandomData(Random, a, 100);
        }

        /// <summary>
        /// Test a configuration where two characters makes a term </summary>
        [Test]
        public virtual void TestTwoChars()
        {
            CharacterRunAutomaton single = new CharacterRunAutomaton((new RegExp("..")).ToAutomaton());
            Analyzer a = new MockAnalyzer(Random, single, false);
            AssertAnalyzesTo(a, "foobar", new string[] { "fo", "ob", "ar" }, new int[] { 0, 2, 4 }, new int[] { 2, 4, 6 });
            // make sure when last term is a "partial" match that End() is correct
            AssertTokenStreamContents(a.GetTokenStream("bogus", new StringReader("fooba")), new string[] { "fo", "ob" }, new int[] { 0, 2 }, new int[] { 2, 4 }, new int[] { 1, 1 }, new int?(5));
            CheckRandomData(Random, a, 100);
        }

        /// <summary>
        /// Test a configuration where three characters makes a term </summary>
        [Test]
        public virtual void TestThreeChars()
        {
            CharacterRunAutomaton single = new CharacterRunAutomaton((new RegExp("...")).ToAutomaton());
            Analyzer a = new MockAnalyzer(Random, single, false);
            AssertAnalyzesTo(a, "foobar", new string[] { "foo", "bar" }, new int[] { 0, 3 }, new int[] { 3, 6 });
            // make sure when last term is a "partial" match that End() is correct
            AssertTokenStreamContents(a.GetTokenStream("bogus", new StringReader("fooba")), new string[] { "foo" }, new int[] { 0 }, new int[] { 3 }, new int[] { 1 }, new int?(5));
            CheckRandomData(Random, a, 100);
        }

        /// <summary>
        /// Test a configuration where word starts with one uppercase </summary>
        [Test]
        public virtual void TestUppercase()
        {
            CharacterRunAutomaton single = new CharacterRunAutomaton((new RegExp("[A-Z][a-z]*")).ToAutomaton());
            Analyzer a = new MockAnalyzer(Random, single, false);
            AssertAnalyzesTo(a, "FooBarBAZ", new string[] { "Foo", "Bar", "B", "A", "Z" }, new int[] { 0, 3, 6, 7, 8 }, new int[] { 3, 6, 7, 8, 9 });
            AssertAnalyzesTo(a, "aFooBar", new string[] { "Foo", "Bar" }, new int[] { 1, 4 }, new int[] { 4, 7 });
            CheckRandomData(Random, a, 100);
        }

        /// <summary>
        /// Test a configuration that behaves a lot like StopAnalyzer </summary>
        [Test]
        public virtual void TestStop()
        {
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET);
            AssertAnalyzesTo(a, "the quick brown a fox", new string[] { "quick", "brown", "fox" }, new int[] { 2, 1, 2 });
        }

        /// <summary>
        /// Test a configuration that behaves a lot like KeepWordFilter </summary>
        [Test]
        public virtual void TestKeep()
        {
            CharacterRunAutomaton keepWords = new CharacterRunAutomaton(BasicOperations.Complement(Automaton.Union(new Automaton[] { BasicAutomata.MakeString("foo"), BasicAutomata.MakeString("bar") })));
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, keepWords);
            AssertAnalyzesTo(a, "quick foo brown bar bar fox foo", new string[] { "foo", "bar", "bar", "foo" }, new int[] { 2, 2, 1, 2 });
        }

        /// <summary>
        /// Test a configuration that behaves a lot like LengthFilter </summary>
        [Test]
        public virtual void TestLength()
        {
            CharacterRunAutomaton length5 = new CharacterRunAutomaton((new RegExp(".{5,}")).ToAutomaton());
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, true, length5);
            AssertAnalyzesTo(a, "ok toolong fine notfine", new string[] { "ok", "fine" }, new int[] { 1, 2 });
        }

        /// <summary>
        /// Test MockTokenizer encountering a too long token </summary>
        [Test]
        public virtual void TestTooLongToken()
        {
            Analyzer whitespace = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false, 5);
                return new TokenStreamComponents(t, t);
            });
            AssertTokenStreamContents(whitespace.GetTokenStream("bogus", new StringReader("test 123 toolong ok ")), new string[] { "test", "123", "toolo", "ng", "ok" }, new int[] { 0, 5, 9, 14, 17 }, new int[] { 4, 8, 14, 16, 19 }, new int?(20));
            AssertTokenStreamContents(whitespace.GetTokenStream("bogus", new StringReader("test 123 toolo")), new string[] { "test", "123", "toolo" }, new int[] { 0, 5, 9 }, new int[] { 4, 8, 14 }, new int?(14));
        }

        [Test]
        public virtual void TestLUCENE_3042()
        {
            string testString = "t";

            Analyzer analyzer = new MockAnalyzer(Random);
            Exception priorException = null;
            TokenStream stream = analyzer.GetTokenStream("dummy", new StringReader(testString));
            try
            {
                stream.Reset();
                while (stream.IncrementToken())
                {
                    // consume
                }
                stream.End();
            }
            catch (Exception e)
            {
                priorException = e;
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(priorException, stream);
            }

            AssertAnalyzesTo(analyzer, testString, new string[] { "t" });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new MockAnalyzer(Random), AtLeast(1000));
        }

        /// <summary>
        /// blast some random strings through differently configured tokenizers </summary>
        [Test]
        [Slow]
        public virtual void TestRandomRegexps()
        {
            int iters = AtLeast(30);
            for (int i = 0; i < iters; i++)
            {
                CharacterRunAutomaton dfa = new CharacterRunAutomaton(AutomatonTestUtil.RandomAutomaton(Random));
                bool lowercase = Random.NextBoolean();
                int limit = TestUtil.NextInt32(Random, 0, 500);
                Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer t = new MockTokenizer(reader, dfa, lowercase, limit);
                    return new TokenStreamComponents(t, t);
                });
                CheckRandomData(Random, a, 100);
                a.Dispose();
            }
        }

        [Test]
        public virtual void TestForwardOffsets()
        {
            int num = AtLeast(10000);
            for (int i = 0; i < num; i++)
            {
                string s = TestUtil.RandomHtmlishString(Random, 20);
                StringReader reader = new StringReader(s);
                MockCharFilter charfilter = new MockCharFilter(reader, 2);
                MockAnalyzer analyzer = new MockAnalyzer(Random);
                Exception priorException = null;
                TokenStream ts = analyzer.GetTokenStream("bogus", charfilter.m_input);
                try
                {
                    ts.Reset();
                    while (ts.IncrementToken())
                    {
                        ;
                    }
                    ts.End();
                }
                catch (Exception e)
                {
                    priorException = e;
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(priorException, ts);
                }
            }
        }

        [Test]
        public virtual void TestWrapReader()
        {
            // LUCENE-5153: test that wrapping an analyzer's reader is allowed
            Random random = Random;

            Analyzer @delegate = new MockAnalyzer(random);
            Analyzer a = new AnalyzerWrapperAnonymousClass(this, @delegate.Strategy, @delegate);

            CheckOneTerm(a, "abc", "aabc");
        }

        private sealed class AnalyzerWrapperAnonymousClass : AnalyzerWrapper
        {
            private readonly TestMockAnalyzer outerInstance;

            private Analyzer @delegate;

            public AnalyzerWrapperAnonymousClass(TestMockAnalyzer outerInstance, ReuseStrategy getReuseStrategy, Analyzer @delegate)
                : base(getReuseStrategy)
            {
                this.outerInstance = outerInstance;
                this.@delegate = @delegate;
            }

            protected override TextReader WrapReader(string fieldName, TextReader reader)
            {
                return new MockCharFilter(reader, 7);
            }

            protected override TokenStreamComponents WrapComponents(string fieldName, TokenStreamComponents components)
            {
                return components;
            }

            protected override Analyzer GetWrappedAnalyzer(string fieldName)
            {
                return @delegate;
            }
        }

        [Test]
        public virtual void TestChangeGaps()
        {
            // LUCENE-5324: check that it is possible to change the wrapper's gaps
            int positionGap = Random.Next(1000);
            int offsetGap = Random.Next(1000);
            Analyzer @delegate = new MockAnalyzer(Random);
            Analyzer a = new AnalyzerWrapperAnonymousClass2(this, @delegate.Strategy, positionGap, offsetGap, @delegate);

            RandomIndexWriter writer = new RandomIndexWriter(Random, NewDirectory());
            Document doc = new Document();
            FieldType ft = new FieldType();
            ft.IsIndexed = true;
            ft.IndexOptions = IndexOptions.DOCS_ONLY;
            ft.IsTokenized = true;
            ft.StoreTermVectors = true;
            ft.StoreTermVectorPositions = true;
            ft.StoreTermVectorOffsets = true;
            doc.Add(new Field("f", "a", ft));
            doc.Add(new Field("f", "a", ft));
            writer.AddDocument(doc, a);
            AtomicReader reader = GetOnlySegmentReader(writer.GetReader());
            Fields fields = reader.GetTermVectors(0);
            Terms terms = fields.GetTerms("f");
            TermsEnum te = terms.GetEnumerator();
            Assert.IsTrue(te.MoveNext());
            Assert.AreEqual(new BytesRef("a"), te.Term);
            DocsAndPositionsEnum dpe = te.DocsAndPositions(null, null);
            Assert.AreEqual(0, dpe.NextDoc());
            Assert.AreEqual(2, dpe.Freq);
            Assert.AreEqual(0, dpe.NextPosition());
            Assert.AreEqual(0, dpe.StartOffset);
            int endOffset = dpe.EndOffset;
            Assert.AreEqual(1 + positionGap, dpe.NextPosition());
            Assert.AreEqual(1 + endOffset + offsetGap, dpe.EndOffset);
            Assert.IsFalse(te.MoveNext());
            reader.Dispose();
            writer.Dispose();
            writer.IndexWriter.Directory.Dispose();
        }

        private sealed class AnalyzerWrapperAnonymousClass2 : AnalyzerWrapper
        {
            private readonly TestMockAnalyzer outerInstance;

            private int positionGap;
            private int offsetGap;
            private Analyzer @delegate;

            public AnalyzerWrapperAnonymousClass2(TestMockAnalyzer outerInstance, ReuseStrategy getReuseStrategy, int positionGap, int offsetGap, Analyzer @delegate)
                : base(getReuseStrategy)
            {
                this.outerInstance = outerInstance;
                this.positionGap = positionGap;
                this.offsetGap = offsetGap;
                this.@delegate = @delegate;
            }

            protected override Analyzer GetWrappedAnalyzer(string fieldName)
            {
                return @delegate;
            }

            public override int GetPositionIncrementGap(string fieldName)
            {
                return positionGap;
            }

            public override int GetOffsetGap(string fieldName)
            {
                return offsetGap;
            }
        }
    }
}