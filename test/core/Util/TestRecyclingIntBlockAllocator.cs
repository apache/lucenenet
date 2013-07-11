using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestRecyclingIntBlockAllocator : LuceneTestCase
    {
        private Random random = new Random();

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        private RecyclingIntBlockAllocator NewAllocator()
        {
            return new RecyclingIntBlockAllocator(1 << (2 + random.Next(15)),
                random.Next(97), Counter.NewCounter());
        }

        [Test]
        public void TestAllocate()
        {
            var allocator = NewAllocator();
            var set = new HashSet<int[]>();
            var block = allocator.IntBlock;
            set.Add(block);
            assertNotNull(block);
            var size = block.Length;

            int num = AtLeast(97);
            for (var i = 0; i < num; i++)
            {
                block = allocator.IntBlock;
                assertNotNull(block);
                assertEquals(size, block.Length);
                assertTrue("block is returned twice", set.Add(block));
                assertEquals(4 * size * (i + 2), allocator.BytesUsed); // zero based + 1
                assertEquals(0, allocator.NumBufferedBlocks);
            }
        }

        [Test]
        public void TestAllocateAndRecycle()
        {
            var allocator = NewAllocator();
            var allocated = new HashSet<int[]>();

            var block = allocator.IntBlock;
            allocated.Add(block);
            assertNotNull(block);
            var size = block.Length;

            int numIters = AtLeast(97);
            for (var i = 0; i < numIters; i++)
            {
                var num = 1 + random.Next(39);
                for (var j = 0; j < num; j++)
                {
                    block = allocator.IntBlock;
                    assertNotNull(block);
                    assertEquals(size, block.Length);
                    assertTrue("block is returned twice", allocated.Add(block));
                    assertEquals(4 * size * (allocated.Count + allocator.NumBufferedBlocks), allocator
                        .BytesUsed);
                }
                var array = allocated.ToArray();
                var begin = random.Next(array.Length);
                var end = begin + random.Next(array.Length - begin);
                var selected = new List<int[]>();
                for (var j = begin; j < end; j++)
                {
                    selected.Add(array[j]);
                }
                allocator.RecycleIntBlocks(array, begin, end);
                for (var j = begin; j < end; j++)
                {
                    assertNull(array[j]);
                    int[] b = selected.Remove(0);
                    assertTrue(allocated.Remove(b));
                }
            }
        }

        [Test]
        public void TestAllocateAndFree()
        {
            var allocator = NewAllocator();
            var allocated = new HashSet<int[]>();
            var freeButAllocated = 0;
            var block = allocator.IntBlock;
            allocated.Add(block);
            assertNotNull(block);
            var size = block.Length;

            int numIters = AtLeast(97);
            for (var i = 0; i < numIters; i++)
            {
                var num = 1 + random.Next(39);
                for (var j = 0; j < num; j++)
                {
                    block = allocator.IntBlock;
                    freeButAllocated = Math.Max(0, freeButAllocated - 1);
                    assertNotNull(block);
                    assertEquals(size, block.Length);
                    assertTrue("block is returned twice", allocated.Add(block));
                    assertEquals("" + (4 * size * (allocated.Count + allocator.NumBufferedBlocks) - allocator.BytesUsed),
                        4 * size * (allocated.Count + allocator.NumBufferedBlocks),
                        allocator.BytesUsed);
                }

                var array = allocated.ToArray();
                var begin = random.Next(array.Length);
                var end = begin + random.Next(array.Length - begin);
                for (var j = begin; j < end; j++)
                {
                    var b = array[j];
                    assertTrue(allocated.Remove(b));
                }
                allocator.RecycleIntBlocks(array, begin, end);
                for (var j = begin; j < end; j++)
                {
                    assertNull(array[j]);
                }
                // randomly free blocks
                var numFreeBlocks = allocator.NumBufferedBlocks;
                var freeBlocks = allocator.FreeBlocks(random.Next(7 + allocator
                    .MaxBufferedBlocks));
                assertEquals(allocator.NumBufferedBlocks, numFreeBlocks - freeBlocks);
            }
        }
    }
}
