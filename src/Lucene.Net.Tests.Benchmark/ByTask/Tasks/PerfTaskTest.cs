using Lucene.Net.Benchmarks.ByTask.Utils;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;

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
    /// Tests the functionality of the abstract {@link PerfTask}.
    /// </summary>
    public class PerfTaskTest : BenchmarkTestCase
    {
        private sealed class MyPerfTask : PerfTask
        {

            public MyPerfTask(PerfRunData runData)
                : base(runData)
            {
            }

            public override int DoLogic()
            {
                return 0;
            }

            public int getLogStep() { return m_logStep; }
        }

        private PerfRunData createPerfRunData(bool setLogStep, int logStepVal,
            bool setTaskLogStep, int taskLogStepVal)
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            if (setLogStep)
            {
                props["log.step"] = logStepVal.ToString(CultureInfo.InvariantCulture);
            }
            if (setTaskLogStep)
            {
                props["log.step.MyPerf"] = taskLogStepVal.ToString(CultureInfo.InvariantCulture);
            }
            props["directory"] = "RAMDirectory"; // no accidental FS dir.
            Config config = new Config(props);
            return new PerfRunData(config);
        }

        private void doLogStepTest(bool setLogStep, int logStepVal,
            bool setTaskLogStep, int taskLogStepVal, int expLogStepValue)
        {
            PerfRunData runData = createPerfRunData(setLogStep, logStepVal, setTaskLogStep, taskLogStepVal);
            MyPerfTask mpt = new MyPerfTask(runData);
            assertEquals(expLogStepValue, mpt.getLogStep());
        }

        [Test]
        public void TestLogStep()
        {
            doLogStepTest(false, -1, false, -1, PerfTask.DEFAULT_LOG_STEP);
            doLogStepTest(true, -1, false, -1, int.MaxValue);
            doLogStepTest(true, 100, false, -1, 100);
            doLogStepTest(false, -1, true, -1, int.MaxValue);
            doLogStepTest(false, -1, true, 100, 100);
        }
    }
}
