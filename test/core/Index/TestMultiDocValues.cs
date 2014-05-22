using System.Collections.Generic;

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

	using BinaryDocValuesField = Lucene.Net.Document.BinaryDocValuesField;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using SortedDocValuesField = Lucene.Net.Document.SortedDocValuesField;
	using SortedSetDocValuesField = Lucene.Net.Document.SortedSetDocValuesField;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;

	/// <summary>
	/// Tests MultiDocValues versus ordinary segment merging </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs("Lucene3x") public class TestMultiDocValues extends Lucene.Net.Util.LuceneTestCase
	public class TestMultiDocValues : LuceneTestCase
	{

	  public virtual void TestNumerics()
	  {
		Directory dir = newDirectory();
		Document doc = new Document();
		Field field = new NumericDocValuesField("numbers", 0);
		doc.add(field);

		IndexWriterConfig iwc = newIndexWriterConfig(random(), TEST_VERSION_CURRENT, null);
		iwc.MergePolicy = newLogMergePolicy();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, iwc);

		int numDocs = atLeast(500);
		for (int i = 0; i < numDocs; i++)
		{
		  field.LongValue = random().nextLong();
		  iw.addDocument(doc);
		  if (random().Next(17) == 0)
		  {
			iw.commit();
		  }
		}
		DirectoryReader ir = iw.Reader;
		iw.forceMerge(1);
		DirectoryReader ir2 = iw.Reader;
		AtomicReader merged = getOnlySegmentReader(ir2);
		iw.close();

		NumericDocValues multi = MultiDocValues.getNumericValues(ir, "numbers");
		NumericDocValues single = merged.getNumericDocValues("numbers");
		for (int i = 0; i < numDocs; i++)
		{
		  Assert.AreEqual(single.get(i), multi.get(i));
		}
		ir.close();
		ir2.close();
		dir.close();
	  }

	  public virtual void TestBinary()
	  {
		Directory dir = newDirectory();
		Document doc = new Document();
		BytesRef @ref = new BytesRef();
		Field field = new BinaryDocValuesField("bytes", @ref);
		doc.add(field);

		IndexWriterConfig iwc = newIndexWriterConfig(random(), TEST_VERSION_CURRENT, null);
		iwc.MergePolicy = newLogMergePolicy();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, iwc);

		int numDocs = atLeast(500);
		for (int i = 0; i < numDocs; i++)
		{
		  @ref.copyChars(TestUtil.randomUnicodeString(random()));
		  iw.addDocument(doc);
		  if (random().Next(17) == 0)
		  {
			iw.commit();
		  }
		}
		DirectoryReader ir = iw.Reader;
		iw.forceMerge(1);
		DirectoryReader ir2 = iw.Reader;
		AtomicReader merged = getOnlySegmentReader(ir2);
		iw.close();

		BinaryDocValues multi = MultiDocValues.getBinaryValues(ir, "bytes");
		BinaryDocValues single = merged.getBinaryDocValues("bytes");
		BytesRef actual = new BytesRef();
		BytesRef expected = new BytesRef();
		for (int i = 0; i < numDocs; i++)
		{
		  single.get(i, expected);
		  multi.get(i, actual);
		  Assert.AreEqual(expected, actual);
		}
		ir.close();
		ir2.close();
		dir.close();
	  }

	  public virtual void TestSorted()
	  {
		Directory dir = newDirectory();
		Document doc = new Document();
		BytesRef @ref = new BytesRef();
		Field field = new SortedDocValuesField("bytes", @ref);
		doc.add(field);

		IndexWriterConfig iwc = newIndexWriterConfig(random(), TEST_VERSION_CURRENT, null);
		iwc.MergePolicy = newLogMergePolicy();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, iwc);

		int numDocs = atLeast(500);
		for (int i = 0; i < numDocs; i++)
		{
		  @ref.copyChars(TestUtil.randomUnicodeString(random()));
		  if (defaultCodecSupportsDocsWithField() && random().Next(7) == 0)
		  {
			iw.addDocument(new Document());
		  }
		  iw.addDocument(doc);
		  if (random().Next(17) == 0)
		  {
			iw.commit();
		  }
		}
		DirectoryReader ir = iw.Reader;
		iw.forceMerge(1);
		DirectoryReader ir2 = iw.Reader;
		AtomicReader merged = getOnlySegmentReader(ir2);
		iw.close();

		SortedDocValues multi = MultiDocValues.getSortedValues(ir, "bytes");
		SortedDocValues single = merged.getSortedDocValues("bytes");
		Assert.AreEqual(single.ValueCount, multi.ValueCount);
		BytesRef actual = new BytesRef();
		BytesRef expected = new BytesRef();
		for (int i = 0; i < numDocs; i++)
		{
		  // check ord
		  Assert.AreEqual(single.getOrd(i), multi.getOrd(i));
		  // check value
		  single.get(i, expected);
		  multi.get(i, actual);
		  Assert.AreEqual(expected, actual);
		}
		ir.close();
		ir2.close();
		dir.close();
	  }

	  // tries to make more dups than testSorted
	  public virtual void TestSortedWithLotsOfDups()
	  {
		Directory dir = newDirectory();
		Document doc = new Document();
		BytesRef @ref = new BytesRef();
		Field field = new SortedDocValuesField("bytes", @ref);
		doc.add(field);

		IndexWriterConfig iwc = newIndexWriterConfig(random(), TEST_VERSION_CURRENT, null);
		iwc.MergePolicy = newLogMergePolicy();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, iwc);

		int numDocs = atLeast(500);
		for (int i = 0; i < numDocs; i++)
		{
		  @ref.copyChars(TestUtil.randomSimpleString(random(), 2));
		  iw.addDocument(doc);
		  if (random().Next(17) == 0)
		  {
			iw.commit();
		  }
		}
		DirectoryReader ir = iw.Reader;
		iw.forceMerge(1);
		DirectoryReader ir2 = iw.Reader;
		AtomicReader merged = getOnlySegmentReader(ir2);
		iw.close();

		SortedDocValues multi = MultiDocValues.getSortedValues(ir, "bytes");
		SortedDocValues single = merged.getSortedDocValues("bytes");
		Assert.AreEqual(single.ValueCount, multi.ValueCount);
		BytesRef actual = new BytesRef();
		BytesRef expected = new BytesRef();
		for (int i = 0; i < numDocs; i++)
		{
		  // check ord
		  Assert.AreEqual(single.getOrd(i), multi.getOrd(i));
		  // check ord value
		  single.get(i, expected);
		  multi.get(i, actual);
		  Assert.AreEqual(expected, actual);
		}
		ir.close();
		ir2.close();
		dir.close();
	  }

	  public virtual void TestSortedSet()
	  {
		assumeTrue("codec does not support SORTED_SET", defaultCodecSupportsSortedSet());
		Directory dir = newDirectory();

		IndexWriterConfig iwc = newIndexWriterConfig(random(), TEST_VERSION_CURRENT, null);
		iwc.MergePolicy = newLogMergePolicy();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, iwc);

		int numDocs = atLeast(500);
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  int numValues = random().Next(5);
		  for (int j = 0; j < numValues; j++)
		  {
			doc.add(new SortedSetDocValuesField("bytes", new BytesRef(TestUtil.randomUnicodeString(random()))));
		  }
		  iw.addDocument(doc);
		  if (random().Next(17) == 0)
		  {
			iw.commit();
		  }
		}
		DirectoryReader ir = iw.Reader;
		iw.forceMerge(1);
		DirectoryReader ir2 = iw.Reader;
		AtomicReader merged = getOnlySegmentReader(ir2);
		iw.close();

		SortedSetDocValues multi = MultiDocValues.getSortedSetValues(ir, "bytes");
		SortedSetDocValues single = merged.getSortedSetDocValues("bytes");
		if (multi == null)
		{
		  assertNull(single);
		}
		else
		{
		  Assert.AreEqual(single.ValueCount, multi.ValueCount);
		  BytesRef actual = new BytesRef();
		  BytesRef expected = new BytesRef();
		  // check values
		  for (long i = 0; i < single.ValueCount; i++)
		  {
			single.lookupOrd(i, expected);
			multi.lookupOrd(i, actual);
			Assert.AreEqual(expected, actual);
		  }
		  // check ord list
		  for (int i = 0; i < numDocs; i++)
		  {
			single.Document = i;
			List<long?> expectedList = new List<long?>();
			long ord;
			while ((ord = single.nextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
			{
			  expectedList.Add(ord);
			}

			multi.Document = i;
			int upto = 0;
			while ((ord = multi.nextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
			{
			  Assert.AreEqual((long)expectedList[upto], ord);
			  upto++;
			}
			Assert.AreEqual(expectedList.Count, upto);
		  }
		}

		ir.close();
		ir2.close();
		dir.close();
	  }

	  // tries to make more dups than testSortedSet
	  public virtual void TestSortedSetWithDups()
	  {
		assumeTrue("codec does not support SORTED_SET", defaultCodecSupportsSortedSet());
		Directory dir = newDirectory();

		IndexWriterConfig iwc = newIndexWriterConfig(random(), TEST_VERSION_CURRENT, null);
		iwc.MergePolicy = newLogMergePolicy();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, iwc);

		int numDocs = atLeast(500);
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  int numValues = random().Next(5);
		  for (int j = 0; j < numValues; j++)
		  {
			doc.add(new SortedSetDocValuesField("bytes", new BytesRef(TestUtil.randomSimpleString(random(), 2))));
		  }
		  iw.addDocument(doc);
		  if (random().Next(17) == 0)
		  {
			iw.commit();
		  }
		}
		DirectoryReader ir = iw.Reader;
		iw.forceMerge(1);
		DirectoryReader ir2 = iw.Reader;
		AtomicReader merged = getOnlySegmentReader(ir2);
		iw.close();

		SortedSetDocValues multi = MultiDocValues.getSortedSetValues(ir, "bytes");
		SortedSetDocValues single = merged.getSortedSetDocValues("bytes");
		if (multi == null)
		{
		  assertNull(single);
		}
		else
		{
		  Assert.AreEqual(single.ValueCount, multi.ValueCount);
		  BytesRef actual = new BytesRef();
		  BytesRef expected = new BytesRef();
		  // check values
		  for (long i = 0; i < single.ValueCount; i++)
		  {
			single.lookupOrd(i, expected);
			multi.lookupOrd(i, actual);
			Assert.AreEqual(expected, actual);
		  }
		  // check ord list
		  for (int i = 0; i < numDocs; i++)
		  {
			single.Document = i;
			List<long?> expectedList = new List<long?>();
			long ord;
			while ((ord = single.nextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
			{
			  expectedList.Add(ord);
			}

			multi.Document = i;
			int upto = 0;
			while ((ord = multi.nextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
			{
			  Assert.AreEqual((long)expectedList[upto], ord);
			  upto++;
			}
			Assert.AreEqual(expectedList.Count, upto);
		  }
		}

		ir.close();
		ir2.close();
		dir.close();
	  }

	  public virtual void TestDocsWithField()
	  {
		assumeTrue("codec does not support docsWithField", defaultCodecSupportsDocsWithField());
		Directory dir = newDirectory();

		IndexWriterConfig iwc = newIndexWriterConfig(random(), TEST_VERSION_CURRENT, null);
		iwc.MergePolicy = newLogMergePolicy();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, iwc);

		int numDocs = atLeast(500);
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  if (random().Next(4) >= 0)
		  {
			doc.add(new NumericDocValuesField("numbers", random().nextLong()));
		  }
		  doc.add(new NumericDocValuesField("numbersAlways", random().nextLong()));
		  iw.addDocument(doc);
		  if (random().Next(17) == 0)
		  {
			iw.commit();
		  }
		}
		DirectoryReader ir = iw.Reader;
		iw.forceMerge(1);
		DirectoryReader ir2 = iw.Reader;
		AtomicReader merged = getOnlySegmentReader(ir2);
		iw.close();

		Bits multi = MultiDocValues.getDocsWithField(ir, "numbers");
		Bits single = merged.getDocsWithField("numbers");
		if (multi == null)
		{
		  assertNull(single);
		}
		else
		{
		  Assert.AreEqual(single.length(), multi.length());
		  for (int i = 0; i < numDocs; i++)
		  {
			Assert.AreEqual(single.get(i), multi.get(i));
		  }
		}

		multi = MultiDocValues.getDocsWithField(ir, "numbersAlways");
		single = merged.getDocsWithField("numbersAlways");
		Assert.AreEqual(single.length(), multi.length());
		for (int i = 0; i < numDocs; i++)
		{
		  Assert.AreEqual(single.get(i), multi.get(i));
		}
		ir.close();
		ir2.close();
		dir.close();
	  }
	}

}