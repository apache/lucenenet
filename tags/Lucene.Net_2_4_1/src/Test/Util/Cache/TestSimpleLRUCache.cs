/**
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

using NUnit.Framework;

namespace Lucene.Net.Util.Cache
{
    [TestFixture]
    public class TestSimpleLRUCache
    {
        [Test]
        public void TestLRUCache()
        {
            int n = 100;
            object dummy = new object();

            Cache cache = new SimpleLRUCache(n);

            for (int i = 0; i < n; i++)
            {
                cache.Put(i, dummy);
            }

            // access every 2nd item in cache
            for (int i = 0; i < n; i += 2)
            {
                Assert.IsNotNull(cache.Get(i));
            }

            // add n/2 elements to cache, the ones that weren't
            // touched in the previous loop should now be thrown away
            for (int i = n; i < n + (n / 2); i++)
            {
                cache.Put(i, dummy);
            }

            // access every 4th item in cache
            for (int i = 0; i < n; i += 4)
            {
                Assert.IsNotNull(cache.Get(i));
            }

            // add 3/4n elements to cache, the ones that weren't
            // touched in the previous loops should now be thrown away
            for (int i = n; i < n + (n * 3 / 4); i++)
            {
                cache.Put(i, dummy);
            }

            // access every 4th item in cache
            for (int i = 0; i < n; i += 4)
            {
                Assert.IsNotNull(cache.Get(i));
            }
        }

        [Test]
        public void TestLRUCache_LUCENENET_190()
        {
            SimpleLRUCache cache = new SimpleLRUCache(3);
                                                        //Item=>LastAccessTime
            cache.Put("a", "a");                        //a=>1
            cache.Put("b", "b");                        //b=>2
            cache.Put("c", "c");                        //c=>3
            Assert.IsNotNull(cache.Get("a"), "DBG1");   //a=>4
            Assert.IsNotNull(cache.Get("b"), "DBG2");   //b=>5
            Assert.IsNotNull(cache.Get("c"), "DBG3");   //c=>6
            cache.Put("d", "d");                        //d=>7 ,remove a
            Assert.IsNull(cache.Get("a"), "DBG4");      //a is removed already
            Assert.IsNotNull(cache.Get("c"), "DBG5");   //c=>8
            cache.Put("e", "e");                        //e=>9 ,remove b
            cache.Put("f", "f");                        //f=>10 ,remove d
            Assert.IsNotNull(cache.Get("c"), "DBG6");   //c=>11
                                                        //final cache: e=>9,f=>10,c=>11
        }
    }
}
