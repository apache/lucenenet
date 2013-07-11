using System;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestDoubleBarrelLRUCache : LuceneTestCase
    {
        [Test]
        private void TestCache(DoubleBarrelLRUCache<CloneableInteger, object> cache, int n)
        {
            var dummy = new object();

            for (var i = 0; i < n; i++)
            {
                cache[new CloneableInteger(i)] = dummy;
            }

            // access every 2nd item in cache
            for (var i = 0; i < n; i += 2)
            {
                Assert.NotNull(cache[new CloneableInteger(i)]);
            }

            // add n/2 elements to cache, the ones that weren't
            // touched in the previous loop should now be thrown away
            for (var i = n; i < n + (n / 2); i++)
            {
                cache[new CloneableInteger(i)] = dummy;
            }

            // access every 4th item in cache
            for (var i = 0; i < n; i += 4)
            {
                Assert.NotNull(cache[new CloneableInteger(i)]);
            }

            // add 3/4n elements to cache, the ones that weren't
            // touched in the previous loops should now be thrown away
            for (var i = n; i < n + (n * 3 / 4); i++)
            {
                cache[new CloneableInteger(i)] = dummy;
            }

            // access every 4th item in cache
            for (var i = 0; i < n; i += 4)
            {
                Assert.NotNull(cache[new CloneableInteger(i)]);
            }
        }

        [Test]
        public virtual void TestLRUCache()
        {
            var n = 100;
            TestCache(new DoubleBarrelLRUCache<CloneableInteger, object>(n), n);
        }

        private class CacheThread : ThreadClass
        {
            private TestDoubleBarrelLRUCache parent;

            private CloneableObject[] objs;
            private DoubleBarrelLRUCache<CloneableObject, object> c;
            private DateTime endTime;
            volatile bool failed;

            public CacheThread(TestDoubleBarrelLRUCache parent, DoubleBarrelLRUCache<CloneableObject, object> c,
                               CloneableObject[] objs, long endTime)
            {
                this.parent = parent;

                this.c = c;
                this.objs = objs;
                this.endTime = new DateTime(1970, 1, 1).AddMilliseconds(endTime);
            }

            public override void Run()
            {
                try
                {
                    long count = 0;
                    long miss = 0;
                    long hit = 0;
                    var limit = objs.Length;

                    while (true)
                    {
                        var obj = objs[(int)((count / 2) % limit)];
                        var v = c[obj];
                        if (v == null)
                        {
                            c[new CloneableObject(obj)] = obj;
                            //c.Put(new CloneableObject(obj), obj);
                            miss++;
                        }
                        else
                        {
                            //assert obj == v;
                            hit++;
                        }
                        if ((++count % 10000) == 0)
                        {
                            if (DateTime.Now >= endTime)
                            {
                                break;
                            }
                        }
                    }

                    parent.AddResults(miss, hit);
                }
                catch (Exception e)
                {
                    failed = true;
                    throw;
                }
            }
        }

        long totMiss, totHit;
        internal virtual void AddResults(long miss, long hit)
        {
            totMiss += miss;
            totHit += hit;
        }

        [Test]
        public virtual void TestThreadCorrectness()
        {
            var NUM_THREADS = 4;
            var CACHE_SIZE = 512;
            var OBJ_COUNT = 3 * CACHE_SIZE;

            var c = new DoubleBarrelLRUCache<CloneableObject, object>(1024);

            var objs = new CloneableObject[OBJ_COUNT];
            for (var i = 0; i < OBJ_COUNT; i++)
            {
                objs[i] = new CloneableObject(new object());
            }

            var threads = new CacheThread[NUM_THREADS];
            var endTime = (long)((DateTime.Now.Subtract(new DateTime(1970, 1, 1))
                .Add(TimeSpan.FromMilliseconds(1000))).TotalMilliseconds);
            //long endTime = System.currentTimeMillis() + 1000L;
            for (var i = 0; i < NUM_THREADS; i++)
            {
                threads[i] = new CacheThread(this, c, objs, endTime);
                threads[i].Start();
            }
            for (var i = 0; i < NUM_THREADS; i++)
            {
                threads[i].Join();
                //assert !threads[i].failed;
            }
            //System.out.println("hits=" + totHit + " misses=" + totMiss);
        }

        public class CloneableObject : DoubleBarrelLRUCache.CloneableKey
        {
            private object value;

            public CloneableObject(object value)
            {
                this.value = value;
            }

            public override bool Equals(object other)
            {
                return this.value.Equals(((CloneableObject)other).value);
            }

            public override int GetHashCode()
            {
                return value.GetHashCode();
            }

            public override DoubleBarrelLRUCache.CloneableKey Clone()
            {
                return new CloneableObject(value);
            }
        }

        public class CloneableInteger : DoubleBarrelLRUCache.CloneableKey
        {
            private int value;

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

            public override DoubleBarrelLRUCache.CloneableKey Clone()
            {
                return new CloneableInteger(value);
            }
        }
    }
}
