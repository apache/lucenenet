using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net
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

    public class TestSearchForDuplicates : LuceneTestCase
    {

        internal const string PRIORITY_FIELD = "priority";
        internal const string ID_FIELD = "id";
        internal const string HIGH_PRIORITY = "high";
        internal const string MED_PRIORITY = "medium";
        internal const string LOW_PRIORITY = "low";


        /// <summary>
        /// this test compares search results when using and not using compound
        ///  files.
        /// 
        ///  TODO: There is rudimentary search result validation as well, but it is
        ///        simply based on asserting the output observed in the old test case,
        ///        without really knowing if the output is correct. Someone needs to
        ///        validate this output and make any changes to the checkHits method.
        /// </summary>
        [Test]
        public void TestRun()
        {
            StringWriter sw;
            string multiFileOutput;
            string singleFileOutput;
            int MAX_DOCS = AtLeast(225);

            using (sw = new StringWriter())
            {
                DoTest(Random, sw, false, MAX_DOCS);
                multiFileOutput = sw.ToString();
            }

            //System.out.println(multiFileOutput);

            using (sw = new StringWriter())
            {
                DoTest(Random, sw, true, MAX_DOCS);
                singleFileOutput = sw.ToString();
            }

            Assert.AreEqual(multiFileOutput, singleFileOutput);
        }


        private void DoTest(Random random, TextWriter @out, bool useCompoundFiles, int MAX_DOCS)
        {
            Store.Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(random);
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            MergePolicy mp = conf.MergePolicy;
            mp.NoCFSRatio = useCompoundFiles ? 1.0 : 0.0;
            IndexWriter writer = new IndexWriter(directory, conf);
            if (Verbose)
            {
                Console.WriteLine("TEST: now build index MAX_DOCS=" + MAX_DOCS);
            }

            for (int j = 0; j < MAX_DOCS; j++)
            {
                Documents.Document d = new Documents.Document();
                d.Add(NewTextField(PRIORITY_FIELD, HIGH_PRIORITY, Field.Store.YES));
                d.Add(NewTextField(ID_FIELD, Convert.ToString(j), Field.Store.YES));
                writer.AddDocument(d);
            }
            writer.Dispose();

            // try a search without OR
            IndexReader reader = DirectoryReader.Open(directory);
            IndexSearcher searcher = NewSearcher(reader);

            Query query = new TermQuery(new Term(PRIORITY_FIELD, HIGH_PRIORITY));
            @out.WriteLine("Query: " + query.ToString(PRIORITY_FIELD));
            if (Verbose)
            {
                Console.WriteLine("TEST: search query=" + query);
            }

            Sort sort = new Sort(SortField.FIELD_SCORE, new SortField(ID_FIELD, SortFieldType.INT32));

            ScoreDoc[] hits = searcher.Search(query, null, MAX_DOCS, sort).ScoreDocs;
            PrintHits(@out, hits, searcher);
            CheckHits(hits, MAX_DOCS, searcher);

            // try a new search with OR
            searcher = NewSearcher(reader);
            hits = null;

            BooleanQuery booleanQuery = new BooleanQuery();
            booleanQuery.Add(new TermQuery(new Term(PRIORITY_FIELD, HIGH_PRIORITY)), Occur.SHOULD);
            booleanQuery.Add(new TermQuery(new Term(PRIORITY_FIELD, MED_PRIORITY)), Occur.SHOULD);
            @out.WriteLine("Query: " + booleanQuery.ToString(PRIORITY_FIELD));

            hits = searcher.Search(booleanQuery, null, MAX_DOCS, sort).ScoreDocs;
            PrintHits(@out, hits, searcher);
            CheckHits(hits, MAX_DOCS, searcher);

            reader.Dispose();
            directory.Dispose();
        }


        private void PrintHits(TextWriter @out, ScoreDoc[] hits, IndexSearcher searcher)
        {
            @out.WriteLine(hits.Length + " total results\n");
            for (int i = 0; i < hits.Length; i++)
            {
                if (i < 10 || (i > 94 && i < 105))
                {
                    Documents.Document d = searcher.Doc(hits[i].Doc);
                    @out.WriteLine(i + " " + d.Get(ID_FIELD));
                }
            }
        }

        private void CheckHits(ScoreDoc[] hits, int expectedCount, IndexSearcher searcher)
        {
            Assert.AreEqual(expectedCount, hits.Length, "total results");
            for (int i = 0; i < hits.Length; i++)
            {
                if (i < 10 || (i > 94 && i < 105))
                {
                    Documents.Document d = searcher.Doc(hits[i].Doc);
                    Assert.AreEqual(Convert.ToString(i), d.Get(ID_FIELD), "check " + i);
                }
            }
        }
    }
}