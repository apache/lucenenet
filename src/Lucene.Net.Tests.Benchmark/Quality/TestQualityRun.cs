using Lucene.Net.Benchmarks.Quality.Trec;
using Lucene.Net.Benchmarks.Quality.Utils;
using Lucene.Net.Index;
using Lucene.Net.Search;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Benchmarks.Quality
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
    /// Test that quality run does its job.
    /// <para/>
    /// NOTE: if the default scoring or StandardAnalyzer is changed, then
    /// this test will not work correctly, as it does not dynamically
    /// generate its test trec topics/qrels!
    /// </summary>
    public class TestQualityRun : BenchmarkTestCase
    {
        public override void SetUp()
        {
            base.SetUp();
            copyToWorkDir("reuters.578.lines.txt.bz2");
        }

        [Test]
        public void TestTrecQuality()
        {
            // first create the partial reuters index
            createReutersIndex();


            int maxResults = 1000;
            String docNameField = "doctitle"; // orig docID is in the linedoc format title 

            TextWriter logger = Verbose ? Console.Out : null;

            // prepare topics
            Stream topics = GetType().getResourceAsStream("trecTopics.txt");
            TrecTopicsReader qReader = new TrecTopicsReader();
            QualityQuery[] qqs = qReader.ReadQueries(new StreamReader(topics, Encoding.UTF8));

            // prepare judge
            Stream qrels = GetType().getResourceAsStream("trecQRels.txt");
            IJudge judge = new TrecJudge(new StreamReader(qrels, Encoding.UTF8));

            // validate topics & judgments match each other
            judge.ValidateData(qqs, logger);

            Store.Directory dir = NewFSDirectory(new DirectoryInfo(System.IO.Path.Combine(getWorkDir().FullName, "index")));
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = new IndexSearcher(reader);

            IQualityQueryParser qqParser = new SimpleQQParser("title", "body");
            QualityBenchmark qrun = new QualityBenchmark(qqs, qqParser, searcher, docNameField);

            SubmissionReport submitLog = Verbose ? new SubmissionReport(logger, "TestRun") : null;
            qrun.MaxResults = (maxResults);
            QualityStats[] stats = qrun.Execute(judge, submitLog, logger);

            // --------- verify by the way judgments were altered for this test:
            // for some queries, depending on m = qnum % 8
            // m==0: avg_precision and recall are hurt, by marking fake docs as relevant
            // m==1: precision_at_n and avg_precision are hurt, by unmarking relevant docs
            // m==2: all precision, precision_at_n and recall are hurt.
            // m>=3: these queries remain perfect
            for (int i = 0; i < stats.Length; i++)
            {
                QualityStats s = stats[i];
                switch (i % 8)
                {

                    case 0:
                        assertTrue("avg-p should be hurt: " + s.GetAvp(), 1.0 > s.GetAvp());
                        assertTrue("recall should be hurt: " + s.Recall, 1.0 > s.Recall);
                        for (int j = 1; j <= QualityStats.MAX_POINTS; j++)
                        {
                            assertEquals("p_at_" + j + " should be perfect: " + s.GetPrecisionAt(j), 1.0, s.GetPrecisionAt(j), 1E-2);
                        }
                        break;

                    case 1:
                        assertTrue("avg-p should be hurt", 1.0 > s.GetAvp());
                        assertEquals("recall should be perfect: " + s.Recall, 1.0, s.Recall, 1E-2);
                        for (int j = 1; j <= QualityStats.MAX_POINTS; j++)
                        {
                            assertTrue("p_at_" + j + " should be hurt: " + s.GetPrecisionAt(j), 1.0 > s.GetPrecisionAt(j));
                        }
                        break;

                    case 2:
                        assertTrue("avg-p should be hurt: " + s.GetAvp(), 1.0 > s.GetAvp());
                        assertTrue("recall should be hurt: " + s.Recall, 1.0 > s.Recall);
                        for (int j = 1; j <= QualityStats.MAX_POINTS; j++)
                        {
                            assertTrue("p_at_" + j + " should be hurt: " + s.GetPrecisionAt(j), 1.0 > s.GetPrecisionAt(j));
                        }
                        break;

                    default:
                        {
                            assertEquals("avg-p should be perfect: " + s.GetAvp(), 1.0, s.GetAvp(), 1E-2);
                            assertEquals("recall should be perfect: " + s.Recall, 1.0, s.Recall, 1E-2);
                            for (int j = 1; j <= QualityStats.MAX_POINTS; j++)
                            {
                                assertEquals("p_at_" + j + " should be perfect: " + s.GetPrecisionAt(j), 1.0, s.GetPrecisionAt(j), 1E-2);
                            }
                            break;
                        }

                }
            }

            QualityStats avg = QualityStats.Average(stats);
            if (logger != null)
            {
                avg.Log("Average statistis:", 1, logger, "  ");
            }


            assertTrue("mean avg-p should be hurt: " + avg.GetAvp(), 1.0 > avg.GetAvp());
            assertTrue("avg recall should be hurt: " + avg.Recall, 1.0 > avg.Recall);
            for (int j = 1; j <= QualityStats.MAX_POINTS; j++)
            {
                assertTrue("avg p_at_" + j + " should be hurt: " + avg.GetPrecisionAt(j), 1.0 > avg.GetPrecisionAt(j));
            }

            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestTrecTopicsReader()
        {
            // prepare topics
            Stream topicsFile = GetType().getResourceAsStream("trecTopics.txt");
            TrecTopicsReader qReader = new TrecTopicsReader();
            QualityQuery[] qqs = qReader.ReadQueries(
                new StreamReader(topicsFile, Encoding.UTF8));


            assertEquals(20, qqs.Length);

            QualityQuery qq = qqs[0];
            assertEquals("statement months  total 1987", qq.GetValue("title"));
            assertEquals("Topic 0 Description Line 1 Topic 0 Description Line 2",
                qq.GetValue("description"));
            assertEquals("Topic 0 Narrative Line 1 Topic 0 Narrative Line 2",
                qq.GetValue("narrative"));

            qq = qqs[1];
            assertEquals("agreed 15  against five", qq.GetValue("title"));
            assertEquals("Topic 1 Description Line 1 Topic 1 Description Line 2",
                qq.GetValue("description"));
            assertEquals("Topic 1 Narrative Line 1 Topic 1 Narrative Line 2",
                qq.GetValue("narrative"));

            qq = qqs[19];
            assertEquals("20 while  common week", qq.GetValue("title"));
            assertEquals("Topic 19 Description Line 1 Topic 19 Description Line 2",
                qq.GetValue("description"));
            assertEquals("Topic 19 Narrative Line 1 Topic 19 Narrative Line 2",
                qq.GetValue("narrative"));
        }

        // use benchmark logic to create the mini Reuters index
        private void createReutersIndex()
        {
            // 1. alg definition
            String[] algLines = {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "analyzer=Lucene.Net.Analysis.Standard.ClassicAnalyzer, Lucene.Net.Analysis.Common",
                "docs.file=" + getWorkDirResourcePath("reuters.578.lines.txt.bz2"),
                "content.source.log.step=2500",
                "doc.term.vector=false",
                "content.source.forever=false",
                "directory=FSDirectory",
                "doc.stored=true",
                "doc.tokenized=true",
                "# ----- alg ",
                "ResetSystemErase",
                "CreateIndex",
                "{ AddDoc } : *",
                "CloseIndex",
            };

            // 2. execute the algorithm  (required in every "logic" test)
            execBenchmark(algLines);
        }
    }
}
