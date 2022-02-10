using Lucene.Net.Benchmarks.ByTask.Stats;
using System;
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
    /// Report by-name-prefix statistics aggregated by name.
    /// <para/>
    /// Other side effects: None.
    /// </summary>
    public class RepSumByPrefTask : ReportTask
    {
        public RepSumByPrefTask(PerfRunData runData)
            : base(runData)
        {
        }

        protected string m_prefix;

        public override int DoLogic()
        {
            Report rp = ReportSumByPrefix(RunData.Points.TaskStats);

            Console.WriteLine();
            Console.WriteLine("------------> Report Sum By Prefix (" + m_prefix + ") (" +
                rp.Count + " about " + rp.Reported + " out of " + rp.OutOf + ")");
            Console.WriteLine(rp.Text);
            Console.WriteLine();

            return 0;
        }

        protected virtual Report ReportSumByPrefix(IList<TaskStats> taskStats)
        {
            // aggregate by task name
            int reported = 0;
            JCG.LinkedDictionary<string, TaskStats> p2 = new JCG.LinkedDictionary<string, TaskStats>();
            foreach (TaskStats stat1 in taskStats)
            {
                if (stat1.Elapsed >= 0 && stat1.Task.GetName().StartsWith(m_prefix, StringComparison.Ordinal))
                { // only ended tasks with proper name
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


        public virtual void SetPrefix(string prefix)
        {
            this.m_prefix = prefix;
        }

        /// <seealso cref="PerfTask.ToString()"/>
        public override string ToString()
        {
            return base.ToString() + " " + m_prefix;
        }
    }
}
