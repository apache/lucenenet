// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.Core
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

    public class TestKeywordAnalyzer : BaseTokenStreamTestCase
    {

        private Store.Directory directory;
        private IndexSearcher searcher;
        private IndexReader reader;

        public override void SetUp()
        {
            base.SetUp();
            directory = NewDirectory();
            IndexWriter writer = new IndexWriter(directory, new IndexWriterConfig(TEST_VERSION_CURRENT, new SimpleAnalyzer(TEST_VERSION_CURRENT)));

            Document doc = new Document();
            doc.Add(new StringField("partnum", "Q36", Field.Store.YES));
            doc.Add(new TextField("description", "Illidium Space Modulator", Field.Store.YES));
            writer.AddDocument(doc);

            writer.Dispose();

            reader = DirectoryReader.Open(directory);
            searcher = NewSearcher(reader);
        }

        public override void TearDown()
        {
            reader.Dispose();
            directory.Dispose();
            base.TearDown();
        }

        /*
        public void testPerFieldAnalyzer() throws Exception {
          PerFieldAnalyzerWrapper analyzer = new PerFieldAnalyzerWrapper(new SimpleAnalyzer(TEST_VERSION_CURRENT));
          analyzer.addAnalyzer("partnum", new KeywordAnalyzer());

          QueryParser queryParser = new QueryParser(TEST_VERSION_CURRENT, "description", analyzer);
          Query query = queryParser.parse("partnum:Q36 AND SPACE");

          ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
          assertEquals("Q36 kept as-is",
                    "+partnum:Q36 +space", query.toString("description"));
          assertEquals("doc found!", 1, hits.length);
        }
        */

        [Test]
        public virtual void TestMutipleDocument()
        {
            RAMDirectory dir = new RAMDirectory();
            IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new KeywordAnalyzer()));
            Document doc = new Document();
            doc.Add(new TextField("partnum", "Q36", Field.Store.YES));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new TextField("partnum", "Q37", Field.Store.YES));
            writer.AddDocument(doc);
            writer.Dispose();

            IndexReader reader = DirectoryReader.Open(dir);
            DocsEnum td = TestUtil.Docs(Random, reader, "partnum", new BytesRef("Q36"), MultiFields.GetLiveDocs(reader), null, 0);
            assertTrue(td.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
            td = TestUtil.Docs(Random, reader, "partnum", new BytesRef("Q37"), MultiFields.GetLiveDocs(reader), null, 0);
            assertTrue(td.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
        }

        // LUCENE-1441
        [Test]
        public virtual void TestOffsets()
        {
            TokenStream stream = (new KeywordAnalyzer()).GetTokenStream("field", new StringReader("abcd"));
            try
            {
                IOffsetAttribute offsetAtt = stream.AddAttribute<IOffsetAttribute>();
                stream.Reset();
                assertTrue(stream.IncrementToken());
                assertEquals(0, offsetAtt.StartOffset);
                assertEquals(4, offsetAtt.EndOffset);
                assertFalse(stream.IncrementToken());
                stream.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(stream);
            }
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new KeywordAnalyzer(), 1000 * RandomMultiplier);
        }
    }
}