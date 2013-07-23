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
using NUnit.Framework;

namespace Lucene.Net.Util
{

    [TestFixture]
    public class TestRamUsageEstimator
    {

        [Test]
        public void TestSanity()
        {
            Assert.IsTrue(RamUsageEstimator.SizeOf("test string") > RamUsageEstimator.ShallowSizeOfInstance(typeof(string)));

            var holder = new Holder { holder = new Holder("string2", 5000L) };
            Assert.IsTrue(RamUsageEstimator.SizeOf(holder) > RamUsageEstimator.ShallowSizeOfInstance(typeof(Holder)));
            Assert.IsTrue(RamUsageEstimator.SizeOf(holder) > RamUsageEstimator.SizeOf(holder.holder));

            Assert.IsTrue(RamUsageEstimator.ShallowSizeOfInstance(typeof(HolderSubclass)) >= RamUsageEstimator.ShallowSizeOfInstance(typeof(Holder)));
            Assert.IsTrue(RamUsageEstimator.ShallowSizeOfInstance(typeof(Holder)) == RamUsageEstimator.ShallowSizeOfInstance(typeof(HolderSubclass2)));

            var strings = new string[] {
                    "test string",
                    "hollow", 
                    "catchmaster"
                };
            Assert.IsTrue(RamUsageEstimator.SizeOf(strings) > RamUsageEstimator.ShallowSizeOf(strings));
        }

        [Test]
        public void TestStaticOverloads()
        {
            var rnd = new Random();
            {
                var array = new byte[rnd.Next(1024)];
                Assert.AreEqual(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((Object)array));
            }

            {
                var array = new bool[rnd.Next(1024)];
                Assert.AreEqual(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((Object)array));
            }

            {
                var array = new char[rnd.Next(1024)];
                Assert.AreEqual(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((Object)array));
            }

            {
                var array = new short[rnd.Next(1024)];
                Assert.AreEqual(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((Object)array));
            }

            {
                var array = new int[rnd.Next(1024)];
                Assert.AreEqual(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((Object)array));
            }

            {
                var array = new float[rnd.Next(1024)];
                Assert.AreEqual(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((Object)array));
            }

            {
                var array = new long[rnd.Next(1024)];
                Assert.AreEqual(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((Object)array));
            }

            {
                var array = new double[rnd.Next(1024)];
                Assert.AreEqual(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((Object)array));
            }
        }

        [Test]
        public void TestReferenceSize()
        {
            if (!IsSupportedJVM())
            {
                Console.Error.WriteLine("WARN: Your JVM does not support certain Oracle/Sun extensions.");
                Console.Error.WriteLine(" Memory estimates may be inaccurate.");
                Console.Error.WriteLine(" Please report this to the Lucene mailing list.");
                Console.Error.WriteLine("JVM version: " + RamUsageEstimator.JVM_INFO_STRING);
                Console.Error.WriteLine("UnsupportedFeatures:");
                foreach (var f in RamUsageEstimator.GetUnsupportedFeatures())
                {
                    Console.Error.Write(" - " + f.ToString());
                    if (f == RamUsageEstimator.JvmFeature.OBJECT_ALIGNMENT)
                    {
                        Console.Error.Write("; Please note: 32bit Oracle/Sun VMs don't allow exact OBJECT_ALIGNMENT retrieval, this is a known issue.");
                    }
                    Console.Error.WriteLine();
                }
            }

            Assert.IsTrue(RamUsageEstimator.NUM_BYTES_OBJECT_REF == 4 || RamUsageEstimator.NUM_BYTES_OBJECT_REF == 8);
            if (!Constants.JRE_IS_64BIT)
            {
                Assert.AreEqual(4, RamUsageEstimator.NUM_BYTES_OBJECT_REF, "For 32bit JVMs, reference size must always be 4?");
            }
        }

        private class Holder
        {
            long field1 = 5000L;
            string name = "name";
            public Holder holder;
            long field2, field3, field4;

            public Holder() { }

            public Holder(string name, long field1)
            {
                this.name = name;
                this.field1 = field1;
            }
        }

        private class HolderSubclass : Holder
        {
            byte foo;
            int bar;
        }

        private class HolderSubclass2 : Holder
        {
            // empty, only inherits all fields -> size should be identical to superclass
        }
    }
}