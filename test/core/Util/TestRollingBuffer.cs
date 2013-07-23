using System;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestRollingBuffer : LuceneTestCase
    {
        private class Position : RollingBuffer.Resettable
        {
            public int pos;

            public void Reset()
            {
                pos = -1;
            }
        }

        private sealed class AnonymousRollingBuffer : RollingBuffer<Position>
        {
            protected override Position NewInstance()
            {
                var pos = new Position { pos = -1 };
                return pos;
            }
        }

        [Test]
        public void Test()
        {
            RollingBuffer<Position> buffer = new AnonymousRollingBuffer();

            for (var iter = 0; iter < 100 * RANDOM_MULTIPLIER; iter++)
            {

                var freeBeforePos = 0;
                int maxPos = AtLeast(10000);
                var posSet = new FixedBitSet(maxPos + 1000);
                var posUpto = 0;
                var random = new Random();
                while (freeBeforePos < maxPos)
                {
                    if (random.Next(4) == 1)
                    {
                        var limit = Rarely() ? 1000 : 20;
                        var inc = random.Next(limit);
                        var pos = freeBeforePos + inc;
                        posUpto = Math.Max(posUpto, pos);
                        if (VERBOSE)
                        {
                            Console.WriteLine("  check pos=" + pos + " posUpto=" + posUpto);
                        }
                        var posData = buffer.Get(pos);
                        if (!posSet.GetAndSet(pos))
                        {
                            assertEquals(-1, posData.pos);
                            posData.pos = pos;
                        }
                        else
                        {
                            assertEquals(pos, posData.pos);
                        }
                    }
                    else
                    {
                        if (posUpto > freeBeforePos)
                        {
                            freeBeforePos += random.Next(posUpto - freeBeforePos);
                        }
                        if (VERBOSE)
                        {
                            Console.WriteLine("  freeBeforePos=" + freeBeforePos);
                        }
                        buffer.FreeBefore(freeBeforePos);
                    }
                }

                buffer.Reset();
            }
        }
    }
}
