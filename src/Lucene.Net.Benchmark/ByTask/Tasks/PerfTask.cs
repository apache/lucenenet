using Lucene.Net.Benchmarks.ByTask.Stats;
using Lucene.Net.Benchmarks.ByTask.Utils;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
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
    /// An abstract task to be tested for performance.
    /// </summary>
    /// <remarks>
    /// Every performance task extends this class, and provides its own
    /// <see cref="DoLogic()"/> method, which performs the actual task.
    /// <para/>
    /// Tasks performing some work that should be measured for the task, can override
    /// <see cref="Setup()"/> and/or <see cref="TearDown()"/> and place that work there.
    /// <para/>
    /// Relevant properties:
    /// <list type="bullet">
    ///     <item><term>task.max.depth.log</term><description></description></item>
    /// </list>
    /// <para/>
    /// Also supports the following logging attributes:
    /// <list type="bullet">
    ///     <item><term>log.step</term><description>
    ///         specifies how often to log messages about the current running
    ///         task. Default is 1000 <see cref="DoLogic()"/> invocations. Set to -1 to disable
    ///         logging.
    ///     </description></item>
    ///     <item><term>log.step.[class Task Name]</term><description>
    ///         specifies the same as 'log.step', only for a
    ///         particular task name. For example, log.step.AddDoc will be applied only for
    ///         <see cref="AddDocTask"/>. It's a way to control
    ///         per task logging settings. If you want to omit logging for any other task,
    ///         include log.step=-1. The syntax is "log.step." together with the Task's
    ///         'short' name (i.e., without the 'Task' part).
    ///     </description></item>
    /// </list>
    /// </remarks>
    public abstract class PerfTask : IDisposable // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        internal const int DEFAULT_LOG_STEP = 1000;

        private readonly PerfRunData runData;

        // propeties that all tasks have
        private string name;
        private int depth = 0;
        protected int m_logStep;
        private int logStepCount = 0;
        private readonly int maxDepthLogStart = 0; // LUCENENET: marked readonly
        private bool disableCounting = false;
        protected string m_params = null;

        private bool runInBackground;
        private int deltaPri;

        // The first line of this task's definition in the alg file
        private int algLineNum = 0;

        protected static readonly string NEW_LINE = Environment.NewLine;

        /// <summary>
        /// Should not be used externally
        /// </summary>
        private PerfTask()
        {
            name = GetType().Name;
            if (name.EndsWith("Task", StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - 4);
            }
        }

        public virtual void SetRunInBackground(int deltaPri)
        {
            runInBackground = true;
            this.deltaPri = deltaPri;
        }

        public virtual bool RunInBackground => runInBackground;

        public virtual int BackgroundDeltaPriority => deltaPri;

        // LUCENENET specific - made private and
        // added Stop property because volatile
        // fields cannot be protected.
        private volatile bool stopNow;

        protected bool Stop
        {
            get => stopNow;
            set => stopNow = value;
        }
        public virtual void StopNow()
        {
            stopNow = true;
        }

        protected PerfTask(PerfRunData runData)
            : this()
        {
            this.runData = runData;
            Config config = runData.Config;
            this.maxDepthLogStart = config.Get("task.max.depth.log", 0);

            string logStepAtt = "log.step";
            string taskLogStepAtt = "log.step." + name;
            if (config.Get(taskLogStepAtt, null) != null)
            {
                logStepAtt = taskLogStepAtt;
            }

            // It's important to read this from Config, to support vals-by-round.
            m_logStep = config.Get(logStepAtt, DEFAULT_LOG_STEP);
            // To avoid the check 'if (logStep > 0)' in tearDown(). This effectively
            // turns logging off.
            if (m_logStep <= 0)
            {
                m_logStep = int.MaxValue;
            }
        }

        public virtual object Clone()
        {
            // tasks having non primitive data structures should override this.
            // otherwise parallel running of a task sequence might not run correctly. 
            return (PerfTask)base.MemberwiseClone();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Run the task, record statistics.
        /// </summary>
        /// <param name="reportStats"></param>
        /// <returns>Number of work items done by this task.</returns>
        public int RunAndMaybeStats(bool reportStats)
        {
            int count;
            if (!reportStats || ShouldNotRecordStats)
            {
                Setup();
                count = DoLogic();
                count = disableCounting ? 0 : count;
                TearDown();
                return count;
            }
            if (reportStats && depth <= maxDepthLogStart && !ShouldNeverLogAtStart)
            {
                Console.WriteLine("------------> starting task: " + GetName());
            }
            Setup();
            Points pnts = runData.Points;
            TaskStats ts = pnts.MarkTaskStart(this, runData.Config.RoundNumber);
            count = DoLogic();
            count = disableCounting ? 0 : count;
            pnts.MarkTaskEnd(ts, count);
            TearDown();
            return count;
        }

        /// <summary>
        /// Perform the task once (ignoring repetitions specification).
        /// Return number of work items done by this task.
        /// For indexing that can be number of docs added.
        /// For warming that can be number of scanned items, etc.
        /// </summary>
        /// <returns>Number of work items done by this task.</returns>
        public abstract int DoLogic();

        /// <summary>
        /// Returns the name.
        /// </summary>
        public virtual string GetName()
        {
            if (m_params is null)
            {
                return name;
            }
            return new StringBuilder(name).Append('(').Append(m_params).Append(')').ToString();
        }

        /// <summary>
        /// Sets the name.
        /// </summary>
        /// <param name="name">The name to set.</param>
        protected virtual void SetName(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Gets the run data.
        /// </summary>
        public virtual PerfRunData RunData => runData;

        /// <summary>
        /// Gets or Sets the depth.
        /// </summary>
        public virtual int Depth
        {
            get => depth;
            set => depth = value;
        }

        // compute a blank string padding for printing this task indented by its depth  
        internal string GetPadding()
        {
            char[] c = new char[4 * Depth];
            for (int i = 0; i < c.Length; i++) c[i] = ' ';
            return new string(c);
        }

        public override string ToString()
        {
            string padd = GetPadding();
            StringBuilder sb = new StringBuilder(padd);
            if (disableCounting)
            {
                sb.Append('-');
            }
            sb.Append(GetName());
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
        /// Returns the maxDepthLogStart.
        /// </summary>
        internal int MaxDepthLogStart => maxDepthLogStart;

        protected virtual string GetLogMessage(int recsCount)
        {
            return "processed " + recsCount + " records";
        }

        /// <summary>
        /// Tasks that should never log at start can override this.
        /// Returns <c>true</c> if this task should never log when it start.
        /// </summary>
        protected virtual bool ShouldNeverLogAtStart => false;

        /// <summary>
        /// Tasks that should not record statistics can override this. 
        /// Returns <c>true</c> if this task should never record its statistics.
        /// </summary>
        protected virtual bool ShouldNotRecordStats => false;

        /// <summary>
        /// Task setup work that should not be measured for that specific task. By
        /// default it does nothing, but tasks can implement this, moving work from
        /// <see cref="DoLogic()"/> to this method. Only the work done in <see cref="DoLogic()"/>
        /// is measured for this task. Notice that higher level (sequence) tasks
        /// containing this task would then measure larger time than the sum of their
        /// contained tasks.
        /// </summary>
        public virtual void Setup()
        {
        }

        /// <summary>
        /// Task teardown work that should not be measured for that specific task. By
        /// default it does nothing, but tasks can implement this, moving work from
        /// <see cref="DoLogic()"/> to this method. Only the work done in <see cref="DoLogic()"/>
        /// is measured for this task. Notice that higher level (sequence) tasks
        /// containing this task would then measure larger time than the sum of their        
        /// contained tasks.
        /// </summary>
        public virtual void TearDown()
        {
            if (++logStepCount % m_logStep == 0)
            {
                double time = ((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - runData.StartTimeMillis) / 1000.0; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:0000000.00}", time) + " sec --> "
                    + Thread.CurrentThread.Name + " " + GetLogMessage(logStepCount));
            }
        }

        /// <summary>
        /// Sub classes that support parameters must override this method to return
        /// <c>true</c> if this task supports command line params.
        /// </summary>
        public virtual bool SupportsParams => false;

        /// <summary>
        /// Set the params of this task.
        /// </summary>
        /// <exception cref="NotSupportedException">For tasks supporting command line parameters.</exception>
        public virtual void SetParams(string @params)
        {
            if (!SupportsParams)
            {
                throw UnsupportedOperationException.Create(GetName() + " does not support command line parameters.");
            }
            this.m_params = @params;
        }

        /// <summary>
        /// Gets the Params.
        /// </summary>
        public virtual string Params => m_params;

        /// <summary>
        /// Return <c>true</c> if counting is disabled for this task.
        /// </summary>
        public virtual bool DisableCounting
        {
            get => disableCounting;
            set => disableCounting = value;
        }

        public virtual int AlgLineNum
        {
            get => algLineNum;
            set => algLineNum = value;
        }
    }
}
