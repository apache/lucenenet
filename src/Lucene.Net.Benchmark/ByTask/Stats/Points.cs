using Lucene.Net.Benchmarks.ByTask.Tasks;
using Lucene.Net.Benchmarks.ByTask.Utils;
using System.Collections.Generic;

namespace Lucene.Net.Benchmarks.ByTask.Stats
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
    /// Test run data points collected as the test proceeds.
    /// </summary>
    public class Points
    {
        // stat points ordered by their start time. 
        // for now we collect points as TaskStats objects.
        // later might optimize to collect only native data.
        private readonly List<TaskStats> points = new List<TaskStats>(); // LUCENENET: marked readonly

        private int nextTaskRunNum = 0;

        private TaskStats currentStats;

        /// <summary>
        /// Create a Points statistics object.
        /// </summary>
#pragma warning disable IDE0060 // Remove unused parameter
        public Points(Config config)
#pragma warning restore IDE0060 // Remove unused parameter
        {
        }

        /// <summary>
        /// Gets the current task stats.
        /// The actual task stats are returned, so caller should not modify this task stats.
        /// </summary>
        public virtual IList<TaskStats> TaskStats => points;

        /// <summary>
        /// Mark that a task is starting.
        /// Create a task stats for it and store it as a point.
        /// </summary>
        /// <param name="task">The starting task.</param>
        /// <param name="round">The new task stats created for the starting task.</param>
        /// <returns></returns>
        public virtual TaskStats MarkTaskStart(PerfTask task, int round)
        {
            lock (this)
            {
                TaskStats stats = new TaskStats(task, NextTaskRunNum(), round);
                this.currentStats = stats;
                points.Add(stats);
                return stats;
            }
        }

        public virtual TaskStats CurrentStats => currentStats;

        // return next task num
        private int NextTaskRunNum()
        {
            lock (this)
            {
                return nextTaskRunNum++;
            }
        }

        /// <summary>
        /// mark the end of a task
        /// </summary>
        public virtual void MarkTaskEnd(TaskStats stats, int count)
        {
            lock (this)
            {
                int numParallelTasks = nextTaskRunNum - 1 - stats.TaskRunNum;
                // note: if the stats were cleared, might be that this stats object is 
                // no longer in points, but this is just ok.
                stats.MarkEnd(numParallelTasks, count);
            }
        }

        /// <summary>
        /// Clear all data, prepare for more tests.
        /// </summary>
        public virtual void ClearData()
        {
            points.Clear();
        }
    }
}
