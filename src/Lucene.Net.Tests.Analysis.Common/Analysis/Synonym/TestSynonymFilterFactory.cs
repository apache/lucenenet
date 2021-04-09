// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Pattern;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;
using Reader = System.IO.TextReader;
using Version = Lucene.Net.Util.LuceneVersion;

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

    public class TestSynonymFilterFactory : BaseTokenStreamFactoryTestCase
    {

        /// <summary>
        /// checks for synonyms of "GB" in synonyms.txt </summary>
        private void CheckSolrSynonyms(TokenFilterFactory factory)
        {
            Reader reader = new StringReader("GB");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = factory.Create(stream);
            assertTrue(stream is SynonymFilter);
            AssertTokenStreamContents(stream, new string[] { "GB", "gib", "gigabyte", "gigabytes" }, new int[] { 1, 0, 0, 0 });
        }

        /// <summary>
        /// checks for synonyms of "second" in synonyms-wordnet.txt </summary>
        private void CheckWordnetSynonyms(TokenFilterFactory factory)
        {
            Reader reader = new StringReader("second");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = factory.Create(stream);
            assertTrue(stream is SynonymFilter);
            AssertTokenStreamContents(stream, new string[] { "second", "2nd", "two" }, new int[] { 1, 0, 0 });
        }

        /// <summary>
        /// test that we can parse and use the solr syn file </summary>
        [Test]
        public virtual void TestSynonyms()
        {
            CheckSolrSynonyms(TokenFilterFactory("Synonym", "synonyms", "synonyms.txt"));
        }

        /// <summary>
        /// test that we can parse and use the solr syn file, with the old impl </summary>
        /// @deprecated Remove this test in Lucene 5.0  
        [Test]
        [Obsolete("Remove this test in Lucene 5.0")]
        public virtual void TestSynonymsOld()
        {
            Reader reader = new StringReader("GB");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Synonym", Version.LUCENE_33, new ClasspathResourceLoader(this.GetType()), "synonyms", "synonyms.txt").Create(stream);
            assertTrue(stream is SlowSynonymFilter);
            AssertTokenStreamContents(stream, new string[] { "GB", "gib", "gigabyte", "gigabytes" }, new int[] { 1, 0, 0, 0 });
        }

        /// <summary>
        /// test multiword offsets with the old impl </summary>
        /// @deprecated Remove this test in Lucene 5.0  
        [Test]
        [Obsolete("Remove this test in Lucene 5.0")]
        public virtual void TestMultiwordOffsetsOld()
        {
            Reader reader = new StringReader("national hockey league");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Synonym", Version.LUCENE_33, new StringMockResourceLoader("national hockey league, nhl"), "synonyms", "synonyms.txt").Create(stream);
            // WTF?
            AssertTokenStreamContents(stream, new string[] { "national", "nhl", "hockey", "league" }, new int[] { 0, 0, 0, 0 }, new int[] { 22, 22, 22, 22 }, new int[] { 1, 0, 1, 1 });
        }

        /// <summary>
        /// if the synonyms are completely empty, test that we still analyze correctly </summary>
        [Test]
        public virtual void TestEmptySynonyms()
        {
            Reader reader = new StringReader("GB");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Synonym", TEST_VERSION_CURRENT, new StringMockResourceLoader(""), "synonyms", "synonyms.txt").Create(stream); // empty file!
            AssertTokenStreamContents(stream, new string[] { "GB" });
        }

        [Test]
        public virtual void TestFormat()
        {
            CheckSolrSynonyms(TokenFilterFactory("Synonym", "synonyms", "synonyms.txt", "format", "solr"));
            CheckWordnetSynonyms(TokenFilterFactory("Synonym", "synonyms", "synonyms-wordnet.txt", "format", "wordnet"));
            // explicit class should work the same as the "solr" alias
            CheckSolrSynonyms(TokenFilterFactory("Synonym", "synonyms", "synonyms.txt", "format", typeof(SolrSynonymParser).AssemblyQualifiedName));
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("Synonym", "synonyms", "synonyms.txt", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }

        internal const string TOK_SYN_ARG_VAL = "argument";
        internal const string TOK_FOO_ARG_VAL = "foofoofoo";

        /// <summary>
        /// Test that we can parse TokenierFactory's arguments </summary>
        [Test]
        public virtual void TestTokenizerFactoryArguments()
        {
            // diff versions produce diff delegator behavior,
            // all should be (mostly) equivilent for our test purposes.
#pragma warning disable 612, 618
            DoTestTokenizerFactoryArguments(Version.LUCENE_33, typeof(SlowSynonymFilterFactory));
            DoTestTokenizerFactoryArguments(Version.LUCENE_34, typeof(FSTSynonymFilterFactory));
            DoTestTokenizerFactoryArguments(Version.LUCENE_35, typeof(FSTSynonymFilterFactory));

            DoTestTokenizerFactoryArguments(Version.LUCENE_CURRENT, typeof(FSTSynonymFilterFactory));
#pragma warning restore 612, 618
        }

        protected internal virtual void DoTestTokenizerFactoryArguments(Version ver, Type delegatorClass)
        {
            string clazz = typeof(PatternTokenizerFactory).AssemblyQualifiedName;
            TokenFilterFactory factory = null;

            // simple arg form
            factory = TokenFilterFactory("Synonym", ver, "synonyms", "synonyms.txt", "tokenizerFactory", clazz, "pattern", "(.*)", "group", "0");
            AssertDelegator(factory, delegatorClass);

            // prefix
            factory = TokenFilterFactory("Synonym", ver, "synonyms", "synonyms.txt", "tokenizerFactory", clazz, "tokenizerFactory.pattern", "(.*)", "tokenizerFactory.group", "0");
            AssertDelegator(factory, delegatorClass);

            // sanity check that sub-PatternTokenizerFactory fails w/o pattern
            try
            {
                factory = TokenFilterFactory("Synonym", ver, "synonyms", "synonyms.txt", "tokenizerFactory", clazz);
                fail("tokenizerFactory should have complained about missing pattern arg");
            }
            catch (Exception expected) when (expected.IsException())
            {
                // :NOOP:
            }

            // sanity check that sub-PatternTokenizerFactory fails on unexpected
            try
            {
                factory = TokenFilterFactory("Synonym", ver, "synonyms", "synonyms.txt", "tokenizerFactory", clazz, "tokenizerFactory.pattern", "(.*)", "tokenizerFactory.bogusbogusbogus", "bogus", "tokenizerFactory.group", "0");
                fail("tokenizerFactory should have complained about missing pattern arg");
            }
            catch (Exception expected) when (expected.IsException())
            {
                // :NOOP:
            }
        }

        private static void AssertDelegator(TokenFilterFactory factory, Type delegatorClass)
        {
            assertNotNull(factory);
            assertTrue("factory not expected class: " + factory.GetType(), factory is SynonymFilterFactory);
            SynonymFilterFactory synFac = (SynonymFilterFactory)factory;
#pragma warning disable 612, 618
            object delegator = synFac.Delegator;
#pragma warning restore 612, 618
            assertNotNull(delegator);
            assertTrue("delegator not expected class: " + delegator.GetType(), delegatorClass.IsInstanceOfType(delegator));

        }
    }
}