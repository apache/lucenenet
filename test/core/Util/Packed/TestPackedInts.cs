using System;
using System.Collections.Generic;

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
	using Slow = Lucene.Net.Util.LuceneTestCase.Slow;
	using Reader = Lucene.Net.Util.Packed.PackedInts.Reader;
	using Ignore = org.junit.Ignore;

	using RandomInts = com.carrotsearch.randomizedtesting.generators.RandomInts;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Slow public class TestPackedInts extends Lucene.Net.Util.LuceneTestCase
	public class TestPackedInts : LuceneTestCase
	{

	  public virtual void TestByteCount()
	  {
		int iters = atLeast(3);
		for (int i = 0; i < iters; ++i)
		{
		  int valueCount = RandomInts.randomIntBetween(random(), 1, int.MaxValue);
		  foreach (PackedInts.Format format in PackedInts.Format.values())
		  {
			for (int bpv = 1; bpv <= 64; ++bpv)
			{
			  long byteCount = format.byteCount(PackedInts.VERSION_CURRENT, valueCount, bpv);
			  string msg = "format=" + format + ", byteCount=" + byteCount + ", valueCount=" + valueCount + ", bpv=" + bpv;
			  Assert.IsTrue(msg, byteCount * 8 >= (long) valueCount * bpv);
			  if (format == PackedInts.Format.PACKED)
			  {
				Assert.IsTrue(msg, (byteCount - 1) * 8 < (long) valueCount * bpv);
			  }
			}
		  }
		}
	  }

	  public virtual void TestBitsRequired()
	  {
		Assert.AreEqual(61, PackedInts.bitsRequired((long)Math.Pow(2, 61) - 1));
		Assert.AreEqual(61, PackedInts.bitsRequired(0x1FFFFFFFFFFFFFFFL));
		Assert.AreEqual(62, PackedInts.bitsRequired(0x3FFFFFFFFFFFFFFFL));
		Assert.AreEqual(63, PackedInts.bitsRequired(0x7FFFFFFFFFFFFFFFL));
	  }

	  public virtual void TestMaxValues()
	  {
		Assert.AreEqual("1 bit -> max == 1", 1, PackedInts.maxValue(1));
		Assert.AreEqual("2 bit -> max == 3", 3, PackedInts.maxValue(2));
		Assert.AreEqual("8 bit -> max == 255", 255, PackedInts.maxValue(8));
		Assert.AreEqual("63 bit -> max == Long.MAX_VALUE", long.MaxValue, PackedInts.maxValue(63));
		Assert.AreEqual("64 bit -> max == Long.MAX_VALUE (same as for 63 bit)", long.MaxValue, PackedInts.maxValue(64));
	  }

	  public virtual void TestPackedInts()
	  {
		int num = atLeast(3);
		for (int iter = 0; iter < num; iter++)
		{
		  for (int nbits = 1;nbits <= 64;nbits++)
		  {
			long maxValue = PackedInts.maxValue(nbits);
			int valueCount = TestUtil.Next(random(), 1, 600);
			int bufferSize = random().nextBoolean() ? TestUtil.Next(random(), 0, 48) : TestUtil.Next(random(), 0, 4096);
			Directory d = newDirectory();

			IndexOutput @out = d.createOutput("out.bin", newIOContext(random()));
			float acceptableOverhead;
			if (iter == 0)
			{
			  // have the first iteration go through exact nbits
			  acceptableOverhead = 0.0f;
			}
			else
			{
			  acceptableOverhead = random().nextFloat();
			}
			PackedInts.Writer w = PackedInts.getWriter(@out, valueCount, nbits, acceptableOverhead);
			long startFp = @out.FilePointer;

			int actualValueCount = random().nextBoolean() ? valueCount : TestUtil.Next(random(), 0, valueCount);
			long[] values = new long[valueCount];
			for (int i = 0;i < actualValueCount;i++)
			{
			  if (nbits == 64)
			  {
				values[i] = random().nextLong();
			  }
			  else
			  {
				values[i] = TestUtil.nextLong(random(), 0, maxValue);
			  }
			  w.add(values[i]);
			}
			w.finish();
			long fp = @out.FilePointer;
			@out.close();

			// ensure that finish() added the (valueCount-actualValueCount) missing values
			long bytes = w.Format.byteCount(PackedInts.VERSION_CURRENT, valueCount, w.bitsPerValue);
			Assert.AreEqual(bytes, fp - startFp);

			{ // test header
			  IndexInput @in = d.openInput("out.bin", newIOContext(random()));
			  // header = codec header | bitsPerValue | valueCount | format
			  CodecUtil.checkHeader(@in, PackedInts.CODEC_NAME, PackedInts.VERSION_START, PackedInts.VERSION_CURRENT); // codec header
			  Assert.AreEqual(w.bitsPerValue, @in.readVInt());
			  Assert.AreEqual(valueCount, @in.readVInt());
			  Assert.AreEqual(w.Format.Id, @in.readVInt());
			  Assert.AreEqual(startFp, @in.FilePointer);
			  @in.close();
			}

			{ // test reader
			  IndexInput @in = d.openInput("out.bin", newIOContext(random()));
			  PackedInts.Reader r = PackedInts.getReader(@in);
			  Assert.AreEqual(fp, @in.FilePointer);
			  for (int i = 0;i < valueCount;i++)
			  {
				Assert.AreEqual("index=" + i + " valueCount=" + valueCount + " nbits=" + nbits + " for " + r.GetType().Name, values[i], r.get(i));
			  }
			  @in.close();

			  long expectedBytesUsed = RamUsageEstimator.sizeOf(r);
			  long computedBytesUsed = r.ramBytesUsed();
			  Assert.AreEqual(r.GetType() + "expected " + expectedBytesUsed + ", got: " + computedBytesUsed, expectedBytesUsed, computedBytesUsed);
			}

			{ // test reader iterator next
			  IndexInput @in = d.openInput("out.bin", newIOContext(random()));
			  PackedInts.ReaderIterator r = PackedInts.getReaderIterator(@in, bufferSize);
			  for (int i = 0;i < valueCount;i++)
			  {
				Assert.AreEqual("index=" + i + " valueCount=" + valueCount + " nbits=" + nbits + " for " + r.GetType().Name, values[i], r.next());
				Assert.AreEqual(i, r.ord());
			  }
			  Assert.AreEqual(fp, @in.FilePointer);
			  @in.close();
			}

			{ // test reader iterator bulk next
			  IndexInput @in = d.openInput("out.bin", newIOContext(random()));
			  PackedInts.ReaderIterator r = PackedInts.getReaderIterator(@in, bufferSize);
			  int i = 0;
			  while (i < valueCount)
			  {
				int count = TestUtil.Next(random(), 1, 95);
				LongsRef next = r.next(count);
				for (int k = 0; k < next.length; ++k)
				{
				  Assert.AreEqual("index=" + i + " valueCount=" + valueCount + " nbits=" + nbits + " for " + r.GetType().Name, values[i + k], next.longs[next.offset + k]);
				}
				i += next.length;
			  }
			  Assert.AreEqual(fp, @in.FilePointer);
			  @in.close();
			}

			{ // test direct reader get
			  IndexInput @in = d.openInput("out.bin", newIOContext(random()));
			  PackedInts.Reader intsEnum = PackedInts.getDirectReader(@in);
			  for (int i = 0; i < valueCount; i++)
			  {
				string msg = "index=" + i + " valueCount=" + valueCount + " nbits=" + nbits + " for " + intsEnum.GetType().Name;
				int index = random().Next(valueCount);
				Assert.AreEqual(msg, values[index], intsEnum.get(index));
			  }
			  intsEnum.get(intsEnum.size() - 1);
			  Assert.AreEqual(fp, @in.FilePointer);
			  @in.close();
			}
			d.close();
		  }
		}
	  }

	  public virtual void TestEndPointer()
	  {
		Directory dir = newDirectory();
		int valueCount = RandomInts.randomIntBetween(random(), 1, 1000);
		IndexOutput @out = dir.createOutput("tests.bin", newIOContext(random()));
		for (int i = 0; i < valueCount; ++i)
		{
		  @out.writeLong(0);
		}
		@out.close();
		IndexInput @in = dir.openInput("tests.bin", newIOContext(random()));
		for (int version = PackedInts.VERSION_START; version <= PackedInts.VERSION_CURRENT; ++version)
		{
		  for (int bpv = 1; bpv <= 64; ++bpv)
		  {
			foreach (PackedInts.Format format in PackedInts.Format.values())
			{
			  if (!format.isSupported(bpv))
			  {
				continue;
			  }
			  long byteCount = format.byteCount(version, valueCount, bpv);
			  string msg = "format=" + format + ",version=" + version + ",valueCount=" + valueCount + ",bpv=" + bpv;

			  // test iterator
			  @in.seek(0L);
			  PackedInts.ReaderIterator it = PackedInts.getReaderIteratorNoHeader(@in, format, version, valueCount, bpv, RandomInts.randomIntBetween(random(), 1, 1 << 16));
			  for (int i = 0; i < valueCount; ++i)
			  {
				it.next();
			  }
			  Assert.AreEqual(msg, byteCount, @in.FilePointer);

			  // test direct reader
			  @in.seek(0L);
			  PackedInts.Reader directReader = PackedInts.getDirectReaderNoHeader(@in, format, version, valueCount, bpv);
			  directReader.get(valueCount - 1);
			  Assert.AreEqual(msg, byteCount, @in.FilePointer);

			  // test reader
			  @in.seek(0L);
			  PackedInts.getReaderNoHeader(@in, format, version, valueCount, bpv);
			  Assert.AreEqual(msg, byteCount, @in.FilePointer);
			}
		  }
		}
		@in.close();
		dir.close();
	  }

	  public virtual void TestControlledEquality()
	  {
		const int VALUE_COUNT = 255;
		const int BITS_PER_VALUE = 8;

		IList<PackedInts.Mutable> packedInts = CreatePackedInts(VALUE_COUNT, BITS_PER_VALUE);
		foreach (PackedInts.Mutable packedInt in packedInts)
		{
		  for (int i = 0 ; i < packedInt.size() ; i++)
		  {
			packedInt.set(i, i + 1);
		  }
		}
		AssertListEquality(packedInts);
	  }

	  public virtual void TestRandomBulkCopy()
	  {
		int numIters = atLeast(3);
		for (int iter = 0;iter < numIters;iter++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: iter=" + iter);
		  }
		  int valueCount = atLeast(100000);
		  int bits1 = TestUtil.Next(random(), 1, 64);
		  int bits2 = TestUtil.Next(random(), 1, 64);
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

		  PackedInts.Mutable packed1 = PackedInts.getMutable(valueCount, bits1, PackedInts.COMPACT);
		  PackedInts.Mutable packed2 = PackedInts.getMutable(valueCount, bits2, PackedInts.COMPACT);

		  long maxValue = PackedInts.maxValue(bits1);
		  for (int i = 0;i < valueCount;i++)
		  {
			long val = TestUtil.nextLong(random(), 0, maxValue);
			packed1.set(i, val);
			packed2.set(i, val);
		  }

		  long[] buffer = new long[valueCount];

		  // Copy random slice over, 20 times:
		  for (int iter2 = 0;iter2 < 20;iter2++)
		  {
			int start = random().Next(valueCount - 1);
			int len = TestUtil.Next(random(), 1, valueCount - start);
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
			  offset = random().Next(valueCount - len);
			}
			if (random().nextBoolean())
			{
			  int got = packed1.get(start, buffer, offset, len);
			  Assert.IsTrue(got <= len);
			  int sot = packed2.set(start, buffer, offset, got);
			  Assert.IsTrue(sot <= got);
			}
			else
			{
			  PackedInts.copy(packed1, offset, packed2, offset, len, random().Next(10 * len));
			}

			/*
			for(int i=0;i<valueCount;i++) {
			  Assert.AreEqual("value " + i, packed1.get(i), packed2.get(i));
			}
			*/
		  }

		  for (int i = 0;i < valueCount;i++)
		  {
			Assert.AreEqual("value " + i, packed1.get(i), packed2.get(i));
		  }
		}
	  }

	  public virtual void TestRandomEquality()
	  {
		int numIters = atLeast(2);
		for (int i = 0; i < numIters; ++i)
		{
		  int valueCount = TestUtil.Next(random(), 1, 300);

		  for (int bitsPerValue = 1 ; bitsPerValue <= 64 ; bitsPerValue++)
		  {
			AssertRandomEquality(valueCount, bitsPerValue, random().nextLong());
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
			Fill(packedInt, PackedInts.maxValue(bitsPerValue), randomSeed);
		  }
		  catch (Exception e)
		  {
			e.printStackTrace(System.err);
			Assert.Fail(string.format(Locale.ROOT, "Exception while filling %s: valueCount=%d, bitsPerValue=%s", packedInt.GetType().Name, valueCount, bitsPerValue));
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
		  if (Packed64SingleBlock.isSupported(bpv))
		  {
			packedInts.Add(Packed64SingleBlock.create(valueCount, bpv));
		  }
		}
		return packedInts;
	  }

	  private static void Fill(PackedInts.Mutable packedInt, long maxValue, long randomSeed)
	  {
		Random rnd2 = new Random(randomSeed);
		for (int i = 0 ; i < packedInt.size() ; i++)
		{
		  long value = TestUtil.nextLong(rnd2, 0, maxValue);
		  packedInt.set(i, value);
		  Assert.AreEqual(string.format(Locale.ROOT, "The set/get of the value at index %d should match for %s", i, packedInt.GetType().Name), value, packedInt.get(i));
		}
	  }

	  private static void assertListEquality<T1>(IList<T1> packedInts) where T1 : PackedInts.Reader
	  {
		AssertListEquality("", packedInts);
	  }

	  private static void assertListEquality<T1>(string message, IList<T1> packedInts) where T1 : PackedInts.Reader
	  {
		if (packedInts.Count == 0)
		{
		  return;
		}
		PackedInts.Reader @base = packedInts[0];
		int valueCount = @base.size();
		foreach (PackedInts.Reader packedInt in packedInts)
		{
		  Assert.AreEqual(message + ". The number of values should be the same ", valueCount, packedInt.size());
		}
		for (int i = 0 ; i < valueCount ; i++)
		{
		  for (int j = 1 ; j < packedInts.Count ; j++)
		  {
			Assert.AreEqual(string.format(Locale.ROOT, "%s. The value at index %d should be the same for %s and %s", message, i, @base.GetType().Name, packedInts[j].GetType().Name), @base.get(i), packedInts[j].get(i));
		  }
		}
	  }

	  public virtual void TestSingleValue()
	  {
		for (int bitsPerValue = 1; bitsPerValue <= 64; ++bitsPerValue)
		{
		  Directory dir = newDirectory();
		  IndexOutput @out = dir.createOutput("out", newIOContext(random()));
		  PackedInts.Writer w = PackedInts.getWriter(@out, 1, bitsPerValue, PackedInts.DEFAULT);
		  long value = 17L & PackedInts.maxValue(bitsPerValue);
		  w.add(value);
		  w.finish();
		  long end = @out.FilePointer;
		  @out.close();

		  IndexInput @in = dir.openInput("out", newIOContext(random()));
		  Reader reader = PackedInts.getReader(@in);
		  string msg = "Impl=" + w.GetType().Name + ", bitsPerValue=" + bitsPerValue;
		  Assert.AreEqual(msg, 1, reader.size());
		  Assert.AreEqual(msg, value, reader.get(0));
		  Assert.AreEqual(msg, end, @in.FilePointer);
		  @in.close();

		  dir.close();
		}
	  }

	  public virtual void TestSecondaryBlockChange()
	  {
		PackedInts.Mutable mutable = new Packed64(26, 5);
		mutable.set(24, 31);
		Assert.AreEqual("The value #24 should be correct", 31, mutable.get(24));
		mutable.set(4, 16);
		Assert.AreEqual("The value #24 should remain unchanged", 31, mutable.get(24));
	  }

	  /*
	    Check if the structures properly handle the case where
	    index * bitsPerValue > Integer.MAX_VALUE
	    
	    NOTE: this test allocates 256 MB
	   */
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ignore("See LUCENE-4488") public void testIntOverflow()
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
		  p64.set(INDEX - 1, 1);
		  Assert.AreEqual("The value at position " + (INDEX - 1) + " should be correct for Packed64", 1, p64.get(INDEX - 1));
		  p64 = null;
		}

		Packed64SingleBlock p64sb = null;
		try
		{
		  p64sb = Packed64SingleBlock.create(INDEX, BITS);
		}
		catch (System.OutOfMemoryException oome)
		{
		  // Ignore: see comment above
		}
		if (p64sb != null)
		{
		  p64sb.set(INDEX - 1, 1);
		  Assert.AreEqual("The value at position " + (INDEX - 1) + " should be correct for " + p64sb.GetType().Name, 1, p64sb.get(INDEX - 1));
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
		  p8.set(index - 1, 1);
		  Assert.AreEqual("The value at position " + (index - 1) + " should be correct for Packed8ThreeBlocks", 1, p8.get(index - 1));
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
		  p16.set(index - 1, 1);
		  Assert.AreEqual("The value at position " + (index - 1) + " should be correct for Packed16ThreeBlocks", 1, p16.get(index - 1));
		  p16 = null;
		}
	  }

	  public virtual void TestFill()
	  {
		const int valueCount = 1111;
		int from = random().Next(valueCount + 1);
		int to = from + random().Next(valueCount + 1 - from);
		for (int bpv = 1; bpv <= 64; ++bpv)
		{
		  long val = TestUtil.nextLong(random(), 0, PackedInts.maxValue(bpv));
		  IList<PackedInts.Mutable> packedInts = CreatePackedInts(valueCount, bpv);
		  foreach (PackedInts.Mutable ints in packedInts)
		  {
			string msg = ints.GetType().Name + " bpv=" + bpv + ", from=" + from + ", to=" + to + ", val=" + val;
			ints.fill(0, ints.size(), 1);
			ints.fill(from, to, val);
			for (int i = 0; i < ints.size(); ++i)
			{
			  if (i >= from && i < to)
			  {
				Assert.AreEqual(msg + ", i=" + i, val, ints.get(i));
			  }
			  else
			  {
				Assert.AreEqual(msg + ", i=" + i, 1, ints.get(i));
			  }
			}
		  }
		}
	  }

	  public virtual void TestPackedIntsNull()
	  {
		// must be > 10 for the bulk reads below
		int size = TestUtil.Next(random(), 11, 256);
		Reader packedInts = new PackedInts.NullReader(size);
		Assert.AreEqual(0, packedInts.get(TestUtil.Next(random(), 0, size - 1)));
		long[] arr = new long[size + 10];
		int r;
		Arrays.fill(arr, 1);
		r = packedInts.get(0, arr, 0, size - 1);
		Assert.AreEqual(size - 1, r);
		for (r--; r >= 0; r--)
		{
		  Assert.AreEqual(0, arr[r]);
		}
		Arrays.fill(arr, 1);
		r = packedInts.get(10, arr, 0, size + 10);
		Assert.AreEqual(size - 10, r);
		for (int i = 0; i < size - 10; i++)
		{
		  Assert.AreEqual(0, arr[i]);
		}

	  }

	  public virtual void TestBulkGet()
	  {
		const int valueCount = 1111;
		int index = random().Next(valueCount);
		int len = TestUtil.Next(random(), 1, valueCount * 2);
		int off = random().Next(77);

		for (int bpv = 1; bpv <= 64; ++bpv)
		{
		  long mask = PackedInts.maxValue(bpv);
		  IList<PackedInts.Mutable> packedInts = CreatePackedInts(valueCount, bpv);

		  foreach (PackedInts.Mutable ints in packedInts)
		  {
			for (int i = 0; i < ints.size(); ++i)
			{
			  ints.set(i, (31L * i - 1099) & mask);
			}
			long[] arr = new long[off + len];

			string msg = ints.GetType().Name + " valueCount=" + valueCount + ", index=" + index + ", len=" + len + ", off=" + off;
			int gets = ints.get(index, arr, off, len);
			Assert.IsTrue(msg, gets > 0);
			Assert.IsTrue(msg, gets <= len);
			Assert.IsTrue(msg, gets <= ints.size() - index);

			for (int i = 0; i < arr.Length; ++i)
			{
			  string m = msg + ", i=" + i;
			  if (i >= off && i < off + gets)
			  {
				Assert.AreEqual(m, ints.get(i - off + index), arr[i]);
			  }
			  else
			  {
				Assert.AreEqual(m, 0, arr[i]);
			  }
			}
		  }
		}
	  }

	  public virtual void TestBulkSet()
	  {
		const int valueCount = 1111;
		int index = random().Next(valueCount);
		int len = TestUtil.Next(random(), 1, valueCount * 2);
		int off = random().Next(77);
		long[] arr = new long[off + len];

		for (int bpv = 1; bpv <= 64; ++bpv)
		{
		  long mask = PackedInts.maxValue(bpv);
		  IList<PackedInts.Mutable> packedInts = CreatePackedInts(valueCount, bpv);
		  for (int i = 0; i < arr.Length; ++i)
		  {
			arr[i] = (31L * i + 19) & mask;
		  }

		  foreach (PackedInts.Mutable ints in packedInts)
		  {
			string msg = ints.GetType().Name + " valueCount=" + valueCount + ", index=" + index + ", len=" + len + ", off=" + off;
			int sets = ints.set(index, arr, off, len);
			Assert.IsTrue(msg, sets > 0);
			Assert.IsTrue(msg, sets <= len);

			for (int i = 0; i < ints.size(); ++i)
			{
			  string m = msg + ", i=" + i;
			  if (i >= index && i < index + sets)
			  {
				Assert.AreEqual(m, arr[off - index + i], ints.get(i));
			  }
			  else
			  {
				Assert.AreEqual(m, 0, ints.get(i));
			  }
			}
		  }
		}
	  }

	  public virtual void TestCopy()
	  {
		int valueCount = TestUtil.Next(random(), 5, 600);
		int off1 = random().Next(valueCount);
		int off2 = random().Next(valueCount);
		int len = random().Next(Math.Min(valueCount - off1, valueCount - off2));
		int mem = random().Next(1024);

		for (int bpv = 1; bpv <= 64; ++bpv)
		{
		  long mask = PackedInts.maxValue(bpv);
		  foreach (PackedInts.Mutable r1 in CreatePackedInts(valueCount, bpv))
		  {
			for (int i = 0; i < r1.size(); ++i)
			{
			  r1.set(i, (31L * i - 1023) & mask);
			}
			foreach (PackedInts.Mutable r2 in CreatePackedInts(valueCount, bpv))
			{
			  string msg = "src=" + r1 + ", dest=" + r2 + ", srcPos=" + off1 + ", destPos=" + off2 + ", len=" + len + ", mem=" + mem;
			  PackedInts.copy(r1, off1, r2, off2, len, mem);
			  for (int i = 0; i < r2.size(); ++i)
			  {
				string m = msg + ", i=" + i;
				if (i >= off2 && i < off2 + len)
				{
				  Assert.AreEqual(m, r1.get(i - off2 + off1), r2.get(i));
				}
				else
				{
				  Assert.AreEqual(m, 0, r2.get(i));
				}
			  }
			}
		  }
		}
	  }

	  public virtual void TestGrowableWriter()
	  {
		int valueCount = 113 + random().Next(1111);
		GrowableWriter wrt = new GrowableWriter(1, valueCount, PackedInts.DEFAULT);
		wrt.set(4, 2);
		wrt.set(7, 10);
		wrt.set(valueCount - 10, 99);
		wrt.set(99, 999);
		wrt.set(valueCount - 1, 1 << 10);
		Assert.AreEqual(1 << 10, wrt.get(valueCount - 1));
		wrt.set(99, (1 << 23) - 1);
		Assert.AreEqual(1 << 10, wrt.get(valueCount - 1));
		wrt.set(1, long.MaxValue);
		wrt.set(2, -3);
		Assert.AreEqual(64, wrt.BitsPerValue);
		Assert.AreEqual(1 << 10, wrt.get(valueCount - 1));
		Assert.AreEqual(long.MaxValue, wrt.get(1));
		Assert.AreEqual(-3L, wrt.get(2));
		Assert.AreEqual(2, wrt.get(4));
		Assert.AreEqual((1 << 23) - 1, wrt.get(99));
		Assert.AreEqual(10, wrt.get(7));
		Assert.AreEqual(99, wrt.get(valueCount - 10));
		Assert.AreEqual(1 << 10, wrt.get(valueCount - 1));
		Assert.AreEqual(RamUsageEstimator.sizeOf(wrt), wrt.ramBytesUsed());
	  }

	  public virtual void TestPagedGrowableWriter()
	  {
		int pageSize = 1 << (TestUtil.Next(random(), 6, 30));
		// supports 0 values?
		PagedGrowableWriter writer = new PagedGrowableWriter(0, pageSize, TestUtil.Next(random(), 1, 64), random().nextFloat());
		Assert.AreEqual(0, writer.size());

		// compare against AppendingDeltaPackedLongBuffer
		AppendingDeltaPackedLongBuffer buf = new AppendingDeltaPackedLongBuffer();
		int size = random().Next(1000000);
		long max = 5;
		for (int i = 0; i < size; ++i)
		{
		  buf.add(TestUtil.nextLong(random(), 0, max));
		  if (rarely())
		  {
			max = PackedInts.maxValue(rarely() ? TestUtil.Next(random(), 0, 63) : TestUtil.Next(random(), 0, 31));
		  }
		}
		writer = new PagedGrowableWriter(size, pageSize, TestUtil.Next(random(), 1, 64), random().nextFloat());
		Assert.AreEqual(size, writer.size());
		for (int i = size - 1; i >= 0; --i)
		{
		  writer.set(i, buf.get(i));
		}
		for (int i = 0; i < size; ++i)
		{
		  Assert.AreEqual(buf.get(i), writer.get(i));
		}

		// test ramBytesUsed
		Assert.AreEqual(RamUsageEstimator.sizeOf(writer), writer.ramBytesUsed(), 8);

		// test copy
		PagedGrowableWriter copy = writer.resize(TestUtil.nextLong(random(), writer.size() / 2, writer.size() * 3 / 2));
		for (long i = 0; i < copy.size(); ++i)
		{
		  if (i < writer.size())
		  {
			Assert.AreEqual(writer.get(i), copy.get(i));
		  }
		  else
		  {
			Assert.AreEqual(0, copy.get(i));
		  }
		}

		// test grow
		PagedGrowableWriter grow = writer.grow(TestUtil.nextLong(random(), writer.size() / 2, writer.size() * 3 / 2));
		for (long i = 0; i < grow.size(); ++i)
		{
		  if (i < writer.size())
		  {
			Assert.AreEqual(writer.get(i), grow.get(i));
		  }
		  else
		  {
			Assert.AreEqual(0, grow.get(i));
		  }
		}
	  }

	  public virtual void TestPagedMutable()
	  {
		int bitsPerValue = TestUtil.Next(random(), 1, 64);
		long max = PackedInts.maxValue(bitsPerValue);
		int pageSize = 1 << (TestUtil.Next(random(), 6, 30));
		// supports 0 values?
		PagedMutable writer = new PagedMutable(0, pageSize, bitsPerValue, random().nextFloat() / 2);
		Assert.AreEqual(0, writer.size());

		// compare against AppendingDeltaPackedLongBuffer
		AppendingDeltaPackedLongBuffer buf = new AppendingDeltaPackedLongBuffer();
		int size = random().Next(1000000);

		for (int i = 0; i < size; ++i)
		{
		  buf.add(bitsPerValue == 64 ? random().nextLong() : TestUtil.nextLong(random(), 0, max));
		}
		writer = new PagedMutable(size, pageSize, bitsPerValue, random().nextFloat());
		Assert.AreEqual(size, writer.size());
		for (int i = size - 1; i >= 0; --i)
		{
		  writer.set(i, buf.get(i));
		}
		for (int i = 0; i < size; ++i)
		{
		  Assert.AreEqual(buf.get(i), writer.get(i));
		}

		// test ramBytesUsed
		Assert.AreEqual(RamUsageEstimator.sizeOf(writer) - RamUsageEstimator.sizeOf(writer.format), writer.ramBytesUsed());

		// test copy
		PagedMutable copy = writer.resize(TestUtil.nextLong(random(), writer.size() / 2, writer.size() * 3 / 2));
		for (long i = 0; i < copy.size(); ++i)
		{
		  if (i < writer.size())
		  {
			Assert.AreEqual(writer.get(i), copy.get(i));
		  }
		  else
		  {
			Assert.AreEqual(0, copy.get(i));
		  }
		}

		// test grow
		PagedMutable grow = writer.grow(TestUtil.nextLong(random(), writer.size() / 2, writer.size() * 3 / 2));
		for (long i = 0; i < grow.size(); ++i)
		{
		  if (i < writer.size())
		  {
			Assert.AreEqual(writer.get(i), grow.get(i));
		  }
		  else
		  {
			Assert.AreEqual(0, grow.get(i));
		  }
		}
	  }

	  // memory hole
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ignore public void testPagedGrowableWriterOverflow()
	  public virtual void TestPagedGrowableWriterOverflow()
	  {
		long size = TestUtil.nextLong(random(), 2 * (long) int.MaxValue, 3 * (long) int.MaxValue);
		int pageSize = 1 << (TestUtil.Next(random(), 16, 30));
		PagedGrowableWriter writer = new PagedGrowableWriter(size, pageSize, 1, random().nextFloat());
		long index = TestUtil.nextLong(random(), (long) int.MaxValue, size - 1);
		writer.set(index, 2);
		Assert.AreEqual(2, writer.get(index));
		for (int i = 0; i < 1000000; ++i)
		{
		  long idx = TestUtil.nextLong(random(), 0, size);
		  if (idx == index)
		  {
			Assert.AreEqual(2, writer.get(idx));
		  }
		  else
		  {
			Assert.AreEqual(0, writer.get(idx));
		  }
		}
	  }

	  public virtual void TestSave()
	  {
		int valueCount = TestUtil.Next(random(), 1, 2048);
		for (int bpv = 1; bpv <= 64; ++bpv)
		{
		  int maxValue = (int) Math.Min(PackedInts.maxValue(31), PackedInts.maxValue(bpv));
		  RAMDirectory directory = new RAMDirectory();
		  IList<PackedInts.Mutable> packedInts = CreatePackedInts(valueCount, bpv);
		  foreach (PackedInts.Mutable mutable in packedInts)
		  {
			for (int i = 0; i < mutable.size(); ++i)
			{
			  mutable.set(i, random().Next(maxValue));
			}

			IndexOutput @out = directory.createOutput("packed-ints.bin", IOContext.DEFAULT);
			mutable.save(@out);
			@out.close();

			IndexInput @in = directory.openInput("packed-ints.bin", IOContext.DEFAULT);
			PackedInts.Reader reader = PackedInts.getReader(@in);
			Assert.AreEqual(mutable.BitsPerValue, reader.BitsPerValue);
			Assert.AreEqual(valueCount, reader.size());
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
			  Assert.AreEqual(mutable.get(i), reader.get(i));
			}
			@in.close();
			directory.deleteFile("packed-ints.bin");
		  }
		  directory.close();
		}
	  }

	  public virtual void TestEncodeDecode()
	  {
		foreach (PackedInts.Format format in PackedInts.Format.values())
		{
		  for (int bpv = 1; bpv <= 64; ++bpv)
		  {
			if (!format.isSupported(bpv))
			{
			  continue;
			}
			string msg = format + " " + bpv;

			PackedInts.Encoder encoder = PackedInts.getEncoder(format, PackedInts.VERSION_CURRENT, bpv);
			PackedInts.Decoder decoder = PackedInts.getDecoder(format, PackedInts.VERSION_CURRENT, bpv);
			int longBlockCount = encoder.longBlockCount();
			int longValueCount = encoder.longValueCount();
			int byteBlockCount = encoder.byteBlockCount();
			int byteValueCount = encoder.byteValueCount();
			Assert.AreEqual(longBlockCount, decoder.longBlockCount());
			Assert.AreEqual(longValueCount, decoder.longValueCount());
			Assert.AreEqual(byteBlockCount, decoder.byteBlockCount());
			Assert.AreEqual(byteValueCount, decoder.byteValueCount());

			int longIterations = random().Next(100);
			int byteIterations = longIterations * longValueCount / byteValueCount;
			Assert.AreEqual(longIterations * longValueCount, byteIterations * byteValueCount);
			int blocksOffset = random().Next(100);
			int valuesOffset = random().Next(100);
			int blocksOffset2 = random().Next(100);
			int blocksLen = longIterations * longBlockCount;

			// 1. generate random inputs
			long[] blocks = new long[blocksOffset + blocksLen];
			for (int i = 0; i < blocks.Length; ++i)
			{
			  blocks[i] = random().nextLong();
			  if (format == PackedInts.Format.PACKED_SINGLE_BLOCK && 64 % bpv != 0)
			  {
				// clear highest bits for packed
				int toClear = 64 % bpv;
				blocks[i] = (int)((uint)(blocks[i] << toClear) >> toClear);
			  }
			}

			// 2. decode
			long[] values = new long[valuesOffset + longIterations * longValueCount];
			decoder.decode(blocks, blocksOffset, values, valuesOffset, longIterations);
			foreach (long value in values)
			{
			  Assert.IsTrue(value <= PackedInts.maxValue(bpv));
			}
			// test decoding to int[]
			int[] intValues;
			if (bpv <= 32)
			{
			  intValues = new int[values.Length];
			  decoder.decode(blocks, blocksOffset, intValues, valuesOffset, longIterations);
			  Assert.IsTrue(Equals(intValues, values));
			}
			else
			{
			  intValues = null;
			}

			// 3. re-encode
			long[] blocks2 = new long[blocksOffset2 + blocksLen];
			encoder.encode(values, valuesOffset, blocks2, blocksOffset2, longIterations);
			assertArrayEquals(msg, Arrays.copyOfRange(blocks, blocksOffset, blocks.Length), Arrays.copyOfRange(blocks2, blocksOffset2, blocks2.Length));
			// test encoding from int[]
			if (bpv <= 32)
			{
			  long[] blocks3 = new long[blocks2.Length];
			  encoder.encode(intValues, valuesOffset, blocks3, blocksOffset2, longIterations);
			  assertArrayEquals(msg, blocks2, blocks3);
			}

			// 4. byte[] decoding
			sbyte[] byteBlocks = new sbyte[8 * blocks.Length];
			ByteBuffer.wrap(byteBlocks).asLongBuffer().put(blocks);
			long[] values2 = new long[valuesOffset + longIterations * longValueCount];
			decoder.decode(byteBlocks, blocksOffset * 8, values2, valuesOffset, byteIterations);
			foreach (long value in values2)
			{
			  Assert.IsTrue(msg, value <= PackedInts.maxValue(bpv));
			}
			assertArrayEquals(msg, values, values2);
			// test decoding to int[]
			if (bpv <= 32)
			{
			  int[] intValues2 = new int[values2.Length];
			  decoder.decode(byteBlocks, blocksOffset * 8, intValues2, valuesOffset, byteIterations);
			  Assert.IsTrue(msg, Equals(intValues2, values2));
			}

			// 5. byte[] encoding
			sbyte[] blocks3 = new sbyte[8 * (blocksOffset2 + blocksLen)];
			encoder.encode(values, valuesOffset, blocks3, 8 * blocksOffset2, byteIterations);
			Assert.AreEqual(msg, LongBuffer.wrap(blocks2), ByteBuffer.wrap(blocks3).asLongBuffer());
			// test encoding from int[]
			if (bpv <= 32)
			{
			  sbyte[] blocks4 = new sbyte[blocks3.Length];
			  encoder.encode(intValues, valuesOffset, blocks4, 8 * blocksOffset2, byteIterations);
			  assertArrayEquals(msg, blocks3, blocks4);
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


	  public virtual void TestAppendingLongBuffer()
	  {

		long[] arr = new long[RandomInts.randomIntBetween(random(), 1, 1000000)];
		float[] ratioOptions = new float[]{PackedInts.DEFAULT, PackedInts.COMPACT, PackedInts.FAST};
		foreach (int bpv in new int[]{0, 1, 63, 64, RandomInts.randomIntBetween(random(), 2, 62)})
		{
		  foreach (DataType dataType in Enum.GetValues(typeof(DataType)))
		  {
			int pageSize = 1 << TestUtil.Next(random(), 6, 20);
			int initialPageCount = TestUtil.Next(random(), 0, 16);
			float acceptableOverheadRatio = ratioOptions[TestUtil.Next(random(), 0, ratioOptions.Length - 1)];
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
				inc = TestUtil.Next(random(), -1000, 1000);
				break;
			  default:
				throw new Exception("added a type and forgot to add it here?");

			}

			if (bpv == 0)
			{
			  arr[0] = random().nextLong();
			  for (int i = 1; i < arr.Length; ++i)
			  {
				arr[i] = arr[i - 1] + inc;
			  }
			}
			else if (bpv == 64)
			{
			  for (int i = 0; i < arr.Length; ++i)
			  {
				arr[i] = random().nextLong();
			  }
			}
			else
			{
			  long minValue = TestUtil.nextLong(random(), long.MinValue, long.MaxValue - PackedInts.maxValue(bpv));
			  for (int i = 0; i < arr.Length; ++i)
			  {
				arr[i] = minValue + inc * i + random().nextLong() & PackedInts.maxValue(bpv); // TestUtil.nextLong is too slow
			  }
			}

			for (int i = 0; i < arr.Length; ++i)
			{
			  buf.add(arr[i]);
			}
			Assert.AreEqual(arr.Length, buf.size());
			if (random().nextBoolean())
			{
			  buf.freeze();
			  if (random().nextBoolean())
			  {
				// Make sure double freeze doesn't break anything
				buf.freeze();
			  }
			}
			Assert.AreEqual(arr.Length, buf.size());

			for (int i = 0; i < arr.Length; ++i)
			{
			  Assert.AreEqual(arr[i], buf.get(i));
			}

			AbstractAppendingLongBuffer.Iterator it = buf.GetEnumerator();
			for (int i = 0; i < arr.Length; ++i)
			{
			  if (random().nextBoolean())
			  {
				Assert.IsTrue(it.hasNext());
			  }
			  Assert.AreEqual(arr[i], it.next());
			}
			Assert.IsFalse(it.hasNext());


			long[] target = new long[arr.Length + 1024]; // check the request for more is OK.
			for (int i = 0; i < arr.Length; i += TestUtil.Next(random(), 0, 10000))
			{
			  int lenToRead = random().Next(buf.pageSize() * 2) + 1;
			  lenToRead = Math.Min(lenToRead, target.Length - i);
			  int lenToCheck = Math.Min(lenToRead, arr.Length - i);
			  int off = i;
			  while (off < arr.Length && lenToRead > 0)
			  {
				int read = buf.get(off, target, off, lenToRead);
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

			long expectedBytesUsed = RamUsageEstimator.sizeOf(buf);
			long computedBytesUsed = buf.ramBytesUsed();
			Assert.AreEqual(expectedBytesUsed, computedBytesUsed);
		  }
		}
	  }

	  public virtual void TestPackedInputOutput()
	  {
		long[] longs = new long[random().Next(8192)];
		int[] bitsPerValues = new int[longs.Length];
		bool[] skip = new bool[longs.Length];
		for (int i = 0; i < longs.Length; ++i)
		{
		  int bpv = RandomInts.randomIntBetween(random(), 1, 64);
		  bitsPerValues[i] = random().nextBoolean() ? bpv : TestUtil.Next(random(), bpv, 64);
		  if (bpv == 64)
		  {
			longs[i] = random().nextLong();
		  }
		  else
		  {
			longs[i] = TestUtil.nextLong(random(), 0, PackedInts.maxValue(bpv));
		  }
		  skip[i] = rarely();
		}

		Directory dir = newDirectory();
		IndexOutput @out = dir.createOutput("out.bin", IOContext.DEFAULT);
		PackedDataOutput pout = new PackedDataOutput(@out);
		long totalBits = 0;
		for (int i = 0; i < longs.Length; ++i)
		{
		  pout.writeLong(longs[i], bitsPerValues[i]);
		  totalBits += bitsPerValues[i];
		  if (skip[i])
		  {
			pout.flush();
			totalBits = 8 * (long) Math.Ceiling((double) totalBits / 8);
		  }
		}
		pout.flush();
		Assert.AreEqual((long) Math.Ceiling((double) totalBits / 8), @out.FilePointer);
		@out.close();
		IndexInput @in = dir.openInput("out.bin", IOContext.READONCE);
		PackedDataInput pin = new PackedDataInput(@in);
		for (int i = 0; i < longs.Length; ++i)
		{
		  Assert.AreEqual("" + i, longs[i], pin.readLong(bitsPerValues[i]));
		  if (skip[i])
		  {
			pin.skipToNextByte();
		  }
		}
		Assert.AreEqual((long) Math.Ceiling((double) totalBits / 8), @in.FilePointer);
		@in.close();
		dir.close();
	  }

	  public virtual void TestBlockPackedReaderWriter()
	  {
		int iters = atLeast(2);
		for (int iter = 0; iter < iters; ++iter)
		{
		  int blockSize = 1 << TestUtil.Next(random(), 6, 18);
		  int valueCount = random().Next(1 << 18);
		  long[] values = new long[valueCount];
		  long minValue = 0;
		  int bpv = 0;
		  for (int i = 0; i < valueCount; ++i)
		  {
			if (i % blockSize == 0)
			{
			  minValue = rarely() ? random().Next(256) : rarely() ? - 5 : random().nextLong();
			  bpv = random().Next(65);
			}
			if (bpv == 0)
			{
			  values[i] = minValue;
			}
			else if (bpv == 64)
			{
			  values[i] = random().nextLong();
			}
			else
			{
			  values[i] = minValue + TestUtil.nextLong(random(), 0, (1L << bpv) - 1);
			}
		  }

		  Directory dir = newDirectory();
		  IndexOutput @out = dir.createOutput("out.bin", IOContext.DEFAULT);
		  BlockPackedWriter writer = new BlockPackedWriter(@out, blockSize);
		  for (int i = 0; i < valueCount; ++i)
		  {
			Assert.AreEqual(i, writer.ord());
			writer.add(values[i]);
		  }
		  Assert.AreEqual(valueCount, writer.ord());
		  writer.finish();
		  Assert.AreEqual(valueCount, writer.ord());
		  long fp = @out.FilePointer;
		  @out.close();

		  IndexInput in1 = dir.openInput("out.bin", IOContext.DEFAULT);
		  sbyte[] buf = new sbyte[(int) fp];
		  in1.readBytes(buf, 0, (int) fp);
		  in1.seek(0L);
		  ByteArrayDataInput in2 = new ByteArrayDataInput(buf);
		  DataInput @in = random().nextBoolean() ? in1 : in2;
		  BlockPackedReaderIterator it = new BlockPackedReaderIterator(@in, PackedInts.VERSION_CURRENT, blockSize, valueCount);
		  for (int i = 0; i < valueCount;)
		  {
			if (random().nextBoolean())
			{
			  Assert.AreEqual("" + i, values[i], it.next());
			  ++i;
			}
			else
			{
			  LongsRef nextValues = it.next(TestUtil.Next(random(), 1, 1024));
			  for (int j = 0; j < nextValues.length; ++j)
			  {
				Assert.AreEqual("" + (i + j), values[i + j], nextValues.longs[nextValues.offset + j]);
			  }
			  i += nextValues.length;
			}
			Assert.AreEqual(i, it.ord());
		  }
		  Assert.AreEqual(fp, @in is ByteArrayDataInput ? ((ByteArrayDataInput) @in).Position : ((IndexInput) @in).FilePointer);
		  try
		  {
			it.next();
			Assert.IsTrue(false);
		  }
		  catch (IOException e)
		  {
			// OK
		  }

		  if (@in is ByteArrayDataInput)
		  {
			((ByteArrayDataInput) @in).Position = 0;
		  }
		  else
		  {
			((IndexInput) @in).seek(0L);
		  }
		  BlockPackedReaderIterator it2 = new BlockPackedReaderIterator(@in, PackedInts.VERSION_CURRENT, blockSize, valueCount);
		  int i = 0;
		  while (true)
		  {
			int skip = TestUtil.Next(random(), 0, valueCount - i);
			it2.skip(skip);
			i += skip;
			Assert.AreEqual(i, it2.ord());
			if (i == valueCount)
			{
			  break;
			}
			else
			{
			  Assert.AreEqual(values[i], it2.next());
			  ++i;
			}
		  }
		  Assert.AreEqual(fp, @in is ByteArrayDataInput ? ((ByteArrayDataInput) @in).Position : ((IndexInput) @in).FilePointer);
		  try
		  {
			it2.skip(1);
			Assert.IsTrue(false);
		  }
		  catch (IOException e)
		  {
			// OK
		  }

		  in1.seek(0L);
		  BlockPackedReader reader = new BlockPackedReader(in1, PackedInts.VERSION_CURRENT, blockSize, valueCount, random().nextBoolean());
		  Assert.AreEqual(in1.FilePointer, in1.length());
		  for (i = 0; i < valueCount; ++i)
		  {
			Assert.AreEqual("i=" + i, values[i], reader.get(i));
		  }
		  in1.close();
		  dir.close();
		}
	  }

	  public virtual void TestMonotonicBlockPackedReaderWriter()
	  {
		int iters = atLeast(2);
		for (int iter = 0; iter < iters; ++iter)
		{
		  int blockSize = 1 << TestUtil.Next(random(), 6, 18);
		  int valueCount = random().Next(1 << 18);
		  long[] values = new long[valueCount];
		  if (valueCount > 0)
		  {
			values[0] = random().nextBoolean() ? random().Next(10) : random().Next(int.MaxValue);
			int maxDelta = random().Next(64);
			for (int i = 1; i < valueCount; ++i)
			{
			  if (random().NextDouble() < 0.1d)
			  {
				maxDelta = random().Next(64);
			  }
			  values[i] = Math.Max(0, values[i - 1] + TestUtil.Next(random(), -16, maxDelta));
			}
		  }

		  Directory dir = newDirectory();
		  IndexOutput @out = dir.createOutput("out.bin", IOContext.DEFAULT);
		  MonotonicBlockPackedWriter writer = new MonotonicBlockPackedWriter(@out, blockSize);
		  for (int i = 0; i < valueCount; ++i)
		  {
			Assert.AreEqual(i, writer.ord());
			writer.add(values[i]);
		  }
		  Assert.AreEqual(valueCount, writer.ord());
		  writer.finish();
		  Assert.AreEqual(valueCount, writer.ord());
		  long fp = @out.FilePointer;
		  @out.close();

		  IndexInput @in = dir.openInput("out.bin", IOContext.DEFAULT);
		  MonotonicBlockPackedReader reader = new MonotonicBlockPackedReader(@in, PackedInts.VERSION_CURRENT, blockSize, valueCount, random().nextBoolean());
		  Assert.AreEqual(fp, @in.FilePointer);
		  for (int i = 0; i < valueCount; ++i)
		  {
			Assert.AreEqual("i=" + i, values[i], reader.get(i));
		  }
		  @in.close();
		  dir.close();
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Nightly public void testBlockReaderOverflow() throws java.io.IOException
	  public virtual void TestBlockReaderOverflow()
	  {
		long valueCount = TestUtil.nextLong(random(), 1L + int.MaxValue, (long) int.MaxValue * 2);
		int blockSize = 1 << TestUtil.Next(random(), 20, 22);
		Directory dir = newDirectory();
		IndexOutput @out = dir.createOutput("out.bin", IOContext.DEFAULT);
		BlockPackedWriter writer = new BlockPackedWriter(@out, blockSize);
		long value = random().Next() & 0xFFFFFFFFL;
		long valueOffset = TestUtil.nextLong(random(), 0, valueCount - 1);
		for (long i = 0; i < valueCount;)
		{
		  Assert.AreEqual(i, writer.ord());
		  if ((i & (blockSize - 1)) == 0 && (i + blockSize < valueOffset || i > valueOffset && i + blockSize < valueCount))
		  {
			writer.addBlockOfZeros();
			i += blockSize;
		  }
		  else if (i == valueOffset)
		  {
			writer.add(value);
			++i;
		  }
		  else
		  {
			writer.add(0);
			++i;
		  }
		}
		writer.finish();
		@out.close();
		IndexInput @in = dir.openInput("out.bin", IOContext.DEFAULT);
		BlockPackedReaderIterator it = new BlockPackedReaderIterator(@in, PackedInts.VERSION_CURRENT, blockSize, valueCount);
		it.skip(valueOffset);
		Assert.AreEqual(value, it.next());
		@in.seek(0L);
		BlockPackedReader reader = new BlockPackedReader(@in, PackedInts.VERSION_CURRENT, blockSize, valueCount, random().nextBoolean());
		Assert.AreEqual(value, reader.get(valueOffset));
		for (int i = 0; i < 5; ++i)
		{
		  long offset = TestUtil.nextLong(random(), 0, valueCount - 1);
		  if (offset == valueOffset)
		  {
			Assert.AreEqual(value, reader.get(offset));
		  }
		  else
		  {
			Assert.AreEqual(0, reader.get(offset));
		  }
		}
		@in.close();
		dir.close();
	  }

	}

}