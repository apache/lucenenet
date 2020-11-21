using Lucene.Net.Benchmarks.ByTask.Stats;
using Lucene.Net.Benchmarks.ByTask.Utils;
using System;
using System.Collections.Generic;
using System.Text;
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
    /// Report (abstract) task - all report tasks extend this task.
    /// </summary>
    public abstract class ReportTask : PerfTask
    {
        protected ReportTask(PerfRunData runData) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : base(runData)
        {
        }

        /// <seealso cref="PerfTask.ShouldNeverLogAtStart"/>
        protected override bool ShouldNeverLogAtStart => true;

        /// <seealso cref="PerfTask.ShouldNotRecordStats"/>
        protected override bool ShouldNotRecordStats => true;

        /// <summary>
        /// From here start the code used to generate the reports.
        /// Subclasses would use this part to generate reports.
        /// </summary>
        protected static readonly string newline = Environment.NewLine;

        /// <summary>
        /// Get a textual summary of the benchmark results, average from all test runs.
        /// </summary>
        protected static readonly string OP = "Operation  ";
        protected static readonly string ROUND = " round";
        protected static readonly string RUNCNT = "   runCnt";
        protected static readonly string RECCNT = "   recsPerRun";
        protected static readonly string RECSEC = "        rec/s";
        protected static readonly string ELAPSED = "  elapsedSec";
        protected static readonly string USEDMEM = "    avgUsedMem";
        protected static readonly string TOTMEM = "    avgTotalMem";
        protected static readonly string[] COLS = {
            RUNCNT,
            RECCNT,
            RECSEC,
            ELAPSED,
            USEDMEM,
            TOTMEM
        };

        /// <summary>
        /// Compute a title line for a report table.
        /// </summary>
        /// <param name="longestOp">Size of longest op name in the table.</param>
        /// <returns>The table title line.</returns>
        protected virtual string TableTitle(string longestOp)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Formatter.Format(OP, longestOp));
            sb.Append(ROUND);
            sb.Append(RunData.Config.GetColsNamesForValsByRound());
            for (int i = 0; i < COLS.Length; i++)
            {
                sb.Append(COLS[i]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Find the longest op name out of completed tasks.
        /// </summary>
        /// <param name="taskStats">Completed tasks to be considered.</param>
        /// <returns>The longest op name out of completed tasks.</returns>
        protected virtual string LongestOp(IEnumerable<TaskStats> taskStats)
        {
            string longest = OP;
            foreach (TaskStats stat in taskStats)
            {
                if (stat.Elapsed >= 0)
                { // consider only tasks that ended
                    string name = stat.Task.GetName();
                    if (name.Length > longest.Length)
                    {
                        longest = name;
                    }
                }
            }
            return longest;
        }

        /// <summary>
        /// Compute a report line for the given task stat.
        /// </summary>
        /// <param name="longestOp">Size of longest op name in the table.</param>
        /// <param name="stat">Task stat to be printed.</param>
        /// <returns>The report line.</returns>
        protected virtual string TaskReportLine(string longestOp, TaskStats stat)
        {
            PerfTask task = stat.Task;
            StringBuilder sb = new StringBuilder();
            sb.Append(Formatter.Format(task.GetName(), longestOp));
            string round = (stat.Round >= 0 ? "" + stat.Round : "-");
            sb.Append(Formatter.FormatPaddLeft(round, ROUND));
            sb.Append(RunData.Config.GetColsValuesForValsByRound(stat.Round));
            sb.Append(Formatter.Format(stat.NumRuns, RUNCNT));
            sb.Append(Formatter.Format(stat.Count / stat.NumRuns, RECCNT));
            long elapsed = (stat.Elapsed > 0 ? stat.Elapsed : 1); // assume at least 1ms
            sb.Append(Formatter.Format(2, (float)(stat.Count * 1000.0 / elapsed), RECSEC));
            sb.Append(Formatter.Format(2, (float)stat.Elapsed / 1000, ELAPSED));
            sb.Append(Formatter.Format(0, (float)stat.MaxUsedMem / stat.NumRuns, USEDMEM));
            sb.Append(Formatter.Format(0, (float)stat.MaxTotMem / stat.NumRuns, TOTMEM));
            return sb.ToString();
        }

        protected virtual Report GenPartialReport(int reported, JCG.LinkedDictionary<string, TaskStats> partOfTasks, int totalSize)
        {
            string longetOp = LongestOp(partOfTasks.Values);
            bool first = true;
            StringBuilder sb = new StringBuilder();
            sb.Append(TableTitle(longetOp));
            sb.Append(newline);
            int lineNum = 0;
            foreach (TaskStats stat in partOfTasks.Values)
            {
                if (!first)
                {
                    sb.Append(newline);
                }
                first = false;
                string line = TaskReportLine(longetOp, stat);
                lineNum++;
                if (partOfTasks.Count > 2 && lineNum % 2 == 0)
                {
                    line = line.Replace("   ", " - ");
                }
                sb.Append(line);
                int[] byTime = stat.GetCountsByTime();
                if (byTime != null)
                {
                    sb.Append(newline);
                    int end = -1;
                    for (int i = byTime.Length - 1; i >= 0; i--)
                    {
                        if (byTime[i] != 0)
                        {
                            end = i;
                            break;
                        }
                    }
                    if (end != -1)
                    {
                        sb.Append("  by time:");
                        for (int i = 0; i < end; i++)
                        {
                            sb.Append(' ').Append(byTime[i]);
                        }
                    }
                }
            }

            string reptxt = (reported == 0 ? "No Matching Entries Were Found!" : sb.ToString());
            return new Report(reptxt, partOfTasks.Count, reported, totalSize);
        }
    }
}
