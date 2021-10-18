using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using Occur = Lucene.Net.Search.Occur;

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

    /// <summary>
    /// JUnit adaptation of an older test case SearchTest. </summary>
    public class TestSearch_ : LuceneTestCase
    {
        [Test]
        public virtual void TestNegativeQueryBoost()
        {
            Query q = new TermQuery(new Term("foo", "bar"));
            q.Boost = -42f;
            Assert.AreEqual(-42f, q.Boost, 0.0f);

            Store.Directory directory = NewDirectory();
            try
            {
                Analyzer analyzer = new MockAnalyzer(Random);
                IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);

                IndexWriter writer = new IndexWriter(directory, conf);
                try
                {
                    Documents.Document d = new Documents.Document();
                    d.Add(NewTextField("foo", "bar", Field.Store.YES));
                    writer.AddDocument(d);
                }
                finally
                {
                    writer.Dispose();
                }

                IndexReader reader = DirectoryReader.Open(directory);
                try
                {
                    IndexSearcher searcher = NewSearcher(reader);

                    ScoreDoc[] hits = searcher.Search(q, null, 1000).ScoreDocs;
                    Assert.AreEqual(1, hits.Length);
                    Assert.IsTrue(hits[0].Score < 0, "score is not negative: " + hits[0].Score);

                    Explanation explain = searcher.Explain(q, hits[0].Doc);
                    Assert.AreEqual(hits[0].Score, explain.Value, 0.001f, "score doesn't match explanation");
                    Assert.IsTrue(explain.IsMatch, "explain doesn't think doc is a match");

                }
                finally
                {
                    reader.Dispose();
                }
            }
            finally
            {
                directory.Dispose();
            }

        }

        /// <summary>
        /// this test performs a number of searches. It also compares output
        ///  of searches using multi-file index segments with single-file
        ///  index segments.
        /// 
        ///  TODO: someone should check that the results of the searches are
        ///        still correct by adding assert statements. Right now, the test
        ///        passes if the results are the same between multi-file and
        ///        single-file formats, even if the results are wrong.
        /// </summary>
        [Test]
        public virtual void TestSearch()
        {
            StringWriter sw;
            string multiFileOutput;
            string singleFileOutput;
            using (sw = new StringWriter())
            {
                DoTestSearch(Random, sw, false);
                multiFileOutput = sw.ToString();
            }

            //System.out.println(multiFileOutput);

            using (sw = new StringWriter())
            {
                DoTestSearch(Random, sw, true);
                singleFileOutput = sw.ToString();
            }

            Assert.AreEqual(multiFileOutput, singleFileOutput);
        }


        private void DoTestSearch(Random random, StringWriter @out, bool useCompoundFile)
        {
            Store.Directory directory = NewDirectory();
            Analyzer analyzer = new MockAnalyzer(random);
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
            MergePolicy mp = conf.MergePolicy;
            mp.NoCFSRatio = useCompoundFile ? 1.0 : 0.0;
            IndexWriter writer = new IndexWriter(directory, conf);

            string[] docs = new string[] { "a b c d e", "a b c d e a b c d e", "a b c d e f g h i j", "a c e", "e c a", "a c e a c e", "a c e a b c" };
            for (int j = 0; j < docs.Length; j++)
            {
                Documents.Document d = new Documents.Document();
                d.Add(NewTextField("contents", docs[j], Field.Store.YES));
                d.Add(NewStringField("id", "" + j, Field.Store.NO));
                writer.AddDocument(d);
            }
            writer.Dispose();

            IndexReader reader = DirectoryReader.Open(directory);
            IndexSearcher searcher = NewSearcher(reader);

            ScoreDoc[] hits = null;

            Sort sort = new Sort(SortField.FIELD_SCORE, new SortField("id", SortFieldType.INT32));

            foreach (Query query in BuildQueries())
            {
                @out.WriteLine("Query: " + query.ToString("contents"));
                if (Verbose)
                {
                    Console.WriteLine("TEST: query=" + query);
                }

                hits = searcher.Search(query, null, 1000, sort).ScoreDocs;

                @out.WriteLine(hits.Length + " total results");
                for (int i = 0; i < hits.Length && i < 10; i++)
                {
                    Documents.Document d = searcher.Doc(hits[i].Doc);
                    @out.WriteLine(i + " " + hits[i].Score + " " + d.Get("contents"));
                }
            }
            reader.Dispose();
            directory.Dispose();
        }

        private IList<Query> BuildQueries()
        {
            IList<Query> queries = new JCG.List<Query>();

            BooleanQuery booleanAB = new BooleanQuery();
            booleanAB.Add(new TermQuery(new Term("contents", "a")), Occur.SHOULD);
            booleanAB.Add(new TermQuery(new Term("contents", "b")), Occur.SHOULD);
            queries.Add(booleanAB);

            PhraseQuery phraseAB = new PhraseQuery();
            phraseAB.Add(new Term("contents", "a"));
            phraseAB.Add(new Term("contents", "b"));
            queries.Add(phraseAB);

            PhraseQuery phraseABC = new PhraseQuery();
            phraseABC.Add(new Term("contents", "a"));
            phraseABC.Add(new Term("contents", "b"));
            phraseABC.Add(new Term("contents", "c"));
            queries.Add(phraseABC);

            BooleanQuery booleanAC = new BooleanQuery();
            booleanAC.Add(new TermQuery(new Term("contents", "a")), Occur.SHOULD);
            booleanAC.Add(new TermQuery(new Term("contents", "c")), Occur.SHOULD);
            queries.Add(booleanAC);

            PhraseQuery phraseAC = new PhraseQuery();
            phraseAC.Add(new Term("contents", "a"));
            phraseAC.Add(new Term("contents", "c"));
            queries.Add(phraseAC);

            PhraseQuery phraseACE = new PhraseQuery();
            phraseACE.Add(new Term("contents", "a"));
            phraseACE.Add(new Term("contents", "c"));
            phraseACE.Add(new Term("contents", "e"));
            queries.Add(phraseACE);

            return queries;
        }
    }
}