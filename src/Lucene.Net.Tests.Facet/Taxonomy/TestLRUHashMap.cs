// Lucene version compatibility level 4.8.1
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Facet.Taxonomy
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
    public class TestLRUHashMap : FacetTestCase
    {
        // testLRU() tests that the specified size limit is indeed honored, and
        // the remaining objects in the map are indeed those that have been most
        // recently used
        [Test]
        public virtual void TestLru()
        {
            LruDictionary<string, string> lru = new LruDictionary<string, string>(3);
            Assert.AreEqual(0, lru.Count);
            lru.Put("one", "Hello world");
            Assert.AreEqual(1, lru.Count);
            lru.Put("two", "Hi man");
            Assert.AreEqual(2, lru.Count);
            lru.Put("three", "Bonjour");
            Assert.AreEqual(3, lru.Count);
            lru.Put("four", "Shalom");
            Assert.AreEqual(3, lru.Count);
            Assert.IsNotNull(lru.Get("three"));
            Assert.IsNotNull(lru.Get("two"));
            Assert.IsNotNull(lru.Get("four"));
            Assert.IsNull(lru.Get("one"));
            lru.Put("five", "Yo!");
            Assert.AreEqual(3, lru.Count);
            Assert.IsNull(lru.Get("three")); // three was last used, so it got removed
            Assert.IsNotNull(lru.Get("five"));
            lru.Get("four");
            lru.Put("six", "hi");
            lru.Put("seven", "hey dude");
            Assert.AreEqual(3, lru.Count);
            Assert.IsNull(lru.Get("one"));
            Assert.IsNull(lru.Get("two"));
            Assert.IsNull(lru.Get("three"));
            Assert.IsNotNull(lru.Get("four"));
            Assert.IsNull(lru.Get("five"));
            Assert.IsNotNull(lru.Get("six"));
            Assert.IsNotNull(lru.Get("seven"));

            // LUCENENET specific tests to ensure Put is implemented correctly
            Assert.IsNull(lru.Put("ten", "oops"));
            assertEquals("oops", lru.Put("ten", "not oops"));
            assertEquals("not oops", lru.Put("ten", "new value"));
            assertEquals("new value", lru.Put("ten", "new value2"));
        }
    }
}