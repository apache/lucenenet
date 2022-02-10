using Lucene.Net.Benchmarks.ByTask.Feeds;
using Lucene.Net.Benchmarks.ByTask.Tasks;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
    /// Test very simply that perf tasks are parses as expected.
    /// </summary>
    public class TestPerfTasksParse : LuceneTestCase
    {
        static readonly String NEW_LINE = Environment.NewLine;
        static readonly String INDENT = "  ";

        // properties in effect in all tests here
        static readonly String propPart =
          INDENT + "directory=RAMDirectory" + NEW_LINE +
          INDENT + "print.props=false" + NEW_LINE
        ;

        /** Test the repetiotion parsing for parallel tasks */
        [Test]
        public void TestParseParallelTaskSequenceRepetition()
        {
            String taskStr = "AddDoc";
            String parsedTasks = "[ " + taskStr + " ] : 1000";
            Benchmark benchmark = new Benchmark(new StringReader(propPart + parsedTasks));
            Algorithm alg = benchmark.Algorithm;
            IList<PerfTask> algTasks = alg.ExtractTasks();
            bool foundAdd = false;
            foreach (PerfTask task in algTasks)
            {
                if (task.toString().IndexOf(taskStr, StringComparison.Ordinal) >= 0)
                {
                    foundAdd = true;
                }
                if (task is TaskSequence)
                {
                    assertEquals("repetions should be 1000 for " + parsedTasks, 1000, ((TaskSequence)task).Repetitions);
                    assertTrue("sequence for " + parsedTasks + " should be parallel!", ((TaskSequence)task).IsParallel);
                }
                assertTrue("Task " + taskStr + " was not found in " + alg.toString(), foundAdd);
            }
        }

        /** Test the repetiotion parsing for sequential  tasks */
        [Test]
        public void TestParseTaskSequenceRepetition()
        {
            String taskStr = "AddDoc";
            String parsedTasks = "{ " + taskStr + " } : 1000";
            Benchmark benchmark = new Benchmark(new StringReader(propPart + parsedTasks));
            Algorithm alg = benchmark.Algorithm;
            IList<PerfTask> algTasks = alg.ExtractTasks();
            bool foundAdd = false;
            foreach (PerfTask task in algTasks)
            {
                if (task.toString().IndexOf(taskStr, StringComparison.Ordinal) >= 0)
                {
                    foundAdd = true;
                }
                if (task is TaskSequence)
                {
                    assertEquals("repetions should be 1000 for " + parsedTasks, 1000, ((TaskSequence)task).Repetitions);
                    assertFalse("sequence for " + parsedTasks + " should be sequential!", ((TaskSequence)task).IsParallel);
                }
                assertTrue("Task " + taskStr + " was not found in " + alg.toString(), foundAdd);
            }
        }

        public class MockContentSource : ContentSource
        {
            public override DocData GetNextDocData(DocData docData)
            {
                return docData;
            }

            protected override void Dispose(bool disposing) { }
        }

        public class MockQueryMaker : AbstractQueryMaker
        {
            protected override Query[] PrepareQueries()
            {
                return new Query[0];
            }
        }

        /// <summary>Test the parsing of example scripts</summary>
        [Test]
        public void TestParseExamples()
        {
            // LUCENENET specific
            // Rather than relying on a file path somewhere, we store the
            // files zipped in an embedded resource and unzip them to a
            // known temp directory for the test.
            DirectoryInfo examplesDir = CreateTempDir("test-parse-examples");
            using (var stream = GetType().getResourceAsStream("conf.zip"))
            {
                TestUtil.Unzip(stream, examplesDir);
            }

            // hackedy-hack-hack
            bool foundFiles = false;

            foreach (FileInfo algFile in examplesDir.EnumerateFiles("*.alg"))
            {
                try
                {
                    Config config = new Config(new StreamReader(new FileStream(algFile.FullName, FileMode.Open, FileAccess.Read), Encoding.UTF8));
                    String contentSource = config.Get("content.source", null);
                    if (contentSource != null)
                    {
                        if (Type.GetType(contentSource) is null)
                            throw ClassNotFoundException.Create(contentSource);
                    }
                    config.Set("work.dir", CreateTempDir(LuceneTestCase.TestType.Name).FullName);
                    config.Set("content.source", typeof(MockContentSource).AssemblyQualifiedName);
                    String dir = config.Get("content.source", null);
                    if (dir != null)
                    {
                        if (Type.GetType(dir) is null)
                            throw ClassNotFoundException.Create(dir);
                    }
                    config.Set("directory", typeof(RAMDirectory).AssemblyQualifiedName);
                    if (config.Get("line.file.out", null) != null)
                    {
                        config.Set("line.file.out", CreateTempFile("linefile", ".txt").FullName);
                    }
                    string queryMaker = config.Get("query.maker", null);
                    if (queryMaker != null)
                    {
                        if (Type.GetType(queryMaker) is null)
                            throw ClassNotFoundException.Create(queryMaker);

                        config.Set("query.maker", typeof(MockQueryMaker).AssemblyQualifiedName);
                    }
                    PerfRunData data = new PerfRunData(config);
                    new Algorithm(data);
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    throw AssertionError.Create("Could not parse sample file: " + algFile, t);
                }
                foundFiles = true;
            }
            if (!foundFiles)
            {
                fail("could not find any .alg files!");
            }
        }
    }
}
