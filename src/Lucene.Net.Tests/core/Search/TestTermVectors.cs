using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using NUnit.Framework;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using Document = Documents.Document;
    using English = Lucene.Net.Util.English;
    using Field = Field;
    using Fields = Lucene.Net.Index.Fields;
    using FieldType = FieldType;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IOUtils = Lucene.Net.Util.IOUtils;
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
    using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TextField = TextField;

    public class TestTermVectors : LuceneTestCase
    {
        private static IndexReader Reader;
        private static Directory Directory;

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewIndexWriterConfig is no longer static.
        /// </summary>
        [OneTimeSetUp]
        public void BeforeClass()
        {
            Directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true)).SetMergePolicy(NewLogMergePolicy()));
            //writer.setNoCFSRatio(1.0);
            //writer.infoStream = System.out;
            for (int i = 0; i < 1000; i++)
            {
                Document doc = new Document();
                FieldType ft = new FieldType(TextField.TYPE_STORED);
                int mod3 = i % 3;
                int mod2 = i % 2;
                if (mod2 == 0 && mod3 == 0)
                {
                    ft.StoreTermVectors = true;
                    ft.StoreTermVectorOffsets = true;
                    ft.StoreTermVectorPositions = true;
                }
                else if (mod2 == 0)
                {
                    ft.StoreTermVectors = true;
                    ft.StoreTermVectorPositions = true;
                }
                else if (mod3 == 0)
                {
                    ft.StoreTermVectors = true;
                    ft.StoreTermVectorOffsets = true;
                }
                else
                {
                    ft.StoreTermVectors = true;
                }
                doc.Add(new Field("field", English.IntToEnglish(i), ft));
                //test no term vectors too
                doc.Add(new TextField("noTV", English.IntToEnglish(i), Field.Store.YES));
                writer.AddDocument(doc);
            }
            Reader = writer.Reader;
            writer.Dispose();
        }

        [OneTimeTearDown]
        public static void AfterClass()
        {
            Reader.Dispose();
            Directory.Dispose();
            Reader = null;
            Directory = null;
        }

        // In a single doc, for the same field, mix the term
        // vectors up
        [Test]
        public virtual void TestMixedVectrosVectors()
        {
            RandomIndexWriter writer = new RandomIndexWriter(Random(), Directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true)).SetOpenMode(OpenMode.CREATE));
            Document doc = new Document();

            FieldType ft2 = new FieldType(TextField.TYPE_STORED);
            ft2.StoreTermVectors = true;

            FieldType ft3 = new FieldType(TextField.TYPE_STORED);
            ft3.StoreTermVectors = true;
            ft3.StoreTermVectorPositions = true;

            FieldType ft4 = new FieldType(TextField.TYPE_STORED);
            ft4.StoreTermVectors = true;
            ft4.StoreTermVectorOffsets = true;

            FieldType ft5 = new FieldType(TextField.TYPE_STORED);
            ft5.StoreTermVectors = true;
            ft5.StoreTermVectorOffsets = true;
            ft5.StoreTermVectorPositions = true;

            doc.Add(NewTextField("field", "one", Field.Store.YES));
            doc.Add(NewField("field", "one", ft2));
            doc.Add(NewField("field", "one", ft3));
            doc.Add(NewField("field", "one", ft4));
            doc.Add(NewField("field", "one", ft5));
            writer.AddDocument(doc);
            IndexReader reader = writer.Reader;
            writer.Dispose();

            IndexSearcher searcher = NewSearcher(reader);

            Query query = new TermQuery(new Term("field", "one"));
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length);

            Fields vectors = searcher.IndexReader.GetTermVectors(hits[0].Doc);
            Assert.IsNotNull(vectors);
            Assert.AreEqual(1, vectors.Size);
            Terms vector = vectors.Terms("field");
            Assert.IsNotNull(vector);
            Assert.AreEqual(1, vector.Size());
            TermsEnum termsEnum = vector.Iterator(null);
            Assert.IsNotNull(termsEnum.Next());
            Assert.AreEqual("one", termsEnum.Term().Utf8ToString());
            Assert.AreEqual(5, termsEnum.TotalTermFreq());
            DocsAndPositionsEnum dpEnum = termsEnum.DocsAndPositions(null, null);
            Assert.IsNotNull(dpEnum);
            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(5, dpEnum.Freq());
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(i, dpEnum.NextPosition());
            }

            dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
            Assert.IsNotNull(dpEnum);
            Assert.IsTrue(dpEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            Assert.AreEqual(5, dpEnum.Freq());
            for (int i = 0; i < 5; i++)
            {
                dpEnum.NextPosition();
                Assert.AreEqual(4 * i, dpEnum.StartOffset());
                Assert.AreEqual(4 * i + 3, dpEnum.EndOffset());
            }
            reader.Dispose();
        }

        private IndexWriter CreateWriter(Directory dir)
        {
            return new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMaxBufferedDocs(2));
        }

        private void CreateDir(Directory dir)
        {
            IndexWriter writer = CreateWriter(dir);
            writer.AddDocument(CreateDoc());
            writer.Dispose();
        }

        private Document CreateDoc()
        {
            Document doc = new Document();
            FieldType ft = new FieldType(TextField.TYPE_STORED);
            ft.StoreTermVectors = true;
            ft.StoreTermVectorOffsets = true;
            ft.StoreTermVectorPositions = true;
            doc.Add(NewField("c", "aaa", ft));
            return doc;
        }

        private void VerifyIndex(Directory dir)
        {
            IndexReader r = DirectoryReader.Open(dir);
            int numDocs = r.NumDocs;
            for (int i = 0; i < numDocs; i++)
            {
                Assert.IsNotNull(r.GetTermVectors(i).Terms("c"), "term vectors should not have been null for document " + i);
            }
            r.Dispose();
        }

        [Test]
        public virtual void TestFullMergeAddDocs()
        {
            Directory target = NewDirectory();
            IndexWriter writer = CreateWriter(target);
            // with maxBufferedDocs=2, this results in two segments, so that forceMerge
            // actually does something.
            for (int i = 0; i < 4; i++)
            {
                writer.AddDocument(CreateDoc());
            }
            writer.ForceMerge(1);
            writer.Dispose();

            VerifyIndex(target);
            target.Dispose();
        }

        [Test]
        public virtual void TestFullMergeAddIndexesDir()
        {
            Directory[] input = new Directory[] { NewDirectory(), NewDirectory() };
            Directory target = NewDirectory();

            foreach (Directory dir in input)
            {
                CreateDir(dir);
            }

            IndexWriter writer = CreateWriter(target);
            writer.AddIndexes(input);
            writer.ForceMerge(1);
            writer.Dispose();

            VerifyIndex(target);

            IOUtils.Close(target, input[0], input[1]);
        }

        [Test]
        public virtual void TestFullMergeAddIndexesReader()
        {
            Directory[] input = new Directory[] { NewDirectory(), NewDirectory() };
            Directory target = NewDirectory();

            foreach (Directory dir in input)
            {
                CreateDir(dir);
            }

            IndexWriter writer = CreateWriter(target);
            foreach (Directory dir in input)
            {
                IndexReader r = DirectoryReader.Open(dir);
                writer.AddIndexes(r);
                r.Dispose();
            }
            writer.ForceMerge(1);
            writer.Dispose();

            VerifyIndex(target);
            IOUtils.Close(target, input[0], input[1]);
        }
    }
}