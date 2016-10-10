using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using NUnit.Framework;

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

    using CodecUtil = Lucene.Net.Codecs.CodecUtil;
    using ByteArrayDataInput = Lucene.Net.Store.ByteArrayDataInput;
    using DataInput = Lucene.Net.Store.DataInput;
    using Directory = Lucene.Net.Store.Directory;
    using IOContext = Lucene.Net.Store.IOContext;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    //using Slow = Lucene.Net.Util.LuceneTestCase.Slow;
    using Reader = Lucene.Net.Util.Packed.PackedInts.Reader;

    [TestFixture]
    public class TestPackedInts : LuceneTestCase
    {
        [Test]
        public virtual void TestByteCount()
        {
            int iters = AtLeast(3);
            for (int i = 0; i < iters; ++i)
            {
                int valueCount = RandomInts.NextIntBetween(Random(), 1, int.MaxValue);
                foreach (PackedInts.Format format in PackedInts.Format.Values())
                {
                    for (int bpv = 1; bpv <= 64; ++bpv)
                    {
                        long byteCount = format.ByteCount(PackedInts.VERSION_CURRENT, valueCount, bpv);
                        string msg = "format=" + format + ", byteCount=" + byteCount + ", valueCount=" + valueCount + ", bpv=" + bpv;
                        Assert.IsTrue(byteCount * 8 >= (long)valueCount * bpv, msg);
                        if (format == PackedInts.Format.PACKED)
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
            Assert.AreEqual(61, PackedInts.BitsRequired((long)Math.Pow(2, 61) - 1));
            Assert.AreEqual(61, PackedInts.BitsRequired(0x1FFFFFFFFFFFFFFFL));
            Assert.AreEqual(62, PackedInts.BitsRequired(0x3FFFFFFFFFFFFFFFL));
            Assert.AreEqual(63, PackedInts.BitsRequired(0x7FFFFFFFFFFFFFFFL));
        }

        [Test]
        public virtual void TestMaxValues()
        {
            Assert.AreEqual(1, PackedInts.MaxValue(1), "1 bit -> max == 1");
            Assert.AreEqual(3, PackedInts.MaxValue(2), "2 bit -> max == 3");
            Assert.AreEqual(255, PackedInts.MaxValue(8), "8 bit -> max == 255");
            Assert.AreEqual(long.MaxValue, PackedInts.MaxValue(63), "63 bit -> max == Long.MAX_VALUE");
            Assert.AreEqual(long.MaxValue, PackedInts.MaxValue(64), "64 bit -> max == Long.MAX_VALUE (same as for 63 bit)");
        }

        [Test]
        public virtual void TestPackedInts_Mem()
        {
            int num = AtLeast(3);
            for (int iter = 0; iter < num; iter++)
            {
                for (int nbits = 1; nbits <= 64; nbits++)
                {
                    long maxValue = PackedInts.MaxValue(nbits);
                    int valueCount = TestUtil.NextInt(Random(), 1, 600);
                    int bufferSize = Random().NextBoolean() ? TestUtil.NextInt(Random(), 0, 48) : TestUtil.NextInt(Random(), 0, 4096);
                    Directory d = NewDirectory();

                    IndexOutput @out = d.CreateOutput("out.bin", NewIOContext(Random()));
                    float acceptableOverhead;
                    if (iter == 0)
                    {
                        // have the first iteration go through exact nbits
                        acceptableOverhead = 0.0f;
                    }
                    else
                    {
                        acceptableOverhead = Random().NextFloat();
                    }
                    PackedInts.Writer w = PackedInts.GetWriter(@out, valueCount, nbits, acceptableOverhead);
                    long startFp = @out.FilePointer;

                    int actualValueCount = Random().NextBoolean() ? valueCount : TestUtil.NextInt(Random(), 0, valueCount);
                    long[] values = new long[valueCount];
                    for (int i = 0; i < actualValueCount; i++)
                    {
                        if (nbits == 64)
                        {
                            values[i] = Random().NextLong();
                        }
                        else
                        {
                            values[i] = TestUtil.NextLong(Random(), 0, maxValue);
                        }
                        w.Add(values[i]);
                    }
                    w.Finish();
                    long fp = @out.FilePointer;
                    @out.Dispose();

                    // ensure that finish() added the (valueCount-actualValueCount) missing values
                    long bytes = w.Format.ByteCount(PackedInts.VERSION_CURRENT, valueCount, w.BitsPerValue());
                    Assert.AreEqual(bytes, fp - startFp);

                    { // test header
                        IndexInput @in = d.OpenInput("out.bin", NewIOContext(Random()));
                        // header = codec header | bitsPerValue | valueCount | format
                        CodecUtil.CheckHeader(@in, PackedInts.CODEC_NAME, PackedInts.VERSION_START, PackedInts.VERSION_CURRENT); // codec header
                        Assert.AreEqual(w.BitsPerValue(), @in.ReadVInt());
                        Assert.AreEqual(valueCount, @in.ReadVInt());
                        Assert.AreEqual(w.Format.id, @in.ReadVInt());
                        Assert.AreEqual(startFp, @in.FilePointer);
                        @in.Dispose();
                    }

                    { // test reader
                        IndexInput @in = d.OpenInput("out.bin", NewIOContext(Random()));
                        PackedInts.Reader r = PackedInts.GetReader(@in);
                        Assert.AreEqual(fp, @in.FilePointer);
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
                        IndexInput @in = d.OpenInput("out.bin", NewIOContext(Random()));
                        PackedInts.ReaderIterator r = PackedInts.GetReaderIterator(@in, bufferSize);
                        for (int i = 0; i < valueCount; i++)
                        {
                            Assert.AreEqual(values[i], r.Next(), "index=" + i + " valueCount=" + valueCount + " nbits=" + nbits + " for " + r.GetType().Name);
                            Assert.AreEqual(i, r.Ord());
                        }
                        Assert.AreEqual(fp, @in.FilePointer);
                        @in.Dispose();
                    }

                    { // test reader iterator bulk next
                        IndexInput @in = d.OpenInput("out.bin", NewIOContext(Random()));
                        PackedInts.ReaderIterator r = PackedInts.GetReaderIterator(@in, bufferSize);
                        int i = 0;
                        while (i < valueCount)
                        {
                            int count = TestUtil.NextInt(Random(), 1, 95);
                            LongsRef next = r.Next(count);
                            for (int k = 0; k < next.Length; ++k)
                            {
                                Assert.AreEqual(values[i + k], next.Longs[next.Offset + k], "index=" + i + " valueCount=" + valueCount + " nbits=" + nbits + " for " + r.GetType().Name);
                            }
                            i += next.Length;
                        }
                        Assert.AreEqual(fp, @in.FilePointer);
                        @in.Dispose();
                    }

                    { // test direct reader get
                        IndexInput @in = d.OpenInput("out.bin", NewIOContext(Random()));
                        PackedInts.Reader intsEnum = PackedInts.GetDirectReader(@in);
                        for (int i = 0; i < valueCount; i++)
                        {
                            string msg = "index=" + i + " valueCount=" + valueCount + " nbits=" + nbits + " for " + intsEnum.GetType().Name;
                            int index = Random().Next(valueCount);
                            Assert.AreEqual(values[index], intsEnum.Get(index), msg);
                        }
                        intsEnum.Get(intsEnum.Size() - 1);
                        Assert.AreEqual(fp, @in.FilePointer);
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
            int valueCount = RandomInts.NextIntBetween(Random(), 1, 1000);
            IndexOutput @out = dir.CreateOutput("tests.bin", NewIOContext(Random()));
            for (int i = 0; i < valueCount; ++i)
            {
                @out.WriteLong(0);
            }
            @out.Dispose();
            IndexInput @in = dir.OpenInput("tests.bin", NewIOContext(Random()));
            for (int version = PackedInts.VERSION_START; version <= PackedInts.VERSION_CURRENT; ++version)
            {
                for (int bpv = 1; bpv <= 64; ++bpv)
                {
                    foreach (PackedInts.Format format in PackedInts.Format.Values())
                    {
                        if (!format.IsSupported(bpv))
                        {
                            continue;
                        }
                        long byteCount = format.ByteCount(version, valueCount, bpv);
                        string msg = "format=" + format + ",version=" + version + ",valueCount=" + valueCount + ",bpv=" + bpv;

                        // test iterator
                        @in.Seek(0L);
                        PackedInts.ReaderIterator it = PackedInts.GetReaderIteratorNoHeader(@in, format, version, valueCount, bpv, RandomInts.NextIntBetween(Random(), 1, 1 << 16));
                        for (int i = 0; i < valueCount; ++i)
                        {
                            it.Next();
                        }
                        Assert.AreEqual(byteCount, @in.FilePointer, msg);

                        // test direct reader
                        @in.Seek(0L);
                        PackedInts.Reader directReader = PackedInts.GetDirectReaderNoHeader(@in, format, version, valueCount, bpv);
                        directReader.Get(valueCount - 1);
                        Assert.AreEqual(byteCount, @in.FilePointer, msg);

                        // test reader
                        @in.Seek(0L);
                        PackedInts.GetReaderNoHeader(@in, format, version, valueCount, bpv);
                        Assert.AreEqual(byteCount, @in.FilePointer, msg);
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

            IList<PackedInts.Mutable> packedInts = CreatePackedInts(VALUE_COUNT, BITS_PER_VALUE);
            foreach (PackedInts.Mutable packedInt in packedInts)
            {
                for (int i = 0; i < packedInt.Size(); i++)
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
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter=" + iter);
                }
                int valueCount = AtLeast(100000);
                int bits1 = TestUtil.NextInt(Random(), 1, 64);
                int bits2 = TestUtil.NextInt(Random(), 1, 64);
                if (bits1 > bits2)
                {
                    int tmp = bits1;
                    bits1 = bits2;
                    bits2 = tmp;
                }
                if (VERBOSE)
                {
                    Console.WriteLine("  valueCount=" + valueCount + " bits1=" + bits1 + " bits2=" + bits2);
                }

                PackedInts.Mutable packed1 = PackedInts.GetMutable(valueCount, bits1, PackedInts.COMPACT);
                PackedInts.Mutable packed2 = PackedInts.GetMutable(valueCount, bits2, PackedInts.COMPACT);

                long maxValue = PackedInts.MaxValue(bits1);
                for (int i = 0; i < valueCount; i++)
                {
                    long val = TestUtil.NextLong(Random(), 0, maxValue);
                    packed1.Set(i, val);
                    packed2.Set(i, val);
                }

                long[] buffer = new long[valueCount];

                // Copy random slice over, 20 times:
                for (int iter2 = 0; iter2 < 20; iter2++)
                {
                    int start = Random().Next(valueCount - 1);
                    int len = TestUtil.NextInt(Random(), 1, valueCount - start);
                    int offset;
                    if (VERBOSE)
                    {
                        Console.WriteLine("  copy " + len + " values @ " + start);
                    }
                    if (len == valueCount)
                    {
                        offset = 0;
                    }
                    else
                    {
                        offset = Random().Next(valueCount - len);
                    }
                    if (Random().NextBoolean())
                    {
                        int got = packed1.Get(start, buffer, offset, len);
                        Assert.IsTrue(got <= len);
                        int sot = packed2.Set(start, buffer, offset, got);
                        Assert.IsTrue(sot <= got);
                    }
                    else
                    {
                        PackedInts.Copy(packed1, offset, packed2, offset, len, Random().Next(10 * len));
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
                int valueCount = TestUtil.NextInt(Random(), 1, 300);

                for (int bitsPerValue = 1; bitsPerValue <= 64; bitsPerValue++)
                {
                    AssertRandomEquality(valueCount, bitsPerValue, Random().NextLong());
                }
            }
        }

        private static void AssertRandomEquality(int valueCount, int bitsPerValue, long randomSeed)
        {
            IList<PackedInts.Mutable> packedInts = CreatePackedInts(valueCount, bitsPerValue);
            foreach (PackedInts.Mutable packedInt in packedInts)
            {
                try
                {
                    Fill(packedInt, PackedInts.MaxValue(bitsPerValue), randomSeed);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.StackTrace);
                    Assert.Fail(string.Format(CultureInfo.InvariantCulture, "Exception while filling %s: valueCount=%d, bitsPerValue=%s", packedInt.GetType().Name, valueCount, bitsPerValue));
                }
            }
            AssertListEquality(packedInts);
        }

        private static IList<PackedInts.Mutable> CreatePackedInts(int valueCount, int bitsPerValue)
        {
            IList<PackedInts.Mutable> packedInts = new List<PackedInts.Mutable>();
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

        private static void Fill(PackedInts.Mutable packedInt, long maxValue, long randomSeed)
        {
            Random rnd2 = new Random((int)randomSeed);
            for (int i = 0; i < packedInt.Size(); i++)
            {
                long value = TestUtil.NextLong(rnd2, 0, maxValue);
                packedInt.Set(i, value);
                Assert.AreEqual(value, packedInt.Get(i), string.Format(CultureInfo.InvariantCulture, "The set/get of the value at index %d should match for %s", i, packedInt.GetType().Name));
            }
        }

        private static void AssertListEquality<T1>(IList<T1> packedInts) where T1 : PackedInts.Reader
        {
            AssertListEquality("", packedInts);
        }

        private static void AssertListEquality<T1>(string message, IList<T1> packedInts) where T1 : PackedInts.Reader
        {
            if (packedInts.Count == 0)
            {
                return;
            }
            PackedInts.Reader @base = packedInts[0];
            int valueCount = @base.Size();
            foreach (PackedInts.Reader packedInt in packedInts)
            {
                Assert.AreEqual(valueCount, packedInt.Size(), message + ". The number of values should be the same ");
            }
            for (int i = 0; i < valueCount; i++)
            {
                for (int j = 1; j < packedInts.Count; j++)
                {
                    Assert.AreEqual( @base.Get(i), packedInts[j].Get(i), string.Format(CultureInfo.InvariantCulture, "%s. The value at index %d should be the same for %s and %s", message, i, @base.GetType().Name, packedInts[j].GetType().Name));
                }
            }
        }

        [Test]
        public virtual void TestSingleValue()
        {
            for (int bitsPerValue = 1; bitsPerValue <= 64; ++bitsPerValue)
            {
                Directory dir = NewDirectory();
                IndexOutput @out = dir.CreateOutput("out", NewIOContext(Random()));
                PackedInts.Writer w = PackedInts.GetWriter(@out, 1, bitsPerValue, PackedInts.DEFAULT);
                long value = 17L & PackedInts.MaxValue(bitsPerValue);
                w.Add(value);
                w.Finish();
                long end = @out.FilePointer;
                @out.Dispose();

                IndexInput @in = dir.OpenInput("out", NewIOContext(Random()));
                Reader reader = PackedInts.GetReader(@in);
                string msg = "Impl=" + w.GetType().Name + ", bitsPerValue=" + bitsPerValue;
                Assert.AreEqual(1, reader.Size(), msg);
                Assert.AreEqual(value, reader.Get(0), msg);
                Assert.AreEqual(end, @in.FilePointer, msg);
                @in.Dispose();

                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestSecondaryBlockChange()
        {
            PackedInts.Mutable mutable = new Packed64(26, 5);
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
        [Ignore]
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
            catch (System.OutOfMemoryException oome)
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
            catch (System.OutOfMemoryException oome)
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
            catch (System.OutOfMemoryException oome)
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
            catch (System.OutOfMemoryException oome)
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
            int from = Random().Next(valueCount + 1);
            int to = from + Random().Next(valueCount + 1 - from);
            for (int bpv = 1; bpv <= 64; ++bpv)
            {
                long val = TestUtil.NextLong(Random(), 0, PackedInts.MaxValue(bpv));
                IList<PackedInts.Mutable> packedInts = CreatePackedInts(valueCount, bpv);
                foreach (PackedInts.Mutable ints in packedInts)
                {
                    string msg = ints.GetType().Name + " bpv=" + bpv + ", from=" + from + ", to=" + to + ", val=" + val;
                    ints.Fill(0, ints.Size(), 1);
                    ints.Fill(from, to, val);
                    for (int i = 0; i < ints.Size(); ++i)
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
            int size = TestUtil.NextInt(Random(), 11, 256);
            Reader packedInts = new PackedInts.NullReader(size);
            Assert.AreEqual(0, packedInts.Get(TestUtil.NextInt(Random(), 0, size - 1)));
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
            int index = Random().Next(valueCount);
            int len = TestUtil.NextInt(Random(), 1, valueCount * 2);
            int off = Random().Next(77);

            for (int bpv = 1; bpv <= 64; ++bpv)
            {
                long mask = PackedInts.MaxValue(bpv);
                IList<PackedInts.Mutable> packedInts = CreatePackedInts(valueCount, bpv);

                foreach (PackedInts.Mutable ints in packedInts)
                {
                    for (int i = 0; i < ints.Size(); ++i)
                    {
                        ints.Set(i, (31L * i - 1099) & mask);
                    }
                    long[] arr = new long[off + len];

                    string msg = ints.GetType().Name + " valueCount=" + valueCount + ", index=" + index + ", len=" + len + ", off=" + off;
                    int gets = ints.Get(index, arr, off, len);
                    Assert.IsTrue(gets > 0, msg);
                    Assert.IsTrue(gets <= len, msg);
                    Assert.IsTrue(gets <= ints.Size() - index, msg);

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
            int index = Random().Next(valueCount);
            int len = TestUtil.NextInt(Random(), 1, valueCount * 2);
            int off = Random().Next(77);
            long[] arr = new long[off + len];

            for (int bpv = 1; bpv <= 64; ++bpv)
            {
                long mask = PackedInts.MaxValue(bpv);
                IList<PackedInts.Mutable> packedInts = CreatePackedInts(valueCount, bpv);
                for (int i = 0; i < arr.Length; ++i)
                {
                    arr[i] = (31L * i + 19) & mask;
                }

                foreach (PackedInts.Mutable ints in packedInts)
                {
                    string msg = ints.GetType().Name + " valueCount=" + valueCount + ", index=" + index + ", len=" + len + ", off=" + off;
                    int sets = ints.Set(index, arr, off, len);
                    Assert.IsTrue(sets > 0, msg);
                    Assert.IsTrue(sets <= len, msg);

                    for (int i = 0; i < ints.Size(); ++i)
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
            int valueCount = TestUtil.NextInt(Random(), 5, 600);
            int off1 = Random().Next(valueCount);
            int off2 = Random().Next(valueCount);
            int len = Random().Next(Math.Min(valueCount - off1, valueCount - off2));
            int mem = Random().Next(1024);

            for (int bpv = 1; bpv <= 64; ++bpv)
            {
                long mask = PackedInts.MaxValue(bpv);
                foreach (PackedInts.Mutable r1 in CreatePackedInts(valueCount, bpv))
                {
                    for (int i = 0; i < r1.Size(); ++i)
                    {
                        r1.Set(i, (31L * i - 1023) & mask);
                    }
                    foreach (PackedInts.Mutable r2 in CreatePackedInts(valueCount, bpv))
                    {
                        string msg = "src=" + r1 + ", dest=" + r2 + ", srcPos=" + off1 + ", destPos=" + off2 + ", len=" + len + ", mem=" + mem;
                        PackedInts.Copy(r1, off1, r2, off2, len, mem);
                        for (int i = 0; i < r2.Size(); ++i)
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
            int valueCount = 113 + Random().Next(1111);
            GrowableWriter wrt = new GrowableWriter(1, valueCount, PackedInts.DEFAULT);
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
            int pageSize = 1 << (TestUtil.NextInt(Random(), 6, 30));
            // supports 0 values?
            PagedGrowableWriter writer = new PagedGrowableWriter(0, pageSize, TestUtil.NextInt(Random(), 1, 64), Random().NextFloat());
            Assert.AreEqual(0, writer.Size());

            // compare against AppendingDeltaPackedLongBuffer
            AppendingDeltaPackedLongBuffer buf = new AppendingDeltaPackedLongBuffer();
            int size = Random().Next(1000000);
            long max = 5;
            for (int i = 0; i < size; ++i)
            {
                buf.Add(TestUtil.NextLong(Random(), 0, max));
                if (Rarely())
                {
                    max = PackedInts.MaxValue(Rarely() ? TestUtil.NextInt(Random(), 0, 63) : TestUtil.NextInt(Random(), 0, 31));
                }
            }
            writer = new PagedGrowableWriter(size, pageSize, TestUtil.NextInt(Random(), 1, 64), Random().NextFloat());
            Assert.AreEqual(size, writer.Size());
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
            PagedGrowableWriter copy = writer.Resize(TestUtil.NextLong(Random(), writer.Size() / 2, writer.Size() * 3 / 2));
            for (long i = 0; i < copy.Size(); ++i)
            {
                if (i < writer.Size())
                {
                    Assert.AreEqual(writer.Get(i), copy.Get(i));
                }
                else
                {
                    Assert.AreEqual(0, copy.Get(i));
                }
            }

            // test grow
            PagedGrowableWriter grow = writer.Grow(TestUtil.NextLong(Random(), writer.Size() / 2, writer.Size() * 3 / 2));
            for (long i = 0; i < grow.Size(); ++i)
            {
                if (i < writer.Size())
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
            int bitsPerValue = TestUtil.NextInt(Random(), 1, 64);
            long max = PackedInts.MaxValue(bitsPerValue);
            int pageSize = 1 << (TestUtil.NextInt(Random(), 6, 30));
            // supports 0 values?
            PagedMutable writer = new PagedMutable(0, pageSize, bitsPerValue, Random().NextFloat() / 2);
            Assert.AreEqual(0, writer.Size());

            // compare against AppendingDeltaPackedLongBuffer
            AppendingDeltaPackedLongBuffer buf = new AppendingDeltaPackedLongBuffer();
            int size = Random().Next(1000000);

            for (int i = 0; i < size; ++i)
            {
                buf.Add(bitsPerValue == 64 ? Random().NextLong() : TestUtil.NextLong(Random(), 0, max));
            }
            writer = new PagedMutable(size, pageSize, bitsPerValue, Random().NextFloat());
            Assert.AreEqual(size, writer.Size());
            for (int i = size - 1; i >= 0; --i)
            {
                writer.Set(i, buf.Get(i));
            }
            for (int i = 0; i < size; ++i)
            {
                Assert.AreEqual(buf.Get(i), writer.Get(i));
            }

            // test ramBytesUsed
            Assert.AreEqual(RamUsageEstimator.SizeOf(writer) - RamUsageEstimator.SizeOf(writer.Format), writer.RamBytesUsed());

            // test copy
            PagedMutable copy = writer.Resize(TestUtil.NextLong(Random(), writer.Size() / 2, writer.Size() * 3 / 2));
            for (long i = 0; i < copy.Size(); ++i)
            {
                if (i < writer.Size())
                {
                    Assert.AreEqual(writer.Get(i), copy.Get(i));
                }
                else
                {
                    Assert.AreEqual(0, copy.Get(i));
                }
            }

            // test grow
            PagedMutable grow = writer.Grow(TestUtil.NextLong(Random(), writer.Size() / 2, writer.Size() * 3 / 2));
            for (long i = 0; i < grow.Size(); ++i)
            {
                if (i < writer.Size())
                {
                    Assert.AreEqual(writer.Get(i), grow.Get(i));
                }
                else
                {
                    Assert.AreEqual(0, grow.Get(i));
                }
            }
        }

        // memory hole
        [Ignore]
        [Test]
        public virtual void TestPagedGrowableWriterOverflow()
        {
            long size = TestUtil.NextLong(Random(), 2 * (long)int.MaxValue, 3 * (long)int.MaxValue);
            int pageSize = 1 << (TestUtil.NextInt(Random(), 16, 30));
            PagedGrowableWriter writer = new PagedGrowableWriter(size, pageSize, 1, Random().NextFloat());
            long index = TestUtil.NextLong(Random(), (long)int.MaxValue, size - 1);
            writer.Set(index, 2);
            Assert.AreEqual(2, writer.Get(index));
            for (int i = 0; i < 1000000; ++i)
            {
                long idx = TestUtil.NextLong(Random(), 0, size);
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
            int valueCount = TestUtil.NextInt(Random(), 1, 2048);
            for (int bpv = 1; bpv <= 64; ++bpv)
            {
                int maxValue = (int)Math.Min(PackedInts.MaxValue(31), PackedInts.MaxValue(bpv));
                RAMDirectory directory = new RAMDirectory();
                IList<PackedInts.Mutable> packedInts = CreatePackedInts(valueCount, bpv);
                foreach (PackedInts.Mutable mutable in packedInts)
                {
                    for (int i = 0; i < mutable.Size(); ++i)
                    {
                        mutable.Set(i, Random().Next(maxValue));
                    }

                    IndexOutput @out = directory.CreateOutput("packed-ints.bin", IOContext.DEFAULT);
                    mutable.Save(@out);
                    @out.Dispose();

                    IndexInput @in = directory.OpenInput("packed-ints.bin", IOContext.DEFAULT);
                    PackedInts.Reader reader = PackedInts.GetReader(@in);
                    Assert.AreEqual(mutable.BitsPerValue, reader.BitsPerValue);
                    Assert.AreEqual(valueCount, reader.Size());
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
        [Ignore("LUCENENET TODO: LongBuffer and the method AsLongBuffer are not yet ported, so we cannot currently run this test without porting or finding an alternative way to compare the data.")]
        public virtual void TestEncodeDecode()
        {
            foreach (PackedInts.Format format in PackedInts.Format.Values())
            {
                for (int bpv = 1; bpv <= 64; ++bpv)
                {
                    if (!format.IsSupported(bpv))
                    {
                        continue;
                    }
                    string msg = format + " " + bpv;

                    PackedInts.Encoder encoder = PackedInts.GetEncoder(format, PackedInts.VERSION_CURRENT, bpv);
                    PackedInts.Decoder decoder = PackedInts.GetDecoder(format, PackedInts.VERSION_CURRENT, bpv);
                    int longBlockCount = encoder.LongBlockCount();
                    int longValueCount = encoder.LongValueCount();
                    int byteBlockCount = encoder.ByteBlockCount();
                    int byteValueCount = encoder.ByteValueCount();
                    Assert.AreEqual(longBlockCount, decoder.LongBlockCount());
                    Assert.AreEqual(longValueCount, decoder.LongValueCount());
                    Assert.AreEqual(byteBlockCount, decoder.ByteBlockCount());
                    Assert.AreEqual(byteValueCount, decoder.ByteValueCount());

                    int longIterations = Random().Next(100);
                    int byteIterations = longIterations * longValueCount / byteValueCount;
                    Assert.AreEqual(longIterations * longValueCount, byteIterations * byteValueCount);
                    int blocksOffset = Random().Next(100);
                    int valuesOffset = Random().Next(100);
                    int blocksOffset2 = Random().Next(100);
                    int blocksLen = longIterations * longBlockCount;

                    // 1. generate random inputs
                    long[] blocks = new long[blocksOffset + blocksLen];
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        blocks[i] = Random().NextLong();
                        if (format == PackedInts.Format.PACKED_SINGLE_BLOCK && 64 % bpv != 0)
                        {
                            // clear highest bits for packed
                            int toClear = 64 % bpv;
                            blocks[i] = (int)((uint)(blocks[i] << toClear) >> toClear);
                        }
                    }

                    // 2. decode
                    long[] values = new long[valuesOffset + longIterations * longValueCount];
                    decoder.Decode(blocks, blocksOffset, values, valuesOffset, longIterations);
                    foreach (long value in values)
                    {
                        Assert.IsTrue(value <= PackedInts.MaxValue(bpv));
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

                    // LUCENENET TODO: LongBuffer and the method AsLongBuffer are not yet ported, so we
                    // cannot currently run the following tests.

                    //// 4. byte[] decoding
                    //byte[] byteBlocks = new byte[8 * blocks.Length];
                    //ByteBuffer.Wrap(byteBlocks).AsLongBuffer().Put(blocks);
                    //long[] values2 = new long[valuesOffset + longIterations * longValueCount];
                    //decoder.Decode(byteBlocks, blocksOffset * 8, values2, valuesOffset, byteIterations);
                    //foreach (long value in values2)
                    //{
                    //    Assert.IsTrue(value <= PackedInts.MaxValue(bpv), msg);
                    //}
                    //Assert.AreEqual(values, values2, msg);
                    //// test decoding to int[]
                    //if (bpv <= 32)
                    //{
                    //    int[] intValues2 = new int[values2.Length];
                    //    decoder.Decode(byteBlocks, blocksOffset * 8, intValues2, valuesOffset, byteIterations);
                    //    Assert.IsTrue(Equals(intValues2, values2), msg);
                    //}

                    //// 5. byte[] encoding
                    //byte[] blocks3_ = new byte[8 * (blocksOffset2 + blocksLen)];
                    //encoder.Encode(values, valuesOffset, blocks3_, 8 * blocksOffset2, byteIterations);
                    //assertEquals(msg, LongBuffer.Wrap(blocks2), ByteBuffer.Wrap(blocks3_).AsLongBuffer());
                    //// test encoding from int[]
                    //if (bpv <= 32)
                    //{
                    //    byte[] blocks4 = new byte[blocks3_.Length];
                    //    encoder.Encode(intValues, valuesOffset, blocks4, 8 * blocksOffset2, byteIterations);
                    //    Assert.AreEqual(blocks3_, blocks4, msg);
                    //}
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


        [Test, Timeout(80000)]
        public virtual void TestAppendingLongBuffer()
        {

            long[] arr = new long[RandomInts.NextIntBetween(Random(), 1, 1000000)];
            float[] ratioOptions = new float[] { PackedInts.DEFAULT, PackedInts.COMPACT, PackedInts.FAST };
            foreach (int bpv in new int[] { 0, 1, 63, 64, RandomInts.NextIntBetween(Random(), 2, 62) })
            {
                foreach (DataType dataType in Enum.GetValues(typeof(DataType)))
                {
                    int pageSize = 1 << TestUtil.NextInt(Random(), 6, 20);
                    int initialPageCount = TestUtil.NextInt(Random(), 0, 16);
                    float acceptableOverheadRatio = ratioOptions[TestUtil.NextInt(Random(), 0, ratioOptions.Length - 1)];
                    AbstractAppendingLongBuffer buf;
                    int inc;
                    switch (dataType)
                    {
                        case Lucene.Net.Util.Packed.TestPackedInts.DataType.PACKED:
                            buf = new AppendingPackedLongBuffer(initialPageCount, pageSize, acceptableOverheadRatio);
                            inc = 0;
                            break;
                        case Lucene.Net.Util.Packed.TestPackedInts.DataType.DELTA_PACKED:
                            buf = new AppendingDeltaPackedLongBuffer(initialPageCount, pageSize, acceptableOverheadRatio);
                            inc = 0;
                            break;
                        case Lucene.Net.Util.Packed.TestPackedInts.DataType.MONOTONIC:
                            buf = new MonotonicAppendingLongBuffer(initialPageCount, pageSize, acceptableOverheadRatio);
                            inc = TestUtil.NextInt(Random(), -1000, 1000);
                            break;
                        default:
                            throw new Exception("added a type and forgot to add it here?");

                    }

                    if (bpv == 0)
                    {
                        arr[0] = Random().NextLong();
                        for (int i = 1; i < arr.Length; ++i)
                        {
                            arr[i] = arr[i - 1] + inc;
                        }
                    }
                    else if (bpv == 64)
                    {
                        for (int i = 0; i < arr.Length; ++i)
                        {
                            arr[i] = Random().NextLong();
                        }
                    }
                    else
                    {
                        long minValue = TestUtil.NextLong(Random(), long.MinValue, long.MaxValue - PackedInts.MaxValue(bpv));
                        for (int i = 0; i < arr.Length; ++i)
                        {
                            arr[i] = minValue + inc * i + Random().NextLong() & PackedInts.MaxValue(bpv); // TestUtil.nextLong is too slow
                        }
                    }

                    for (int i = 0; i < arr.Length; ++i)
                    {
                        buf.Add(arr[i]);
                    }
                    Assert.AreEqual(arr.Length, buf.Size());
                    if (Random().NextBoolean())
                    {
                        buf.Freeze();
                        if (Random().NextBoolean())
                        {
                            // Make sure double freeze doesn't break anything
                            buf.Freeze();
                        }
                    }
                    Assert.AreEqual(arr.Length, buf.Size());

                    for (int i = 0; i < arr.Length; ++i)
                    {
                        Assert.AreEqual(arr[i], buf.Get(i));
                    }

                    AbstractAppendingLongBuffer.Iterator it = buf.GetIterator();
                    for (int i = 0; i < arr.Length; ++i)
                    {
                        if (Random().NextBoolean())
                        {
                            Assert.IsTrue(it.HasNext());
                        }
                        Assert.AreEqual(arr[i], it.Next());
                    }
                    Assert.IsFalse(it.HasNext());


                    long[] target = new long[arr.Length + 1024]; // check the request for more is OK.
                    for (int i = 0; i < arr.Length; i += TestUtil.NextInt(Random(), 0, 10000))
                    {
                        int lenToRead = Random().Next(buf.PageSize() * 2) + 1;
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
            long[] longs = new long[Random().Next(8192)];
            int[] bitsPerValues = new int[longs.Length];
            bool[] skip = new bool[longs.Length];
            for (int i = 0; i < longs.Length; ++i)
            {
                int bpv = RandomInts.NextIntBetween(Random(), 1, 64);
                bitsPerValues[i] = Random().NextBoolean() ? bpv : TestUtil.NextInt(Random(), bpv, 64);
                if (bpv == 64)
                {
                    longs[i] = Random().NextLong();
                }
                else
                {
                    longs[i] = TestUtil.NextLong(Random(), 0, PackedInts.MaxValue(bpv));
                }
                skip[i] = Rarely();
            }

            Directory dir = NewDirectory();
            IndexOutput @out = dir.CreateOutput("out.bin", IOContext.DEFAULT);
            PackedDataOutput pout = new PackedDataOutput(@out);
            long totalBits = 0;
            for (int i = 0; i < longs.Length; ++i)
            {
                pout.WriteLong(longs[i], bitsPerValues[i]);
                totalBits += bitsPerValues[i];
                if (skip[i])
                {
                    pout.Flush();
                    totalBits = 8 * (long)Math.Ceiling((double)totalBits / 8);
                }
            }
            pout.Flush();
            Assert.AreEqual((long)Math.Ceiling((double)totalBits / 8), @out.FilePointer);
            @out.Dispose();
            IndexInput @in = dir.OpenInput("out.bin", IOContext.READONCE);
            PackedDataInput pin = new PackedDataInput(@in);
            for (int i = 0; i < longs.Length; ++i)
            {
                Assert.AreEqual(longs[i], pin.ReadLong(bitsPerValues[i]), "" + i);
                if (skip[i])
                {
                    pin.SkipToNextByte();
                }
            }
            Assert.AreEqual((long)Math.Ceiling((double)totalBits / 8), @in.FilePointer);
            @in.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestBlockPackedReaderWriter()
        {
            int iters = AtLeast(2);
            for (int iter = 0; iter < iters; ++iter)
            {
                int blockSize = 1 << TestUtil.NextInt(Random(), 6, 18);
                int valueCount = Random().Next(1 << 18);
                long[] values = new long[valueCount];
                long minValue = 0;
                int bpv = 0;
                for (int i = 0; i < valueCount; ++i)
                {
                    if (i % blockSize == 0)
                    {
                        minValue = Rarely() ? Random().Next(256) : Rarely() ? -5 : Random().NextLong();
                        bpv = Random().Next(65);
                    }
                    if (bpv == 0)
                    {
                        values[i] = minValue;
                    }
                    else if (bpv == 64)
                    {
                        values[i] = Random().NextLong();
                    }
                    else
                    {
                        values[i] = minValue + TestUtil.NextLong(Random(), 0, (1L << bpv) - 1);
                    }
                }

                Directory dir = NewDirectory();
                IndexOutput @out = dir.CreateOutput("out.bin", IOContext.DEFAULT);
                BlockPackedWriter writer = new BlockPackedWriter(@out, blockSize);
                for (int i = 0; i < valueCount; ++i)
                {
                    Assert.AreEqual(i, writer.Ord());
                    writer.Add(values[i]);
                }
                Assert.AreEqual(valueCount, writer.Ord());
                writer.Finish();
                Assert.AreEqual(valueCount, writer.Ord());
                long fp = @out.FilePointer;
                @out.Dispose();

                IndexInput in1 = dir.OpenInput("out.bin", IOContext.DEFAULT);
                byte[] buf = new byte[(int)fp];
                in1.ReadBytes(buf, 0, (int)fp);
                in1.Seek(0L);
                ByteArrayDataInput in2 = new ByteArrayDataInput((byte[])(Array)buf);
                DataInput @in = Random().NextBoolean() ? (DataInput)in1 : in2;
                BlockPackedReaderIterator it = new BlockPackedReaderIterator(@in, PackedInts.VERSION_CURRENT, blockSize, valueCount);
                for (int i = 0; i < valueCount; )
                {
                    if (Random().NextBoolean())
                    {
                        Assert.AreEqual(values[i], it.Next(), "" + i);
                        ++i;
                    }
                    else
                    {
                        LongsRef nextValues = it.Next(TestUtil.NextInt(Random(), 1, 1024));
                        for (int j = 0; j < nextValues.Length; ++j)
                        {
                            Assert.AreEqual(values[i + j], nextValues.Longs[nextValues.Offset + j], "" + (i + j));
                        }
                        i += nextValues.Length;
                    }
                    Assert.AreEqual(i, it.Ord());
                }
                Assert.AreEqual(fp, @in is ByteArrayDataInput ? ((ByteArrayDataInput)@in).Position : ((IndexInput)@in).FilePointer);
                try
                {
                    it.Next();
                    Assert.IsTrue(false);
                }
                catch (IOException e)
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
                BlockPackedReaderIterator it2 = new BlockPackedReaderIterator(@in, PackedInts.VERSION_CURRENT, blockSize, valueCount);
                int k = 0;
                while (true)
                {
                    int skip = TestUtil.NextInt(Random(), 0, valueCount - k);
                    it2.Skip(skip);
                    k += skip;
                    Assert.AreEqual(k, it2.Ord());
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
                Assert.AreEqual(fp, @in is ByteArrayDataInput ? ((ByteArrayDataInput)@in).Position : ((IndexInput)@in).FilePointer);
                try
                {
                    it2.Skip(1);
                    Assert.IsTrue(false);
                }
                catch (IOException e)
                {
                    // OK
                }

                in1.Seek(0L);
                BlockPackedReader reader = new BlockPackedReader(in1, PackedInts.VERSION_CURRENT, blockSize, valueCount, Random().NextBoolean());
                Assert.AreEqual(in1.FilePointer, in1.Length());
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
                int blockSize = 1 << TestUtil.NextInt(Random(), 6, 18);
                int valueCount = Random().Next(1 << 18);
                long[] values = new long[valueCount];
                if (valueCount > 0)
                {
                    values[0] = Random().NextBoolean() ? Random().Next(10) : Random().Next(int.MaxValue);
                    int maxDelta = Random().Next(64);
                    for (int i = 1; i < valueCount; ++i)
                    {
                        if (Random().NextDouble() < 0.1d)
                        {
                            maxDelta = Random().Next(64);
                        }
                        values[i] = Math.Max(0, values[i - 1] + TestUtil.NextInt(Random(), -16, maxDelta));
                    }
                }

                Directory dir = NewDirectory();
                IndexOutput @out = dir.CreateOutput("out.bin", IOContext.DEFAULT);
                MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(@out, blockSize);
                for (int i = 0; i < valueCount; ++i)
                {
                    Assert.AreEqual(i, writer.Ord());
                    writer.Add(values[i]);
                }
                Assert.AreEqual(valueCount, writer.Ord());
                writer.Finish();
                Assert.AreEqual(valueCount, writer.Ord());
                long fp = @out.FilePointer;
                @out.Dispose();

                IndexInput @in = dir.OpenInput("out.bin", IOContext.DEFAULT);
                MonotonicBlockPackedReader reader = new MonotonicBlockPackedReader(@in, PackedInts.VERSION_CURRENT, blockSize, valueCount, Random().NextBoolean());
                Assert.AreEqual(fp, @in.FilePointer);
                for (int i = 0; i < valueCount; ++i)
                {
                    Assert.AreEqual(values[i], reader.Get(i),"i=" + i);
                }
                @in.Dispose();
                dir.Dispose();
            }
        }

        [Test, Timeout(180000)]
        public virtual void TestBlockReaderOverflow()
        {
            long valueCount = TestUtil.NextLong(Random(), 1L + int.MaxValue, (long)int.MaxValue * 2);
            int blockSize = 1 << TestUtil.NextInt(Random(), 20, 22);
            Directory dir = NewDirectory();
            IndexOutput @out = dir.CreateOutput("out.bin", IOContext.DEFAULT);
            BlockPackedWriter writer = new BlockPackedWriter(@out, blockSize);
            long value = Random().Next() & 0xFFFFFFFFL;
            long valueOffset = TestUtil.NextLong(Random(), 0, valueCount - 1);
            for (long i = 0; i < valueCount; )
            {
                Assert.AreEqual(i, writer.Ord());
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
            BlockPackedReaderIterator it = new BlockPackedReaderIterator(@in, PackedInts.VERSION_CURRENT, blockSize, valueCount);
            it.Skip(valueOffset);
            Assert.AreEqual(value, it.Next());
            @in.Seek(0L);
            BlockPackedReader reader = new BlockPackedReader(@in, PackedInts.VERSION_CURRENT, blockSize, valueCount, Random().NextBoolean());
            Assert.AreEqual(value, reader.Get(valueOffset));
            for (int i = 0; i < 5; ++i)
            {
                long offset = TestUtil.NextLong(Random(), 0, valueCount - 1);
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