using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    public class StressRamUsageEstimator : LuceneTestCase
    {
        internal class Entry
        {
            internal object o;
            internal Entry next;

            public Entry CreateNext(object o)
            {
                var e = new Entry {o = o, next = next};
                this.next = e;
                return e;
            }
        }

        [Ignore]
        public void TestChainedEstimation()
        {
            MemoryMXBean memoryMXBean = ManagementFactory.GetMemoryMXBean();

            var rnd = new Random();
            var first = new Entry();

            try
            {
                while (true)
                {
                    // Check the current memory consumption and provide the estimate
                    var jvmUsed = memoryMXBean.GetHeapMemoryUsage().GetUsed();
                    var estimated = RamUsageEstimator.SizeOf(first);
                    Console.WriteLine("{0}, {1}", jvmUsed, estimated);

                    // Make a batch of objects
                    for (var i = 0; i < 5000; i++)
                    {
                        first.CreateNext(new sbyte[rnd.Next(1024)]);
                    }
                }
            }
            catch (OutOfMemoryException ex)
            {
                // Release and quit
            }
        }

        internal volatile object guard;

        // This shows an easy stack overflow because we're counting recursively.
        public void TestLargeSetOfByteArrays()
        {
            MemoryMXBean memoryMXBean = ManagementFactory.GetMemoryMXBean();

            CauseGC();
            var before = memoryMXBean.GetHeapMemoryUsage().GetUsed();
            var all = new object[1000000];
            for (var i = 0; i < all.Length; i++)
            {
                all[i] = new byte[new Random().Next(3)];
            }
            CauseGC();
            var after = memoryMXBean.GetHeapMemoryUsage().GetUsed();
            Console.WriteLine("mx:  " + RamUsageEstimator.HumanReadableUnits(after - before));
            Console.WriteLine("rue: " + RamUsageEstimator.HumanReadableUnits(ShallowSizeOf(all)));

            guard = all;
        }

        private long ShallowSizeOf(object[] all)
        {
            return RamUsageEstimator.ShallowSizeOf(all)
                + all.Sum(o => RamUsageEstimator.ShallowSizeOf(o));
        }

        private long ShallowSizeOf(object[][] all)
        {
            var s = RamUsageEstimator.ShallowSizeOf(all);
            foreach (var o in all)
            {
                s += RamUsageEstimator.ShallowSizeOf(o);
                s += o.Sum(o2 => RamUsageEstimator.ShallowSizeOf(o2));
            }
            return s;
        }

        public void TestSimpleByteArrays()
        {
            MemoryMXBean memoryMXBean = ManagementFactory.GetMemoryMXBean();

            var all = new object[0][];
            try
            {
                while (true)
                {
                    // Check the current memory consumption and provide the estimate.
                    CauseGc();
                    var mu = memoryMXBean.GetHeapMemoryUsage();
                    var estimated = ShallowSizeOf(all);
                    if (estimated > 50 * RamUsageEstimator.ONE_MB)
                    {
                        break;
                    }

                    Console.WriteLine("{0}\t{1}]t{2}",
                        RamUsageEstimator.HumanReadableUnits(mu.GetUsed()),
                        RamUsageEstimator.HumanReadableUnits(mu.GetMax()),
                        RamUsageEstimator.HumanReadableUnits(estimated));

                    // Make another batch of objects.
                    var seg = new object[10000];
                    all = Arrays.CopyOf(all, all.Length + 1);
                    all[all.Length - 1] = seg;
                    for (var i = 0; i < seg.Length; i++)
                    {
                        seg[i] = new byte[new Random().Next(7)];
                    }
                }
            }
            catch (OutOfMemoryException ex)
            {
                // Release and quit.
            }
        }

        private void CauseGc()
        {
            var garbageCollectorMXBeans = ManagementFactory.GetGarbageCollectorMXBeans();
            var ccounts = new List<long>();
            foreach (var g in garbageCollectorMXBeans)
            {
                ccounts.Add(g.GetCollectionCount());
            }
            var ccounts2 = new List<long>();
            do
            {
                GC.Collect();
                ccounts.Clear();
                foreach (var g in garbageCollectorMXBeans)
                {
                    ccounts2.Add(g.GetCollectionCount());
                }
            } while (ccounts2.Equals(ccounts));
        }
    }
}
