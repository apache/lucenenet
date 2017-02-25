using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Support
{
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

    [TestFixture]
    public class TestHashMap
    {
        protected virtual HashMap<TKey, TValue> GetNewHashMap<TKey, TValue>()
        {
            return new HashMap<TKey, TValue>();
        }

        private HashMap<string,string> GetDefaultHashMap1()
        {
            var hm = GetNewHashMap<string, string>();
            hm.Add("key1", "value1");
            hm.Add("key2", "value2");
            return hm;
        }

        [Test, LuceneNetSpecific]
        public virtual void TestKeyEnumeration()
        {
            var keys = new List<string> {"key1", "key2"};
            var dict = GetDefaultHashMap1();
            foreach (var key in dict.Keys)
            {
                Assert.IsTrue(keys.Contains(key));
            }

            keys.Add(null);
            dict[null] = "nullvalue";
            foreach (var key in dict.Keys)
            {
                Assert.IsTrue(keys.Contains(key));
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestValueEnumeration()
        {
            var values = new List<string> { "value1", "value2" };
            var dict = GetDefaultHashMap1();
            foreach (var value in dict.Values)
            {
                Assert.IsTrue(values.Contains(value));
            }

            values.Add("nullvalue");
            dict[null] = "nullvalue";
            foreach (var value in dict.Values)
            {
                Assert.IsTrue(values.Contains(value));
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestKeyValuePairEnumeration()
        {
            var dict = GetDefaultHashMap1();
            Action<KeyValuePair<string, string>> act = kvp =>
                {
                    Assert.IsNotNull(kvp);
                    if (kvp.Key == "key1")
                    {
                        Assert.AreEqual("value1", kvp.Value);
                    }
                    else if (kvp.Key == "key2")
                    {
                        Assert.AreEqual("value2", kvp.Value);
                    }
                    else if (kvp.Key == null)
                    {
                        Assert.AreEqual("nullval", kvp.Value);
                    }
                };

            foreach (var kvp in dict)
            {
                act.Invoke(kvp);
            }

            dict.Add(null, "nullval");
            foreach (var kvp in dict)
            {
                act.Invoke(kvp);
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestContainsNullKey()
        {
            var dict = GetDefaultHashMap1();
            Assert.IsFalse(dict.ContainsKey(null));
            Assert.IsNull(dict[null]);

            dict.Add(null, "value");
            Assert.IsTrue(dict.ContainsKey(null));
            Assert.AreEqual("value", dict[null]);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestContainsKey()
        {
            var dict = GetDefaultHashMap1();
            Assert.IsTrue(dict.ContainsKey("key1"));
            Assert.IsTrue(dict.ContainsKey("key2"));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAdd_NoNullKeys_NullValues()
        {
            var dict = GetNewHashMap<string, string>();
            dict.Add("key1", null);
            dict.Add("key2", "value2");

            Assert.AreEqual(2, dict.Count);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAdd_WithNullKeys_NoNullValues()
        {
            var dict = GetNewHashMap<string, string>();
            dict.Add("key1", "value1");
            dict.Add(null, "nullValue");

            Assert.AreEqual(2, dict.Count);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestGetWithNonExistantKey_EmptyCollection()
        {
            var dict = GetNewHashMap<string, string>();
            Assert.IsNull(dict["nothing"]);
            Assert.IsNull(dict[null]);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestGetWithNonExistantKey()
        {
            var dict = GetDefaultHashMap1();
            Assert.IsNull(dict["nothing"]);
            Assert.IsNull(dict[null]);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAddsUpdate_NotThrowException()
        {
            var dict = GetNewHashMap<string, string>();

            dict.Add("key1", "value1");
            Assert.AreEqual("value1", dict["key1"]);
            Assert.AreEqual(1, dict.Count);

            dict.Add("key1", "value2");
            Assert.AreEqual("value2", dict["key1"], "Value was not updated by Add!");
            Assert.AreEqual(1, dict.Count);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIndexersUpdate_NotThrowException()
        {
            var dict = GetNewHashMap<string, string>();

            dict["key1"] = "value1";
            Assert.AreEqual("value1", dict["key1"]);
            Assert.AreEqual(1, dict.Count);

            dict["key1"] = "value2";
            Assert.AreEqual("value2", dict["key1"], "Value was not updated by Add!");
            Assert.AreEqual(1, dict.Count);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestWithValueType()
        {
            // Make sure default value types are stored in the internal dictionary
            // and not the _nullValue variable
            var dict = GetNewHashMap<int, string>();
            
            dict[2] = "MyString";
            dict[0] = "OtherString";

            Assert.AreEqual("MyString", dict[2]);
            Assert.AreEqual("OtherString", dict[0]);
            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual(2, dict.Count, "0 (default(int)) was not stored in internal dict!");
        }
    }
}
