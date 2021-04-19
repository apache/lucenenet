using NUnit.Framework;
using RandomizedTesting.Generators;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Util
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
    public class TestSentinelIntSet : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            SentinelInt32Set set = new SentinelInt32Set(10, -1);
            Assert.IsFalse(set.Exists(50));
            set.Put(50);
            Assert.IsTrue(set.Exists(50));
            Assert.AreEqual(1, set.Count);
            Assert.AreEqual(-11, set.Find(10));
            Assert.AreEqual(1, set.Count);
            set.Clear();
            Assert.AreEqual(0, set.Count);
            Assert.AreEqual(50, set.Hash(50));
            //force a rehash
            for (int i = 0; i < 20; i++)
            {
                set.Put(i);
            }
            Assert.AreEqual(20, set.Count);
            Assert.AreEqual(24, set.RehashCount);
        }

        [Test]
        public virtual void TestRandom()
        {
            for (int i = 0; i < 10000; i++)
            {
                int initSz = Random.Next(20);
                int num = Random.Next(30);
                int maxVal = (Random.NextBoolean() ? Random.Next(50) : Random.Next(int.MaxValue)) + 1;

                ISet<int> a = new JCG.HashSet<int>(initSz);
                SentinelInt32Set b = new SentinelInt32Set(initSz, -1);

                for (int j = 0; j < num; j++)
                {
                    int val = Random.Next(maxVal);
                    bool exists = !a.Add(val);
                    bool existsB = b.Exists(val);
                    Assert.AreEqual(exists, existsB);
                    int slot = b.Find(val);
                    Assert.AreEqual(exists, slot >= 0);
                    b.Put(val);

                    Assert.AreEqual(a.Count, b.Count);
                }
            }
        }
    }
}