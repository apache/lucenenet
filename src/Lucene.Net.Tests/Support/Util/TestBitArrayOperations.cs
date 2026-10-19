using Lucene.Net.Attributes;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Numerics;
using Assert = Lucene.Net.TestFramework.Assert;

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
    /// LUCENENET specific: pins the vectorized word-loop helper behind
    /// <see cref="FixedBitSet"/>, <see cref="OpenBitSet"/> and <see cref="Int64BitSet"/>
    /// to the original scalar semantics at every vector-width boundary.
    /// </summary>
    [TestFixture]
    public class TestBitArrayOperations : LuceneTestCase
    {
        private delegate void BulkOp(long[] dst, long[] src, int numWords);

        private static readonly (string name, BulkOp op, Func<long, long, long> scalar)[] Ops =
        {
            ("Or", BitArrayOperations.Or, (a, b) => a | b),
            ("And", BitArrayOperations.And, (a, b) => a & b),
            ("AndNot", BitArrayOperations.AndNot, (a, b) => a & ~b),
            ("Xor", BitArrayOperations.Xor, (a, b) => a ^ b),
        };

        /// <summary>
        /// Word counts straddling the SIMD width so that the all-vector, vector-plus-tail,
        /// tail-only and empty paths are each exercised regardless of hardware.
        /// </summary>
        private static IEnumerable<int> BoundaryWordCounts()
        {
            int count = Vector<long>.Count;
            yield return 0;
            yield return 1;
            yield return count - 1;
            yield return count;
            yield return count + 1;
            yield return 2 * count + 1;
            yield return 1000;
        }

        private long[] RandomWords(int length)
        {
            long[] words = new long[length];
            for (int i = 0; i < length; i++)
            {
                words[i] = Random.NextInt64();
            }
            return words;
        }

        [Test, LuceneNetSpecific]
        public void BulkOps_MatchScalarReference_AtEveryVectorBoundary()
        {
            foreach (int numWords in BoundaryWordCounts())
            {
                foreach (var (name, op, scalar) in Ops)
                {
                    long[] dst = RandomWords(numWords);
                    long[] src = RandomWords(numWords);
                    long[] expected = new long[numWords];
                    for (int i = 0; i < numWords; i++)
                    {
                        expected[i] = scalar(dst[i], src[i]);
                    }

                    op(dst, src, numWords);

                    Assert.AreEqual(expected, dst, "{0} with numWords={1}", name, numWords);
                }
            }
        }

        [Test, LuceneNetSpecific]
        public void BulkOps_LeaveWordsPastNumWordsUntouched()
        {
            foreach (int numWords in BoundaryWordCounts())
            {
                foreach (var (name, op, scalar) in Ops)
                {
                    const int slack = 7;
                    long[] dst = RandomWords(numWords + slack);
                    long[] src = RandomWords(numWords + slack);
                    long[] dstCopy = (long[])dst.Clone();
                    long[] srcCopy = (long[])src.Clone();

                    op(dst, src, numWords);

                    for (int i = 0; i < numWords; i++)
                    {
                        Assert.AreEqual(scalar(dstCopy[i], srcCopy[i]), dst[i], "{0} word {1} of numWords={2}", name, i, numWords);
                    }
                    for (int i = numWords; i < dst.Length; i++)
                    {
                        Assert.AreEqual(dstCopy[i], dst[i], "{0} modified dst[{1}] past numWords={2}", name, i, numWords);
                    }
                    Assert.AreEqual(srcCopy, src, "{0} modified src with numWords={1}", name, numWords);
                }
            }
        }

        [Test, LuceneNetSpecific]
        public void BulkOps_ThrowWhenNumWordsExceedsEitherArray()
        {
            foreach (var (name, op, _) in Ops)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => op(new long[4], new long[8], 5), "{0} dst too short", name);
                Assert.Throws<ArgumentOutOfRangeException>(() => op(new long[8], new long[4], 5), "{0} src too short", name);
            }
        }
    }
}
