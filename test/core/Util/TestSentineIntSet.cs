using System;
using System.Collections.Generic;
using Lucene.Net.Test.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestSentineIntSet : LuceneTestCase
    {
        private Random random = new Random();

        [Test]
        public void test()
        {
            var set = new SentinelIntSet(10, -1);
            Assert.IsFalse(set.Exists(50));
            set.Put(50);
            assertTrue(set.Exists(50));
            assertEquals(1, set.Size);
            assertEquals(-11, set.Find(10));
            assertEquals(1, set.Size);
            set.Clear();
            assertEquals(0, set.Size);
            assertEquals(50, set.Hash(50));
            //force a rehash
            for (int i = 0; i < 20; i++)
            {
                set.Put(i);
            }
            assertEquals(20, set.Size);
            assertEquals(24, set.rehashCount);
        }


        [Test]
        public void TestRandom()
        {
            for (var i = 0; i < 10000; i++)
            {
                var initSz = random.Next(20);
                var num = random.Next(30);
                var maxVal = (random.NextBool() ? random.Next(50) : random.Next(int.MaxValue)) + 1;

                var a = new HashSet<int>();
                var b = new SentinelIntSet(initSz, -1);

                for (var j = 0; j < num; j++)
                {
                    var val = random.Next(maxVal);
                    var exists = !a.Add(val);
                    var existsB = b.Exists(val);
                    assertEquals(exists, existsB);
                    var slot = b.Find(val);
                    assertEquals(exists, slot >= 0);
                    b.Put(val);

                    assertEquals(a.Count, b.Size);
                }
            }

        }
    }
}
