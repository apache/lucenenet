using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Search
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

    using AutomatonTestUtil = Lucene.Net.Util.Automaton.AutomatonTestUtil;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
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
        protected internal IndexSearcher searcher1;
        protected internal IndexSearcher searcher2;
        private IndexReader reader;
        private Directory dir;
        protected internal string fieldName;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            fieldName = Random.NextBoolean() ? "field" : ""; // sometimes use an empty string as field name
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.KEYWORD, false)).SetMaxBufferedDocs(TestUtil.NextInt32(Random, 50, 1000)));
            JCG.List<string> terms = new JCG.List<string>();
            int num = AtLeast(200);
            for (int i = 0; i < num; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", Convert.ToString(i), Field.Store.NO));
                int numTerms = Random.Next(4);
                for (int j = 0; j < numTerms; j++)
                {
                    string s = TestUtil.RandomUnicodeString(Random);
                    doc.Add(NewStringField(fieldName, s, Field.Store.NO));
                    // if the default codec doesn't support sortedset, we will uninvert at search time
                    if (DefaultCodecSupportsSortedSet)
                    {
                        doc.Add(new SortedSetDocValuesField(fieldName, new BytesRef(s)));
                    }
                    terms.Add(s);
                }
                writer.AddDocument(doc);
            }

            if (Verbose)
            {
                // utf16 order
                terms.Sort(StringComparer.Ordinal);
                Console.WriteLine("UTF16 order:");
                foreach (string s in terms)
                {
                    Console.WriteLine("  " + UnicodeUtil.ToHexString(s));
                }
            }

            int numDeletions = Random.Next(num / 10);
            for (int i = 0; i < numDeletions; i++)
            {
                writer.DeleteDocuments(new Term("id", Convert.ToString(Random.Next(num))));
            }

            reader = writer.GetReader();
            searcher1 = NewSearcher(reader);
            searcher2 = NewSearcher(reader);
            writer.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
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
                string reg = AutomatonTestUtil.RandomRegexp(Random);
                if (Verbose)
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
            RegexpQuery docValues = new RegexpQuery(new Term(fieldName, regexp), RegExpSyntax.NONE);
            docValues.MultiTermRewriteMethod = (new DocTermOrdsRewriteMethod());
            RegexpQuery inverted = new RegexpQuery(new Term(fieldName, regexp), RegExpSyntax.NONE);

            TopDocs invertedDocs = searcher1.Search(inverted, 25);
            TopDocs docValuesDocs = searcher2.Search(docValues, 25);

            CheckHits.CheckEqual(inverted, invertedDocs.ScoreDocs, docValuesDocs.ScoreDocs);
        }

        [Test]
        public virtual void TestEquals()
        {
            RegexpQuery a1 = new RegexpQuery(new Term(fieldName, "[aA]"), RegExpSyntax.NONE);
            RegexpQuery a2 = new RegexpQuery(new Term(fieldName, "[aA]"), RegExpSyntax.NONE);
            RegexpQuery b = new RegexpQuery(new Term(fieldName, "[bB]"), RegExpSyntax.NONE);
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