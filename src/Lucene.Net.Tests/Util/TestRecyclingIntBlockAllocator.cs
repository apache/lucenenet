using J2N.Collections.Generic.Extensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// Testcase for <seealso cref="RecyclingInt32BlockAllocator"/>
    /// </summary>
    [TestFixture]
    public class TestRecyclingIntBlockAllocator : LuceneTestCase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        private RecyclingInt32BlockAllocator NewAllocator()
        {
            return new RecyclingInt32BlockAllocator(1 << (2 + Random.Next(15)), Random.Next(97), Util.Counter.NewCounter());
        }

        [Test]
        public virtual void TestAllocate()
        {
            RecyclingInt32BlockAllocator allocator = NewAllocator();
            ISet<int[]> set = new JCG.HashSet<int[]>();
            int[] block = allocator.GetInt32Block();
            set.Add(block);
            Assert.IsNotNull(block);
            int size = block.Length;

            int num = AtLeast(97);
            for (int i = 0; i < num; i++)
            {
                block = allocator.GetInt32Block();
                Assert.IsNotNull(block);
                Assert.AreEqual(size, block.Length);
                Assert.IsTrue(set.Add(block), "block is returned twice");
                Assert.AreEqual(4 * size * (i + 2), allocator.BytesUsed); // zero based + 1
                Assert.AreEqual(0, allocator.NumBufferedBlocks);
            }
        }

        [Test]
        public virtual void TestAllocateAndRecycle()
        {
            RecyclingInt32BlockAllocator allocator = NewAllocator();
            ISet<int[]> allocated = new JCG.HashSet<int[]>();

            int[] block = allocator.GetInt32Block();
            allocated.Add(block);
            Assert.IsNotNull(block);
            int size = block.Length;

            int numIters = AtLeast(97);
            for (int i = 0; i < numIters; i++)
            {
                int num = 1 + Random.Next(39);
                for (int j = 0; j < num; j++)
                {
                    block = allocator.GetInt32Block();
                    Assert.IsNotNull(block);
                    Assert.AreEqual(size, block.Length);
                    Assert.IsTrue(allocated.Add(block), "block is returned twice");
                    Assert.AreEqual(4 * size * (allocated.Count + allocator.NumBufferedBlocks), allocator.BytesUsed);
                }
                int[][] array = allocated.ToArray(/*new int[0][]*/);
                int begin = Random.Next(array.Length);
                int end = begin + Random.Next(array.Length - begin);
                IList<int[]> selected = new JCG.List<int[]>();
                for (int j = begin; j < end; j++)
                {
                    selected.Add(array[j]);
                }
                allocator.RecycleInt32Blocks(array, begin, end);
                for (int j = begin; j < end; j++)
                {
                    Assert.IsNull(array[j]);
                    int[] b = selected[0];
                    selected.RemoveAt(0);
                    Assert.IsTrue(allocated.Remove(b));
                }
            }
        }

        [Test]
        public virtual void TestAllocateAndFree()
        {
            RecyclingInt32BlockAllocator allocator = NewAllocator();
            ISet<int[]> allocated = new JCG.HashSet<int[]>();
            int freeButAllocated = 0;
            int[] block = allocator.GetInt32Block();
            allocated.Add(block);
            Assert.IsNotNull(block);
            int size = block.Length;

            int numIters = AtLeast(97);
            for (int i = 0; i < numIters; i++)
            {
                int num = 1 + Random.Next(39);
                for (int j = 0; j < num; j++)
                {
                    block = allocator.GetInt32Block();
                    freeButAllocated = Math.Max(0, freeButAllocated - 1);
                    Assert.IsNotNull(block);
                    Assert.AreEqual(size, block.Length);
                    Assert.IsTrue(allocated.Add(block), "block is returned twice");
                    Assert.AreEqual(4 * size * (allocated.Count + allocator.NumBufferedBlocks), allocator.BytesUsed, "" + (4 * size * (allocated.Count + allocator.NumBufferedBlocks) - allocator.BytesUsed));
                }

                int[][] array = allocated.ToArray(/*new int[0][]*/);
                int begin = Random.Next(array.Length);
                int end = begin + Random.Next(array.Length - begin);
                for (int j = begin; j < end; j++)
                {
                    int[] b = array[j];
                    Assert.IsTrue(allocated.Remove(b));
                }
                allocator.RecycleInt32Blocks(array, begin, end);
                for (int j = begin; j < end; j++)
                {
                    Assert.IsNull(array[j]);
                }
                // randomly free blocks
                int numFreeBlocks = allocator.NumBufferedBlocks;
                int freeBlocks = allocator.FreeBlocks(Random.Next(7 + allocator.MaxBufferedBlocks));
                Assert.AreEqual(allocator.NumBufferedBlocks, numFreeBlocks - freeBlocks);
            }
        }
    }
}