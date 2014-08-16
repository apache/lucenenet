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

namespace Lucene.Net.Util
{
    using Random;
    using Support;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

 
    public class TestWeakIdentityMap : LuceneTestCase
    {

        [Test]
        public async virtual void TestSimpleHashMap()
        {
            var map = WeakIdentityMap<string, string>.NewHashMap(this.Random.NextBoolean());
            // we keep strong references to the keys,
            // so WeakIdentityMap will not forget about them:
            string key1 = "foo",
                key2 = "foo",
                key3 = "foo",
                key4 = null;



            // Java Version assumes that the 3 keys
            // are not the same instance.  In .NET all 3 keys will 
            // be considered the same reference.
           
            Equal(key1, key2);
            Equal(key1, key3);
            Equal(key2, key3);

            // try null key & check its iterator also return null:
// ReSharper disable once ExpressionIsAlwaysNull
            map.Put(key4, "null");
            {
                var iterator = map.Keys.GetEnumerator();
                Ok(iterator.MoveNext());
                Null(iterator.Current);
                Ok(!iterator.MoveNext());
                Ok(!iterator.MoveNext());
            }
            // 2 more keys:
            map.Put(key1, "bar1");
            map.Put(key2, "bar2");

            Equal(2, map.Count);

            // this will not work in .NET, key1 is the same as key2
            // Equal("bar1", map.Get(key1));
            // Equal(null, map.Get(key3));
            Equal("bar2", map.Get(key2));
            Equal("null", map.Get(null));

            Ok(map.ContainsKey(key1));
            Ok(map.ContainsKey(key2));
            Ok(map.ContainsKey(key3));
            Ok(map.ContainsKey(null));

            // repeat and check that we have no double entries
            map.Put(key1, "bar1");
            map.Put(key2, "bar2");
            map.Put(null, "null");

            Equal(2, map.Count);

            // this will not work in .NET, key1, key2, key3 are the same reference
            // Equal("bar1", map.Get(key1));
            // Equal(null, map.Get(key3));
            Equal("bar2", map.Get(key2));
        
            Equal("null", map.Get(null));

            Ok(map.ContainsKey(key1));
            Ok(map.ContainsKey(key2));
            Ok(map.ContainsKey(key3));
            Ok(map.ContainsKey(null));

            map.Remove(null);
            Equal(1, map.Count);
            map.Remove(key1);
            Equal(0, map.Count);

            key1 = "a";
            key2 = "b";
            key3 = "c";

            map.Put(key1, "bar1");
            map.Put(key2, "bar2");
            map.Put(key3, "bar1");

            int c = 0, keysAssigned = 0;
            for (var iterator = map.Keys.GetEnumerator(); iterator.MoveNext(); )
            {
               
                var k = iterator.Current;
                Ok(k == key1 || k == key2 || k == key3);
                keysAssigned += (k == key1) ? 1 : ((k == key2) ? 2 : 4);
                c++;
            }
            Equal(3, c);
            Equal(1 + 2 + 4, keysAssigned, "all keys must have been seen");

            c = 0;
            for (var iterator = map.Values.GetEnumerator(); iterator.MoveNext(); )
            {
                var v = iterator.Current;
                Ok(v.StartsWith("bar"));
                c++;
            }
            Equal(3, c);

            // clear strong refs
            // ReSharper disable RedundantAssignment
            key1 = null;
            key2 = null;
            key3 = null;

            // check that GC does not cause problems in reap() method, wait 1 second and let GC work:
            var size = map.Count;
            for (var i = 0; size > 0 && i < 10; i++)
            {
               
                GC.Collect();
                var newSize = map.Count;
                Ok(size >= newSize, "previousSize(" + size + ")>=newSize(" + newSize + ")");
                size = newSize;
                    
                await Task.Delay(new TimeSpan(100L));
                c = 0;
                for (var iteration = map.Keys.GetEnumerator(); iteration.MoveNext(); )
                {
                    NotNull(iteration.Current);
                    c++;
                }
                newSize = map.Count;
                Ok(size >= c, "previousSize(" + size + ")>=iteratorSize(" + c + ")");
                Ok(c >= newSize, "iteratorSize(" + c + ")>=newSize(" + newSize + ")");
                size = newSize;
            }

            map.Clear();
            Equal(0, map.Count);
            Ok(map.Empty);

            IEnumerator<string> it = map.Keys.GetEnumerator();
            Ok(!it.MoveNext());
            /*try
            {
              it.Next();
              Assert.Fail("Should throw NoSuchElementException");
            }
            catch (NoSuchElementException nse)
            {
            }*/

            key1 = "foo1";
            key2 = "foo2";
            map.Put(key1, "bar1");
            map.Put(key2, "bar2");
            Equal(2, map.Count);

            map.Clear();
            Equal(0, map.Count);
            Ok(map.Empty);
        }

        [Test]
        public async virtual void TestConcurrentHashMap()
        {
            // don't make threadCount and keyCount random, otherwise easily OOMs or fails otherwise:
            const int threadCount = 8, keyCount = 1024;
            var tasks = new List<Task>();
            var map = WeakIdentityMap<object, int?>.NewConcurrentHashMap(this.Random.NextBoolean());
            // we keep strong references to the keys,
            // so WeakIdentityMap will not forget about them:
            var keys = new AtomicReferenceArray<object>(keyCount);
            for (var j = 0; j < keyCount; j++)
            {
                keys[j] = new object();
            }

            for (var t = 0; t < threadCount; t++)
            {
                var rnd = new Random(this.Random.Next());
                tasks.Add(Task.Run(() =>
                {
                       
                    var helper = new RunnableAnonymousInnerClassHelper(this, keyCount, map, keys, rnd);
                    helper.Run();
                          
                      
                       
                }));
            }
            await Task.WhenAll(tasks);

            var taskWithException = tasks.FirstOrDefault(o => o.Exception != null);
            if (taskWithException != null)
                throw taskWithException.Exception;
            

            // clear strong refs
            for (var j = 0; j < keyCount; j++)
            {
                keys[j] = null;
            }

            // check that GC does not cause problems in reap() method:
            var size = map.Count;
            for (var i = 0; size > 0 && i < 10; i++)
            {

                GC.Collect();
                int newSize = map.Count,
                    c = 0;

                Ok(size >= newSize, "previousSize(" + size + ")>=newSize(" + newSize + ")");
                size = newSize;
                await Task.Delay(100);

                for (var iterator = map.Keys.GetEnumerator(); iterator.MoveNext(); )
                {
                    NotNull(iterator.Current);
                    c++;
                }
                newSize = map.Count;
                Ok(size >= c, "previousSize(" + size + ")>=iteratorSize(" + c + ")");
                Ok(c >= newSize, "iteratorSize(" + c + ")>=newSize(" + newSize + ")");
                size = newSize;
            }
        }

        private class RunnableAnonymousInnerClassHelper 
        {
            private readonly TestWeakIdentityMap outerInstance;

            private readonly int keyCount;
            private readonly WeakIdentityMap<object, int?> map;
            private readonly AtomicReferenceArray<object> keys;
            private readonly Random rnd;


            public RunnableAnonymousInnerClassHelper(TestWeakIdentityMap outerInstance, int keyCount, WeakIdentityMap<object, int?> map, AtomicReferenceArray<object> keys, Random rnd)
            {
                this.outerInstance = outerInstance;
                this.keyCount = keyCount;
                this.map = map;
                this.keys = keys;
                this.rnd = rnd;
            }

            public void Run()
            {
// ReSharper disable once InvokeAsExtensionMethod
                var count =  this.outerInstance.AtLeast(10000);
                for (var i = 0; i < count; i++)
                {
                    var j = rnd.Next(keyCount);
                    switch (rnd.Next(5))
                    {
                        case 0:
                            map.Put(keys[j], Convert.ToInt32(j));
                            break;
                        case 1:
                            var v = map.Get(keys[j]);
                            if (v != null)
                            {
                                Equal(j, (int)v);
                            }
                            break;
                        case 2:
                            map.Remove(keys[j]);
                            break;
                        case 3:
                            // renew key, the old one will be GCed at some time:
                            keys[j] = new object();
                            break;
                        case 4:
                            // check iterator still working
                            for (var it = map.Keys.GetEnumerator(); it.MoveNext(); )
                            {
                                NotNull(it.Current);
                            }
                            break;
                        default:
                            throw new LuceneAssertionException("This point should not be reached");
                    }
                }
            }
        }
    }
}