using ICU4N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Benchmarks.ByTask.Feeds;
using Lucene.Net.Benchmarks.ByTask.Stats;
using Lucene.Net.Benchmarks.ByTask.Tasks;
using Lucene.Net.Collation;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Lucene.Net.Benchmarks.ByTask
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
    /// Test very simply that perf tasks - simple algorithms - are doing what they should.
    /// </summary>
    [SuppressCodecs("Lucene3x")]
    public class TestPerfTasksLogic : BenchmarkTestCase
    {
        //public override void SetUp()
        //{
        //    base.SetUp();
        //    copyToWorkDir("reuters.first20.lines.txt");
        //    copyToWorkDir("test-mapping-ISOLatin1Accent-partial.txt");
        //}

        public override void BeforeClass()
        {
            base.BeforeClass();
            copyToWorkDir("reuters.first20.lines.txt");
            copyToWorkDir("test-mapping-ISOLatin1Accent-partial.txt");
        }

        /**
         * Test index creation logic
         */
        [Test]
        public void TestIndexAndSearchTasks()
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = {
                "ResetSystemErase",
                "CreateIndex",
                "{ AddDoc } : 1000",
                "ForceMerge(1)",
                "CloseIndex",
                "OpenReader",
                "{ CountingSearchTest } : 200",
                "CloseReader",
                "[ CountingSearchTest > : 70",
                "[ CountingSearchTest > : 9",
            };

            // 2. we test this value later
            CountingSearchTestTask.numSearches = 0;

            // 3. execute the algorithm  (required in every "logic" test)
            Benchmark benchmark = execBenchmark(algLines);

            // 4. test specific checks after the benchmark run completed.
            assertEquals("TestSearchTask was supposed to be called!", 279, CountingSearchTestTask.numSearches);
            assertTrue("Index does not exist?...!", DirectoryReader.IndexExists(benchmark.RunData.Directory));
            // now we should be able to open the index for write. 
            IndexWriter iw = new IndexWriter(benchmark.RunData.Directory,
                new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                .SetOpenMode(OpenMode.APPEND));
            iw.Dispose();
            IndexReader ir = DirectoryReader.Open(benchmark.RunData.Directory);
            assertEquals("1000 docs were added to the index, this is what we expect to find!", 1000, ir.NumDocs);
            ir.Dispose();
        }

        /**
         * Test timed sequence task.
         */
        [Test]
        public void TestTimedSearchTask()
        {
            String[] algLines = {
                "log.step=100000",
                "ResetSystemErase",
                "CreateIndex",
                "{ AddDoc } : 100",
                "ForceMerge(1)",
                "CloseIndex",
                "OpenReader",
                "{ CountingSearchTest } : .5s",
                "CloseReader",
            };

            CountingSearchTestTask.numSearches = 0;
            execBenchmark(algLines);
            assertTrue(CountingSearchTestTask.numSearches > 0);
            long elapsed = CountingSearchTestTask.prevLastMillis - CountingSearchTestTask.startMillis;
            assertTrue("elapsed time was " + elapsed + " msec", elapsed <= 1500);
        }

        // disabled until we fix BG thread prio -- this test
        // causes build to hang
        [Test]
        public void TestBGSearchTaskThreads()
        {
            String[] algLines = {
                "log.time.step.msec = 100",
                "log.step=100000",
                "ResetSystemErase",
                "CreateIndex",
                "{ AddDoc } : 1000",
                "ForceMerge(1)",
                "CloseIndex",
                "OpenReader",
                "{",
                "  [ \"XSearch\" { CountingSearchTest > : * ] : 2 &-1",
                "  Wait(0.5)",
                "}",
                "CloseReader",
                "RepSumByPref X"
            };

            CountingSearchTestTask.numSearches = 0;
            execBenchmark(algLines);

            // NOTE: cannot assert this, because on a super-slow
            // system, it could be after waiting 0.5 seconds that
            // the search threads hadn't yet succeeded in starting
            // up and then they start up and do no searching:
            //assertTrue(CountingSearchTestTask.numSearches > 0);
        }

        [Test]
        public void TestHighlighting()
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = {
                "doc.stored=true",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "query.maker=" + typeof(ReutersQueryMaker).AssemblyQualifiedName,
                "ResetSystemErase",
                "CreateIndex",
                "{ AddDoc } : 100",
                "ForceMerge(1)",
                "CloseIndex",
                "OpenReader",
                "{ CountingHighlighterTest(size[1],highlight[1],mergeContiguous[true],maxFrags[1],fields[body]) } : 200",
                "CloseReader",
            };

            // 2. we test this value later
            CountingHighlighterTestTask.numHighlightedResults = 0;
            CountingHighlighterTestTask.numDocsRetrieved = 0;
            // 3. execute the algorithm  (required in every "logic" test)
            Benchmark benchmark = execBenchmark(algLines);

            // 4. test specific checks after the benchmark run completed.
            assertEquals("TestSearchTask was supposed to be called!", 92, CountingHighlighterTestTask.numDocsRetrieved);
            //pretty hard to figure out a priori how many docs are going to have highlighted fragments returned, but we can never have more than the number of docs
            //we probably should use a different doc/query maker, but...
            assertTrue("TestSearchTask was supposed to be called!", CountingHighlighterTestTask.numDocsRetrieved >= CountingHighlighterTestTask.numHighlightedResults && CountingHighlighterTestTask.numHighlightedResults > 0);

            assertTrue("Index does not exist?...!", DirectoryReader.IndexExists(benchmark.RunData.Directory));
            // now we should be able to open the index for write.
            IndexWriter iw = new IndexWriter(benchmark.RunData.Directory, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND));
            iw.Dispose();
            IndexReader ir = DirectoryReader.Open(benchmark.RunData.Directory);
            assertEquals("100 docs were added to the index, this is what we expect to find!", 100, ir.NumDocs);
            ir.Dispose();
        }

        [Test]
        public void TestHighlightingTV()
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = {
                "doc.stored=true",//doc storage is required in order to have text to highlight
                "doc.term.vector=true",
                "doc.term.vector.offsets=true",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "query.maker=" + typeof(ReutersQueryMaker).AssemblyQualifiedName,
                "ResetSystemErase",
                "CreateIndex",
                "{ AddDoc } : 1000",
                "ForceMerge(1)",
                "CloseIndex",
                "OpenReader",
                "{ CountingHighlighterTest(size[1],highlight[1],mergeContiguous[true],maxFrags[1],fields[body]) } : 200",
                "CloseReader",
            };

            // 2. we test this value later
            CountingHighlighterTestTask.numHighlightedResults = 0;
            CountingHighlighterTestTask.numDocsRetrieved = 0;
            // 3. execute the algorithm  (required in every "logic" test)
            Benchmark benchmark = execBenchmark(algLines);

            // 4. test specific checks after the benchmark run completed.
            assertEquals("TestSearchTask was supposed to be called!", 92, CountingHighlighterTestTask.numDocsRetrieved);
            //pretty hard to figure out a priori how many docs are going to have highlighted fragments returned, but we can never have more than the number of docs
            //we probably should use a different doc/query maker, but...
            assertTrue("TestSearchTask was supposed to be called!", CountingHighlighterTestTask.numDocsRetrieved >= CountingHighlighterTestTask.numHighlightedResults && CountingHighlighterTestTask.numHighlightedResults > 0);

            assertTrue("Index does not exist?...!", DirectoryReader.IndexExists(benchmark.RunData.Directory));
            // now we should be able to open the index for write.
            IndexWriter iw = new IndexWriter(benchmark.RunData.Directory, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND));
            iw.Dispose();
            IndexReader ir = DirectoryReader.Open(benchmark.RunData.Directory);
            assertEquals("1000 docs were added to the index, this is what we expect to find!", 1000, ir.NumDocs);
            ir.Dispose();
        }

        [Test]
        public void TestHighlightingNoTvNoStore()
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = {
                "doc.stored=false",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "query.maker=" + typeof(ReutersQueryMaker).AssemblyQualifiedName,
                "ResetSystemErase",
                "CreateIndex",
                "{ AddDoc } : 1000",
                "ForceMerge(1)",
                "CloseIndex",
                "OpenReader",
                "{ CountingHighlighterTest(size[1],highlight[1],mergeContiguous[true],maxFrags[1],fields[body]) } : 200",
                "CloseReader",
            };

            // 2. we test this value later
            CountingHighlighterTestTask.numHighlightedResults = 0;
            CountingHighlighterTestTask.numDocsRetrieved = 0;
            // 3. execute the algorithm  (required in every "logic" test)
            try
            {
                Benchmark benchmark = execBenchmark(algLines);
                assertTrue("CountingHighlighterTest should have thrown an exception", false);
                assertNotNull(benchmark); // (avoid compile warning on unused variable)
            }
            catch (Exception e) when (e.IsException())
            {
                assertTrue(true);
            }
        }

        /**
         * Test Exhasting Doc Maker logic
         */
        [Test]
        public void TestExhaustContentSource()
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.SingleDocSource, Lucene.Net.Benchmark",
                "content.source.log.step=1",
                "doc.term.vector=false",
                "content.source.forever=false",
                "directory=RAMDirectory",
                "doc.stored=false",
                "doc.tokenized=false",
                "# ----- alg ",
                "CreateIndex",
                "{ AddDoc } : * ",
                "ForceMerge(1)",
                "CloseIndex",
                "OpenReader",
                "{ CountingSearchTest } : 100",
                "CloseReader",
                "[ CountingSearchTest > : 30",
                "[ CountingSearchTest > : 9",
            };

            // 2. we test this value later
            CountingSearchTestTask.numSearches = 0;

            // 3. execute the algorithm  (required in every "logic" test)
            Benchmark benchmark = execBenchmark(algLines);

            // 4. test specific checks after the benchmark run completed.
            assertEquals("TestSearchTask was supposed to be called!", 139, CountingSearchTestTask.numSearches);
            assertTrue("Index does not exist?...!", DirectoryReader.IndexExists(benchmark.RunData.Directory));
            // now we should be able to open the index for write. 
            IndexWriter iw = new IndexWriter(benchmark.RunData.Directory, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.APPEND));
            iw.Dispose();
            IndexReader ir = DirectoryReader.Open(benchmark.RunData.Directory);
            assertEquals("1 docs were added to the index, this is what we expect to find!", 1, ir.NumDocs);
            ir.Dispose();
        }

        // LUCENE-1994: test thread safety of SortableSingleDocMaker
        [Test]
        public void TestDocMakerThreadSafety()
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.SortableSingleDocSource, Lucene.Net.Benchmark",
                "doc.term.vector=false",
                "log.step.AddDoc=10000",
                "content.source.forever=true",
                "directory=RAMDirectory",
                "doc.reuse.fields=false",
                "doc.stored=false",
                "doc.tokenized=false",
                "doc.index.props=true",
                "# ----- alg ",
                "CreateIndex",
                "[ { AddDoc > : 250 ] : 4",
                "CloseIndex",
            };

            // 2. we test this value later
            CountingSearchTestTask.numSearches = 0;

            // 3. execute the algorithm  (required in every "logic" test)
            Benchmark benchmark = execBenchmark(algLines);

            DirectoryReader r = DirectoryReader.Open(benchmark.RunData.Directory);
            SortedDocValues idx = FieldCache.DEFAULT.GetTermsIndex(SlowCompositeReaderWrapper.Wrap(r), "country");
            int maxDoc = r.MaxDoc;
            assertEquals(1000, maxDoc);
            for (int i = 0; i < 1000; i++)
            {
                assertTrue("doc " + i + " has null country", idx.GetOrd(i) != -1);
            }
            r.Dispose();
        }

        /**
         * Test Parallel Doc Maker logic (for LUCENE-940)
         */
        [Test]
        public void TestParallelDocMaker()
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "content.source.log.step=3",
                "doc.term.vector=false",
                "content.source.forever=false",
                "directory=FSDirectory",
                "doc.stored=false",
                "doc.tokenized=false",
                "# ----- alg ",
                "CreateIndex",
                "[ { AddDoc } : * ] : 4 ",
                "CloseIndex",
            };

            // 2. execute the algorithm  (required in every "logic" test)
            Benchmark benchmark = execBenchmark(algLines);

            // 3. test number of docs in the index
            IndexReader ir = DirectoryReader.Open(benchmark.RunData.Directory);
            int ndocsExpected = 20; // first 20 reuters docs.
            assertEquals("wrong number of docs in the index!", ndocsExpected, ir.NumDocs);
            ir.Dispose();
        }

        /**
         * Test WriteLineDoc and LineDocSource.
         */
        [Test]
        public void TestLineDocFile()
        {
            FileInfo lineFile = CreateTempFile("test.reuters.lines", ".txt");

            // We will call WriteLineDocs this many times
            int NUM_TRY_DOCS = 50;

            // Creates a line file with first 50 docs from SingleDocSource
            String[] algLines1 = {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.SingleDocSource, Lucene.Net.Benchmark",
                "content.source.forever=true",
                "line.file.out=" + lineFile.FullName.Replace('\\', '/'),
                "# ----- alg ",
                "{WriteLineDoc()}:" + NUM_TRY_DOCS,
            };

            // Run algo
            Benchmark benchmark = execBenchmark(algLines1);

            TextReader r =
                new StreamReader(
                    new FileStream(lineFile.FullName, FileMode.Open, FileAccess.Read), Encoding.UTF8);
            int numLines = 0;
            String line;
            while ((line = r.ReadLine()) != null)
            {
                if (numLines == 0 && line.StartsWith(WriteLineDocTask.FIELDS_HEADER_INDICATOR, StringComparison.Ordinal))
                {
                    continue; // do not count the header line as a doc 
                }
                numLines++;
            }
            r.Dispose();
            assertEquals("did not see the right number of docs; should be " + NUM_TRY_DOCS + " but was " + numLines, NUM_TRY_DOCS, numLines);

            // Index the line docs
            String[] algLines2 = {
                "# ----- properties ",
                "analyzer=Lucene.Net.Analysis.Core.WhitespaceAnalyzer, Lucene.Net.Analysis.Common",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + lineFile.FullName.Replace('\\', '/'),
                "content.source.forever=false",
                "doc.reuse.fields=false",
                "ram.flush.mb=4",
                "# ----- alg ",
                "ResetSystemErase",
                "CreateIndex",
                "{AddDoc}: *",
                "CloseIndex",
            };

            // Run algo
            benchmark = execBenchmark(algLines2);

            // now we should be able to open the index for write. 
            IndexWriter iw = new IndexWriter(benchmark.RunData.Directory,
                new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                    .SetOpenMode(OpenMode.APPEND));
            iw.Dispose();

            IndexReader ir = DirectoryReader.Open(benchmark.RunData.Directory);
            assertEquals(numLines + " lines were created but " + ir.NumDocs + " docs are in the index", numLines, ir.NumDocs);
            ir.Dispose();

            lineFile.Delete();
        }

        /**
         * Test ReadTokensTask
         */
        [Test]
        public void TestReadTokens()
        {

            // We will call ReadTokens on this many docs
            int NUM_DOCS = 20;

            // Read tokens from first NUM_DOCS docs from Reuters and
            // then build index from the same docs
            String[] algLines1 = {
                "# ----- properties ",
                "analyzer=Lucene.Net.Analysis.Core.WhitespaceAnalyzer, Lucene.Net.Analysis.Common",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "# ----- alg ",
                "{ReadTokens}: " + NUM_DOCS,
                "ResetSystemErase",
                "CreateIndex",
                "{AddDoc}: " + NUM_DOCS,
                "CloseIndex",
            };

            // Run algo
            Benchmark benchmark = execBenchmark(algLines1);

            IList<TaskStats> stats = benchmark.RunData.Points.TaskStats;

            // Count how many tokens all ReadTokens saw
            int totalTokenCount1 = 0;
            foreach (TaskStats stat in stats)
            {
                if (stat.Task.GetName().Equals("ReadTokens", StringComparison.Ordinal))
                {
                    totalTokenCount1 += stat.Count;
                }
            }

            // Separately count how many tokens are actually in the index:
            IndexReader reader = DirectoryReader.Open(benchmark.RunData.Directory);
            assertEquals(NUM_DOCS, reader.NumDocs);

            int totalTokenCount2 = 0;

            Fields fields = MultiFields.GetFields(reader);

            foreach (String fieldName in fields)
            {
                if (fieldName.Equals(DocMaker.ID_FIELD, StringComparison.Ordinal) || fieldName.Equals(DocMaker.DATE_MSEC_FIELD, StringComparison.Ordinal) || fieldName.Equals(DocMaker.TIME_SEC_FIELD, StringComparison.Ordinal))
                {
                    continue;
                }
                Terms terms = fields.GetTerms(fieldName);
                if (terms is null)
                {
                    continue;
                }
                TermsEnum termsEnum = terms.GetEnumerator();
                DocsEnum docs = null;
                while (termsEnum.MoveNext())
                {
                    docs = TestUtil.Docs(Random, termsEnum, MultiFields.GetLiveDocs(reader), docs, DocsFlags.FREQS);
                    while (docs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        totalTokenCount2 += docs.Freq;
                    }
                }
            }
            reader.Dispose();

            // Make sure they are the same
            assertEquals(totalTokenCount1, totalTokenCount2);
        }

        /**
         * Test that " {[AddDoc(4000)]: 4} : * " works corrcetly (for LUCENE-941)
         */
        [Test]
        public void TestParallelExhausted()
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "content.source.log.step=3",
                "doc.term.vector=false",
                "content.source.forever=false",
                "directory=RAMDirectory",
                "doc.stored=false",
                "doc.tokenized=false",
                "task.max.depth.log=1",
                "# ----- alg ",
                "CreateIndex",
                "{ [ AddDoc]: 4} : * ",
                "ResetInputs ",
                "{ [ AddDoc]: 4} : * ",
                "WaitForMerges",
                "CloseIndex",
            };

            // 2. execute the algorithm  (required in every "logic" test)
            Benchmark benchmark = execBenchmark(algLines);

            // 3. test number of docs in the index
            IndexReader ir = DirectoryReader.Open(benchmark.RunData.Directory);
            int ndocsExpected = 2 * 20; // first 20 reuters docs.
            assertEquals("wrong number of docs in the index!", ndocsExpected, ir.NumDocs);
            ir.Dispose();
        }


        /**
         * Test that exhaust in loop works as expected (LUCENE-1115).
         */
        [Test]
        public void TestExhaustedLooped()
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "content.source.log.step=3",
                "doc.term.vector=false",
                "content.source.forever=false",
                "directory=RAMDirectory",
                "doc.stored=false",
                "doc.tokenized=false",
                "task.max.depth.log=1",
                "# ----- alg ",
                "{ \"Rounds\"",
                "  ResetSystemErase",
                "  CreateIndex",
                "  { \"AddDocs\"  AddDoc > : * ",
                "  WaitForMerges",
                "  CloseIndex",
                "} : 2",
            };

            // 2. execute the algorithm  (required in every "logic" test)
            Benchmark benchmark = execBenchmark(algLines);

            // 3. test number of docs in the index
            IndexReader ir = DirectoryReader.Open(benchmark.RunData.Directory);
            int ndocsExpected = 20;  // first 20 reuters docs.
            assertEquals("wrong number of docs in the index!", ndocsExpected, ir.NumDocs);
            ir.Dispose();
        }

        /**
         * Test that we can close IndexWriter with argument "false".
         */
        [Test]
        public void TestCloseIndexFalse()
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "ram.flush.mb=-1",
                "max.buffered=2",
                "content.source.log.step=3",
                "doc.term.vector=false",
                "content.source.forever=false",
                "directory=RAMDirectory",
                "doc.stored=false",
                "doc.tokenized=false",
                "debug.level=1",
                "# ----- alg ",
                "{ \"Rounds\"",
                "  ResetSystemErase",
                "  CreateIndex",
                "  { \"AddDocs\"  AddDoc > : * ",
                "  CloseIndex(false)",
                "} : 2",
            };

            // 2. execute the algorithm  (required in every "logic" test)
            Benchmark benchmark = execBenchmark(algLines);

            // 3. test number of docs in the index
            IndexReader ir = DirectoryReader.Open(benchmark.RunData.Directory);
            int ndocsExpected = 20; // first 20 reuters docs.
            assertEquals("wrong number of docs in the index!", ndocsExpected, ir.NumDocs);
            ir.Dispose();
        }

        public class MyMergeScheduler : SerialMergeScheduler
        {
            internal bool called;
            public MyMergeScheduler()
                : base()
            {
                called = true;
            }
        }

        /**
         * Test that we can set merge scheduler".
         */
        [Test]
        public void TestMergeScheduler()
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "content.source.log.step=3",
                "doc.term.vector=false",
                "content.source.forever=false",
                "directory=RAMDirectory",
                "merge.scheduler=" + typeof(MyMergeScheduler).AssemblyQualifiedName,
                "doc.stored=false",
                "doc.tokenized=false",
                "debug.level=1",
                "# ----- alg ",
                "{ \"Rounds\"",
                "  ResetSystemErase",
                "  CreateIndex",
                "  { \"AddDocs\"  AddDoc > : * ",
                "} : 2",
            };
            // 2. execute the algorithm  (required in every "logic" test)
            Benchmark benchmark = execBenchmark(algLines);

            assertTrue("did not use the specified MergeScheduler",
                ((MyMergeScheduler)benchmark.RunData.IndexWriter.Config
                    .MergeScheduler).called);
            benchmark.RunData.IndexWriter.Dispose();

            // 3. test number of docs in the index
            IndexReader ir = DirectoryReader.Open(benchmark.RunData.Directory);
            int ndocsExpected = 20; // first 20 reuters docs.
            assertEquals("wrong number of docs in the index!", ndocsExpected, ir.NumDocs);
            ir.Dispose();
        }

        public class MyMergePolicy : LogDocMergePolicy
        {
            internal bool called;
            public MyMergePolicy()
            {
                called = true;
            }
        }

        /**
         * Test that we can set merge policy".
         */
        [Test]
        public void TestMergePolicy()
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "content.source.log.step=3",
                "ram.flush.mb=-1",
                "max.buffered=2",
                "doc.term.vector=false",
                "content.source.forever=false",
                "directory=RAMDirectory",
                "merge.policy=" + typeof(MyMergePolicy).AssemblyQualifiedName,
                "doc.stored=false",
                "doc.tokenized=false",
                "debug.level=1",
                "# ----- alg ",
                "{ \"Rounds\"",
                "  ResetSystemErase",
                "  CreateIndex",
                "  { \"AddDocs\"  AddDoc > : * ",
                "} : 2",
            };

            // 2. execute the algorithm  (required in every "logic" test)
            Benchmark benchmark = execBenchmark(algLines);
            assertTrue("did not use the specified MergePolicy", ((MyMergePolicy)benchmark.RunData.IndexWriter.Config.MergePolicy).called);
            benchmark.RunData.IndexWriter.Dispose();

            // 3. test number of docs in the index
            IndexReader ir = DirectoryReader.Open(benchmark.RunData.Directory);
            int ndocsExpected = 20; // first 20 reuters docs.
            assertEquals("wrong number of docs in the index!", ndocsExpected, ir.NumDocs);
            ir.Dispose();
        }

        /**
         * Test that IndexWriter settings stick.
         */
        [Test]
        public void TestIndexWriterSettings()
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "content.source.log.step=3",
                "ram.flush.mb=-1",
                "max.buffered=2",
                "compound=cmpnd:true:false",
                "doc.term.vector=vector:false:true",
                "content.source.forever=false",
                "directory=RAMDirectory",
                "doc.stored=false",
                "merge.factor=3",
                "doc.tokenized=false",
                "debug.level=1",
                "# ----- alg ",
                "{ \"Rounds\"",
                "  ResetSystemErase",
                "  CreateIndex",
                "  { \"AddDocs\"  AddDoc > : * ",
                "  NewRound",
                "} : 2",
            };

            // 2. execute the algorithm  (required in every "logic" test)
            Benchmark benchmark = execBenchmark(algLines);
            IndexWriter writer = benchmark.RunData.IndexWriter;
            assertEquals(2, writer.Config.MaxBufferedDocs);
            assertEquals(IndexWriterConfig.DISABLE_AUTO_FLUSH, (int)writer.Config.RAMBufferSizeMB);
            assertEquals(3, ((LogMergePolicy)writer.Config.MergePolicy).MergeFactor);
            assertEquals(0.0d, writer.Config.MergePolicy.NoCFSRatio, 0.0);
            writer.Dispose();
            Store.Directory dir = benchmark.RunData.Directory;
            IndexReader reader = DirectoryReader.Open(dir);
            Fields tfv = reader.GetTermVectors(0);
            assertNotNull(tfv);
            assertTrue(tfv.Count > 0);
            reader.Dispose();
        }

        /**
         * Test indexing with facets tasks.
         */
        [Test]
        public void TestIndexingWithFacets()
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "content.source.log.step=100",
                "content.source.forever=false",
                "directory=RAMDirectory",
                "doc.stored=false",
                "merge.factor=3",
                "doc.tokenized=false",
                "debug.level=1",
                "# ----- alg ",
                "ResetSystemErase",
                "CreateIndex",
                "CreateTaxonomyIndex",
                "{ \"AddDocs\"  AddFacetedDoc > : * ",
                "CloseIndex",
                "CloseTaxonomyIndex",
                "OpenTaxonomyReader",
            };

            // 2. execute the algorithm  (required in every "logic" test)
            Benchmark benchmark = execBenchmark(algLines);
            PerfRunData runData = benchmark.RunData;
            assertNull("taxo writer was not properly closed", runData.TaxonomyWriter);
            TaxonomyReader taxoReader = runData.GetTaxonomyReader();
            assertNotNull("taxo reader was not opened", taxoReader);
            assertTrue("nothing was added to the taxnomy (expecting root and at least one addtional category)", taxoReader.Count > 1);
            taxoReader.Dispose();
        }

        /**
         * Test that we can call forceMerge(maxNumSegments).
         */
        [Test]
        public void TestForceMerge()
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "content.source.log.step=3",
                "ram.flush.mb=-1",
                "max.buffered=3",
                "doc.term.vector=false",
                "content.source.forever=false",
                "directory=RAMDirectory",
                "merge.policy=Lucene.Net.Index.LogDocMergePolicy, Lucene.Net",
                "doc.stored=false",
                "doc.tokenized=false",
                "debug.level=1",
                "# ----- alg ",
                "{ \"Rounds\"",
                "  ResetSystemErase",
                "  CreateIndex",
                "  { \"AddDocs\"  AddDoc > : * ",
                "  ForceMerge(3)",
                "  CloseIndex()",
                "} : 2",
            };

            // 2. execute the algorithm  (required in every "logic" test)
            Benchmark benchmark = execBenchmark(algLines);

            // 3. test number of docs in the index
            IndexReader ir = DirectoryReader.Open(benchmark.RunData.Directory);
            int ndocsExpected = 20; // first 20 reuters docs.
            assertEquals("wrong number of docs in the index!", ndocsExpected, ir.NumDocs);
            ir.Dispose();

            // Make sure we have 3 segments:
            SegmentInfos infos = new SegmentInfos();
            infos.Read(benchmark.RunData.Directory);
            assertEquals(3, infos.Count);
        }

        /**
         * Test disabling task count (LUCENE-1136).
         */
        [Test]
        public void TestDisableCounting()
        {
            doTestDisableCounting(true);
            doTestDisableCounting(false);
        }

        private void doTestDisableCounting(bool disable)
        {
            // 1. alg definition (required in every "logic" test)
            String[] algLines = disableCountingLines(disable);

            // 2. execute the algorithm  (required in every "logic" test)
            Benchmark benchmark = execBenchmark(algLines);

            // 3. test counters
            int n = disable ? 0 : 1;
            int nChecked = 0;
            foreach (TaskStats stats in benchmark.RunData.Points.TaskStats)
            {
                String taskName = stats.Task.GetName();
                if (taskName.Equals("Rounds", StringComparison.Ordinal))
                {
                    assertEquals("Wrong total count!", 20 + 2 * n, stats.Count);
                    nChecked++;
                }
                else if (taskName.Equals("CreateIndex", StringComparison.Ordinal))
                {
                    assertEquals("Wrong count for CreateIndex!", n, stats.Count);
                    nChecked++;
                }
                else if (taskName.Equals("CloseIndex", StringComparison.Ordinal))
                {
                    assertEquals("Wrong count for CloseIndex!", n, stats.Count);
                    nChecked++;
                }
            }
            assertEquals("Missing some tasks to check!", 3, nChecked);
        }

        private String[] disableCountingLines(bool disable)
        {
            String dis = disable ? "-" : "";
            return new String[] {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "content.source.log.step=30",
                "doc.term.vector=false",
                "content.source.forever=false",
                "directory=RAMDirectory",
                "doc.stored=false",
                "doc.tokenized=false",
                "task.max.depth.log=1",
                "# ----- alg ",
                "{ \"Rounds\"",
                "  ResetSystemErase",
                "  "+dis+"CreateIndex",            // optionally disable counting here
                "  { \"AddDocs\"  AddDoc > : * ",
                "  "+dis+"  CloseIndex",             // optionally disable counting here (with extra blanks)
                "}",
                "RepSumByName",
            };
        }

        /**
         * Test that we can change the Locale in the runData,
         * that it is parsed as we expect.
         */
        [Test]
        public void TestLocale()
        {
            // empty Locale: clear it (null)
            Benchmark benchmark = execBenchmark(getLocaleConfig(""));
            assertNull(benchmark.RunData.Locale);

            // ROOT locale
            benchmark = execBenchmark(getLocaleConfig("ROOT"));
            assertEquals(CultureInfo.InvariantCulture, benchmark.RunData.Locale);

            // specify just a language 
            benchmark = execBenchmark(getLocaleConfig("de"));
            assertEquals(new CultureInfo("de"), benchmark.RunData.Locale);

            // specify language + country
            benchmark = execBenchmark(getLocaleConfig("en,US"));
            assertEquals(new CultureInfo("en-US"), benchmark.RunData.Locale);

            // specify language + country + variant
            //benchmark = execBenchmark(getLocaleConfig("no,NO,NY"));
            //assertEquals(new CultureInfo("no-NO"/*, "NY"*/), benchmark.RunData.Locale);

            // LUCENENET specific - in .NET Norwegian is specified as either nb-NO (Bokmål) or 
            // nn-NO (Nynorsk) + a few other dialects. no-NO works sometimes, but is not
            // supported across all OS's, so doesn't make a reliable test case.
            benchmark = execBenchmark(getLocaleConfig("nb,NO,NY"));
            assertEquals(new CultureInfo("nb-NO"/*, "NY"*/), benchmark.RunData.Locale);
        }

        private String[] getLocaleConfig(String localeParam)
        {
            String[] algLines = {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "content.source.log.step=3",
                "content.source.forever=false",
                "directory=RAMDirectory",
                "# ----- alg ",
                "{ \"Rounds\"",
                "  ResetSystemErase",
                "  NewLocale(" + localeParam + ")",
                "  CreateIndex",
                "  { \"AddDocs\"  AddDoc > : * ",
                "  NewRound",
                "} : 1",
            };
            return algLines;
        }

        /**
         * Test that we can create CollationAnalyzers.
         */
        [Test]
        public void TestCollator()
        {
            // LUCENENET specific - we don't have a JDK version of collator
            // so we are using ICU
            var collatorParam = "impl:icu";

            // ROOT locale
            Benchmark benchmark = execBenchmark(getCollatorConfig("ROOT", collatorParam));
            ICUCollationKeyAnalyzer expected = new ICUCollationKeyAnalyzer(TEST_VERSION_CURRENT, Collator
                .GetInstance(CultureInfo.InvariantCulture));
            assertEqualCollation(expected, benchmark.RunData.Analyzer, "foobar");

            // specify just a language
            benchmark = execBenchmark(getCollatorConfig("de", collatorParam));
            expected = new ICUCollationKeyAnalyzer(TEST_VERSION_CURRENT, Collator.GetInstance(new CultureInfo("de")));
            assertEqualCollation(expected, benchmark.RunData.Analyzer, "foobar");

            // specify language + country
            benchmark = execBenchmark(getCollatorConfig("en,US", collatorParam));
            expected = new ICUCollationKeyAnalyzer(TEST_VERSION_CURRENT, Collator.GetInstance(new CultureInfo("en-US")));
            assertEqualCollation(expected, benchmark.RunData.Analyzer, "foobar");

            //// specify language + country + variant
            //benchmark = execBenchmark(getCollatorConfig("no,NO,NY", collatorParam));
            //expected = new ICUCollationKeyAnalyzer(TEST_VERSION_CURRENT, Collator.Create(new CultureInfo("no-NO"/*, "NY"*/), Collator.Fallback.FallbackAllowed));
            //assertEqualCollation(expected, benchmark.RunData.Analyzer, "foobar");

            // LUCENENET specific - in .NET Norwegian is specified as either nb-NO (Bokmål) or 
            // nn-NO (Nynorsk) + a few other dialects. no-NO works sometimes, but is not
            // supported across all OS's, so doesn't make a reliable test case.

            // specify language + country + variant
            benchmark = execBenchmark(getCollatorConfig("nb,NO,NY", collatorParam));
            expected = new ICUCollationKeyAnalyzer(TEST_VERSION_CURRENT, Collator.GetInstance(new CultureInfo("nb-NO"/*, "NY"*/)));
            assertEqualCollation(expected, benchmark.RunData.Analyzer, "foobar");
        }

        private void assertEqualCollation(Analyzer a1, Analyzer a2, String text)
        {
            TokenStream ts1 = a1.GetTokenStream("bogus", text);
            TokenStream ts2 = a2.GetTokenStream("bogus", text);
            ts1.Reset();
            ts2.Reset();
            ITermToBytesRefAttribute termAtt1 = ts1.AddAttribute<ITermToBytesRefAttribute>();
            ITermToBytesRefAttribute termAtt2 = ts2.AddAttribute<ITermToBytesRefAttribute>();
            assertTrue(ts1.IncrementToken());
            assertTrue(ts2.IncrementToken());
            BytesRef bytes1 = termAtt1.BytesRef;
            BytesRef bytes2 = termAtt2.BytesRef;
            termAtt1.FillBytesRef();
            termAtt2.FillBytesRef();
            assertEquals(bytes1, bytes2);
            assertFalse(ts1.IncrementToken());
            assertFalse(ts2.IncrementToken());
            ts1.Dispose();
            ts2.Dispose();
        }

        private String[] getCollatorConfig(String localeParam,
            String collationParam)
        {
            String[] algLines = {
                "# ----- properties ",
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "content.source.log.step=3",
                "content.source.forever=false",
                "directory=RAMDirectory",
                "# ----- alg ",
                "{ \"Rounds\"",
                "  ResetSystemErase",
                "  NewLocale(" + localeParam + ")",
                "  NewCollationAnalyzer(" + collationParam + ")",
                "  CreateIndex",
                "  { \"AddDocs\"  AddDoc > : * ",
                "  NewRound",
                "} : 1",
            };
            return algLines;
        }

        /**
         * Test that we can create shingle analyzers using AnalyzerFactory.
         */
        [Test]
        public void TestShingleAnalyzer()
        {
            String text = "one,two,three, four five six";

            // StandardTokenizer, maxShingleSize, and outputUnigrams
            Benchmark benchmark = execBenchmark(getAnalyzerFactoryConfig
                ("shingle-analyzer", "StandardTokenizer,ShingleFilter"));
            benchmark.RunData.Analyzer.GetTokenStream
                ("bogus", text).Dispose();
            BaseTokenStreamTestCase.AssertAnalyzesTo(benchmark.RunData.Analyzer, text,
                                                     new String[] { "one", "one two", "two", "two three",
                                                            "three", "three four", "four", "four five",
                                                            "five", "five six", "six" });
            // StandardTokenizer, maxShingleSize = 3, and outputUnigrams = false
            benchmark = execBenchmark
              (getAnalyzerFactoryConfig
                  ("shingle-analyzer",
                   "StandardTokenizer,ShingleFilter(maxShingleSize:3,outputUnigrams:false)"));
            BaseTokenStreamTestCase.AssertAnalyzesTo(benchmark.RunData.Analyzer, text,
                                                     new String[] { "one two", "one two three", "two three",
                                                            "two three four", "three four",
                                                            "three four five", "four five",
                                                            "four five six", "five six" });
            // WhitespaceTokenizer, default maxShingleSize and outputUnigrams
            benchmark = execBenchmark
              (getAnalyzerFactoryConfig("shingle-analyzer", "WhitespaceTokenizer,ShingleFilter"));
            BaseTokenStreamTestCase.AssertAnalyzesTo(benchmark.RunData.Analyzer, text,
                                                     new String[] { "one,two,three,", "one,two,three, four",
                                                            "four", "four five", "five", "five six",
                                                            "six" });

            // WhitespaceTokenizer, maxShingleSize=3 and outputUnigrams=false
            benchmark = execBenchmark
              (getAnalyzerFactoryConfig
                ("shingle-factory",
                 "WhitespaceTokenizer,ShingleFilter(outputUnigrams:false,maxShingleSize:3)"));
            BaseTokenStreamTestCase.AssertAnalyzesTo(benchmark.RunData.Analyzer, text,
                                                     new String[] { "one,two,three, four",
                                                            "one,two,three, four five",
                                                            "four five", "four five six",
                                                            "five six" });
        }

        private String[] getAnalyzerFactoryConfig(String name, String @params)
        {
            //String singleQuoteEscapedName = name.Replace("'", "\\\\'");
            //String[] algLines = {
            //    "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
            //    "docs.file=" + getReuters20LinesFile(),
            //    "work.dir=" + getWorkDir().FullName.Replace(@"\\\\", "/"), // Fix Windows path
            //    "content.source.forever=false",
            //    "directory=RAMDirectory",
            //    "AnalyzerFactory(name:'" + singleQuoteEscapedName + "', " + @params + ")",
            //    "NewAnalyzer('" + singleQuoteEscapedName + "')",
            //    "CreateIndex",
            //    "{ \"AddDocs\"  AddDoc > : * "
            //};
            //String singleQuoteEscapedName = name.Replace("'", @"\'");
            String singleQuoteEscapedName = name.Replace("'", @"\'");
            String[] algLines = {
                "content.source=Lucene.Net.Benchmarks.ByTask.Feeds.LineDocSource, Lucene.Net.Benchmark",
                "docs.file=" + getReuters20LinesFile(),
                "work.dir=" + getWorkDir().FullName.Replace(@"\", "/"), // Fix Windows path
                "content.source.forever=false",
                "directory=RAMDirectory",
                "AnalyzerFactory(name:'" + singleQuoteEscapedName + "', " + @params + ")",
                "NewAnalyzer('" + singleQuoteEscapedName + "')",
                "CreateIndex",
                "{ \"AddDocs\"  AddDoc > : * "
            };
            return algLines;
        }

        [Test]
        public void TestAnalyzerFactory()
        {
            String text = "Fortieth, Quarantième, Cuadragésimo";
            Benchmark benchmark = execBenchmark(getAnalyzerFactoryConfig
                ("ascii folded, pattern replaced, standard tokenized, downcased, bigrammed.'analyzer'",
                 "positionIncrementGap:100,offsetGap:1111,"
                 + "MappingCharFilter(mapping:'test-mapping-ISOLatin1Accent-partial.txt'),"
                 + "PatternReplaceCharFilterFactory(pattern:'e(\\\\\\\\S*)m',replacement:\"$1xxx$1\"),"
                 + "StandardTokenizer,LowerCaseFilter,NGramTokenFilter(minGramSize:2,maxGramSize:2)"));
            BaseTokenStreamTestCase.AssertAnalyzesTo(benchmark.RunData.Analyzer, text,
                new String[] { "fo", "or", "rt", "ti", "ie", "et", "th",
                       "qu", "ua", "ar", "ra", "an", "nt", "ti", "ix", "xx", "xx", "xe",
                       "cu", "ua", "ad", "dr", "ra", "ag", "gs", "si", "ix", "xx", "xx", "xs", "si", "io"});
        }

        private String getReuters20LinesFile()
        {
            return getWorkDirResourcePath("reuters.first20.lines.txt");
        }
    }
}
