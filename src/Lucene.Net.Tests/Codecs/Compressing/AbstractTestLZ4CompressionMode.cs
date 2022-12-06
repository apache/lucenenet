using J2N.Numerics;
using J2N.Text;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;
using RandomInts = RandomizedTesting.Generators.RandomNumbers;

namespace Lucene.Net.Codecs.Compressing
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
    public abstract class AbstractTestLZ4CompressionMode : AbstractTestCompressionMode
    {
        public override byte[] Test(byte[] decompressed)
        {
            var compressed = base.Test(decompressed);
            int off = 0;
            int decompressedOff = 0;
            for (; ; )
            {
                int token = compressed[off++] & 0xFF;
                int literalLen = token.TripleShift(4);
                if (literalLen == 0x0F)
                {
                    while (compressed[off] == 0xFF)
                    {
                        literalLen += 0xFF;
                        ++off;
                    }
                    literalLen += compressed[off++] & 0xFF;
                }
                // skip literals
                off += literalLen;
                decompressedOff += literalLen;

                // check that the stream ends with literals and that there are at least
                // 5 of them
                if (off == compressed.Length)
                {
                    Assert.AreEqual(decompressed.Length, decompressedOff);
                    Assert.IsTrue(literalLen >= LZ4.LAST_LITERALS || literalLen == decompressed.Length, "lastLiterals=" + literalLen + ", bytes=" + decompressed.Length);
                    break;
                }

                int matchDec = (compressed[off++] & 0xFF) | ((compressed[off++] & 0xFF) << 8);
                // check that match dec is not 0
                Assert.IsTrue(matchDec > 0 && matchDec <= decompressedOff, matchDec + " " + decompressedOff);

                int matchLen = token & 0x0F;
                if (matchLen == 0x0F)
                {
                    while (compressed[off] == 0xFF)
                    {
                        matchLen += 0xFF;
                        ++off;
                    }
                    matchLen += compressed[off++] & 0xFF;
                }
                matchLen += LZ4.MIN_MATCH;

                // if the match ends prematurely, the next sequence should not have
                // literals or this means we are wasting space
                if (decompressedOff + matchLen < decompressed.Length - LZ4.LAST_LITERALS)
                {
                    bool moreCommonBytes = decompressed[decompressedOff + matchLen] == decompressed[decompressedOff - matchDec + matchLen];
                    bool nextSequenceHasLiterals = (compressed[off] & 0xFF).TripleShift(4) != 0;
                    Assert.IsTrue(!moreCommonBytes || !nextSequenceHasLiterals);
                }

                decompressedOff += matchLen;
            }
            Assert.AreEqual(decompressed.Length, decompressedOff);
            return compressed;
        }

        [Test]
        public virtual void TestShortLiteralsAndMatchs()
        {
            // literals and matchs lengths <= 15
            var decompressed = "1234562345673456745678910123".GetBytes(Encoding.UTF8);
            Test(decompressed);
        }

        [Test]
        public virtual void TestLongMatchs()
        {
            // match length >= 20
            var decompressed = new byte[RandomInts.RandomInt32Between(Random, 300, 1024)];
            for (int i = 0; i < decompressed.Length; ++i)
            {
                decompressed[i] = (byte)i;
            }
            Test(decompressed);
        }

        [Test]
        public virtual void TestLongLiterals()
        {
            // long literals (length >= 16) which are not the last literals
            var decompressed = RandomArray(RandomInts.RandomInt32Between(Random, 400, 1024), 256);
            int matchRef = Random.Next(30);
            int matchOff = RandomInts.RandomInt32Between(Random, decompressed.Length - 40, decompressed.Length - 20);
            int matchLength = RandomInts.RandomInt32Between(Random, 4, 10);
            Arrays.Copy(decompressed, matchRef, decompressed, matchOff, matchLength);
            Test(decompressed);
        }

        [Test]
        public virtual void TestMatchRightBeforeLastLiterals()
        {
            Test(new byte[] { 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4, 5 });
        }
    }
}