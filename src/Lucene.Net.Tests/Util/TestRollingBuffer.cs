using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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

    [TestFixture]
    public class TestRollingBuffer : LuceneTestCase
    {
        private class Position : IResettable
        {
            public int Pos { get; set; }

            public void Reset()
            {
                Pos = -1;
            }
        }

        [Test]
        public virtual void Test()
        {
            RollingBuffer<Position> buffer = new RollingBufferAnonymousClass();

            for (int iter = 0; iter < 100 * RandomMultiplier; iter++)
            {
                int freeBeforePos = 0;
                int maxPos = AtLeast(10000);
                FixedBitSet posSet = new FixedBitSet(maxPos + 1000);
                int posUpto = 0;
                Random random = Random;
                while (freeBeforePos < maxPos)
                {
                    if (random.Next(4) == 1)
                    {
                        int limit = Rarely() ? 1000 : 20;
                        int inc = random.Next(limit);
                        int pos = freeBeforePos + inc;
                        posUpto = Math.Max(posUpto, pos);
                        if (Verbose)
                        {
                            Console.WriteLine("  check pos=" + pos + " posUpto=" + posUpto);
                        }
                        Position posData = buffer.Get(pos);
                        if (!posSet.GetAndSet(pos))
                        {
                            Assert.AreEqual(-1, posData.Pos);
                            posData.Pos = pos;
                        }
                        else
                        {
                            Assert.AreEqual(pos, posData.Pos);
                        }
                    }
                    else
                    {
                        if (posUpto > freeBeforePos)
                        {
                            freeBeforePos += random.Next(posUpto - freeBeforePos);
                        }
                        if (Verbose)
                        {
                            Console.WriteLine("  freeBeforePos=" + freeBeforePos);
                        }
                        buffer.FreeBefore(freeBeforePos);
                    }
                }

                buffer.Reset();
            }
        }

        private sealed class RollingBufferAnonymousClass : RollingBuffer<Position>
        {
            // LUCENENET specific - removed NewPosition override and using factory instead
            public RollingBufferAnonymousClass()
                : base(PositionFactory.Default)
            {
            }

            private class PositionFactory : IRollingBufferItemFactory<Position>
            {
                public static readonly PositionFactory Default = new PositionFactory();

                public Position Create(object rollingBuffer)
                {
                    Position pos = new Position();
                    pos.Pos = -1;
                    return pos;
                }
            }
        }
    }
}