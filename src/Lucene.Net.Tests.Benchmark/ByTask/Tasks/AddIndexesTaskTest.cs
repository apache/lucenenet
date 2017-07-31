using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Benchmarks.ByTask.Tasks
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
    /// Tests the functionality of {@link AddIndexesTask}.
    /// </summary>
    public class AddIndexesTaskTest : BenchmarkTestCase
    {
        private static DirectoryInfo testDir, inputDir;

        public override void BeforeClass()
        {
            base.BeforeClass();
            testDir = CreateTempDir("addIndexesTask");

            // create a dummy index under inputDir
            inputDir = new DirectoryInfo(Path.Combine(testDir.FullName, "input"));
            Store.Directory tmpDir = NewFSDirectory(inputDir);
            try
            {
                IndexWriter writer = new IndexWriter(tmpDir, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
                for (int i = 0; i < 10; i++)
                {
                    writer.AddDocument(new Document());
                }
                writer.Dispose();
            }
            finally
            {
                tmpDir.Dispose();
            }
        }


        private PerfRunData createPerfRunData()
        {
            IDictionary<string, string> props = new Dictionary<string, string>();
            props["writer.version"] = TEST_VERSION_CURRENT.ToString();
            props["print.props"] = "false"; // don't print anything
            props["directory"] = "RAMDirectory";
            props[AddIndexesTask.ADDINDEXES_INPUT_DIR] = inputDir.FullName;
            Config config = new Config(props);
            return new PerfRunData(config);
        }

        private void assertIndex(PerfRunData runData)
        {
            Store.Directory taskDir = runData.Directory;
            assertSame(typeof(RAMDirectory), taskDir.GetType());
            IndexReader r = DirectoryReader.Open(taskDir);
            try
            {
                assertEquals(10, r.NumDocs);
            }
            finally
            {
                r.Dispose();
            }
        }

        [Test]
        public void TestAddIndexesDefault()
        {
            PerfRunData runData = createPerfRunData();
            // create the target index first
            new CreateIndexTask(runData).DoLogic();

            AddIndexesTask task = new AddIndexesTask(runData);
            task.Setup();

            // add the input index
            task.DoLogic();

            // close the index
            new CloseIndexTask(runData).DoLogic();


            assertIndex(runData);

            runData.Dispose();
        }

        [Test]
        public void TestAddIndexesDir()
        {
            PerfRunData runData = createPerfRunData();
            // create the target index first
            new CreateIndexTask(runData).DoLogic();

            AddIndexesTask task = new AddIndexesTask(runData);
            task.Setup();

            // add the input index
            task.SetParams("true");
            task.DoLogic();

            // close the index
            new CloseIndexTask(runData).DoLogic();


            assertIndex(runData);

            runData.Dispose();
        }

        [Test]
        public void TestAddIndexesReader()
        {
            PerfRunData runData = createPerfRunData();
            // create the target index first
            new CreateIndexTask(runData).DoLogic();

            AddIndexesTask task = new AddIndexesTask(runData);
            task.Setup();

            // add the input index
            task.SetParams("false");
            task.DoLogic();

            // close the index
            new CloseIndexTask(runData).DoLogic();


            assertIndex(runData);

            runData.Dispose();
        }
    }
}
