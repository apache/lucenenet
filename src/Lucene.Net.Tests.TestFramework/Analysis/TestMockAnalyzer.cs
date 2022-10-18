// Lucene version compatibility level 8.2.0
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using RandomizedTesting.Generators;
using System;
using System.IO;
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

    public class TestMockAnalyzer : BaseTokenStreamTestCase
    {
        /** Test a configuration that behaves a lot like WhitespaceAnalyzer */
        [Test]
        public void TestWhitespace()
        {
            Analyzer a = new MockAnalyzer(Random);
            AssertAnalyzesTo(a, "A bc defg hiJklmn opqrstuv wxy z ",
            new String[] { "a", "bc", "defg", "hijklmn", "opqrstuv", "wxy", "z" });
            AssertAnalyzesTo(a, "aba cadaba shazam",
                new String[] { "aba", "cadaba", "shazam" });
            AssertAnalyzesTo(a, "break on whitespace",
                new String[] { "break", "on", "whitespace" });
        }

        /** Test a configuration that behaves a lot like SimpleAnalyzer */
        [Test]
        public void TestSimple()
        {
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
            AssertAnalyzesTo(a, "a-bc123 defg+hijklmn567opqrstuv78wxy_z ",
                    new String[] { "a", "bc", "defg", "hijklmn", "opqrstuv", "wxy", "z" });
            AssertAnalyzesTo(a, "aba4cadaba-Shazam",
                new String[] { "aba", "cadaba", "shazam" });
            AssertAnalyzesTo(a, "break+on/Letters",
                new String[] { "break", "on", "letters" });
        }

        /** Test a configuration that behaves a lot like KeywordAnalyzer */
        [Test]
        public void TestKeyword()
        {
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.KEYWORD, false);
            AssertAnalyzesTo(a, "a-bc123 defg+hijklmn567opqrstuv78wxy_z ",
                    new String[] { "a-bc123 defg+hijklmn567opqrstuv78wxy_z " });
            AssertAnalyzesTo(a, "aba4cadaba-Shazam",
                new String[] { "aba4cadaba-Shazam" });
            AssertAnalyzesTo(a, "break+on/Nothing",
                new String[] { "break+on/Nothing" });
            // currently though emits no tokens for empty string: maybe we can do it,
            // but we don't want to emit tokens infinitely...
            AssertAnalyzesTo(a, "", new String[0]);
        }

        // Test some regular expressions as tokenization patterns
        /** Test a configuration where each character is a term */
        [Test]
        public void TestSingleChar()
        {
            CharacterRunAutomaton single =
                new CharacterRunAutomaton(new RegExp(".").ToAutomaton());
            Analyzer a = new MockAnalyzer(Random, single, false);
            AssertAnalyzesTo(a, "foobar",
                    new String[] { "f", "o", "o", "b", "a", "r" },
                    new int[] { 0, 1, 2, 3, 4, 5 },
                    new int[] { 1, 2, 3, 4, 5, 6 }
                );
            CheckRandomData(Random, a, 100);
        }

        /** Test a configuration where two characters makes a term */
        [Test]
        public void TestTwoChars()
        {
            CharacterRunAutomaton single =
                new CharacterRunAutomaton(new RegExp("..").ToAutomaton());
            Analyzer a = new MockAnalyzer(Random, single, false);
            AssertAnalyzesTo(a, "foobar",
                    new String[] { "fo", "ob", "ar" },
                    new int[] { 0, 2, 4 },
                    new int[] { 2, 4, 6 }
                );
            // make sure when last term is a "partial" match that end() is correct
            AssertTokenStreamContents(a.GetTokenStream("bogus", "fooba"),
                new String[] { "fo", "ob" },
                new int[] { 0, 2 },
                new int[] { 2, 4 },
                new int[] { 1, 1 },
                5
            );
            CheckRandomData(Random, a, 100);
        }

        /** Test a configuration where three characters makes a term */
        [Test]
        public void TestThreeChars()
        {
            CharacterRunAutomaton single =
                new CharacterRunAutomaton(new RegExp("...").ToAutomaton());
            Analyzer a = new MockAnalyzer(Random, single, false);
            AssertAnalyzesTo(a, "foobar",
                    new String[] { "foo", "bar" },
                    new int[] { 0, 3 },
                    new int[] { 3, 6 }
                );
            // make sure when last term is a "partial" match that end() is correct
            AssertTokenStreamContents(a.GetTokenStream("bogus", "fooba"),
                new String[] { "foo" },
                new int[] { 0 },
                new int[] { 3 },
                new int[] { 1 },
                5
            );
            CheckRandomData(Random, a, 100);
        }

        /** Test a configuration where word starts with one uppercase */
        [Test]
        public void TestUppercase()
        {
            CharacterRunAutomaton single =
                new CharacterRunAutomaton(new RegExp("[A-Z][a-z]*").ToAutomaton());
            Analyzer a = new MockAnalyzer(Random, single, false);
            AssertAnalyzesTo(a, "FooBarBAZ",
                    new String[] { "Foo", "Bar", "B", "A", "Z" },
                    new int[] { 0, 3, 6, 7, 8 },
                    new int[] { 3, 6, 7, 8, 9 }
                );
            AssertAnalyzesTo(a, "aFooBar",
                new String[] { "Foo", "Bar" },
                new int[] { 1, 4 },
                new int[] { 4, 7 }
            );
            CheckRandomData(Random, a, 100);
        }

        /** Test a configuration that behaves a lot like StopAnalyzer */
        [Test]
        public void TestStop()
        {
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET);
            AssertAnalyzesTo(a, "the quick brown a fox",
                    new String[] { "quick", "brown", "fox" },
                    new int[] { 2, 1, 2 });
        }

        /** Test a configuration that behaves a lot like KeepWordFilter */
        [Test]
        public void TestKeep()
        {
            CharacterRunAutomaton keepWords =
              new CharacterRunAutomaton(
                  BasicOperations.Complement(
                      BasicOperations.Union(
                          BasicAutomata.MakeString("foo"), BasicAutomata.MakeString("bar")) /*,
                      Operations.DEFAULT_MAX_DETERMINIZED_STATES*/));
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true, keepWords);
            AssertAnalyzesTo(a, "quick foo brown bar bar fox foo",
                    new String[] { "foo", "bar", "bar", "foo" },
                    new int[] { 2, 2, 1, 2 });
        }

        /** Test a configuration that behaves a lot like LengthFilter */
        [Test]
        public void TestLength()
        {
            CharacterRunAutomaton length5 = new CharacterRunAutomaton(new RegExp(".{5,}").ToAutomaton());
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, true, length5);
            AssertAnalyzesTo(a, "ok toolong fine notfine",
                    new String[] { "ok", "fine" },
                    new int[] { 1, 2 });
        }

        /** Test MockTokenizer encountering a too long token */
        [Test]
        public void TestTooLongToken()
        {
            Analyzer whitespace = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer t = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false, 5);
                return new TokenStreamComponents(t, t);
            });

            AssertTokenStreamContents(whitespace.GetTokenStream("bogus", "test 123 toolong ok "),
                    new String[] { "test", "123", "toolo", "ng", "ok" },
                    new int[] { 0, 5, 9, 14, 17 },
                    new int[] { 4, 8, 14, 16, 19 },
                    20);

            AssertTokenStreamContents(whitespace.GetTokenStream("bogus", "test 123 toolo"),
                new String[] { "test", "123", "toolo" },
                new int[] { 0, 5, 9 },
                new int[] { 4, 8, 14 },
                14);
        }

        [Test]
        public void TestLUCENE_3042()
        {
            String testString = "t";

            Analyzer analyzer = new MockAnalyzer(Random);
            using (TokenStream stream = analyzer.GetTokenStream("dummy", testString))
            {
                stream.Reset();
                while (stream.IncrementToken())
                {
                    // consume
                }
                stream.End();
            }

            AssertAnalyzesTo(analyzer, testString, new String[] { "t" });
        }

        /** blast some random strings through the analyzer */
        [Test]
        public void TestRandomStrings()
        {
            CheckRandomData(Random, new MockAnalyzer(Random), AtLeast(1000));
        }

        /** blast some random strings through differently configured tokenizers */
        [Test]
        public void TestRandomRegexps()
        {
            //int iters = TestNightly ? AtLeast(30) : AtLeast(1);
            // LUCENENET specific - reduced Nightly iterations from 30 to 15
            // to keep it under the 1 hour free limit of Azure DevOps
            int iters = TestNightly ? AtLeast(15) : AtLeast(1);
            for (int i = 0; i < iters; i++)
            {
                CharacterRunAutomaton dfa = new CharacterRunAutomaton(AutomatonTestUtil.RandomAutomaton(Random) /*, int.MaxValue*/);
                bool lowercase = Random.NextBoolean();
                int limit = TestUtil.NextInt32(Random, 0, 500);
                Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) => {
                    Tokenizer t = new MockTokenizer(reader, dfa, lowercase, limit);
                    return new TokenStreamComponents(t, t);
                });
                CheckRandomData(Random, a, 100);
                a.Dispose();
            }
        }

        [Test]
        public void TestForwardOffsets()
        {
            int num = AtLeast(1000);
            for (int i = 0; i < num; i++)
            {
                String s = TestUtil.RandomHtmlishString(Random, 20);
                StringReader reader = new StringReader(s);
                MockCharFilter charfilter = new MockCharFilter(reader, 2);
                MockAnalyzer analyzer = new MockAnalyzer(Random);
                using TokenStream ts = analyzer.GetTokenStream("bogus", charfilter);
                ts.Reset();
                while (ts.IncrementToken())
                {
                    ;
                }
                ts.End();
            }
        }

        private sealed class AnalyzerWrapperAnonymousClass : AnalyzerWrapper
        {
            private readonly Analyzer @delegate;
            public AnalyzerWrapperAnonymousClass(Analyzer @delegate)
                : base(@delegate.Strategy)
            {
                this.@delegate = @delegate;
            }

            protected override TextReader WrapReader(string fieldName, TextReader reader)
            {
                return new MockCharFilter(reader, 7);
            }
            protected override Analyzer GetWrappedAnalyzer(string fieldName)
            {
                return @delegate;
            }
        }

        [Test]
        public void TestWrapReader()
        {
            // LUCENE-5153: test that wrapping an analyzer's reader is allowed
            Random random = Random;

            Analyzer @delegate = new MockAnalyzer(random);
            Analyzer a = new AnalyzerWrapperAnonymousClass(@delegate);


            CheckOneTerm(a, "abc", "aabc");
        }

        // LUCENENET NOTE: This has some compatibility issues with Lucene 4.8.1, but need this test when
        // DelegatingAnalyzerWrapper is ported
        //[Test]
        //public void TestChangeGaps() 
        //{
        //    // LUCENE-5324: check that it is possible to change the wrapper's gaps
        //     int positionGap = Random.nextInt(1000);
        //     int offsetGap = Random.nextInt(1000);
        //     Analyzer @delegate = new MockAnalyzer(Random);
        //// Analyzer a = new DelegatingAnalyzerWrapper(@delegate.getReuseStrategy()) {
        ////      @Override
        ////      protected Analyzer getWrappedAnalyzer(String fieldName)
        ////{
        ////    return @delegate;
        ////}
        ////@Override
        ////      public int getPositionIncrementGap(String fieldName)
        ////{
        ////    return positionGap;
        ////}
        ////@Override
        ////      public int getOffsetGap(String fieldName)
        ////{
        ////    return offsetGap;
        ////}
        ////    };

        //     RandomIndexWriter writer = new RandomIndexWriter(Random, NewDirectory(), a);
        // Document doc = new Document();
        // FieldType ft = new FieldType();
        //ft.IndexOptions=(IndexOptions.DOCS);
        //    ft.IsTokenized=(true);
        //    ft.setStoreTermVectors(true);
        //    ft.setStoreTermVectorPositions(true);
        //    ft.setStoreTermVectorOffsets(true);
        //    doc.add(new Field("f", "a", ft));
        //    doc.add(new Field("f", "a", ft));
        //    writer.addDocument(doc);
        //     LeafReader reader = getOnlyLeafReader(writer.getReader());
        // Fields fields = reader.getTermVectors(0);
        //     Terms terms = fields.terms("f");
        //     TermsEnum te = terms.iterator();
        //    assertEquals(new BytesRef("a"), te.next());
        //     PostingsEnum dpe = te.postings(null, PostingsEnum.ALL);
        //    assertEquals(0, dpe.nextDoc());
        //assertEquals(2, dpe.freq());
        //assertEquals(0, dpe.nextPosition());
        //assertEquals(0, dpe.startOffset());
        // int endOffset = dpe.endOffset();
        //assertEquals(1 + positionGap, dpe.nextPosition());
        //assertEquals(1 + endOffset + offsetGap, dpe.endOffset());
        //assertEquals(null, te.Next());
        //reader.close();
        //    writer.Dispose();
        //    writer.IndexWriter.Directory.Dispose();
        //  }
    }
}
