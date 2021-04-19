using Lucene.Net.Documents;
using NUnit.Framework;
using RandomizedTesting.Generators;
using Assert = Lucene.Net.TestFramework.Assert;

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

    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    ///
    [TestFixture]
    public class TestFieldValueFilter : LuceneTestCase
    {
        [Test]
        public virtual void TestFieldValueFilterNoValue()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            int docs = AtLeast(10);
            int[] docStates = BuildIndex(writer, docs);
            int numDocsNoValue = 0;
            for (int i = 0; i < docStates.Length; i++)
            {
                if (docStates[i] == 0)
                {
                    numDocsNoValue++;
                }
            }

            IndexReader reader = DirectoryReader.Open(directory);
            IndexSearcher searcher = NewSearcher(reader);
            TopDocs search = searcher.Search(new TermQuery(new Term("all", "test")), new FieldValueFilter("some", true), docs);
            Assert.AreEqual(search.TotalHits, numDocsNoValue);

            ScoreDoc[] scoreDocs = search.ScoreDocs;
            foreach (ScoreDoc scoreDoc in scoreDocs)
            {
                Assert.IsNull(reader.Document(scoreDoc.Doc).Get("some"));
            }

            reader.Dispose();
            directory.Dispose();
        }

        [Test]
        public virtual void TestFieldValueFilter_Mem()
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            int docs = AtLeast(10);
            int[] docStates = BuildIndex(writer, docs);
            int numDocsWithValue = 0;
            for (int i = 0; i < docStates.Length; i++)
            {
                if (docStates[i] == 1)
                {
                    numDocsWithValue++;
                }
            }
            IndexReader reader = DirectoryReader.Open(directory);
            IndexSearcher searcher = NewSearcher(reader);
            TopDocs search = searcher.Search(new TermQuery(new Term("all", "test")), new FieldValueFilter("some"), docs);
            Assert.AreEqual(search.TotalHits, numDocsWithValue);

            ScoreDoc[] scoreDocs = search.ScoreDocs;
            foreach (ScoreDoc scoreDoc in scoreDocs)
            {
                Assert.AreEqual("value", reader.Document(scoreDoc.Doc).Get("some"));
            }

            reader.Dispose();
            directory.Dispose();
        }

        private int[] BuildIndex(RandomIndexWriter writer, int docs)
        {
            int[] docStates = new int[docs];
            for (int i = 0; i < docs; i++)
            {
                Document doc = new Document();
                if (Random.NextBoolean())
                {
                    docStates[i] = 1;
                    doc.Add(NewTextField("some", "value", Field.Store.YES));
                }
                doc.Add(NewTextField("all", "test", Field.Store.NO));
                doc.Add(NewTextField("id", "" + i, Field.Store.YES));
                writer.AddDocument(doc);
            }
            writer.Commit();
            int numDeletes = Random.Next(docs);
            for (int i = 0; i < numDeletes; i++)
            {
                int docID = Random.Next(docs);
                writer.DeleteDocuments(new Term("id", "" + docID));
                docStates[docID] = 2;
            }
            writer.Dispose();
            return docStates;
        }
    }
}