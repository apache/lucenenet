using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using JCG = J2N.Collections.Generic;
#nullable enable

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

        private static IEnumerable<TestCaseData> StringKeyDictionaryCases()
        {
            yield return new TestCaseData((Func<IDictionary<string, string>>)(() => new JCG.Dictionary<string, string>()))
                .SetName("JCG_Dictionary_StringKey");

            yield return new TestCaseData((Func<IDictionary<string, string>>)(() => new JCG.OrderedDictionary<string, string>()))
                .SetName("JCG_OrderedDictionary_StringKey");

            yield return new TestCaseData((Func<IDictionary<string, string>>)(() => new JCG.SortedDictionary<string, string>()))
                .SetName("JCG_SortedDictionary_StringKey");

            yield return new TestCaseData((Func<IDictionary<string, string>>)(() => new Dictionary<string, string>()))
                .SetName("SCG_Dictionary_StringKey");

            yield return new TestCaseData((Func<IDictionary<string, string>>)(() => new ConcurrentDictionary<string, string>()))
                .SetName("SCG_ConcurrentDictionary_StringKey");

            yield return new TestCaseData((Func<IDictionary<string, string>>)(() => new SortedDictionary<string, string>()))
                .SetName("SCG_SortedDictionary_StringKey");

            yield return new TestCaseData((Func<IDictionary<string, string>>)(() => new SortedList<string, string>()))
                .SetName("SCG_SortedList_StringKey");
        }

        private static IEnumerable<TestCaseData> IntKeyDictionaryCases()
        {
            yield return new TestCaseData((Func<IDictionary<int, string>>)(() => new JCG.Dictionary<int, string>()))
                .SetName("JCG_Dictionary_IntKey");

            yield return new TestCaseData((Func<IDictionary<int, string>>)(() => new JCG.OrderedDictionary<int, string>()))
                .SetName("JCG_OrderedDictionary_IntKey");

            yield return new TestCaseData((Func<IDictionary<int, string>>)(() => new JCG.SortedDictionary<int, string>()))
                .SetName("JCG_SortedDictionary_IntKey");

            yield return new TestCaseData((Func<IDictionary<int, string>>)(() => new Dictionary<int, string>()))
                .SetName("SCG_Dictionary_IntKey");

            yield return new TestCaseData((Func<IDictionary<int, string>>)(() => new ConcurrentDictionary<int, string>()))
                .SetName("SCG_ConcurrentDictionary_IntKey");

            yield return new TestCaseData((Func<IDictionary<int, string>>)(() => new SortedDictionary<int, string>()))
                .SetName("SCG_SortedDictionary_IntKey");

            yield return new TestCaseData((Func<IDictionary<int, string>>)(() => new SortedList<int, string>()))
                .SetName("SCG_SortedList_IntKey");
        }

        [Test, LuceneNetSpecific]
        [TestCaseSource(nameof(StringKeyDictionaryCases))]
        public void TestPut_StringKeys_AcrossCollections(Func<IDictionary<string, string>> createDictionary)
        {
            IDictionary<string, string> dictionary = createDictionary();
            dictionary["key1"] = "value1";
            dictionary["key2"] = "value2";

            string? oldFirst = dictionary.Put("key1", "value1.1");
            string? oldSecond = dictionary.Put("key3", "value3");

            Assert.AreEqual(3, dictionary.Count);
            Assert.AreEqual("value1.1", dictionary["key1"]);
            Assert.AreEqual("value2", dictionary["key2"]);
            Assert.AreEqual("value3", dictionary["key3"]);
            Assert.AreEqual("value1", oldFirst);
            Assert.IsNull(oldSecond);
        }

        [Test, LuceneNetSpecific]
        [TestCaseSource(nameof(IntKeyDictionaryCases))]
        public void TestPut_IntKeys_AcrossCollections(Func<IDictionary<int, string>> createDictionary)
        {
            IDictionary<int, string> dictionary = createDictionary();
            dictionary[1] = "value1";
            dictionary[2] = "value2";

            string? oldFirst = dictionary.Put(1, "value1.1");
            string? oldSecond = dictionary.Put(3, "value3");

            Assert.AreEqual(3, dictionary.Count);
            Assert.AreEqual("value1.1", dictionary[1]);
            Assert.AreEqual("value2", dictionary[2]);
            Assert.AreEqual("value3", dictionary[3]);
            Assert.AreEqual("value1", oldFirst);
            Assert.IsNull(oldSecond);
        }

        [Test, LuceneNetSpecific]
        public void TestPut_StringOverloads_DirectAndInterface()
        {
            var jcgDict = new JCG.Dictionary<string, string> { ["a"] = "1" };
            Assert.AreEqual("1", jcgDict.Put("a", "2"));
            Assert.IsNull(jcgDict.Put("b", "3"));
            Assert.AreEqual("2", jcgDict["a"]);
            Assert.AreEqual("3", jcgDict["b"]);

            var orderedDict = new JCG.OrderedDictionary<string, string> { ["a"] = "1" };
            Assert.AreEqual("1", orderedDict.Put("a", "2"));
            Assert.IsNull(orderedDict.Put("b", "3"));
            Assert.AreEqual("2", orderedDict["a"]);
            Assert.AreEqual("3", orderedDict["b"]);

            IDictionary<string, string> interfaceDict = new Dictionary<string, string> { ["a"] = "1" };
            Assert.AreEqual("1", interfaceDict.Put("a", "2"));
            Assert.IsNull(interfaceDict.Put("b", "3"));
            Assert.AreEqual("2", interfaceDict["a"]);
            Assert.AreEqual("3", interfaceDict["b"]);
        }
    }
}
