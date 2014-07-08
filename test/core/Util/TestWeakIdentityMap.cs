using System;
using System.Collections.Generic;
using System.Threading;
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
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using NUnit.Framework;

namespace Lucene.Net.Util
{

    [TestFixture]
    public class TestWeakIdentityMap : LuceneTestCase
    {

        [Test]
        public virtual void TestSimpleHashMap()
        {
            WeakIdentityMap<string, string> map = WeakIdentityMap<string, string>.NewHashMap(Random().NextBoolean());
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
            map.Put(null, "null");
            {
                IEnumerator<string> iter = map.Keys.GetEnumerator();
                Assert.IsTrue(iter.MoveNext());
                Assert.IsNull(iter.Current);
                Assert.IsFalse(iter.MoveNext());
                Assert.IsFalse(iter.MoveNext());
            }
            // 2 more keys:
            map.Put(key1, "bar1");
            map.Put(key2, "bar2");

            Assert.AreEqual(3, map.Size());

            Assert.AreEqual("bar1", map.Get(key1));
            Assert.AreEqual("bar2", map.Get(key2));
            Assert.AreEqual(null, map.Get(key3));
            Assert.AreEqual("null", map.Get(null));

            Assert.IsTrue(map.ContainsKey(key1));
            Assert.IsTrue(map.ContainsKey(key2));
            Assert.IsFalse(map.ContainsKey(key3));
            Assert.IsTrue(map.ContainsKey(null));

            // repeat and check that we have no double entries
            map.Put(key1, "bar1");
            map.Put(key2, "bar2");
            map.Put(null, "null");

            Assert.AreEqual(3, map.Size());

            Assert.AreEqual("bar1", map.Get(key1));
            Assert.AreEqual("bar2", map.Get(key2));
            Assert.AreEqual(null, map.Get(key3));
            Assert.AreEqual("null", map.Get(null));

            Assert.IsTrue(map.ContainsKey(key1));
            Assert.IsTrue(map.ContainsKey(key2));
            Assert.IsFalse(map.ContainsKey(key3));
            Assert.IsTrue(map.ContainsKey(null));

            map.Remove(null);
            Assert.AreEqual(2, map.Size());
            map.Remove(key1);
            Assert.AreEqual(1, map.Size());
            map.Put(key1, "bar1");
            map.Put(key2, "bar2");
            map.Put(key3, "bar3");
            Assert.AreEqual(3, map.Size());

            int c = 0, keysAssigned = 0;
            for (IEnumerator<string> iter = map.Keys.GetEnumerator(); iter.MoveNext(); )
            {
                //Assert.IsTrue(iter.hasNext()); // try again, should return same result!
                string k = iter.Current;
                Assert.IsTrue(k == key1 || k == key2 | k == key3);
                keysAssigned += (k == key1) ? 1 : ((k == key2) ? 2 : 4);
                c++;
            }
            Assert.AreEqual(3, c);
            Assert.AreEqual(1 + 2 + 4, keysAssigned, "all keys must have been seen");

            c = 0;
            for (IEnumerator<string> iter = map.Values.GetEnumerator(); iter.MoveNext(); )
            {
                string v = iter.Current;
                Assert.IsTrue(v.StartsWith("bar"));
                c++;
            }
            Assert.AreEqual(3, c);

            // clear strong refs
            key1 = key2 = key3 = null;

            // check that GC does not cause problems in reap() method, wait 1 second and let GC work:
            int size = map.Size();
            for (int i = 0; size > 0 && i < 10; i++)
            {
                try
                {
                    System.RunFinalization();
                    System.gc();
                    int newSize = map.Size();
                    Assert.IsTrue(size >= newSize, "previousSize(" + size + ")>=newSize(" + newSize + ")");
                    size = newSize;
                    Thread.Sleep(new TimeSpan(100L));
                    c = 0;
                    for (IEnumerator<string> iter = map.Keys.GetEnumerator(); iter.MoveNext(); )
                    {
                        Assert.IsNotNull(iter.Current);
                        c++;
                    }
                    newSize = map.Size();
                    Assert.IsTrue(size >= c, "previousSize(" + size + ")>=iteratorSize(" + c + ")");
                    Assert.IsTrue(c >= newSize, "iteratorSize(" + c + ")>=newSize(" + newSize + ")");
                    size = newSize;
                }
                catch (ThreadInterruptedException ie)
                {
                }
            }

            map.Clear();
            Assert.AreEqual(0, map.Size());
            Assert.IsTrue(map.Empty);

            IEnumerator<string> it = map.Keys.GetEnumerator();
            Assert.IsFalse(it.MoveNext());
            /*try
            {
              it.Next();
              Assert.Fail("Should throw NoSuchElementException");
            }
            catch (NoSuchElementException nse)
            {
            }*/

            key1 = "foo";
            key2 = "foo";
            map.Put(key1, "bar1");
            map.Put(key2, "bar2");
            Assert.AreEqual(2, map.Size());

            map.Clear();
            Assert.AreEqual(0, map.Size());
            Assert.IsTrue(map.Empty);
        }

        [Test]
        public virtual void TestConcurrentHashMap()
        {
            // don't make threadCount and keyCount random, otherwise easily OOMs or fails otherwise:
            const int threadCount = 8, keyCount = 1024;
            ExecutorService exec = Executors.newFixedThreadPool(threadCount, new NamedThreadFactory("testConcurrentHashMap"));
            WeakIdentityMap<object, int?> map = WeakIdentityMap<object, int?>.NewConcurrentHashMap(Random().NextBoolean());
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
                    Random rnd = new Random(Random().Next());
                    exec.execute(new RunnableAnonymousInnerClassHelper(this, keyCount, map, keys, rnd));
                }
            }
            finally
            {
                exec.shutdown();
                while (!exec.awaitTermination(1000L, TimeUnit.MILLISECONDS)) ;
            }

            // clear strong refs
            for (int j = 0; j < keyCount; j++)
            {
                keys.Set(j, null);
            }

            // check that GC does not cause problems in reap() method:
            int size = map.Size();
            for (int i = 0; size > 0 && i < 10; i++)
            {
                try
                {
                    System.runFinalization();
                    System.gc();
                    int newSize = map.Size();
                    Assert.IsTrue(size >= newSize, "previousSize(" + size + ")>=newSize(" + newSize + ")");
                    size = newSize;
                    Thread.Sleep(new TimeSpan(100L));
                    int c = 0;
                    for (IEnumerator<object> it = map.Keys.GetEnumerator(); it.MoveNext(); )
                    {
                        Assert.IsNotNull(it.Current);
                        c++;
                    }
                    newSize = map.Size();
                    Assert.IsTrue(size >= c, "previousSize(" + size + ")>=iteratorSize(" + c + ")");
                    Assert.IsTrue(c >= newSize, "iteratorSize(" + c + ")>=newSize(" + newSize + ")");
                    size = newSize;
                }
                catch (ThreadInterruptedException ie)
                {
                }
            }
        }

        private class RunnableAnonymousInnerClassHelper : IThreadRunnable
        {
            private readonly TestWeakIdentityMap OuterInstance;

            private int KeyCount;
            private WeakIdentityMap<object, int?> Map;
            private AtomicReferenceArray<object> Keys;
            private Random Rnd;

            public RunnableAnonymousInnerClassHelper(TestWeakIdentityMap outerInstance, int keyCount, WeakIdentityMap<object, int?> map, AtomicReferenceArray<object> keys, Random rnd)
            {
                this.OuterInstance = outerInstance;
                this.KeyCount = keyCount;
                this.Map = map;
                this.Keys = keys;
                this.Rnd = rnd;
            }

            public void Run()
            {
                int count = AtLeast(Rnd, 10000);
                for (int i = 0; i < count; i++)
                {
                    int j = Rnd.Next(KeyCount);
                    switch (Rnd.Next(5))
                    {
                        case 0:
                            Map.Put(Keys.Get(j), Convert.ToInt32(j));
                            break;
                        case 1:
                            int? v = Map.Get(Keys.Get(j));
                            if (v != null)
                            {
                                Assert.AreEqual(j, (int)v);
                            }
                            break;
                        case 2:
                            Map.Remove(Keys.Get(j));
                            break;
                        case 3:
                            // renew key, the old one will be GCed at some time:
                            Keys.Set(j, new object());
                            break;
                        case 4:
                            // check iterator still working
                            for (IEnumerator<object> it = Map.Keys.GetEnumerator(); it.MoveNext(); )
                            {
                                Assert.IsNotNull(it.Current);
                            }
                            break;
                        default:
                            Assert.Fail("Should not get here.");
                            break;
                    }
                }
            }
        }
    }
}