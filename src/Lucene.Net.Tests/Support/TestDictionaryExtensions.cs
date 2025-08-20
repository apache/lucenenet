using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;

namespace Lucene.Net.Support
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
    public class TestDictionaryExtensions : LuceneTestCase
    {
        [Test, LuceneNetSpecific]
        public void TestPutAll()
        {
            var dictionary1 = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };
            var dictionary2 = new Dictionary<string, string>
            {
                { "key1", "value1.1" },
                { "key3", "value3" }
            };

            dictionary1.PutAll(dictionary2);

            Assert.AreEqual(3, dictionary1.Count);
            Assert.AreEqual("value1.1", dictionary1["key1"]);
            Assert.AreEqual("value2", dictionary1["key2"]);
            Assert.AreEqual("value3", dictionary1["key3"]);
        }

        [Test, LuceneNetSpecific]
        public void TestPut()
        {
            var dictionary = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            var oldFirst = dictionary.Put("key1", "value1.1");
            var oldSecond = dictionary.Put("key3", "value3");

            Assert.AreEqual(3, dictionary.Count);
            Assert.AreEqual("value1.1", dictionary["key1"]);
            Assert.AreEqual("value2", dictionary["key2"]);
            Assert.AreEqual("value3", dictionary["key3"]);
            Assert.AreEqual("value1", oldFirst);
            Assert.IsNull(oldSecond);
        }
    }
}
