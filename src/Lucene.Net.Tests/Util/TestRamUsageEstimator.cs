using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

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
        [Test]
        public virtual void TestSanity()
        {
            Assert.IsTrue(RamUsageEstimator.SizeOf("test string") > RamUsageEstimator.ShallowSizeOfInstance(typeof(string)));

            Holder holder = new Holder();
            holder.holder = new Holder("string2", 5000L);
            Assert.IsTrue(RamUsageEstimator.SizeOf(holder) > RamUsageEstimator.ShallowSizeOfInstance(typeof(Holder)));
            Assert.IsTrue(RamUsageEstimator.SizeOf(holder) > RamUsageEstimator.SizeOf(holder.holder));

            Assert.IsTrue(RamUsageEstimator.ShallowSizeOfInstance(typeof(HolderSubclass)) >= RamUsageEstimator.ShallowSizeOfInstance(typeof(Holder)));
            Assert.IsTrue(RamUsageEstimator.ShallowSizeOfInstance(typeof(Holder)) == RamUsageEstimator.ShallowSizeOfInstance(typeof(HolderSubclass2)));

            string[] strings = new string[] { "test string", "hollow", "catchmaster" };
            Assert.IsTrue(RamUsageEstimator.SizeOf(strings) > RamUsageEstimator.ShallowSizeOf(strings));
        }

        [Test]
        public virtual void TestStaticOverloads()
        {
            Random rnd = Random;
            {
                sbyte[] array = new sbyte[rnd.Next(1024)];
                assertEquals(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((object)array));
            }

            {
                bool[] array = new bool[rnd.Next(1024)];
                assertEquals(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((object)array));
            }

            {
                char[] array = new char[rnd.Next(1024)];
                assertEquals(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((object)array));
            }

            {
                short[] array = new short[rnd.Next(1024)];
                assertEquals(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((object)array));
            }

            {
                int[] array = new int[rnd.Next(1024)];
                assertEquals(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((object)array));
            }

            {
                float[] array = new float[rnd.Next(1024)];
                assertEquals(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((object)array));
            }

            {
                long[] array = new long[rnd.Next(1024)];
                assertEquals(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((object)array));
            }

            {
                double[] array = new double[rnd.Next(1024)];
                assertEquals(RamUsageEstimator.SizeOf(array), RamUsageEstimator.SizeOf((object)array));
            }
        }

        
       [Test]
        public virtual void TestReferenceSize()
        {
            //LUCENENET: The JVM is obviously not relevant to our purposes
            //if (!SupportedJVM)
            //{
            //    Console.Error.WriteLine("WARN: Your JVM does not support certain Oracle/Sun extensions.");
            //    Console.Error.WriteLine(" Memory estimates may be inaccurate.");
            //    Console.Error.WriteLine(" Please report this to the Lucene mailing list.");
            //    Console.Error.WriteLine("JVM version: " + RamUsageEstimator.JVM_INFO_STRING);
            //    Console.Error.WriteLine("UnsupportedFeatures:");
            //    foreach (JvmFeature f in RamUsageEstimator.UnsupportedFeatures)
            //    {
            //        Console.Error.Write(" - " + f.ToString());
            //        if (f == RamUsageEstimator.JvmFeature.OBJECT_ALIGNMENT)
            //        {
            //            Console.Error.Write("; Please note: 32bit Oracle/Sun VMs don't allow exact OBJECT_ALIGNMENT retrieval, this is a known issue.");
            //        }
            //        Console.Error.WriteLine();
            //    }
            //}

            Assert.IsTrue(RamUsageEstimator.NUM_BYTES_OBJECT_REF == 4 || RamUsageEstimator.NUM_BYTES_OBJECT_REF == 8);
            if (!Constants.RUNTIME_IS_64BIT)
            {
                assertEquals("For 32bit JVMs, reference size must always be 4?", 4, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
            }
        }

        private class Holder
        {
#pragma warning disable IDE0052, IDE0044 // Remove unread private members
            private long field1 = 5000L;
            private string name = "name";
#pragma warning restore IDE0052, IDE0044 // Remove unread private members
            internal Holder holder;

            public long Field2 { get; set; }
            public long Field3 { get; set; }
            public long Field4 { get; set; }

            internal Holder()
            {
            }

            internal Holder(string name, long field1)
            {
                this.name = name;
                this.field1 = field1;
            }
        }

        private class HolderSubclass : Holder
        {
            public sbyte Foo { get; set; }
            public int Bar { get; set; }
        }

        private class HolderSubclass2 : Holder
        {
            // empty, only inherits all fields -> size should be identical to superclass
        }
    }

}