using NUnit.Framework;
using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Test huge RAMFile with more than Integer.MAX_VALUE bytes. </summary>
    [TestFixture]
    public class TestHugeRamFile : LuceneTestCase
    {
        private static readonly long MAX_VALUE = (long)2 * (long)int.MaxValue;

        /// <summary>
        /// Fake a huge ram file by using the same byte buffer for all
        /// buffers under maxint.
        /// </summary>
        private class DenseRAMFile : RAMFile
        {
            private long capacity = 0;
            private readonly Dictionary<int, byte[]> singleBuffers = new Dictionary<int, byte[]>();

            protected override byte[] NewBuffer(int size)
            {
                capacity += size;
                if (capacity <= MAX_VALUE)
                {
                    // below maxint we reuse buffers
                    singleBuffers.TryGetValue(Convert.ToInt32(size), out byte[] buf);
                    if (buf is null)
                    {
                        buf = new byte[size];
                        //System.out.println("allocate: "+size);
                        singleBuffers[Convert.ToInt32(size)] = buf;
                    }
                    return buf;
                }
                //System.out.println("allocate: "+size); System.out.Flush();
                return new byte[size];
            }
        }

        /// <summary>
        /// Test huge RAMFile with more than Integer.MAX_VALUE bytes. (LUCENE-957) </summary>
        [Test]
        [Slow]
        public virtual void TestHugeFile()
        {
            var f = new DenseRAMFile();
            // output part
            var @out = new RAMOutputStream(f);
            var b1 = new byte[RAMOutputStream.BUFFER_SIZE];
            var b2 = new byte[RAMOutputStream.BUFFER_SIZE / 3];
            for (int i = 0; i < b1.Length; i++)
            {
                b1[i] = (byte)(i & 0x0007F);
            }
            for (int i = 0; i < b2.Length; i++)
            {
                b2[i] = (byte)(i & 0x0003F);
            }
            long n = 0;
            Assert.AreEqual(n, @out.Length, "output length must match");
            while (n <= MAX_VALUE - b1.Length)
            {
                @out.WriteBytes(b1, 0, b1.Length);
                @out.Flush();
                n += b1.Length;
                Assert.AreEqual(n, @out.Length, "output length must match");
            }
            //System.out.println("after writing b1's, length = "+out.Length()+" (MAX_VALUE="+MAX_VALUE+")");
            int m = b2.Length;
            long L = 12;
            for (int j = 0; j < L; j++)
            {
                for (int i = 0; i < b2.Length; i++)
                {
                    b2[i]++;
                }
                @out.WriteBytes(b2, 0, m);
                @out.Flush();
                n += m;
                Assert.AreEqual(n, @out.Length, "output length must match");
            }
            @out.Dispose();
            // input part
            var @in = new RAMInputStream("testcase", f);
            Assert.AreEqual(n, @in.Length, "input length must match");
            //System.out.println("input length = "+in.Length()+" % 1024 = "+in.Length()%1024);
            for (int j = 0; j < L; j++)
            {
                long loc = n - (L - j) * m;
                @in.Seek(loc / 3);
                @in.Seek(loc);
                for (int i = 0; i < m; i++)
                {
                    var bt = (sbyte)@in.ReadByte();
                    var expected = (sbyte)(1 + j + (i & 0x0003F));
                    Assert.AreEqual(expected, bt, "must read same value that was written! j=" + j + " i=" + i);
                }
            }
        }
    }
}