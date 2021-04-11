// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.En;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Synonym
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
    /// Tests parser for the Solr synonyms format
    /// @lucene.experimental
    /// </summary>
    public class TestSolrSynonymParser : BaseTokenStreamTestCase
    {

        /// <summary>
        /// Tests some simple examples from the solr wiki </summary>
        [Test]
        public virtual void TestSimple()
        {
            string testFile = "i-pod, ipod, ipoooood\n" + "foo => foo bar\n" + "foo => baz\n" + "this test, that testing";

            SolrSynonymParser parser = new SolrSynonymParser(true, true, new MockAnalyzer(Random));
            parser.Parse(new StringReader(testFile));
            SynonymMap map = parser.Build();

            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, true));
            });

            AssertAnalyzesTo(analyzer, "ball", new string[] { "ball" }, new int[] { 1 });

            AssertAnalyzesTo(analyzer, "i-pod", new string[] { "i-pod", "ipod", "ipoooood" }, new int[] { 1, 0, 0 });

            AssertAnalyzesTo(analyzer, "foo", new string[] { "foo", "baz", "bar" }, new int[] { 1, 0, 1 });

            AssertAnalyzesTo(analyzer, "this test", new string[] { "this", "that", "test", "testing" }, new int[] { 1, 0, 1, 0 });
        }

        /// <summary>
        /// parse a syn file with bad syntax </summary>
        [Test]
        public virtual void TestInvalidDoubleMap()
        {
            string testFile = "a => b => c";
            SolrSynonymParser parser = new SolrSynonymParser(true, true, new MockAnalyzer(Random));
            try
            {
                parser.Parse(new StringReader(testFile));
                fail();
            }
            catch (Exception pe) when (pe.IsParseException())
            {
                // expected
            }
        }

        /// <summary>
        /// parse a syn file with bad syntax </summary>
        [Test]
        public virtual void TestInvalidAnalyzesToNothingOutput()
        {
            string testFile = "a => 1";
            SolrSynonymParser parser = new SolrSynonymParser(true, true, new MockAnalyzer(Random, MockTokenizer.SIMPLE, false));
            try
            {
                parser.Parse(new StringReader(testFile));
                fail();
            }
            catch (Exception pe) when (pe.IsParseException())
            {
                // expected
            }
        }

        /// <summary>
        /// parse a syn file with bad syntax </summary>
        [Test]
        public virtual void TestInvalidAnalyzesToNothingInput()
        {
            string testFile = "1 => a";
            SolrSynonymParser parser = new SolrSynonymParser(true, true, new MockAnalyzer(Random, MockTokenizer.SIMPLE, false));
            try
            {
                parser.Parse(new StringReader(testFile));
                fail();
            }
            catch (Exception pe) when (pe.IsParseException())
            {
                // expected
            }
        }

        /// <summary>
        /// parse a syn file with bad syntax </summary>
        [Test]
        public virtual void TestInvalidPositionsInput()
        {
            string testFile = "testola => the test";
            SolrSynonymParser parser = new SolrSynonymParser(true, true, new EnglishAnalyzer(TEST_VERSION_CURRENT));
            try
            {
                parser.Parse(new StringReader(testFile));
                fail();
            }
            catch (Exception pe) when (pe.IsParseException())
            {
                // expected
            }
        }

        /// <summary>
        /// parse a syn file with bad syntax </summary>
        [Test]
        public virtual void TestInvalidPositionsOutput()
        {
            string testFile = "the test => testola";
            SolrSynonymParser parser = new SolrSynonymParser(true, true, new EnglishAnalyzer(TEST_VERSION_CURRENT));
            try
            {
                parser.Parse(new StringReader(testFile));
                fail();
            }
            catch (Exception pe) when (pe.IsParseException())
            {
                // expected
            }
        }

        /// <summary>
        /// parse a syn file with some escaped syntax chars </summary>
        [Test]
        public virtual void TestEscapedStuff()
        {
            string testFile = "a\\=>a => b\\=>b\n" + "a\\,a => b\\,b";
            SolrSynonymParser parser = new SolrSynonymParser(true, true, new MockAnalyzer(Random, MockTokenizer.KEYWORD, false));
            parser.Parse(new StringReader(testFile));
            SynonymMap map = parser.Build();
            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.KEYWORD, false);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, false));
            });

            AssertAnalyzesTo(analyzer, "ball", new string[] { "ball" }, new int[] { 1 });

            AssertAnalyzesTo(analyzer, "a=>a", new string[] { "b=>b" }, new int[] { 1 });

            AssertAnalyzesTo(analyzer, "a,a", new string[] { "b,b" }, new int[] { 1 });
        }
    }
}