using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestLurchTableThreading
    {
        private const int MAXTHREADS = 8;
        private const int COUNT = 1000;
        static LurchTable<Guid, T> CreateMap<T>()
        {
            var ht = new LurchTable<Guid, T>(COUNT, LurchTableOrder.Access);
            return ht;
        }

        private static void Parallel<T>(int loopCount, T[] args, Action<T> task)
        {
            var timer = Stopwatch.StartNew();
            int[] ready = new[] { 0 };
            ManualResetEvent start = new ManualResetEvent(false);
            int nthreads = Math.Min(MAXTHREADS, args.Length);
            var threads = new Thread[nthreads];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread((ithread) =>
                {
                    Interlocked.Increment(ref ready[0]);
                    start.WaitOne();
                    for (int loop = 0; loop < loopCount; loop++)
                        for (int ix = (int)ithread; ix < args.Length; ix += nthreads)
                            task(args[ix]);
                });
            }

            int threadIx = 0;
            foreach (var t in threads)
                t.Start(threadIx++);

            while (Interlocked.CompareExchange(ref ready[0], 0, 0) < nthreads)
                Thread.Sleep(0);

            start.Set();

            foreach (var t in threads)
                t.Join();

            Trace.TraceInformation("Execution time: {0}", timer.Elapsed);
        }

        [Test, LuceneNetSpecific]
        public void TestGuidHashCollision()
        {
            Guid id1 = Guid.NewGuid();
            Guid id2 = NextHashCollision(id1);

            Assert.AreNotEqual(id1, id2);
            Assert.AreEqual(id1.GetHashCode(), id2.GetHashCode());
        }

        [Test, LuceneNetSpecific]
        public void TestLimitedInsert()
        {
            var Map = new LurchTable<Guid, bool>(LurchTableOrder.Access, 1000);
            var ids = CreateSample(Guid.NewGuid(), 1000000, 0);

            Parallel(1, ids,
                     id =>
                     {
                         bool test;
                         Assert.IsTrue(Map.TryAdd(id, true));
                         Map.TryGetValue(id, out test);
                     });

            Assert.AreEqual(1000, Map.Count);
        }

        [Test, LuceneNetSpecific]
        public void TestInsert()
        {
            var Map = CreateMap<bool>();
            var ids = CreateSample(Guid.NewGuid(), COUNT, 4);

            bool test;
            Parallel(1, ids, id => { Assert.IsTrue(Map.TryAdd(id, true)); });

            Assert.AreEqual(ids.Length, Map.Count);
            foreach (var id in ids)
                Assert.IsTrue(Map.TryGetValue(id, out test) && test);
        }

        [Test, LuceneNetSpecific]
        public void TestDelete()
        {
            var Map = CreateMap<bool>();
            var ids = CreateSample(Guid.NewGuid(), COUNT, 4);
            foreach (var id in ids)
                Assert.IsTrue(Map.TryAdd(id, true));

            bool test;
            Parallel(1, ids, id => { Assert.IsTrue(Map.Remove(id)); });

            Assert.AreEqual(0, Map.Count);
            foreach (var id in ids)
                Assert.IsTrue(!Map.TryGetValue(id, out test));
        }

        [Test, LuceneNetSpecific]
        public void TestInsertDelete()
        {
            var Map = CreateMap<bool>();
            var ids = CreateSample(Guid.NewGuid(), COUNT, 4);

            bool test;
            Parallel(100, ids, id => { Assert.IsTrue(Map.TryAdd(id, true)); Assert.IsTrue(Map.Remove(id)); });

            Assert.AreEqual(0, Map.Count);
            foreach (var id in ids)
                Assert.IsTrue(!Map.TryGetValue(id, out test));
        }

        [Test, LuceneNetSpecific]
        public void TestInsertUpdateDelete()
        {
            var Map = CreateMap<bool>();
            var ids = CreateSample(Guid.NewGuid(), COUNT, 4);

            bool test;
            Parallel(100, ids, id => { Assert.IsTrue(Map.TryAdd(id, true)); Assert.IsTrue(Map.TryUpdate(id, false, true)); Assert.IsTrue(Map.Remove(id)); });

            Assert.AreEqual(0, Map.Count);
            foreach (var id in ids)
                Assert.IsTrue(!Map.TryGetValue(id, out test));
        }

        [Test, LuceneNetSpecific]
        public void CompareTest()
        {
            const int size = 1000000;
            int reps = 3;
            Stopwatch timer;

            IDictionary<Guid, TestValue> dict = new ConcurrentDictionary<Guid, TestValue>(new Dictionary<Guid, TestValue>(size));
            IDictionary<Guid, TestValue> test = new LurchTable<Guid, TestValue>(size);

            for (int rep = 0; rep < reps; rep++)
            {
                var sample = CreateSample(Guid.NewGuid(), size, 1);

                timer = Stopwatch.StartNew();
                Parallel(1, sample, item => dict.Add(item, new TestValue { Id = item, Count = rep }));
                Trace.TraceInformation("Dict Add: {0}", timer.Elapsed);

                timer = Stopwatch.StartNew();
                Parallel(1, sample, item => test.Add(item, new TestValue { Id = item, Count = rep }));
                Trace.TraceInformation("Test Add: {0}", timer.Elapsed);

                timer = Stopwatch.StartNew();
                Parallel(1, sample, item => dict[item] = new TestValue { Id = item, Count = rep });
                Trace.TraceInformation("Dict Update: {0}", timer.Elapsed);

                timer = Stopwatch.StartNew();
                Parallel(1, sample, item => test[item] = new TestValue { Id = item, Count = rep });
                Trace.TraceInformation("Test Update: {0}", timer.Elapsed);

                timer = Stopwatch.StartNew();
                Parallel(1, sample, item => dict.Remove(item));
                Trace.TraceInformation("Dict Rem: {0}", timer.Elapsed);
                Assert.AreEqual(0, dict.Count);

                timer = Stopwatch.StartNew();
                Parallel(1, sample, item => test.Remove(item));
                Trace.TraceInformation("Test Rem: {0}", timer.Elapsed);

                test.Clear();
                dict.Clear();

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        struct TestValue
        {
            public Guid Id;
            public int Count;
        };

        #region Guid Hash Collision Generator

        private static Random random = new Random();
        private static int iCounter = 0x01010101;

        public static Guid NextHashCollision(Guid guid)
        {
            var bytes = guid.ToByteArray();

            // Modify bytes 8 & 9 with random number
            Array.Copy(
                BitConverter.GetBytes((short)random.Next()),
                0,
                bytes,
                8,
                2
            );

            // Increment bytes 11, 12, 13, & 14
            Array.Copy(
                BitConverter.GetBytes(
                    BitConverter.ToInt32(bytes, 11) +
                    Interlocked.Increment(ref iCounter)
                    ),
                0,
                bytes,
                11,
                4
            );

            Guid result = new Guid(bytes);
            Assert.AreEqual(guid.GetHashCode(), result.GetHashCode());
            return result;
        }

        public static Guid[] CreateSample(Guid seed, int size, double collisions)
        {
            var sample = new Guid[size];
            int count = 0, collis = 0, uix = 0;
            for (int i = 0; i < size; i++)
            {
                if (collis >= count * collisions)
                {
                    sample[uix = i] = Guid.NewGuid();
                    count++;
                }
                else
                {
                    sample[i] = NextHashCollision(sample[uix]);
                    collis++;
                }
            }
            return sample;
        }
        #endregion
    }
}
