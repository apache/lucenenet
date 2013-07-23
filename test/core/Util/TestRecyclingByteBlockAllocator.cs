using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestRecyclingByteBlockAllocator : LuceneTestCase
    {
        private Random random = new Random();

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        private RecyclingByteBlockAllocator newAllocator()
        {
            return new RecyclingByteBlockAllocator(1 << (2 + random.Next(15)),
                random.Next(97), Counter.NewCounter());
        }

        [Test]
        public void testAllocate()
        {
            var allocator = newAllocator();
            var set = new HashSet<sbyte[]>();
            var block = allocator.ByteBlock;
            set.Add(block);
            assertNotNull(block);
            var size = block.Length;

            int num = AtLeast(97);
            for (var i = 0; i < num; i++)
            {
                block = allocator.ByteBlock;
                assertNotNull(block);
                assertEquals(size, block.Length);
                assertTrue("block is returned twice", set.Add(block));
                assertEquals(size * (i + 2), allocator.BytesUsed); // zero based + 1
                assertEquals(0, allocator.NumBufferedBlocks);
            }
        }

        [Test]
        public void TestAllocateAndRecycle()
        {
            var allocator = newAllocator();
            var allocated = new HashSet<sbyte[]>();

            var block = allocator.ByteBlock;
            allocated.Add(block);
            assertNotNull(block);
            var size = block.Length;

            int numIters = AtLeast(97);
            for (var i = 0; i < numIters; i++)
            {
                var num = 1 + random.Next(39);
                for (var j = 0; j < num; j++)
                {
                    block = allocator.ByteBlock;
                    assertNotNull(block);
                    assertEquals(size, block.Length);
                    assertTrue("block is returned twice", allocated.Add(block));
                    assertEquals(size * (allocated.Count + allocator.NumBufferedBlocks), allocator
                        .BytesUsed);
                }
                var array = allocated.ToArray();
                var begin = random.Next(array.Length);
                var end = begin + random.Next(array.Length - begin);
                var selected = new List<sbyte[]>();
                for (var j = begin; j < end; j++)
                {
                    selected.Add(array[j]);
                }
                allocator.RecycleByteBlocks(array, begin, end);
                for (var j = begin; j < end; j++)
                {
                    assertNull(array[j]);
                    var b = selected.Remove(new sbyte[] {0});
                    assertTrue(allocated.Remove(new sbyte[] { b }));
                }
            }
        }

        [Test]
        public void TestAllocateAndFree()
        {
            var allocator = newAllocator();
            var allocated = new HashSet<sbyte[]>();
            var freeButAllocated = 0;
            var block = allocator.ByteBlock;
            allocated.Add(block);
            assertNotNull(block);
            var size = block.Length;

            int numIters = AtLeast(97);
            for (var i = 0; i < numIters; i++)
            {
                var num = 1 + random.Next(39);
                for (var j = 0; j < num; j++)
                {
                    block = allocator.ByteBlock;
                    freeButAllocated = Math.Max(0, freeButAllocated - 1);
                    assertNotNull(block);
                    assertEquals(size, block.Length);
                    assertTrue("block is returned twice", allocated.Add(block));
                    assertEquals(size * (allocated.Count + allocator.NumBufferedBlocks),
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
                allocator.RecycleByteBlocks(array, begin, end);
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
