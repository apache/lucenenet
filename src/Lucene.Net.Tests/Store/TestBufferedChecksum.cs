using Lucene.Net.Attributes;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Store
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

    using LuceneTestCase = Util.LuceneTestCase;

    [TestFixture]
    public class TestBufferedChecksum : LuceneTestCase
    {
        [Test]
        public virtual void TestSimple()
        {
            var c = new BufferedCrc32Algorithm();
            c.Update(1);
            c.Update(2);
            c.Update(3);
            Assert.AreEqual(1438416925L, c.Value);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestTransformBlock()
        {
            var c1 = new CRC32();
            var c2 = new BufferedCrc32Algorithm();

            var _ = new byte[5];
            var data = new byte[15];

            Random.NextBytes(data);

            c1.Update(data);

            c2.TransformBlock(data, 0, 5, _, 0);
            c2.TransformBlock(data, 5, 5, _, 0);
            c2.TransformFinalBlock(data, 10, 5);

            Assert.AreEqual(c1.Value, c2.Value);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestInitiateResetStateCorrectly()
        {
            var data = new byte[Random.Next(1024)];
            Random.NextBytes(data);

            var c1 = new CRC32();
            var c2 = new BufferedCrc32Algorithm();


            c1.Update(data);
            c2.TransformFinalBlock(data, 0, data.Length);
            Assert.AreEqual(c1.Value, c2.Value);

            c1.Reset();
            c2.Initialize();
            Assert.AreEqual(c1.Value, c2.Value);

            Random.NextBytes(data);
            c1.Update(data);
            c2.Update(data);
            Assert.AreEqual(c1.Value, c2.Value);
        }

        [Test]
        public virtual void TestRandom()
        {
            var c1 = new CRC32();
            var c2 = new BufferedCrc32Algorithm();
            int iterations = AtLeast(10000);
            for (int i = 0; i < iterations; i++)
            {
                switch (Random.Next(4))
                {
                    case 0:
                        // update(byte[], int, int)

                        int length = Random.Next(1024);
                        byte[] bytes = new byte[length];
                        Random.NextBytes(bytes);
                        c1.Update(bytes, 0, bytes.Length);
                        c2.Update(bytes, 0, bytes.Length);
                        break;

                    case 1:
                        // update(int)

                        int b = Random.Next(256);
                        c1.Update(b);

                        var bArray = new byte[] { (byte)b };
                        c2.Update(bArray,0,bArray.Length);
                        break;

                    case 2:
                        // reset()

                        c1.Reset();
                        c2.Initialize();
                        break;

                    case 3:
                        // getValue()

                        Assert.AreEqual(c1.Value, c2.Value);
                        break;
                }
            }

            Assert.AreEqual(c1.Value, c2.Value);
        }
    }
}