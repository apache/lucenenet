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


namespace Lucene.Net.Util
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    public class TestRamUsageEstimator : LuceneTestCase
    {

        [Test]
        public void TestSanity()
        {
            Func<object, long> sizeOf = RamUsageTester.SizeOf;
         

            var size = RamUsageEstimator.SizeOf("test string".ToCharArray());
            var shallowSize = RamUsageEstimator.ShallowSizeOfInstance(typeof(string));

            Ok(size > shallowSize, "the size {0} must be greater than the shallow size {1}", size, shallowSize);

            var holder = new Holder { holder = new Holder("string2", 5000L) };

            var left = sizeOf(holder);
                var right = RamUsageEstimator.ShallowSizeOfInstance(holder.GetType());
            Ok(left > right, "sizeOf(holder) {0} must be greater than the shallow size {1}", left, right);
            Ok(sizeOf(holder) > sizeOf(holder.holder));

            Ok(RamUsageEstimator.ShallowSizeOfInstance(typeof(HolderSubclass)) >= RamUsageEstimator.ShallowSizeOfInstance(typeof(Holder)));

            var holderShallow = RamUsageEstimator.ShallowSizeOfInstance(typeof(Holder));
            var holder2Shallow = RamUsageEstimator.ShallowSizeOfInstance(typeof(HolderSubclass2));
            Ok(holderShallow ==  holder2Shallow, "Holder {0} should be equal to HolderSubclass2 {1}", holderShallow, holder2Shallow);

            var strings = new string[] {
                    "test string",
                    "hollow",
                    "catchmaster"
                };

            Ok(sizeOf((object)strings) > RamUsageEstimator.ShallowSizeOf(strings));
        }

        [Test]
        public void TestStaticOverloads()
        {
            Func<object, long> sizeOf = RamUsageTester.SizeOf;

            var rnd = new Random();
            {
                var array = new byte[rnd.Next(1024)];
                Equal(sizeOf(array), sizeOf((object)array));
            }

            {
                var array = new bool[rnd.Next(1024)];
                Equal(sizeOf(array), sizeOf((Object)array));
            }

            {
                var array = new char[rnd.Next(1024)];
                Equal(sizeOf(array), sizeOf((Object)array));
            }

            {
                var array = new short[rnd.Next(1024)];
                Equal(sizeOf(array), sizeOf((Object)array));
            }

            {
                var array = new int[rnd.Next(1024)];
                Equal(sizeOf(array), sizeOf((Object)array));
            }

            {
                var array = new float[rnd.Next(1024)];
                Equal(sizeOf(array), sizeOf((Object)array));
            }

            {
                var array = new long[rnd.Next(1024)];
                Equal(sizeOf(array), sizeOf((Object)array));
            }

            {
                var array = new double[rnd.Next(1024)];
                Equal(sizeOf(array), sizeOf((Object)array));
            }
        }

        [Test]
        public void TestReferenceSize()
        {
            /* 
                .NET really doesn't need to support this.  It should be the same on Mono as it
                is on .NET.

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
            } */

            Ok(RamUsageEstimator.NUM_BYTES_OBJECT_REF == 4 || RamUsageEstimator.NUM_BYTES_OBJECT_REF == 8);
            if (!Constants.KRE_IS_64BIT)
            {
                Ok(4 == RamUsageEstimator.NUM_BYTES_OBJECT_REF, "For 32bit JVMs, reference size must always be 4?");
            }
        }

#pragma warning disable 0169,0649
        private class Holder
        {
            public long field1 = 5000L;
            public string name = "name";
            public Holder holder;
            public long field2, field3, field4;

            public Holder() { }

            public Holder(string name, long field1)
            {
                this.name = name;
                this.field1 = field1;
            }
        }

        private class HolderSubclass : Holder
        {
            public byte foo;
            public int bar;
        }

        private class HolderSubclass2 : Holder
        {
            // empty, only inherits all fields -> size should be identical to superclass
        }
#pragma warning restore 0169,0649
    }
}