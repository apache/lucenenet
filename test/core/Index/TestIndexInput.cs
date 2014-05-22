using System;

namespace Lucene.Net.Index
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

	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using ByteArrayDataInput = Lucene.Net.Store.ByteArrayDataInput;
	using ByteArrayDataOutput = Lucene.Net.Store.ByteArrayDataOutput;
	using DataInput = Lucene.Net.Store.DataInput;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;

	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;


	public class TestIndexInput : LuceneTestCase
	{

	  internal static readonly sbyte[] READ_TEST_BYTES = new sbyte[] {unchecked((sbyte) 0x80), 0x01, unchecked((sbyte) 0xFF), 0x7F, unchecked((sbyte) 0x80), unchecked((sbyte) 0x80), 0x01, unchecked((sbyte) 0x81), unchecked((sbyte) 0x80), 0x01, unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), (sbyte) 0x07, unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), (sbyte) 0x0F, unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), (sbyte) 0x07, unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), (sbyte) 0x7F, 0x06, 'L', 'u', 'c', 'e', 'n', 'e', 0x02, unchecked((sbyte) 0xC2), unchecked((sbyte) 0xBF), 0x0A, 'L', 'u', unchecked((sbyte) 0xC2), unchecked((sbyte) 0xBF), 'c', 'e', unchecked((sbyte) 0xC2), unchecked((sbyte) 0xBF), 'n', 'e', 0x03, unchecked((sbyte) 0xE2), unchecked((sbyte) 0x98), unchecked((sbyte) 0xA0), 0x0C, 'L', 'u', unchecked((sbyte) 0xE2), unchecked((sbyte) 0x98), unchecked((sbyte) 0xA0), 'c', 'e', unchecked((sbyte) 0xE2), unchecked((sbyte) 0x98), unchecked((sbyte) 0xA0), 'n', 'e', 0x04, unchecked((sbyte) 0xF0), unchecked((sbyte) 0x9D), unchecked((sbyte) 0x84), unchecked((sbyte) 0x9E), 0x08, unchecked((sbyte) 0xF0), unchecked((sbyte) 0x9D), unchecked((sbyte) 0x84), unchecked((sbyte) 0x9E), unchecked((sbyte) 0xF0), unchecked((sbyte) 0x9D), unchecked((sbyte) 0x85), unchecked((sbyte) 0xA0), 0x0E, 'L', 'u', unchecked((sbyte) 0xF0), unchecked((sbyte) 0x9D), unchecked((sbyte) 0x84), unchecked((sbyte) 0x9E), 'c', 'e', unchecked((sbyte) 0xF0), unchecked((sbyte) 0x9D), unchecked((sbyte) 0x85), unchecked((sbyte) 0xA0), 'n', 'e', 0x01, 0x00, 0x08, 'L', 'u', 0x00, 'c', 'e', 0x00, 'n', 'e', unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), (sbyte) 0x17, (sbyte) 0x01, unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), unchecked((sbyte) 0xFF), (sbyte) 0x01};

	  internal static readonly int COUNT = RANDOM_MULTIPLIER * 65536;
	  internal static int[] INTS;
	  internal static long[] LONGS;
	  internal static sbyte[] RANDOM_TEST_BYTES;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws java.io.IOException
	  public static void BeforeClass()
	  {
		Random random = random();
		INTS = new int[COUNT];
		LONGS = new long[COUNT];
		RANDOM_TEST_BYTES = new sbyte[COUNT * (5 + 4 + 9 + 8)];
		ByteArrayDataOutput bdo = new ByteArrayDataOutput(RANDOM_TEST_BYTES);
		for (int i = 0; i < COUNT; i++)
		{
		  int i1 = INTS[i] = random.Next();
		  bdo.writeVInt(i1);
		  bdo.writeInt(i1);

		  long l1;
		  if (rarely())
		  {
			// a long with lots of zeroes at the end
			l1 = LONGS[i] = TestUtil.nextLong(random, 0, int.MaxValue) << 32;
		  }
		  else
		  {
			l1 = LONGS[i] = TestUtil.nextLong(random, 0, long.MaxValue);
		  }
		  bdo.writeVLong(l1);
		  bdo.writeLong(l1);
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass()
	  public static void AfterClass()
	  {
		INTS = null;
		LONGS = null;
		RANDOM_TEST_BYTES = null;
	  }

	  private void CheckReads(DataInput @is, Type expectedEx)
	  {
		Assert.AreEqual(128,@is.readVInt());
		Assert.AreEqual(16383,@is.readVInt());
		Assert.AreEqual(16384,@is.readVInt());
		Assert.AreEqual(16385,@is.readVInt());
		Assert.AreEqual(int.MaxValue, @is.readVInt());
		Assert.AreEqual(-1, @is.readVInt());
		Assert.AreEqual((long) int.MaxValue, @is.readVLong());
		Assert.AreEqual(long.MaxValue, @is.readVLong());
		Assert.AreEqual("Lucene",@is.readString());

		Assert.AreEqual("\u00BF",@is.readString());
		Assert.AreEqual("Lu\u00BFce\u00BFne",@is.readString());

		Assert.AreEqual("\u2620",@is.readString());
		Assert.AreEqual("Lu\u2620ce\u2620ne",@is.readString());

		Assert.AreEqual("\uD834\uDD1E",@is.readString());
		Assert.AreEqual("\uD834\uDD1E\uD834\uDD60",@is.readString());
		Assert.AreEqual("Lu\uD834\uDD1Ece\uD834\uDD60ne",@is.readString());

		Assert.AreEqual("\u0000",@is.readString());
		Assert.AreEqual("Lu\u0000ce\u0000ne",@is.readString());

		try
		{
		  @is.readVInt();
		  Assert.Fail("Should throw " + expectedEx.Name);
		}
		catch (Exception e)
		{
		  Assert.IsTrue(e.Message.StartsWith("Invalid vInt"));
		  Assert.IsTrue(expectedEx.IsInstanceOfType(e));
		}
		Assert.AreEqual(1, @is.readVInt()); // guard value

		try
		{
		  @is.readVLong();
		  Assert.Fail("Should throw " + expectedEx.Name);
		}
		catch (Exception e)
		{
		  Assert.IsTrue(e.Message.StartsWith("Invalid vLong"));
		  Assert.IsTrue(expectedEx.IsInstanceOfType(e));
		}
		Assert.AreEqual(1L, @is.readVLong()); // guard value
	  }

	  private void CheckRandomReads(DataInput @is)
	  {
		for (int i = 0; i < COUNT; i++)
		{
		  Assert.AreEqual(INTS[i], @is.readVInt());
		  Assert.AreEqual(INTS[i], @is.readInt());
		  Assert.AreEqual(LONGS[i], @is.readVLong());
		  Assert.AreEqual(LONGS[i], @is.readLong());
		}
	  }

	  // this test only checks BufferedIndexInput because MockIndexInput extends BufferedIndexInput
	  public virtual void TestBufferedIndexInputRead()
	  {
		IndexInput @is = new MockIndexInput(READ_TEST_BYTES);
		CheckReads(@is, typeof(IOException));
		@is.close();
		@is = new MockIndexInput(RANDOM_TEST_BYTES);
		CheckRandomReads(@is);
		@is.close();
	  }

	  // this test checks the raw IndexInput methods as it uses RAMIndexInput which extends IndexInput directly
	  public virtual void TestRawIndexInputRead()
	  {
		Random random = random();
		RAMDirectory dir = new RAMDirectory();
		IndexOutput os = dir.createOutput("foo", newIOContext(random));
		os.writeBytes(READ_TEST_BYTES, READ_TEST_BYTES.Length);
		os.close();
		IndexInput @is = dir.openInput("foo", newIOContext(random));
		CheckReads(@is, typeof(IOException));
		@is.close();

		os = dir.createOutput("bar", newIOContext(random));
		os.writeBytes(RANDOM_TEST_BYTES, RANDOM_TEST_BYTES.Length);
		os.close();
		@is = dir.openInput("bar", newIOContext(random));
		CheckRandomReads(@is);
		@is.close();
		dir.close();
	  }

	  public virtual void TestByteArrayDataInput()
	  {
		ByteArrayDataInput @is = new ByteArrayDataInput(READ_TEST_BYTES);
		CheckReads(@is, typeof(Exception));
		@is = new ByteArrayDataInput(RANDOM_TEST_BYTES);
		CheckRandomReads(@is);
	  }

	}

}