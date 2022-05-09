using Lucene.Net.Benchmarks.ByTask.Stats;
using System.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

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
    /// Report all statistics aggregated by name.
    /// <para/>
    /// Other side effects: None.
    /// </summary>
    public class RepSumByNameTask : ReportTask
    {
        public RepSumByNameTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override int DoLogic()
        {
            Report rp = ReportSumByName(RunData.Points.TaskStats);

            Console.WriteLine();
            Console.WriteLine("------------> Report Sum By (any) Name (" +
                rp.Count + " about " + rp.Reported + " out of " + rp.OutOf + ")");
            Console.WriteLine(rp.Text);
            Console.WriteLine();

            return 0;
        }

        /// <summary>
        /// Report statistics as a string, aggregate for tasks named the same.
        /// </summary>
        /// <param name="taskStats"></param>
        /// <returns>The report.</returns>
        protected virtual Report ReportSumByName(IList<TaskStats> taskStats)
        {
            // aggregate by task name
            int reported = 0;
            JCG.LinkedDictionary<string, TaskStats> p2 = new JCG.LinkedDictionary<string, TaskStats>();
            foreach (TaskStats stat1 in taskStats)
            {
                if (stat1.Elapsed >= 0)
                { // consider only tasks that ended
                    reported++;
                    string name = stat1.Task.GetName();
                    if (!p2.TryGetValue(name, out TaskStats stat2) || stat2 is null)
                    {
                        stat2 = (TaskStats)stat1.Clone();
                        p2[name] = stat2;
                    }
                    else
                    {
                        stat2.Add(stat1);
                    }
                }
            }
            // now generate report from secondary list p2    
            return GenPartialReport(reported, p2, taskStats.Count);
        }
    }
}
