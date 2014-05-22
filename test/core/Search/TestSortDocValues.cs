namespace Lucene.Net.Search
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
	using DoubleDocValuesField = Lucene.Net.Document.DoubleDocValuesField;
	using Field = Lucene.Net.Document.Field;
	using FloatDocValuesField = Lucene.Net.Document.FloatDocValuesField;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using SortedDocValuesField = Lucene.Net.Document.SortedDocValuesField;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;

	/// <summary>
	/// Tests basic sorting on docvalues fields.
	/// These are mostly like TestSort's tests, except each test
	/// indexes the field up-front as docvalues, and checks no fieldcaches were made 
	/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({"Lucene3x", "Appending", "Lucene40", "Lucene41", "Lucene42"}) public class TestSortDocValues extends Lucene.Net.Util.LuceneTestCase
	public class TestSortDocValues : LuceneTestCase
	{
		public override void SetUp()
		{
		base.setUp();
		// ensure there is nothing in fieldcache before test starts
		FieldCache.DEFAULT.purgeAllCaches();
		}

	  private void AssertNoFieldCaches()
	  {
		// docvalues sorting should NOT create any fieldcache entries!
		Assert.AreEqual(0, FieldCache.DEFAULT.CacheEntries.length);
	  }

	  /// <summary>
	  /// Tests sorting on type string </summary>
	  public virtual void TestString()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new SortedDocValuesField("value", new BytesRef("foo")));
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new SortedDocValuesField("value", new BytesRef("bar")));
		doc.add(newStringField("value", "bar", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.STRING));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(2, td.totalHits);
		// 'bar' comes before 'foo'
		Assert.AreEqual("bar", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("foo", searcher.doc(td.scoreDocs[1].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests reverse sorting on type string </summary>
	  public virtual void TestStringReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new SortedDocValuesField("value", new BytesRef("bar")));
		doc.add(newStringField("value", "bar", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new SortedDocValuesField("value", new BytesRef("foo")));
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.STRING, true));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(2, td.totalHits);
		// 'foo' comes after 'bar' in reverse order
		Assert.AreEqual("foo", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("bar", searcher.doc(td.scoreDocs[1].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type string_val </summary>
	  public virtual void TestStringVal()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new BinaryDocValuesField("value", new BytesRef("foo")));
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new BinaryDocValuesField("value", new BytesRef("bar")));
		doc.add(newStringField("value", "bar", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.STRING_VAL));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(2, td.totalHits);
		// 'bar' comes before 'foo'
		Assert.AreEqual("bar", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("foo", searcher.doc(td.scoreDocs[1].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests reverse sorting on type string_val </summary>
	  public virtual void TestStringValReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new BinaryDocValuesField("value", new BytesRef("bar")));
		doc.add(newStringField("value", "bar", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new BinaryDocValuesField("value", new BytesRef("foo")));
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.STRING_VAL, true));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(2, td.totalHits);
		// 'foo' comes after 'bar' in reverse order
		Assert.AreEqual("foo", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("bar", searcher.doc(td.scoreDocs[1].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type string_val, but with a SortedDocValuesField </summary>
	  public virtual void TestStringValSorted()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new SortedDocValuesField("value", new BytesRef("foo")));
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new SortedDocValuesField("value", new BytesRef("bar")));
		doc.add(newStringField("value", "bar", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.STRING_VAL));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(2, td.totalHits);
		// 'bar' comes before 'foo'
		Assert.AreEqual("bar", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("foo", searcher.doc(td.scoreDocs[1].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests reverse sorting on type string_val, but with a SortedDocValuesField </summary>
	  public virtual void TestStringValReverseSorted()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new SortedDocValuesField("value", new BytesRef("bar")));
		doc.add(newStringField("value", "bar", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new SortedDocValuesField("value", new BytesRef("foo")));
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.STRING_VAL, true));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(2, td.totalHits);
		// 'foo' comes after 'bar' in reverse order
		Assert.AreEqual("foo", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("bar", searcher.doc(td.scoreDocs[1].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type byte </summary>
	  public virtual void TestByte()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("value", 23));
		doc.add(newStringField("value", "23", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", -1));
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", 4));
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.BYTE));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// numeric order
		Assert.AreEqual("-1", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("23", searcher.doc(td.scoreDocs[2].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type byte in reverse </summary>
	  public virtual void TestByteReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("value", 23));
		doc.add(newStringField("value", "23", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", -1));
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", 4));
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.BYTE, true));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// reverse numeric order
		Assert.AreEqual("23", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("-1", searcher.doc(td.scoreDocs[2].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type short </summary>
	  public virtual void TestShort()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("value", 300));
		doc.add(newStringField("value", "300", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", -1));
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", 4));
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.SHORT));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// numeric order
		Assert.AreEqual("-1", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("300", searcher.doc(td.scoreDocs[2].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type short in reverse </summary>
	  public virtual void TestShortReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("value", 300));
		doc.add(newStringField("value", "300", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", -1));
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", 4));
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.SHORT, true));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// reverse numeric order
		Assert.AreEqual("300", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("-1", searcher.doc(td.scoreDocs[2].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type int </summary>
	  public virtual void TestInt()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("value", 300000));
		doc.add(newStringField("value", "300000", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", -1));
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", 4));
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.INT));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// numeric order
		Assert.AreEqual("-1", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("300000", searcher.doc(td.scoreDocs[2].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type int in reverse </summary>
	  public virtual void TestIntReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("value", 300000));
		doc.add(newStringField("value", "300000", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", -1));
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", 4));
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.INT, true));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// reverse numeric order
		Assert.AreEqual("300000", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("-1", searcher.doc(td.scoreDocs[2].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type int with a missing value </summary>
	  public virtual void TestIntMissing()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", -1));
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", 4));
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.INT));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// null is treated as a 0
		Assert.AreEqual("-1", searcher.doc(td.scoreDocs[0].doc).get("value"));
		assertNull(searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("4", searcher.doc(td.scoreDocs[2].doc).get("value"));

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type int, specifying the missing value should be treated as Integer.MAX_VALUE </summary>
	  public virtual void TestIntMissingLast()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", -1));
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", 4));
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		SortField sortField = new SortField("value", SortField.Type.INT);
		sortField.MissingValue = int.MaxValue;
		Sort sort = new Sort(sortField);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// null is treated as a Integer.MAX_VALUE
		Assert.AreEqual("-1", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4", searcher.doc(td.scoreDocs[1].doc).get("value"));
		assertNull(searcher.doc(td.scoreDocs[2].doc).get("value"));

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type long </summary>
	  public virtual void TestLong()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("value", 3000000000L));
		doc.add(newStringField("value", "3000000000", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", -1));
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", 4));
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.LONG));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// numeric order
		Assert.AreEqual("-1", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("3000000000", searcher.doc(td.scoreDocs[2].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type long in reverse </summary>
	  public virtual void TestLongReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("value", 3000000000L));
		doc.add(newStringField("value", "3000000000", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", -1));
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", 4));
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.LONG, true));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// reverse numeric order
		Assert.AreEqual("3000000000", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("-1", searcher.doc(td.scoreDocs[2].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type long with a missing value </summary>
	  public virtual void TestLongMissing()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", -1));
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", 4));
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.LONG));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// null is treated as 0
		Assert.AreEqual("-1", searcher.doc(td.scoreDocs[0].doc).get("value"));
		assertNull(searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("4", searcher.doc(td.scoreDocs[2].doc).get("value"));

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type long, specifying the missing value should be treated as Long.MAX_VALUE </summary>
	  public virtual void TestLongMissingLast()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", -1));
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("value", 4));
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		SortField sortField = new SortField("value", SortField.Type.LONG);
		sortField.MissingValue = long.MaxValue;
		Sort sort = new Sort(sortField);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// null is treated as Long.MAX_VALUE
		Assert.AreEqual("-1", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4", searcher.doc(td.scoreDocs[1].doc).get("value"));
		assertNull(searcher.doc(td.scoreDocs[2].doc).get("value"));

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type float </summary>
	  public virtual void TestFloat()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new FloatDocValuesField("value", 30.1F));
		doc.add(newStringField("value", "30.1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new FloatDocValuesField("value", -1.3F));
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new FloatDocValuesField("value", 4.2F));
		doc.add(newStringField("value", "4.2", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.FLOAT));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// numeric order
		Assert.AreEqual("-1.3", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4.2", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("30.1", searcher.doc(td.scoreDocs[2].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type float in reverse </summary>
	  public virtual void TestFloatReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new FloatDocValuesField("value", 30.1F));
		doc.add(newStringField("value", "30.1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new FloatDocValuesField("value", -1.3F));
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new FloatDocValuesField("value", 4.2F));
		doc.add(newStringField("value", "4.2", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.FLOAT, true));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// reverse numeric order
		Assert.AreEqual("30.1", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4.2", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("-1.3", searcher.doc(td.scoreDocs[2].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type float with a missing value </summary>
	  public virtual void TestFloatMissing()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new FloatDocValuesField("value", -1.3F));
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new FloatDocValuesField("value", 4.2F));
		doc.add(newStringField("value", "4.2", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.FLOAT));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// null is treated as 0
		Assert.AreEqual("-1.3", searcher.doc(td.scoreDocs[0].doc).get("value"));
		assertNull(searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("4.2", searcher.doc(td.scoreDocs[2].doc).get("value"));

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type float, specifying the missing value should be treated as Float.MAX_VALUE </summary>
	  public virtual void TestFloatMissingLast()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new FloatDocValuesField("value", -1.3F));
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new FloatDocValuesField("value", 4.2F));
		doc.add(newStringField("value", "4.2", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		SortField sortField = new SortField("value", SortField.Type.FLOAT);
		sortField.MissingValue = float.MaxValue;
		Sort sort = new Sort(sortField);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// null is treated as Float.MAX_VALUE
		Assert.AreEqual("-1.3", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4.2", searcher.doc(td.scoreDocs[1].doc).get("value"));
		assertNull(searcher.doc(td.scoreDocs[2].doc).get("value"));

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type double </summary>
	  public virtual void TestDouble()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new DoubleDocValuesField("value", 30.1));
		doc.add(newStringField("value", "30.1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new DoubleDocValuesField("value", -1.3));
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new DoubleDocValuesField("value", 4.2333333333333));
		doc.add(newStringField("value", "4.2333333333333", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new DoubleDocValuesField("value", 4.2333333333332));
		doc.add(newStringField("value", "4.2333333333332", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.DOUBLE));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(4, td.totalHits);
		// numeric order
		Assert.AreEqual("-1.3", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4.2333333333332", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("4.2333333333333", searcher.doc(td.scoreDocs[2].doc).get("value"));
		Assert.AreEqual("30.1", searcher.doc(td.scoreDocs[3].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type double with +/- zero </summary>
	  public virtual void TestDoubleSignedZero()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new DoubleDocValuesField("value", +0D));
		doc.add(newStringField("value", "+0", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new DoubleDocValuesField("value", -0D));
		doc.add(newStringField("value", "-0", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.DOUBLE));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(2, td.totalHits);
		// numeric order
		Assert.AreEqual("-0", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("+0", searcher.doc(td.scoreDocs[1].doc).get("value"));

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type double in reverse </summary>
	  public virtual void TestDoubleReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(new DoubleDocValuesField("value", 30.1));
		doc.add(newStringField("value", "30.1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new DoubleDocValuesField("value", -1.3));
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new DoubleDocValuesField("value", 4.2333333333333));
		doc.add(newStringField("value", "4.2333333333333", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new DoubleDocValuesField("value", 4.2333333333332));
		doc.add(newStringField("value", "4.2333333333332", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.DOUBLE, true));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(4, td.totalHits);
		// numeric order
		Assert.AreEqual("30.1", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4.2333333333333", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("4.2333333333332", searcher.doc(td.scoreDocs[2].doc).get("value"));
		Assert.AreEqual("-1.3", searcher.doc(td.scoreDocs[3].doc).get("value"));
		AssertNoFieldCaches();

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type double with a missing value </summary>
	  public virtual void TestDoubleMissing()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new DoubleDocValuesField("value", -1.3));
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new DoubleDocValuesField("value", 4.2333333333333));
		doc.add(newStringField("value", "4.2333333333333", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new DoubleDocValuesField("value", 4.2333333333332));
		doc.add(newStringField("value", "4.2333333333332", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.DOUBLE));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(4, td.totalHits);
		// null treated as a 0
		Assert.AreEqual("-1.3", searcher.doc(td.scoreDocs[0].doc).get("value"));
		assertNull(searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("4.2333333333332", searcher.doc(td.scoreDocs[2].doc).get("value"));
		Assert.AreEqual("4.2333333333333", searcher.doc(td.scoreDocs[3].doc).get("value"));

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type double, specifying the missing value should be treated as Double.MAX_VALUE </summary>
	  public virtual void TestDoubleMissingLast()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new DoubleDocValuesField("value", -1.3));
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new DoubleDocValuesField("value", 4.2333333333333));
		doc.add(newStringField("value", "4.2333333333333", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(new DoubleDocValuesField("value", 4.2333333333332));
		doc.add(newStringField("value", "4.2333333333332", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		SortField sortField = new SortField("value", SortField.Type.DOUBLE);
		sortField.MissingValue = double.MaxValue;
		Sort sort = new Sort(sortField);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(4, td.totalHits);
		// null treated as Double.MAX_VALUE
		Assert.AreEqual("-1.3", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4.2333333333332", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("4.2333333333333", searcher.doc(td.scoreDocs[2].doc).get("value"));
		assertNull(searcher.doc(td.scoreDocs[3].doc).get("value"));

		ir.close();
		dir.close();
	  }
	}

}