using System;
using System.Collections.Generic;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;
    using AutomatonTestUtil = Lucene.Net.Util.Automaton.AutomatonTestUtil;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using RegExp = Lucene.Net.Util.Automaton.RegExp;
    using SortedSetDocValuesField = SortedSetDocValuesField;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

    /// <summary>
    /// Tests the DocTermOrdsRewriteMethod
    /// </summary>
    [TestFixture]
    public class TestDocTermOrdsRewriteMethod : LuceneTestCase
    {
        protected internal IndexSearcher Searcher1;
        protected internal IndexSearcher Searcher2;
        private IndexReader Reader;
        private Directory Dir;
        protected internal string FieldName;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Dir = NewDirectory();
            FieldName = Random().NextBoolean() ? "field" : ""; // sometimes use an empty string as field name
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.KEYWORD, false)).SetMaxBufferedDocs(TestUtil.NextInt(Random(), 50, 1000)));
            List<string> terms = new List<string>();
            int num = AtLeast(200);
            for (int i = 0; i < num; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", Convert.ToString(i), Field.Store.NO));
                int numTerms = Random().Next(4);
                for (int j = 0; j < numTerms; j++)
                {
                    string s = TestUtil.RandomUnicodeString(Random());
                    doc.Add(NewStringField(FieldName, s, Field.Store.NO));
                    // if the default codec doesn't support sortedset, we will uninvert at search time
                    if (DefaultCodecSupportsSortedSet())
                    {
                        doc.Add(new SortedSetDocValuesField(FieldName, new BytesRef(s)));
                    }
                    terms.Add(s);
                }
                writer.AddDocument(doc);
            }

            if (VERBOSE)
            {
                // utf16 order
                terms.Sort();
                Console.WriteLine("UTF16 order:");
                foreach (string s in terms)
                {
                    Console.WriteLine("  " + UnicodeUtil.ToHexString(s));
                }
            }

            int numDeletions = Random().Next(num / 10);
            for (int i = 0; i < numDeletions; i++)
            {
                writer.DeleteDocuments(new Term("id", Convert.ToString(Random().Next(num))));
            }

            Reader = writer.Reader;
            Searcher1 = NewSearcher(Reader);
            Searcher2 = NewSearcher(Reader);
            writer.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            Reader.Dispose();
            Dir.Dispose();
            base.TearDown();
        }

        /// <summary>
        /// test a bunch of random regular expressions </summary>
        [Test]
        public virtual void TestRegexps()
        {
            int num = AtLeast(1000);
            for (int i = 0; i < num; i++)
            {
                string reg = AutomatonTestUtil.RandomRegexp(Random());
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: regexp=" + reg);
                }
                AssertSame(reg);
            }
        }

        /// <summary>
        /// check that the # of hits is the same as if the query
        /// is run against the inverted index
        /// </summary>
        protected internal virtual void AssertSame(string regexp)
        {
            RegexpQuery docValues = new RegexpQuery(new Term(FieldName, regexp), RegExp.NONE);
            docValues.MultiTermRewriteMethod = (new DocTermOrdsRewriteMethod());
            RegexpQuery inverted = new RegexpQuery(new Term(FieldName, regexp), RegExp.NONE);

            TopDocs invertedDocs = Searcher1.Search(inverted, 25);
            TopDocs docValuesDocs = Searcher2.Search(docValues, 25);

            CheckHits.CheckEqual(inverted, invertedDocs.ScoreDocs, docValuesDocs.ScoreDocs);
        }

        [Test]
        public virtual void TestEquals()
        {
            RegexpQuery a1 = new RegexpQuery(new Term(FieldName, "[aA]"), RegExp.NONE);
            RegexpQuery a2 = new RegexpQuery(new Term(FieldName, "[aA]"), RegExp.NONE);
            RegexpQuery b = new RegexpQuery(new Term(FieldName, "[bB]"), RegExp.NONE);
            Assert.AreEqual(a1, a2);
            Assert.IsFalse(a1.Equals(b));

            a1.MultiTermRewriteMethod = (new DocTermOrdsRewriteMethod());
            a2.MultiTermRewriteMethod = (new DocTermOrdsRewriteMethod());
            b.MultiTermRewriteMethod = (new DocTermOrdsRewriteMethod());
            Assert.AreEqual(a1, a2);
            Assert.IsFalse(a1.Equals(b));
            QueryUtils.Check(a1);
        }
    }
}