using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.ComplexPhrase
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
    public class TestComplexPhraseQuery : LuceneTestCase
    {
        Directory rd;
        Analyzer analyzer;
        DocData[] docsContent = {
            new DocData("john smith", "1", "developer"),
            new DocData("johathon smith", "2", "developer"),
            new DocData("john percival smith", "3", "designer"),
            new DocData("jackson waits tom", "4", "project manager")
        };

        private IndexSearcher searcher;
        private IndexReader reader;

        string defaultFieldName = "name";

        bool inOrder = true;

        [Test]
        public virtual void TestComplexPhrases()
        {
            CheckMatches("\"john smith\"", "1"); // Simple multi-term still works
            CheckMatches("\"j*   smyth~\"", "1,2"); // wildcards and fuzzies are OK in
            // phrases
            CheckMatches("\"(jo* -john)  smith\"", "2"); // boolean logic works
            CheckMatches("\"jo*  smith\"~2", "1,2,3"); // position logic works.
            CheckMatches("\"jo* [sma TO smZ]\" ", "1,2"); // range queries supported
            CheckMatches("\"john\"", "1,3"); // Simple single-term still works
            CheckMatches("\"(john OR johathon)  smith\"", "1,2"); // boolean logic with
            // brackets works.
            CheckMatches("\"(jo* -john) smyth~\"", "2"); // boolean logic with
            // brackets works.

            // CheckMatches("\"john -percival\"", "1"); // not logic doesn't work
            // currently :(.

            CheckMatches("\"john  nosuchword*\"", ""); // phrases with clauses producing
            // empty sets

            CheckBadQuery("\"jo*  id:1 smith\""); // mixing fields in a phrase is bad
            CheckBadQuery("\"jo* \"smith\" \""); // phrases inside phrases is bad
        }

        [Test]
        public virtual void TestUnOrderedProximitySearches()
        {
            inOrder = true;
            CheckMatches("\"smith jo*\"~2", ""); // ordered proximity produces empty set

            inOrder = false;
            CheckMatches("\"smith jo*\"~2", "1,2,3"); // un-ordered proximity
        }

        private void CheckBadQuery(String qString)
        {
            ComplexPhraseQueryParser qp = new ComplexPhraseQueryParser(TEST_VERSION_CURRENT, defaultFieldName, analyzer);
            qp.InOrder = inOrder;
            Exception expected = null;
            try
            {
                qp.Parse(qString);
            }
            catch (Exception e)
            {
                expected = e;
            }
            assertNotNull("Expected parse error in " + qString, expected);
        }

        private void CheckMatches(string qString, string expectedVals)
        {
            ComplexPhraseQueryParser qp = new ComplexPhraseQueryParser(TEST_VERSION_CURRENT, defaultFieldName, analyzer);
            qp.InOrder = inOrder;
            qp.FuzzyPrefixLength = 1; // usually a good idea

            Query q = qp.Parse(qString);

            HashSet<string> expecteds = new HashSet<string>();
            string[] vals = expectedVals.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < vals.Length; i++)
            {
                if (vals[i].Length > 0)
                    expecteds.Add(vals[i]);
            }

            TopDocs td = searcher.Search(q, 10);
            ScoreDoc[] sd = td.ScoreDocs;
            for (int i = 0; i < sd.Length; i++)
            {
                Document doc = searcher.Doc(sd[i].Doc);
                string id = doc.Get("id");
                assertTrue(qString + "matched doc#" + id + " not expected", expecteds
                    .Contains(id));
                expecteds.Remove(id);
            }

            assertEquals(qString + " missing some matches ", 0, expecteds.Count);
        }

        [Test]
        public virtual void TestFieldedQuery()
        {
            CheckMatches("name:\"john smith\"", "1");
            CheckMatches("name:\"j*   smyth~\"", "1,2");
            CheckMatches("role:\"developer\"", "1,2");
            CheckMatches("role:\"p* manager\"", "4");
            CheckMatches("role:de*", "1,2,3");
            CheckMatches("name:\"j* smyth~\"~5", "1,2,3");
            CheckMatches("role:\"p* manager\" AND name:jack*", "4");
            CheckMatches("+role:developer +name:jack*", "");
            CheckMatches("name:\"john smith\"~2 AND role:designer AND id:3", "3");
        }

        [Test]
        public virtual void TestHashcodeEquals()
        {
            ComplexPhraseQueryParser qp = new ComplexPhraseQueryParser(TEST_VERSION_CURRENT, defaultFieldName, analyzer);
            qp.InOrder = true;
            qp.FuzzyPrefixLength = 1;

            String qString = "\"aaa* bbb*\"";

            Query q = qp.Parse(qString);
            Query q2 = qp.Parse(qString);

            assertEquals(q.GetHashCode(), q2.GetHashCode());
            assertEquals(q, q2);

            qp.InOrder = (false); // SOLR-6011

            q2 = qp.Parse(qString);

            // although the general contract of hashCode can't guarantee different values, if we only change one thing
            // about a single query, it normally should result in a different value (and will with the current
            // implementation in ComplexPhraseQuery)
            assertTrue(q.GetHashCode() != q2.GetHashCode());
            assertTrue(!q.equals(q2));
            assertTrue(!q2.equals(q));
        }

        public override void SetUp()
        {
            base.SetUp();

            analyzer = new MockAnalyzer(Random());
            rd = NewDirectory();
            using (IndexWriter w = new IndexWriter(rd, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer)))
            {
                for (int i = 0; i < docsContent.Length; i++)
                {
                    Document doc = new Document();
                    doc.Add(NewTextField("name", docsContent[i].Name, Field.Store.YES));
                    doc.Add(NewTextField("id", docsContent[i].Id, Field.Store.YES));
                    doc.Add(NewTextField("role", docsContent[i].Role, Field.Store.YES));
                    w.AddDocument(doc);
                }
            }
            reader = DirectoryReader.Open(rd);
            searcher = NewSearcher(reader);
        }

        public override void TearDown()
        {
            reader.Dispose();
            rd.Dispose();
            base.TearDown();
        }


        private class DocData
        {
            public DocData(string name, string id, string role)
            {
                this.Name = name;
                this.Id = id;
                this.Role = role;
            }

            public string Name { get; private set; }
            public string Id { get; private set; }
            public string Role { get; private set; }
        }
    }
}
