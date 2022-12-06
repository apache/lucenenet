// LUCENENET specific - factored out this WeakIdentityMap<TKey, TValue> and replaced it with ConditionalWeakTable<TKey, TValue>.
// ConditionalWeakTable<TKey, TValue> is thread-safe and internally uses RuntimeHelpers.GetHashCode()
// to lookup the key, so it can be used as a direct replacement for WeakIdentityMap<TKey, TValue>
// in most cases.

//using J2N.Threading;
//using J2N.Threading.Atomic;
//using Lucene.Net.Attributes;
//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Threading;
//using Assert = Lucene.Net.TestFramework.Assert;

//namespace Lucene.Net.Util
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    [TestFixture]
//    public class TestWeakIdentityMap : LuceneTestCase
//    {

//        [Test, Slow]
//        public virtual void TestSimpleHashMap()
//        {
//            WeakIdentityMap<string, string> map = WeakIdentityMap<string, string>.NewHashMap(Random.NextBoolean());
//            // we keep strong references to the keys,
//            // so WeakIdentityMap will not forget about them:
//            string key1 = "foo";
//            string key2 = "test1 foo".Split(' ')[1];
//            string key3 = "test2 foo".Split(' ')[1];

//            // LUCENENET NOTE: As per http://stackoverflow.com/a/543329/181087,
//            // the above hack is required in order to ensure the AreNotSame
//            // check will work. If you assign the same string to 3 different variables
//            // without doing some kind of manipulation from the original string, the
//            // AreNotSame test will fail because the references will be the same.

//            Assert.AreNotSame(key1, key2);
//            Assert.AreEqual(key1, key2);
//            Assert.AreNotSame(key1, key3);
//            Assert.AreEqual(key1, key3);
//            Assert.AreNotSame(key2, key3);
//            Assert.AreEqual(key2, key3);

//            // try null key & check its iterator also return null:
//            map.Put(null, "null");
//            {
//                using (IEnumerator<string> iter = map.Keys.GetEnumerator())
//                {
//                    Assert.IsTrue(iter.MoveNext());
//                    Assert.IsNull(iter.Current);
//                    Assert.IsFalse(iter.MoveNext());
//                    Assert.IsFalse(iter.MoveNext());
//                }
//            }
//            // 2 more keys:
//            map.Put(key1, "bar1");
//            map.Put(key2, "bar2");

//            Assert.AreEqual(3, map.Count);

//            Assert.AreEqual("bar1", map.Get(key1));
//            Assert.AreEqual("bar2", map.Get(key2));
//            Assert.AreEqual(null, map.Get(key3));
//            Assert.AreEqual("null", map.Get(null));

//            Assert.IsTrue(map.ContainsKey(key1));
//            Assert.IsTrue(map.ContainsKey(key2));
//            Assert.IsFalse(map.ContainsKey(key3));
//            Assert.IsTrue(map.ContainsKey(null));

//            // repeat and check that we have no double entries
//            map.Put(key1, "bar1");
//            map.Put(key2, "bar2");
//            map.Put(null, "null");

//            Assert.AreEqual(3, map.Count);

//            Assert.AreEqual("bar1", map.Get(key1));
//            Assert.AreEqual("bar2", map.Get(key2));
//            Assert.AreEqual(null, map.Get(key3));
//            Assert.AreEqual("null", map.Get(null));

//            Assert.IsTrue(map.ContainsKey(key1));
//            Assert.IsTrue(map.ContainsKey(key2));
//            Assert.IsFalse(map.ContainsKey(key3));
//            Assert.IsTrue(map.ContainsKey(null));

//            map.Remove(null);
//            Assert.AreEqual(2, map.Count);
//            map.Remove(key1);
//            Assert.AreEqual(1, map.Count);
//            map.Put(key1, "bar1");
//            map.Put(key2, "bar2");
//            map.Put(key3, "bar3");
//            Assert.AreEqual(3, map.Count);

//            int c = 0, keysAssigned = 0;
//            foreach (object k in map.Keys)
//            {
//                //Assert.IsTrue(iter.hasNext()); // try again, should return same result!
//                // LUCENENET NOTE: Need object.ReferenceEquals here because the == operator does more than check reference equality
//                Assert.IsTrue(object.ReferenceEquals(k, key1) || object.ReferenceEquals(k, key2) | object.ReferenceEquals(k, key3));
//                keysAssigned += object.ReferenceEquals(k, key1) ? 1 : (object.ReferenceEquals(k, key2) ? 2 : 4);
//                c++;
//            }
//            Assert.AreEqual(3, c);
//            Assert.AreEqual(1 + 2 + 4, keysAssigned, "all keys must have been seen");

//            c = 0;
//            for (IEnumerator<string> iter = map.Values.GetEnumerator(); iter.MoveNext();)
//            {
//                string v = iter.Current;
//                Assert.IsTrue(v.StartsWith("bar", StringComparison.Ordinal));
//                c++;
//            }
//            Assert.AreEqual(3, c);

//            // clear strong refs
//            key1 = key2 = key3 = null;

//            // check that GC does not cause problems in reap() method, wait 1 second and let GC work:
//            int size = map.Count;
//            for (int i = 0; size > 0 && i < 10; i++)
//            {
//                try
//                {
//                    GC.Collect();
//                    int newSize = map.Count;
//                    Assert.IsTrue(size >= newSize, "previousSize(" + size + ")>=newSize(" + newSize + ")");
//                    size = newSize;
//                    Thread.Sleep(TimeSpan.FromSeconds(1));
//                    c = 0;
//                    foreach (object k in map.Keys)
//                    {
//                        Assert.IsNotNull(k);
//                        c++;
//                    }
//                    newSize = map.Count;
//                    Assert.IsTrue(size >= c, "previousSize(" + size + ")>=iteratorSize(" + c + ")");
//                    Assert.IsTrue(c >= newSize, "iteratorSize(" + c + ")>=newSize(" + newSize + ")");
//                    size = newSize;
//                }
//                catch (Exception ie) when (ie.IsInterruptedException())
//                {
//                }
//            }

//            map.Clear();
//            Assert.AreEqual(0, map.Count);
//            Assert.IsTrue(map.IsEmpty);

//            using (IEnumerator<string> it = map.Keys.GetEnumerator())
//                Assert.IsFalse(it.MoveNext());
//            /*try
//            {
//              it.Next();
//              Assert.Fail("Should throw NoSuchElementException");
//            }
//            catch (NoSuchElementException nse)
//            {
//            }*/

//            // LUCENENET NOTE: As per http://stackoverflow.com/a/543329/181087,
//            // the following hack is required in order to ensure the string references
//            // are different. If you assign the same string to 2 different variables
//            // without doing some kind of manipulation from the original string, the
//            // references will be the same.

//            key1 = "test3 foo".Split(' ')[1];
//            key2 = "test4 foo".Split(' ')[1];
//            map.Put(key1, "bar1");
//            map.Put(key2, "bar2");
//            Assert.AreEqual(2, map.Count);

//            map.Clear();
//            Assert.AreEqual(0, map.Count);
//            Assert.IsTrue(map.IsEmpty);
//        }

//        [Test, Slow]
//        public virtual void TestConcurrentHashMap()
//        {
//            // don't make threadCount and keyCount random, otherwise easily OOMs or fails otherwise:
//            const int threadCount = 8, keyCount = 1024;

//            RunnableAnonymousClass[] workers = new RunnableAnonymousClass[threadCount];
//            WeakIdentityMap<object, int?> map = WeakIdentityMap<object, int?>.NewConcurrentHashMap(Random.NextBoolean());
//            // we keep strong references to the keys,
//            // so WeakIdentityMap will not forget about them:
//            AtomicReferenceArray<object> keys = new AtomicReferenceArray<object>(keyCount);
//            for (int j = 0; j < keyCount; j++)
//            {
//                keys[j] = new object();
//            }

//            try
//            {
//                for (int t = 0; t < threadCount; t++)
//                {
//                    Random rnd = new J2N.Randomizer(Random.NextInt64());
//                    var worker = new RunnableAnonymousClass(this, keyCount, map, keys, rnd);
//                    workers[t] = worker;
//                    worker.Start();
//                }
//            }
//            finally
//            {
//                foreach (var w in workers)
//                {
//                    w.Join();
//                }
//            }

//            // LUCENENET: Since assertions were done on the other threads, we need to check the
//            // results here.
//            for (int i = 0; i < workers.Length; i++)
//            {
//                assertTrue(string.Format(CultureInfo.InvariantCulture,
//                    "worker thread {0} of {1} failed \n" + workers[i].Error, i, workers.Length),
//                    workers[i].Error is null);
//            }


//            // clear strong refs
//            for (int j = 0; j < keyCount; j++)
//            {
//                keys[j] = null;
//            }

//            // check that GC does not cause problems in reap() method:
//            int size = map.Count;
//            for (int i = 0; size > 0 && i < 10; i++)
//            {
//                try
//                {
//                    GC.Collect();
//                    int newSize = map.Count;
//                    Assert.IsTrue(size >= newSize, "previousSize(" + size + ")>=newSize(" + newSize + ")");
//                    size = newSize;
//                    Thread.Sleep(new TimeSpan(100L));
//                    int c = 0;
//                    foreach (object k in map.Keys)
//                    {
//                        Assert.IsNotNull(k);
//                        c++;
//                    }
//                    newSize = map.Count;
//                    Assert.IsTrue(size >= c, "previousSize(" + size + ")>=iteratorSize(" + c + ")");
//                    Assert.IsTrue(c >= newSize, "iteratorSize(" + c + ")>=newSize(" + newSize + ")");
//                    size = newSize;
//                }
//                catch (Exception ie) when (ie.IsInterruptedException())
//                {
//                }
//            }
//        }

//        private sealed class RunnableAnonymousClass : ThreadJob
//        {
//            private readonly TestWeakIdentityMap outerInstance;

//            private readonly int keyCount;
//            private readonly WeakIdentityMap<object, int?> map;
//            private AtomicReferenceArray<object> keys;
//            private readonly Random rnd;
//            private volatile Exception error;

//            public RunnableAnonymousClass(TestWeakIdentityMap outerInstance, int keyCount, WeakIdentityMap<object, int?> map, AtomicReferenceArray<object> keys, Random rnd)
//            {
//                this.outerInstance = outerInstance;
//                this.keyCount = keyCount;
//                this.map = map;
//                this.keys = keys;
//                this.rnd = rnd;
//            }

//            public Exception Error
//            {
//                get { return error; }
//            }


//            public override void Run()
//            {
//                int count = AtLeast(rnd, 10000);
//                try
//                {
//                    for (int i = 0; i < count; i++)
//                    {
//                        int j = rnd.Next(keyCount);
//                        switch (rnd.Next(5))
//                        {
//                            case 0:
//                                map.Put(keys[j], Convert.ToInt32(j));
//                                break;
//                            case 1:
//                                int? v = map.Get(keys[j]);
//                                if (v != null)
//                                {
//                                    Assert.AreEqual(j, (int)v);
//                                }
//                                break;
//                            case 2:
//                                map.Remove(keys[j]);
//                                break;
//                            case 3:
//                                // renew key, the old one will be GCed at some time:
//                                keys[j] = new object();
//                                break;
//                            case 4:
//                                // check iterator still working
//                                foreach (object k in map.Keys)
//                                {
//                                    Assert.IsNotNull(k);
//                                }
//                                break;
//                            default:
//                                Assert.Fail("Should not get here.");
//                                break;
//                        }
//                    }
//                }
//                catch (Exception e)
//                {
//                    e.printStackTrace();
//                    this.error = e;
//                }
//            }
//        }
//    }
//}