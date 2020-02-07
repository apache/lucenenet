using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Index;
using Lucene.Net.Support.IO;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;

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
    public class CreateIndexTaskTest : BenchmarkTestCase
    {
        private PerfRunData createPerfRunData(String infoStreamValue)
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            // :Post-Release-Update-Version.LUCENE_XY:
#pragma warning disable 612, 618
            props["writer.version"] = LuceneVersion.LUCENE_47.ToString();
#pragma warning restore 612, 618
            props["print.props"] = "false"; // don't print anything
            props["directory"] = "RAMDirectory";
            if (infoStreamValue != null)
            {
                props["writer.info.stream"] = infoStreamValue;
            }
            Config config = new Config(props);
            return new PerfRunData(config);
        }

        [Test]
        public void TestInfoStream_SystemOutErr()
        {

            TextWriter curOut = Console.Out;
            ByteArrayOutputStream baos = new ByteArrayOutputStream();
            Console.Out = new StreamWriter(baos, Encoding.GetEncoding(0));
            try
            {
                PerfRunData runData = createPerfRunData("SystemOut");
                CreateIndexTask cit = new CreateIndexTask(runData);
                cit.DoLogic();
                new CloseIndexTask(runData).DoLogic();
                assertTrue(baos.Length > 0);
            }
            finally
            {
                Console.Out = curOut;
            }

            TextWriter curErr = Console.Error;
            baos = new ByteArrayOutputStream();
            Console.Error = new StreamWriter(baos, Encoding.GetEncoding(0));
            try
            {
                PerfRunData runData = createPerfRunData("SystemErr");
                CreateIndexTask cit = new CreateIndexTask(runData);
                cit.DoLogic();
                new CloseIndexTask(runData).DoLogic();
                assertTrue(baos.Length > 0);
            }
            finally
            {
                Console.Error = curErr;
            }

        }

        [Test]
        public void TestInfoStream_File()
        {

            FileInfo outFile = new FileInfo(Path.Combine(getWorkDir().FullName, "infoStreamTest"));
            PerfRunData runData = createPerfRunData(outFile.FullName);
            new CreateIndexTask(runData).DoLogic();
            new CloseIndexTask(runData).DoLogic();
            assertTrue(new FileInfo(outFile.FullName).Length > 0);
        }

        [Test]
        public void TestNoMergePolicy()
        {
            PerfRunData runData = createPerfRunData(null);
            runData.Config.Set("merge.policy", typeof(NoMergePolicy).AssemblyQualifiedName);
            new CreateIndexTask(runData).DoLogic();
            new CloseIndexTask(runData).DoLogic();
        }

        [Test]
        public void TestNoMergeScheduler()
        {
            PerfRunData runData = createPerfRunData(null);
            runData.Config.Set("merge.scheduler", typeof(NoMergeScheduler).AssemblyQualifiedName);
            new CreateIndexTask(runData).DoLogic();
            new CloseIndexTask(runData).DoLogic();
        }

        [Test]
        public void TestNoDeletionPolicy()
        {
            PerfRunData runData = createPerfRunData(null);
            runData.Config.Set("deletion.policy", typeof(NoDeletionPolicy).AssemblyQualifiedName);
            new CreateIndexTask(runData).DoLogic();
            new CloseIndexTask(runData).DoLogic();
        }
    }
}
