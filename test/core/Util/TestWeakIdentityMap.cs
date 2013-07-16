using System;
using System.Threading;
using Lucene.Net.Support;
using Lucene.Net.Test.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestWeakIdentityMap : LuceneTestCase
    {
        [Test]
        public void TestSimpleHashMap()
        {
            WeakIdentityMap<string, string> map =
              WeakIdentityMap<string, string>.NewHashMap(new Random().NextBool());
            // we keep strong references to the keys,
            // so WeakIdentityMap will not forget about them:
            string key1 = "foo";
            string key2 = "foo";
            string key3 = "foo";

            Assert.AreNotSame(key1, key2);
            Assert.AreEqual(key1, key2);
            Assert.AreNotSame(key1, key3);
            Assert.AreEqual(key1, key3);
            Assert.AreNotSame(key2, key3);
            Assert.AreEqual(key2, key3);

            // try null key & check its iterator also return null:
            map[null] = "null";
            {
                var it = map.Keys.GetEnumerator();
                Assert.IsTrue(it.MoveNext());
                Assert.IsNull(it.Current);
                Assert.IsFalse(it.MoveNext());
                Assert.IsFalse(it.MoveNext());
            }
            // 2 more keys:
            map[key1] = "bar1";
            map[key2] = "bar2";

            Assert.AreEqual(3, map.Size);

            Assert.AreEqual("bar1", map[key1]);
            Assert.AreEqual("bar2", map[key2]);
            Assert.AreEqual(null, map[key3]);
            Assert.AreEqual("null", map[null]);

            Assert.IsTrue(map.ContainsKey(key1));
            Assert.IsTrue(map.ContainsKey(key2));
            Assert.IsFalse(map.ContainsKey(key3));
            Assert.IsTrue(map.ContainsKey(null));

            // repeat and check that we have no double entries
            map[key1] = "bar1";
            map[key2] = "bar2";
            map[null] = null;

            Assert.AreEqual(3, map.Size);

            Assert.AreEqual("bar1", map[key1]);
            Assert.AreEqual("bar2", map[key2]);
            Assert.AreEqual(null, map[key3]);
            Assert.AreEqual("null", map[null]);

            Assert.IsTrue(map.ContainsKey(key1));
            Assert.IsTrue(map.ContainsKey(key2));
            Assert.IsFalse(map.ContainsKey(key3));
            Assert.IsTrue(map.ContainsKey(null));

            map.Remove(null);
            Assert.AreEqual(2, map.Size);
            map.Remove(key1);
            Assert.AreEqual(1, map.Size);
            map[key1] = "bar1";
            map[key2] = "bar2";
            map[key3] = "bar3";
            Assert.AreEqual(3, map.Size);

            int c = 0, keysAssigned = 0;
            for (var enumerator = map.Keys.GetEnumerator(); enumerator.MoveNext(); )
            {
                Assert.IsTrue(enumerator.MoveNext()); // try again, should return same result!
                var k = enumerator.Current;
                Assert.IsTrue(k == key1 || k == key2 | k == key3);
                keysAssigned += (k == key1) ? 1 : ((k == key2) ? 2 : 4);
                c++;
            }
            Assert.AreEqual(3, c);
            Assert.AreEqual(1 + 2 + 4, keysAssigned, "all keys must have been seen");

            c = 0;
            for (var enumerator = map.Values.GetEnumerator(); enumerator.MoveNext(); )
            {
                var v = enumerator.Current;
                Assert.IsTrue(v.StartsWith("bar"));
                c++;
            }
            Assert.AreEqual(3, c);

            // clear strong refs
            key1 = key2 = key3 = null;

            // check that GC does not cause problems in reap() method, wait 1 second and let GC work:
            int size = map.Size;
            for (int i = 0; size > 0 && i < 10; i++) try
                {
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    int newSize = map.Size;
                    Assert.IsTrue(size >= newSize, "previousSize(" + size + ")>=newSize(" + newSize + ")");
                    size = newSize;
                    Thread.Sleep(TimeSpan.FromMilliseconds(100L));
                    c = 0;
                    for (var enumerator = map.Keys.GetEnumerator(); enumerator.MoveNext(); )
                    {
                        assertNotNull(enumerator.Current);
                        c++;
                    }
                    newSize = map.Size;
                    Assert.IsTrue(size >= c, "previousSize(" + size + ")>=iteratorSize(" + c + ")");
                    Assert.IsTrue(c >= newSize, "iteratorSize(" + c + ")>=newSize(" + newSize + ")");
                    size = newSize;
                }
                catch (ThreadInterruptedException ie) { }

            map.Clear();
            Assert.AreEqual(0, map.Size);
            Assert.IsTrue(map.IsEmpty);

            var enumerator2 = map.Keys.GetEnumerator();
            Assert.IsFalse(enumerator2.MoveNext());
            try
            {
                var current = enumerator2.Current;
                Fail("Should throw InvalidOperationException");
            }
            catch (InvalidOperationException nse)
            {
            }

            key1 = "foo";
            key2 = "foo";
            map[key1] = "bar1";
            map[key2] = "bar2";
            Assert.AreEqual(2, map.Size);

            map.Clear();
            Assert.AreEqual(0, map.Size);
            Assert.IsTrue(map.IsEmpty);
        }

        private sealed class AnonymousThreadClass : ThreadClass
        {
            private readonly Random rnd;
            private readonly WeakIdentityMap<object, int> map;
            private readonly int keyCount;
            private readonly AtomicReferenceArray<object> keys;

            public AnonymousThreadClass(Random rnd, WeakIdentityMap<object, int> map, AtomicReferenceArray<object> keys, int keyCount)
            {
                this.rnd = rnd;
                this.map = map;
                this.keyCount = keyCount;
                this.keys = keys;
            }

            public override void Run()
            {
                int count = AtLeast(rnd, 10000);
                for (int i = 0; i < count; i++)
                {
                    int j = rnd.Next(keyCount);
                    switch (rnd.Next(5))
                    {
                        case 0:
                            map[keys[j]] = j;
                            break;
                        case 1:
                            int v = map[keys[j]];
                            if (v != null)
                            {
                                Assert.AreEqual(j, v);
                            }
                            break;
                        case 2:
                            map.Remove(keys[j]);
                            break;
                        case 3:
                            // renew key, the old one will be GCed at some time:
                            keys.Set(j, new object());
                            break;
                        case 4:
                            // check iterator still working
                            for (var it = map.Keys.GetEnumerator(); it.MoveNext(); )
                            {
                                Assert.IsNotNull(it.Current);
                            }
                            break;
                        default:
                            Fail("Should not get here.");
                            break;
                    }
                }
            }
        }

        [Test]
        public void TestConcurrentHashMap()
        {
            // don't make threadCount and keyCount random, otherwise easily OOMs or fails otherwise:
            int threadCount = 8, keyCount = 1024;
            ExecutorService exec = Executors.newFixedThreadPool(threadCount, new NamedThreadFactory("TestConcurrentHashMap"));
            WeakIdentityMap<object, int> map =
              WeakIdentityMap<object, int>.NewConcurrentHashMap(new Random().NextBool());
            // we keep strong references to the keys,
            // so WeakIdentityMap will not forget about them:
            AtomicReferenceArray<object> keys = new AtomicReferenceArray<object>(keyCount);
            for (int j = 0; j < keyCount; j++)
            {
                keys.Set(j, new object());
            }

            try
            {
                for (int t = 0; t < threadCount; t++)
                {
                    Random rnd = new Random(new Random().Next());
                    exec.execute(new AnonymousThreadClass());
                }
            }
            finally
            {
                exec.Shutdown();
                while (!exec.AwaitTermination(1000L, TimeUnit.Milliseconds)) ;
            }

            // clear strong refs
            for (int j = 0; j < keyCount; j++)
            {
                keys.Set(j, null);
            }

            // check that GC does not cause problems in reap() method:
            int size = map.Size;
            for (int i = 0; size > 0 && i < 10; i++)
            {
                try
                {
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    int newSize = map.Size;
                    Assert.IsTrue(size >= newSize, "previousSize(" + size + ")>=newSize(" + newSize + ")");
                    size = newSize;
                    Thread.Sleep(TimeSpan.FromMilliseconds(100L));
                    int c = 0;
                    for (var it = map.Keys.GetEnumerator(); it.MoveNext();)
                    {
                        assertNotNull(it.Current);
                        c++;
                    }
                    newSize = map.Size;
                    Assert.IsTrue(size >= c, "previousSize(" + size + ")>=iteratorSize(" + c + ")");
                    Assert.IsTrue(c >= newSize, "iteratorSize(" + c + ")>=newSize(" + newSize + ")");
                    size = newSize;
                }
                catch (ThreadInterruptedException ie)
                {

                }
            }
        }
    }
}
