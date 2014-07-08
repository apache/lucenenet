using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Lucene.Net.Util
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
    /// Estimates how <seealso cref="RamUsageEstimator"/> estimates physical memory consumption
    /// of Java objects. 
    /// </summary>
    [TestFixture]
    public class StressRamUsageEstimator : LuceneTestCase
    {
        internal class Entry
        {
            internal object o;
            internal Entry Next;

            public virtual Entry CreateNext(object o)
            {
                Entry e = new Entry();
                e.o = o;
                e.Next = Next;
                this.Next = e;
                return e;
            }
        }

        // this shows an easy stack overflow because we're counting recursively.
        [Ignore]
        [Test]
        public virtual void TestChainedEstimation()
        {
            MemoryMXBean memoryMXBean = ManagementFactory.MemoryMXBean;

            Random rnd = Random();
            Entry first = new Entry();
            try
            {
                while (true)
                {
                    // Check the current memory consumption and provide the estimate.
                    long jvmUsed = memoryMXBean.HeapMemoryUsage.Used;
                    long estimated = RamUsageEstimator.sizeOf(first);
                    Console.WriteLine(string.format(Locale.ROOT, "%10d, %10d", jvmUsed, estimated));

                    // Make a batch of objects.
                    for (int i = 0; i < 5000; i++)
                    {
                        first.CreateNext(new sbyte[rnd.Next(1024)]);
                    }
                }
            }
            catch (System.OutOfMemoryException e)
            {
                // Release and quit.
            }
        }

        internal volatile object Guard;

        // this shows an easy stack overflow because we're counting recursively.
        [Test]
        public virtual void TestLargeSetOfByteArrays()
        {
            MemoryMXBean memoryMXBean = ManagementFactory.MemoryMXBean;

            CauseGc();
            long before = memoryMXBean.HeapMemoryUsage.Used;
            object[] all = new object[1000000];
            for (int i = 0; i < all.Length; i++)
            {
                all[i] = new sbyte[Random().Next(3)];
            }
            CauseGc();
            long after = memoryMXBean.HeapMemoryUsage.Used;
            Console.WriteLine("mx:  " + RamUsageEstimator.humanReadableUnits(after - before));
            Console.WriteLine("rue: " + RamUsageEstimator.humanReadableUnits(ShallowSizeOf(all)));

            Guard = all;
        }

        private long ShallowSizeOf(object[] all)
        {
            long s = RamUsageEstimator.shallowSizeOf(all);
            foreach (object o in all)
            {
                s += RamUsageEstimator.shallowSizeOf(o);
            }
            return s;
        }

        private long ShallowSizeOf(object[][] all)
        {
            long s = RamUsageEstimator.shallowSizeOf(all);
            foreach (object[] o in all)
            {
                s += RamUsageEstimator.shallowSizeOf(o);
                foreach (object o2 in o)
                {
                    s += RamUsageEstimator.shallowSizeOf(o2);
                }
            }
            return s;
        }

        [Test]
        public virtual void TestSimpleByteArrays()
        {
            MemoryMXBean memoryMXBean = ManagementFactory.MemoryMXBean;

            object[][] all = new object[0][];
            try
            {
                while (true)
                {
                    // Check the current memory consumption and provide the estimate.
                    CauseGc();
                    MemoryUsage mu = memoryMXBean.HeapMemoryUsage;
                    long estimated = ShallowSizeOf(all);
                    if (estimated > 50 * RamUsageEstimator.ONE_MB)
                    {
                        break;
                    }

                    Console.WriteLine(string.format(Locale.ROOT, "%10s\t%10s\t%10s", RamUsageEstimator.humanReadableUnits(mu.Used), RamUsageEstimator.humanReadableUnits(mu.Max), RamUsageEstimator.humanReadableUnits(estimated)));

                    // Make another batch of objects.
                    object[] seg = new object[10000];
                    all = Arrays.copyOf(all, all.Length + 1);
                    all[all.Length - 1] = seg;
                    for (int i = 0; i < seg.Length; i++)
                    {
                        seg[i] = new sbyte[Random().Next(7)];
                    }
                }
            }
            catch (System.OutOfMemoryException e)
            {
                // Release and quit.
            }
        }

        /// <summary>
        /// Very hacky, very crude, but (sometimes) works. 
        /// Don't look, it will burn your eyes out. 
        /// </summary>
        private void CauseGc()
        {
            IList<GarbageCollectorMXBean> garbageCollectorMXBeans = ManagementFactory.GarbageCollectorMXBeans;
            IList<long?> ccounts = new List<long?>();
            foreach (GarbageCollectorMXBean g in garbageCollectorMXBeans)
            {
                ccounts.Add(g.CollectionCount);
            }
            IList<long?> ccounts2 = new List<long?>();
            do
            {
                System.gc();
                ccounts.Clear();
                foreach (GarbageCollectorMXBean g in garbageCollectorMXBeans)
                {
                    ccounts2.Add(g.CollectionCount);
                }
            } while (ccounts2.Equals(ccounts));
        }
    }

}