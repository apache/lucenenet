using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Globalization;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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
            internal Entry next;

            public virtual Entry CreateNext(object o)
            {
                Entry e = new Entry();
                e.o = o;
                e.next = next;
                this.next = e;
                return e;
            }
        }

        [Ignore("// this shows an easy stack overflow because we're counting recursively.")]
        [Test]
        public virtual void TestChainedEstimation()
        {
            Random rnd = Random;
            Entry first = new Entry();
            try
            {
                while (true)
                {
                    // Check the current memory consumption and provide the estimate.
                    long jvmUsed = GC.GetTotalMemory(false);
                    long estimated = RamUsageEstimator.SizeOf(first);
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:0000000000}, {1:0000000000}", jvmUsed, estimated));

                    // Make a batch of objects.
                    for (int i = 0; i < 5000; i++)
                    {
                        first.CreateNext(new sbyte[rnd.Next(1024)]);
                    }
                }
            }
            catch (Exception e) when (e.IsOutOfMemoryError())
            {
                // Release and quit.
            }
        }

        internal volatile object guard;

        // this shows an easy stack overflow because we're counting recursively.
        [Test]
        public virtual void TestLargeSetOfByteArrays()
        {
            CauseGc();
            long before = GC.GetTotalMemory(false);
            object[] all = new object[1000000];
            for (int i = 0; i < all.Length; i++)
            {
                all[i] = new sbyte[Random.Next(3)];
            }
            CauseGc();
            long after = GC.GetTotalMemory(false);
            Console.WriteLine("mx:  " + RamUsageEstimator.HumanReadableUnits(after - before));
            Console.WriteLine("rue: " + RamUsageEstimator.HumanReadableUnits(ShallowSizeOf(all)));

            guard = all;
        }

        private long ShallowSizeOf(object[] all)
        {
            long s = RamUsageEstimator.ShallowSizeOf(all);
            foreach (object o in all)
            {
                s += RamUsageEstimator.ShallowSizeOf(o);
            }
            return s;
        }

        private long ShallowSizeOf(object[][] all)
        {
            long s = RamUsageEstimator.ShallowSizeOf(all);
            foreach (object[] o in all)
            {
                s += RamUsageEstimator.ShallowSizeOf(o);
                foreach (object o2 in o)
                {
                    s += RamUsageEstimator.ShallowSizeOf(o2);
                }
            }
            return s;
        }

        [Test]
        [Slow]
        public virtual void TestSimpleByteArrays()
        {
            object[][] all = new object[0][];
            try
            {
                while (true)
                {
                    // Check the current memory consumption and provide the estimate.
                    CauseGc();
                    
                    long estimated = ShallowSizeOf(all);
                    if (estimated > 50 * RamUsageEstimator.ONE_MB)
                    {
                        break;
                    }

                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}", RamUsageEstimator.HumanReadableUnits(GC.GetTotalMemory(false)).PadLeft(10, ' '), RamUsageEstimator.HumanReadableUnits(GC.MaxGeneration).PadLeft(10, ' '), RamUsageEstimator.HumanReadableUnits(estimated).PadLeft(10, ' ')));

                    // Make another batch of objects.
                    object[] seg = new object[10000];
                    all = Arrays.CopyOf(all, all.Length + 1);
                    all[all.Length - 1] = seg;
                    for (int i = 0; i < seg.Length; i++)
                    {
                        seg[i] = new sbyte[Random.Next(7)];
                    }
                }
            }
            catch (Exception e) when (e.IsOutOfMemoryError())
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
            GC.Collect();
        }
    }
}