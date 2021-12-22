using Lucene.Net.Documents;
using NUnit.Framework;

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

    using DateTools = DateTools;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Test date sorting, i.e. auto-sorting of fields with type "long".
    /// See http://issues.apache.org/jira/browse/LUCENE-1045
    /// </summary>
    [TestFixture]
    public class TestDateSort : LuceneTestCase
    {
        private const string TEXT_FIELD = "text";
        private const string DATE_TIME_FIELD = "dateTime";

        private Directory directory;
        private IndexReader reader;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            // Create an index writer.
            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);

            // oldest doc:
            // Add the first document.  text = "Document 1"  dateTime = Oct 10 03:25:22 EDT 2007
            writer.AddDocument(CreateDocument("Document 1", 1192001122000L));
            // Add the second document.  text = "Document 2"  dateTime = Oct 10 03:25:26 EDT 2007
            writer.AddDocument(CreateDocument("Document 2", 1192001126000L));
            // Add the third document.  text = "Document 3"  dateTime = Oct 11 07:12:13 EDT 2007
            writer.AddDocument(CreateDocument("Document 3", 1192101133000L));
            // Add the fourth document.  text = "Document 4"  dateTime = Oct 11 08:02:09 EDT 2007
            writer.AddDocument(CreateDocument("Document 4", 1192104129000L));
            // latest doc:
            // Add the fifth document.  text = "Document 5"  dateTime = Oct 12 13:25:43 EDT 2007
            writer.AddDocument(CreateDocument("Document 5", 1192209943000L));

            reader = writer.GetReader();
            writer.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            directory.Dispose();
            base.TearDown();
        }

        [Test]
        public virtual void TestReverseDateSort()
        {
            IndexSearcher searcher = NewSearcher(reader);

            Sort sort = new Sort(new SortField(DATE_TIME_FIELD, SortFieldType.STRING, true));
            Query query = new TermQuery(new Term(TEXT_FIELD, "document"));

            // Execute the search and process the search results.
            string[] actualOrder = new string[5];
            ScoreDoc[] hits = searcher.Search(query, null, 1000, sort).ScoreDocs;
            for (int i = 0; i < hits.Length; i++)
            {
                Document document = searcher.Doc(hits[i].Doc);
                string text = document.Get(TEXT_FIELD);
                actualOrder[i] = text;
            }

            // Set up the expected order (i.e. Document 5, 4, 3, 2, 1).
            string[] expectedOrder = new string[5];
            expectedOrder[0] = "Document 5";
            expectedOrder[1] = "Document 4";
            expectedOrder[2] = "Document 3";
            expectedOrder[3] = "Document 2";
            expectedOrder[4] = "Document 1";

            assertEquals(expectedOrder, actualOrder);
        }

        private Document CreateDocument(string text, long time)
        {
            Document document = new Document();

            // Add the text field.
            Field textField = NewTextField(TEXT_FIELD, text, Field.Store.YES);
            document.Add(textField);

            // Add the date/time field.
            string dateTimeString = DateTools.TimeToString(time, DateResolution.SECOND);
            Field dateTimeField = NewStringField(DATE_TIME_FIELD, dateTimeString, Field.Store.YES);
            document.Add(dateTimeField);

            return document;
        }
    }
}