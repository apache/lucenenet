using System;
using System.Threading;

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


	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using BinaryDocValuesField = Lucene.Net.Document.BinaryDocValuesField;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using Store = Lucene.Net.Document.Field.Store;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using SortedDocValuesField = Lucene.Net.Document.SortedDocValuesField;
	using SortedSetDocValuesField = Lucene.Net.Document.SortedSetDocValuesField;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using FieldCache = Lucene.Net.Search.FieldCache;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;

	/// 
	/// <summary>
	/// Tests DocValues integration into IndexWriter
	/// 
	/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs("Lucene3x") public class TestDocValuesIndexing extends Lucene.Net.Util.LuceneTestCase
	public class TestDocValuesIndexing : LuceneTestCase
	{
	  /*
	   * - add test for multi segment case with deletes
	   * - add multithreaded tests / integrate into stress indexing?
	   */

	  public virtual void TestAddIndexes()
	  {
		Directory d1 = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), d1);
		Document doc = new Document();
		doc.add(newStringField("id", "1", Field.Store.YES));
		doc.add(new NumericDocValuesField("dv", 1));
		w.addDocument(doc);
		IndexReader r1 = w.Reader;
		w.close();

		Directory d2 = newDirectory();
		w = new RandomIndexWriter(random(), d2);
		doc = new Document();
		doc.add(newStringField("id", "2", Field.Store.YES));
		doc.add(new NumericDocValuesField("dv", 2));
		w.addDocument(doc);
		IndexReader r2 = w.Reader;
		w.close();

		Directory d3 = newDirectory();
		w = new RandomIndexWriter(random(), d3);
		w.addIndexes(SlowCompositeReaderWrapper.wrap(r1), SlowCompositeReaderWrapper.wrap(r2));
		r1.close();
		d1.close();
		r2.close();
		d2.close();

		w.forceMerge(1);
		DirectoryReader r3 = w.Reader;
		w.close();
		AtomicReader sr = getOnlySegmentReader(r3);
		Assert.AreEqual(2, sr.numDocs());
		NumericDocValues docValues = sr.getNumericDocValues("dv");
		Assert.IsNotNull(docValues);
		r3.close();
		d3.close();
	  }

	  public virtual void TestMultiValuedDocValuesField()
	  {
		Directory d = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), d);
		Document doc = new Document();
		Field f = new NumericDocValuesField("field", 17);
		// Index doc values are single-valued so we should not
		// be able to add same field more than once:
		doc.add(f);
		doc.add(f);
		try
		{
		  w.addDocument(doc);
		  Assert.Fail("didn't hit expected exception");
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}

		doc = new Document();
		doc.add(f);
		w.addDocument(doc);
		w.forceMerge(1);
		DirectoryReader r = w.Reader;
		w.close();
		Assert.AreEqual(17, FieldCache.DEFAULT.getInts(getOnlySegmentReader(r), "field", false).get(0));
		r.close();
		d.close();
	  }

	  public virtual void TestDifferentTypedDocValuesField()
	  {
		Directory d = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), d);
		Document doc = new Document();
		// Index doc values are single-valued so we should not
		// be able to add same field more than once:
		Field f;
		doc.add(f = new NumericDocValuesField("field", 17));
		doc.add(new BinaryDocValuesField("field", new BytesRef("blah")));
		try
		{
		  w.addDocument(doc);
		  Assert.Fail("didn't hit expected exception");
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}

		doc = new Document();
		doc.add(f);
		w.addDocument(doc);
		w.forceMerge(1);
		DirectoryReader r = w.Reader;
		w.close();
		Assert.AreEqual(17, FieldCache.DEFAULT.getInts(getOnlySegmentReader(r), "field", false).get(0));
		r.close();
		d.close();
	  }

	  public virtual void TestDifferentTypedDocValuesField2()
	  {
		Directory d = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), d);
		Document doc = new Document();
		// Index doc values are single-valued so we should not
		// be able to add same field more than once:
		Field f = new NumericDocValuesField("field", 17);
		doc.add(f);
		doc.add(new SortedDocValuesField("field", new BytesRef("hello")));
		try
		{
		  w.addDocument(doc);
		  Assert.Fail("didn't hit expected exception");
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}
		doc = new Document();
		doc.add(f);
		w.addDocument(doc);
		w.forceMerge(1);
		DirectoryReader r = w.Reader;
		Assert.AreEqual(17, getOnlySegmentReader(r).getNumericDocValues("field").get(0));
		r.close();
		w.close();
		d.close();
	  }

	  // LUCENE-3870
	  public virtual void TestLengthPrefixAcrossTwoPages()
	  {
		Directory d = newDirectory();
		IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		sbyte[] bytes = new sbyte[32764];
		BytesRef b = new BytesRef();
		b.bytes = bytes;
		b.length = bytes.Length;
		doc.add(new SortedDocValuesField("field", b));
		w.addDocument(doc);
		bytes[0] = 1;
		w.addDocument(doc);
		w.forceMerge(1);
		DirectoryReader r = w.Reader;
		BinaryDocValues s = FieldCache.DEFAULT.getTerms(getOnlySegmentReader(r), "field", false);

		BytesRef bytes1 = new BytesRef();
		s.get(0, bytes1);
		Assert.AreEqual(bytes.Length, bytes1.length);
		bytes[0] = 0;
		Assert.AreEqual(b, bytes1);

		s.get(1, bytes1);
		Assert.AreEqual(bytes.Length, bytes1.length);
		bytes[0] = 1;
		Assert.AreEqual(b, bytes1);
		r.close();
		w.close();
		d.close();
	  }

	  public virtual void TestDocValuesUnstored()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig iwconfig = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwconfig.MergePolicy = newLogMergePolicy();
		IndexWriter writer = new IndexWriter(dir, iwconfig);
		for (int i = 0; i < 50; i++)
		{
		  Document doc = new Document();
		  doc.add(new NumericDocValuesField("dv", i));
		  doc.add(new TextField("docId", "" + i, Field.Store.YES));
		  writer.addDocument(doc);
		}
		DirectoryReader r = writer.Reader;
		AtomicReader slow = SlowCompositeReaderWrapper.wrap(r);
		FieldInfos fi = slow.FieldInfos;
		FieldInfo dvInfo = fi.fieldInfo("dv");
		Assert.IsTrue(dvInfo.hasDocValues());
		NumericDocValues dv = slow.getNumericDocValues("dv");
		for (int i = 0; i < 50; i++)
		{
		  Assert.AreEqual(i, dv.get(i));
		  Document d = slow.document(i);
		  // cannot use d.get("dv") due to another bug!
		  assertNull(d.getField("dv"));
		  Assert.AreEqual(Convert.ToString(i), d.get("docId"));
		}
		slow.close();
		writer.close();
		dir.close();
	  }

	  // Same field in one document as different types:
	  public virtual void TestMixedTypesSameDocument()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(new NumericDocValuesField("foo", 0));
		doc.add(new SortedDocValuesField("foo", new BytesRef("hello")));
		try
		{
		  w.addDocument(doc);
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}
		w.close();
		dir.close();
	  }

	  // Two documents with same field as different types:
	  public virtual void TestMixedTypesDifferentDocuments()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(new NumericDocValuesField("foo", 0));
		w.addDocument(doc);

		doc = new Document();
		doc.add(new SortedDocValuesField("foo", new BytesRef("hello")));
		try
		{
		  w.addDocument(doc);
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}
		w.close();
		dir.close();
	  }

	  public virtual void TestAddSortedTwice()
	  {
		Analyzer analyzer = new MockAnalyzer(random());

		Directory directory = newDirectory();
		// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwc.MergePolicy = newLogMergePolicy();
		IndexWriter iwriter = new IndexWriter(directory, iwc);
		Document doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("foo!")));
		doc.add(new SortedDocValuesField("dv", new BytesRef("bar!")));
		try
		{
		  iwriter.addDocument(doc);
		  Assert.Fail("didn't hit expected exception");
		}
		catch (System.ArgumentException expected)
		{
		  // expected
		}

		iwriter.close();
		directory.close();
	  }

	  public virtual void TestAddBinaryTwice()
	  {
		Analyzer analyzer = new MockAnalyzer(random());

		Directory directory = newDirectory();
		// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwc.MergePolicy = newLogMergePolicy();
		IndexWriter iwriter = new IndexWriter(directory, iwc);
		Document doc = new Document();
		doc.add(new BinaryDocValuesField("dv", new BytesRef("foo!")));
		doc.add(new BinaryDocValuesField("dv", new BytesRef("bar!")));
		try
		{
		  iwriter.addDocument(doc);
		  Assert.Fail("didn't hit expected exception");
		}
		catch (System.ArgumentException expected)
		{
		  // expected
		}

		iwriter.close();
		directory.close();
	  }

	  public virtual void TestAddNumericTwice()
	  {
		Analyzer analyzer = new MockAnalyzer(random());

		Directory directory = newDirectory();
		// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwc.MergePolicy = newLogMergePolicy();
		IndexWriter iwriter = new IndexWriter(directory, iwc);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("dv", 1));
		doc.add(new NumericDocValuesField("dv", 2));
		try
		{
		  iwriter.addDocument(doc);
		  Assert.Fail("didn't hit expected exception");
		}
		catch (System.ArgumentException expected)
		{
		  // expected
		}

		iwriter.close();
		directory.close();
	  }

	  public virtual void TestTooLargeSortedBytes()
	  {
		Analyzer analyzer = new MockAnalyzer(random());

		Directory directory = newDirectory();
		// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwc.MergePolicy = newLogMergePolicy();
		IndexWriter iwriter = new IndexWriter(directory, iwc);
		Document doc = new Document();
		sbyte[] bytes = new sbyte[100000];
		BytesRef b = new BytesRef(bytes);
		random().nextBytes(bytes);
		doc.add(new SortedDocValuesField("dv", b));
		try
		{
		  iwriter.addDocument(doc);
		  Assert.Fail("did not get expected exception");
		}
		catch (System.ArgumentException expected)
		{
		  // expected
		}
		iwriter.close();
		directory.close();
	  }

	  public virtual void TestTooLargeTermSortedSetBytes()
	  {
		assumeTrue("codec does not support SORTED_SET", defaultCodecSupportsSortedSet());
		Analyzer analyzer = new MockAnalyzer(random());

		Directory directory = newDirectory();
		// we don't use RandomIndexWriter because it might add more docvalues than we expect !!!!1
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwc.MergePolicy = newLogMergePolicy();
		IndexWriter iwriter = new IndexWriter(directory, iwc);
		Document doc = new Document();
		sbyte[] bytes = new sbyte[100000];
		BytesRef b = new BytesRef(bytes);
		random().nextBytes(bytes);
		doc.add(new SortedSetDocValuesField("dv", b));
		try
		{
		  iwriter.addDocument(doc);
		  Assert.Fail("did not get expected exception");
		}
		catch (System.ArgumentException expected)
		{
		  // expected
		}
		iwriter.close();
		directory.close();
	  }

	  // Two documents across segments
	  public virtual void TestMixedTypesDifferentSegments()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(new NumericDocValuesField("foo", 0));
		w.addDocument(doc);
		w.commit();

		doc = new Document();
		doc.add(new SortedDocValuesField("foo", new BytesRef("hello")));
		try
		{
		  w.addDocument(doc);
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}
		w.close();
		dir.close();
	  }

	  // Add inconsistent document after deleteAll
	  public virtual void TestMixedTypesAfterDeleteAll()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(new NumericDocValuesField("foo", 0));
		w.addDocument(doc);
		w.deleteAll();

		doc = new Document();
		doc.add(new SortedDocValuesField("foo", new BytesRef("hello")));
		w.addDocument(doc);
		w.close();
		dir.close();
	  }

	  // Add inconsistent document after reopening IW w/ create
	  public virtual void TestMixedTypesAfterReopenCreate()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(new NumericDocValuesField("foo", 0));
		w.addDocument(doc);
		w.close();

		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwc.OpenMode = IndexWriterConfig.OpenMode.CREATE;
		w = new IndexWriter(dir, iwc);
		doc = new Document();
		doc.add(new SortedDocValuesField("foo", new BytesRef("hello")));
		w.addDocument(doc);
		w.close();
		dir.close();
	  }

	  // Two documents with same field as different types, added
	  // from separate threads:
	  public virtual void TestMixedTypesDifferentThreads()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		CountDownLatch startingGun = new CountDownLatch(1);
		AtomicBoolean hitExc = new AtomicBoolean();
		Thread[] threads = new Thread[3];
		for (int i = 0;i < 3;i++)
		{
		  Field field;
		  if (i == 0)
		  {
			field = new SortedDocValuesField("foo", new BytesRef("hello"));
		  }
		  else if (i == 1)
		  {
			field = new NumericDocValuesField("foo", 0);
		  }
		  else
		  {
			field = new BinaryDocValuesField("foo", new BytesRef("bazz"));
		  }
		  Document doc = new Document();
		  doc.add(field);

		  threads[i] = new ThreadAnonymousInnerClassHelper(this, w, startingGun, hitExc, doc);
		  threads[i].Start();
		}

		startingGun.countDown();

		foreach (Thread t in threads)
		{
		  t.Join();
		}
		Assert.IsTrue(hitExc.get());
		w.close();
		dir.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestDocValuesIndexing OuterInstance;

		  private IndexWriter w;
		  private CountDownLatch StartingGun;
		  private AtomicBoolean HitExc;
		  private Document Doc;

		  public ThreadAnonymousInnerClassHelper(TestDocValuesIndexing outerInstance, IndexWriter w, CountDownLatch startingGun, AtomicBoolean hitExc, Document doc)
		  {
			  this.OuterInstance = outerInstance;
			  this.w = w;
			  this.StartingGun = startingGun;
			  this.HitExc = hitExc;
			  this.Doc = doc;
		  }

		  public override void Run()
		  {
			try
			{
			  StartingGun.@await();
			  w.addDocument(Doc);
			}
			catch (System.ArgumentException iae)
			{
			  // expected
			  HitExc.set(true);
			}
			catch (Exception e)
			{
			  throw new Exception(e);
			}
		  }
	  }

	  // Adding documents via addIndexes
	  public virtual void TestMixedTypesViaAddIndexes()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(new NumericDocValuesField("foo", 0));
		w.addDocument(doc);

		// Make 2nd index w/ inconsistent field
		Directory dir2 = newDirectory();
		IndexWriter w2 = new IndexWriter(dir2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		doc = new Document();
		doc.add(new SortedDocValuesField("foo", new BytesRef("hello")));
		w2.addDocument(doc);
		w2.close();

		try
		{
		  w.addIndexes(new Directory[] {dir2});
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}

		IndexReader r = DirectoryReader.open(dir2);
		try
		{
		  w.addIndexes(new IndexReader[] {r});
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}

		r.close();
		dir2.close();
		w.close();
		dir.close();
	  }

	  public virtual void TestIllegalTypeChange()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("dv", 0L));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("foo")));
		try
		{
		  writer.addDocument(doc);
		  Assert.Fail("did not hit exception");
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}
		writer.close();
		dir.close();
	  }

	  public virtual void TestIllegalTypeChangeAcrossSegments()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf.clone());
		Document doc = new Document();
		doc.add(new NumericDocValuesField("dv", 0L));
		writer.addDocument(doc);
		writer.close();

		writer = new IndexWriter(dir, conf.clone());
		doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("foo")));
		try
		{
		  writer.addDocument(doc);
		  Assert.Fail("did not hit exception");
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}
		writer.close();
		dir.close();
	  }

	  public virtual void TestTypeChangeAfterCloseAndDeleteAll()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf.clone());
		Document doc = new Document();
		doc.add(new NumericDocValuesField("dv", 0L));
		writer.addDocument(doc);
		writer.close();

		writer = new IndexWriter(dir, conf.clone());
		writer.deleteAll();
		doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("foo")));
		writer.addDocument(doc);
		writer.close();
		dir.close();
	  }

	  public virtual void TestTypeChangeAfterDeleteAll()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("dv", 0L));
		writer.addDocument(doc);
		writer.deleteAll();
		doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("foo")));
		writer.addDocument(doc);
		writer.close();
		dir.close();
	  }

	  public virtual void TestTypeChangeAfterCommitAndDeleteAll()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("dv", 0L));
		writer.addDocument(doc);
		writer.commit();
		writer.deleteAll();
		doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("foo")));
		writer.addDocument(doc);
		writer.close();
		dir.close();
	  }

	  public virtual void TestTypeChangeAfterOpenCreate()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf.clone());
		Document doc = new Document();
		doc.add(new NumericDocValuesField("dv", 0L));
		writer.addDocument(doc);
		writer.close();
		conf.OpenMode = IndexWriterConfig.OpenMode.CREATE;
		writer = new IndexWriter(dir, conf.clone());
		doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("foo")));
		writer.addDocument(doc);
		writer.close();
		dir.close();
	  }

	  public virtual void TestTypeChangeViaAddIndexes()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf.clone());
		Document doc = new Document();
		doc.add(new NumericDocValuesField("dv", 0L));
		writer.addDocument(doc);
		writer.close();

		Directory dir2 = newDirectory();
		writer = new IndexWriter(dir2, conf.clone());
		doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("foo")));
		writer.addDocument(doc);
		try
		{
		  writer.addIndexes(dir);
		  Assert.Fail("did not hit exception");
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}
		writer.close();

		dir.close();
		dir2.close();
	  }

	  public virtual void TestTypeChangeViaAddIndexesIR()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf.clone());
		Document doc = new Document();
		doc.add(new NumericDocValuesField("dv", 0L));
		writer.addDocument(doc);
		writer.close();

		Directory dir2 = newDirectory();
		writer = new IndexWriter(dir2, conf.clone());
		doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("foo")));
		writer.addDocument(doc);
		IndexReader[] readers = new IndexReader[] {DirectoryReader.open(dir)};
		try
		{
		  writer.addIndexes(readers);
		  Assert.Fail("did not hit exception");
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}
		readers[0].close();
		writer.close();

		dir.close();
		dir2.close();
	  }

	  public virtual void TestTypeChangeViaAddIndexes2()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf.clone());
		Document doc = new Document();
		doc.add(new NumericDocValuesField("dv", 0L));
		writer.addDocument(doc);
		writer.close();

		Directory dir2 = newDirectory();
		writer = new IndexWriter(dir2, conf.clone());
		writer.addIndexes(dir);
		doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("foo")));
		try
		{
		  writer.addDocument(doc);
		  Assert.Fail("did not hit exception");
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}
		writer.close();
		dir2.close();
		dir.close();
	  }

	  public virtual void TestTypeChangeViaAddIndexesIR2()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf.clone());
		Document doc = new Document();
		doc.add(new NumericDocValuesField("dv", 0L));
		writer.addDocument(doc);
		writer.close();

		Directory dir2 = newDirectory();
		writer = new IndexWriter(dir2, conf.clone());
		IndexReader[] readers = new IndexReader[] {DirectoryReader.open(dir)};
		writer.addIndexes(readers);
		readers[0].close();
		doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("foo")));
		try
		{
		  writer.addDocument(doc);
		  Assert.Fail("did not hit exception");
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}
		writer.close();
		dir2.close();
		dir.close();
	  }

	  public virtual void TestDocsWithField()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("dv", 0L));
		writer.addDocument(doc);

		doc = new Document();
		doc.add(new TextField("dv", "some text", Field.Store.NO));
		doc.add(new NumericDocValuesField("dv", 0L));
		writer.addDocument(doc);

		DirectoryReader r = writer.Reader;
		writer.close();

		AtomicReader subR = r.leaves().get(0).reader();
		Assert.AreEqual(2, subR.numDocs());

		Bits bits = FieldCache.DEFAULT.getDocsWithField(subR, "dv");
		Assert.IsTrue(bits.get(0));
		Assert.IsTrue(bits.get(1));
		r.close();
		dir.close();
	  }

	  public virtual void TestSameFieldNameForPostingAndDocValue()
	  {
		// LUCENE-5192: FieldInfos.Builder neglected to update
		// globalFieldNumbers.docValuesType map if the field existed, resulting in
		// potentially adding the same field with different DV types.
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);

		Document doc = new Document();
		doc.add(new StringField("f", "mock-value", Field.Store.NO));
		doc.add(new NumericDocValuesField("f", 5));
		writer.addDocument(doc);
		writer.commit();

		doc = new Document();
		doc.add(new BinaryDocValuesField("f", new BytesRef("mock")));
		try
		{
		  writer.addDocument(doc);
		  Assert.Fail("should not have succeeded to add a field with different DV type than what already exists");
		}
		catch (System.ArgumentException e)
		{
		  writer.rollback();
		}

		dir.close();
	  }

	}

}