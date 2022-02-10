using J2N.Threading;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

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
            private readonly TestDoubleBarrelLRUCache outerInstance;

            private readonly CloneableObject[] objs;
            private readonly DoubleBarrelLRUCache<CloneableObject, object> c;
            private readonly long endTime;
            internal volatile bool failed;

            public CacheThread(TestDoubleBarrelLRUCache outerInstance, DoubleBarrelLRUCache<CloneableObject, object> c, CloneableObject[] objs, long endTime)
            {
                this.outerInstance = outerInstance;
                this.c = c;
                this.objs = objs;
                this.endTime = endTime;
            }

            public override void Run()
            {
                try
                {
                    long count = 0;
                    long miss = 0;
                    long hit = 0;
                    int limit = objs.Length;

                    while (true)
                    {
                        CloneableObject obj = objs[(int)((count / 2) % limit)];
                        object v = c.Get(obj);
                        if (v is null)
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
                            if (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond > endTime) // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                            {
                                break;
                            }
                        }
                    }

                    outerInstance.AddResults(miss, hit);
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    failed = true;
                    throw RuntimeException.Create(t);
                }
            }
        }

        internal long totMiss, totHit;

        internal virtual void AddResults(long miss, long hit)
        {
            totMiss += miss;
            totHit += hit;
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
            long endTime = (J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond) + 1000L; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
            for (int i = 0; i < NUM_THREADS; i++)
            {
                threads[i] = new CacheThread(this, c, objs, endTime);
                threads[i].Start();
            }
            for (int i = 0; i < NUM_THREADS; i++)
            {
                threads[i].Join();
                Assert.False(threads[i].failed);
            }
            //System.out.println("hits=" + totHit + " misses=" + totMiss);
        }

        private class CloneableObject : DoubleBarrelLRUCache.CloneableKey
        {
            private readonly object value;

            public CloneableObject(object value)
            {
                this.value = value;
            }

            public override bool Equals(object other)
            {
                if (other.GetType().Equals(typeof (CloneableObject)))
                    return this.value.Equals(((CloneableObject) other).value);
                else
                    return false;
            }

            public override int GetHashCode()
            {
                return value.GetHashCode();
            }

            public override object Clone()
            {
                return new CloneableObject(value);
            }
        }

        protected internal class CloneableInteger : DoubleBarrelLRUCache.CloneableKey
        {
            internal int value;

            public CloneableInteger(int value)
            {
                this.value = value;
            }

            public override bool Equals(object other)
            {
                return this.value.Equals(((CloneableInteger)other).value);
            }

            public override int GetHashCode()
            {
                return value.GetHashCode();
            }

            public override object Clone()
            {
                return new CloneableInteger(value);
            }
        }
    }
}