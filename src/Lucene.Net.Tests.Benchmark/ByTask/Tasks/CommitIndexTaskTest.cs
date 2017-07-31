using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Index;
using NUnit.Framework;
using System.Collections.Generic;

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
    /// Tests the functionality of {@link CreateIndexTask}.
    /// </summary>
    public class CommitIndexTaskTest : BenchmarkTestCase
    {
        private PerfRunData createPerfRunData()
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            props["writer.version"] = TEST_VERSION_CURRENT.ToString();
            props["print.props"] = "false"; // don't print anything
            props["directory"] = "RAMDirectory";
            Config config = new Config(props);
            return new PerfRunData(config);
        }

        [Test]
        public void TestNoParams()
        {
            PerfRunData runData = createPerfRunData();
            new CreateIndexTask(runData).DoLogic();
            new CommitIndexTask(runData).DoLogic();
            new CloseIndexTask(runData).DoLogic();
        }

        [Test]
        public void TestCommitData()
        {
            PerfRunData runData = createPerfRunData();
            new CreateIndexTask(runData).DoLogic();
            CommitIndexTask task = new CommitIndexTask(runData);
            task.SetParams("params");
            task.DoLogic();
            SegmentInfos infos = new SegmentInfos();
            infos.Read(runData.Directory);
            assertEquals("params", infos.UserData[OpenReaderTask.USER_DATA]);
            new CloseIndexTask(runData).DoLogic();
        }
    }
}
