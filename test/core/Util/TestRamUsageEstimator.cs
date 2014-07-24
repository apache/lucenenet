using System;
using Lucene.Net.Util;
using NUnit.Framework;

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
    public class TestRamUsageEstimator : LuceneTestCase
    {
        /*[Test]
        public virtual void TestSanity()
        {
            Assert.IsTrue(sizeof(new string("test string")) > ShallowSizeOfInstance(typeof(string)));

            Holder holder = new Holder();
            holder.Holder_Renamed = new Holder("string2", 5000L);
            Assert.IsTrue(sizeof(holder) > shallowSizeOfInstance(typeof(Holder)));
            Assert.IsTrue(sizeof(holder) > sizeof(holder.Holder_Renamed));

            Assert.IsTrue(shallowSizeOfInstance(typeof(HolderSubclass)) >= shallowSizeOfInstance(typeof(Holder)));
            Assert.IsTrue(shallowSizeOfInstance(typeof(Holder)) == shallowSizeOfInstance(typeof(HolderSubclass2)));

            string[] strings = new string[] { new string("test string"), new string("hollow"), new string("catchmaster") };
            Assert.IsTrue(sizeof(strings) > shallowSizeOf(strings));
        }*/

        [Test]
        public virtual void TestStaticOverloads()
        {
            Random rnd = Random();
            {
                sbyte[] array = new sbyte[rnd.Next(1024)];
                Assert.AreEqual(sizeof(array), sizeof((object)array));
            }

            {
                bool[] array = new bool[rnd.Next(1024)];
                Assert.AreEqual(sizeof(array), sizeof((object)array));
            }

            {
                char[] array = new char[rnd.Next(1024)];
                Assert.AreEqual(sizeof(array), sizeof((object)array));
            }

            {
                short[] array = new short[rnd.Next(1024)];
                Assert.AreEqual(sizeof(array), sizeof((object)array));
            }

            {
                int[] array = new int[rnd.Next(1024)];
                Assert.AreEqual(sizeof(array), sizeof((object)array));
            }

            {
                float[] array = new float[rnd.Next(1024)];
                Assert.AreEqual(sizeof(array), sizeof((object)array));
            }

            {
                long[] array = new long[rnd.Next(1024)];
                Assert.AreEqual(sizeof(array), sizeof((object)array));
            }

            {
                double[] array = new double[rnd.Next(1024)];
                Assert.AreEqual(sizeof(array), sizeof((object)array));
            }
        }

        /* LUCENE PORT: The JVM is obviously not relevant to our purposes
        [Test]
        public virtual void TestReferenceSize()
        {
            if (!SupportedJVM)
            {
                Console.Error.WriteLine("WARN: Your JVM does not support certain Oracle/Sun extensions.");
                Console.Error.WriteLine(" Memory estimates may be inaccurate.");
                Console.Error.WriteLine(" Please report this to the Lucene mailing list.");
                Console.Error.WriteLine("JVM version: " + RamUsageEstimator.JVM_INFO_STRING);
                Console.Error.WriteLine("UnsupportedFeatures:");
                foreach (JvmFeature f in RamUsageEstimator.UnsupportedFeatures)
                {
                    Console.Error.Write(" - " + f.ToString());
                    if (f == RamUsageEstimator.JvmFeature.OBJECT_ALIGNMENT)
                    {
                        Console.Error.Write("; Please note: 32bit Oracle/Sun VMs don't allow exact OBJECT_ALIGNMENT retrieval, this is a known issue.");
                    }
                    Console.Error.WriteLine();
                }
            }

            Assert.IsTrue(NUM_BYTES_OBJECT_REF == 4 || NUM_BYTES_OBJECT_REF == 8);
            if (!Constants.JRE_IS_64BIT)
            {
                Assert.AreEqual("For 32bit JVMs, reference size must always be 4?", 4, NUM_BYTES_OBJECT_REF);
            }
        }*/

        private class Holder
        {
            internal long Field1 = 5000L;
            internal string Name = "name";
            internal Holder Holder_Renamed;
            internal long Field2, Field3, Field4;

            internal Holder()
            {
            }

            internal Holder(string name, long field1)
            {
                this.Name = name;
                this.Field1 = field1;
            }
        }

        private class HolderSubclass : Holder
        {
            internal sbyte Foo;
            internal int Bar;
        }

        private class HolderSubclass2 : Holder
        {
            // empty, only inherits all fields -> size should be identical to superclass
        }
    }

}