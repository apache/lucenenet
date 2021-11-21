using J2N.IO;
using J2N.Numerics;
using Lucene.Net.Support;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Globalization;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using RandomInts = RandomizedTesting.Generators.RandomNumbers;

namespace Lucene.Net.Util.Packed
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

    using Assert = Lucene.Net.TestFramework.Assert;
    using ByteArrayDataInput = Lucene.Net.Store.ByteArrayDataInput;
    using CodecUtil = Lucene.Net.Codecs.CodecUtil;
    using DataInput = Lucene.Net.Store.DataInput;
    using Directory = Lucene.Net.Store.Directory;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using Reader = Lucene.Net.Util.Packed.PackedInt32s.Reader;

    [TestFixture]
    public class TestPackedInts : LuceneTestCase
    {
        [Test]
        public virtual void TestByteCount()
        {
            int iters = AtLeast(3);
            for (int i = 0; i < iters; ++i)
            {
                int valueCount = RandomInts.RandomInt32Between(Random, 1, int.MaxValue);
                foreach (PackedInt32s.Format format in PackedInt32s.Format.Values)
                {
                    for (int bpv = 1; bpv <= 64; ++bpv)
                    {
                        long byteCount = format.ByteCount(PackedInt32s.VERSION_CURRENT, valueCount, bpv);
                        string msg = "format=" + format + ", byteCount=" + byteCount + ", valueCount=" + valueCount + ", bpv=" + bpv;
                        Assert.IsTrue(byteCount * 8 >= (long)valueCount * bpv, msg);
                        if (format == PackedInt32s.Format.PACKED)
                        {
                            Assert.IsTrue((byteCount - 1) * 8 < (long)valueCount * bpv, msg);
                        }
                    }
                }
            }
        }

        [Test]
        public virtual void TestBitsRequired()
        {
            Assert.AreEqual(61, PackedInt32s.BitsRequired((long)Math.Pow(2, 61) - 1));
            Assert.AreEqual(61, PackedInt32s.BitsRequired(0x1FFFFFFFFFFFFFFFL));
            Assert.AreEqual(62, PackedInt32s.BitsRequired(0x3FFFFFFFFFFFFFFFL));
            Assert.AreEqual(63, PackedInt32s.BitsRequired(0x7FFFFFFFFFFFFFFFL));
        }

        [Test]
        public virtual void TestMaxValues()
        {
            Assert.AreEqual(1, PackedInt32s.MaxValue(1), "1 bit -> max == 1");
            Assert.AreEqual(3, PackedInt32s.MaxValue(2), "2 bit -> max == 3");
            Assert.AreEqual(255, PackedInt32s.MaxValue(8), "8 bit -> max == 255");
            Assert.AreEqual(long.MaxValue, PackedInt32s.MaxValue(63), "63 bit -> max == Long.MAX_VALUE");
            Assert.AreEqual(long.MaxValue, PackedInt32s.MaxValue(64), "64 bit -> max == Long.MAX_VALUE (same as for 63 bit)");
        }

        [Test]
        public virtual void TestPackedInts_Mem()
        {
            int num = AtLeast(3);
            for (int iter = 0; iter < num; iter++)
            {
                for (int nbits = 1; nbits <= 64; nbits++)
                {
                    long maxValue = PackedInt32s.MaxValue(nbits);
                    int valueCount = TestUtil.NextInt32(Random, 1, 600);
                    int bufferSize = Random.NextBoolean() ? TestUtil.NextInt32(Random, 0, 48) : TestUtil.NextInt32(Random, 0, 4096);
                    Directory d = NewDirectory();

                    IndexOutput @out = d.CreateOutput("out.bin", NewIOContext(Random));
                    float acceptableOverhead;
                    if (iter == 0)
                    {
                        // have the first iteration go through exact nbits
                        acceptableOverhead = 0.0f;
                    }
                    else
                    {
                        acceptableOverhead = Random.NextSingle();
                    }
                    PackedInt32s.Writer w = PackedInt32s.GetWriter(@out, valueCount, nbits, acceptableOverhead);
                    long startFp = @out.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

                    int actualValueCount = Random.NextBoolean() ? valueCount : TestUtil.NextInt32(Random, 0, valueCount);
                    long[] values = new long[valueCount];
                    for (int i = 0; i < actualValueCount; i++)
                    {
                        if (nbits == 64)
                        {
                            values[i] = Random.NextInt64();
                        }
                        else
                        {
                            values[i] = TestUtil.NextInt64(Random, 0, maxValue);
                        }
                        w.Add(values[i]);
                    }
                    w.Finish();
                    long fp = @out.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    @out.Dispose();

                    // ensure that finish() added the (valueCount-actualValueCount) missing values
                    long bytes = w.Format.ByteCount(PackedInt32s.VERSION_CURRENT, valueCount, w.BitsPerValue);
                    Assert.AreEqual(bytes, fp - startFp);

                    { // test header
                        IndexInput @in = d.OpenInput("out.bin", NewIOContext(Random));
                        // header = codec header | bitsPerValue | valueCount | format
                        CodecUtil.CheckHeader(@in, PackedInt32s.CODEC_NAME, PackedInt32s.VERSION_START, PackedInt32s.VERSION_CURRENT); // codec header
                        Assert.AreEqual(w.BitsPerValue, @in.ReadVInt32());
                        Assert.AreEqual(valueCount, @in.ReadVInt32());
                        Assert.AreEqual(w.Format.Id, @in.ReadVInt32());
                        Assert.AreEqual(startFp, @in.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        @in.Dispose();
                    }

                    { // test reader
                        IndexInput @in = d.OpenInput("out.bin", NewIOContext(Random));
                        PackedInt32s.Reader r = PackedInt32s.GetReader(@in);
                        Assert.AreEqual(fp, @in.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        for (int i = 0; i < valueCount; i++)
                        {
                            Assert.AreEqual(values[i], r.Get(i), "index=" + i + " valueCount=" + valueCount + " nbits=" + nbits + " for " + r.GetType().Name);
                        }
                        @in.Dispose();

                        long expectedBytesUsed = RamUsageEstimator.SizeOf(r);
                        long computedBytesUsed = r.RamBytesUsed();
                        Assert.AreEqual(expectedBytesUsed, computedBytesUsed, r.GetType() + "expected " + expectedBytesUsed + ", got: " + computedBytesUsed);
                    }

                    { // test reader iterator next
                        IndexInput @in = d.OpenInput("out.bin", NewIOContext(Random));
                        PackedInt32s.IReaderIterator r = PackedInt32s.GetReaderIterator(@in, bufferSize);
                        for (int i = 0; i < valueCount; i++)
                        {
                            Assert.AreEqual(values[i], r.Next(), "index=" + i + " valueCount=" + valueCount + " nbits=" + nbits + " for " + r.GetType().Name);
                            Assert.AreEqual(i, r.Ord);
                        }
                        assertEquals(fp, @in.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        @in.Dispose();
                    }

                    { // test reader iterator bulk next
                        IndexInput @in = d.OpenInput("out.bin", NewIOContext(Random));
                        PackedInt32s.IReaderIterator r = PackedInt32s.GetReaderIterator(@in, bufferSize);
                        int i = 0;
                        while (i < valueCount)
                        {
                            int count = TestUtil.NextInt32(Random, 1, 95);
                            Int64sRef next = r.Next(count);
                            for (int k = 0; k < next.Length; ++k)
                            {
                                Assert.AreEqual(values[i + k], next.Int64s[next.Offset + k], "index=" + i + " valueCount=" + valueCount + " nbits=" + nbits + " for " + r.GetType().Name);
                            }
                            i += next.Length;
                        }
                        Assert.AreEqual(fp, @in.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        @in.Dispose();
                    }

                    { // test direct reader get
                        IndexInput @in = d.OpenInput("out.bin", NewIOContext(Random));
                        PackedInt32s.Reader intsEnum = PackedInt32s.GetDirectReader(@in);
                        for (int i = 0; i < valueCount; i++)
                        {
                            string msg = "index=" + i + " valueCount=" + valueCount + " nbits=" + nbits + " for " + intsEnum.GetType().Name;
                            int index = Random.Next(valueCount);
                            Assert.AreEqual(values[index], intsEnum.Get(index), msg);
                        }
                        intsEnum.Get(intsEnum.Count - 1);
                        Assert.AreEqual(fp, @in.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                        @in.Dispose();
                    }
                    d.Dispose();
                }
            }
        }

        [Test]
        public virtual void TestEndPointer()
        {
            Directory dir = NewDirectory();
            int valueCount = RandomInts.RandomInt32Between(Random, 1, 1000);
            IndexOutput @out = dir.CreateOutput("tests.bin", NewIOContext(Random));
            for (int i = 0; i < valueCount; ++i)
            {
                @out.WriteInt64(0);
            }
            @out.Dispose();
            IndexInput @in = dir.OpenInput("tests.bin", NewIOContext(Random));
            for (int version = PackedInt32s.VERSION_START; version <= PackedInt32s.VERSION_CURRENT; ++version)
            {
                for (int bpv = 1; bpv <= 64; ++bpv)
                {
                    foreach (PackedInt32s.Format format in PackedInt32s.Format.Values)
                    {
                        if (!format.IsSupported(bpv))
                        {
                            continue;
                        }
                        long byteCount = format.ByteCount(version, valueCount, bpv);
                        string msg = "format=" + format + ",version=" + version + ",valueCount=" + valueCount + ",bpv=" + bpv;

                        // test iterator
                        @in.Seek(0L);
                        PackedInt32s.IReaderIterator it = PackedInt32s.GetReaderIteratorNoHeader(@in, format, version, valueCount, bpv, RandomInts.RandomInt32Between(Random, 1, 1 << 16));
                        for (int i = 0; i < valueCount; ++i)
                        {
                            it.Next();
                        }
                        Assert.AreEqual(byteCount, @in.Position, msg); // LUCENENET specific: Renamed from getFilePointer() to match FileStream

                        // test direct reader
                        @in.Seek(0L);
                        PackedInt32s.Reader directReader = PackedInt32s.GetDirectReaderNoHeader(@in, format, version, valueCount, bpv);
                        directReader.Get(valueCount - 1);
                        Assert.AreEqual(byteCount, @in.Position, msg); // LUCENENET specific: Renamed from getFilePointer() to match FileStream

                        // test reader
                        @in.Seek(0L);
                        PackedInt32s.GetReaderNoHeader(@in, format, version, valueCount, bpv);
                        Assert.AreEqual(byteCount, @in.Position, msg); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    }
                }
            }
            @in.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestControlledEquality()
        {
            const int VALUE_COUNT = 255;
            const int BITS_PER_VALUE = 8;

            IList<PackedInt32s.Mutable> packedInts = CreatePackedInts(VALUE_COUNT, BITS_PER_VALUE);
            foreach (PackedInt32s.Mutable packedInt in packedInts)
            {
                for (int i = 0; i < packedInt.Count; i++)
                {
                    packedInt.Set(i, i + 1);
                }
            }
            AssertListEquality(packedInts);
        }

        [Test]
        public virtual void TestRandomBulkCopy()
        {
            int numIters = AtLeast(3);
            for (int iter = 0; iter < numIters; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("\nTEST: iter=" + iter);
                }
                int valueCount = AtLeast(100000);
                int bits1 = TestUtil.NextInt32(Random, 1, 64);
                int bits2 = TestUtil.NextInt32(Random, 1, 64);
                if (bits1 > bits2)
                {
                    int tmp = bits1;
                    bits1 = bits2;
                    bits2 = tmp;
                }
                if (Verbose)
                {
                    Console.WriteLine("  valueCount=" + valueCount + " bits1=" + bits1 + " bits2=" + bits2);
                }

                PackedInt32s.Mutable packed1 = PackedInt32s.GetMutable(valueCount, bits1, PackedInt32s.COMPACT);
                PackedInt32s.Mutable packed2 = PackedInt32s.GetMutable(valueCount, bits2, PackedInt32s.COMPACT);

                long maxValue = PackedInt32s.MaxValue(bits1);
                for (int i = 0; i < valueCount; i++)
                {
                    long val = TestUtil.NextInt64(Random, 0, maxValue);
                    packed1.Set(i, val);
                    packed2.Set(i, val);
                }

                long[] buffer = new long[valueCount];

                // Copy random slice over, 20 times:
                for (int iter2 = 0; iter2 < 20; iter2++)
                {
                    int start = Random.Next(valueCount - 1);
                    int len = TestUtil.NextInt32(Random, 1, valueCount - start);
                    int offset;
                    if (Verbose)
                    {
                        Console.WriteLine("  copy " + len + " values @ " + start);
                    }
                    if (len == valueCount)
                    {
                        offset = 0;
                    }
                    else
                    {
                        offset = Random.Next(valueCount - len);
                    }
                    if (Random.NextBoolean())
                    {
                        int got = packed1.Get(start, buffer, offset, len);
                        Assert.IsTrue(got <= len);
                        int sot = packed2.Set(start, buffer, offset, got);
                        Assert.IsTrue(sot <= got);
                    }
                    else
                    {
                        PackedInt32s.Copy(packed1, offset, packed2, offset, len, Random.Next(10 * len));
                    }

                    /*
                    for(int i=0;i<valueCount;i++) {
                      Assert.AreEqual("value " + i, packed1.Get(i), packed2.Get(i));
                    }
                    */
                }

                for (int i = 0; i < valueCount; i++)
                {
                    Assert.AreEqual(packed1.Get(i), packed2.Get(i), "value " + i);
                }
            }
        }

        [Test]
        public virtual void TestRandomEquality()
        {
            int numIters = AtLeast(2);
            for (int i = 0; i < numIters; ++i)
            {
                int valueCount = TestUtil.NextInt32(Random, 1, 300);

                for (int bitsPerValue = 1; bitsPerValue <= 64; bitsPerValue++)
                {
                    AssertRandomEquality(valueCount, bitsPerValue, Random.NextInt64());
                }
            }
        }

        private static void AssertRandomEquality(int valueCount, int bitsPerValue, long randomSeed)
        {
            IList<PackedInt32s.Mutable> packedInts = CreatePackedInts(valueCount, bitsPerValue);
            foreach (PackedInt32s.Mutable packedInt in packedInts)
            {
                try
                {
                    Fill(packedInt, PackedInt32s.MaxValue(bitsPerValue), randomSeed);
                }
                catch (Exception e) when (e.IsException())
                {
                    e.printStackTrace(Console.Error);
                    Assert.Fail(string.Format(CultureInfo.InvariantCulture, "Exception while filling {0}: valueCount={1}, bitsPerValue={2}", packedInt.GetType().Name, valueCount, bitsPerValue));
                }
            }
            AssertListEquality(packedInts);
        }

        private static IList<PackedInt32s.Mutable> CreatePackedInts(int valueCount, int bitsPerValue)
        {
            IList<PackedInt32s.Mutable> packedInts = new JCG.List<PackedInt32s.Mutable>();
            if (bitsPerValue <= 8)
            {
                packedInts.Add(new Direct8(valueCount));
            }
            if (bitsPerValue <= 16)
            {
                packedInts.Add(new Direct16(valueCount));
            }
            if (bitsPerValue <= 24 && valueCount <= Packed8ThreeBlocks.MAX_SIZE)
            {
                packedInts.Add(new Packed8ThreeBlocks(valueCount));
            }
            if (bitsPerValue <= 32)
            {
                packedInts.Add(new Direct32(valueCount));
            }
            if (bitsPerValue <= 48 && valueCount <= Packed16ThreeBlocks.MAX_SIZE)
            {
                packedInts.Add(new Packed16ThreeBlocks(valueCount));
            }
            if (bitsPerValue <= 63)
            {
                packedInts.Add(new Packed64(valueCount, bitsPerValue));
            }
            packedInts.Add(new Direct64(valueCount));
            for (int bpv = bitsPerValue; bpv <= Packed64SingleBlock.MAX_SUPPORTED_BITS_PER_VALUE; ++bpv)
            {
                if (Packed64SingleBlock.IsSupported(bpv))
                {
                    packedInts.Add(Packed64SingleBlock.Create(valueCount, bpv));
                }
            }
            return packedInts;
        }

        private static void Fill(PackedInt32s.Mutable packedInt, long maxValue, long randomSeed)
        {
            Random rnd2 = new J2N.Randomizer(randomSeed);
            for (int i = 0; i < packedInt.Count; i++)
            {
                long value = TestUtil.NextInt64(rnd2, 0, maxValue);
                packedInt.Set(i, value);
                Assert.AreEqual(value, packedInt.Get(i), string.Format(CultureInfo.InvariantCulture, "The set/get of the value at index {0} should match for {1}", i, packedInt.GetType().Name));
            }
        }

        private static void AssertListEquality<T1>(IList<T1> packedInts) where T1 : PackedInt32s.Reader
        {
            AssertListEquality("", packedInts);
        }

        private static void AssertListEquality<T1>(string message, IList<T1> packedInts) where T1 : PackedInt32s.Reader
        {
            if (packedInts.Count == 0)
            {
                return;
            }
            PackedInt32s.Reader @base = packedInts[0];
            int valueCount = @base.Count;
            foreach (PackedInt32s.Reader packedInt in packedInts)
            {
                Assert.AreEqual(valueCount, packedInt.Count, message + ". The number of values should be the same ");
            }
            for (int i = 0; i < valueCount; i++)
            {
                for (int j = 1; j < packedInts.Count; j++)
                {
                    Assert.AreEqual( @base.Get(i), packedInts[j].Get(i), string.Format(CultureInfo.InvariantCulture, "{0}. The value at index {1} should be the same for {2} and {3}", message, i, @base.GetType().Name, packedInts[j].GetType().Name));
                }
            }
        }

        [Test]
        public virtual void TestSingleValue()
        {
            for (int bitsPerValue = 1; bitsPerValue <= 64; ++bitsPerValue)
            {
                Directory dir = NewDirectory();
                IndexOutput @out = dir.CreateOutput("out", NewIOContext(Random));
                PackedInt32s.Writer w = PackedInt32s.GetWriter(@out, 1, bitsPerValue, PackedInt32s.DEFAULT);
                long value = 17L & PackedInt32s.MaxValue(bitsPerValue);
                w.Add(value);
                w.Finish();
                long end = @out.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                @out.Dispose();

                IndexInput @in = dir.OpenInput("out", NewIOContext(Random));
                Reader reader = PackedInt32s.GetReader(@in);
                string msg = "Impl=" + w.GetType().Name + ", bitsPerValue=" + bitsPerValue;
                Assert.AreEqual(1, reader.Count, msg);
                Assert.AreEqual(value, reader.Get(0), msg);
                Assert.AreEqual(end, @in.Position, msg); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                @in.Dispose();

                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestSecondaryBlockChange()
        {
            PackedInt32s.Mutable mutable = new Packed64(26, 5);
            mutable.Set(24, 31);
            Assert.AreEqual(31, mutable.Get(24), "The value #24 should be correct");
            mutable.Set(4, 16);
            Assert.AreEqual(31, mutable.Get(24), "The value #24 should remain unchanged");
        }

        /*
          Check if the structures properly handle the case where
          index * bitsPerValue > Integer.MAX_VALUE
        
          NOTE: this test allocates 256 MB
         */
        [Ignore("See LUCENE-4488 - LUCENENET NOTE: In .NET it is not possible to catch OOME")]
        [Test]
        public virtual void TestIntOverflow()
        {
            int INDEX = (int)Math.Pow(2, 30) + 1;
            int BITS = 2;

            Packed64 p64 = null;
            try
            {
                p64 = new Packed64(INDEX, BITS);
            }
            catch (Exception oome) when (oome.IsOutOfMemoryError())
            {
                // this can easily happen: we're allocating a
                // long[] that needs 256-273 MB.  Heap is 512 MB,
                // but not all of that is available for large
                // objects ... empirical testing shows we only
                // have ~ 67 MB free.
            }
            if (p64 != null)
            {
                p64.Set(INDEX - 1, 1);
                Assert.AreEqual(1, p64.Get(INDEX - 1), "The value at position " + (INDEX - 1) + " should be correct for Packed64");
                p64 = null;
            }

            Packed64SingleBlock p64sb = null;
            try
            {
                p64sb = Packed64SingleBlock.Create(INDEX, BITS);
            }
            catch (Exception oome) when (oome.IsOutOfMemoryError())
            {
                // Ignore: see comment above
            }
            if (p64sb != null)
            {
                p64sb.Set(INDEX - 1, 1);
                Assert.AreEqual(1, p64sb.Get(INDEX - 1), "The value at position " + (INDEX - 1) + " should be correct for " + p64sb.GetType().Name);
            }

            int index = int.MaxValue / 24 + 1;
            Packed8ThreeBlocks p8 = null;
            try
            {
                p8 = new Packed8ThreeBlocks(index);
            }
            catch (Exception oome) when (oome.IsOutOfMemoryError())
            {
                // Ignore: see comment above
            }
            if (p8 != null)
            {
                p8.Set(index - 1, 1);
                Assert.AreEqual(1, p8.Get(index - 1), "The value at position " + (index - 1) + " should be correct for Packed8ThreeBlocks");
                p8 = null;
            }

            index = int.MaxValue / 48 + 1;
            Packed16ThreeBlocks p16 = null;
            try
            {
                p16 = new Packed16ThreeBlocks(index);
            }
            catch (Exception oome) when (oome.IsOutOfMemoryError())
            {
                // Ignore: see comment above
            }
            if (p16 != null)
            {
                p16.Set(index - 1, 1);
                Assert.AreEqual(1, p16.Get(index - 1), "The value at position " + (index - 1) + " should be correct for Packed16ThreeBlocks");
                p16 = null;
            }
        }

        [Test]
        public virtual void TestFill()
        {
            const int valueCount = 1111;
            int from = Random.Next(valueCount + 1);
            int to = from + Random.Next(valueCount + 1 - from);
            for (int bpv = 1; bpv <= 64; ++bpv)
            {
                long val = TestUtil.NextInt64(Random, 0, PackedInt32s.MaxValue(bpv));
                IList<PackedInt32s.Mutable> packedInts = CreatePackedInts(valueCount, bpv);
                foreach (PackedInt32s.Mutable ints in packedInts)
                {
                    string msg = ints.GetType().Name + " bpv=" + bpv + ", from=" + from + ", to=" + to + ", val=" + val;
                    ints.Fill(0, ints.Count, 1);
                    ints.Fill(from, to, val);
                    for (int i = 0; i < ints.Count; ++i)
                    {
                        if (i >= from && i < to)
                        {
                            Assert.AreEqual(val, ints.Get(i), msg + ", i=" + i);
                        }
                        else
                        {
                            Assert.AreEqual(1, ints.Get(i), msg + ", i=" + i);
                        }
                    }
                }
            }
        }

        [Test]
        public virtual void TestPackedIntsNull()
        {
            // must be > 10 for the bulk reads below
            int size = TestUtil.NextInt32(Random, 11, 256);
            Reader packedInts = new PackedInt32s.NullReader(size);
            Assert.AreEqual(0, packedInts.Get(TestUtil.NextInt32(Random, 0, size - 1)));
            long[] arr = new long[size + 10];
            int r;
            Arrays.Fill(arr, 1);
            r = packedInts.Get(0, arr, 0, size - 1);
            Assert.AreEqual(size - 1, r);
            for (r--; r >= 0; r--)
            {
                Assert.AreEqual(0, arr[r]);
            }
            Arrays.Fill(arr, 1);
            r = packedInts.Get(10, arr, 0, size + 10);
            Assert.AreEqual(size - 10, r);
            for (int i = 0; i < size - 10; i++)
            {
                Assert.AreEqual(0, arr[i]);
            }

        }

        [Test]
        public virtual void TestBulkGet()
        {
            const int valueCount = 1111;
            int index = Random.Next(valueCount);
            int len = TestUtil.NextInt32(Random, 1, valueCount * 2);
            int off = Random.Next(77);

            for (int bpv = 1; bpv <= 64; ++bpv)
            {
                long mask = PackedInt32s.MaxValue(bpv);
                IList<PackedInt32s.Mutable> packedInts = CreatePackedInts(valueCount, bpv);

                foreach (PackedInt32s.Mutable ints in packedInts)
                {
                    for (int i = 0; i < ints.Count; ++i)
                    {
                        ints.Set(i, (31L * i - 1099) & mask);
                    }
                    long[] arr = new long[off + len];

                    string msg = ints.GetType().Name + " valueCount=" + valueCount + ", index=" + index + ", len=" + len + ", off=" + off;
                    int gets = ints.Get(index, arr, off, len);
                    Assert.IsTrue(gets > 0, msg);
                    Assert.IsTrue(gets <= len, msg);
                    Assert.IsTrue(gets <= ints.Count - index, msg);

                    for (int i = 0; i < arr.Length; ++i)
                    {
                        string m = msg + ", i=" + i;
                        if (i >= off && i < off + gets)
                        {
                            Assert.AreEqual(ints.Get(i - off + index), arr[i], m);
                        }
                        else
                        {
                            Assert.AreEqual(0, arr[i], m);
                        }
                    }
                }
            }
        }

        [Test]
        public virtual void TestBulkSet()
        {
            const int valueCount = 1111;
            int index = Random.Next(valueCount);
            int len = TestUtil.NextInt32(Random, 1, valueCount * 2);
            int off = Random.Next(77);
            long[] arr = new long[off + len];

            for (int bpv = 1; bpv <= 64; ++bpv)
            {
                long mask = PackedInt32s.MaxValue(bpv);
                IList<PackedInt32s.Mutable> packedInts = CreatePackedInts(valueCount, bpv);
                for (int i = 0; i < arr.Length; ++i)
                {
                    arr[i] = (31L * i + 19) & mask;
                }

                foreach (PackedInt32s.Mutable ints in packedInts)
                {
                    string msg = ints.GetType().Name + " valueCount=" + valueCount + ", index=" + index + ", len=" + len + ", off=" + off;
                    int sets = ints.Set(index, arr, off, len);
                    Assert.IsTrue(sets > 0, msg);
                    Assert.IsTrue(sets <= len, msg);

                    for (int i = 0; i < ints.Count; ++i)
                    {
                        string m = msg + ", i=" + i;
                        if (i >= index && i < index + sets)
                        {
                            Assert.AreEqual(arr[off - index + i], ints.Get(i), m);
                        }
                        else
                        {
                            Assert.AreEqual(0, ints.Get(i), m);
                        }
                    }
                }
            }
        }

        [Test]
        public virtual void TestCopy()
        {
            int valueCount = TestUtil.NextInt32(Random, 5, 600);
            int off1 = Random.Next(valueCount);
            int off2 = Random.Next(valueCount);
            int len = Random.Next(Math.Min(valueCount - off1, valueCount - off2));
            int mem = Random.Next(1024);

            for (int bpv = 1; bpv <= 64; ++bpv)
            {
                long mask = PackedInt32s.MaxValue(bpv);
                foreach (PackedInt32s.Mutable r1 in CreatePackedInts(valueCount, bpv))
                {
                    for (int i = 0; i < r1.Count; ++i)
                    {
                        r1.Set(i, (31L * i - 1023) & mask);
                    }
                    foreach (PackedInt32s.Mutable r2 in CreatePackedInts(valueCount, bpv))
                    {
                        string msg = "src=" + r1 + ", dest=" + r2 + ", srcPos=" + off1 + ", destPos=" + off2 + ", len=" + len + ", mem=" + mem;
                        PackedInt32s.Copy(r1, off1, r2, off2, len, mem);
                        for (int i = 0; i < r2.Count; ++i)
                        {
                            string m = msg + ", i=" + i;
                            if (i >= off2 && i < off2 + len)
                            {
                                Assert.AreEqual(r1.Get(i - off2 + off1), r2.Get(i), m);
                            }
                            else
                            {
                                Assert.AreEqual(0, r2.Get(i), m);
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public virtual void TestGrowableWriter()
        {
            int valueCount = 113 + Random.Next(1111);
            GrowableWriter wrt = new GrowableWriter(1, valueCount, PackedInt32s.DEFAULT);
            wrt.Set(4, 2);
            wrt.Set(7, 10);
            wrt.Set(valueCount - 10, 99);
            wrt.Set(99, 999);
            wrt.Set(valueCount - 1, 1 << 10);
            Assert.AreEqual(1 << 10, wrt.Get(valueCount - 1));
            wrt.Set(99, (1 << 23) - 1);
            Assert.AreEqual(1 << 10, wrt.Get(valueCount - 1));
            wrt.Set(1, long.MaxValue);
            wrt.Set(2, -3);
            Assert.AreEqual(64, wrt.BitsPerValue);
            Assert.AreEqual(1 << 10, wrt.Get(valueCount - 1));
            Assert.AreEqual(long.MaxValue, wrt.Get(1));
            Assert.AreEqual(-3L, wrt.Get(2));
            Assert.AreEqual(2, wrt.Get(4));
            Assert.AreEqual((1 << 23) - 1, wrt.Get(99));
            Assert.AreEqual(10, wrt.Get(7));
            Assert.AreEqual(99, wrt.Get(valueCount - 10));
            Assert.AreEqual(1 << 10, wrt.Get(valueCount - 1));
            Assert.AreEqual(RamUsageEstimator.SizeOf(wrt), wrt.RamBytesUsed());
        }

        [Test]
        public virtual void TestPagedGrowableWriter()
        {
            int pageSize = 1 << (TestUtil.NextInt32(Random, 6, 30));
            // supports 0 values?
            PagedGrowableWriter writer = new PagedGrowableWriter(0, pageSize, TestUtil.NextInt32(Random, 1, 64), Random.NextSingle());
            Assert.AreEqual(0, writer.Count);

            // compare against AppendingDeltaPackedLongBuffer
            AppendingDeltaPackedInt64Buffer buf = new AppendingDeltaPackedInt64Buffer();
            int size = Random.Next(1000000);
            long max = 5;
            for (int i = 0; i < size; ++i)
            {
                buf.Add(TestUtil.NextInt64(Random, 0, max));
                if (Rarely())
                {
                    max = PackedInt32s.MaxValue(Rarely() ? TestUtil.NextInt32(Random, 0, 63) : TestUtil.NextInt32(Random, 0, 31));
                }
            }
            writer = new PagedGrowableWriter(size, pageSize, TestUtil.NextInt32(Random, 1, 64), Random.NextSingle());
            Assert.AreEqual(size, writer.Count);
            for (int i = size - 1; i >= 0; --i)
            {
                writer.Set(i, buf.Get(i));
            }
            for (int i = 0; i < size; ++i)
            {
                Assert.AreEqual(buf.Get(i), writer.Get(i));
            }

            // test ramBytesUsed
            Assert.AreEqual(RamUsageEstimator.SizeOf(writer), writer.RamBytesUsed(), 8);

            // test copy
            PagedGrowableWriter copy = writer.Resize(TestUtil.NextInt64(Random, writer.Count / 2, writer.Count * 3 / 2));
            for (long i = 0; i < copy.Count; ++i)
            {
                if (i < writer.Count)
                {
                    Assert.AreEqual(writer.Get(i), copy.Get(i));
                }
                else
                {
                    Assert.AreEqual(0, copy.Get(i));
                }
            }

            // test grow
            PagedGrowableWriter grow = writer.Grow(TestUtil.NextInt64(Random, writer.Count / 2, writer.Count * 3 / 2));
            for (long i = 0; i < grow.Count; ++i)
            {
                if (i < writer.Count)
                {
                    Assert.AreEqual(writer.Get(i), grow.Get(i));
                }
                else
                {
                    Assert.AreEqual(0, grow.Get(i));
                }
            }
        }

        [Test]
        public virtual void TestPagedMutable()
        {
            int bitsPerValue = TestUtil.NextInt32(Random, 1, 64);
            long max = PackedInt32s.MaxValue(bitsPerValue);
            int pageSize = 1 << (TestUtil.NextInt32(Random, 6, 30));
            // supports 0 values?
            PagedMutable writer = new PagedMutable(0, pageSize, bitsPerValue, Random.NextSingle() / 2);
            Assert.AreEqual(0, writer.Count);

            // compare against AppendingDeltaPackedLongBuffer
            AppendingDeltaPackedInt64Buffer buf = new AppendingDeltaPackedInt64Buffer();
            int size = Random.Next(1000000);

            for (int i = 0; i < size; ++i)
            {
                buf.Add(bitsPerValue == 64 ? Random.NextInt64() : TestUtil.NextInt64(Random, 0, max));
            }
            writer = new PagedMutable(size, pageSize, bitsPerValue, Random.NextSingle());
            Assert.AreEqual(size, writer.Count);
            for (int i = size - 1; i >= 0; --i)
            {
                writer.Set(i, buf.Get(i));
            }
            for (int i = 0; i < size; ++i)
            {
                Assert.AreEqual(buf.Get(i), writer.Get(i));
            }

            // test ramBytesUsed
            Assert.AreEqual(RamUsageEstimator.SizeOf(writer) - RamUsageEstimator.SizeOf(writer.format), writer.RamBytesUsed());

            // test copy
            PagedMutable copy = writer.Resize(TestUtil.NextInt64(Random, writer.Count / 2, writer.Count * 3 / 2));
            for (long i = 0; i < copy.Count; ++i)
            {
                if (i < writer.Count)
                {
                    Assert.AreEqual(writer.Get(i), copy.Get(i));
                }
                else
                {
                    Assert.AreEqual(0, copy.Get(i));
                }
            }

            // test grow
            PagedMutable grow = writer.Grow(TestUtil.NextInt64(Random, writer.Count / 2, writer.Count * 3 / 2));
            for (long i = 0; i < grow.Count; ++i)
            {
                if (i < writer.Count)
                {
                    Assert.AreEqual(writer.Get(i), grow.Get(i));
                }
                else
                {
                    Assert.AreEqual(0, grow.Get(i));
                }
            }
        }

        [Ignore("// memory hole")]
        [Test]
        public virtual void TestPagedGrowableWriterOverflow()
        {
            long size = TestUtil.NextInt64(Random, 2 * (long)int.MaxValue, 3 * (long)int.MaxValue);
            int pageSize = 1 << (TestUtil.NextInt32(Random, 16, 30));
            PagedGrowableWriter writer = new PagedGrowableWriter(size, pageSize, 1, Random.NextSingle());
            long index = TestUtil.NextInt64(Random, (long)int.MaxValue, size - 1);
            writer.Set(index, 2);
            Assert.AreEqual(2, writer.Get(index));
            for (int i = 0; i < 1000000; ++i)
            {
                long idx = TestUtil.NextInt64(Random, 0, size);
                if (idx == index)
                {
                    Assert.AreEqual(2, writer.Get(idx));
                }
                else
                {
                    Assert.AreEqual(0, writer.Get(idx));
                }
            }
        }

        [Test]
        public virtual void TestSave()
        {
            int valueCount = TestUtil.NextInt32(Random, 1, 2048);
            for (int bpv = 1; bpv <= 64; ++bpv)
            {
                int maxValue = (int)Math.Min(PackedInt32s.MaxValue(31), PackedInt32s.MaxValue(bpv));
                RAMDirectory directory = new RAMDirectory();
                IList<PackedInt32s.Mutable> packedInts = CreatePackedInts(valueCount, bpv);
                foreach (PackedInt32s.Mutable mutable in packedInts)
                {
                    for (int i = 0; i < mutable.Count; ++i)
                    {
                        mutable.Set(i, Random.Next(maxValue));
                    }

                    IndexOutput @out = directory.CreateOutput("packed-ints.bin", IOContext.DEFAULT);
                    mutable.Save(@out);
                    @out.Dispose();

                    IndexInput @in = directory.OpenInput("packed-ints.bin", IOContext.DEFAULT);
                    PackedInt32s.Reader reader = PackedInt32s.GetReader(@in);
                    Assert.AreEqual(mutable.BitsPerValue, reader.BitsPerValue);
                    Assert.AreEqual(valueCount, reader.Count);
                    if (mutable is Packed64SingleBlock)
                    {
                        // make sure that we used the right format so that the reader has
                        // the same performance characteristics as the mutable that has been
                        // serialized
                        Assert.IsTrue(reader is Packed64SingleBlock);
                    }
                    else
                    {
                        Assert.IsFalse(reader is Packed64SingleBlock);
                    }
                    for (int i = 0; i < valueCount; ++i)
                    {
                        Assert.AreEqual(mutable.Get(i), reader.Get(i));
                    }
                    @in.Dispose();
                    directory.DeleteFile("packed-ints.bin");
                }
                directory.Dispose();
            }
        }

        [Test]
        public virtual void TestEncodeDecode()
        {
            foreach (PackedInt32s.Format format in PackedInt32s.Format.Values)
            {
                for (int bpv = 1; bpv <= 64; ++bpv)
                {
                    if (!format.IsSupported(bpv))
                    {
                        continue;
                    }
                    string msg = format + " " + bpv;

                    PackedInt32s.IEncoder encoder = PackedInt32s.GetEncoder(format, PackedInt32s.VERSION_CURRENT, bpv);
                    PackedInt32s.IDecoder decoder = PackedInt32s.GetDecoder(format, PackedInt32s.VERSION_CURRENT, bpv);
                    int longBlockCount = encoder.Int64BlockCount;
                    int longValueCount = encoder.Int64ValueCount;
                    int byteBlockCount = encoder.ByteBlockCount;
                    int byteValueCount = encoder.ByteValueCount;
                    Assert.AreEqual(longBlockCount, decoder.Int64BlockCount);
                    Assert.AreEqual(longValueCount, decoder.Int64ValueCount);
                    Assert.AreEqual(byteBlockCount, decoder.ByteBlockCount);
                    Assert.AreEqual(byteValueCount, decoder.ByteValueCount);

                    int longIterations = Random.Next(100);
                    int byteIterations = longIterations * longValueCount / byteValueCount;
                    Assert.AreEqual(longIterations * longValueCount, byteIterations * byteValueCount);
                    int blocksOffset = Random.Next(100);
                    int valuesOffset = Random.Next(100);
                    int blocksOffset2 = Random.Next(100);
                    int blocksLen = longIterations * longBlockCount;

                    // 1. generate random inputs
                    long[] blocks = new long[blocksOffset + blocksLen];
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        blocks[i] = Random.NextInt64();
                        if (format == PackedInt32s.Format.PACKED_SINGLE_BLOCK && 64 % bpv != 0)
                        {
                            // clear highest bits for packed
                            int toClear = 64 % bpv;
                            blocks[i] = (blocks[i] << toClear).TripleShift(toClear);
                        }
                    }

                    // 2. decode
                    long[] values = new long[valuesOffset + longIterations * longValueCount];
                    decoder.Decode(blocks, blocksOffset, values, valuesOffset, longIterations);
                    foreach (long value in values)
                    {
                        Assert.IsTrue(value <= PackedInt32s.MaxValue(bpv));
                    }
                    // test decoding to int[]
                    int[] intValues;
                    if (bpv <= 32)
                    {
                        intValues = new int[values.Length];
                        decoder.Decode(blocks, blocksOffset, intValues, valuesOffset, longIterations);
                        Assert.IsTrue(Equals(intValues, values));
                    }
                    else
                    {
                        intValues = null;
                    }

                    // 3. re-encode
                    long[] blocks2 = new long[blocksOffset2 + blocksLen];
                    encoder.Encode(values, valuesOffset, blocks2, blocksOffset2, longIterations);
                    Assert.AreEqual(Arrays.CopyOfRange(blocks, blocksOffset, blocks.Length), Arrays.CopyOfRange(blocks2, blocksOffset2, blocks2.Length), msg);
                    // test encoding from int[]
                    if (bpv <= 32)
                    {
                        long[] blocks3 = new long[blocks2.Length];
                        encoder.Encode(intValues, valuesOffset, blocks3, blocksOffset2, longIterations);
                        Assert.AreEqual(blocks2, blocks3, msg);
                    }

                    // 4. byte[] decoding
                    byte[] byteBlocks = new byte[8 * blocks.Length];
                    ByteBuffer.Wrap(byteBlocks).AsInt64Buffer().Put(blocks);
                    long[] values2 = new long[valuesOffset + longIterations * longValueCount];
                    decoder.Decode(byteBlocks, blocksOffset * 8, values2, valuesOffset, byteIterations);
                    foreach (long value in values2)
                    {
                        Assert.IsTrue(value <= PackedInt32s.MaxValue(bpv), msg);
                    }
                    Assert.AreEqual(values, values2, msg);
                    // test decoding to int[]
                    if (bpv <= 32)
                    {
                        int[] intValues2 = new int[values2.Length];
                        decoder.Decode(byteBlocks, blocksOffset * 8, intValues2, valuesOffset, byteIterations);
                        Assert.IsTrue(Equals(intValues2, values2), msg);
                    }

                    // 5. byte[] encoding
                    byte[] blocks3_ = new byte[8 * (blocksOffset2 + blocksLen)];
                    encoder.Encode(values, valuesOffset, blocks3_, 8 * blocksOffset2, byteIterations);
                    assertEquals(msg, Int64Buffer.Wrap(blocks2), ByteBuffer.Wrap(blocks3_).AsInt64Buffer());
                    // test encoding from int[]
                    if (bpv <= 32)
                    {
                        byte[] blocks4 = new byte[blocks3_.Length];
                        encoder.Encode(intValues, valuesOffset, blocks4, 8 * blocksOffset2, byteIterations);
                        Assert.AreEqual(blocks3_, blocks4, msg);
                    }
                }
            }
        }

        private static bool Equals(int[] ints, long[] longs)
        {
            if (ints.Length != longs.Length)
            {
                return false;
            }
            for (int i = 0; i < ints.Length; ++i)
            {
                if ((ints[i] & 0xFFFFFFFFL) != longs[i])
                {
                    return false;
                }
            }
            return true;
        }

        internal enum DataType
        {
            PACKED,
            DELTA_PACKED,
            MONOTONIC
        }


        [Test]
        public virtual void TestAppendingLongBuffer()
        {

            long[] arr = new long[RandomInts.RandomInt32Between(Random, 1, 1000000)];
            float[] ratioOptions = new float[] { PackedInt32s.DEFAULT, PackedInt32s.COMPACT, PackedInt32s.FAST };
            foreach (int bpv in new int[] { 0, 1, 63, 64, RandomInts.RandomInt32Between(Random, 2, 62) })
            {
                foreach (DataType dataType in Enum.GetValues(typeof(DataType)))
                {
                    int pageSize = 1 << TestUtil.NextInt32(Random, 6, 20);
                    int initialPageCount = TestUtil.NextInt32(Random, 0, 16);
                    float acceptableOverheadRatio = ratioOptions[TestUtil.NextInt32(Random, 0, ratioOptions.Length - 1)];
                    AbstractAppendingInt64Buffer buf;
                    int inc;
                    switch (dataType)
                    {
                        case Lucene.Net.Util.Packed.TestPackedInts.DataType.PACKED:
                            buf = new AppendingPackedInt64Buffer(initialPageCount, pageSize, acceptableOverheadRatio);
                            inc = 0;
                            break;
                        case Lucene.Net.Util.Packed.TestPackedInts.DataType.DELTA_PACKED:
                            buf = new AppendingDeltaPackedInt64Buffer(initialPageCount, pageSize, acceptableOverheadRatio);
                            inc = 0;
                            break;
                        case Lucene.Net.Util.Packed.TestPackedInts.DataType.MONOTONIC:
                            buf = new MonotonicAppendingInt64Buffer(initialPageCount, pageSize, acceptableOverheadRatio);
                            inc = TestUtil.NextInt32(Random, -1000, 1000);
                            break;
                        default:
                            throw RuntimeException.Create("added a type and forgot to add it here?");

                    }

                    if (bpv == 0)
                    {
                        arr[0] = Random.NextInt64();
                        for (int i = 1; i < arr.Length; ++i)
                        {
                            arr[i] = arr[i - 1] + inc;
                        }
                    }
                    else if (bpv == 64)
                    {
                        for (int i = 0; i < arr.Length; ++i)
                        {
                            arr[i] = Random.NextInt64();
                        }
                    }
                    else
                    {
                        long minValue = TestUtil.NextInt64(Random, long.MinValue, long.MaxValue - PackedInt32s.MaxValue(bpv));
                        for (int i = 0; i < arr.Length; ++i)
                        {
                            arr[i] = minValue + inc * i + Random.NextInt64() & PackedInt32s.MaxValue(bpv); // TestUtil.nextLong is too slow
                        }
                    }

                    for (int i = 0; i < arr.Length; ++i)
                    {
                        buf.Add(arr[i]);
                    }
                    Assert.AreEqual(arr.Length, buf.Count);
                    if (Random.NextBoolean())
                    {
                        buf.Freeze();
                        if (Random.NextBoolean())
                        {
                            // Make sure double freeze doesn't break anything
                            buf.Freeze();
                        }
                    }
                    Assert.AreEqual(arr.Length, buf.Count);

                    for (int i = 0; i < arr.Length; ++i)
                    {
                        Assert.AreEqual(arr[i], buf.Get(i));
                    }

                    AbstractAppendingInt64Buffer.Iterator it = buf.GetIterator();
                    for (int i = 0; i < arr.Length; ++i)
                    {
                        if (Random.NextBoolean())
                        {
                            Assert.IsTrue(it.HasNext);
                        }
                        Assert.AreEqual(arr[i], it.Next());
                    }
                    Assert.IsFalse(it.HasNext);


                    long[] target = new long[arr.Length + 1024]; // check the request for more is OK.
                    for (int i = 0; i < arr.Length; i += TestUtil.NextInt32(Random, 0, 10000))
                    {
                        int lenToRead = Random.Next(buf.PageSize * 2) + 1;
                        lenToRead = Math.Min(lenToRead, target.Length - i);
                        int lenToCheck = Math.Min(lenToRead, arr.Length - i);
                        int off = i;
                        while (off < arr.Length && lenToRead > 0)
                        {
                            int read = buf.Get(off, target, off, lenToRead);
                            Assert.IsTrue(read > 0);
                            Assert.IsTrue(read <= lenToRead);
                            lenToRead -= read;
                            off += read;
                        }

                        for (int j = 0; j < lenToCheck; j++)
                        {
                            Assert.AreEqual(arr[j + i], target[j + i]);
                        }
                    }

                    long expectedBytesUsed = RamUsageEstimator.SizeOf(buf);
                    long computedBytesUsed = buf.RamBytesUsed();
                    Assert.AreEqual(expectedBytesUsed, computedBytesUsed);
                }
            }
        }

        [Test]
        public virtual void TestPackedInputOutput()
        {
            long[] longs = new long[Random.Next(8192)];
            int[] bitsPerValues = new int[longs.Length];
            bool[] skip = new bool[longs.Length];
            for (int i = 0; i < longs.Length; ++i)
            {
                int bpv = RandomInts.RandomInt32Between(Random, 1, 64);
                bitsPerValues[i] = Random.NextBoolean() ? bpv : TestUtil.NextInt32(Random, bpv, 64);
                if (bpv == 64)
                {
                    longs[i] = Random.NextInt64();
                }
                else
                {
                    longs[i] = TestUtil.NextInt64(Random, 0, PackedInt32s.MaxValue(bpv));
                }
                skip[i] = Rarely();
            }

            Directory dir = NewDirectory();
            IndexOutput @out = dir.CreateOutput("out.bin", IOContext.DEFAULT);
            PackedDataOutput pout = new PackedDataOutput(@out);
            long totalBits = 0;
            for (int i = 0; i < longs.Length; ++i)
            {
                pout.WriteInt64(longs[i], bitsPerValues[i]);
                totalBits += bitsPerValues[i];
                if (skip[i])
                {
                    pout.Flush();
                    totalBits = 8 * (long)Math.Ceiling((double)totalBits / 8);
                }
            }
            pout.Flush();
            Assert.AreEqual((long)Math.Ceiling((double)totalBits / 8), @out.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            @out.Dispose();
            IndexInput @in = dir.OpenInput("out.bin", IOContext.READ_ONCE);
            PackedDataInput pin = new PackedDataInput(@in);
            for (int i = 0; i < longs.Length; ++i)
            {
                Assert.AreEqual(longs[i], pin.ReadInt64(bitsPerValues[i]), "" + i);
                if (skip[i])
                {
                    pin.SkipToNextByte();
                }
            }
            assertEquals((long)Math.Ceiling((double)totalBits / 8), @in.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            @in.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestBlockPackedReaderWriter()
        {
            int iters = AtLeast(2);
            for (int iter = 0; iter < iters; ++iter)
            {
                int blockSize = 1 << TestUtil.NextInt32(Random, 6, 18);
                int valueCount = Random.Next(1 << 18);
                long[] values = new long[valueCount];
                long minValue = 0;
                int bpv = 0;
                for (int i = 0; i < valueCount; ++i)
                {
                    if (i % blockSize == 0)
                    {
                        minValue = Rarely() ? Random.Next(256) : Rarely() ? -5 : Random.NextInt64();
                        bpv = Random.Next(65);
                    }
                    if (bpv == 0)
                    {
                        values[i] = minValue;
                    }
                    else if (bpv == 64)
                    {
                        values[i] = Random.NextInt64();
                    }
                    else
                    {
                        values[i] = minValue + TestUtil.NextInt64(Random, 0, (1L << bpv) - 1);
                    }
                }

                Directory dir = NewDirectory();
                IndexOutput @out = dir.CreateOutput("out.bin", IOContext.DEFAULT);
                BlockPackedWriter writer = new BlockPackedWriter(@out, blockSize);
                for (int i = 0; i < valueCount; ++i)
                {
                    Assert.AreEqual(i, writer.Ord);
                    writer.Add(values[i]);
                }
                Assert.AreEqual(valueCount, writer.Ord);
                writer.Finish();
                Assert.AreEqual(valueCount, writer.Ord);
                long fp = @out.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                @out.Dispose();

                IndexInput in1 = dir.OpenInput("out.bin", IOContext.DEFAULT);
                byte[] buf = new byte[(int)fp];
                in1.ReadBytes(buf, 0, (int)fp);
                in1.Seek(0L);
                ByteArrayDataInput in2 = new ByteArrayDataInput(buf);
                DataInput @in = Random.NextBoolean() ? (DataInput)in1 : in2;
                BlockPackedReaderIterator it = new BlockPackedReaderIterator(@in, PackedInt32s.VERSION_CURRENT, blockSize, valueCount);
                for (int i = 0; i < valueCount; )
                {
                    if (Random.NextBoolean())
                    {
                        Assert.AreEqual(values[i], it.Next(), "" + i);
                        ++i;
                    }
                    else
                    {
                        Int64sRef nextValues = it.Next(TestUtil.NextInt32(Random, 1, 1024));
                        for (int j = 0; j < nextValues.Length; ++j)
                        {
                            Assert.AreEqual(values[i + j], nextValues.Int64s[nextValues.Offset + j], "" + (i + j));
                        }
                        i += nextValues.Length;
                    }
                    Assert.AreEqual(i, it.Ord);
                }
                assertEquals(fp, @in is ByteArrayDataInput ? ((ByteArrayDataInput)@in).Position : ((IndexInput)@in).Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                try
                {
                    it.Next();
                    Assert.IsTrue(false);
                }
                catch (Exception e) when (e.IsIOException())
                {
                    // OK
                }

                if (@in is ByteArrayDataInput)
                {
                    ((ByteArrayDataInput)@in).Position = 0;
                }
                else
                {
                    ((IndexInput)@in).Seek(0L);
                }
                BlockPackedReaderIterator it2 = new BlockPackedReaderIterator(@in, PackedInt32s.VERSION_CURRENT, blockSize, valueCount);
                int k = 0;
                while (true)
                {
                    int skip = TestUtil.NextInt32(Random, 0, valueCount - k);
                    it2.Skip(skip);
                    k += skip;
                    Assert.AreEqual(k, it2.Ord);
                    if (k == valueCount)
                    {
                        break;
                    }
                    else
                    {
                        Assert.AreEqual(values[k], it2.Next());
                        ++k;
                    }
                }
                assertEquals(fp, @in is ByteArrayDataInput ? ((ByteArrayDataInput)@in).Position : (((IndexInput)@in).Position)); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                try
                {
                    it2.Skip(1);
                    Assert.IsTrue(false);
                }
                catch (Exception e) when (e.IsIOException())
                {
                    // OK
                }

                in1.Seek(0L);
                BlockPackedReader reader = new BlockPackedReader(in1, PackedInt32s.VERSION_CURRENT, blockSize, valueCount, Random.NextBoolean());
                assertEquals(in1.Position, in1.Length); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                for (k = 0; k < valueCount; ++k)
                {
                    Assert.AreEqual(values[k], reader.Get(k), "i=" + k);
                }
                in1.Dispose();
                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestMonotonicBlockPackedReaderWriter()
        {
            int iters = AtLeast(2);
            for (int iter = 0; iter < iters; ++iter)
            {
                int blockSize = 1 << TestUtil.NextInt32(Random, 6, 18);
                int valueCount = Random.Next(1 << 18);
                long[] values = new long[valueCount];
                if (valueCount > 0)
                {
                    values[0] = Random.NextBoolean() ? Random.Next(10) : Random.Next(int.MaxValue);
                    int maxDelta = Random.Next(64);
                    for (int i = 1; i < valueCount; ++i)
                    {
                        if (Random.NextDouble() < 0.1d)
                        {
                            maxDelta = Random.Next(64);
                        }
                        values[i] = Math.Max(0, values[i - 1] + TestUtil.NextInt32(Random, -16, maxDelta));
                    }
                }

                Directory dir = NewDirectory();
                IndexOutput @out = dir.CreateOutput("out.bin", IOContext.DEFAULT);
                MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(@out, blockSize);
                for (int i = 0; i < valueCount; ++i)
                {
                    Assert.AreEqual(i, writer.Ord);
                    writer.Add(values[i]);
                }
                Assert.AreEqual(valueCount, writer.Ord);
                writer.Finish();
                Assert.AreEqual(valueCount, writer.Ord);
                long fp = @out.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                @out.Dispose();

                IndexInput @in = dir.OpenInput("out.bin", IOContext.DEFAULT);
                MonotonicBlockPackedReader reader = new MonotonicBlockPackedReader(@in, PackedInt32s.VERSION_CURRENT, blockSize, valueCount, Random.NextBoolean());
                assertEquals(fp, @in.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                for (int i = 0; i < valueCount; ++i)
                {
                    Assert.AreEqual(values[i], reader.Get(i),"i=" + i);
                }
                @in.Dispose();
                dir.Dispose();
            }
        }

        [Test]
        [Nightly]
        public virtual void TestBlockReaderOverflow()
        {
            long valueCount = TestUtil.NextInt64(Random, 1L + int.MaxValue, (long)int.MaxValue * 2);
            int blockSize = 1 << TestUtil.NextInt32(Random, 20, 22);
            Directory dir = NewDirectory();
            IndexOutput @out = dir.CreateOutput("out.bin", IOContext.DEFAULT);
            BlockPackedWriter writer = new BlockPackedWriter(@out, blockSize);
            long value = Random.Next() & 0xFFFFFFFFL;
            long valueOffset = TestUtil.NextInt64(Random, 0, valueCount - 1);
            for (long i = 0; i < valueCount; )
            {
                Assert.AreEqual(i, writer.Ord);
                if ((i & (blockSize - 1)) == 0 && (i + blockSize < valueOffset || i > valueOffset && i + blockSize < valueCount))
                {
                    writer.AddBlockOfZeros();
                    i += blockSize;
                }
                else if (i == valueOffset)
                {
                    writer.Add(value);
                    ++i;
                }
                else
                {
                    writer.Add(0);
                    ++i;
                }
            }
            writer.Finish();
            @out.Dispose();
            IndexInput @in = dir.OpenInput("out.bin", IOContext.DEFAULT);
            BlockPackedReaderIterator it = new BlockPackedReaderIterator(@in, PackedInt32s.VERSION_CURRENT, blockSize, valueCount);
            it.Skip(valueOffset);
            Assert.AreEqual(value, it.Next());
            @in.Seek(0L);
            BlockPackedReader reader = new BlockPackedReader(@in, PackedInt32s.VERSION_CURRENT, blockSize, valueCount, Random.NextBoolean());
            Assert.AreEqual(value, reader.Get(valueOffset));
            for (int i = 0; i < 5; ++i)
            {
                long offset = TestUtil.NextInt64(Random, 0, valueCount - 1);
                if (offset == valueOffset)
                {
                    Assert.AreEqual(value, reader.Get(offset));
                }
                else
                {
                    Assert.AreEqual(0, reader.Get(offset));
                }
            }
            @in.Dispose();
            dir.Dispose();
        }

    }

}