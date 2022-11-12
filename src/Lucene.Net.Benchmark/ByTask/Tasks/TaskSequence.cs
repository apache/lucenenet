using J2N.Threading;
using Lucene.Net.Benchmarks.ByTask.Feeds;
using Lucene.Net.Benchmarks.ByTask.Stats;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
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
    /// Sequence of parallel or sequential tasks.
    /// </summary>
    public class TaskSequence : PerfTask
    {
        public static int REPEAT_EXHAUST = -2;
        private IList<PerfTask> tasks;
        private int repetitions = 1;
        private readonly bool parallel;
        private readonly TaskSequence parent;
        private bool letChildReport = true;
        private int rate = 0;
        private bool perMin = false; // rate, if set, is, by default, be sec.
        private string seqName;
        private bool exhausted = false;
        private bool resetExhausted = false;
        private PerfTask[] tasksArray;
        private bool anyExhaustibleTasks;
        private readonly bool collapsable = false; // to not collapse external sequence named in alg.  

        private bool fixedTime;                      // true if we run for fixed time
        private double runTimeSec;                      // how long to run for
        private readonly long logByTimeMsec;

        public TaskSequence(PerfRunData runData, string name, TaskSequence parent, bool parallel)
            : base(runData)
        {
            collapsable = name is null;
            name = name ?? (parallel ? "Par" : "Seq");
            SetName(name);
            SetSequenceName();
            this.parent = parent;
            this.parallel = parallel;
            tasks = new JCG.List<PerfTask>();
            logByTimeMsec = runData.Config.Get("report.time.step.msec", 0);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                InitTasksArray();
                for (int i = 0; i < tasksArray.Length; i++)
                {
                    tasksArray[i].Dispose();
                }
                RunData.DocMaker.Dispose();
            }
            base.Dispose(disposing); // LUCENENET specific - disposable pattern requires calling the base class implementation
        }

        private void InitTasksArray()
        {
            if (tasksArray is null)
            {
                int numTasks = tasks.Count;
                tasksArray = new PerfTask[numTasks];
                for (int k = 0; k < numTasks; k++)
                {
                    tasksArray[k] = tasks[k];
                    anyExhaustibleTasks |= tasksArray[k] is ResetInputsTask;
                    anyExhaustibleTasks |= tasksArray[k] is TaskSequence;
                }
            }
            if (!parallel && logByTimeMsec != 0 && !letChildReport)
            {
                countsByTime = new int[1];
            }
        }

        /// <summary>
        /// Gets the parallel.
        /// </summary>
        public virtual bool IsParallel => parallel;

        /// <summary>
        /// Gets the repetitions.
        /// </summary>
        public virtual int Repetitions => repetitions;

        private int[] countsByTime;

        public virtual void SetRunTime(double sec)
        {
            runTimeSec = sec;
            fixedTime = true;
        }

        /// <summary>
        /// Sets the repetitions.
        /// </summary>
        /// <param name="repetitions">The repetitions to set.</param>
        public virtual void SetRepetitions(int repetitions)
        {
            fixedTime = false;
            this.repetitions = repetitions;
            if (repetitions == REPEAT_EXHAUST)
            {
                if (IsParallel)
                {
                    throw new Exception("REPEAT_EXHAUST is not allowed for parallel tasks");
                }
            }
            SetSequenceName();
        }

        /// <summary>
        /// Gets the parent.
        /// </summary>
        public virtual TaskSequence Parent => parent;

        /// <seealso cref="PerfTask.DoLogic()"/>
        public override int DoLogic()
        {
            exhausted = resetExhausted = false;
            return (parallel ? DoParallelTasks() : DoSerialTasks());
        }

        private class RunBackgroundTask : ThreadJob
        {
            private readonly PerfTask task;
            private readonly bool letChildReport;
            private volatile int count;

            public RunBackgroundTask(PerfTask task, bool letChildReport)
            {
                this.task = task;
                this.letChildReport = letChildReport;
            }

            public virtual void StopNow()
            {
                task.StopNow();
            }

            public virtual int Count => count;

            public override void Run()
            {
                try
                {
                    count = task.RunAndMaybeStats(letChildReport);
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }

        private int DoSerialTasks()
        {
            if (rate > 0)
            {
                return DoSerialTasksWithRate();
            }

            InitTasksArray();
            int count = 0;

            long runTime = (long)(runTimeSec * 1000);
            IList<RunBackgroundTask> bgTasks = null;

            long t0 = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            for (int k = 0; fixedTime || (repetitions == REPEAT_EXHAUST && !exhausted) || k < repetitions; k++)
            {
                if (Stop)
                {
                    break;
                }
                for (int l = 0; l < tasksArray.Length; l++)
                {
                    PerfTask task = tasksArray[l];
                    if (task.RunInBackground)
                    {
                        if (bgTasks is null)
                        {
                            bgTasks = new JCG.List<RunBackgroundTask>();
                        }
                        RunBackgroundTask bgTask = new RunBackgroundTask(task, letChildReport);
                        bgTask.Priority = (task.BackgroundDeltaPriority + Thread.CurrentThread.Priority);
                        bgTask.Start();
                        bgTasks.Add(bgTask);
                    }
                    else
                    {
                        try
                        {
                            int inc = task.RunAndMaybeStats(letChildReport);
                            count += inc;
                            if (countsByTime != null)
                            {
                                int slot = (int)(((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - t0) / logByTimeMsec); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                                if (slot >= countsByTime.Length)
                                {
                                    countsByTime = ArrayUtil.Grow(countsByTime, 1 + slot);
                                }
                                countsByTime[slot] += inc;
                            }
                            if (anyExhaustibleTasks)
                                UpdateExhausted(task);
                        }
                        catch (NoMoreDataException /*e*/)
                        {
                            exhausted = true;
                        }
                    }
                }
                if (fixedTime && (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - t0 > runTime) // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                {
                    repetitions = k + 1;
                    break;
                }
            }

            if (bgTasks != null)
            {
                foreach (RunBackgroundTask bgTask in bgTasks)
                {
                    bgTask.StopNow();
                }
                foreach (RunBackgroundTask bgTask in bgTasks)
                {
                    bgTask.Join();
                    count += bgTask.Count;
                }
            }

            if (countsByTime != null)
            {
                RunData.Points.CurrentStats.SetCountsByTime(countsByTime, logByTimeMsec);
            }

            Stop = false;

            return count;
        }

        private int DoSerialTasksWithRate()
        {
            InitTasksArray();
            long delayStep = (perMin ? 60000 : 1000) / rate;
            long nextStartTime = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            int count = 0;
            long t0 = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            for (int k = 0; (repetitions == REPEAT_EXHAUST && !exhausted) || k < repetitions; k++)
            {
                if (Stop)
                {
                    break;
                }
                for (int l = 0; l < tasksArray.Length; l++)
                {
                    PerfTask task = tasksArray[l];
                    while (!Stop)
                    {
                        long waitMore = nextStartTime - (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                        if (waitMore > 0)
                        {
                            // TODO: better to use condition to notify
                            Thread.Sleep(1);
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (Stop)
                    {
                        break;
                    }
                    nextStartTime += delayStep; // this aims at avarage rate. 
                    try
                    {
                        int inc = task.RunAndMaybeStats(letChildReport);
                        count += inc;
                        if (countsByTime != null)
                        {
                            int slot = (int)(((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - t0) / logByTimeMsec); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                            if (slot >= countsByTime.Length)
                            {
                                countsByTime = ArrayUtil.Grow(countsByTime, 1 + slot);
                            }
                            countsByTime[slot] += inc;
                        }

                        if (anyExhaustibleTasks)
                            UpdateExhausted(task);
                    }
                    catch (NoMoreDataException /*e*/)
                    {
                        exhausted = true;
                    }
                }
            }
            Stop = false;
            return count;
        }

        // update state regarding exhaustion.
        private void UpdateExhausted(PerfTask task)
        {
            if (task is ResetInputsTask)
            {
                exhausted = false;
                resetExhausted = true;
            }
            else if (task is TaskSequence t)
            {
                if (t.resetExhausted)
                {
                    exhausted = false;
                    resetExhausted = true;
                    t.resetExhausted = false;
                }
                else
                {
                    exhausted |= t.exhausted;
                }
            }
        }

        private class ParallelTask : ThreadJob
        {
            private int count;
            private readonly PerfTask task;
            private readonly TaskSequence outerInstance;

            // LUCENENET specific - expose field through property
            public int Count => count;

            // LUCENENET specific - expose field through property
            public PerfTask Task => task;

            public ParallelTask(TaskSequence outerInstance, PerfTask task)
            {
                this.outerInstance = outerInstance;
                this.task = task;
            }

            public override void Run()
            {
                try
                {
                    int n = task.RunAndMaybeStats(outerInstance.letChildReport);
                    if (outerInstance.anyExhaustibleTasks)
                    {
                        outerInstance.UpdateExhausted(task);
                    }
                    count += n;
                }
                catch (NoMoreDataException)
                {
                    outerInstance.exhausted = true;
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }

        public override void StopNow()
        {
            base.StopNow();
            // Forwards top request to children
            if (runningParallelTasks != null)
            {
                foreach (ParallelTask t in runningParallelTasks)
                {
                    if (t != null)
                    {
                        t.Task.StopNow();
                    }
                }
            }
        }

        private ParallelTask[] runningParallelTasks;

        private int DoParallelTasks()
        {

            TaskStats stats = RunData.Points.CurrentStats;

            InitTasksArray();
            ParallelTask[] t = runningParallelTasks = new ParallelTask[repetitions * tasks.Count];
            // prepare threads
            int index = 0;
            for (int k = 0; k < repetitions; k++)
            {
                for (int i = 0; i < tasksArray.Length; i++)
                {
                    PerfTask task = (PerfTask)(tasksArray[i].Clone());
                    t[index++] = new ParallelTask(this, task);
                }
            }
            // run threads
            StartThreads(t);

            if (Stop)
            {
                foreach (ParallelTask task in t)
                {
                    task.Task.StopNow();
                }
            }

            // wait for all threads to complete
            int count = 0;
            for (int i = 0; i < t.Length; i++)
            {
                t[i].Join();
                count += t[i].Count;
                if (t[i].Task is TaskSequence sub && sub.countsByTime != null)
                {
                    if (countsByTime is null)
                    {
                        countsByTime = new int[sub.countsByTime.Length];
                    }
                    else if (countsByTime.Length < sub.countsByTime.Length)
                    {
                        countsByTime = ArrayUtil.Grow(countsByTime, sub.countsByTime.Length);
                    }
                    for (int j = 0; j < sub.countsByTime.Length; j++)
                    {
                        countsByTime[j] += sub.countsByTime[j];
                    }
                }
            }

            if (countsByTime != null)
            {
                stats.SetCountsByTime(countsByTime, logByTimeMsec);
            }

            // return total count
            return count;
        }

        // run threads
        private void StartThreads(ParallelTask[] t)
        {
            if (rate > 0)
            {
                StartlThreadsWithRate(t);
                return;
            }
            for (int i = 0; i < t.Length; i++)
            {
                t[i].Start();
            }
        }

        // run threads with rate
        private void StartlThreadsWithRate(ParallelTask[] t)
        {
            long delayStep = (perMin ? 60000 : 1000) / rate;
            long nextStartTime = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            for (int i = 0; i < t.Length; i++)
            {
                long waitMore = nextStartTime - (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                if (waitMore > 0)
                {
                    Thread.Sleep((int)waitMore);
                }
                nextStartTime += delayStep; // this aims at average rate of starting threads. 
                t[i].Start();
            }
        }

        public virtual void AddTask(PerfTask task)
        {
            tasks.Add(task);
            task.Depth = Depth + 1;
        }

        /// <seealso cref="object.ToString()"/>
        public override string ToString()
        {
            string padd = GetPadding();
            StringBuilder sb = new StringBuilder(base.ToString());
            sb.Append(parallel ? " [" : " {");
            sb.Append(NEW_LINE);
            foreach (PerfTask task in tasks)
            {
                sb.Append(task.ToString());
                sb.Append(NEW_LINE);
            }
            sb.Append(padd);
            sb.Append(!letChildReport ? ">" : (parallel ? "]" : "}"));
            if (fixedTime)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, " {0:N}s", runTimeSec);
            }
            else if (repetitions > 1)
            {
                sb.Append(" * " + repetitions);
            }
            else if (repetitions == REPEAT_EXHAUST)
            {
                sb.Append(" * EXHAUST");
            }
            if (rate > 0)
            {
                sb.Append(",  rate: " + rate + "/" + (perMin ? "min" : "sec"));
            }
            if (RunInBackground)
            {
                sb.Append(" &");
                int x = BackgroundDeltaPriority;
                if (x != 0)
                {
                    sb.Append(x);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Execute child tasks in a way that they do not report their time separately.
        /// </summary>
        public virtual void SetNoChildReport()
        {
            letChildReport = false;
            foreach (PerfTask task in tasks)
            {
                if (task is TaskSequence taskSequence)
                {
                    taskSequence.SetNoChildReport();
                }
            }
        }

        /// <summary>
        /// Returns the rate per minute: how many operations should be performed in a minute.
        /// If 0 this has no effect.
        /// </summary>
        /// <returns>The rate per min: how many operations should be performed in a minute.</returns>
        public virtual int GetRate()
        {
            return (perMin ? rate : 60 * rate);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rate">The rate to set.</param>
        /// <param name="perMin"></param>
        public virtual void SetRate(int rate, bool perMin)
        {
            this.rate = rate;
            this.perMin = perMin;
            SetSequenceName();
        }

        private void SetSequenceName()
        {
            seqName = base.GetName();
            if (repetitions == REPEAT_EXHAUST)
            {
                seqName += "_Exhaust";
            }
            else if (repetitions > 1)
            {
                seqName += "_" + repetitions;
            }
            if (rate > 0)
            {
                seqName += "_" + rate + (perMin ? "/min" : "/sec");
            }
            if (parallel && seqName.ToLowerInvariant().IndexOf("par", StringComparison.Ordinal) < 0)
            {
                seqName += "_Par";
            }
        }

        public override string GetName()
        {
            return seqName; // override to include more info 
        }

        /// <summary>
        /// Gets the tasks.
        /// </summary>
        public virtual IList<PerfTask> Tasks => tasks;

        public override object Clone()
        {
            TaskSequence res = (TaskSequence)base.Clone();
            res.tasks = new JCG.List<PerfTask>();
            for (int i = 0; i < tasks.Count; i++)
            {
                res.tasks.Add((PerfTask)tasks[i].Clone());
            }
            return res;
        }

        /// <summary>
        /// Return <c>true</c> if can be collapsed in case it is outermost sequence.
        /// </summary>
        public virtual bool IsCollapsable => collapsable;
    }
}
