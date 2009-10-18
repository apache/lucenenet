/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections;
using System.Diagnostics;
using NUnit.Framework;

namespace Lucene.Net._SupportClass
{
    [TestFixture]
    public class TestWeakHashTable
    {
        [Test]
        public void A_TestBasicOps()
        {
            IDictionary weakHashTable = TestWeakHashTableBehavior.CreateDictionary();// new SupportClass.TjWeakHashTable();
            Hashtable realHashTable = new Hashtable();

            SmallObject[] so = new SmallObject[100];
            for (int i = 0; i < 20000; i++)
            {
                SmallObject key = new SmallObject(i);
                SmallObject value = key;
                so[i / 200] = key;
                realHashTable.Add(key, value);
                weakHashTable.Add(key, value);
            }

            Assert.AreEqual(weakHashTable.Count, realHashTable.Count);

            ICollection keys = (ICollection)realHashTable.Keys;

            foreach (SmallObject key in keys)
            {
                Assert.AreEqual(((SmallObject)realHashTable[key]).i,
                                ((SmallObject)weakHashTable[key]).i);

                Assert.IsTrue(realHashTable[key].Equals(weakHashTable[key]));
            }


            ICollection values1 = (ICollection)weakHashTable.Values;
            ICollection values2 = (ICollection)realHashTable.Values;
            Assert.AreEqual(values1.Count, values2.Count);

            realHashTable.Remove(new SmallObject(10000));
            weakHashTable.Remove(new SmallObject(10000));
            Assert.AreEqual(weakHashTable.Count, 20000);
            Assert.AreEqual(realHashTable.Count, 20000);

            for (int i = 0; i < so.Length; i++)
            {
                realHashTable.Remove(so[i]);
                weakHashTable.Remove(so[i]);
                Assert.AreEqual(weakHashTable.Count, 20000 - i - 1);
                Assert.AreEqual(realHashTable.Count, 20000 - i - 1);
            }

            //After removals, compare the collections again.
            ICollection keys2 = (ICollection)realHashTable.Keys;
            foreach (SmallObject o in keys2)
            {
                Assert.AreEqual(((SmallObject)realHashTable[o]).i,
                                ((SmallObject)weakHashTable[o]).i);
                Assert.IsTrue(realHashTable[o].Equals(weakHashTable[o]));
            }
        }

        [Test]
        public void B_TestOutOfMemory()
        {
            IDictionary wht = TestWeakHashTableBehavior.CreateDictionary(); //new SupportClass.TjWeakHashTable();

            for (int i = 0; i < 1024 * 8 + 32; i++) // requested Mem. > 8GB
            {
                wht.Add(new BigObject(i), i);
            }

            GC.Collect();
            Console.WriteLine("Passed out of memory exception.");
        }

        private int GetMemUsageInKB()
        {
            return Process.GetCurrentProcess().WorkingSet / 1024;
        }

        [Test]
        public void C_TestMemLeakage()
        {

            IDictionary wht = TestWeakHashTableBehavior.CreateDictionary(); //new SupportClass.TjWeakHashTable();

            GC.Collect();
            int initialMemUsage = GetMemUsageInKB();

            Console.WriteLine("Initial MemUsage=" + initialMemUsage);
            for (int i = 0; i < 10000; i++)
            {
                wht.Add(new BigObject(i), i);
                if (i % 100 == 0)
                {
                    int mu = GetMemUsageInKB();
                    Console.WriteLine(i.ToString() + ") MemUsage=" + mu);
                }
            }

            GC.Collect();
            int memUsage = GetMemUsageInKB();
            if (memUsage > initialMemUsage * 2) Assert.Fail("Memory Leakage.MemUsage = " + memUsage);
        }
    }

    [TestFixture]
    public class TestWeakHashTableBehavior
    {
        IDictionary dictionary;

        public static IDictionary CreateDictionary()
        {
            return new SupportClass.WeakHashTable();
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
            Assert.IsTrue(dictionary.Contains(key));
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
            Assert.IsTrue(dictionary.Contains(key));
            Assert.IsTrue(dictionary.Contains(key2));
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
        [ExpectedException(typeof(ArgumentException))]
        public void Test_Dictionary_AddTwice()
        {
            string key = "A";

            dictionary.Add(key, "value");
            dictionary.Add(key, "value2");
        }

        [Test]
        public void Test_Dictionary_AddReplace()
        {
            string key = "A";
            string key2 = "a".ToUpper();

            dictionary.Add(key, "value");
            dictionary[key2] = "value2";

            Assert.AreEqual(1, dictionary.Count);
            Assert.IsTrue(dictionary.Contains(key));
            Assert.AreEqual("value2", dictionary[key]);
        }

        [Test]
        public void Test_Dictionary_AddRemove()
        {
            string key = "A";

            dictionary.Add(key, "value");
            dictionary.Remove(key);

            Assert.AreEqual(0, dictionary.Count);
            Assert.IsFalse(dictionary.Contains(key));
            Assert.IsNull(dictionary[key]);
        }

        [Test]
        public void Test_Dictionary_Clear()
        {
            string key = "A";

            dictionary.Add(key, "value");
            dictionary.Clear();

            Assert.AreEqual(0, dictionary.Count);
            Assert.IsFalse(dictionary.Contains(key));
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
            Assert.IsFalse(dictionary.Contains(key));
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
        public void Test_Dictionary_CopyTo()
        {
            string key = "A";

            dictionary.Add(key, "value");
            DictionaryEntry[] a = new DictionaryEntry[1];
            dictionary.CopyTo(a, 0);

            DictionaryEntry de = (DictionaryEntry)a[0];
            Assert.AreEqual(key, de.Key);
            Assert.AreEqual("value", de.Value);
        }

        [Test]
        public void Test_Dictionary_GetEnumerator()
        {
            string key = "A";

            dictionary.Add(key, "value");

            IDictionaryEnumerator de = dictionary.GetEnumerator();
            Assert.IsTrue(de.MoveNext());
            Assert.AreEqual(key, de.Key);
            Assert.AreEqual("value", de.Value);
        }

        [Test]
        public void Test_Dictionary_ForEach()
        {
            string key = "A";

            dictionary.Add(key, "value");

            IEnumerable enumerable = dictionary;

            foreach (DictionaryEntry de in enumerable)
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
            foreach (DictionaryEntry de in dictionary)
            {
                CallGC();
                count++;
            }

            Assert.LessOrEqual(20, count);
            Assert.Greater(40, count);
            Assert.IsNotNull(keys1);
        }
    }

    class CollisionTester
    {
        int id;
        int hashCode;

        public CollisionTester(int id, int hashCode)
        {
            this.id = id;
            this.hashCode = hashCode;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj is CollisionTester)
            {
                return this.id == ((CollisionTester)obj).id;
            }
            else
                return base.Equals(obj);
        }
    }

    [TestFixture]
    public class TestWeakHashTablePerformance
    {
        IDictionary dictionary;
        SmallObject[] keys;


        [SetUp]
        public void Setup()
        {
            dictionary = TestWeakHashTableBehavior.CreateDictionary();
        }

        private void Fill(IDictionary dictionary)
        {
            foreach (SmallObject key in keys)
                dictionary.Add(key, "value");
        }

        [TestFixtureSetUp]
        public void TestSetup()
        {
            keys = new SmallObject[100000];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = new SmallObject(i);
        }

        [Test]
        public void Test_Performance_Add()
        {
            for (int i = 0; i < 10; i++)
            {
                dictionary.Clear();
                Fill(dictionary);
            }
        }

        [Test]
        public void Test_Performance_Remove()
        {
            for (int i = 0; i < 10; i++)
            {
                Fill(dictionary);
                foreach (SmallObject key in keys)
                    dictionary.Remove(key);
            }
        }

        [Test]
        public void Test_Performance_Replace()
        {
            for (int i = 0; i < 10; i++)
            {
                foreach (SmallObject key in keys)
                    dictionary[key] = "value2";
            }
        }

        [Test]
        public void Test_Performance_Access()
        {
            Fill(dictionary);
            for (int i = 0; i < 10; i++)
            {
                foreach (SmallObject key in keys)
                {
                    object value = dictionary[key];
                }
            }
        }

        [Test]
        public void Test_Performance_Contains()
        {
            Fill(dictionary);
            for (int i = 0; i < 10; i++)
            {
                foreach (SmallObject key in keys)
                {
                    dictionary.Contains(key);
                }
            }
        }

        [Test]
        public void Test_Performance_Keys()
        {
            Fill(dictionary);
            for (int i = 0; i < 100; i++)
            {
                ICollection keys = dictionary.Keys;
            }
        }

        [Test]
        public void Test_Performance_ForEach()
        {
            Fill(dictionary);
            for (int i = 0; i < 10; i++)
            {
                foreach (DictionaryEntry de in dictionary)
                {

                }
            }
        }

        [Test]
        public void Test_Performance_CopyTo()
        {
            Fill(dictionary);
            DictionaryEntry[] array = new DictionaryEntry[dictionary.Count];

            for (int i = 0; i < 10; i++)
            {
                dictionary.CopyTo(array, 0);
            }
        }
    }

    internal class BigObject
    {
        public int i = 0;
        public byte[] buf = null;

        public BigObject(int i)
        {
            this.i = i;
            buf = new byte[1024 * 1024]; //1MB
        }
    }


    internal class SmallObject
    {
        public int i = 0;

        public SmallObject(int i)
        {
            this.i = i;
        }
    }
}