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
using NUnit.Framework;

namespace Lucene.Net.Support
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
            IDictionary wht = TestWeakHashTableBehavior.CreateDictionary();
            int OOMECount = 0;

            for (int i = 0; i < 1024 * 24 + 32; i++) // total requested Mem. > 24GB
            {
                try
                {
                    wht.Add(new BigObject(i), i);
                    if (i % 1024 == 0) Console.WriteLine("Requested Mem: " + i.ToString() + " MB");
                    OOMECount = 0;
                }
                catch (OutOfMemoryException oom)
                {
                    if (OOMECount++ > 10) throw new Exception("Memory Allocation Error in B_TestOutOfMemory");
                    //Try Again. GC will eventually release some memory.
                    Console.WriteLine("OOME WHEN i=" + i.ToString() + ". Try Again");
                    System.Threading.Thread.Sleep(10);
                    i--;
                    continue;
                }
            }

            GC.Collect();
            Console.WriteLine("Passed out of memory exception.");
        }

        private int GetMemUsageInKB()
        {
            return System.Diagnostics.Process.GetCurrentProcess().WorkingSet / 1024;
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
}