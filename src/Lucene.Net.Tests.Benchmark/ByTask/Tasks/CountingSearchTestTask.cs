using Lucene.Net.Support;
using Lucene.Net.Support.Threading;

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
    /// Test Search task which counts number of searches.
    /// </summary>
    public class CountingSearchTestTask : SearchTask
    {
        public static int numSearches = 0;
        public static long startMillis;
        public static long lastMillis;
        public static long prevLastMillis;

        private static object syncLock = new object();

        public CountingSearchTestTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override int DoLogic()
        {
            int res = base.DoLogic();
            IncrNumSearches();
            return res;
        }

        private static void IncrNumSearches()
        {
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                prevLastMillis = lastMillis;
                lastMillis = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                if (0 == numSearches)
                {
                    startMillis = prevLastMillis = lastMillis;
                }
                numSearches++;
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }

        public long GetElapsedMillis()
        {
            return lastMillis - startMillis;
        }
    }
}
