using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using BitSet = J2N.Collections.BitSet;
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

    using MaxBytesLengthExceededException = Lucene.Net.Util.BytesRefHash.MaxBytesLengthExceededException;

    [TestFixture]
    public class TestBytesRefHash : LuceneTestCase
    {
        internal BytesRefHash hash;
        internal ByteBlockPool pool;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            pool = NewPool();
            hash = NewHash(pool);
        }

        private ByteBlockPool NewPool()
        {
            return Random.NextBoolean() && pool != null ? pool : new ByteBlockPool(new RecyclingByteBlockAllocator(ByteBlockPool.BYTE_BLOCK_SIZE, Random.Next(25)));
        }

        private BytesRefHash NewHash(ByteBlockPool blockPool)
        {
            int initSize = 2 << 1 + Random.Next(5);
            return Random.NextBoolean() ? new BytesRefHash(blockPool) : new BytesRefHash(blockPool, initSize, new BytesRefHash.DirectBytesStartArray(initSize));
        }

        /// <summary>
        /// Test method for <seealso cref="Lucene.Net.Util.BytesRefHash#size()"/>.
        /// </summary>
        [Test]
        public virtual void TestSize()
        {
            BytesRef @ref = new BytesRef();
            int num = AtLeast(2);
            for (int j = 0; j < num; j++)
            {
                int mod = 1 + Random.Next(39);
                for (int i = 0; i < 797; i++)
                {
                    string str;
                    do
                    {
                        str = TestUtil.RandomRealisticUnicodeString(Random, 1000);
                    } while (str.Length == 0);
                    @ref.CopyChars(str);
                    int count = hash.Count;
                    int key = hash.Add(@ref);
                    if (key < 0)
                    {
                        Assert.AreEqual(hash.Count, count);
                    }
                    else
                    {
                        Assert.AreEqual(hash.Count, count + 1);
                    }
                    if (i % mod == 0)
                    {
                        hash.Clear();
                        Assert.AreEqual(0, hash.Count);
                        hash.Reinit();
                    }
                }
            }
        }

        /// <summary>
        /// Test method for
        /// <seealso cref="Lucene.Net.Util.BytesRefHash#get(int, BytesRef)"/>
        /// .
        /// </summary>
        [Test]
        public virtual void TestGet()
        {
            BytesRef @ref = new BytesRef();
            BytesRef scratch = new BytesRef();
            int num = AtLeast(2);
            for (int j = 0; j < num; j++)
            {
                IDictionary<string, int> strings = new Dictionary<string, int>();
                int uniqueCount = 0;
                for (int i = 0; i < 797; i++)
                {
                    string str;
                    do
                    {
                        str = TestUtil.RandomRealisticUnicodeString(Random, 1000);
                    } while (str.Length == 0);
                    @ref.CopyChars(str);
                    int count = hash.Count;
                    int key = hash.Add(@ref);
                    if (key >= 0)
                    {
                        Assert.IsFalse(strings.ContainsKey(str));
                        strings[str] = Convert.ToInt32(key);
                        Assert.AreEqual(uniqueCount, key);
                        uniqueCount++;
                        Assert.AreEqual(hash.Count, count + 1);
                    }
                    else
                    {
                        Assert.IsTrue((-key) - 1 < count);
                        Assert.AreEqual(hash.Count, count);
                    }
                }
                foreach (KeyValuePair<string, int> entry in strings)
                {
                    @ref.CopyChars(entry.Key);
                    Assert.AreEqual(@ref, hash.Get((int)entry.Value, scratch));
                }
                hash.Clear();
                Assert.AreEqual(0, hash.Count);
                hash.Reinit();
            }
        }

        /// <summary>
        /// Test method for <seealso cref="Lucene.Net.Util.BytesRefHash.Compact()"/>.
        /// </summary>
        [Test]
        public virtual void TestCompact()
        {
            BytesRef @ref = new BytesRef();
            int num = AtLeast(2);
            for (int j = 0; j < num; j++)
            {
                int numEntries = 0;
                const int size = 797;
                BitSet bits = new BitSet(size);
                for (int i = 0; i < size; i++)
                {
                    string str;
                    do
                    {
                        str = TestUtil.RandomRealisticUnicodeString(Random, 1000);
                    } while (str.Length == 0);
                    @ref.CopyChars(str);
                    int key = hash.Add(@ref);
                    if (key < 0)
                    {
                        Assert.IsTrue(bits.Get((-key) - 1));
                    }
                    else
                    {
                        Assert.IsFalse(bits.Get(key));
                        bits.Set(key);
                        numEntries++;
                    }
                }
                Assert.AreEqual(hash.Count, bits.Cardinality);
                Assert.AreEqual(numEntries, bits.Cardinality);
                Assert.AreEqual(numEntries, hash.Count);
                int[] compact = hash.Compact();
                Assert.IsTrue(numEntries < compact.Length);
                for (int i = 0; i < numEntries; i++)
                {
                    bits.Clear(compact[i]);
                }
                Assert.AreEqual(0, bits.Cardinality);
                hash.Clear();
                Assert.AreEqual(0, hash.Count);
                hash.Reinit();
            }
        }

        /// <summary>
        /// Test method for
        /// <seealso cref="Lucene.Net.Util.BytesRefHash#sort(java.util.Comparer)"/>.
        /// </summary>
        [Test]
        public virtual void TestSort()
        {
            BytesRef @ref = new BytesRef();
            int num = AtLeast(2);
            for (int j = 0; j < num; j++)
            {
                // LUCENENET specific - to ensure sorting strings works the same in the SortedSet,
                // we need to use StringComparer.Ordinal, which compares strings the same
                // way they are done in Java.
                JCG.SortedSet<string> strings = new JCG.SortedSet<string>(StringComparer.Ordinal);
                for (int k = 0; k < 797; k++)
                {
                    string str;
                    do
                    {
                        str = TestUtil.RandomRealisticUnicodeString(Random, 1000);
                    } while (str.Length == 0);
                    @ref.CopyChars(str);
                    hash.Add(@ref);
                    strings.Add(str);
                }
                // We use the UTF-16 comparer here, because we need to be able to
                // compare to native String.CompareTo() [UTF-16]:
#pragma warning disable 612, 618
                int[] sort = hash.Sort(BytesRef.UTF8SortedAsUTF16Comparer);
#pragma warning restore 612, 618
                Assert.IsTrue(strings.Count < sort.Length);
                int i = 0;
                BytesRef scratch = new BytesRef();
                foreach (string @string in strings)
                {
                    @ref.CopyChars(@string);
                    Assert.AreEqual(@ref, hash.Get(sort[i++], scratch));
                }
                hash.Clear();
                Assert.AreEqual(0, hash.Count);
                hash.Reinit();
            }
        }

        /// <summary>
        /// Test method for
        /// <seealso cref="Lucene.Net.Util.BytesRefHash#add(Lucene.Net.Util.BytesRef)"/>
        /// .
        /// </summary>
        [Test]
        public virtual void TestAdd()
        {
            BytesRef @ref = new BytesRef();
            BytesRef scratch = new BytesRef();
            int num = AtLeast(2);
            for (int j = 0; j < num; j++)
            {
                ISet<string> strings = new JCG.HashSet<string>();
                int uniqueCount = 0;
                for (int i = 0; i < 797; i++)
                {
                    string str;
                    do
                    {
                        str = TestUtil.RandomRealisticUnicodeString(Random, 1000);
                    } while (str.Length == 0);
                    @ref.CopyChars(str);
                    int count = hash.Count;
                    int key = hash.Add(@ref);

                    if (key >= 0)
                    {
                        Assert.IsTrue(strings.Add(str));
                        Assert.AreEqual(uniqueCount, key);
                        Assert.AreEqual(hash.Count, count + 1);
                        uniqueCount++;
                    }
                    else
                    {
                        Assert.IsFalse(strings.Add(str));
                        Assert.IsTrue((-key) - 1 < count);
                        Assert.AreEqual(str, hash.Get((-key) - 1, scratch).Utf8ToString());
                        Assert.AreEqual(count, hash.Count);
                    }
                }

                AssertAllIn(strings, hash);
                hash.Clear();
                Assert.AreEqual(0, hash.Count);
                hash.Reinit();
            }
        }

        [Test]
        public virtual void TestFind()
        {
            BytesRef @ref = new BytesRef();
            BytesRef scratch = new BytesRef();
            int num = AtLeast(2);
            for (int j = 0; j < num; j++)
            {
                ISet<string> strings = new JCG.HashSet<string>();
                int uniqueCount = 0;
                for (int i = 0; i < 797; i++)
                {
                    string str;
                    do
                    {
                        str = TestUtil.RandomRealisticUnicodeString(Random, 1000);
                    } while (str.Length == 0);
                    @ref.CopyChars(str);
                    int count = hash.Count;
                    int key = hash.Find(@ref); //hash.Add(ref);
                    if (key >= 0) // string found in hash
                    {
                        Assert.IsFalse(strings.Add(str));
                        Assert.IsTrue(key < count);
                        Assert.AreEqual(str, hash.Get(key, scratch).Utf8ToString());
                        Assert.AreEqual(count, hash.Count);
                    }
                    else
                    {
                        key = hash.Add(@ref);
                        Assert.IsTrue(strings.Add(str));
                        Assert.AreEqual(uniqueCount, key);
                        Assert.AreEqual(hash.Count, count + 1);
                        uniqueCount++;
                    }
                }

                AssertAllIn(strings, hash);
                hash.Clear();
                Assert.AreEqual(0, hash.Count);
                hash.Reinit();
            }
        }

        [Test]
        public virtual void TestLargeValue()
        {
            int[] sizes = { Random.Next(5), ByteBlockPool.BYTE_BLOCK_SIZE - 33 + Random.Next(31), ByteBlockPool.BYTE_BLOCK_SIZE - 1 + Random.Next(37) };
            BytesRef @ref = new BytesRef();

            var exceptionThrown = false;

            for (int i = 0; i < sizes.Length; i++)
            {
                @ref.Bytes = new byte[sizes[i]];
                @ref.Offset = 0;
                @ref.Length = sizes[i];
                try
                {
                    Assert.AreEqual(i, hash.Add(@ref));
                }
#pragma warning disable 168
                catch (MaxBytesLengthExceededException e)
#pragma warning restore 168
                {
                    exceptionThrown = true;
                    if (i < sizes.Length - 1)
                    {
                        Assert.Fail("unexpected exception at size: " + sizes[i]);
                    }
                }
            }

            Assert.True(exceptionThrown, "Expected that MaxBytesLengthExceededException would be thrown at least once.");
        }

        /// <summary>
        /// Test method for
        /// <seealso cref="Lucene.Net.Util.BytesRefHash#addByPoolOffset(int)"/>
        /// .
        /// </summary>
        [Test]
        public virtual void TestAddByPoolOffset()
        {
            BytesRef @ref = new BytesRef();
            BytesRef scratch = new BytesRef();
            BytesRefHash offsetHash = NewHash(pool);
            int num = AtLeast(2);
            for (int j = 0; j < num; j++)
            {
                ISet<string> strings = new JCG.HashSet<string>();
                int uniqueCount = 0;
                for (int i = 0; i < 797; i++)
                {
                    string str;
                    do
                    {
                        str = TestUtil.RandomRealisticUnicodeString(Random, 1000);
                    } while (str.Length == 0);
                    @ref.CopyChars(str);
                    int count = hash.Count;
                    int key = hash.Add(@ref);

                    if (key >= 0)
                    {
                        Assert.IsTrue(strings.Add(str));
                        Assert.AreEqual(uniqueCount, key);
                        Assert.AreEqual(hash.Count, count + 1);
                        int offsetKey = offsetHash.AddByPoolOffset(hash.ByteStart(key));
                        Assert.AreEqual(uniqueCount, offsetKey);
                        Assert.AreEqual(offsetHash.Count, count + 1);
                        uniqueCount++;
                    }
                    else
                    {
                        Assert.IsFalse(strings.Add(str));
                        Assert.IsTrue((-key) - 1 < count);
                        Assert.AreEqual(str, hash.Get((-key) - 1, scratch).Utf8ToString());
                        Assert.AreEqual(count, hash.Count);
                        int offsetKey = offsetHash.AddByPoolOffset(hash.ByteStart((-key) - 1));
                        Assert.IsTrue((-offsetKey) - 1 < count);
                        Assert.AreEqual(str, hash.Get((-offsetKey) - 1, scratch).Utf8ToString());
                        Assert.AreEqual(count, hash.Count);
                    }
                }

                AssertAllIn(strings, hash);
                foreach (string @string in strings)
                {
                    @ref.CopyChars(@string);
                    int key = hash.Add(@ref);
                    BytesRef bytesRef = offsetHash.Get((-key) - 1, scratch);
                    Assert.AreEqual(@ref, bytesRef);
                }

                hash.Clear();
                Assert.AreEqual(0, hash.Count);
                offsetHash.Clear();
                Assert.AreEqual(0, offsetHash.Count);
                hash.Reinit(); // init for the next round
                offsetHash.Reinit();
            }
        }

        private void AssertAllIn(ISet<string> strings, BytesRefHash hash)
        {
            BytesRef @ref = new BytesRef();
            BytesRef scratch = new BytesRef();
            int count = hash.Count;
            foreach (string @string in strings)
            {
                @ref.CopyChars(@string);
                int key = hash.Add(@ref); // add again to check duplicates
                Assert.AreEqual(@string, hash.Get((-key) - 1, scratch).Utf8ToString());
                Assert.AreEqual(count, hash.Count);
                Assert.IsTrue(key < count, "key: " + key + " count: " + count + " string: " + @string);
            }
        }
    }
}