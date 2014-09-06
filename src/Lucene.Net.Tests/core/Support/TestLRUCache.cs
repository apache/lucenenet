/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using NUnit.Framework;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestLRUCache
    {
        [Test]
        public void Test()
        {
            Lucene.Net.Util.Cache.SimpleLRUCache<string, string> cache = new Lucene.Net.Util.Cache.SimpleLRUCache<string, string>(3);
            cache.Put("a", "a");
            cache.Put("b", "b");
            cache.Put("c", "c");
            Assert.IsNotNull(cache.Get("a"));
            Assert.IsNotNull(cache.Get("b"));
            Assert.IsNotNull(cache.Get("c"));
            cache.Put("d", "d");
            Assert.IsNull(cache.Get("a"));
            Assert.IsNotNull(cache.Get("c"));
            cache.Put("e", "e");
            cache.Put("f", "f");
            Assert.IsNotNull(cache.Get("c"));
        }
    }
}
