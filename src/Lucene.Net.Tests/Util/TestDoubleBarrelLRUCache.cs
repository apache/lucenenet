using J2N.Threading;
using NUnit.Framework;
using System;

namespace Lucene.Net.Util
{
    /// <summary>
    /// Licensed to the Apache Software Foundation (ASF) under one or more
    /// contributor license agreements.  See the NOTICE file distributed with
    /// this work for additional information regarding copyright ownership.
    /// The ASF licenses this file to You under the Apache License, Version 2.0
    /// (the "License"); you may not use this file except in compliance with
    /// the License.  You may obtain a copy of the License at
    ///
    ///     http://www.apache.org/licenses/LICENSE-2.0
    ///
    /// Unless required by applicable law or agreed to in writing, software
    /// distributed under the License is distributed on an "AS IS" BASIS,
    /// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    /// See the License for the specific language governing permissions and
    /// limitations under the License.
    /// </summary>

    [TestFixture]
    public class TestDoubleBarrelLRUCache : LuceneTestCase
    {
        private void TestCache(DoubleBarrelLRUCache<CloneableInteger, object> cache, int n)
        {
            object dummy = new object();

            for (int i = 0; i < n; i++)
            {
                cache.Put(new CloneableInteger(i), dummy);
            }

            // access every 2nd item in cache
            for (int i = 0; i < n; i += 2)
            {
                Assert.IsNotNull(cache.Get(new CloneableInteger(i)));
            }

            // add n/2 elements to cache, the ones that weren't
            // touched in the previous loop should now be thrown away
            for (int i = n; i < n + (n / 2); i++)
            {
                cache.Put(new CloneableInteger(i), dummy);
            }

            // access every 4th item in cache
            for (int i = 0; i < n; i += 4)
            {
                Assert.IsNotNull(cache.Get(new CloneableInteger(i)));
            }

            // add 3/4n elements to cache, the ones that weren't
            // touched in the previous loops should now be thrown away
            for (int i = n; i < n + (n * 3 / 4); i++)
            {
                cache.Put(new CloneableInteger(i), dummy);
            }

            // access every 4th item in cache
            for (int i = 0; i < n; i += 4)
            {
                Assert.IsNotNull(cache.Get(new CloneableInteger(i)));
            }
        }

        [Test]
        public virtual void TestLRUCache()
        {
            const int n = 100;
            TestCache(new DoubleBarrelLRUCache<CloneableInteger, object>(n), n);
        }

        private class CacheThread : ThreadJob
        {
            private readonly TestDoubleBarrelLRUCache OuterInstance;

            internal readonly CloneableObject[] Objs;
            internal readonly DoubleBarrelLRUCache<CloneableObject, object> c;
            internal readonly DateTime EndTime;
            internal volatile bool Failed;

            public CacheThread(TestDoubleBarrelLRUCache outerInstance, DoubleBarrelLRUCache<CloneableObject, object> c, CloneableObject[] objs, DateTime endTime)
            {
                this.OuterInstance = outerInstance;
                this.c = c;
                this.Objs = objs;
                this.EndTime = endTime;
            }

            public override void Run()
            {
                try
                {
                    long count = 0;
                    long miss = 0;
                    long hit = 0;
                    int limit = Objs.Length;

                    while (true)
                    {
                        CloneableObject obj = Objs[(int)((count / 2) % limit)];
                        object v = c.Get(obj);
                        if (v == null)
                        {
                            c.Put(new CloneableObject(obj), obj);
                            miss++;
                        }
                        else
                        {
                            Assert.True(obj == v);
                            hit++;
                        }
                        if ((++count % 10000) == 0)
                        {
                            if (DateTime.Now.CompareTo(EndTime) > 0)
                            {
                                break;
                            }
                        }
                    }

                    OuterInstance.AddResults(miss, hit);
                }
                catch (Exception t)
                {
                    Failed = true;
                    throw new Exception(t.Message, t);
                }
            }
        }

        internal long TotMiss, TotHit;

        internal virtual void AddResults(long miss, long hit)
        {
            TotMiss += miss;
            TotHit += hit;
        }

        [Test]
        public virtual void TestThreadCorrectness()
        {
            const int NUM_THREADS = 4;
            const int CACHE_SIZE = 512;
            int OBJ_COUNT = 3 * CACHE_SIZE;

            DoubleBarrelLRUCache<CloneableObject, object> c = new DoubleBarrelLRUCache<CloneableObject, object>(1024);

            CloneableObject[] objs = new CloneableObject[OBJ_COUNT];
            for (int i = 0; i < OBJ_COUNT; i++)
            {
                objs[i] = new CloneableObject(new object());
            }

            CacheThread[] threads = new CacheThread[NUM_THREADS];
            DateTime endTime = DateTime.Now.AddSeconds(1);
            for (int i = 0; i < NUM_THREADS; i++)
            {
                threads[i] = new CacheThread(this, c, objs, endTime);
                threads[i].Start();
            }
            for (int i = 0; i < NUM_THREADS; i++)
            {
                threads[i].Join();
                Assert.False(threads[i].Failed);
            }
            //System.out.println("hits=" + totHit + " misses=" + totMiss);
        }

        private class CloneableObject : DoubleBarrelLRUCache.CloneableKey
        {
            internal object Value;

            public CloneableObject(object value)
            {
                this.Value = value;
            }

            public override bool Equals(object other)
            {
                if (other.GetType().Equals(typeof (CloneableObject)))
                    return this.Value.Equals(((CloneableObject) other).Value);
                else
                    return false;
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }

            public override object Clone()
            {
                return new CloneableObject(Value);
            }
        }

        protected internal class CloneableInteger : DoubleBarrelLRUCache.CloneableKey
        {
            internal int? Value;

            public CloneableInteger(int? value)
            {
                this.Value = value;
            }

            public override bool Equals(object other)
            {
                return this.Value.Equals(((CloneableInteger)other).Value);
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }

            public override object Clone()
            {
                return new CloneableInteger(Value);
            }
        }
    }
}