using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Sandbox.Queries
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

    public class DuplicateFilterTest : LuceneTestCase
    {
        private static readonly string KEY_FIELD = "url";
        private Directory directory;
        private IndexReader reader;
        TermQuery tq = new TermQuery(new Term("text", "lucene"));
        private IndexSearcher searcher;


        public override void SetUp()
        {
            base.SetUp();
            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));

            //Add series of docs with filterable fields : url, text and dates  flags
            AddDoc(writer, "http://lucene.apache.org", "lucene 1.4.3 available", "20040101");
            AddDoc(writer, "http://lucene.apache.org", "New release pending", "20040102");
            AddDoc(writer, "http://lucene.apache.org", "Lucene 1.9 out now", "20050101");
            AddDoc(writer, "http://www.bar.com", "Local man bites dog", "20040101");
            AddDoc(writer, "http://www.bar.com", "Dog bites local man", "20040102");
            AddDoc(writer, "http://www.bar.com", "Dog uses Lucene", "20050101");
            AddDoc(writer, "http://lucene.apache.org", "Lucene 2.0 out", "20050101");
            AddDoc(writer, "http://lucene.apache.org", "Oops. Lucene 2.1 out", "20050102");

            // Until we fix LUCENE-2348, the index must
            // have only 1 segment:
            writer.ForceMerge(1);

            reader = writer.GetReader();
            writer.Dispose();
            searcher = NewSearcher(reader);

        }

        public override void TearDown()
        {
            reader.Dispose();
            directory.Dispose();
            base.TearDown();
        }

        private void AddDoc(RandomIndexWriter writer, string url, string text, string date)
        {
            Document doc = new Document();
            doc.Add(NewStringField(KEY_FIELD, url, Field.Store.YES));
            doc.Add(NewTextField("text", text, Field.Store.YES));
            doc.Add(NewTextField("date", date, Field.Store.YES));
            writer.AddDocument(doc);
        }

        [Test]
        public void TestDefaultFilter()
        {
            DuplicateFilter df = new DuplicateFilter(KEY_FIELD);
            ISet<string> results = new JCG.HashSet<string>();
            ScoreDoc[] hits = searcher.Search(tq, df, 1000).ScoreDocs;

            foreach (ScoreDoc hit in hits)
            {
                Document d = searcher.Doc(hit.Doc);
                string url = d.Get(KEY_FIELD);
                assertFalse("No duplicate urls should be returned", results.contains(url));
                results.add(url);
            }
        }
        [Test]
        public void TestNoFilter()
        {
            ISet<string> results = new JCG.HashSet<string>();
            ScoreDoc[] hits = searcher.Search(tq, null, 1000).ScoreDocs;
            assertTrue("Default searching should have found some matches", hits.Length > 0);
            bool dupsFound = false;

            foreach (ScoreDoc hit in hits)
            {
                Document d = searcher.Doc(hit.Doc);
                string url = d.Get(KEY_FIELD);
                if (!dupsFound)
                    dupsFound = results.contains(url);
                results.add(url);
            }
            assertTrue("Default searching should have found duplicate urls", dupsFound);
        }

        [Test]
        public void TestFastFilter()
        {
            DuplicateFilter df = new DuplicateFilter(KEY_FIELD);
            df.ProcessingMode = (ProcessingMode.PM_FAST_INVALIDATION);
            ISet<string> results = new JCG.HashSet<string>();
            ScoreDoc[] hits = searcher.Search(tq, df, 1000).ScoreDocs;
            assertTrue("Filtered searching should have found some matches", hits.Length > 0);

            foreach (ScoreDoc hit in hits)
            {
                Document d = searcher.Doc(hit.Doc);
                string url = d.Get(KEY_FIELD);
                assertFalse("No duplicate urls should be returned", results.contains(url));
                results.add(url);
            }
            assertEquals("Two urls found", 2, results.size());
        }

        [Test]
        public void TestKeepsLastFilter()
        {
            DuplicateFilter df = new DuplicateFilter(KEY_FIELD);
            df.KeepMode = (KeepMode.KM_USE_LAST_OCCURRENCE);
            ScoreDoc[] hits = searcher.Search(tq, df, 1000).ScoreDocs;
            assertTrue("Filtered searching should have found some matches", hits.Length > 0);
            foreach (ScoreDoc hit in hits)
            {
                Document d = searcher.Doc(hit.Doc);
                string url = d.Get(KEY_FIELD);
                DocsEnum td = TestUtil.Docs(Random, reader,
                    KEY_FIELD,
                    new BytesRef(url),
                    MultiFields.GetLiveDocs(reader),
                    null,
                    0);

                int lastDoc = 0;
                while (td.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                {
                    lastDoc = td.DocID;
                }
                assertEquals("Duplicate urls should return last doc", lastDoc, hit.Doc);
            }
        }

        [Test]
        public void TestKeepsFirstFilter()
        {
            DuplicateFilter df = new DuplicateFilter(KEY_FIELD);
            df.KeepMode = (KeepMode.KM_USE_FIRST_OCCURRENCE);
            ScoreDoc[] hits = searcher.Search(tq, df, 1000).ScoreDocs;
            assertTrue("Filtered searching should have found some matches", hits.Length > 0);
            foreach (ScoreDoc hit in hits)
            {
                Document d = searcher.Doc(hit.Doc);
                string url = d.Get(KEY_FIELD);
                DocsEnum td = TestUtil.Docs(Random, reader,
                    KEY_FIELD,
                    new BytesRef(url),
                    MultiFields.GetLiveDocs(reader),
                    null,
                    0);

                int lastDoc = 0;
                td.NextDoc();
                lastDoc = td.DocID;
                assertEquals("Duplicate urls should return first doc", lastDoc, hit.Doc);
            }
        }
    }
}
