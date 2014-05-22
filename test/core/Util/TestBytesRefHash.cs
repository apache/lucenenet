using System;
using System.Collections;
using System.Collections.Generic;

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
	using Before = org.junit.Before;
	using Test = org.junit.Test;

	public class TestBytesRefHash : LuceneTestCase
	{

	  internal BytesRefHash Hash;
	  internal ByteBlockPool Pool;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Override @Before public void setUp() throws Exception
	  public override void SetUp()
	  {
		base.setUp();
		Pool = NewPool();
		Hash = NewHash(Pool);
	  }

	  private ByteBlockPool NewPool()
	  {
		return random().nextBoolean() && Pool != null ? Pool : new ByteBlockPool(new RecyclingByteBlockAllocator(ByteBlockPool.BYTE_BLOCK_SIZE, random().Next(25)));
	  }

	  private BytesRefHash NewHash(ByteBlockPool blockPool)
	  {
		int initSize = 2 << 1 + random().Next(5);
		return random().nextBoolean() ? new BytesRefHash(blockPool) : new BytesRefHash(blockPool, initSize, new BytesRefHash.DirectBytesStartArray(initSize));
	  }

	  /// <summary>
	  /// Test method for <seealso cref="Lucene.Net.Util.BytesRefHash#size()"/>.
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSize()
	  public virtual void TestSize()
	  {
		BytesRef @ref = new BytesRef();
		int num = atLeast(2);
		for (int j = 0; j < num; j++)
		{
		  int mod = 1 + random().Next(39);
		  for (int i = 0; i < 797; i++)
		  {
			string str;
			do
			{
			  str = TestUtil.randomRealisticUnicodeString(random(), 1000);
			} while (str.Length == 0);
			@ref.copyChars(str);
			int count = Hash.size();
			int key = Hash.add(@ref);
			if (key < 0)
			{
			  Assert.AreEqual(Hash.size(), count);
			}
			else
			{
			  Assert.AreEqual(Hash.size(), count + 1);
			}
			if (i % mod == 0)
			{
			  Hash.clear();
			  Assert.AreEqual(0, Hash.size());
			  Hash.reinit();
			}
		  }
		}
	  }

	  /// <summary>
	  /// Test method for
	  /// <seealso cref="Lucene.Net.Util.BytesRefHash#get(int, BytesRef)"/>
	  /// .
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testGet()
	  public virtual void TestGet()
	  {
		BytesRef @ref = new BytesRef();
		BytesRef scratch = new BytesRef();
		int num = atLeast(2);
		for (int j = 0; j < num; j++)
		{
		  IDictionary<string, int?> strings = new Dictionary<string, int?>();
		  int uniqueCount = 0;
		  for (int i = 0; i < 797; i++)
		  {
			string str;
			do
			{
			  str = TestUtil.randomRealisticUnicodeString(random(), 1000);
			} while (str.Length == 0);
			@ref.copyChars(str);
			int count = Hash.size();
			int key = Hash.add(@ref);
			if (key >= 0)
			{
			  assertNull(strings.put(str, Convert.ToInt32(key)));
			  Assert.AreEqual(uniqueCount, key);
			  uniqueCount++;
			  Assert.AreEqual(Hash.size(), count + 1);
			}
			else
			{
			  Assert.IsTrue((-key) - 1 < count);
			  Assert.AreEqual(Hash.size(), count);
			}
		  }
		  foreach (KeyValuePair<string, int?> entry in strings)
		  {
			@ref.copyChars(entry.Key);
			Assert.AreEqual(@ref, Hash.get((int)entry.Value, scratch));
		  }
		  Hash.clear();
		  Assert.AreEqual(0, Hash.size());
		  Hash.reinit();
		}
	  }

	  /// <summary>
	  /// Test method for <seealso cref="Lucene.Net.Util.BytesRefHash#compact()"/>.
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testCompact()
	  public virtual void TestCompact()
	  {
		BytesRef @ref = new BytesRef();
		int num = atLeast(2);
		for (int j = 0; j < num; j++)
		{
		  int numEntries = 0;
		  const int size = 797;
		  BitArray bits = new BitArray(size);
		  for (int i = 0; i < size; i++)
		  {
			string str;
			do
			{
			  str = TestUtil.randomRealisticUnicodeString(random(), 1000);
			} while (str.Length == 0);
			@ref.copyChars(str);
			int key = Hash.add(@ref);
			if (key < 0)
			{
			  Assert.IsTrue(bits.Get((-key) - 1));
			}
			else
			{
			  Assert.IsFalse(bits.Get(key));
			  bits.Set(key, true);
			  numEntries++;
			}
		  }
		  Assert.AreEqual(Hash.size(), bits.cardinality());
		  Assert.AreEqual(numEntries, bits.cardinality());
		  Assert.AreEqual(numEntries, Hash.size());
		  int[] compact = Hash.compact();
		  Assert.IsTrue(numEntries < compact.Length);
		  for (int i = 0; i < numEntries; i++)
		  {
			bits.Set(compact[i], false);
		  }
		  Assert.AreEqual(0, bits.cardinality());
		  Hash.clear();
		  Assert.AreEqual(0, Hash.size());
		  Hash.reinit();
		}
	  }

	  /// <summary>
	  /// Test method for
	  /// <seealso cref="Lucene.Net.Util.BytesRefHash#sort(java.util.Comparator)"/>.
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSort()
	  public virtual void TestSort()
	  {
		BytesRef @ref = new BytesRef();
		int num = atLeast(2);
		for (int j = 0; j < num; j++)
		{
		  SortedSet<string> strings = new SortedSet<string>();
		  for (int i = 0; i < 797; i++)
		  {
			string str;
			do
			{
			  str = TestUtil.randomRealisticUnicodeString(random(), 1000);
			} while (str.Length == 0);
			@ref.copyChars(str);
			Hash.add(@ref);
			strings.add(str);
		  }
		  // We use the UTF-16 comparator here, because we need to be able to
		  // compare to native String.compareTo() [UTF-16]:
		  int[] sort = Hash.sort(BytesRef.UTF8SortedAsUTF16Comparator);
		  Assert.IsTrue(strings.size() < sort.Length);
		  int i = 0;
		  BytesRef scratch = new BytesRef();
		  foreach (string @string in strings)
		  {
			@ref.copyChars(@string);
			Assert.AreEqual(@ref, Hash.get(sort[i++], scratch));
		  }
		  Hash.clear();
		  Assert.AreEqual(0, Hash.size());
		  Hash.reinit();

		}
	  }

	  /// <summary>
	  /// Test method for
	  /// <seealso cref="Lucene.Net.Util.BytesRefHash#add(Lucene.Net.Util.BytesRef)"/>
	  /// .
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAdd()
	  public virtual void TestAdd()
	  {
		BytesRef @ref = new BytesRef();
		BytesRef scratch = new BytesRef();
		int num = atLeast(2);
		for (int j = 0; j < num; j++)
		{
		  Set<string> strings = new HashSet<string>();
		  int uniqueCount = 0;
		  for (int i = 0; i < 797; i++)
		  {
			string str;
			do
			{
			  str = TestUtil.randomRealisticUnicodeString(random(), 1000);
			} while (str.Length == 0);
			@ref.copyChars(str);
			int count = Hash.size();
			int key = Hash.add(@ref);

			if (key >= 0)
			{
			  Assert.IsTrue(strings.add(str));
			  Assert.AreEqual(uniqueCount, key);
			  Assert.AreEqual(Hash.size(), count + 1);
			  uniqueCount++;
			}
			else
			{
			  Assert.IsFalse(strings.add(str));
			  Assert.IsTrue((-key) - 1 < count);
			  Assert.AreEqual(str, Hash.get((-key) - 1, scratch).utf8ToString());
			  Assert.AreEqual(count, Hash.size());
			}
		  }

		  AssertAllIn(strings, Hash);
		  Hash.clear();
		  Assert.AreEqual(0, Hash.size());
		  Hash.reinit();
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFind() throws Exception
	  public virtual void TestFind()
	  {
		BytesRef @ref = new BytesRef();
		BytesRef scratch = new BytesRef();
		int num = atLeast(2);
		for (int j = 0; j < num; j++)
		{
		  Set<string> strings = new HashSet<string>();
		  int uniqueCount = 0;
		  for (int i = 0; i < 797; i++)
		  {
			string str;
			do
			{
			  str = TestUtil.randomRealisticUnicodeString(random(), 1000);
			} while (str.Length == 0);
			@ref.copyChars(str);
			int count = Hash.size();
			int key = Hash.find(@ref); //hash.add(ref);
			if (key >= 0) // string found in hash
			{
			  Assert.IsFalse(strings.add(str));
			  Assert.IsTrue(key < count);
			  Assert.AreEqual(str, Hash.get(key, scratch).utf8ToString());
			  Assert.AreEqual(count, Hash.size());
			}
			else
			{
			  key = Hash.add(@ref);
			  Assert.IsTrue(strings.add(str));
			  Assert.AreEqual(uniqueCount, key);
			  Assert.AreEqual(Hash.size(), count + 1);
			  uniqueCount++;
			}
		  }

		  AssertAllIn(strings, Hash);
		  Hash.clear();
		  Assert.AreEqual(0, Hash.size());
		  Hash.reinit();
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = Lucene.Net.Util.BytesRefHash.MaxBytesLengthExceededException.class) public void testLargeValue()
	  public virtual void TestLargeValue()
	  {
		int[] sizes = new int[] {random().Next(5), ByteBlockPool.BYTE_BLOCK_SIZE - 33 + random().Next(31), ByteBlockPool.BYTE_BLOCK_SIZE - 1 + random().Next(37)};
		BytesRef @ref = new BytesRef();
		for (int i = 0; i < sizes.Length; i++)
		{
		  @ref.bytes = new sbyte[sizes[i]];
		  @ref.offset = 0;
		  @ref.length = sizes[i];
		  try
		  {
			Assert.AreEqual(i, Hash.add(@ref));
		  }
		  catch (MaxBytesLengthExceededException e)
		  {
			if (i < sizes.Length - 1)
			{
			  Assert.Fail("unexpected exception at size: " + sizes[i]);
			}
			throw e;
		  }
		}
	  }

	  /// <summary>
	  /// Test method for
	  /// <seealso cref="Lucene.Net.Util.BytesRefHash#addByPoolOffset(int)"/>
	  /// .
	  /// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAddByPoolOffset()
	  public virtual void TestAddByPoolOffset()
	  {
		BytesRef @ref = new BytesRef();
		BytesRef scratch = new BytesRef();
		BytesRefHash offsetHash = NewHash(Pool);
		int num = atLeast(2);
		for (int j = 0; j < num; j++)
		{
		  Set<string> strings = new HashSet<string>();
		  int uniqueCount = 0;
		  for (int i = 0; i < 797; i++)
		  {
			string str;
			do
			{
			  str = TestUtil.randomRealisticUnicodeString(random(), 1000);
			} while (str.Length == 0);
			@ref.copyChars(str);
			int count = Hash.size();
			int key = Hash.add(@ref);

			if (key >= 0)
			{
			  Assert.IsTrue(strings.add(str));
			  Assert.AreEqual(uniqueCount, key);
			  Assert.AreEqual(Hash.size(), count + 1);
			  int offsetKey = offsetHash.addByPoolOffset(Hash.byteStart(key));
			  Assert.AreEqual(uniqueCount, offsetKey);
			  Assert.AreEqual(offsetHash.size(), count + 1);
			  uniqueCount++;
			}
			else
			{
			  Assert.IsFalse(strings.add(str));
			  Assert.IsTrue((-key) - 1 < count);
			  Assert.AreEqual(str, Hash.get((-key) - 1, scratch).utf8ToString());
			  Assert.AreEqual(count, Hash.size());
			  int offsetKey = offsetHash.addByPoolOffset(Hash.byteStart((-key) - 1));
			  Assert.IsTrue((-offsetKey) - 1 < count);
			  Assert.AreEqual(str, Hash.get((-offsetKey) - 1, scratch).utf8ToString());
			  Assert.AreEqual(count, Hash.size());
			}
		  }

		  AssertAllIn(strings, Hash);
		  foreach (string @string in strings)
		  {
			@ref.copyChars(@string);
			int key = Hash.add(@ref);
			BytesRef bytesRef = offsetHash.get((-key) - 1, scratch);
			Assert.AreEqual(@ref, bytesRef);
		  }

		  Hash.clear();
		  Assert.AreEqual(0, Hash.size());
		  offsetHash.clear();
		  Assert.AreEqual(0, offsetHash.size());
		  Hash.reinit(); // init for the next round
		  offsetHash.reinit();
		}
	  }

	  private void AssertAllIn(Set<string> strings, BytesRefHash hash)
	  {
		BytesRef @ref = new BytesRef();
		BytesRef scratch = new BytesRef();
		int count = hash.size();
		foreach (string @string in strings)
		{
		  @ref.copyChars(@string);
		  int key = hash.add(@ref); // add again to check duplicates
		  Assert.AreEqual(@string, hash.get((-key) - 1, scratch).utf8ToString());
		  Assert.AreEqual(count, hash.size());
		  Assert.IsTrue("key: " + key + " count: " + count + " string: " + @string, key < count);
		}
	  }


	}

}