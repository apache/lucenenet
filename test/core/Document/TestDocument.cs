namespace Lucene.Net.Document
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


	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
	using Fields = Lucene.Net.Index.Fields;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexableField = Lucene.Net.Index.IndexableField;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using PhraseQuery = Lucene.Net.Search.PhraseQuery;
	using Query = Lucene.Net.Search.Query;
	using ScoreDoc = Lucene.Net.Search.ScoreDoc;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using NUnit.Framework;


	/// <summary>
	/// Tests <seealso cref="Document"/> class.
	/// </summary>
	public class TestDocument : LuceneTestCase
	{

	  internal string BinaryVal = "this text will be stored as a byte array in the index";
	  internal string BinaryVal2 = "this text will be also stored as a byte array in the index";

	  public virtual void TestBinaryField()
	  {
		Document doc = new Document();

		FieldType ft = new FieldType();
		ft.Stored = true;
		IndexableField stringFld = new Field("string", BinaryVal, ft);
		IndexableField binaryFld = new StoredField("binary", BinaryVal.getBytes(StandardCharsets.UTF_8));
		IndexableField binaryFld2 = new StoredField("binary", BinaryVal2.getBytes(StandardCharsets.UTF_8));

		doc.add(stringFld);
		doc.add(binaryFld);

		Assert.AreEqual(2, doc.Fields.size());

		Assert.IsTrue(binaryFld.binaryValue() != null);
		Assert.IsTrue(binaryFld.fieldType().stored());
		Assert.IsFalse(binaryFld.fieldType().indexed());

		string binaryTest = doc.getBinaryValue("binary").utf8ToString();
		Assert.IsTrue(binaryTest.Equals(BinaryVal));

		string stringTest = doc.get("string");
		Assert.IsTrue(binaryTest.Equals(stringTest));

		doc.add(binaryFld2);

		Assert.AreEqual(3, doc.Fields.size());

		BytesRef[] binaryTests = doc.getBinaryValues("binary");

		Assert.AreEqual(2, binaryTests.Length);

		binaryTest = binaryTests[0].utf8ToString();
		string binaryTest2 = binaryTests[1].utf8ToString();

		Assert.IsFalse(binaryTest.Equals(binaryTest2));

		Assert.IsTrue(binaryTest.Equals(BinaryVal));
		Assert.IsTrue(binaryTest2.Equals(BinaryVal2));

		doc.removeField("string");
		Assert.AreEqual(2, doc.Fields.size());

		doc.removeFields("binary");
		Assert.AreEqual(0, doc.Fields.size());
	  }

	  /// <summary>
	  /// Tests <seealso cref="Document#removeField(String)"/> method for a brand new Document
	  /// that has not been indexed yet.
	  /// </summary>
	  /// <exception cref="Exception"> on error </exception>
	  public virtual void TestRemoveForNewDocument()
	  {
		Document doc = MakeDocumentWithFields();
		Assert.AreEqual(10, doc.Fields.size());
		doc.removeFields("keyword");
		Assert.AreEqual(8, doc.Fields.size());
		doc.removeFields("doesnotexists"); // removing non-existing fields is
										   // siltenlty ignored
		doc.removeFields("keyword"); // removing a field more than once
		Assert.AreEqual(8, doc.Fields.size());
		doc.removeField("text");
		Assert.AreEqual(7, doc.Fields.size());
		doc.removeField("text");
		Assert.AreEqual(6, doc.Fields.size());
		doc.removeField("text");
		Assert.AreEqual(6, doc.Fields.size());
		doc.removeField("doesnotexists"); // removing non-existing fields is
										  // siltenlty ignored
		Assert.AreEqual(6, doc.Fields.size());
		doc.removeFields("unindexed");
		Assert.AreEqual(4, doc.Fields.size());
		doc.removeFields("unstored");
		Assert.AreEqual(2, doc.Fields.size());
		doc.removeFields("doesnotexists"); // removing non-existing fields is
										   // siltenlty ignored
		Assert.AreEqual(2, doc.Fields.size());

		doc.removeFields("indexed_not_tokenized");
		Assert.AreEqual(0, doc.Fields.size());
	  }

	  public virtual void TestConstructorExceptions()
	  {
		FieldType ft = new FieldType();
		ft.Stored = true;
		new Field("name", "value", ft); // okay
		new StringField("name", "value", Field.Store.NO); // okay
		try
		{
		  new Field("name", "value", new FieldType());
		  Assert.Fail();
		}
		catch (System.ArgumentException e)
		{
		  // expected exception
		}
		new Field("name", "value", ft); // okay
		try
		{
		  FieldType ft2 = new FieldType();
		  ft2.Stored = true;
		  ft2.StoreTermVectors = true;
		  new Field("name", "value", ft2);
		  Assert.Fail();
		}
		catch (System.ArgumentException e)
		{
		  // expected exception
		}
	  }

	  /// <summary>
	  /// Tests <seealso cref="Document#getValues(String)"/> method for a brand new Document
	  /// that has not been indexed yet.
	  /// </summary>
	  /// <exception cref="Exception"> on error </exception>
	  public virtual void TestGetValuesForNewDocument()
	  {
		DoAssert(MakeDocumentWithFields(), false);
	  }

	  /// <summary>
	  /// Tests <seealso cref="Document#getValues(String)"/> method for a Document retrieved
	  /// from an index.
	  /// </summary>
	  /// <exception cref="Exception"> on error </exception>
	  public virtual void TestGetValuesForIndexedDocument()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		writer.addDocument(MakeDocumentWithFields());
		IndexReader reader = writer.Reader;

		IndexSearcher searcher = newSearcher(reader);

		// search for something that does exists
		Query query = new TermQuery(new Term("keyword", "test1"));

		// ensure that queries return expected results without DateFilter first
		ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);

		DoAssert(searcher.doc(hits[0].doc), true);
		writer.close();
		reader.close();
		dir.close();
	  }

	  public virtual void TestGetValues()
	  {
		Document doc = MakeDocumentWithFields();
		Assert.AreEqual(new string[] {"test1", "test2"}, doc.getValues("keyword"));
		Assert.AreEqual(new string[] {"test1", "test2"}, doc.getValues("text"));
		Assert.AreEqual(new string[] {"test1", "test2"}, doc.getValues("unindexed"));
		Assert.AreEqual(new string[0], doc.getValues("nope"));
	  }

	  public virtual void TestPositionIncrementMultiFields()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		writer.addDocument(MakeDocumentWithFields());
		IndexReader reader = writer.Reader;

		IndexSearcher searcher = newSearcher(reader);
		PhraseQuery query = new PhraseQuery();
		query.add(new Term("indexed_not_tokenized", "test1"));
		query.add(new Term("indexed_not_tokenized", "test2"));

		ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);

		DoAssert(searcher.doc(hits[0].doc), true);
		writer.close();
		reader.close();
		dir.close();
	  }

	  private Document MakeDocumentWithFields()
	  {
		Document doc = new Document();
		FieldType stored = new FieldType();
		stored.Stored = true;
		FieldType indexedNotTokenized = new FieldType();
		indexedNotTokenized.Indexed = true;
		indexedNotTokenized.Tokenized = false;
		doc.add(new StringField("keyword", "test1", Field.Store.YES));
		doc.add(new StringField("keyword", "test2", Field.Store.YES));
		doc.add(new TextField("text", "test1", Field.Store.YES));
		doc.add(new TextField("text", "test2", Field.Store.YES));
		doc.add(new Field("unindexed", "test1", stored));
		doc.add(new Field("unindexed", "test2", stored));
		doc.add(new TextField("unstored", "test1", Field.Store.NO));
		doc.add(new TextField("unstored", "test2", Field.Store.NO));
		doc.add(new Field("indexed_not_tokenized", "test1", indexedNotTokenized));
		doc.add(new Field("indexed_not_tokenized", "test2", indexedNotTokenized));
		return doc;
	  }

	  private void DoAssert(Document doc, bool fromIndex)
	  {
		IndexableField[] keywordFieldValues = doc.getFields("keyword");
		IndexableField[] textFieldValues = doc.getFields("text");
		IndexableField[] unindexedFieldValues = doc.getFields("unindexed");
		IndexableField[] unstoredFieldValues = doc.getFields("unstored");

		Assert.IsTrue(keywordFieldValues.Length == 2);
		Assert.IsTrue(textFieldValues.Length == 2);
		Assert.IsTrue(unindexedFieldValues.Length == 2);
		// this test cannot work for documents retrieved from the index
		// since unstored fields will obviously not be returned
		if (!fromIndex)
		{
		  Assert.IsTrue(unstoredFieldValues.Length == 2);
		}

		Assert.IsTrue(keywordFieldValues[0].stringValue().Equals("test1"));
		Assert.IsTrue(keywordFieldValues[1].stringValue().Equals("test2"));
		Assert.IsTrue(textFieldValues[0].stringValue().Equals("test1"));
		Assert.IsTrue(textFieldValues[1].stringValue().Equals("test2"));
		Assert.IsTrue(unindexedFieldValues[0].stringValue().Equals("test1"));
		Assert.IsTrue(unindexedFieldValues[1].stringValue().Equals("test2"));
		// this test cannot work for documents retrieved from the index
		// since unstored fields will obviously not be returned
		if (!fromIndex)
		{
		  Assert.IsTrue(unstoredFieldValues[0].stringValue().Equals("test1"));
		  Assert.IsTrue(unstoredFieldValues[1].stringValue().Equals("test2"));
		}
	  }

	  public virtual void TestFieldSetValue()
	  {

		Field field = new StringField("id", "id1", Field.Store.YES);
		Document doc = new Document();
		doc.add(field);
		doc.add(new StringField("keyword", "test", Field.Store.YES));

		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		writer.addDocument(doc);
		field.StringValue = "id2";
		writer.addDocument(doc);
		field.StringValue = "id3";
		writer.addDocument(doc);

		IndexReader reader = writer.Reader;
		IndexSearcher searcher = newSearcher(reader);

		Query query = new TermQuery(new Term("keyword", "test"));

		// ensure that queries return expected results without DateFilter first
		ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(3, hits.Length);
		int result = 0;
		for (int i = 0; i < 3; i++)
		{
		  Document doc2 = searcher.doc(hits[i].doc);
		  Field f = (Field) doc2.getField("id");
		  if (f.stringValue().Equals("id1"))
		  {
			  result |= 1;
		  }
		  else if (f.stringValue().Equals("id2"))
		  {
			  result |= 2;
		  }
		  else if (f.stringValue().Equals("id3"))
		  {
			  result |= 4;
		  }
		  else
		  {
			  Assert.Fail("unexpected id field");
		  }
		}
		writer.close();
		reader.close();
		dir.close();
		Assert.AreEqual("did not see all IDs", 7, result);
	  }

	  // LUCENE-3616
	  public virtual void TestInvalidFields()
	  {
		try
		{
		  new Field("foo", new MockTokenizer(new StringReader("")), StringField.TYPE_STORED);
		  Assert.Fail("did not hit expected exc");
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}
	  }

	  // LUCENE-3682
	  public virtual void TestTransitionAPI()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);

		Document doc = new Document();
		doc.add(new Field("stored", "abc", Field.Store.YES, Field.Index.NO));
		doc.add(new Field("stored_indexed", "abc xyz", Field.Store.YES, Field.Index.NOT_ANALYZED));
		doc.add(new Field("stored_tokenized", "abc xyz", Field.Store.YES, Field.Index.ANALYZED));
		doc.add(new Field("indexed", "abc xyz", Field.Store.NO, Field.Index.NOT_ANALYZED));
		doc.add(new Field("tokenized", "abc xyz", Field.Store.NO, Field.Index.ANALYZED));
		doc.add(new Field("tokenized_reader", new StringReader("abc xyz")));
		doc.add(new Field("tokenized_tokenstream", w.w.Analyzer.tokenStream("tokenized_tokenstream", new StringReader("abc xyz"))));
		doc.add(new Field("binary", new sbyte[10]));
		doc.add(new Field("tv", "abc xyz", Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.YES));
		doc.add(new Field("tv_pos", "abc xyz", Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS));
		doc.add(new Field("tv_off", "abc xyz", Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_OFFSETS));
		doc.add(new Field("tv_pos_off", "abc xyz", Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
		w.addDocument(doc);
		IndexReader r = w.Reader;
		w.close();

		doc = r.document(0);
		// 4 stored fields
		Assert.AreEqual(4, doc.Fields.size());
		Assert.AreEqual("abc", doc.get("stored"));
		Assert.AreEqual("abc xyz", doc.get("stored_indexed"));
		Assert.AreEqual("abc xyz", doc.get("stored_tokenized"));
		BytesRef br = doc.getBinaryValue("binary");
		Assert.IsNotNull(br);
		Assert.AreEqual(10, br.length);

		IndexSearcher s = new IndexSearcher(r);
		Assert.AreEqual(1, s.search(new TermQuery(new Term("stored_indexed", "abc xyz")), 1).totalHits);
		Assert.AreEqual(1, s.search(new TermQuery(new Term("stored_tokenized", "abc")), 1).totalHits);
		Assert.AreEqual(1, s.search(new TermQuery(new Term("stored_tokenized", "xyz")), 1).totalHits);
		Assert.AreEqual(1, s.search(new TermQuery(new Term("indexed", "abc xyz")), 1).totalHits);
		Assert.AreEqual(1, s.search(new TermQuery(new Term("tokenized", "abc")), 1).totalHits);
		Assert.AreEqual(1, s.search(new TermQuery(new Term("tokenized", "xyz")), 1).totalHits);
		Assert.AreEqual(1, s.search(new TermQuery(new Term("tokenized_reader", "abc")), 1).totalHits);
		Assert.AreEqual(1, s.search(new TermQuery(new Term("tokenized_reader", "xyz")), 1).totalHits);
		Assert.AreEqual(1, s.search(new TermQuery(new Term("tokenized_tokenstream", "abc")), 1).totalHits);
		Assert.AreEqual(1, s.search(new TermQuery(new Term("tokenized_tokenstream", "xyz")), 1).totalHits);

		foreach (string field in new string[] {"tv", "tv_pos", "tv_off", "tv_pos_off"})
		{
		  Fields tvFields = r.getTermVectors(0);
		  Terms tvs = tvFields.terms(field);
		  Assert.IsNotNull(tvs);
		  Assert.AreEqual(2, tvs.size());
		  TermsEnum tvsEnum = tvs.iterator(null);
		  Assert.AreEqual(new BytesRef("abc"), tvsEnum.next());
		  DocsAndPositionsEnum dpEnum = tvsEnum.docsAndPositions(null, null);
		  if (field.Equals("tv"))
		  {
			assertNull(dpEnum);
		  }
		  else
		  {
			Assert.IsNotNull(dpEnum);
		  }
		  Assert.AreEqual(new BytesRef("xyz"), tvsEnum.next());
		  assertNull(tvsEnum.next());
		}

		r.close();
		dir.close();
	  }

	  public virtual void TestNumericFieldAsString()
	  {
		Document doc = new Document();
		doc.add(new IntField("int", 5, Field.Store.YES));
		Assert.AreEqual("5", doc.get("int"));
		assertNull(doc.get("somethingElse"));
		doc.add(new IntField("int", 4, Field.Store.YES));
		assertArrayEquals(new string[] {"5", "4"}, doc.getValues("int"));

		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		iw.addDocument(doc);
		DirectoryReader ir = iw.Reader;
		Document sdoc = ir.document(0);
		Assert.AreEqual("5", sdoc.get("int"));
		assertNull(sdoc.get("somethingElse"));
		assertArrayEquals(new string[] {"5", "4"}, sdoc.getValues("int"));
		ir.close();
		iw.close();
		dir.close();
	  }
	}

}