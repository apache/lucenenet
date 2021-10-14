using Lucene.Net.Support.Threading;
using System;
using System.Globalization;
using System.Threading;

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
    /// Simply waits for the specified (via the parameter) amount
    /// of time.  For example Wait(30s) waits for 30 seconds.
    /// This is useful with background tasks to control how long
    /// the tasks run.
    /// <para/>
    /// You can specify h, m, or s (hours, minutes, seconds) as
    /// the trailing time unit.  No unit is interpreted as
    /// seconds.
    /// </summary>
    public class WaitTask : PerfTask
    {
        private double waitTimeSec;

        public WaitTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override void SetParams(string @params)
        {
            base.SetParams(@params);
            if (@params != null)
            {
                int multiplier;
                if (@params.EndsWith("s", StringComparison.Ordinal))
                {
                    multiplier = 1;
                    @params = @params.Substring(0, @params.Length - 1);
                }
                else if (@params.EndsWith("m", StringComparison.Ordinal))
                {
                    multiplier = 60;
                    @params = @params.Substring(0, @params.Length - 1);
                }
                else if (@params.EndsWith("h", StringComparison.Ordinal))
                {
                    multiplier = 3600;
                    @params = @params.Substring(0, @params.Length - 1);
                }
                else
                {
                    // Assume seconds
                    multiplier = 1;
                }

                waitTimeSec = double.Parse(@params, CultureInfo.InvariantCulture) * multiplier;
            }
            else
            {
                throw new ArgumentException("you must specify the wait time, eg: 10.0s, 4.5m, 2h");
            }
        }

        public override int DoLogic()
        {
            Thread.Sleep((int)(1000 * waitTimeSec));
            return 0;
        }

        public override bool SupportsParams => true;
    }
}
