using Lucene.Net.Benchmarks.ByTask.Stats;
using System.Collections.Generic;
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
    /// Report all statistics with no aggregations.
    /// <para/>
    /// Other side effects: None.
    /// </summary>
    public class RepAllTask : ReportTask
    {
        public RepAllTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override int DoLogic()
        {
            Report rp = ReportAll(RunData.Points.TaskStats);

            Console.WriteLine();
            Console.WriteLine("------------> Report All (" + rp.Count + " out of " + rp.OutOf + ")");
            Console.WriteLine(rp.Text);
            Console.WriteLine();
            return 0;
        }

        /// <summary>
        /// Report detailed statistics as a string.
        /// </summary>
        /// <param name="taskStats"></param>
        /// <returns>The report.</returns>
        protected virtual Report ReportAll(IList<TaskStats> taskStats)
        {
            string longestOp = LongestOp(taskStats);
            bool first = true;
            StringBuilder sb = new StringBuilder();
            sb.Append(TableTitle(longestOp));
            sb.Append(newline);
            int reported = 0;
            foreach (TaskStats stat in taskStats)
            {
                if (stat.Elapsed >= 0)
                { // consider only tasks that ended
                    if (!first)
                    {
                        sb.Append(newline);
                    }
                    first = false;
                    string line = TaskReportLine(longestOp, stat);
                    reported++;
                    if (taskStats.Count > 2 && reported % 2 == 0)
                    {
                        line = line.Replace("   ", " - ");
                    }
                    sb.Append(line);
                }
            }
            string reptxt = (reported == 0 ? "No Matching Entries Were Found!" : sb.ToString());
            return new Report(reptxt, reported, reported, taskStats.Count);
        }
    }
}
