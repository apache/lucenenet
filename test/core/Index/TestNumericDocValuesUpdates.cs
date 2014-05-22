using System;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Index
{


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using Codec = Lucene.Net.Codecs.Codec;
	using DocValuesFormat = Lucene.Net.Codecs.DocValuesFormat;
	using AssertingDocValuesFormat = Lucene.Net.Codecs.asserting.AssertingDocValuesFormat;
	using Lucene40RWCodec = Lucene.Net.Codecs.Lucene40.Lucene40RWCodec;
	using Lucene41RWCodec = Lucene.Net.Codecs.Lucene41.Lucene41RWCodec;
	using Lucene42RWCodec = Lucene.Net.Codecs.Lucene42.Lucene42RWCodec;
	using Lucene45DocValuesFormat = Lucene.Net.Codecs.Lucene45.Lucene45DocValuesFormat;
	using Lucene45RWCodec = Lucene.Net.Codecs.Lucene45.Lucene45RWCodec;
	using Lucene46Codec = Lucene.Net.Codecs.lucene46.Lucene46Codec;
	using BinaryDocValuesField = Lucene.Net.Document.BinaryDocValuesField;
	using Document = Lucene.Net.Document.Document;
	using Store = Lucene.Net.Document.Field.Store;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using SortedDocValuesField = Lucene.Net.Document.SortedDocValuesField;
	using SortedSetDocValuesField = Lucene.Net.Document.SortedSetDocValuesField;
	using StringField = Lucene.Net.Document.StringField;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using Test = org.junit.Test;

	using RandomPicks = com.carrotsearch.randomizedtesting.generators.RandomPicks;

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

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({"Appending","Lucene3x","Lucene40","Lucene41","Lucene42","Lucene45"}) public class TestNumericDocValuesUpdates extends Lucene.Net.Util.LuceneTestCase
	public class TestNumericDocValuesUpdates : LuceneTestCase
	{

	  private Document Doc(int id)
	  {
		Document doc = new Document();
		doc.add(new StringField("id", "doc-" + id, Store.NO));
		// make sure we don't set the doc's value to 0, to not confuse with a document that's missing values
		doc.add(new NumericDocValuesField("val", id + 1));
		return doc;
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdatesAreFlushed() throws java.io.IOException
	  public virtual void TestUpdatesAreFlushed()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)).setRAMBufferSizeMB(0.00000001));
		writer.addDocument(Doc(0)); // val=1
		writer.addDocument(Doc(1)); // val=2
		writer.addDocument(Doc(3)); // val=2
		writer.commit();
		Assert.AreEqual(1, writer.FlushDeletesCount);
		writer.updateNumericDocValue(new Term("id", "doc-0"), "val", 5L);
		Assert.AreEqual(2, writer.FlushDeletesCount);
		writer.updateNumericDocValue(new Term("id", "doc-1"), "val", 6L);
		Assert.AreEqual(3, writer.FlushDeletesCount);
		writer.updateNumericDocValue(new Term("id", "doc-2"), "val", 7L);
		Assert.AreEqual(4, writer.FlushDeletesCount);
		writer.Config.RAMBufferSizeMB = 1000d;
		writer.updateNumericDocValue(new Term("id", "doc-2"), "val", 7L);
		Assert.AreEqual(4, writer.FlushDeletesCount);
		writer.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSimple() throws Exception
	  public virtual void TestSimple()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		// make sure random config doesn't flush on us
		conf.MaxBufferedDocs = 10;
		conf.RAMBufferSizeMB = IndexWriterConfig.DISABLE_AUTO_FLUSH;
		IndexWriter writer = new IndexWriter(dir, conf);
		writer.addDocument(Doc(0)); // val=1
		writer.addDocument(Doc(1)); // val=2
		if (random().nextBoolean()) // randomly commit before the update is sent
		{
		  writer.commit();
		}
		writer.updateNumericDocValue(new Term("id", "doc-0"), "val", 2L); // doc=0, exp=2

		DirectoryReader reader;
		if (random().nextBoolean()) // not NRT
		{
		  writer.close();
		  reader = DirectoryReader.open(dir);
		} // NRT
		else
		{
		  reader = DirectoryReader.open(writer, true);
		  writer.close();
		}

		Assert.AreEqual(1, reader.leaves().size());
		AtomicReader r = reader.leaves().get(0).reader();
		NumericDocValues ndv = r.getNumericDocValues("val");
		Assert.AreEqual(2, ndv.get(0));
		Assert.AreEqual(2, ndv.get(1));
		reader.close();

		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdateFewSegments() throws Exception
	  public virtual void TestUpdateFewSegments()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.MaxBufferedDocs = 2; // generate few segments
		conf.MergePolicy = NoMergePolicy.COMPOUND_FILES; // prevent merges for this test
		IndexWriter writer = new IndexWriter(dir, conf);
		int numDocs = 10;
		long[] expectedValues = new long[numDocs];
		for (int i = 0; i < numDocs; i++)
		{
		  writer.addDocument(Doc(i));
		  expectedValues[i] = i + 1;
		}
		writer.commit();

		// update few docs
		for (int i = 0; i < numDocs; i++)
		{
		  if (random().NextDouble() < 0.4)
		  {
			long value = (i + 1) * 2;
			writer.updateNumericDocValue(new Term("id", "doc-" + i), "val", value);
			expectedValues[i] = value;
		  }
		}

		DirectoryReader reader;
		if (random().nextBoolean()) // not NRT
		{
		  writer.close();
		  reader = DirectoryReader.open(dir);
		} // NRT
		else
		{
		  reader = DirectoryReader.open(writer, true);
		  writer.close();
		}

		foreach (AtomicReaderContext context in reader.leaves())
		{
		  AtomicReader r = context.reader();
		  NumericDocValues ndv = r.getNumericDocValues("val");
		  Assert.IsNotNull(ndv);
		  for (int i = 0; i < r.maxDoc(); i++)
		  {
			long expected = expectedValues[i + context.docBase];
			long actual = ndv.get(i);
			Assert.AreEqual(expected, actual);
		  }
		}

		reader.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testReopen() throws Exception
	  public virtual void TestReopen()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);
		writer.addDocument(Doc(0));
		writer.addDocument(Doc(1));

		bool isNRT = random().nextBoolean();
		DirectoryReader reader1;
		if (isNRT)
		{
		  reader1 = DirectoryReader.open(writer, true);
		}
		else
		{
		  writer.commit();
		  reader1 = DirectoryReader.open(dir);
		}

		// update doc
		writer.updateNumericDocValue(new Term("id", "doc-0"), "val", 10L); // update doc-0's value to 10
		if (!isNRT)
		{
		  writer.commit();
		}

		// reopen reader and assert only it sees the update
		DirectoryReader reader2 = DirectoryReader.openIfChanged(reader1);
		Assert.IsNotNull(reader2);
		Assert.IsTrue(reader1 != reader2);

		Assert.AreEqual(1, reader1.leaves().get(0).reader().getNumericDocValues("val").get(0));
		Assert.AreEqual(10, reader2.leaves().get(0).reader().getNumericDocValues("val").get(0));

		IOUtils.close(writer, reader1, reader2, dir);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdatesAndDeletes() throws Exception
	  public virtual void TestUpdatesAndDeletes()
	  {
		// create an index with a segment with only deletes, a segment with both
		// deletes and updates and a segment with only updates
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.MaxBufferedDocs = 10; // control segment flushing
		conf.MergePolicy = NoMergePolicy.COMPOUND_FILES; // prevent merges for this test
		IndexWriter writer = new IndexWriter(dir, conf);

		for (int i = 0; i < 6; i++)
		{
		  writer.addDocument(Doc(i));
		  if (i % 2 == 1)
		  {
			writer.commit(); // create 2-docs segments
		  }
		}

		// delete doc-1 and doc-2
		writer.deleteDocuments(new Term("id", "doc-1"), new Term("id", "doc-2")); // 1st and 2nd segments

		// update docs 3 and 5
		writer.updateNumericDocValue(new Term("id", "doc-3"), "val", 17L);
		writer.updateNumericDocValue(new Term("id", "doc-5"), "val", 17L);

		DirectoryReader reader;
		if (random().nextBoolean()) // not NRT
		{
		  writer.close();
		  reader = DirectoryReader.open(dir);
		} // NRT
		else
		{
		  reader = DirectoryReader.open(writer, true);
		  writer.close();
		}

		AtomicReader slow = SlowCompositeReaderWrapper.wrap(reader);

		Bits liveDocs = slow.LiveDocs;
		bool[] expectedLiveDocs = new bool[] {true, false, false, true, true, true};
		for (int i = 0; i < expectedLiveDocs.Length; i++)
		{
		  Assert.AreEqual(expectedLiveDocs[i], liveDocs.get(i));
		}

		long[] expectedValues = new long[] {1, 2, 3, 17, 5, 17};
		NumericDocValues ndv = slow.getNumericDocValues("val");
		for (int i = 0; i < expectedValues.Length; i++)
		{
		  Assert.AreEqual(expectedValues[i], ndv.get(i));
		}

		reader.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdatesWithDeletes() throws Exception
	  public virtual void TestUpdatesWithDeletes()
	  {
		// update and delete different documents in the same commit session
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.MaxBufferedDocs = 10; // control segment flushing
		IndexWriter writer = new IndexWriter(dir, conf);

		writer.addDocument(Doc(0));
		writer.addDocument(Doc(1));

		if (random().nextBoolean())
		{
		  writer.commit();
		}

		writer.deleteDocuments(new Term("id", "doc-0"));
		writer.updateNumericDocValue(new Term("id", "doc-1"), "val", 17L);

		DirectoryReader reader;
		if (random().nextBoolean()) // not NRT
		{
		  writer.close();
		  reader = DirectoryReader.open(dir);
		} // NRT
		else
		{
		  reader = DirectoryReader.open(writer, true);
		  writer.close();
		}

		AtomicReader r = reader.leaves().get(0).reader();
		Assert.IsFalse(r.LiveDocs.get(0));
		Assert.AreEqual(17, r.getNumericDocValues("val").get(1));

		reader.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdateAndDeleteSameDocument() throws Exception
	  public virtual void TestUpdateAndDeleteSameDocument()
	  {
		// update and delete same document in same commit session
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.MaxBufferedDocs = 10; // control segment flushing
		IndexWriter writer = new IndexWriter(dir, conf);

		writer.addDocument(Doc(0));
		writer.addDocument(Doc(1));

		if (random().nextBoolean())
		{
		  writer.commit();
		}

		writer.deleteDocuments(new Term("id", "doc-0"));
		writer.updateNumericDocValue(new Term("id", "doc-0"), "val", 17L);

		DirectoryReader reader;
		if (random().nextBoolean()) // not NRT
		{
		  writer.close();
		  reader = DirectoryReader.open(dir);
		} // NRT
		else
		{
		  reader = DirectoryReader.open(writer, true);
		  writer.close();
		}

		AtomicReader r = reader.leaves().get(0).reader();
		Assert.IsFalse(r.LiveDocs.get(0));
		Assert.AreEqual(1, r.getNumericDocValues("val").get(0)); // deletes are currently applied first

		reader.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testMultipleDocValuesTypes() throws Exception
	  public virtual void TestMultipleDocValuesTypes()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.MaxBufferedDocs = 10; // prevent merges
		IndexWriter writer = new IndexWriter(dir, conf);

		for (int i = 0; i < 4; i++)
		{
		  Document doc = new Document();
		  doc.add(new StringField("dvUpdateKey", "dv", Store.NO));
		  doc.add(new NumericDocValuesField("ndv", i));
		  doc.add(new BinaryDocValuesField("bdv", new BytesRef(Convert.ToString(i))));
		  doc.add(new SortedDocValuesField("sdv", new BytesRef(Convert.ToString(i))));
		  doc.add(new SortedSetDocValuesField("ssdv", new BytesRef(Convert.ToString(i))));
		  doc.add(new SortedSetDocValuesField("ssdv", new BytesRef(Convert.ToString(i * 2))));
		  writer.addDocument(doc);
		}
		writer.commit();

		// update all docs' ndv field
		writer.updateNumericDocValue(new Term("dvUpdateKey", "dv"), "ndv", 17L);
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		AtomicReader r = reader.leaves().get(0).reader();
		NumericDocValues ndv = r.getNumericDocValues("ndv");
		BinaryDocValues bdv = r.getBinaryDocValues("bdv");
		SortedDocValues sdv = r.getSortedDocValues("sdv");
		SortedSetDocValues ssdv = r.getSortedSetDocValues("ssdv");
		BytesRef scratch = new BytesRef();
		for (int i = 0; i < r.maxDoc(); i++)
		{
		  Assert.AreEqual(17, ndv.get(i));
		  bdv.get(i, scratch);
		  Assert.AreEqual(new BytesRef(Convert.ToString(i)), scratch);
		  sdv.get(i, scratch);
		  Assert.AreEqual(new BytesRef(Convert.ToString(i)), scratch);
		  ssdv.Document = i;
		  long ord = ssdv.nextOrd();
		  ssdv.lookupOrd(ord, scratch);
		  Assert.AreEqual(i, Convert.ToInt32(scratch.utf8ToString()));
		  if (i != 0)
		  {
			ord = ssdv.nextOrd();
			ssdv.lookupOrd(ord, scratch);
			Assert.AreEqual(i * 2, Convert.ToInt32(scratch.utf8ToString()));
		  }
		  Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, ssdv.nextOrd());
		}

		reader.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testMultipleNumericDocValues() throws Exception
	  public virtual void TestMultipleNumericDocValues()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.MaxBufferedDocs = 10; // prevent merges
		IndexWriter writer = new IndexWriter(dir, conf);

		for (int i = 0; i < 2; i++)
		{
		  Document doc = new Document();
		  doc.add(new StringField("dvUpdateKey", "dv", Store.NO));
		  doc.add(new NumericDocValuesField("ndv1", i));
		  doc.add(new NumericDocValuesField("ndv2", i));
		  writer.addDocument(doc);
		}
		writer.commit();

		// update all docs' ndv1 field
		writer.updateNumericDocValue(new Term("dvUpdateKey", "dv"), "ndv1", 17L);
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		AtomicReader r = reader.leaves().get(0).reader();
		NumericDocValues ndv1 = r.getNumericDocValues("ndv1");
		NumericDocValues ndv2 = r.getNumericDocValues("ndv2");
		for (int i = 0; i < r.maxDoc(); i++)
		{
		  Assert.AreEqual(17, ndv1.get(i));
		  Assert.AreEqual(i, ndv2.get(i));
		}

		reader.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testDocumentWithNoValue() throws Exception
	  public virtual void TestDocumentWithNoValue()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);

		for (int i = 0; i < 2; i++)
		{
		  Document doc = new Document();
		  doc.add(new StringField("dvUpdateKey", "dv", Store.NO));
		  if (i == 0) // index only one document with value
		  {
			doc.add(new NumericDocValuesField("ndv", 5));
		  }
		  writer.addDocument(doc);
		}
		writer.commit();

		// update all docs' ndv field
		writer.updateNumericDocValue(new Term("dvUpdateKey", "dv"), "ndv", 17L);
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		AtomicReader r = reader.leaves().get(0).reader();
		NumericDocValues ndv = r.getNumericDocValues("ndv");
		for (int i = 0; i < r.maxDoc(); i++)
		{
		  Assert.AreEqual(17, ndv.get(i));
		}

		reader.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUnsetValue() throws Exception
	  public virtual void TestUnsetValue()
	  {
		assumeTrue("codec does not support docsWithField", defaultCodecSupportsDocsWithField());
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);

		for (int i = 0; i < 2; i++)
		{
		  Document doc = new Document();
		  doc.add(new StringField("id", "doc" + i, Store.NO));
		  doc.add(new NumericDocValuesField("ndv", 5));
		  writer.addDocument(doc);
		}
		writer.commit();

		// unset the value of 'doc0'
		writer.updateNumericDocValue(new Term("id", "doc0"), "ndv", null);
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		AtomicReader r = reader.leaves().get(0).reader();
		NumericDocValues ndv = r.getNumericDocValues("ndv");
		for (int i = 0; i < r.maxDoc(); i++)
		{
		  if (i == 0)
		  {
			Assert.AreEqual(0, ndv.get(i));
		  }
		  else
		  {
			Assert.AreEqual(5, ndv.get(i));
		  }
		}

		Bits docsWithField = r.getDocsWithField("ndv");
		Assert.IsFalse(docsWithField.get(0));
		Assert.IsTrue(docsWithField.get(1));

		reader.close();
		dir.close();
	  }

	  public virtual void TestUnsetAllValues()
	  {
		assumeTrue("codec does not support docsWithField", defaultCodecSupportsDocsWithField());
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);

		for (int i = 0; i < 2; i++)
		{
		  Document doc = new Document();
		  doc.add(new StringField("id", "doc", Store.NO));
		  doc.add(new NumericDocValuesField("ndv", 5));
		  writer.addDocument(doc);
		}
		writer.commit();

		// unset the value of 'doc'
		writer.updateNumericDocValue(new Term("id", "doc"), "ndv", null);
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		AtomicReader r = reader.leaves().get(0).reader();
		NumericDocValues ndv = r.getNumericDocValues("ndv");
		for (int i = 0; i < r.maxDoc(); i++)
		{
		  Assert.AreEqual(0, ndv.get(i));
		}

		Bits docsWithField = r.getDocsWithField("ndv");
		Assert.IsFalse(docsWithField.get(0));
		Assert.IsFalse(docsWithField.get(1));

		reader.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdateNonNumericDocValuesField() throws Exception
	  public virtual void TestUpdateNonNumericDocValuesField()
	  {
		// we don't support adding new fields or updating existing non-numeric-dv
		// fields through numeric updates
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);

		Document doc = new Document();
		doc.add(new StringField("key", "doc", Store.NO));
		doc.add(new StringField("foo", "bar", Store.NO));
		writer.addDocument(doc); // flushed document
		writer.commit();
		writer.addDocument(doc); // in-memory document

		try
		{
		  writer.updateNumericDocValue(new Term("key", "doc"), "ndv", 17L);
		  Assert.Fail("should not have allowed creating new fields through update");
		}
		catch (System.ArgumentException e)
		{
		  // ok
		}

		try
		{
		  writer.updateNumericDocValue(new Term("key", "doc"), "foo", 17L);
		  Assert.Fail("should not have allowed updating an existing field to numeric-dv");
		}
		catch (System.ArgumentException e)
		{
		  // ok
		}

		writer.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testDifferentDVFormatPerField() throws Exception
	  public virtual void TestDifferentDVFormatPerField()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.Codec = new Lucene46CodecAnonymousInnerClassHelper(this);
		IndexWriter writer = new IndexWriter(dir, conf);

		Document doc = new Document();
		doc.add(new StringField("key", "doc", Store.NO));
		doc.add(new NumericDocValuesField("ndv", 5));
		doc.add(new SortedDocValuesField("sorted", new BytesRef("value")));
		writer.addDocument(doc); // flushed document
		writer.commit();
		writer.addDocument(doc); // in-memory document

		writer.updateNumericDocValue(new Term("key", "doc"), "ndv", 17L);
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);

		AtomicReader r = SlowCompositeReaderWrapper.wrap(reader);
		NumericDocValues ndv = r.getNumericDocValues("ndv");
		SortedDocValues sdv = r.getSortedDocValues("sorted");
		BytesRef scratch = new BytesRef();
		for (int i = 0; i < r.maxDoc(); i++)
		{
		  Assert.AreEqual(17, ndv.get(i));
		  sdv.get(i, scratch);
		  Assert.AreEqual(new BytesRef("value"), scratch);
		}

		reader.close();
		dir.close();
	  }

	  private class Lucene46CodecAnonymousInnerClassHelper : Lucene46Codec
	  {
		  private readonly TestNumericDocValuesUpdates OuterInstance;

		  public Lucene46CodecAnonymousInnerClassHelper(TestNumericDocValuesUpdates outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override DocValuesFormat GetDocValuesFormatForField(string field)
		  {
			return new Lucene45DocValuesFormat();
		  }
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdateSameDocMultipleTimes() throws Exception
	  public virtual void TestUpdateSameDocMultipleTimes()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);

		Document doc = new Document();
		doc.add(new StringField("key", "doc", Store.NO));
		doc.add(new NumericDocValuesField("ndv", 5));
		writer.addDocument(doc); // flushed document
		writer.commit();
		writer.addDocument(doc); // in-memory document

		writer.updateNumericDocValue(new Term("key", "doc"), "ndv", 17L); // update existing field
		writer.updateNumericDocValue(new Term("key", "doc"), "ndv", 3L); // update existing field 2nd time in this commit
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		AtomicReader r = SlowCompositeReaderWrapper.wrap(reader);
		NumericDocValues ndv = r.getNumericDocValues("ndv");
		for (int i = 0; i < r.maxDoc(); i++)
		{
		  Assert.AreEqual(3, ndv.get(i));
		}
		reader.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSegmentMerges() throws Exception
	  public virtual void TestSegmentMerges()
	  {
		Directory dir = newDirectory();
		Random random = random();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random));
		IndexWriter writer = new IndexWriter(dir, conf.clone());

		int docid = 0;
		int numRounds = atLeast(10);
		for (int rnd = 0; rnd < numRounds; rnd++)
		{
		  Document doc = new Document();
		  doc.add(new StringField("key", "doc", Store.NO));
		  doc.add(new NumericDocValuesField("ndv", -1));
		  int numDocs = atLeast(30);
		  for (int i = 0; i < numDocs; i++)
		  {
			doc.removeField("id");
			doc.add(new StringField("id", Convert.ToString(docid++), Store.NO));
			writer.addDocument(doc);
		  }

		  long value = rnd + 1;
		  writer.updateNumericDocValue(new Term("key", "doc"), "ndv", value);

		  if (random.NextDouble() < 0.2) // randomly delete some docs
		  {
			writer.deleteDocuments(new Term("id", Convert.ToString(random.Next(docid))));
		  }

		  // randomly commit or reopen-IW (or nothing), before forceMerge
		  if (random.NextDouble() < 0.4)
		  {
			writer.commit();
		  }
		  else if (random.NextDouble() < 0.1)
		  {
			writer.close();
			writer = new IndexWriter(dir, conf.clone());
		  }

		  // add another document with the current value, to be sure forceMerge has
		  // something to merge (for instance, it could be that CMS finished merging
		  // all segments down to 1 before the delete was applied, so when
		  // forceMerge is called, the index will be with one segment and deletes
		  // and some MPs might now merge it, thereby invalidating test's
		  // assumption that the reader has no deletes).
		  doc = new Document();
		  doc.add(new StringField("id", Convert.ToString(docid++), Store.NO));
		  doc.add(new StringField("key", "doc", Store.NO));
		  doc.add(new NumericDocValuesField("ndv", value));
		  writer.addDocument(doc);

		  writer.forceMerge(1, true);
		  DirectoryReader reader;
		  if (random.nextBoolean())
		  {
			writer.commit();
			reader = DirectoryReader.open(dir);
		  }
		  else
		  {
			reader = DirectoryReader.open(writer, true);
		  }

		  Assert.AreEqual(1, reader.leaves().size());
		  AtomicReader r = reader.leaves().get(0).reader();
		  assertNull("index should have no deletes after forceMerge", r.LiveDocs);
		  NumericDocValues ndv = r.getNumericDocValues("ndv");
		  Assert.IsNotNull(ndv);
		  for (int i = 0; i < r.maxDoc(); i++)
		  {
			Assert.AreEqual(value, ndv.get(i));
		  }
		  reader.close();
		}

		writer.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdateDocumentByMultipleTerms() throws Exception
	  public virtual void TestUpdateDocumentByMultipleTerms()
	  {
		// make sure the order of updates is respected, even when multiple terms affect same document
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);

		Document doc = new Document();
		doc.add(new StringField("k1", "v1", Store.NO));
		doc.add(new StringField("k2", "v2", Store.NO));
		doc.add(new NumericDocValuesField("ndv", 5));
		writer.addDocument(doc); // flushed document
		writer.commit();
		writer.addDocument(doc); // in-memory document

		writer.updateNumericDocValue(new Term("k1", "v1"), "ndv", 17L);
		writer.updateNumericDocValue(new Term("k2", "v2"), "ndv", 3L);
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		AtomicReader r = SlowCompositeReaderWrapper.wrap(reader);
		NumericDocValues ndv = r.getNumericDocValues("ndv");
		for (int i = 0; i < r.maxDoc(); i++)
		{
		  Assert.AreEqual(3, ndv.get(i));
		}
		reader.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testManyReopensAndFields() throws Exception
	  public virtual void TestManyReopensAndFields()
	  {
		Directory dir = newDirectory();
		Random random = random();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random));
		LogMergePolicy lmp = newLogMergePolicy();
		lmp.MergeFactor = 3; // merge often
		conf.MergePolicy = lmp;
		IndexWriter writer = new IndexWriter(dir, conf);

		bool isNRT = random.nextBoolean();
		DirectoryReader reader;
		if (isNRT)
		{
		  reader = DirectoryReader.open(writer, true);
		}
		else
		{
		  writer.commit();
		  reader = DirectoryReader.open(dir);
		}

		int numFields = random.Next(4) + 3; // 3-7
		long[] fieldValues = new long[numFields];
		bool[] fieldHasValue = new bool[numFields];
		Arrays.fill(fieldHasValue, true);
		for (int i = 0; i < fieldValues.Length; i++)
		{
		  fieldValues[i] = 1;
		}

		int numRounds = atLeast(15);
		int docID = 0;
		for (int i = 0; i < numRounds; i++)
		{
		  int numDocs = atLeast(5);
	//      System.out.println("[" + Thread.currentThread().getName() + "]: round=" + i + ", numDocs=" + numDocs);
		  for (int j = 0; j < numDocs; j++)
		  {
			Document doc = new Document();
			doc.add(new StringField("id", "doc-" + docID, Store.NO));
			doc.add(new StringField("key", "all", Store.NO)); // update key
			// add all fields with their current value
			for (int f = 0; f < fieldValues.Length; f++)
			{
			  doc.add(new NumericDocValuesField("f" + f, fieldValues[f]));
			}
			writer.addDocument(doc);
			++docID;
		  }

		  // if field's value was unset before, unset it from all new added documents too
		  for (int field = 0; field < fieldHasValue.Length; field++)
		  {
			if (!fieldHasValue[field])
			{
			  writer.updateNumericDocValue(new Term("key", "all"), "f" + field, null);
			}
		  }

		  int fieldIdx = random.Next(fieldValues.Length);
		  string updateField = "f" + fieldIdx;
		  if (random.nextBoolean())
		  {
	//        System.out.println("[" + Thread.currentThread().getName() + "]: unset field '" + updateField + "'");
			fieldHasValue[fieldIdx] = false;
			writer.updateNumericDocValue(new Term("key", "all"), updateField, null);
		  }
		  else
		  {
			fieldHasValue[fieldIdx] = true;
			writer.updateNumericDocValue(new Term("key", "all"), updateField, ++fieldValues[fieldIdx]);
	//        System.out.println("[" + Thread.currentThread().getName() + "]: updated field '" + updateField + "' to value " + fieldValues[fieldIdx]);
		  }

		  if (random.NextDouble() < 0.2)
		  {
			int deleteDoc = random.Next(docID); // might also delete an already deleted document, ok!
			writer.deleteDocuments(new Term("id", "doc-" + deleteDoc));
	//        System.out.println("[" + Thread.currentThread().getName() + "]: deleted document: doc-" + deleteDoc);
		  }

		  // verify reader
		  if (!isNRT)
		  {
			writer.commit();
		  }

	//      System.out.println("[" + Thread.currentThread().getName() + "]: reopen reader: " + reader);
		  DirectoryReader newReader = DirectoryReader.openIfChanged(reader);
		  Assert.IsNotNull(newReader);
		  reader.close();
		  reader = newReader;
	//      System.out.println("[" + Thread.currentThread().getName() + "]: reopened reader: " + reader);
		  Assert.IsTrue(reader.numDocs() > 0); // we delete at most one document per round
		  foreach (AtomicReaderContext context in reader.leaves())
		  {
			AtomicReader r = context.reader();
	//        System.out.println(((SegmentReader) r).getSegmentName());
			Bits liveDocs = r.LiveDocs;
			for (int field = 0; field < fieldValues.Length; field++)
			{
			  string f = "f" + field;
			  NumericDocValues ndv = r.getNumericDocValues(f);
			  Bits docsWithField = r.getDocsWithField(f);
			  Assert.IsNotNull(ndv);
			  int maxDoc = r.maxDoc();
			  for (int doc = 0; doc < maxDoc; doc++)
			  {
				if (liveDocs == null || liveDocs.get(doc))
				{
	//              System.out.println("doc=" + (doc + context.docBase) + " f='" + f + "' vslue=" + ndv.get(doc));
				  if (fieldHasValue[field])
				  {
					Assert.IsTrue(docsWithField.get(doc));
					Assert.AreEqual("invalid value for doc=" + doc + ", field=" + f + ", reader=" + r, fieldValues[field], ndv.get(doc));
				  }
				  else
				  {
					Assert.IsFalse(docsWithField.get(doc));
				  }
				}
			  }
			}
		  }
	//      System.out.println();
		}

		IOUtils.close(writer, reader, dir);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdateSegmentWithNoDocValues() throws Exception
	  public virtual void TestUpdateSegmentWithNoDocValues()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		// prevent merges, otherwise by the time updates are applied
		// (writer.close()), the segments might have merged and that update becomes
		// legit.
		conf.MergePolicy = NoMergePolicy.COMPOUND_FILES;
		IndexWriter writer = new IndexWriter(dir, conf);

		// first segment with NDV
		Document doc = new Document();
		doc.add(new StringField("id", "doc0", Store.NO));
		doc.add(new NumericDocValuesField("ndv", 3));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new StringField("id", "doc4", Store.NO)); // document without 'ndv' field
		writer.addDocument(doc);
		writer.commit();

		// second segment with no NDV
		doc = new Document();
		doc.add(new StringField("id", "doc1", Store.NO));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new StringField("id", "doc2", Store.NO)); // document that isn't updated
		writer.addDocument(doc);
		writer.commit();

		// update document in the first segment - should not affect docsWithField of
		// the document without NDV field
		writer.updateNumericDocValue(new Term("id", "doc0"), "ndv", 5L);

		// update document in the second segment - field should be added and we should
		// be able to handle the other document correctly (e.g. no NPE)
		writer.updateNumericDocValue(new Term("id", "doc1"), "ndv", 5L);
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		foreach (AtomicReaderContext context in reader.leaves())
		{
		  AtomicReader r = context.reader();
		  NumericDocValues ndv = r.getNumericDocValues("ndv");
		  Bits docsWithField = r.getDocsWithField("ndv");
		  Assert.IsNotNull(docsWithField);
		  Assert.IsTrue(docsWithField.get(0));
		  Assert.AreEqual(5L, ndv.get(0));
		  Assert.IsFalse(docsWithField.get(1));
		  Assert.AreEqual(0L, ndv.get(1));
		}
		reader.close();

		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdateSegmentWithPostingButNoDocValues() throws Exception
	  public virtual void TestUpdateSegmentWithPostingButNoDocValues()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		// prevent merges, otherwise by the time updates are applied
		// (writer.close()), the segments might have merged and that update becomes
		// legit.
		conf.MergePolicy = NoMergePolicy.COMPOUND_FILES;
		IndexWriter writer = new IndexWriter(dir, conf);

		// first segment with NDV
		Document doc = new Document();
		doc.add(new StringField("id", "doc0", Store.NO));
		doc.add(new StringField("ndv", "mock-value", Store.NO));
		doc.add(new NumericDocValuesField("ndv", 5));
		writer.addDocument(doc);
		writer.commit();

		// second segment with no NDV
		doc = new Document();
		doc.add(new StringField("id", "doc1", Store.NO));
		doc.add(new StringField("ndv", "mock-value", Store.NO));
		writer.addDocument(doc);
		writer.commit();

		// update document in the second segment
		writer.updateNumericDocValue(new Term("id", "doc1"), "ndv", 5L);
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		foreach (AtomicReaderContext context in reader.leaves())
		{
		  AtomicReader r = context.reader();
		  NumericDocValues ndv = r.getNumericDocValues("ndv");
		  for (int i = 0; i < r.maxDoc(); i++)
		  {
			Assert.AreEqual(5L, ndv.get(i));
		  }
		}
		reader.close();

		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdateNumericDVFieldWithSameNameAsPostingField() throws Exception
	  public virtual void TestUpdateNumericDVFieldWithSameNameAsPostingField()
	  {
		// this used to fail because FieldInfos.Builder neglected to update
		// globalFieldMaps.docValueTypes map
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);

		Document doc = new Document();
		doc.add(new StringField("f", "mock-value", Store.NO));
		doc.add(new NumericDocValuesField("f", 5));
		writer.addDocument(doc);
		writer.commit();
		writer.updateNumericDocValue(new Term("f", "mock-value"), "f", 17L);
		writer.close();

		DirectoryReader r = DirectoryReader.open(dir);
		NumericDocValues ndv = r.leaves().get(0).reader().getNumericDocValues("f");
		Assert.AreEqual(17, ndv.get(0));
		r.close();

		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdateOldSegments() throws Exception
	  public virtual void TestUpdateOldSegments()
	  {
		Codec[] oldCodecs = new Codec[] {new Lucene40RWCodec(), new Lucene41RWCodec(), new Lucene42RWCodec(), new Lucene45RWCodec()};
		Directory dir = newDirectory();

		bool oldValue = OLD_FORMAT_IMPERSONATION_IS_ACTIVE;
		// create a segment with an old Codec
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.Codec = oldCodecs[random().Next(oldCodecs.Length)];
		OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
		IndexWriter writer = new IndexWriter(dir, conf);
		Document doc = new Document();
		doc.add(new StringField("id", "doc", Store.NO));
		doc.add(new NumericDocValuesField("f", 5));
		writer.addDocument(doc);
		writer.close();

		conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		writer = new IndexWriter(dir, conf);
		writer.updateNumericDocValue(new Term("id", "doc"), "f", 4L);
		OLD_FORMAT_IMPERSONATION_IS_ACTIVE = false;
		try
		{
		  writer.close();
		  Assert.Fail("should not have succeeded to update a segment written with an old Codec");
		}
		catch (System.NotSupportedException e)
		{
		  writer.rollback();
		}
		finally
		{
		  OLD_FORMAT_IMPERSONATION_IS_ACTIVE = oldValue;
		}

		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testStressMultiThreading() throws Exception
	  public virtual void TestStressMultiThreading()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);

		// create index
		int numThreads = TestUtil.Next(random(), 3, 6);
		int numDocs = atLeast(2000);
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  doc.add(new StringField("id", "doc" + i, Store.NO));
		  double group = random().NextDouble();
		  string g;
		  if (group < 0.1)
		  {
			  g = "g0";
		  }
		  else if (group < 0.5)
		  {
			  g = "g1";
		  }
		  else if (group < 0.8)
		  {
			  g = "g2";
		  }
		  else
		  {
			  g = "g3";
		  }
		  doc.add(new StringField("updKey", g, Store.NO));
		  for (int j = 0; j < numThreads; j++)
		  {
			long value = random().Next();
			doc.add(new NumericDocValuesField("f" + j, value));
			doc.add(new NumericDocValuesField("cf" + j, value * 2)); // control, always updated to f * 2
		  }
		  writer.addDocument(doc);
		}

		CountDownLatch done = new CountDownLatch(numThreads);
		AtomicInteger numUpdates = new AtomicInteger(atLeast(100));

		// same thread updates a field as well as reopens
		Thread[] threads = new Thread[numThreads];
		for (int i = 0; i < threads.Length; i++)
		{
		  string f = "f" + i;
		  string cf = "cf" + i;
		  threads[i] = new ThreadAnonymousInnerClassHelper(this, "UpdateThread-" + i, writer, numDocs, done, numUpdates, f, cf);
		}

		foreach (Thread t in threads)
		{
			t.Start();
		}
		done.@await();
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		foreach (AtomicReaderContext context in reader.leaves())
		{
		  AtomicReader r = context.reader();
		  for (int i = 0; i < numThreads; i++)
		  {
			NumericDocValues ndv = r.getNumericDocValues("f" + i);
			NumericDocValues control = r.getNumericDocValues("cf" + i);
			Bits docsWithNdv = r.getDocsWithField("f" + i);
			Bits docsWithControl = r.getDocsWithField("cf" + i);
			Bits liveDocs = r.LiveDocs;
			for (int j = 0; j < r.maxDoc(); j++)
			{
			  if (liveDocs == null || liveDocs.get(j))
			  {
				Assert.AreEqual(docsWithNdv.get(j), docsWithControl.get(j));
				if (docsWithNdv.get(j))
				{
				  Assert.AreEqual(control.get(j), ndv.get(j) * 2);
				}
			  }
			}
		  }
		}
		reader.close();

		dir.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestNumericDocValuesUpdates OuterInstance;

		  private IndexWriter Writer;
		  private int NumDocs;
		  private CountDownLatch Done;
		  private AtomicInteger NumUpdates;
		  private string f;
		  private string Cf;

		  public ThreadAnonymousInnerClassHelper(TestNumericDocValuesUpdates outerInstance, string "UpdateThread-" + i, IndexWriter writer, int numDocs, CountDownLatch done, AtomicInteger numUpdates, string f, string cf) : base("UpdateThread-" + i)
		  {
			  this.OuterInstance = outerInstance;
			  this.Writer = writer;
			  this.NumDocs = numDocs;
			  this.Done = done;
			  this.NumUpdates = numUpdates;
			  this.f = f;
			  this.Cf = cf;
		  }

		  public override void Run()
		  {
			DirectoryReader reader = null;
			bool success = false;
			try
			{
			  Random random = random();
			  while (NumUpdates.AndDecrement > 0)
			  {
				double group = random.NextDouble();
				Term t;
				if (group < 0.1)
				{
					t = new Term("updKey", "g0");
				}
				else if (group < 0.5)
				{
					t = new Term("updKey", "g1");
				}
				else if (group < 0.8)
				{
					t = new Term("updKey", "g2");
				}
				else
				{
					t = new Term("updKey", "g3");
				}
	  //              System.out.println("[" + Thread.currentThread().getName() + "] numUpdates=" + numUpdates + " updateTerm=" + t);
				if (random.nextBoolean()) // sometimes unset a value
				{
				  Writer.updateNumericDocValue(t, f, null);
				  Writer.updateNumericDocValue(t, Cf, null);
				}
				else
				{
				  long updValue = random.Next();
				  Writer.updateNumericDocValue(t, f, updValue);
				  Writer.updateNumericDocValue(t, Cf, updValue * 2);
				}

				if (random.NextDouble() < 0.2)
				{
				  // delete a random document
				  int doc = random.Next(NumDocs);
	  //                System.out.println("[" + Thread.currentThread().getName() + "] deleteDoc=doc" + doc);
				  Writer.deleteDocuments(new Term("id", "doc" + doc));
				}

				if (random.NextDouble() < 0.05) // commit every 20 updates on average
				{
	  //                  System.out.println("[" + Thread.currentThread().getName() + "] commit");
				  Writer.commit();
				}

				if (random.NextDouble() < 0.1) // reopen NRT reader (apply updates), on average once every 10 updates
				{
				  if (reader == null)
				  {
	  //                  System.out.println("[" + Thread.currentThread().getName() + "] open NRT");
					reader = DirectoryReader.open(Writer, true);
				  }
				  else
				  {
	  //                  System.out.println("[" + Thread.currentThread().getName() + "] reopen NRT");
					DirectoryReader r2 = DirectoryReader.openIfChanged(reader, Writer, true);
					if (r2 != null)
					{
					  reader.close();
					  reader = r2;
					}
				  }
				}
			  }
	  //            System.out.println("[" + Thread.currentThread().getName() + "] DONE");
			  success = true;
			}
			catch (IOException e)
			{
			  throw new Exception(e);
			}
			finally
			{
			  if (reader != null)
			  {
				try
				{
				  reader.close();
				}
				catch (IOException e)
				{
				  if (success) // suppress this exception only if there was another exception
				  {
					throw new Exception(e);
				  }
				}
			  }
			  Done.countDown();
			}
		  }
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdateDifferentDocsInDifferentGens() throws Exception
	  public virtual void TestUpdateDifferentDocsInDifferentGens()
	  {
		// update same document multiple times across generations
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.MaxBufferedDocs = 4;
		IndexWriter writer = new IndexWriter(dir, conf);
		int numDocs = atLeast(10);
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  doc.add(new StringField("id", "doc" + i, Store.NO));
		  long value = random().Next();
		  doc.add(new NumericDocValuesField("f", value));
		  doc.add(new NumericDocValuesField("cf", value * 2));
		  writer.addDocument(doc);
		}

		int numGens = atLeast(5);
		for (int i = 0; i < numGens; i++)
		{
		  int doc = random().Next(numDocs);
		  Term t = new Term("id", "doc" + doc);
		  long value = random().nextLong();
		  writer.updateNumericDocValue(t, "f", value);
		  writer.updateNumericDocValue(t, "cf", value * 2);
		  DirectoryReader reader = DirectoryReader.open(writer, true);
		  foreach (AtomicReaderContext context in reader.leaves())
		  {
			AtomicReader r = context.reader();
			NumericDocValues fndv = r.getNumericDocValues("f");
			NumericDocValues cfndv = r.getNumericDocValues("cf");
			for (int j = 0; j < r.maxDoc(); j++)
			{
			  Assert.AreEqual(cfndv.get(j), fndv.get(j) * 2);
			}
		  }
		  reader.close();
		}
		writer.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testChangeCodec() throws Exception
	  public virtual void TestChangeCodec()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.MergePolicy = NoMergePolicy.COMPOUND_FILES; // disable merges to simplify test assertions.
		conf.Codec = new Lucene46CodecAnonymousInnerClassHelper2(this);
		IndexWriter writer = new IndexWriter(dir, conf.clone());
		Document doc = new Document();
		doc.add(new StringField("id", "d0", Store.NO));
		doc.add(new NumericDocValuesField("f1", 5L));
		doc.add(new NumericDocValuesField("f2", 13L));
		writer.addDocument(doc);
		writer.close();

		// change format
		conf.Codec = new Lucene46CodecAnonymousInnerClassHelper3(this);
		writer = new IndexWriter(dir, conf.clone());
		doc = new Document();
		doc.add(new StringField("id", "d1", Store.NO));
		doc.add(new NumericDocValuesField("f1", 17L));
		doc.add(new NumericDocValuesField("f2", 2L));
		writer.addDocument(doc);
		writer.updateNumericDocValue(new Term("id", "d0"), "f1", 12L);
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		AtomicReader r = SlowCompositeReaderWrapper.wrap(reader);
		NumericDocValues f1 = r.getNumericDocValues("f1");
		NumericDocValues f2 = r.getNumericDocValues("f2");
		Assert.AreEqual(12L, f1.get(0));
		Assert.AreEqual(13L, f2.get(0));
		Assert.AreEqual(17L, f1.get(1));
		Assert.AreEqual(2L, f2.get(1));
		reader.close();
		dir.close();
	  }

	  private class Lucene46CodecAnonymousInnerClassHelper2 : Lucene46Codec
	  {
		  private readonly TestNumericDocValuesUpdates OuterInstance;

		  public Lucene46CodecAnonymousInnerClassHelper2(TestNumericDocValuesUpdates outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override DocValuesFormat GetDocValuesFormatForField(string field)
		  {
			return new Lucene45DocValuesFormat();
		  }
	  }

	  private class Lucene46CodecAnonymousInnerClassHelper3 : Lucene46Codec
	  {
		  private readonly TestNumericDocValuesUpdates OuterInstance;

		  public Lucene46CodecAnonymousInnerClassHelper3(TestNumericDocValuesUpdates outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override DocValuesFormat GetDocValuesFormatForField(string field)
		  {
			return new AssertingDocValuesFormat();
		  }
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAddIndexes() throws Exception
	  public virtual void TestAddIndexes()
	  {
		Directory dir1 = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir1, conf);

		int numDocs = atLeast(50);
		int numTerms = TestUtil.Next(random(), 1, numDocs / 5);
		Set<string> randomTerms = new HashSet<string>();
		while (randomTerms.size() < numTerms)
		{
		  randomTerms.add(TestUtil.randomSimpleString(random()));
		}

		// create first index
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  doc.add(new StringField("id", RandomPicks.randomFrom(random(), randomTerms), Store.NO));
		  doc.add(new NumericDocValuesField("ndv", 4L));
		  doc.add(new NumericDocValuesField("control", 8L));
		  writer.addDocument(doc);
		}

		if (random().nextBoolean())
		{
		  writer.commit();
		}

		// update some docs to a random value
		long value = random().Next();
		Term term = new Term("id", RandomPicks.randomFrom(random(), randomTerms));
		writer.updateNumericDocValue(term, "ndv", value);
		writer.updateNumericDocValue(term, "control", value * 2);
		writer.close();

		Directory dir2 = newDirectory();
		conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		writer = new IndexWriter(dir2, conf);
		if (random().nextBoolean())
		{
		  writer.addIndexes(dir1);
		}
		else
		{
		  DirectoryReader reader = DirectoryReader.open(dir1);
		  writer.addIndexes(reader);
		  reader.close();
		}
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir2);
		foreach (AtomicReaderContext context in reader.leaves())
		{
		  AtomicReader r = context.reader();
		  NumericDocValues ndv = r.getNumericDocValues("ndv");
		  NumericDocValues control = r.getNumericDocValues("control");
		  for (int i = 0; i < r.maxDoc(); i++)
		  {
			Assert.AreEqual(ndv.get(i) * 2, control.get(i));
		  }
		}
		reader.close();

		IOUtils.close(dir1, dir2);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testDeleteUnusedUpdatesFiles() throws Exception
	  public virtual void TestDeleteUnusedUpdatesFiles()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);

		Document doc = new Document();
		doc.add(new StringField("id", "d0", Store.NO));
		doc.add(new NumericDocValuesField("f", 1L));
		writer.addDocument(doc);

		// create first gen of update files
		writer.updateNumericDocValue(new Term("id", "d0"), "f", 2L);
		writer.commit();
		int numFiles = dir.listAll().length;

		DirectoryReader r = DirectoryReader.open(dir);
		Assert.AreEqual(2L, r.leaves().get(0).reader().getNumericDocValues("f").get(0));
		r.close();

		// create second gen of update files, first gen should be deleted
		writer.updateNumericDocValue(new Term("id", "d0"), "f", 5L);
		writer.commit();
		Assert.AreEqual(numFiles, dir.listAll().length);

		r = DirectoryReader.open(dir);
		Assert.AreEqual(5L, r.leaves().get(0).reader().getNumericDocValues("f").get(0));
		r.close();

		writer.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testTonsOfUpdates() throws Exception
	  public virtual void TestTonsOfUpdates()
	  {
		// LUCENE-5248: make sure that when there are many updates, we don't use too much RAM
		Directory dir = newDirectory();
		Random random = random();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random));
		conf.RAMBufferSizeMB = IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB;
		conf.MaxBufferedDocs = IndexWriterConfig.DISABLE_AUTO_FLUSH; // don't flush by doc
		IndexWriter writer = new IndexWriter(dir, conf);

		// test data: lots of documents (few 10Ks) and lots of update terms (few hundreds)
		int numDocs = atLeast(20000);
		int numNumericFields = atLeast(5);
		int numTerms = TestUtil.Next(random, 10, 100); // terms should affect many docs
		Set<string> updateTerms = new HashSet<string>();
		while (updateTerms.size() < numTerms)
		{
		  updateTerms.add(TestUtil.randomSimpleString(random));
		}

	//    System.out.println("numDocs=" + numDocs + " numNumericFields=" + numNumericFields + " numTerms=" + numTerms);

		// build a large index with many NDV fields and update terms
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  int numUpdateTerms = TestUtil.Next(random, 1, numTerms / 10);
		  for (int j = 0; j < numUpdateTerms; j++)
		  {
			doc.add(new StringField("upd", RandomPicks.randomFrom(random, updateTerms), Store.NO));
		  }
		  for (int j = 0; j < numNumericFields; j++)
		  {
			long val = random.Next();
			doc.add(new NumericDocValuesField("f" + j, val));
			doc.add(new NumericDocValuesField("cf" + j, val * 2));
		  }
		  writer.addDocument(doc);
		}

		writer.commit(); // commit so there's something to apply to

		// set to flush every 2048 bytes (approximately every 12 updates), so we get
		// many flushes during numeric updates
		writer.Config.RAMBufferSizeMB = 2048.0 / 1024 / 1024;
		int numUpdates = atLeast(100);
	//    System.out.println("numUpdates=" + numUpdates);
		for (int i = 0; i < numUpdates; i++)
		{
		  int field = random.Next(numNumericFields);
		  Term updateTerm = new Term("upd", RandomPicks.randomFrom(random, updateTerms));
		  long value = random.Next();
		  writer.updateNumericDocValue(updateTerm, "f" + field, value);
		  writer.updateNumericDocValue(updateTerm, "cf" + field, value * 2);
		}

		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		foreach (AtomicReaderContext context in reader.leaves())
		{
		  for (int i = 0; i < numNumericFields; i++)
		  {
			AtomicReader r = context.reader();
			NumericDocValues f = r.getNumericDocValues("f" + i);
			NumericDocValues cf = r.getNumericDocValues("cf" + i);
			for (int j = 0; j < r.maxDoc(); j++)
			{
			  Assert.AreEqual("reader=" + r + ", field=f" + i + ", doc=" + j, cf.get(j), f.get(j) * 2);
			}
		  }
		}
		reader.close();

		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdatesOrder() throws Exception
	  public virtual void TestUpdatesOrder()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);

		Document doc = new Document();
		doc.add(new StringField("upd", "t1", Store.NO));
		doc.add(new StringField("upd", "t2", Store.NO));
		doc.add(new NumericDocValuesField("f1", 1L));
		doc.add(new NumericDocValuesField("f2", 1L));
		writer.addDocument(doc);
		writer.updateNumericDocValue(new Term("upd", "t1"), "f1", 2L); // update f1 to 2
		writer.updateNumericDocValue(new Term("upd", "t1"), "f2", 2L); // update f2 to 2
		writer.updateNumericDocValue(new Term("upd", "t2"), "f1", 3L); // update f1 to 3
		writer.updateNumericDocValue(new Term("upd", "t2"), "f2", 3L); // update f2 to 3
		writer.updateNumericDocValue(new Term("upd", "t1"), "f1", 4L); // update f1 to 4 (but not f2)
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(4, reader.leaves().get(0).reader().getNumericDocValues("f1").get(0));
		Assert.AreEqual(3, reader.leaves().get(0).reader().getNumericDocValues("f2").get(0));
		reader.close();

		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdateAllDeletedSegment() throws Exception
	  public virtual void TestUpdateAllDeletedSegment()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);

		Document doc = new Document();
		doc.add(new StringField("id", "doc", Store.NO));
		doc.add(new NumericDocValuesField("f1", 1L));
		writer.addDocument(doc);
		writer.addDocument(doc);
		writer.commit();
		writer.deleteDocuments(new Term("id", "doc")); // delete all docs in the first segment
		writer.addDocument(doc);
		writer.updateNumericDocValue(new Term("id", "doc"), "f1", 2L);
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(1, reader.leaves().size());
		Assert.AreEqual(2L, reader.leaves().get(0).reader().getNumericDocValues("f1").get(0));
		reader.close();

		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUpdateTwoNonexistingTerms() throws Exception
	  public virtual void TestUpdateTwoNonexistingTerms()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);

		Document doc = new Document();
		doc.add(new StringField("id", "doc", Store.NO));
		doc.add(new NumericDocValuesField("f1", 1L));
		writer.addDocument(doc);
		// update w/ multiple nonexisting terms in same field
		writer.updateNumericDocValue(new Term("c", "foo"), "f1", 2L);
		writer.updateNumericDocValue(new Term("c", "bar"), "f1", 2L);
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		Assert.AreEqual(1, reader.leaves().size());
		Assert.AreEqual(1L, reader.leaves().get(0).reader().getNumericDocValues("f1").get(0));
		reader.close();

		dir.close();
	  }

	}

}