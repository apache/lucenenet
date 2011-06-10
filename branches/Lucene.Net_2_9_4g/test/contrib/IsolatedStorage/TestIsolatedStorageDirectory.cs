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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Store;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Util;

using NUnit.Framework;

namespace Lucene.Net.Store
{
    [TestFixture]
    public class TestIsolatedStorageDirectory
    {
        string IndexDir = "TestIsolatedStorage";

        [TearDown]
        public void TearDown()
        {
            //Adding this would be good.
            //IsolatedStorageDirectory.Remove(IndexDir);
        }

        [Test]
        public void TestDirectoryFilter()
        {
            using (IsolatedStorageDirectory dir = new IsolatedStorageDirectory(IndexDir))
            {
                System.String name = "file";

                dir.CreateOutput(name).Close();
                Assert.IsTrue(dir.FileExists(name));
                Assert.IsTrue(new System.Collections.ArrayList(dir.ListAll()).Contains(name));
                dir.DeleteFile(name);
                Assert.IsFalse(dir.FileExists(name));
            }
        }

        [Test]
        public void TestDetectClose()
        {
            Directory dir = new IsolatedStorageDirectory(IndexDir);
            dir.Close();
            try
            {
                dir.CreateOutput("test");
                Assert.Fail("did not hit expected exception");
            }
            catch (AlreadyClosedException ace)
            {
            }
        }

        [Test]
        public void TestLock()
        {
            using (var dir = new IsolatedStorageDirectory(IndexDir))
            {
                IndexWriter writer1 = new IndexWriter(dir, new StandardAnalyzer());
                try
                {
                    IndexWriter writer2 = new IndexWriter(dir, new StandardAnalyzer());
                    Assert.Fail("Unexpected lock error");
                }
                catch
                {
                }
                finally
                {
                    writer1.Close();
                }
            }
        }

        [Test]
        public void TestIndexAndSearch()
        {
            GenerateRandomDocs(10, 5000);

            using (var dir = new IsolatedStorageDirectory(IndexDir))
            {
                Random rgen = new Random();
                using (IndexReader reader = IndexReader.Open(dir, true))
                {
                    IndexSearcher sfs = new IndexSearcher(reader);
                    QueryParser parser = new MultiFieldQueryParser(new string[] { "category", "title", "body" }, new StandardAnalyzer());

                    for (int i = 0; i < 100; i++)
                    {
                        Query q = parser.Parse("some stuff " + RandomField(3, rgen));
                        TopDocs hits = sfs.Search(q, 200);
                        for (int j = 0; j < hits.ScoreDocs.Length; j++)
                        {
                            Document doc = reader.Document(hits.ScoreDocs[j].doc);
                            Fieldable f = doc.GetField("title");
                        }
                    }
                }
            }
        }


        private void GenerateRandomDocs(int numCats, int numDocs)
        {
            using (var dir = new IsolatedStorageDirectory(IndexDir))
            {
                Random rgen = new Random();
                string[] categories = Enumerable.Range(0, numCats).Select(x => RandomString(4, rgen)).ToArray();
                IEnumerable<Document> docs = Enumerable.Range(0, numDocs).Select(x => RandomDocument(categories[rgen.Next(0, numCats - 1)], rgen));

                using (IndexWriter writer = new IndexWriter(dir, new StandardAnalyzer(), true))
                {
                    System.Threading.Tasks.Parallel.ForEach(docs, d =>
                    {
                        writer.AddDocument(d); //multi-access to writer
                    });

                    Assert.AreEqual(docs.Count(), writer.MaxDoc(), "Unexpected error in \"writer.AddDocument\"");
                }
            }
        }

        private Document RandomDocument(string category, Random rgen)
        {
            string usefulWords = "some stuff to search for ";
            Document doc = new Document();
            doc.Add(new Field("category", category, Field.Store.NO, Field.Index.ANALYZED));
            doc.Add(new Field("title", RandomField(20, rgen), Field.Store.NO, Field.Index.ANALYZED));
            doc.Add(new Field("body", usefulWords + RandomField(100, rgen), Field.Store.NO, Field.Index.ANALYZED));
            return doc;
        }

        private string RandomField(int words, Random rgen)
        {
            return string.Join(" ", Enumerable.Range(0, words).Select(x => RandomString(5, rgen)));
        }

        private string RandomString(int length, Random rgen)
        {
            return new string(Enumerable.Range(0, length).Select(x => (char)rgen.Next(65, 81)).ToArray());
        }
    }
}
