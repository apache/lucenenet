/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Regex;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;


namespace Lucene.Net.Search.Regex
{
    /// <summary>
    /// See http://svn.eu.apache.org/repos/asf/lucene/dev/trunk/lucene/src/test/org/apache/lucene/search/TestRegexpQuery.java
    /// </summary>
    [TestFixture]
    public class TestRegexpQuery : LuceneTestCase
    {
        private IndexSearcher _searcher;
        private const string FIELDNAME = "field";

        public override void SetUp()
        {
            base.SetUp();

            RAMDirectory directory = new RAMDirectory();
            IndexWriter writer = new IndexWriter(directory, new SimpleAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED);

            Document doc = new Document();
            doc.Add(new Field(FIELDNAME, "the quick brown fox jumps over the lazy crazy pretty dog",
                Field.Store.NO, Field.Index.ANALYZED));
            writer.AddDocument(doc);
            writer.Optimize();
            writer.Close();
            _searcher = new IndexSearcher(directory, true);
        }

        public override void TearDown()
        {
            _searcher.Close();
            base.TearDown();
        }

        private static Term NewTerm(string sValue)
        {
            return new Term(FIELDNAME, sValue);
        }

        private int GetRegexQueryHitsCount(string sRegex)
        {
            RegexQuery query = new RegexQuery(NewTerm(sRegex));
            return _searcher.Search(query, 5).TotalHits;
        }

        [Test]
        public void TestLiterals()
        {
            Assert.AreEqual(0, GetRegexQueryHitsCount(@"nothing"));

            Assert.AreEqual(1, GetRegexQueryHitsCount(@"brown"));
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"ver"));

            Assert.AreEqual(1, GetRegexQueryHitsCount(@"the"));
        }

        [Test]
        public void TestExpressions()
        {
            // wildcards
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"j.mps"));

            // repetitions
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"dogs?"));
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"pret+y"));

            // classes
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"q.[aeiou]c.*"));
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"br?own?"));
            Assert.AreEqual(0, GetRegexQueryHitsCount(@"z.[aeiou]c.*"));
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"c?[rl]azy"));
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"\bc?[rl]azy\b"));
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"c[^lmn]azy"));
            Assert.AreEqual(0, GetRegexQueryHitsCount(@"c[^r]azy"));

            Assert.AreEqual(1, GetRegexQueryHitsCount(@"\p{L}+"));
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"\p{L}{6,}"));
            Assert.AreEqual(0, GetRegexQueryHitsCount(@"\p{L}{7,}"));
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"\p{L}{6,7}"));
            Assert.AreEqual(0, GetRegexQueryHitsCount(@"\p{L}{7,9}"));

            Assert.AreEqual(1, GetRegexQueryHitsCount(@"\D+"));
            Assert.AreEqual(0, GetRegexQueryHitsCount(@"\d+"));

            // position
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"^q"));
            Assert.AreEqual(0, GetRegexQueryHitsCount(@"q$"));

            // alternatives
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"brown|red"));
            Assert.AreEqual(0, GetRegexQueryHitsCount(@"yellow|red"));
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"(l|cr)azy"));

            // lookaround
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"\b\w+(?=zy)"));
            Assert.AreEqual(0, GetRegexQueryHitsCount(@"\b\w+(?=zu)"));

            Assert.AreEqual(0, GetRegexQueryHitsCount(@"\b\w*q[^u]\w*\b"));
            Assert.AreEqual(1, GetRegexQueryHitsCount(@"\b\w*qu\w*\b"));
        }
    }

}
