using Lucene.Net.Index;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Globalization;
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
    /// Spawns a BG thread that periodically (defaults to 3.0
    /// seconds, but accepts param in seconds) wakes up and asks
    /// IndexWriter for a near real-time reader.  Then runs a
    /// single query (body: 1) sorted by docdate, and prints
    /// time to reopen and time to run the search.
    /// <para/>
    /// @lucene.experimental It's also not generally usable, eg
    /// you cannot change which query is executed.
    /// </summary>
    public class NearRealtimeReaderTask : PerfTask
    {
        internal long pauseMSec = 3000L;

        internal int reopenCount;
        internal int[] reopenTimes = new int[1];

        public NearRealtimeReaderTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override int DoLogic()
        {
            PerfRunData runData = RunData;

            // Get initial reader
            IndexWriter w = runData.IndexWriter;
            if (w is null)
            {
                throw RuntimeException.Create("please open the writer before invoking NearRealtimeReader");
            }

            if (runData.GetIndexReader() != null)
            {
                throw RuntimeException.Create("please close the existing reader before invoking NearRealtimeReader");
            }


            long t = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            DirectoryReader r = DirectoryReader.Open(w, true);
            runData.SetIndexReader(r);
            // Transfer our reference to runData
            r.DecRef();

            // TODO: gather basic metrics for reporting -- eg mean,
            // stddev, min/max reopen latencies

            // Parent sequence sets stopNow
            reopenCount = 0;
            while (!Stop)
            {
                long waitForMsec = (pauseMSec - ((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - t)); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                if (waitForMsec > 0)
                {
                    Thread.Sleep((int)waitForMsec);
                    //System.out.println("NRT wait: " + waitForMsec + " msec");
                }

                t = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                DirectoryReader newReader = DirectoryReader.OpenIfChanged(r);
                if (newReader != null)
                {
                    int delay = (int)((J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) - t); // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                    if (reopenTimes.Length == reopenCount)
                    {
                        reopenTimes = ArrayUtil.Grow(reopenTimes, 1 + reopenCount);
                    }
                    reopenTimes[reopenCount++] = delay;
                    // TODO: somehow we need to enable warming, here
                    runData.SetIndexReader(newReader);
                    // Transfer our reference to runData
                    newReader.DecRef();
                    r = newReader;
                }
            }
            Stop = false;

            return reopenCount;
        }

        public override void SetParams(string @params)
        {
            base.SetParams(@params);
            pauseMSec = (long)(1000.0 * float.Parse(@params, CultureInfo.InvariantCulture));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Console.WriteLine("NRT reopen times:");
                for (int i = 0; i < reopenCount; i++)
                {
                    Console.Write(" " + reopenTimes[i]);
                }
                Console.WriteLine();
            }
            base.Dispose(disposing); // LUCENENET specific - disposable pattern requires calling the base class implementation
        }

        public override bool SupportsParams => true;
    }
}
