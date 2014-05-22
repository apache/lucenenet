using System;
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


	using BufferSize = Lucene.Net.Util.OfflineSorter.BufferSize;
	using ByteSequencesWriter = Lucene.Net.Util.OfflineSorter.ByteSequencesWriter;
	using SortInfo = Lucene.Net.Util.OfflineSorter.SortInfo;

	/// <summary>
	/// Tests for on-disk merge sorting.
	/// </summary>
	public class TestOfflineSorter : LuceneTestCase
	{
	  private File TempDir;

	  public override void SetUp()
	  {
		base.setUp();
		TempDir = createTempDir("mergesort");
		TestUtil.rm(TempDir);
		TempDir.mkdirs();
	  }

	  public override void TearDown()
	  {
		if (TempDir != null)
		{
		  TestUtil.rm(TempDir);
		}
		base.tearDown();
	  }

	  public virtual void TestEmpty()
	  {
		CheckSort(new OfflineSorter(), new sbyte [][] {});
	  }

	  public virtual void TestSingleLine()
	  {
		CheckSort(new OfflineSorter(), new sbyte [][] {"Single line only.".getBytes(StandardCharsets.UTF_8)});
	  }

	  public virtual void TestIntermediateMerges()
	  {
		// Sort 20 mb worth of data with 1mb buffer, binary merging.
		OfflineSorter.SortInfo info = CheckSort(new OfflineSorter(OfflineSorter.DEFAULT_COMPARATOR, OfflineSorter.BufferSize.megabytes(1), OfflineSorter.defaultTempDir(), 2), GenerateRandom((int)OfflineSorter.MB * 20));
		Assert.IsTrue(info.mergeRounds > 10);
	  }

	  public virtual void TestSmallRandom()
	  {
		// Sort 20 mb worth of data with 1mb buffer.
		OfflineSorter.SortInfo sortInfo = CheckSort(new OfflineSorter(OfflineSorter.DEFAULT_COMPARATOR, OfflineSorter.BufferSize.megabytes(1), OfflineSorter.defaultTempDir(), OfflineSorter.MAX_TEMPFILES), GenerateRandom((int)OfflineSorter.MB * 20));
		Assert.AreEqual(1, sortInfo.mergeRounds);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Nightly public void testLargerRandom() throws Exception
	  public virtual void TestLargerRandom()
	  {
		// Sort 100MB worth of data with 15mb buffer.
		CheckSort(new OfflineSorter(OfflineSorter.DEFAULT_COMPARATOR, OfflineSorter.BufferSize.megabytes(16), OfflineSorter.defaultTempDir(), OfflineSorter.MAX_TEMPFILES), GenerateRandom((int)OfflineSorter.MB * 100));
	  }

	  private sbyte[][] GenerateRandom(int howMuchData)
	  {
		List<sbyte[]> data = new List<sbyte[]>();
		while (howMuchData > 0)
		{
		  sbyte[] current = new sbyte [random().Next(256)];
		  random().nextBytes(current);
		  data.Add(current);
		  howMuchData -= current.Length;
		}
		sbyte[][] bytes = data.ToArray();
		return bytes;
	  }

	  internal static readonly IComparer<sbyte[]> unsignedByteOrderComparator = new ComparatorAnonymousInnerClassHelper();

	  private class ComparatorAnonymousInnerClassHelper : IComparer<byte[]>
	  {
		  public ComparatorAnonymousInnerClassHelper()
		  {
		  }

		  public virtual int Compare(sbyte[] left, sbyte[] right)
		  {
			int max = Math.Min(left.Length, right.Length);
			for (int i = 0, j = 0; i < max; i++, j++)
			{
			  int diff = (left[i] & 0xff) - (right[j] & 0xff);
			  if (diff != 0)
			  {
				return diff;
			  }
			}
			return left.Length - right.Length;
		  }
	  }
	  /// <summary>
	  /// Check sorting data on an instance of <seealso cref="OfflineSorter"/>.
	  /// </summary>
	  private OfflineSorter.SortInfo CheckSort(OfflineSorter sort, sbyte[][] data)
	  {
		File unsorted = WriteAll("unsorted", data);

		Arrays.sort(data, unsignedByteOrderComparator);
		File golden = WriteAll("golden", data);

		File sorted = new File(TempDir, "sorted");
		OfflineSorter.SortInfo sortInfo = sort.sort(unsorted, sorted);
		//System.out.println("Input size [MB]: " + unsorted.length() / (1024 * 1024));
		//System.out.println(sortInfo);

		AssertFilesIdentical(golden, sorted);
		return sortInfo;
	  }

	  /// <summary>
	  /// Make sure two files are byte-byte identical.
	  /// </summary>
	  private void AssertFilesIdentical(File golden, File sorted)
	  {
		Assert.AreEqual(golden.length(), sorted.length());

		sbyte[] buf1 = new sbyte [64 * 1024];
		sbyte[] buf2 = new sbyte [64 * 1024];
		int len;
		DataInputStream is1 = new DataInputStream(new FileInputStream(golden));
		DataInputStream is2 = new DataInputStream(new FileInputStream(sorted));
		while ((len = is1.read(buf1)) > 0)
		{
		  is2.readFully(buf2, 0, len);
		  for (int i = 0; i < len; i++)
		  {
			Assert.AreEqual(buf1[i], buf2[i]);
		  }
		}
		IOUtils.close(is1, is2);
	  }

	  private File WriteAll(string name, sbyte[][] data)
	  {
		File file = new File(TempDir, name);
		OfflineSorter.ByteSequencesWriter w = new OfflineSorter.ByteSequencesWriter(file);
		foreach (sbyte [] datum in data)
		{
		  w.write(datum);
		}
		w.close();
		return file;
	  }

	  public virtual void TestRamBuffer()
	  {
		int numIters = atLeast(10000);
		for (int i = 0; i < numIters; i++)
		{
		  OfflineSorter.BufferSize.megabytes(1 + random().Next(2047));
		}
		OfflineSorter.BufferSize.megabytes(2047);
		OfflineSorter.BufferSize.megabytes(1);

		try
		{
		  OfflineSorter.BufferSize.megabytes(2048);
		  Assert.Fail("max mb is 2047");
		}
		catch (System.ArgumentException e)
		{
		}

		try
		{
		  OfflineSorter.BufferSize.megabytes(0);
		  Assert.Fail("min mb is 0.5");
		}
		catch (System.ArgumentException e)
		{
		}

		try
		{
		  OfflineSorter.BufferSize.megabytes(-1);
		  Assert.Fail("min mb is 0.5");
		}
		catch (System.ArgumentException e)
		{
		}
	  }
	}

}