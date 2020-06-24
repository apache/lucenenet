using Lucene.Net.Support;
using NUnit.Framework;
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

    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    [TestFixture]
    public class TestBufferedChecksum : LuceneTestCase
    {
        [Test]
        public virtual void TestSimple()
        {
            IChecksum c = new BufferedChecksum(new CRC32());
            c.Update(1);
            c.Update(2);
            c.Update(3);
            Assert.AreEqual(1438416925L, c.Value);
        }

        [Test]
        public virtual void TestRandom()
        {
            IChecksum c1 = new CRC32();
            IChecksum c2 = new BufferedChecksum(new CRC32());
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
                        c2.Update(b);
                        break;

                    case 2:
                        // reset()
                        c1.Reset();
                        c2.Reset();
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