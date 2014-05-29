using System;
using System.Collections.Generic;
using System.Text;

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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using StringField = Lucene.Net.Document.StringField;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using MultiReader = Lucene.Net.Index.MultiReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using Occur = Lucene.Net.Search.BooleanClause.Occur_e;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	/*
	 * Very simple tests of sorting.
	 * 
	 * THE RULES:
	 * 1. keywords like 'abstract' and 'static' should not appear in this file.
	 * 2. each test method should be self-contained and understandable. 
	 * 3. no test methods should share code with other test methods.
	 * 4. no testing of things unrelated to sorting.
	 * 5. no tracers.
	 * 6. keyword 'class' should appear only once in this file, here ----
	 *                                                                  |
	 *        -----------------------------------------------------------
	 *        |
	 *       \./
	 */
	public class TestSort : LuceneTestCase
	{

	  /// <summary>
	  /// Tests sorting on type string </summary>
	  public virtual void TestString()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type string with a missing value </summary>
	  public virtual void TestStringMissing()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "bar", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.STRING));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// null comes first
		assertNull(searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("bar", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("foo", searcher.doc(td.scoreDocs[2].doc).get("value"));

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
		doc.add(newStringField("value", "bar", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type string_val with a missing value </summary>
	  public virtual void TestStringValMissing()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "bar", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.STRING_VAL));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// null comes first
		assertNull(searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("bar", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("foo", searcher.doc(td.scoreDocs[2].doc).get("value"));

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type string with a missing
	  ///  value sorted first 
	  /// </summary>
	  public virtual void TestStringMissingSortedFirst()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "bar", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		SortField sf = new SortField("value", SortField.Type.STRING);
		Sort sort = new Sort(sf);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// null comes first
		assertNull(searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("bar", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("foo", searcher.doc(td.scoreDocs[2].doc).get("value"));

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests reverse sorting on type string with a missing
	  ///  value sorted first 
	  /// </summary>
	  public virtual void TestStringMissingSortedFirstReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "bar", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		SortField sf = new SortField("value", SortField.Type.STRING, true);
		Sort sort = new Sort(sf);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		Assert.AreEqual("foo", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("bar", searcher.doc(td.scoreDocs[1].doc).get("value"));
		// null comes last
		assertNull(searcher.doc(td.scoreDocs[2].doc).get("value"));

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type string with a missing
	  ///  value sorted last 
	  /// </summary>
	  public virtual void TestStringValMissingSortedLast()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "bar", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		SortField sf = new SortField("value", SortField.Type.STRING);
		sf.MissingValue = SortField.STRING_LAST;
		Sort sort = new Sort(sf);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		Assert.AreEqual("bar", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("foo", searcher.doc(td.scoreDocs[1].doc).get("value"));
		// null comes last
		assertNull(searcher.doc(td.scoreDocs[2].doc).get("value"));

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests reverse sorting on type string with a missing
	  ///  value sorted last 
	  /// </summary>
	  public virtual void TestStringValMissingSortedLastReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "bar", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		SortField sf = new SortField("value", SortField.Type.STRING, true);
		sf.MissingValue = SortField.STRING_LAST;
		Sort sort = new Sort(sf);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// null comes first
		assertNull(searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("foo", searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("bar", searcher.doc(td.scoreDocs[2].doc).get("value"));

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
		doc.add(newStringField("value", "bar", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on internal docid order </summary>
	  public virtual void TestFieldDoc()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newStringField("value", "foo", Field.Store.NO));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "bar", Field.Store.NO));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(SortField.FIELD_DOC);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(2, td.totalHits);
		// docid 0, then docid 1
		Assert.AreEqual(0, td.scoreDocs[0].doc);
		Assert.AreEqual(1, td.scoreDocs[1].doc);

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on reverse internal docid order </summary>
	  public virtual void TestFieldDocReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newStringField("value", "foo", Field.Store.NO));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "bar", Field.Store.NO));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField(null, SortField.Type.DOC, true));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(2, td.totalHits);
		// docid 1, then docid 0
		Assert.AreEqual(1, td.scoreDocs[0].doc);
		Assert.AreEqual(0, td.scoreDocs[1].doc);

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests default sort (by score) </summary>
	  public virtual void TestFieldScore()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newTextField("value", "foo bar bar bar bar", Field.Store.NO));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newTextField("value", "foo foo foo foo foo", Field.Store.NO));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort();

		TopDocs actual = searcher.search(new TermQuery(new Term("value", "foo")), 10, sort);
		Assert.AreEqual(2, actual.totalHits);

		TopDocs expected = searcher.search(new TermQuery(new Term("value", "foo")), 10);
		// the two topdocs should be the same
		Assert.AreEqual(expected.totalHits, actual.totalHits);
		for (int i = 0; i < actual.scoreDocs.length; i++)
		{
		  Assert.AreEqual(actual.scoreDocs[i].doc, expected.scoreDocs[i].doc);
		}

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests default sort (by score) in reverse </summary>
	  public virtual void TestFieldScoreReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newTextField("value", "foo bar bar bar bar", Field.Store.NO));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newTextField("value", "foo foo foo foo foo", Field.Store.NO));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField(null, SortField.Type.SCORE, true));

		TopDocs actual = searcher.search(new TermQuery(new Term("value", "foo")), 10, sort);
		Assert.AreEqual(2, actual.totalHits);

		TopDocs expected = searcher.search(new TermQuery(new Term("value", "foo")), 10);
		// the two topdocs should be the reverse of each other
		Assert.AreEqual(expected.totalHits, actual.totalHits);
		Assert.AreEqual(actual.scoreDocs[0].doc, expected.scoreDocs[1].doc);
		Assert.AreEqual(actual.scoreDocs[1].doc, expected.scoreDocs[0].doc);

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
		doc.add(newStringField("value", "23", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type byte with a missing value </summary>
	  public virtual void TestByteMissing()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.BYTE));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// null value is treated as a 0
		Assert.AreEqual("-1", searcher.doc(td.scoreDocs[0].doc).get("value"));
		assertNull(searcher.doc(td.scoreDocs[1].doc).get("value"));
		Assert.AreEqual("4", searcher.doc(td.scoreDocs[2].doc).get("value"));

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type byte, specifying the missing value should be treated as Byte.MAX_VALUE </summary>
	  public virtual void TestByteMissingLast()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		SortField sortField = new SortField("value", SortField.Type.BYTE);
		sortField.MissingValue = sbyte.MaxValue;
		Sort sort = new Sort(sortField);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// null value is treated Byte.MAX_VALUE
		Assert.AreEqual("-1", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4", searcher.doc(td.scoreDocs[1].doc).get("value"));
		assertNull(searcher.doc(td.scoreDocs[2].doc).get("value"));

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
		doc.add(newStringField("value", "23", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
		doc.add(newStringField("value", "300", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting on type short with a missing value </summary>
	  public virtual void TestShortMissing()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.SHORT));

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
	  /// Tests sorting on type short, specifying the missing value should be treated as Short.MAX_VALUE </summary>
	  public virtual void TestShortMissingLast()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "4", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		SortField sortField = new SortField("value", SortField.Type.SHORT);
		sortField.MissingValue = short.MaxValue;
		Sort sort = new Sort(sortField);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(3, td.totalHits);
		// null is treated as Short.MAX_VALUE
		Assert.AreEqual("-1", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("4", searcher.doc(td.scoreDocs[1].doc).get("value"));
		assertNull(searcher.doc(td.scoreDocs[2].doc).get("value"));

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
		doc.add(newStringField("value", "300", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
		doc.add(newStringField("value", "300000", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
	  /// Tests sorting on type int in reverse </summary>
	  public virtual void TestIntReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newStringField("value", "300000", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
		doc.add(newStringField("value", "3000000000", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
	  /// Tests sorting on type long in reverse </summary>
	  public virtual void TestLongReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newStringField("value", "3000000000", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
		doc.add(newStringField("value", "30.1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
	  /// Tests sorting on type float in reverse </summary>
	  public virtual void TestFloatReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newStringField("value", "30.1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
		doc.add(newStringField("value", "30.1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "4.2333333333333", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
		doc.add(newStringField("value", "+0", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
	  /// Tests sorting on type double with a missing value </summary>
	  public virtual void TestDoubleMissing()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "4.2333333333333", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "4.2333333333333", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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

	  /// <summary>
	  /// Tests sorting on type double in reverse </summary>
	  public virtual void TestDoubleReverse()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newStringField("value", "30.1", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "-1.3", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "4.2333333333333", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
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

		ir.close();
		dir.close();
	  }

	  public virtual void TestEmptyStringVsNullStringSort()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(newStringField("f", "", Field.Store.NO));
		doc.add(newStringField("t", "1", Field.Store.NO));
		w.addDocument(doc);
		w.commit();
		doc = new Document();
		doc.add(newStringField("t", "1", Field.Store.NO));
		w.addDocument(doc);

		IndexReader r = DirectoryReader.open(w, true);
		w.close();
		IndexSearcher s = newSearcher(r);
		TopDocs hits = s.search(new TermQuery(new Term("t", "1")), null, 10, new Sort(new SortField("f", SortField.Type.STRING)));
		Assert.AreEqual(2, hits.totalHits);
		// null sorts first
		Assert.AreEqual(1, hits.scoreDocs[0].doc);
		Assert.AreEqual(0, hits.scoreDocs[1].doc);
		r.close();
		dir.close();
	  }

	  /// <summary>
	  /// test that we don't throw exception on multi-valued field (LUCENE-2142) </summary>
	  public virtual void TestMultiValuedField()
	  {
		Directory indexStore = newDirectory();
		IndexWriter writer = new IndexWriter(indexStore, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		for (int i = 0; i < 5; i++)
		{
			Document doc = new Document();
			doc.add(new StringField("string", "a" + i, Field.Store.NO));
			doc.add(new StringField("string", "b" + i, Field.Store.NO));
			writer.addDocument(doc);
		}
		writer.forceMerge(1); // enforce one segment to have a higher unique term count in all cases
		writer.close();
		Sort sort = new Sort(new SortField("string", SortField.Type.STRING), SortField.FIELD_DOC);
		// this should not throw AIOOBE or RuntimeEx
		IndexReader reader = DirectoryReader.open(indexStore);
		IndexSearcher searcher = newSearcher(reader);
		searcher.search(new MatchAllDocsQuery(), null, 500, sort);
		reader.close();
		indexStore.close();
	  }

	  public virtual void TestMaxScore()
	  {
		Directory d = newDirectory();
		// Not RIW because we need exactly 2 segs:
		IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		int id = 0;
		for (int seg = 0;seg < 2;seg++)
		{
		  for (int docIDX = 0;docIDX < 10;docIDX++)
		  {
			Document doc = new Document();
			doc.add(newStringField("id", "" + docIDX, Field.Store.YES));
			StringBuilder sb = new StringBuilder();
			for (int i = 0;i < id;i++)
			{
			  sb.Append(' ');
			  sb.Append("text");
			}
			doc.add(newTextField("body", sb.ToString(), Field.Store.NO));
			w.addDocument(doc);
			id++;
		  }
		  w.commit();
		}

		IndexReader r = DirectoryReader.open(w, true);
		w.close();
		Query q = new TermQuery(new Term("body", "text"));
		IndexSearcher s = newSearcher(r);
		float maxScore = s.search(q, 10).MaxScore;
		Assert.AreEqual(maxScore, s.search(q, null, 3, Sort.INDEXORDER, random().nextBoolean(), true).MaxScore, 0.0);
		Assert.AreEqual(maxScore, s.search(q, null, 3, Sort.RELEVANCE, random().nextBoolean(), true).MaxScore, 0.0);
		Assert.AreEqual(maxScore, s.search(q, null, 3, new Sort(new SortField[] {new SortField("id", SortField.Type.INT, false)}), random().nextBoolean(), true).MaxScore, 0.0);
		Assert.AreEqual(maxScore, s.search(q, null, 3, new Sort(new SortField[] {new SortField("id", SortField.Type.INT, true)}), random().nextBoolean(), true).MaxScore, 0.0);
		r.close();
		d.close();
	  }

	  /// <summary>
	  /// test sorts when there's nothing in the index </summary>
	  public virtual void TestEmptyIndex()
	  {
		IndexSearcher empty = newSearcher(new MultiReader());
		Query query = new TermQuery(new Term("contents", "foo"));

		Sort sort = new Sort();
		TopDocs td = empty.search(query, null, 10, sort, true, true);
		Assert.AreEqual(0, td.totalHits);

		sort.Sort = SortField.FIELD_DOC;
		td = empty.search(query, null, 10, sort, true, true);
		Assert.AreEqual(0, td.totalHits);

		sort.setSort(new SortField("int", SortField.Type.INT), SortField.FIELD_DOC);
		td = empty.search(query, null, 10, sort, true, true);
		Assert.AreEqual(0, td.totalHits);

		sort.setSort(new SortField("string", SortField.Type.STRING, true), SortField.FIELD_DOC);
		td = empty.search(query, null, 10, sort, true, true);
		Assert.AreEqual(0, td.totalHits);

		sort.setSort(new SortField("string_val", SortField.Type.STRING_VAL, true), SortField.FIELD_DOC);
		td = empty.search(query, null, 10, sort, true, true);
		Assert.AreEqual(0, td.totalHits);

		sort.setSort(new SortField("float", SortField.Type.FLOAT), new SortField("string", SortField.Type.STRING));
		td = empty.search(query, null, 10, sort, true, true);
		Assert.AreEqual(0, td.totalHits);
	  }

	  /// <summary>
	  /// test sorts for a custom int parser that uses a simple char encoding 
	  /// </summary>
	  public virtual void TestCustomIntParser()
	  {
		IList<string> letters = Arrays.asList(new string[] {"A", "B", "C", "D", "E", "F", "G", "H", "I", "J"});
		Collections.shuffle(letters, random());

		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		foreach (string letter in letters)
		{
		  Document doc = new Document();
		  doc.add(newStringField("parser", letter, Field.Store.YES));
		  iw.addDocument(doc);
		}

		IndexReader ir = iw.Reader;
		iw.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("parser", new IntParserAnonymousInnerClassHelper(this)), SortField.FIELD_DOC);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);

		// results should be in alphabetical order
		Assert.AreEqual(10, td.totalHits);
		letters.Sort();
		for (int i = 0; i < letters.Count; i++)
		{
		  Assert.AreEqual(letters[i], searcher.doc(td.scoreDocs[i].doc).get("parser"));
		}

		ir.close();
		dir.close();
	  }

	  private class IntParserAnonymousInnerClassHelper : FieldCache.IntParser
	  {
		  private readonly TestSort OuterInstance;

		  public IntParserAnonymousInnerClassHelper(TestSort outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override int ParseInt(BytesRef term)
		  {
			return (term.bytes[term.offset] - 'A') * 123456;
		  }

		  public override TermsEnum TermsEnum(Terms terms)
		  {
			return terms.iterator(null);
		  }
	  }

	  /// <summary>
	  /// test sorts for a custom byte parser that uses a simple char encoding 
	  /// </summary>
	  public virtual void TestCustomByteParser()
	  {
		IList<string> letters = Arrays.asList(new string[] {"A", "B", "C", "D", "E", "F", "G", "H", "I", "J"});
		Collections.shuffle(letters, random());

		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		foreach (string letter in letters)
		{
		  Document doc = new Document();
		  doc.add(newStringField("parser", letter, Field.Store.YES));
		  iw.addDocument(doc);
		}

		IndexReader ir = iw.Reader;
		iw.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("parser", new ByteParserAnonymousInnerClassHelper(this)), SortField.FIELD_DOC);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);

		// results should be in alphabetical order
		Assert.AreEqual(10, td.totalHits);
		letters.Sort();
		for (int i = 0; i < letters.Count; i++)
		{
		  Assert.AreEqual(letters[i], searcher.doc(td.scoreDocs[i].doc).get("parser"));
		}

		ir.close();
		dir.close();
	  }

	  private class ByteParserAnonymousInnerClassHelper : FieldCache.ByteParser
	  {
		  private readonly TestSort OuterInstance;

		  public ByteParserAnonymousInnerClassHelper(TestSort outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override sbyte ParseByte(BytesRef term)
		  {
			return (sbyte)(term.bytes[term.offset] - 'A');
		  }

		  public override TermsEnum TermsEnum(Terms terms)
		  {
			return terms.iterator(null);
		  }
	  }

	  /// <summary>
	  /// test sorts for a custom short parser that uses a simple char encoding 
	  /// </summary>
	  public virtual void TestCustomShortParser()
	  {
		IList<string> letters = Arrays.asList(new string[] {"A", "B", "C", "D", "E", "F", "G", "H", "I", "J"});
		Collections.shuffle(letters, random());

		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		foreach (string letter in letters)
		{
		  Document doc = new Document();
		  doc.add(newStringField("parser", letter, Field.Store.YES));
		  iw.addDocument(doc);
		}

		IndexReader ir = iw.Reader;
		iw.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("parser", new ShortParserAnonymousInnerClassHelper(this)), SortField.FIELD_DOC);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);

		// results should be in alphabetical order
		Assert.AreEqual(10, td.totalHits);
		letters.Sort();
		for (int i = 0; i < letters.Count; i++)
		{
		  Assert.AreEqual(letters[i], searcher.doc(td.scoreDocs[i].doc).get("parser"));
		}

		ir.close();
		dir.close();
	  }

	  private class ShortParserAnonymousInnerClassHelper : FieldCache.ShortParser
	  {
		  private readonly TestSort OuterInstance;

		  public ShortParserAnonymousInnerClassHelper(TestSort outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override short ParseShort(BytesRef term)
		  {
			return (short)(term.bytes[term.offset] - 'A');
		  }

		  public override TermsEnum TermsEnum(Terms terms)
		  {
			return terms.iterator(null);
		  }
	  }

	  /// <summary>
	  /// test sorts for a custom long parser that uses a simple char encoding 
	  /// </summary>
	  public virtual void TestCustomLongParser()
	  {
		IList<string> letters = Arrays.asList(new string[] {"A", "B", "C", "D", "E", "F", "G", "H", "I", "J"});
		Collections.shuffle(letters, random());

		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		foreach (string letter in letters)
		{
		  Document doc = new Document();
		  doc.add(newStringField("parser", letter, Field.Store.YES));
		  iw.addDocument(doc);
		}

		IndexReader ir = iw.Reader;
		iw.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("parser", new LongParserAnonymousInnerClassHelper(this)), SortField.FIELD_DOC);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);

		// results should be in alphabetical order
		Assert.AreEqual(10, td.totalHits);
		letters.Sort();
		for (int i = 0; i < letters.Count; i++)
		{
		  Assert.AreEqual(letters[i], searcher.doc(td.scoreDocs[i].doc).get("parser"));
		}

		ir.close();
		dir.close();
	  }

	  private class LongParserAnonymousInnerClassHelper : FieldCache.LongParser
	  {
		  private readonly TestSort OuterInstance;

		  public LongParserAnonymousInnerClassHelper(TestSort outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override long ParseLong(BytesRef term)
		  {
			return (term.bytes[term.offset] - 'A') * 1234567890L;
		  }

		  public override TermsEnum TermsEnum(Terms terms)
		  {
			return terms.iterator(null);
		  }
	  }

	  /// <summary>
	  /// test sorts for a custom float parser that uses a simple char encoding 
	  /// </summary>
	  public virtual void TestCustomFloatParser()
	  {
		IList<string> letters = Arrays.asList(new string[] {"A", "B", "C", "D", "E", "F", "G", "H", "I", "J"});
		Collections.shuffle(letters, random());

		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		foreach (string letter in letters)
		{
		  Document doc = new Document();
		  doc.add(newStringField("parser", letter, Field.Store.YES));
		  iw.addDocument(doc);
		}

		IndexReader ir = iw.Reader;
		iw.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("parser", new FloatParserAnonymousInnerClassHelper(this)), SortField.FIELD_DOC);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);

		// results should be in alphabetical order
		Assert.AreEqual(10, td.totalHits);
		letters.Sort();
		for (int i = 0; i < letters.Count; i++)
		{
		  Assert.AreEqual(letters[i], searcher.doc(td.scoreDocs[i].doc).get("parser"));
		}

		ir.close();
		dir.close();
	  }

	  private class FloatParserAnonymousInnerClassHelper : FieldCache.FloatParser
	  {
		  private readonly TestSort OuterInstance;

		  public FloatParserAnonymousInnerClassHelper(TestSort outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override float ParseFloat(BytesRef term)
		  {
			return (float) Math.Sqrt(term.bytes[term.offset]);
		  }

		  public override TermsEnum TermsEnum(Terms terms)
		  {
			return terms.iterator(null);
		  }
	  }

	  /// <summary>
	  /// test sorts for a custom double parser that uses a simple char encoding 
	  /// </summary>
	  public virtual void TestCustomDoubleParser()
	  {
		IList<string> letters = Arrays.asList(new string[] {"A", "B", "C", "D", "E", "F", "G", "H", "I", "J"});
		Collections.shuffle(letters, random());

		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir);
		foreach (string letter in letters)
		{
		  Document doc = new Document();
		  doc.add(newStringField("parser", letter, Field.Store.YES));
		  iw.addDocument(doc);
		}

		IndexReader ir = iw.Reader;
		iw.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("parser", new DoubleParserAnonymousInnerClassHelper(this)), SortField.FIELD_DOC);

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);

		// results should be in alphabetical order
		Assert.AreEqual(10, td.totalHits);
		letters.Sort();
		for (int i = 0; i < letters.Count; i++)
		{
		  Assert.AreEqual(letters[i], searcher.doc(td.scoreDocs[i].doc).get("parser"));
		}

		ir.close();
		dir.close();
	  }

	  private class DoubleParserAnonymousInnerClassHelper : FieldCache.DoubleParser
	  {
		  private readonly TestSort OuterInstance;

		  public DoubleParserAnonymousInnerClassHelper(TestSort outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override double ParseDouble(BytesRef term)
		  {
			return Math.Pow(term.bytes[term.offset], (term.bytes[term.offset] - 'A'));
		  }

		  public override TermsEnum TermsEnum(Terms terms)
		  {
			return terms.iterator(null);
		  }
	  }

	  /// <summary>
	  /// Tests sorting a single document </summary>
	  public virtual void TestSortOneDocument()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.STRING));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(1, td.totalHits);
		Assert.AreEqual("foo", searcher.doc(td.scoreDocs[0].doc).get("value"));

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting a single document with scores </summary>
	  public virtual void TestSortOneDocumentWithScores()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(new SortField("value", SortField.Type.STRING));

		TopDocs expected = searcher.search(new TermQuery(new Term("value", "foo")), 10);
		Assert.AreEqual(1, expected.totalHits);
		TopDocs actual = searcher.search(new TermQuery(new Term("value", "foo")), null, 10, sort, true, true);

		Assert.AreEqual(expected.totalHits, actual.totalHits);
		Assert.AreEqual(expected.scoreDocs[0].score, actual.scoreDocs[0].score, 0F);

		ir.close();
		dir.close();
	  }

	  /// <summary>
	  /// Tests sorting with two fields </summary>
	  public virtual void TestSortTwoFields()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newStringField("tievalue", "tied", Field.Store.NO));
		doc.add(newStringField("value", "foo", Field.Store.YES));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("tievalue", "tied", Field.Store.NO));
		doc.add(newStringField("value", "bar", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		// tievalue, then value
		Sort sort = new Sort(new SortField("tievalue", SortField.Type.STRING), new SortField("value", SortField.Type.STRING));

		TopDocs td = searcher.search(new MatchAllDocsQuery(), 10, sort);
		Assert.AreEqual(2, td.totalHits);
		// 'bar' comes before 'foo'
		Assert.AreEqual("bar", searcher.doc(td.scoreDocs[0].doc).get("value"));
		Assert.AreEqual("foo", searcher.doc(td.scoreDocs[1].doc).get("value"));

		ir.close();
		dir.close();
	  }

	  public virtual void TestScore()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newStringField("value", "bar", Field.Store.NO));
		writer.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("value", "foo", Field.Store.NO));
		writer.addDocument(doc);
		IndexReader ir = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(ir);
		Sort sort = new Sort(SortField.FIELD_SCORE);

		BooleanQuery bq = new BooleanQuery();
		bq.add(new TermQuery(new Term("value", "foo")), Occur.SHOULD);
		bq.add(new MatchAllDocsQuery(), Occur.SHOULD);
		TopDocs td = searcher.search(bq, 10, sort);
		Assert.AreEqual(2, td.totalHits);
		Assert.AreEqual(1, td.scoreDocs[0].doc);
		Assert.AreEqual(0, td.scoreDocs[1].doc);

		ir.close();
		dir.close();
	  }
	}

}