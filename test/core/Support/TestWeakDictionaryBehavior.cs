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

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestWeakDictionaryBehavior
    {
        IDictionary<object, object> dictionary;

        public static IDictionary<object, object> CreateDictionary()
        {
            return new WeakDictionary<object, object>();
        }


        private void CallGC()
        {
            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        [SetUp]
        public void Setup()
        {
            dictionary = CreateDictionary();
        }

        [Test]
        public void Test_Dictionary_Add()
        {
            string key = "A";

            dictionary.Add(key, "value");
            Assert.IsTrue(dictionary.ContainsKey(key));
            Assert.AreEqual("value", dictionary[key]);
            Assert.AreEqual(1, dictionary.Count);

            CollectionAssert.AreEquivalent(dictionary.Values, new object[] { "value" });
        }

        [Test]
        public void Test_Dictionary_Add_2()
        {
            string key = "A";
            string key2 = "B";

            dictionary.Add(key, "value");
            dictionary.Add(key2, "value2");
            Assert.IsTrue(dictionary.ContainsKey(key));
            Assert.IsTrue(dictionary.ContainsKey(key2));
            Assert.AreEqual("value", dictionary[key]);
            Assert.AreEqual("value2", dictionary[key2]);
            Assert.AreEqual(2, dictionary.Count);

            CollectionAssert.AreEquivalent(dictionary.Values, new object[] { "value", "value2" });
        }

        [Test]
        public void Test_Keys()
        {
            string key = "A";
            string key2 = "B";

            dictionary.Add(key, "value");
            CollectionAssert.AreEquivalent(dictionary.Keys, new object[] { key });

            dictionary.Add(key2, "value2");
            CollectionAssert.AreEquivalent(dictionary.Keys, new object[] { key, key2 });
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Dictionary_Add_Null()
        {
            dictionary.Add(null, "value");
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Dictionary_Set_Null()
        {
            dictionary[null] = "value";
        }

        [Test]
        public void Test_Dictionary_AddReplace()
        {
            string key = "A";
            string key2 = "a".ToUpper();

            dictionary.Add(key, "value");
            dictionary[key2] = "value2";

            Assert.AreEqual(1, dictionary.Count);
            Assert.IsTrue(dictionary.ContainsKey(key));
            Assert.AreEqual("value2", dictionary[key]);
        }

        [Test]
        public void Test_Dictionary_AddRemove()
        {
            string key = "A";

            dictionary.Add(key, "value");
            dictionary.Remove(key);

            Assert.AreEqual(0, dictionary.Count);
            Assert.IsFalse(dictionary.ContainsKey(key));
            Assert.IsNull(dictionary[key]);
        }

        [Test]
        public void Test_Dictionary_Clear()
        {
            string key = "A";

            dictionary.Add(key, "value");
            dictionary.Clear();

            Assert.AreEqual(0, dictionary.Count);
            Assert.IsFalse(dictionary.ContainsKey(key));
            Assert.IsNull(dictionary[key]);
        }

        [Test]
        public void Test_Dictionary_AddRemove_2()
        {
            string key = "A";

            dictionary.Add(key, "value");
            dictionary.Remove(key);
            dictionary.Remove(key);

            Assert.AreEqual(0, dictionary.Count);
            Assert.IsFalse(dictionary.ContainsKey(key));
            Assert.IsNull(dictionary[key]);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Dictionary_Get_Null()
        {
            object value = dictionary[null];
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_Dictionary_Remove_Null()
        {
            dictionary.Remove(null);
        }

        [Test]
        public void Test_Dictionary_GetEnumerator()
        {
            string key = "A";

            dictionary.Add(key, "value");

            var de = dictionary.GetEnumerator();
            Assert.IsTrue(de.MoveNext());
            Assert.AreEqual(key, de.Current.Key);
            Assert.AreEqual("value", de.Current.Value);
        }

        [Test]
        public void Test_Dictionary_ForEach()
        {
            string key = "A";

            dictionary.Add(key, "value");

            foreach (var de in dictionary)
            {
                Assert.AreEqual(key, de.Key);
                Assert.AreEqual("value", de.Value);
            }
        }

        [Test]
        public void Test_Collisions()
        {
            //Create 2 keys with same hashcode but that are not equal
            CollisionTester key1 = new CollisionTester(1, 100);
            CollisionTester key2 = new CollisionTester(2, 100);

            dictionary.Add(key1, "value1");
            dictionary.Add(key2, "value2");

            Assert.AreEqual("value1", dictionary[key1]);
            Assert.AreEqual("value2", dictionary[key2]);

            dictionary.Remove(key1);
            Assert.AreEqual(null, dictionary[key1]);
        }

        [Test]
        public void Test_Weak_1()
        {
            BigObject key = new BigObject(1);
            BigObject key2 = new BigObject(2);

            dictionary.Add(key, "value");
            Assert.AreEqual("value", dictionary[key]);

            key = null;
            CallGC();

            dictionary.Add(key2, "value2");
            Assert.AreEqual(1, dictionary.Count);
        }

        [Test]
        public void Test_Weak_2()
        {
            BigObject key = new BigObject(1);
            BigObject key2 = new BigObject(2);
            BigObject key3 = new BigObject(3);

            dictionary.Add(key, "value");
            dictionary.Add(key2, "value2");
            Assert.AreEqual("value", dictionary[key]);

            key = null;
            CallGC();

            dictionary.Add(key3, "value3");

            Assert.AreEqual(2, dictionary.Count);
            Assert.IsNotNull(key2);
        }

        [Test]
        public void Test_Weak_ForEach()
        {
            BigObject[] keys1 = new BigObject[20];
            BigObject[] keys2 = new BigObject[20];

            for (int i = 0; i < keys1.Length; i++)
            {
                keys1[i] = new BigObject(i);
                dictionary.Add(keys1[i], "value");
            }
            for (int i = 0; i < keys2.Length; i++)
            {
                keys2[i] = new BigObject(i);
                dictionary.Add(keys2[i], "value");
            }

            Assert.AreEqual(40, dictionary.Count);

            keys2 = null;
            int count = 0;
            foreach (var de in dictionary)
            {
                CallGC();
                count++;
            }

            Assert.LessOrEqual(20, count);
            Assert.Greater(40, count);
            Assert.IsNotNull(keys1);
        }
    }
}
