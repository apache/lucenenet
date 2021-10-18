using NUnit.Framework;
using RandomizedTesting.Generators;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements. See the NOTICE file distributed with this
     * work for additional information regarding copyright ownership. The ASF
     * licenses this file to You under the Apache License, Version 2.0 (the
     * "License"); you may not use this file except in compliance with the License.
     * You may obtain a copy of the License at
     *
     * http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
     * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
     * License for the specific language governing permissions and limitations under
     * the License.
     */

    [TestFixture]
    public class TestByteBlockPool : LuceneTestCase
    {
        [Test]
        public virtual void TestReadAndWrite()
        {
            Counter bytesUsed = Util.Counter.NewCounter();
            ByteBlockPool pool = new ByteBlockPool(new ByteBlockPool.DirectTrackingAllocator(bytesUsed));
            pool.NextBuffer();
            bool reuseFirst = Random.NextBoolean();
            for (int j = 0; j < 2; j++)
            {
                IList<BytesRef> list = new JCG.List<BytesRef>();
                int maxLength = AtLeast(500);
                int numValues = AtLeast(100);
                BytesRef @ref = new BytesRef();
                for (int i = 0; i < numValues; i++)
                {
                    string value = TestUtil.RandomRealisticUnicodeString(Random, maxLength);
                    list.Add(new BytesRef(value));
                    @ref.CopyChars(value);
                    pool.Append(@ref);
                }
                // verify
                long position = 0;
                foreach (BytesRef expected in list)
                {
                    @ref.Grow(expected.Length);
                    @ref.Length = expected.Length;
                    pool.ReadBytes(position, @ref.Bytes, @ref.Offset, @ref.Length);
                    Assert.AreEqual(expected, @ref);
                    position += @ref.Length;
                }
                pool.Reset(Random.NextBoolean(), reuseFirst);
                if (reuseFirst)
                {
                    Assert.AreEqual(ByteBlockPool.BYTE_BLOCK_SIZE, bytesUsed);
                }
                else
                {
                    Assert.AreEqual(0, bytesUsed);
                    pool.NextBuffer(); // prepare for next iter
                }
            }
        }
    }
}