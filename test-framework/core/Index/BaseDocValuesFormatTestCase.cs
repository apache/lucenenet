using System;
using System.Diagnostics;
using System.Collections.Generic;
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

//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Index.SortedSetDocValues.NO_MORE_ORDS;


	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Codec = Lucene.Net.Codecs.Codec;
	using Lucene42DocValuesFormat = Lucene.Net.Codecs.Lucene42.Lucene42DocValuesFormat;
	using BinaryDocValuesField = Lucene.Net.Document.BinaryDocValuesField;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FloatDocValuesField = Lucene.Net.Document.FloatDocValuesField;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using SortedDocValuesField = Lucene.Net.Document.SortedDocValuesField;
	using SortedSetDocValuesField = Lucene.Net.Document.SortedSetDocValuesField;
	using StoredField = Lucene.Net.Document.StoredField;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using SeekStatus = Lucene.Net.Index.TermsEnum.SeekStatus;
	using BooleanClause = Lucene.Net.Search.BooleanClause;
	using BooleanQuery = Lucene.Net.Search.BooleanQuery;
	using FieldCache = Lucene.Net.Search.FieldCache;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using Query = Lucene.Net.Search.Query;
	using ScoreDoc = Lucene.Net.Search.ScoreDoc;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using TopDocs = Lucene.Net.Search.TopDocs;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using BytesRefHash = Lucene.Net.Util.BytesRefHash;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Abstract class to do basic tests for a docvalues format.
	/// NOTE: this test focuses on the docvalues impl, nothing else.
	/// The [stretch] goal is for this test to be
	/// so thorough in testing a new DocValuesFormat that if this
	/// test passes, then all Lucene/Solr tests should also pass.  Ie,
	/// if there is some bug in a given DocValuesFormat that this
	/// test fails to catch then this test needs to be improved! 
	/// </summary>
	public abstract class BaseDocValuesFormatTestCase : BaseIndexFileFormatTestCase
	{

	  protected internal override void AddRandomFields(Document doc)
	  {
		if (Usually())
		{
		  doc.add(new NumericDocValuesField("ndv", Random().Next(1 << 12)));
		  doc.add(new BinaryDocValuesField("bdv", new BytesRef(TestUtil.RandomSimpleString(Random()))));
		  doc.add(new SortedDocValuesField("sdv", new BytesRef(TestUtil.RandomSimpleString(Random(), 2))));
		}
		if (DefaultCodecSupportsSortedSet())
		{
		  int numValues = Random().Next(5);
		  for (int i = 0; i < numValues; ++i)
		  {
			doc.add(new SortedSetDocValuesField("ssdv", new BytesRef(TestUtil.RandomSimpleString(Random(), 2))));
		  }
		}
	  }

	  public virtual void TestOneNumber()
	  {
		Directory directory = NewDirectory();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
		Document doc = new Document();
		string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
		string text = "this is the text to be indexed. " + longTerm;
		doc.add(NewTextField("fieldname", text, Field.Store.YES));
		doc.add(new NumericDocValuesField("dv", 5));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		IndexSearcher isearcher = new IndexSearcher(ireader);

		AssertEquals(1, isearcher.search(new TermQuery(new Term("fieldname", longTerm)), 1).totalHits);
		Query query = new TermQuery(new Term("fieldname", "text"));
		TopDocs hits = isearcher.search(query, null, 1);
		AssertEquals(1, hits.totalHits);
		// Iterate through the results:
		for (int i = 0; i < hits.scoreDocs.length; i++)
		{
		  Document hitDoc = isearcher.doc(hits.scoreDocs[i].doc);
		  AssertEquals(text, hitDoc.get("fieldname"));
		  Debug.Assert(ireader.leaves().size() == 1);
		  NumericDocValues dv = ireader.leaves().get(0).reader().getNumericDocValues("dv");
		  AssertEquals(5, dv.get(hits.scoreDocs[i].doc));
		}

		ireader.close();
		directory.close();
	  }

	  public virtual void TestOneFloat()
	  {
		Directory directory = NewDirectory();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
		Document doc = new Document();
		string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
		string text = "this is the text to be indexed. " + longTerm;
		doc.add(NewTextField("fieldname", text, Field.Store.YES));
		doc.add(new FloatDocValuesField("dv", 5.7f));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		IndexSearcher isearcher = new IndexSearcher(ireader);

		AssertEquals(1, isearcher.search(new TermQuery(new Term("fieldname", longTerm)), 1).totalHits);
		Query query = new TermQuery(new Term("fieldname", "text"));
		TopDocs hits = isearcher.search(query, null, 1);
		AssertEquals(1, hits.totalHits);
		// Iterate through the results:
		for (int i = 0; i < hits.scoreDocs.length; i++)
		{
		  Document hitDoc = isearcher.doc(hits.scoreDocs[i].doc);
		  AssertEquals(text, hitDoc.get("fieldname"));
		  Debug.Assert(ireader.leaves().size() == 1);
		  NumericDocValues dv = ireader.leaves().get(0).reader().getNumericDocValues("dv");
		  AssertEquals(float.floatToRawIntBits(5.7f), dv.get(hits.scoreDocs[i].doc));
		}

		ireader.close();
		directory.close();
	  }

	  public virtual void TestTwoNumbers()
	  {
		Directory directory = NewDirectory();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
		Document doc = new Document();
		string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
		string text = "this is the text to be indexed. " + longTerm;
		doc.add(NewTextField("fieldname", text, Field.Store.YES));
		doc.add(new NumericDocValuesField("dv1", 5));
		doc.add(new NumericDocValuesField("dv2", 17));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		IndexSearcher isearcher = new IndexSearcher(ireader);

		AssertEquals(1, isearcher.search(new TermQuery(new Term("fieldname", longTerm)), 1).totalHits);
		Query query = new TermQuery(new Term("fieldname", "text"));
		TopDocs hits = isearcher.search(query, null, 1);
		AssertEquals(1, hits.totalHits);
		// Iterate through the results:
		for (int i = 0; i < hits.scoreDocs.length; i++)
		{
		  Document hitDoc = isearcher.doc(hits.scoreDocs[i].doc);
		  AssertEquals(text, hitDoc.get("fieldname"));
		  Debug.Assert(ireader.leaves().size() == 1);
		  NumericDocValues dv = ireader.leaves().get(0).reader().getNumericDocValues("dv1");
		  AssertEquals(5, dv.get(hits.scoreDocs[i].doc));
		  dv = ireader.leaves().get(0).reader().getNumericDocValues("dv2");
		  AssertEquals(17, dv.get(hits.scoreDocs[i].doc));
		}

		ireader.close();
		directory.close();
	  }

	  public virtual void TestTwoBinaryValues()
	  {
		Directory directory = NewDirectory();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
		Document doc = new Document();
		string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
		string text = "this is the text to be indexed. " + longTerm;
		doc.add(NewTextField("fieldname", text, Field.Store.YES));
		doc.add(new BinaryDocValuesField("dv1", new BytesRef(longTerm)));
		doc.add(new BinaryDocValuesField("dv2", new BytesRef(text)));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		IndexSearcher isearcher = new IndexSearcher(ireader);

		AssertEquals(1, isearcher.search(new TermQuery(new Term("fieldname", longTerm)), 1).totalHits);
		Query query = new TermQuery(new Term("fieldname", "text"));
		TopDocs hits = isearcher.search(query, null, 1);
		AssertEquals(1, hits.totalHits);
		// Iterate through the results:
		for (int i = 0; i < hits.scoreDocs.length; i++)
		{
		  Document hitDoc = isearcher.doc(hits.scoreDocs[i].doc);
		  AssertEquals(text, hitDoc.get("fieldname"));
		  Debug.Assert(ireader.leaves().size() == 1);
		  BinaryDocValues dv = ireader.leaves().get(0).reader().getBinaryDocValues("dv1");
		  BytesRef scratch = new BytesRef();
		  dv.get(hits.scoreDocs[i].doc, scratch);
		  AssertEquals(new BytesRef(longTerm), scratch);
		  dv = ireader.leaves().get(0).reader().getBinaryDocValues("dv2");
		  dv.get(hits.scoreDocs[i].doc, scratch);
		  AssertEquals(new BytesRef(text), scratch);
		}

		ireader.close();
		directory.close();
	  }

	  public virtual void TestTwoFieldsMixed()
	  {
		Directory directory = NewDirectory();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
		Document doc = new Document();
		string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
		string text = "this is the text to be indexed. " + longTerm;
		doc.add(NewTextField("fieldname", text, Field.Store.YES));
		doc.add(new NumericDocValuesField("dv1", 5));
		doc.add(new BinaryDocValuesField("dv2", new BytesRef("hello world")));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		IndexSearcher isearcher = new IndexSearcher(ireader);

		AssertEquals(1, isearcher.search(new TermQuery(new Term("fieldname", longTerm)), 1).totalHits);
		Query query = new TermQuery(new Term("fieldname", "text"));
		TopDocs hits = isearcher.search(query, null, 1);
		AssertEquals(1, hits.totalHits);
		BytesRef scratch = new BytesRef();
		// Iterate through the results:
		for (int i = 0; i < hits.scoreDocs.length; i++)
		{
		  Document hitDoc = isearcher.doc(hits.scoreDocs[i].doc);
		  AssertEquals(text, hitDoc.get("fieldname"));
		  Debug.Assert(ireader.leaves().size() == 1);
		  NumericDocValues dv = ireader.leaves().get(0).reader().getNumericDocValues("dv1");
		  AssertEquals(5, dv.get(hits.scoreDocs[i].doc));
		  BinaryDocValues dv2 = ireader.leaves().get(0).reader().getBinaryDocValues("dv2");
		  dv2.get(hits.scoreDocs[i].doc, scratch);
		  AssertEquals(new BytesRef("hello world"), scratch);
		}

		ireader.close();
		directory.close();
	  }

	  public virtual void TestThreeFieldsMixed()
	  {
		Directory directory = NewDirectory();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
		Document doc = new Document();
		string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
		string text = "this is the text to be indexed. " + longTerm;
		doc.add(NewTextField("fieldname", text, Field.Store.YES));
		doc.add(new SortedDocValuesField("dv1", new BytesRef("hello hello")));
		doc.add(new NumericDocValuesField("dv2", 5));
		doc.add(new BinaryDocValuesField("dv3", new BytesRef("hello world")));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		IndexSearcher isearcher = new IndexSearcher(ireader);

		AssertEquals(1, isearcher.search(new TermQuery(new Term("fieldname", longTerm)), 1).totalHits);
		Query query = new TermQuery(new Term("fieldname", "text"));
		TopDocs hits = isearcher.search(query, null, 1);
		AssertEquals(1, hits.totalHits);
		BytesRef scratch = new BytesRef();
		// Iterate through the results:
		for (int i = 0; i < hits.scoreDocs.length; i++)
		{
		  Document hitDoc = isearcher.doc(hits.scoreDocs[i].doc);
		  AssertEquals(text, hitDoc.get("fieldname"));
		  Debug.Assert(ireader.leaves().size() == 1);
		  SortedDocValues dv = ireader.leaves().get(0).reader().getSortedDocValues("dv1");
		  int ord = dv.getOrd(0);
		  dv.lookupOrd(ord, scratch);
		  AssertEquals(new BytesRef("hello hello"), scratch);
		  NumericDocValues dv2 = ireader.leaves().get(0).reader().getNumericDocValues("dv2");
		  AssertEquals(5, dv2.get(hits.scoreDocs[i].doc));
		  BinaryDocValues dv3 = ireader.leaves().get(0).reader().getBinaryDocValues("dv3");
		  dv3.get(hits.scoreDocs[i].doc, scratch);
		  AssertEquals(new BytesRef("hello world"), scratch);
		}

		ireader.close();
		directory.close();
	  }

	  public virtual void TestThreeFieldsMixed2()
	  {
		Directory directory = NewDirectory();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);
		Document doc = new Document();
		string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
		string text = "this is the text to be indexed. " + longTerm;
		doc.add(NewTextField("fieldname", text, Field.Store.YES));
		doc.add(new BinaryDocValuesField("dv1", new BytesRef("hello world")));
		doc.add(new SortedDocValuesField("dv2", new BytesRef("hello hello")));
		doc.add(new NumericDocValuesField("dv3", 5));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		IndexSearcher isearcher = new IndexSearcher(ireader);

		AssertEquals(1, isearcher.search(new TermQuery(new Term("fieldname", longTerm)), 1).totalHits);
		Query query = new TermQuery(new Term("fieldname", "text"));
		TopDocs hits = isearcher.search(query, null, 1);
		AssertEquals(1, hits.totalHits);
		BytesRef scratch = new BytesRef();
		// Iterate through the results:
		for (int i = 0; i < hits.scoreDocs.length; i++)
		{
		  Document hitDoc = isearcher.doc(hits.scoreDocs[i].doc);
		  AssertEquals(text, hitDoc.get("fieldname"));
		  Debug.Assert(ireader.leaves().size() == 1);
		  SortedDocValues dv = ireader.leaves().get(0).reader().getSortedDocValues("dv2");
		  int ord = dv.getOrd(0);
		  dv.lookupOrd(ord, scratch);
		  AssertEquals(new BytesRef("hello hello"), scratch);
		  NumericDocValues dv2 = ireader.leaves().get(0).reader().getNumericDocValues("dv3");
		  AssertEquals(5, dv2.get(hits.scoreDocs[i].doc));
		  BinaryDocValues dv3 = ireader.leaves().get(0).reader().getBinaryDocValues("dv1");
		  dv3.get(hits.scoreDocs[i].doc, scratch);
		  AssertEquals(new BytesRef("hello world"), scratch);
		}

		ireader.close();
		directory.close();
	  }

	  public virtual void TestTwoDocumentsNumeric()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("dv", 1));
		iwriter.AddDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("dv", 2));
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		NumericDocValues dv = ireader.leaves().get(0).reader().getNumericDocValues("dv");
		AssertEquals(1, dv.get(0));
		AssertEquals(2, dv.get(1));

		ireader.close();
		directory.close();
	  }

	  public virtual void TestTwoDocumentsMerged()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(NewField("id", "0", StringField.TYPE_STORED));
		doc.add(new NumericDocValuesField("dv", -10));
		iwriter.AddDocument(doc);
		iwriter.Commit();
		doc = new Document();
		doc.add(NewField("id", "1", StringField.TYPE_STORED));
		doc.add(new NumericDocValuesField("dv", 99));
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		NumericDocValues dv = ireader.leaves().get(0).reader().getNumericDocValues("dv");
		for (int i = 0;i < 2;i++)
		{
		  Document doc2 = ireader.leaves().get(0).reader().document(i);
		  long expected;
		  if (doc2.get("id").Equals("0"))
		  {
			expected = -10;
		  }
		  else
		  {
			expected = 99;
		  }
		  AssertEquals(expected, dv.get(i));
		}

		ireader.close();
		directory.close();
	  }

	  public virtual void TestBigNumericRange()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("dv", long.MinValue));
		iwriter.AddDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("dv", long.MaxValue));
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		NumericDocValues dv = ireader.leaves().get(0).reader().getNumericDocValues("dv");
		AssertEquals(long.MinValue, dv.get(0));
		AssertEquals(long.MaxValue, dv.get(1));

		ireader.close();
		directory.close();
	  }

	  public virtual void TestBigNumericRange2()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new NumericDocValuesField("dv", -8841491950446638677L));
		iwriter.AddDocument(doc);
		doc = new Document();
		doc.add(new NumericDocValuesField("dv", 9062230939892376225L));
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		NumericDocValues dv = ireader.leaves().get(0).reader().getNumericDocValues("dv");
		AssertEquals(-8841491950446638677L, dv.get(0));
		AssertEquals(9062230939892376225L, dv.get(1));

		ireader.close();
		directory.close();
	  }

	  public virtual void TestBytes()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
		string text = "this is the text to be indexed. " + longTerm;
		doc.add(NewTextField("fieldname", text, Field.Store.YES));
		doc.add(new BinaryDocValuesField("dv", new BytesRef("hello world")));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		IndexSearcher isearcher = new IndexSearcher(ireader);

		AssertEquals(1, isearcher.search(new TermQuery(new Term("fieldname", longTerm)), 1).totalHits);
		Query query = new TermQuery(new Term("fieldname", "text"));
		TopDocs hits = isearcher.search(query, null, 1);
		AssertEquals(1, hits.totalHits);
		BytesRef scratch = new BytesRef();
		// Iterate through the results:
		for (int i = 0; i < hits.scoreDocs.length; i++)
		{
		  Document hitDoc = isearcher.doc(hits.scoreDocs[i].doc);
		  AssertEquals(text, hitDoc.get("fieldname"));
		  Debug.Assert(ireader.leaves().size() == 1);
		  BinaryDocValues dv = ireader.leaves().get(0).reader().getBinaryDocValues("dv");
		  dv.get(hits.scoreDocs[i].doc, scratch);
		  AssertEquals(new BytesRef("hello world"), scratch);
		}

		ireader.close();
		directory.close();
	  }

	  public virtual void TestBytesTwoDocumentsMerged()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(NewField("id", "0", StringField.TYPE_STORED));
		doc.add(new BinaryDocValuesField("dv", new BytesRef("hello world 1")));
		iwriter.AddDocument(doc);
		iwriter.Commit();
		doc = new Document();
		doc.add(NewField("id", "1", StringField.TYPE_STORED));
		doc.add(new BinaryDocValuesField("dv", new BytesRef("hello 2")));
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		BinaryDocValues dv = ireader.leaves().get(0).reader().getBinaryDocValues("dv");
		BytesRef scratch = new BytesRef();
		for (int i = 0;i < 2;i++)
		{
		  Document doc2 = ireader.leaves().get(0).reader().document(i);
		  string expected;
		  if (doc2.get("id").Equals("0"))
		  {
			expected = "hello world 1";
		  }
		  else
		  {
			expected = "hello 2";
		  }
		  dv.get(i, scratch);
		  AssertEquals(expected, scratch.utf8ToString());
		}

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedBytes()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
		string text = "this is the text to be indexed. " + longTerm;
		doc.add(NewTextField("fieldname", text, Field.Store.YES));
		doc.add(new SortedDocValuesField("dv", new BytesRef("hello world")));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		IndexSearcher isearcher = new IndexSearcher(ireader);

		AssertEquals(1, isearcher.search(new TermQuery(new Term("fieldname", longTerm)), 1).totalHits);
		Query query = new TermQuery(new Term("fieldname", "text"));
		TopDocs hits = isearcher.search(query, null, 1);
		AssertEquals(1, hits.totalHits);
		BytesRef scratch = new BytesRef();
		// Iterate through the results:
		for (int i = 0; i < hits.scoreDocs.length; i++)
		{
		  Document hitDoc = isearcher.doc(hits.scoreDocs[i].doc);
		  AssertEquals(text, hitDoc.get("fieldname"));
		  Debug.Assert(ireader.leaves().size() == 1);
		  SortedDocValues dv = ireader.leaves().get(0).reader().getSortedDocValues("dv");
		  dv.lookupOrd(dv.getOrd(hits.scoreDocs[i].doc), scratch);
		  AssertEquals(new BytesRef("hello world"), scratch);
		}

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedBytesTwoDocuments()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("hello world 1")));
		iwriter.AddDocument(doc);
		doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("hello world 2")));
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		SortedDocValues dv = ireader.leaves().get(0).reader().getSortedDocValues("dv");
		BytesRef scratch = new BytesRef();
		dv.lookupOrd(dv.getOrd(0), scratch);
		AssertEquals("hello world 1", scratch.utf8ToString());
		dv.lookupOrd(dv.getOrd(1), scratch);
		AssertEquals("hello world 2", scratch.utf8ToString());

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedBytesThreeDocuments()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("hello world 1")));
		iwriter.AddDocument(doc);
		doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("hello world 2")));
		iwriter.AddDocument(doc);
		doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("hello world 1")));
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		SortedDocValues dv = ireader.leaves().get(0).reader().getSortedDocValues("dv");
		AssertEquals(2, dv.ValueCount);
		BytesRef scratch = new BytesRef();
		AssertEquals(0, dv.getOrd(0));
		dv.lookupOrd(0, scratch);
		AssertEquals("hello world 1", scratch.utf8ToString());
		AssertEquals(1, dv.getOrd(1));
		dv.lookupOrd(1, scratch);
		AssertEquals("hello world 2", scratch.utf8ToString());
		AssertEquals(0, dv.getOrd(2));

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedBytesTwoDocumentsMerged()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(NewField("id", "0", StringField.TYPE_STORED));
		doc.add(new SortedDocValuesField("dv", new BytesRef("hello world 1")));
		iwriter.AddDocument(doc);
		iwriter.Commit();
		doc = new Document();
		doc.add(NewField("id", "1", StringField.TYPE_STORED));
		doc.add(new SortedDocValuesField("dv", new BytesRef("hello world 2")));
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		SortedDocValues dv = ireader.leaves().get(0).reader().getSortedDocValues("dv");
		AssertEquals(2, dv.ValueCount); // 2 ords
		BytesRef scratch = new BytesRef();
		dv.lookupOrd(0, scratch);
		AssertEquals(new BytesRef("hello world 1"), scratch);
		dv.lookupOrd(1, scratch);
		AssertEquals(new BytesRef("hello world 2"), scratch);
		for (int i = 0;i < 2;i++)
		{
		  Document doc2 = ireader.leaves().get(0).reader().document(i);
		  string expected;
		  if (doc2.get("id").Equals("0"))
		  {
			expected = "hello world 1";
		  }
		  else
		  {
			expected = "hello world 2";
		  }
		  dv.lookupOrd(dv.getOrd(i), scratch);
		  AssertEquals(expected, scratch.utf8ToString());
		}

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedMergeAwayAllValues()
	  {
		Directory directory = NewDirectory();
		Analyzer analyzer = new MockAnalyzer(Random());
		IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwconfig.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

		Document doc = new Document();
		doc.add(new StringField("id", "0", Field.Store.NO));
		iwriter.AddDocument(doc);
		doc = new Document();
		doc.add(new StringField("id", "1", Field.Store.NO));
		doc.add(new SortedDocValuesField("field", new BytesRef("hello")));
		iwriter.AddDocument(doc);
		iwriter.Commit();
		iwriter.DeleteDocuments(new Term("id", "1"));
		iwriter.ForceMerge(1);

		DirectoryReader ireader = iwriter.Reader;
		iwriter.Close();

		SortedDocValues dv = GetOnlySegmentReader(ireader).getSortedDocValues("field");
		if (DefaultCodecSupportsDocsWithField())
		{
		  AssertEquals(-1, dv.getOrd(0));
		  AssertEquals(0, dv.ValueCount);
		}
		else
		{
		  AssertEquals(0, dv.getOrd(0));
		  AssertEquals(1, dv.ValueCount);
		  BytesRef @ref = new BytesRef();
		  dv.lookupOrd(0, @ref);
		  AssertEquals(new BytesRef(), @ref);
		}

		ireader.close();
		directory.close();
	  }

	  public virtual void TestBytesWithNewline()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new BinaryDocValuesField("dv", new BytesRef("hello\nworld\r1")));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		BinaryDocValues dv = ireader.leaves().get(0).reader().getBinaryDocValues("dv");
		BytesRef scratch = new BytesRef();
		dv.get(0, scratch);
		AssertEquals(new BytesRef("hello\nworld\r1"), scratch);

		ireader.close();
		directory.close();
	  }

	  public virtual void TestMissingSortedBytes()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("hello world 2")));
		iwriter.AddDocument(doc);
		// 2nd doc missing the DV field
		iwriter.AddDocument(new Document());
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		SortedDocValues dv = ireader.leaves().get(0).reader().getSortedDocValues("dv");
		BytesRef scratch = new BytesRef();
		dv.lookupOrd(dv.getOrd(0), scratch);
		AssertEquals(new BytesRef("hello world 2"), scratch);
		if (DefaultCodecSupportsDocsWithField())
		{
		  AssertEquals(-1, dv.getOrd(1));
		}
		dv.get(1, scratch);
		AssertEquals(new BytesRef(""), scratch);
		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedTermsEnum()
	  {
		Directory directory = NewDirectory();
		Analyzer analyzer = new MockAnalyzer(Random());
		IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwconfig.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

		Document doc = new Document();
		doc.add(new SortedDocValuesField("field", new BytesRef("hello")));
		iwriter.AddDocument(doc);

		doc = new Document();
		doc.add(new SortedDocValuesField("field", new BytesRef("world")));
		iwriter.AddDocument(doc);

		doc = new Document();
		doc.add(new SortedDocValuesField("field", new BytesRef("beer")));
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);

		DirectoryReader ireader = iwriter.Reader;
		iwriter.Close();

		SortedDocValues dv = GetOnlySegmentReader(ireader).getSortedDocValues("field");
		AssertEquals(3, dv.ValueCount);

		TermsEnum termsEnum = dv.termsEnum();

		// next()
		AssertEquals("beer", termsEnum.next().utf8ToString());
		AssertEquals(0, termsEnum.ord());
		AssertEquals("hello", termsEnum.next().utf8ToString());
		AssertEquals(1, termsEnum.ord());
		AssertEquals("world", termsEnum.next().utf8ToString());
		AssertEquals(2, termsEnum.ord());

		// seekCeil()
		AssertEquals(SeekStatus.NOT_FOUND, termsEnum.seekCeil(new BytesRef("ha!")));
		AssertEquals("hello", termsEnum.term().utf8ToString());
		AssertEquals(1, termsEnum.ord());
		AssertEquals(SeekStatus.FOUND, termsEnum.seekCeil(new BytesRef("beer")));
		AssertEquals("beer", termsEnum.term().utf8ToString());
		AssertEquals(0, termsEnum.ord());
		AssertEquals(SeekStatus.END, termsEnum.seekCeil(new BytesRef("zzz")));

		// seekExact()
		Assert.IsTrue(termsEnum.seekExact(new BytesRef("beer")));
		AssertEquals("beer", termsEnum.term().utf8ToString());
		AssertEquals(0, termsEnum.ord());
		Assert.IsTrue(termsEnum.seekExact(new BytesRef("hello")));
		AssertEquals(Codec.Default.ToString(), "hello", termsEnum.term().utf8ToString());
		AssertEquals(1, termsEnum.ord());
		Assert.IsTrue(termsEnum.seekExact(new BytesRef("world")));
		AssertEquals("world", termsEnum.term().utf8ToString());
		AssertEquals(2, termsEnum.ord());
		Assert.IsFalse(termsEnum.seekExact(new BytesRef("bogus")));

		// seek(ord)
		termsEnum.seekExact(0);
		AssertEquals("beer", termsEnum.term().utf8ToString());
		AssertEquals(0, termsEnum.ord());
		termsEnum.seekExact(1);
		AssertEquals("hello", termsEnum.term().utf8ToString());
		AssertEquals(1, termsEnum.ord());
		termsEnum.seekExact(2);
		AssertEquals("world", termsEnum.term().utf8ToString());
		AssertEquals(2, termsEnum.ord());
		ireader.close();
		directory.close();
	  }

	  public virtual void TestEmptySortedBytes()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("")));
		iwriter.AddDocument(doc);
		doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("")));
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		SortedDocValues dv = ireader.leaves().get(0).reader().getSortedDocValues("dv");
		BytesRef scratch = new BytesRef();
		AssertEquals(0, dv.getOrd(0));
		AssertEquals(0, dv.getOrd(1));
		dv.lookupOrd(dv.getOrd(0), scratch);
		AssertEquals("", scratch.utf8ToString());

		ireader.close();
		directory.close();
	  }

	  public virtual void TestEmptyBytes()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new BinaryDocValuesField("dv", new BytesRef("")));
		iwriter.AddDocument(doc);
		doc = new Document();
		doc.add(new BinaryDocValuesField("dv", new BytesRef("")));
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		BinaryDocValues dv = ireader.leaves().get(0).reader().getBinaryDocValues("dv");
		BytesRef scratch = new BytesRef();
		dv.get(0, scratch);
		AssertEquals("", scratch.utf8ToString());
		dv.get(1, scratch);
		AssertEquals("", scratch.utf8ToString());

		ireader.close();
		directory.close();
	  }

	  public virtual void TestVeryLargeButLegalBytes()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		sbyte[] bytes = new sbyte[32766];
		BytesRef b = new BytesRef(bytes);
		Random().nextBytes(bytes);
		doc.add(new BinaryDocValuesField("dv", b));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		BinaryDocValues dv = ireader.leaves().get(0).reader().getBinaryDocValues("dv");
		BytesRef scratch = new BytesRef();
		dv.get(0, scratch);
		AssertEquals(new BytesRef(bytes), scratch);

		ireader.close();
		directory.close();
	  }

	  public virtual void TestVeryLargeButLegalSortedBytes()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		sbyte[] bytes = new sbyte[32766];
		BytesRef b = new BytesRef(bytes);
		Random().nextBytes(bytes);
		doc.add(new SortedDocValuesField("dv", b));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		BinaryDocValues dv = ireader.leaves().get(0).reader().getSortedDocValues("dv");
		BytesRef scratch = new BytesRef();
		dv.get(0, scratch);
		AssertEquals(new BytesRef(bytes), scratch);
		ireader.close();
		directory.close();
	  }

	  public virtual void TestCodecUsesOwnBytes()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new BinaryDocValuesField("dv", new BytesRef("boo!")));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		BinaryDocValues dv = ireader.leaves().get(0).reader().getBinaryDocValues("dv");
		sbyte[] mybytes = new sbyte[20];
		BytesRef scratch = new BytesRef(mybytes);
		dv.get(0, scratch);
		AssertEquals("boo!", scratch.utf8ToString());
		Assert.IsFalse(scratch.bytes == mybytes);

		ireader.close();
		directory.close();
	  }

	  public virtual void TestCodecUsesOwnSortedBytes()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("boo!")));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		BinaryDocValues dv = ireader.leaves().get(0).reader().getSortedDocValues("dv");
		sbyte[] mybytes = new sbyte[20];
		BytesRef scratch = new BytesRef(mybytes);
		dv.get(0, scratch);
		AssertEquals("boo!", scratch.utf8ToString());
		Assert.IsFalse(scratch.bytes == mybytes);

		ireader.close();
		directory.close();
	  }

	  public virtual void TestCodecUsesOwnBytesEachTime()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new BinaryDocValuesField("dv", new BytesRef("foo!")));
		iwriter.AddDocument(doc);
		doc = new Document();
		doc.add(new BinaryDocValuesField("dv", new BytesRef("bar!")));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		BinaryDocValues dv = ireader.leaves().get(0).reader().getBinaryDocValues("dv");
		BytesRef scratch = new BytesRef();
		dv.get(0, scratch);
		AssertEquals("foo!", scratch.utf8ToString());

		BytesRef scratch2 = new BytesRef();
		dv.get(1, scratch2);
		AssertEquals("bar!", scratch2.utf8ToString());
		// check scratch is still valid
		AssertEquals("foo!", scratch.utf8ToString());

		ireader.close();
		directory.close();
	  }

	  public virtual void TestCodecUsesOwnSortedBytesEachTime()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());

		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("foo!")));
		iwriter.AddDocument(doc);
		doc = new Document();
		doc.add(new SortedDocValuesField("dv", new BytesRef("bar!")));
		iwriter.AddDocument(doc);
		iwriter.Close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		Debug.Assert(ireader.leaves().size() == 1);
		BinaryDocValues dv = ireader.leaves().get(0).reader().getSortedDocValues("dv");
		BytesRef scratch = new BytesRef();
		dv.get(0, scratch);
		AssertEquals("foo!", scratch.utf8ToString());

		BytesRef scratch2 = new BytesRef();
		dv.get(1, scratch2);
		AssertEquals("bar!", scratch2.utf8ToString());
		// check scratch is still valid
		AssertEquals("foo!", scratch.utf8ToString());

		ireader.close();
		directory.close();
	  }

	  /*
	   * Simple test case to show how to use the API
	   */
	  public virtual void TestDocValuesSimple()
	  {
		Directory dir = NewDirectory();
		Analyzer analyzer = new MockAnalyzer(Random());
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		conf.MergePolicy = NewLogMergePolicy();
		IndexWriter writer = new IndexWriter(dir, conf);
		for (int i = 0; i < 5; i++)
		{
		  Document doc = new Document();
		  doc.add(new NumericDocValuesField("docId", i));
		  doc.add(new TextField("docId", "" + i, Field.Store.NO));
		  writer.addDocument(doc);
		}
		writer.commit();
		writer.forceMerge(1, true);

		writer.close(true);

		DirectoryReader reader = DirectoryReader.open(dir, 1);
		AssertEquals(1, reader.leaves().size());

		IndexSearcher searcher = new IndexSearcher(reader);

		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term("docId", "0")), BooleanClause.Occur.SHOULD);
		query.add(new TermQuery(new Term("docId", "1")), BooleanClause.Occur.SHOULD);
		query.add(new TermQuery(new Term("docId", "2")), BooleanClause.Occur.SHOULD);
		query.add(new TermQuery(new Term("docId", "3")), BooleanClause.Occur.SHOULD);
		query.add(new TermQuery(new Term("docId", "4")), BooleanClause.Occur.SHOULD);

		TopDocs search = searcher.search(query, 10);
		AssertEquals(5, search.totalHits);
		ScoreDoc[] scoreDocs = search.scoreDocs;
		NumericDocValues docValues = GetOnlySegmentReader(reader).getNumericDocValues("docId");
		for (int i = 0; i < scoreDocs.Length; i++)
		{
		  AssertEquals(i, scoreDocs[i].doc);
		  AssertEquals(i, docValues.get(scoreDocs[i].doc));
		}
		reader.close();
		dir.close();
	  }

	  public virtual void TestRandomSortedBytes()
	  {
		Directory dir = NewDirectory();
		IndexWriterConfig cfg = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		if (!DefaultCodecSupportsDocsWithField())
		{
		  // if the codec doesnt support missing, we expect missing to be mapped to byte[]
		  // by the impersonator, but we have to give it a chance to merge them to this
		  cfg.MergePolicy = NewLogMergePolicy();
		}
		RandomIndexWriter w = new RandomIndexWriter(Random(), dir, cfg);
		int numDocs = AtLeast(100);
		BytesRefHash hash = new BytesRefHash();
		IDictionary<string, string> docToString = new Dictionary<string, string>();
		int maxLength = TestUtil.NextInt(Random(), 1, 50);
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  doc.add(NewTextField("id", "" + i, Field.Store.YES));
		  string @string = TestUtil.RandomRealisticUnicodeString(Random(), 1, maxLength);
		  BytesRef br = new BytesRef(@string);
		  doc.add(new SortedDocValuesField("field", br));
		  hash.add(br);
		  docToString["" + i] = @string;
		  w.AddDocument(doc);
		}
		if (Rarely())
		{
		  w.Commit();
		}
		int numDocsNoValue = AtLeast(10);
		for (int i = 0; i < numDocsNoValue; i++)
		{
		  Document doc = new Document();
		  doc.add(NewTextField("id", "noValue", Field.Store.YES));
		  w.AddDocument(doc);
		}
		if (!DefaultCodecSupportsDocsWithField())
		{
		  BytesRef bytesRef = new BytesRef();
		  hash.add(bytesRef); // add empty value for the gaps
		}
		if (Rarely())
		{
		  w.Commit();
		}
		if (!DefaultCodecSupportsDocsWithField())
		{
		  // if the codec doesnt support missing, we expect missing to be mapped to byte[]
		  // by the impersonator, but we have to give it a chance to merge them to this
		  w.ForceMerge(1);
		}
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  string id = "" + i + numDocs;
		  doc.add(NewTextField("id", id, Field.Store.YES));
		  string @string = TestUtil.RandomRealisticUnicodeString(Random(), 1, maxLength);
		  BytesRef br = new BytesRef(@string);
		  hash.add(br);
		  docToString[id] = @string;
		  doc.add(new SortedDocValuesField("field", br));
		  w.AddDocument(doc);
		}
		w.Commit();
		IndexReader reader = w.Reader;
		SortedDocValues docValues = MultiDocValues.getSortedValues(reader, "field");
		int[] sort = hash.sort(BytesRef.UTF8SortedAsUnicodeComparator);
		BytesRef expected = new BytesRef();
		BytesRef actual = new BytesRef();
		AssertEquals(hash.size(), docValues.ValueCount);
		for (int i = 0; i < hash.size(); i++)
		{
		  hash.get(sort[i], expected);
		  docValues.lookupOrd(i, actual);
		  AssertEquals(expected.utf8ToString(), actual.utf8ToString());
		  int ord = docValues.lookupTerm(expected);
		  AssertEquals(i, ord);
		}
		AtomicReader slowR = SlowCompositeReaderWrapper.wrap(reader);
//JAVA TO C# CONVERTER TODO TASK: There is no .NET Dictionary equivalent to the Java 'entrySet' method:
		Set<KeyValuePair<string, string>> entrySet = docToString.entrySet();

		foreach (KeyValuePair<string, string> entry in entrySet)
		{
		  // pk lookup
		  DocsEnum termDocsEnum = slowR.termDocsEnum(new Term("id", entry.Key));
		  int docId = termDocsEnum.nextDoc();
		  expected = new BytesRef(entry.Value);
		  docValues.get(docId, actual);
		  AssertEquals(expected, actual);
		}

		reader.close();
		w.Close();
		dir.close();
	  }

	  internal abstract class LongProducer
	  {
		internal abstract long Next();
	  }

	  private void DoTestNumericsVsStoredFields(long minValue, long maxValue)
	  {
		DoTestNumericsVsStoredFields(new LongProducerAnonymousInnerClassHelper(this, minValue, maxValue));
	  }

	  private class LongProducerAnonymousInnerClassHelper : LongProducer
	  {
		  private readonly BaseDocValuesFormatTestCase OuterInstance;

		  private long MinValue;
		  private long MaxValue;

		  public LongProducerAnonymousInnerClassHelper(BaseDocValuesFormatTestCase outerInstance, long minValue, long maxValue)
		  {
			  this.OuterInstance = outerInstance;
			  this.MinValue = minValue;
			  this.MaxValue = maxValue;
		  }

		  internal override long Next()
		  {
			return TestUtil.NextLong(Random(), MinValue, MaxValue);
		  }
	  }

	  private void DoTestNumericsVsStoredFields(LongProducer longs)
	  {
		Directory dir = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
		Document doc = new Document();
		Field idField = new StringField("id", "", Field.Store.NO);
		Field storedField = NewStringField("stored", "", Field.Store.YES);
		Field dvField = new NumericDocValuesField("dv", 0);
		doc.add(idField);
		doc.add(storedField);
		doc.add(dvField);

		// index some docs
		int numDocs = AtLeast(300);
		// numDocs should be always > 256 so that in case of a codec that optimizes
		// for numbers of values <= 256, all storage layouts are tested
		Debug.Assert(numDocs > 256);
		for (int i = 0; i < numDocs; i++)
		{
		  idField.StringValue = Convert.ToString(i);
		  long value = longs.Next();
		  storedField.StringValue = Convert.ToString(value);
		  dvField.LongValue = value;
		  writer.AddDocument(doc);
		  if (Random().Next(31) == 0)
		  {
			writer.Commit();
		  }
		}

		// delete some docs
		int numDeletions = Random().Next(numDocs / 10);
		for (int i = 0; i < numDeletions; i++)
		{
		  int id = Random().Next(numDocs);
		  writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
		}

		// merge some segments and ensure that at least one of them has more than
		// 256 values
		writer.ForceMerge(numDocs / 256);

		writer.Close();

		// compare
		DirectoryReader ir = DirectoryReader.open(dir);
		foreach (AtomicReaderContext context in ir.leaves())
		{
		  AtomicReader r = context.reader();
		  NumericDocValues docValues = r.getNumericDocValues("dv");
		  for (int i = 0; i < r.maxDoc(); i++)
		  {
			long storedValue = Convert.ToInt64(r.document(i).get("stored"));
			AssertEquals(storedValue, docValues.get(i));
		  }
		}
		ir.close();
		dir.close();
	  }

	  private void DoTestMissingVsFieldCache(long minValue, long maxValue)
	  {
		DoTestMissingVsFieldCache(new LongProducerAnonymousInnerClassHelper2(this, minValue, maxValue));
	  }

	  private class LongProducerAnonymousInnerClassHelper2 : LongProducer
	  {
		  private readonly BaseDocValuesFormatTestCase OuterInstance;

		  private long MinValue;
		  private long MaxValue;

		  public LongProducerAnonymousInnerClassHelper2(BaseDocValuesFormatTestCase outerInstance, long minValue, long maxValue)
		  {
			  this.OuterInstance = outerInstance;
			  this.MinValue = minValue;
			  this.MaxValue = maxValue;
		  }

		  internal override long Next()
		  {
			return TestUtil.NextLong(Random(), MinValue, MaxValue);
		  }
	  }

	  private void DoTestMissingVsFieldCache(LongProducer longs)
	  {
		AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField());
		Directory dir = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
		Field idField = new StringField("id", "", Field.Store.NO);
		Field indexedField = NewStringField("indexed", "", Field.Store.NO);
		Field dvField = new NumericDocValuesField("dv", 0);


		// index some docs
		int numDocs = AtLeast(300);
		// numDocs should be always > 256 so that in case of a codec that optimizes
		// for numbers of values <= 256, all storage layouts are tested
		Debug.Assert(numDocs > 256);
		for (int i = 0; i < numDocs; i++)
		{
		  idField.StringValue = Convert.ToString(i);
		  long value = longs.Next();
		  indexedField.StringValue = Convert.ToString(value);
		  dvField.LongValue = value;
		  Document doc = new Document();
		  doc.add(idField);
		  // 1/4 of the time we neglect to add the fields
		  if (Random().Next(4) > 0)
		  {
			doc.add(indexedField);
			doc.add(dvField);
		  }
		  writer.AddDocument(doc);
		  if (Random().Next(31) == 0)
		  {
			writer.Commit();
		  }
		}

		// delete some docs
		int numDeletions = Random().Next(numDocs / 10);
		for (int i = 0; i < numDeletions; i++)
		{
		  int id = Random().Next(numDocs);
		  writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
		}

		// merge some segments and ensure that at least one of them has more than
		// 256 values
		writer.ForceMerge(numDocs / 256);

		writer.Close();

		// compare
		DirectoryReader ir = DirectoryReader.open(dir);
		foreach (AtomicReaderContext context in ir.leaves())
		{
		  AtomicReader r = context.reader();
		  Bits expected = FieldCache.DEFAULT.getDocsWithField(r, "indexed");
		  Bits actual = FieldCache.DEFAULT.getDocsWithField(r, "dv");
		  AssertEquals(expected, actual);
		}
		ir.close();
		dir.close();
	  }

	  public virtual void TestBooleanNumericsVsStoredFields()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  DoTestNumericsVsStoredFields(0, 1);
		}
	  }

	  public virtual void TestByteNumericsVsStoredFields()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  DoTestNumericsVsStoredFields(sbyte.MinValue, sbyte.MaxValue);
		}
	  }

	  public virtual void TestByteMissingVsFieldCache()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  DoTestMissingVsFieldCache(sbyte.MinValue, sbyte.MaxValue);
		}
	  }

	  public virtual void TestShortNumericsVsStoredFields()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  DoTestNumericsVsStoredFields(short.MinValue, short.MaxValue);
		}
	  }

	  public virtual void TestShortMissingVsFieldCache()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  DoTestMissingVsFieldCache(short.MinValue, short.MaxValue);
		}
	  }

	  public virtual void TestIntNumericsVsStoredFields()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  DoTestNumericsVsStoredFields(int.MinValue, int.MaxValue);
		}
	  }

	  public virtual void TestIntMissingVsFieldCache()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  DoTestMissingVsFieldCache(int.MinValue, int.MaxValue);
		}
	  }

	  public virtual void TestLongNumericsVsStoredFields()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  DoTestNumericsVsStoredFields(long.MinValue, long.MaxValue);
		}
	  }

	  public virtual void TestLongMissingVsFieldCache()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  DoTestMissingVsFieldCache(long.MinValue, long.MaxValue);
		}
	  }

	  private void DoTestBinaryVsStoredFields(int minLength, int maxLength)
	  {
		Directory dir = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
		Document doc = new Document();
		Field idField = new StringField("id", "", Field.Store.NO);
		Field storedField = new StoredField("stored", new sbyte[0]);
		Field dvField = new BinaryDocValuesField("dv", new BytesRef());
		doc.add(idField);
		doc.add(storedField);
		doc.add(dvField);

		// index some docs
		int numDocs = AtLeast(300);
		for (int i = 0; i < numDocs; i++)
		{
		  idField.StringValue = Convert.ToString(i);
		  int length;
		  if (minLength == maxLength)
		  {
			length = minLength; // fixed length
		  }
		  else
		  {
			length = TestUtil.NextInt(Random(), minLength, maxLength);
		  }
		  sbyte[] buffer = new sbyte[length];
		  Random().nextBytes(buffer);
		  storedField.BytesValue = buffer;
		  dvField.BytesValue = buffer;
		  writer.AddDocument(doc);
		  if (Random().Next(31) == 0)
		  {
			writer.Commit();
		  }
		}

		// delete some docs
		int numDeletions = Random().Next(numDocs / 10);
		for (int i = 0; i < numDeletions; i++)
		{
		  int id = Random().Next(numDocs);
		  writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
		}
		writer.Close();

		// compare
		DirectoryReader ir = DirectoryReader.open(dir);
		foreach (AtomicReaderContext context in ir.leaves())
		{
		  AtomicReader r = context.reader();
		  BinaryDocValues docValues = r.getBinaryDocValues("dv");
		  for (int i = 0; i < r.maxDoc(); i++)
		  {
			BytesRef binaryValue = r.document(i).getBinaryValue("stored");
			BytesRef scratch = new BytesRef();
			docValues.get(i, scratch);
			AssertEquals(binaryValue, scratch);
		  }
		}
		ir.close();
		dir.close();
	  }

	  public virtual void TestBinaryFixedLengthVsStoredFields()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  int fixedLength = TestUtil.NextInt(Random(), 0, 10);
		  DoTestBinaryVsStoredFields(fixedLength, fixedLength);
		}
	  }

	  public virtual void TestBinaryVariableLengthVsStoredFields()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  DoTestBinaryVsStoredFields(0, 10);
		}
	  }

	  private void DoTestSortedVsStoredFields(int minLength, int maxLength)
	  {
		Directory dir = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
		Document doc = new Document();
		Field idField = new StringField("id", "", Field.Store.NO);
		Field storedField = new StoredField("stored", new sbyte[0]);
		Field dvField = new SortedDocValuesField("dv", new BytesRef());
		doc.add(idField);
		doc.add(storedField);
		doc.add(dvField);

		// index some docs
		int numDocs = AtLeast(300);
		for (int i = 0; i < numDocs; i++)
		{
		  idField.StringValue = Convert.ToString(i);
		  int length;
		  if (minLength == maxLength)
		  {
			length = minLength; // fixed length
		  }
		  else
		  {
			length = TestUtil.NextInt(Random(), minLength, maxLength);
		  }
		  sbyte[] buffer = new sbyte[length];
		  Random().nextBytes(buffer);
		  storedField.BytesValue = buffer;
		  dvField.BytesValue = buffer;
		  writer.AddDocument(doc);
		  if (Random().Next(31) == 0)
		  {
			writer.Commit();
		  }
		}

		// delete some docs
		int numDeletions = Random().Next(numDocs / 10);
		for (int i = 0; i < numDeletions; i++)
		{
		  int id = Random().Next(numDocs);
		  writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
		}
		writer.Close();

		// compare
		DirectoryReader ir = DirectoryReader.open(dir);
		foreach (AtomicReaderContext context in ir.leaves())
		{
		  AtomicReader r = context.reader();
		  BinaryDocValues docValues = r.getSortedDocValues("dv");
		  for (int i = 0; i < r.maxDoc(); i++)
		  {
			BytesRef binaryValue = r.document(i).getBinaryValue("stored");
			BytesRef scratch = new BytesRef();
			docValues.get(i, scratch);
			AssertEquals(binaryValue, scratch);
		  }
		}
		ir.close();
		dir.close();
	  }

	  private void DoTestSortedVsFieldCache(int minLength, int maxLength)
	  {
		Directory dir = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
		Document doc = new Document();
		Field idField = new StringField("id", "", Field.Store.NO);
		Field indexedField = new StringField("indexed", "", Field.Store.NO);
		Field dvField = new SortedDocValuesField("dv", new BytesRef());
		doc.add(idField);
		doc.add(indexedField);
		doc.add(dvField);

		// index some docs
		int numDocs = AtLeast(300);
		for (int i = 0; i < numDocs; i++)
		{
		  idField.StringValue = Convert.ToString(i);
		  int length;
		  if (minLength == maxLength)
		  {
			length = minLength; // fixed length
		  }
		  else
		  {
			length = TestUtil.NextInt(Random(), minLength, maxLength);
		  }
		  string value = TestUtil.RandomSimpleString(Random(), length);
		  indexedField.StringValue = value;
		  dvField.BytesValue = new BytesRef(value);
		  writer.AddDocument(doc);
		  if (Random().Next(31) == 0)
		  {
			writer.Commit();
		  }
		}

		// delete some docs
		int numDeletions = Random().Next(numDocs / 10);
		for (int i = 0; i < numDeletions; i++)
		{
		  int id = Random().Next(numDocs);
		  writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
		}
		writer.Close();

		// compare
		DirectoryReader ir = DirectoryReader.open(dir);
		foreach (AtomicReaderContext context in ir.leaves())
		{
		  AtomicReader r = context.reader();
		  SortedDocValues expected = FieldCache.DEFAULT.getTermsIndex(r, "indexed");
		  SortedDocValues actual = r.getSortedDocValues("dv");
		  AssertEquals(r.maxDoc(), expected, actual);
		}
		ir.close();
		dir.close();
	  }

	  public virtual void TestSortedFixedLengthVsStoredFields()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  int fixedLength = TestUtil.NextInt(Random(), 1, 10);
		  DoTestSortedVsStoredFields(fixedLength, fixedLength);
		}
	  }

	  public virtual void TestSortedFixedLengthVsFieldCache()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  int fixedLength = TestUtil.NextInt(Random(), 1, 10);
		  DoTestSortedVsFieldCache(fixedLength, fixedLength);
		}
	  }

	  public virtual void TestSortedVariableLengthVsFieldCache()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  DoTestSortedVsFieldCache(1, 10);
		}
	  }

	  public virtual void TestSortedVariableLengthVsStoredFields()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  DoTestSortedVsStoredFields(1, 10);
		}
	  }

	  public virtual void TestSortedSetOneValue()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		Directory directory = NewDirectory();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);

		Document doc = new Document();
		doc.add(new SortedSetDocValuesField("field", new BytesRef("hello")));
		iwriter.AddDocument(doc);

		DirectoryReader ireader = iwriter.Reader;
		iwriter.Close();

		SortedSetDocValues dv = GetOnlySegmentReader(ireader).getSortedSetDocValues("field");

		dv.Document = 0;
		AssertEquals(0, dv.nextOrd());
		AssertEquals(NO_MORE_ORDS, dv.nextOrd());

		BytesRef bytes = new BytesRef();
		dv.lookupOrd(0, bytes);
		AssertEquals(new BytesRef("hello"), bytes);

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedSetTwoFields()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		Directory directory = NewDirectory();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);

		Document doc = new Document();
		doc.add(new SortedSetDocValuesField("field", new BytesRef("hello")));
		doc.add(new SortedSetDocValuesField("field2", new BytesRef("world")));
		iwriter.AddDocument(doc);

		DirectoryReader ireader = iwriter.Reader;
		iwriter.Close();

		SortedSetDocValues dv = GetOnlySegmentReader(ireader).getSortedSetDocValues("field");

		dv.Document = 0;
		AssertEquals(0, dv.nextOrd());
		AssertEquals(NO_MORE_ORDS, dv.nextOrd());

		BytesRef bytes = new BytesRef();
		dv.lookupOrd(0, bytes);
		AssertEquals(new BytesRef("hello"), bytes);

		dv = GetOnlySegmentReader(ireader).getSortedSetDocValues("field2");

		dv.Document = 0;
		AssertEquals(0, dv.nextOrd());
		AssertEquals(NO_MORE_ORDS, dv.nextOrd());

		dv.lookupOrd(0, bytes);
		AssertEquals(new BytesRef("world"), bytes);

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedSetTwoDocumentsMerged()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		Directory directory = NewDirectory();
		Analyzer analyzer = new MockAnalyzer(Random());
		IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwconfig.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

		Document doc = new Document();
		doc.add(new SortedSetDocValuesField("field", new BytesRef("hello")));
		iwriter.AddDocument(doc);
		iwriter.Commit();

		doc = new Document();
		doc.add(new SortedSetDocValuesField("field", new BytesRef("world")));
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);

		DirectoryReader ireader = iwriter.Reader;
		iwriter.Close();

		SortedSetDocValues dv = GetOnlySegmentReader(ireader).getSortedSetDocValues("field");
		AssertEquals(2, dv.ValueCount);

		dv.Document = 0;
		AssertEquals(0, dv.nextOrd());
		AssertEquals(NO_MORE_ORDS, dv.nextOrd());

		BytesRef bytes = new BytesRef();
		dv.lookupOrd(0, bytes);
		AssertEquals(new BytesRef("hello"), bytes);

		dv.Document = 1;
		AssertEquals(1, dv.nextOrd());
		AssertEquals(NO_MORE_ORDS, dv.nextOrd());

		dv.lookupOrd(1, bytes);
		AssertEquals(new BytesRef("world"), bytes);

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedSetTwoValues()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		Directory directory = NewDirectory();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);

		Document doc = new Document();
		doc.add(new SortedSetDocValuesField("field", new BytesRef("hello")));
		doc.add(new SortedSetDocValuesField("field", new BytesRef("world")));
		iwriter.AddDocument(doc);

		DirectoryReader ireader = iwriter.Reader;
		iwriter.Close();

		SortedSetDocValues dv = GetOnlySegmentReader(ireader).getSortedSetDocValues("field");

		dv.Document = 0;
		AssertEquals(0, dv.nextOrd());
		AssertEquals(1, dv.nextOrd());
		AssertEquals(NO_MORE_ORDS, dv.nextOrd());

		BytesRef bytes = new BytesRef();
		dv.lookupOrd(0, bytes);
		AssertEquals(new BytesRef("hello"), bytes);

		dv.lookupOrd(1, bytes);
		AssertEquals(new BytesRef("world"), bytes);

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedSetTwoValuesUnordered()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		Directory directory = NewDirectory();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory);

		Document doc = new Document();
		doc.add(new SortedSetDocValuesField("field", new BytesRef("world")));
		doc.add(new SortedSetDocValuesField("field", new BytesRef("hello")));
		iwriter.AddDocument(doc);

		DirectoryReader ireader = iwriter.Reader;
		iwriter.Close();

		SortedSetDocValues dv = GetOnlySegmentReader(ireader).getSortedSetDocValues("field");

		dv.Document = 0;
		AssertEquals(0, dv.nextOrd());
		AssertEquals(1, dv.nextOrd());
		AssertEquals(NO_MORE_ORDS, dv.nextOrd());

		BytesRef bytes = new BytesRef();
		dv.lookupOrd(0, bytes);
		AssertEquals(new BytesRef("hello"), bytes);

		dv.lookupOrd(1, bytes);
		AssertEquals(new BytesRef("world"), bytes);

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedSetThreeValuesTwoDocs()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		Directory directory = NewDirectory();
		Analyzer analyzer = new MockAnalyzer(Random());
		IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwconfig.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

		Document doc = new Document();
		doc.add(new SortedSetDocValuesField("field", new BytesRef("hello")));
		doc.add(new SortedSetDocValuesField("field", new BytesRef("world")));
		iwriter.AddDocument(doc);
		iwriter.Commit();

		doc = new Document();
		doc.add(new SortedSetDocValuesField("field", new BytesRef("hello")));
		doc.add(new SortedSetDocValuesField("field", new BytesRef("beer")));
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);

		DirectoryReader ireader = iwriter.Reader;
		iwriter.Close();

		SortedSetDocValues dv = GetOnlySegmentReader(ireader).getSortedSetDocValues("field");
		AssertEquals(3, dv.ValueCount);

		dv.Document = 0;
		AssertEquals(1, dv.nextOrd());
		AssertEquals(2, dv.nextOrd());
		AssertEquals(NO_MORE_ORDS, dv.nextOrd());

		dv.Document = 1;
		AssertEquals(0, dv.nextOrd());
		AssertEquals(1, dv.nextOrd());
		AssertEquals(NO_MORE_ORDS, dv.nextOrd());

		BytesRef bytes = new BytesRef();
		dv.lookupOrd(0, bytes);
		AssertEquals(new BytesRef("beer"), bytes);

		dv.lookupOrd(1, bytes);
		AssertEquals(new BytesRef("hello"), bytes);

		dv.lookupOrd(2, bytes);
		AssertEquals(new BytesRef("world"), bytes);

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedSetTwoDocumentsLastMissing()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		Directory directory = NewDirectory();
		Analyzer analyzer = new MockAnalyzer(Random());
		IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwconfig.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

		Document doc = new Document();
		doc.add(new SortedSetDocValuesField("field", new BytesRef("hello")));
		iwriter.AddDocument(doc);

		doc = new Document();
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);
		DirectoryReader ireader = iwriter.Reader;
		iwriter.Close();

		SortedSetDocValues dv = GetOnlySegmentReader(ireader).getSortedSetDocValues("field");
		AssertEquals(1, dv.ValueCount);

		dv.Document = 0;
		AssertEquals(0, dv.nextOrd());
		AssertEquals(NO_MORE_ORDS, dv.nextOrd());

		BytesRef bytes = new BytesRef();
		dv.lookupOrd(0, bytes);
		AssertEquals(new BytesRef("hello"), bytes);

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedSetTwoDocumentsLastMissingMerge()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		Directory directory = NewDirectory();
		Analyzer analyzer = new MockAnalyzer(Random());
		IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwconfig.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

		Document doc = new Document();
		doc.add(new SortedSetDocValuesField("field", new BytesRef("hello")));
		iwriter.AddDocument(doc);
		iwriter.Commit();

		doc = new Document();
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);

		DirectoryReader ireader = iwriter.Reader;
		iwriter.Close();

		SortedSetDocValues dv = GetOnlySegmentReader(ireader).getSortedSetDocValues("field");
		AssertEquals(1, dv.ValueCount);

		dv.Document = 0;
		AssertEquals(0, dv.nextOrd());
		AssertEquals(NO_MORE_ORDS, dv.nextOrd());

		BytesRef bytes = new BytesRef();
		dv.lookupOrd(0, bytes);
		AssertEquals(new BytesRef("hello"), bytes);

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedSetTwoDocumentsFirstMissing()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		Directory directory = NewDirectory();
		Analyzer analyzer = new MockAnalyzer(Random());
		IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwconfig.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

		Document doc = new Document();
		iwriter.AddDocument(doc);

		doc = new Document();
		doc.add(new SortedSetDocValuesField("field", new BytesRef("hello")));
		iwriter.AddDocument(doc);

		iwriter.ForceMerge(1);
		DirectoryReader ireader = iwriter.Reader;
		iwriter.Close();

		SortedSetDocValues dv = GetOnlySegmentReader(ireader).getSortedSetDocValues("field");
		AssertEquals(1, dv.ValueCount);

		dv.Document = 1;
		AssertEquals(0, dv.nextOrd());
		AssertEquals(NO_MORE_ORDS, dv.nextOrd());

		BytesRef bytes = new BytesRef();
		dv.lookupOrd(0, bytes);
		AssertEquals(new BytesRef("hello"), bytes);

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedSetTwoDocumentsFirstMissingMerge()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		Directory directory = NewDirectory();
		Analyzer analyzer = new MockAnalyzer(Random());
		IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwconfig.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

		Document doc = new Document();
		iwriter.AddDocument(doc);
		iwriter.Commit();

		doc = new Document();
		doc.add(new SortedSetDocValuesField("field", new BytesRef("hello")));
		iwriter.AddDocument(doc);
		iwriter.ForceMerge(1);

		DirectoryReader ireader = iwriter.Reader;
		iwriter.Close();

		SortedSetDocValues dv = GetOnlySegmentReader(ireader).getSortedSetDocValues("field");
		AssertEquals(1, dv.ValueCount);

		dv.Document = 1;
		AssertEquals(0, dv.nextOrd());
		AssertEquals(NO_MORE_ORDS, dv.nextOrd());

		BytesRef bytes = new BytesRef();
		dv.lookupOrd(0, bytes);
		AssertEquals(new BytesRef("hello"), bytes);

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedSetMergeAwayAllValues()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		Directory directory = NewDirectory();
		Analyzer analyzer = new MockAnalyzer(Random());
		IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwconfig.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

		Document doc = new Document();
		doc.add(new StringField("id", "0", Field.Store.NO));
		iwriter.AddDocument(doc);
		doc = new Document();
		doc.add(new StringField("id", "1", Field.Store.NO));
		doc.add(new SortedSetDocValuesField("field", new BytesRef("hello")));
		iwriter.AddDocument(doc);
		iwriter.Commit();
		iwriter.DeleteDocuments(new Term("id", "1"));
		iwriter.ForceMerge(1);

		DirectoryReader ireader = iwriter.Reader;
		iwriter.Close();

		SortedSetDocValues dv = GetOnlySegmentReader(ireader).getSortedSetDocValues("field");
		AssertEquals(0, dv.ValueCount);

		ireader.close();
		directory.close();
	  }

	  public virtual void TestSortedSetTermsEnum()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		Directory directory = NewDirectory();
		Analyzer analyzer = new MockAnalyzer(Random());
		IndexWriterConfig iwconfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		iwconfig.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iwriter = new RandomIndexWriter(Random(), directory, iwconfig);

		Document doc = new Document();
		doc.add(new SortedSetDocValuesField("field", new BytesRef("hello")));
		doc.add(new SortedSetDocValuesField("field", new BytesRef("world")));
		doc.add(new SortedSetDocValuesField("field", new BytesRef("beer")));
		iwriter.AddDocument(doc);

		DirectoryReader ireader = iwriter.Reader;
		iwriter.Close();

		SortedSetDocValues dv = GetOnlySegmentReader(ireader).getSortedSetDocValues("field");
		AssertEquals(3, dv.ValueCount);

		TermsEnum termsEnum = dv.termsEnum();

		// next()
		AssertEquals("beer", termsEnum.next().utf8ToString());
		AssertEquals(0, termsEnum.ord());
		AssertEquals("hello", termsEnum.next().utf8ToString());
		AssertEquals(1, termsEnum.ord());
		AssertEquals("world", termsEnum.next().utf8ToString());
		AssertEquals(2, termsEnum.ord());

		// seekCeil()
		AssertEquals(SeekStatus.NOT_FOUND, termsEnum.seekCeil(new BytesRef("ha!")));
		AssertEquals("hello", termsEnum.term().utf8ToString());
		AssertEquals(1, termsEnum.ord());
		AssertEquals(SeekStatus.FOUND, termsEnum.seekCeil(new BytesRef("beer")));
		AssertEquals("beer", termsEnum.term().utf8ToString());
		AssertEquals(0, termsEnum.ord());
		AssertEquals(SeekStatus.END, termsEnum.seekCeil(new BytesRef("zzz")));

		// seekExact()
		Assert.IsTrue(termsEnum.seekExact(new BytesRef("beer")));
		AssertEquals("beer", termsEnum.term().utf8ToString());
		AssertEquals(0, termsEnum.ord());
		Assert.IsTrue(termsEnum.seekExact(new BytesRef("hello")));
		AssertEquals("hello", termsEnum.term().utf8ToString());
		AssertEquals(1, termsEnum.ord());
		Assert.IsTrue(termsEnum.seekExact(new BytesRef("world")));
		AssertEquals("world", termsEnum.term().utf8ToString());
		AssertEquals(2, termsEnum.ord());
		Assert.IsFalse(termsEnum.seekExact(new BytesRef("bogus")));

		// seek(ord)
		termsEnum.seekExact(0);
		AssertEquals("beer", termsEnum.term().utf8ToString());
		AssertEquals(0, termsEnum.ord());
		termsEnum.seekExact(1);
		AssertEquals("hello", termsEnum.term().utf8ToString());
		AssertEquals(1, termsEnum.ord());
		termsEnum.seekExact(2);
		AssertEquals("world", termsEnum.term().utf8ToString());
		AssertEquals(2, termsEnum.ord());
		ireader.close();
		directory.close();
	  }

	  private void DoTestSortedSetVsStoredFields(int minLength, int maxLength, int maxValuesPerDoc)
	  {
		Directory dir = NewDirectory();
		IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);

		// index some docs
		int numDocs = AtLeast(300);
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  Field idField = new StringField("id", Convert.ToString(i), Field.Store.NO);
		  doc.add(idField);
		  int length;
		  if (minLength == maxLength)
		  {
			length = minLength; // fixed length
		  }
		  else
		  {
			length = TestUtil.NextInt(Random(), minLength, maxLength);
		  }
		  int numValues = TestUtil.NextInt(Random(), 0, maxValuesPerDoc);
		  // create a random set of strings
		  Set<string> values = new SortedSet<string>();
		  for (int v = 0; v < numValues; v++)
		  {
			values.add(TestUtil.RandomSimpleString(Random(), length));
		  }

		  // add ordered to the stored field
		  foreach (string v in values)
		  {
			doc.add(new StoredField("stored", v));
		  }

		  // add in any order to the dv field
		  List<string> unordered = new List<string>(values);
		  Collections.shuffle(unordered, Random());
		  foreach (string v in unordered)
		  {
			doc.add(new SortedSetDocValuesField("dv", new BytesRef(v)));
		  }

		  writer.AddDocument(doc);
		  if (Random().Next(31) == 0)
		  {
			writer.Commit();
		  }
		}

		// delete some docs
		int numDeletions = Random().Next(numDocs / 10);
		for (int i = 0; i < numDeletions; i++)
		{
		  int id = Random().Next(numDocs);
		  writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
		}
		writer.Close();

		// compare
		DirectoryReader ir = DirectoryReader.open(dir);
		foreach (AtomicReaderContext context in ir.leaves())
		{
		  AtomicReader r = context.reader();
		  SortedSetDocValues docValues = r.getSortedSetDocValues("dv");
		  BytesRef scratch = new BytesRef();
		  for (int i = 0; i < r.maxDoc(); i++)
		  {
			string[] stringValues = r.document(i).getValues("stored");
			if (docValues != null)
			{
			  docValues.Document = i;
			}
			for (int j = 0; j < stringValues.Length; j++)
			{
			  Debug.Assert(docValues != null);
			  long ord = docValues.nextOrd();
			  Debug.Assert(ord != NO_MORE_ORDS);
			  docValues.lookupOrd(ord, scratch);
			  AssertEquals(stringValues[j], scratch.utf8ToString());
			}
			Debug.Assert(docValues == null || docValues.nextOrd() == NO_MORE_ORDS);
		  }
		}
		ir.close();
		dir.close();
	  }

	  public virtual void TestSortedSetFixedLengthVsStoredFields()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  int fixedLength = TestUtil.NextInt(Random(), 1, 10);
		  DoTestSortedSetVsStoredFields(fixedLength, fixedLength, 16);
		}
	  }

	  public virtual void TestSortedSetVariableLengthVsStoredFields()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  DoTestSortedSetVsStoredFields(1, 10, 16);
		}
	  }

	  public virtual void TestSortedSetFixedLengthSingleValuedVsStoredFields()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  int fixedLength = TestUtil.NextInt(Random(), 1, 10);
		  DoTestSortedSetVsStoredFields(fixedLength, fixedLength, 1);
		}
	  }

	  public virtual void TestSortedSetVariableLengthSingleValuedVsStoredFields()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  DoTestSortedSetVsStoredFields(1, 10, 1);
		}
	  }

	  private void AssertEquals(Bits expected, Bits actual)
	  {
		AssertEquals(expected.length(), actual.length());
		for (int i = 0; i < expected.length(); i++)
		{
		  AssertEquals(expected.get(i), actual.get(i));
		}
	  }

	  private void AssertEquals(int maxDoc, SortedDocValues expected, SortedDocValues actual)
	  {
		AssertEquals(maxDoc, new SingletonSortedSetDocValues(expected), new SingletonSortedSetDocValues(actual));
	  }

	  private void AssertEquals(int maxDoc, SortedSetDocValues expected, SortedSetDocValues actual)
	  {
		// can be null for the segment if no docs actually had any SortedDocValues
		// in this case FC.getDocTermsOrds returns EMPTY
		if (actual == null)
		{
		  AssertEquals(DocValues.EMPTY_SORTED_SET, expected);
		  return;
		}
		AssertEquals(expected.ValueCount, actual.ValueCount);
		// compare ord lists
		for (int i = 0; i < maxDoc; i++)
		{
		  expected.Document = i;
		  actual.Document = i;
		  long expectedOrd;
		  while ((expectedOrd = expected.nextOrd()) != NO_MORE_ORDS)
		  {
			AssertEquals(expectedOrd, actual.nextOrd());
		  }
		  AssertEquals(NO_MORE_ORDS, actual.nextOrd());
		}

		// compare ord dictionary
		BytesRef expectedBytes = new BytesRef();
		BytesRef actualBytes = new BytesRef();
		for (long i = 0; i < expected.ValueCount; i++)
		{
		  expected.lookupTerm(expectedBytes);
		  actual.lookupTerm(actualBytes);
		  AssertEquals(expectedBytes, actualBytes);
		}

		// compare termsenum
		AssertEquals(expected.ValueCount, expected.termsEnum(), actual.termsEnum());
	  }

	  private void AssertEquals(long numOrds, TermsEnum expected, TermsEnum actual)
	  {
		BytesRef @ref;

		// sequential next() through all terms
		while ((@ref = expected.next()) != null)
		{
		  AssertEquals(@ref, actual.next());
		  AssertEquals(expected.ord(), actual.ord());
		  AssertEquals(expected.term(), actual.term());
		}
		assertNull(actual.next());

		// sequential seekExact(ord) through all terms
		for (long i = 0; i < numOrds; i++)
		{
		  expected.seekExact(i);
		  actual.seekExact(i);
		  AssertEquals(expected.ord(), actual.ord());
		  AssertEquals(expected.term(), actual.term());
		}

		// sequential seekExact(BytesRef) through all terms
		for (long i = 0; i < numOrds; i++)
		{
		  expected.seekExact(i);
		  Assert.IsTrue(actual.seekExact(expected.term()));
		  AssertEquals(expected.ord(), actual.ord());
		  AssertEquals(expected.term(), actual.term());
		}

		// sequential seekCeil(BytesRef) through all terms
		for (long i = 0; i < numOrds; i++)
		{
		  expected.seekExact(i);
		  AssertEquals(SeekStatus.FOUND, actual.seekCeil(expected.term()));
		  AssertEquals(expected.ord(), actual.ord());
		  AssertEquals(expected.term(), actual.term());
		}

		// random seekExact(ord)
		for (long i = 0; i < numOrds; i++)
		{
		  long randomOrd = TestUtil.NextLong(Random(), 0, numOrds - 1);
		  expected.seekExact(randomOrd);
		  actual.seekExact(randomOrd);
		  AssertEquals(expected.ord(), actual.ord());
		  AssertEquals(expected.term(), actual.term());
		}

		// random seekExact(BytesRef)
		for (long i = 0; i < numOrds; i++)
		{
		  long randomOrd = TestUtil.NextLong(Random(), 0, numOrds - 1);
		  expected.seekExact(randomOrd);
		  actual.seekExact(expected.term());
		  AssertEquals(expected.ord(), actual.ord());
		  AssertEquals(expected.term(), actual.term());
		}

		// random seekCeil(BytesRef)
		for (long i = 0; i < numOrds; i++)
		{
		  BytesRef target = new BytesRef(TestUtil.RandomUnicodeString(Random()));
		  SeekStatus expectedStatus = expected.seekCeil(target);
		  AssertEquals(expectedStatus, actual.seekCeil(target));
		  if (expectedStatus != SeekStatus.END)
		  {
			AssertEquals(expected.ord(), actual.ord());
			AssertEquals(expected.term(), actual.term());
		  }
		}
	  }

	  private void DoTestSortedSetVsUninvertedField(int minLength, int maxLength)
	  {
		Directory dir = NewDirectory();
		IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);

		// index some docs
		int numDocs = AtLeast(300);
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  Field idField = new StringField("id", Convert.ToString(i), Field.Store.NO);
		  doc.add(idField);
		  int length;
		  if (minLength == maxLength)
		  {
			length = minLength; // fixed length
		  }
		  else
		  {
			length = TestUtil.NextInt(Random(), minLength, maxLength);
		  }
		  int numValues = Random().Next(17);
		  // create a random list of strings
		  IList<string> values = new List<string>();
		  for (int v = 0; v < numValues; v++)
		  {
			values.Add(TestUtil.RandomSimpleString(Random(), length));
		  }

		  // add in any order to the indexed field
		  List<string> unordered = new List<string>(values);
		  Collections.shuffle(unordered, Random());
		  foreach (string v in values)
		  {
			doc.add(NewStringField("indexed", v, Field.Store.NO));
		  }

		  // add in any order to the dv field
		  List<string> unordered2 = new List<string>(values);
		  Collections.shuffle(unordered2, Random());
		  foreach (string v in unordered2)
		  {
			doc.add(new SortedSetDocValuesField("dv", new BytesRef(v)));
		  }

		  writer.AddDocument(doc);
		  if (Random().Next(31) == 0)
		  {
			writer.Commit();
		  }
		}

		// delete some docs
		int numDeletions = Random().Next(numDocs / 10);
		for (int i = 0; i < numDeletions; i++)
		{
		  int id = Random().Next(numDocs);
		  writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
		}

		// compare per-segment
		DirectoryReader ir = writer.Reader;
		foreach (AtomicReaderContext context in ir.leaves())
		{
		  AtomicReader r = context.reader();
		  SortedSetDocValues expected = FieldCache.DEFAULT.getDocTermOrds(r, "indexed");
		  SortedSetDocValues actual = r.getSortedSetDocValues("dv");
		  AssertEquals(r.maxDoc(), expected, actual);
		}
		ir.close();

		writer.ForceMerge(1);

		// now compare again after the merge
		ir = writer.Reader;
		AtomicReader ar = GetOnlySegmentReader(ir);
		SortedSetDocValues expected = FieldCache.DEFAULT.getDocTermOrds(ar, "indexed");
		SortedSetDocValues actual = ar.getSortedSetDocValues("dv");
		AssertEquals(ir.maxDoc(), expected, actual);
		ir.close();

		writer.Close();
		dir.close();
	  }

	  public virtual void TestSortedSetFixedLengthVsUninvertedField()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  int fixedLength = TestUtil.NextInt(Random(), 1, 10);
		  DoTestSortedSetVsUninvertedField(fixedLength, fixedLength);
		}
	  }

	  public virtual void TestSortedSetVariableLengthVsUninvertedField()
	  {
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  DoTestSortedSetVsUninvertedField(1, 10);
		}
	  }

	  public virtual void TestGCDCompression()
	  {
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  long min = - (((long) Random().Next(1 << 30)) << 32);
		  long mul = Random().Next() & 0xFFFFFFFFL;
		  LongProducer longs = new LongProducerAnonymousInnerClassHelper3(this, min, mul);
		  DoTestNumericsVsStoredFields(longs);
		}
	  }

	  private class LongProducerAnonymousInnerClassHelper3 : LongProducer
	  {
		  private readonly BaseDocValuesFormatTestCase OuterInstance;

		  private long Min;
		  private long Mul;

		  public LongProducerAnonymousInnerClassHelper3(BaseDocValuesFormatTestCase outerInstance, long min, long mul)
		  {
			  this.OuterInstance = outerInstance;
			  this.Min = min;
			  this.Mul = mul;
		  }

		  internal override long Next()
		  {
			return Min + Mul * Random().Next(1 << 20);
		  }
	  }

	  public virtual void TestZeros()
	  {
		DoTestNumericsVsStoredFields(0, 0);
	  }

	  public virtual void TestZeroOrMin()
	  {
		// try to make GCD compression fail if the format did not anticipate that
		// the GCD of 0 and MIN_VALUE is negative
		int numIterations = AtLeast(1);
		for (int i = 0; i < numIterations; i++)
		{
		  LongProducer longs = new LongProducerAnonymousInnerClassHelper4(this);
		  DoTestNumericsVsStoredFields(longs);
		}
	  }

	  private class LongProducerAnonymousInnerClassHelper4 : LongProducer
	  {
		  private readonly BaseDocValuesFormatTestCase OuterInstance;

		  public LongProducerAnonymousInnerClassHelper4(BaseDocValuesFormatTestCase outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  internal override long Next()
		  {
			return Random().nextBoolean() ? 0 : long.MinValue;
		  }
	  }

	  public virtual void TestTwoNumbersOneMissing()
	  {
		AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField());
		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new StringField("id", "0", Field.Store.YES));
		doc.add(new NumericDocValuesField("dv1", 0));
		iw.AddDocument(doc);
		doc = new Document();
		doc.add(new StringField("id", "1", Field.Store.YES));
		iw.AddDocument(doc);
		iw.ForceMerge(1);
		iw.Close();

		IndexReader ir = DirectoryReader.open(directory);
		AssertEquals(1, ir.leaves().size());
		AtomicReader ar = ir.leaves().get(0).reader();
		NumericDocValues dv = ar.getNumericDocValues("dv1");
		AssertEquals(0, dv.get(0));
		AssertEquals(0, dv.get(1));
		Bits docsWithField = ar.getDocsWithField("dv1");
		Assert.IsTrue(docsWithField.get(0));
		Assert.IsFalse(docsWithField.get(1));
		ir.close();
		directory.close();
	  }

	  public virtual void TestTwoNumbersOneMissingWithMerging()
	  {
		AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField());
		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new StringField("id", "0", Field.Store.YES));
		doc.add(new NumericDocValuesField("dv1", 0));
		iw.AddDocument(doc);
		iw.Commit();
		doc = new Document();
		doc.add(new StringField("id", "1", Field.Store.YES));
		iw.AddDocument(doc);
		iw.ForceMerge(1);
		iw.Close();

		IndexReader ir = DirectoryReader.open(directory);
		AssertEquals(1, ir.leaves().size());
		AtomicReader ar = ir.leaves().get(0).reader();
		NumericDocValues dv = ar.getNumericDocValues("dv1");
		AssertEquals(0, dv.get(0));
		AssertEquals(0, dv.get(1));
		Bits docsWithField = ar.getDocsWithField("dv1");
		Assert.IsTrue(docsWithField.get(0));
		Assert.IsFalse(docsWithField.get(1));
		ir.close();
		directory.close();
	  }

	  public virtual void TestThreeNumbersOneMissingWithMerging()
	  {
		AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField());
		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new StringField("id", "0", Field.Store.YES));
		doc.add(new NumericDocValuesField("dv1", 0));
		iw.AddDocument(doc);
		doc = new Document();
		doc.add(new StringField("id", "1", Field.Store.YES));
		iw.AddDocument(doc);
		iw.Commit();
		doc = new Document();
		doc.add(new StringField("id", "2", Field.Store.YES));
		doc.add(new NumericDocValuesField("dv1", 5));
		iw.AddDocument(doc);
		iw.ForceMerge(1);
		iw.Close();

		IndexReader ir = DirectoryReader.open(directory);
		AssertEquals(1, ir.leaves().size());
		AtomicReader ar = ir.leaves().get(0).reader();
		NumericDocValues dv = ar.getNumericDocValues("dv1");
		AssertEquals(0, dv.get(0));
		AssertEquals(0, dv.get(1));
		AssertEquals(5, dv.get(2));
		Bits docsWithField = ar.getDocsWithField("dv1");
		Assert.IsTrue(docsWithField.get(0));
		Assert.IsFalse(docsWithField.get(1));
		Assert.IsTrue(docsWithField.get(2));
		ir.close();
		directory.close();
	  }

	  public virtual void TestTwoBytesOneMissing()
	  {
		AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField());
		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new StringField("id", "0", Field.Store.YES));
		doc.add(new BinaryDocValuesField("dv1", new BytesRef()));
		iw.AddDocument(doc);
		doc = new Document();
		doc.add(new StringField("id", "1", Field.Store.YES));
		iw.AddDocument(doc);
		iw.ForceMerge(1);
		iw.Close();

		IndexReader ir = DirectoryReader.open(directory);
		AssertEquals(1, ir.leaves().size());
		AtomicReader ar = ir.leaves().get(0).reader();
		BinaryDocValues dv = ar.getBinaryDocValues("dv1");
		BytesRef @ref = new BytesRef();
		dv.get(0, @ref);
		AssertEquals(new BytesRef(), @ref);
		dv.get(1, @ref);
		AssertEquals(new BytesRef(), @ref);
		Bits docsWithField = ar.getDocsWithField("dv1");
		Assert.IsTrue(docsWithField.get(0));
		Assert.IsFalse(docsWithField.get(1));
		ir.close();
		directory.close();
	  }

	  public virtual void TestTwoBytesOneMissingWithMerging()
	  {
		AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField());
		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new StringField("id", "0", Field.Store.YES));
		doc.add(new BinaryDocValuesField("dv1", new BytesRef()));
		iw.AddDocument(doc);
		iw.Commit();
		doc = new Document();
		doc.add(new StringField("id", "1", Field.Store.YES));
		iw.AddDocument(doc);
		iw.ForceMerge(1);
		iw.Close();

		IndexReader ir = DirectoryReader.open(directory);
		AssertEquals(1, ir.leaves().size());
		AtomicReader ar = ir.leaves().get(0).reader();
		BinaryDocValues dv = ar.getBinaryDocValues("dv1");
		BytesRef @ref = new BytesRef();
		dv.get(0, @ref);
		AssertEquals(new BytesRef(), @ref);
		dv.get(1, @ref);
		AssertEquals(new BytesRef(), @ref);
		Bits docsWithField = ar.getDocsWithField("dv1");
		Assert.IsTrue(docsWithField.get(0));
		Assert.IsFalse(docsWithField.get(1));
		ir.close();
		directory.close();
	  }

	  public virtual void TestThreeBytesOneMissingWithMerging()
	  {
		AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField());
		Directory directory = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
		conf.MergePolicy = NewLogMergePolicy();
		RandomIndexWriter iw = new RandomIndexWriter(Random(), directory, conf);
		Document doc = new Document();
		doc.add(new StringField("id", "0", Field.Store.YES));
		doc.add(new BinaryDocValuesField("dv1", new BytesRef()));
		iw.AddDocument(doc);
		doc = new Document();
		doc.add(new StringField("id", "1", Field.Store.YES));
		iw.AddDocument(doc);
		iw.Commit();
		doc = new Document();
		doc.add(new StringField("id", "2", Field.Store.YES));
		doc.add(new BinaryDocValuesField("dv1", new BytesRef("boo")));
		iw.AddDocument(doc);
		iw.ForceMerge(1);
		iw.Close();

		IndexReader ir = DirectoryReader.open(directory);
		AssertEquals(1, ir.leaves().size());
		AtomicReader ar = ir.leaves().get(0).reader();
		BinaryDocValues dv = ar.getBinaryDocValues("dv1");
		BytesRef @ref = new BytesRef();
		dv.get(0, @ref);
		AssertEquals(new BytesRef(), @ref);
		dv.get(1, @ref);
		AssertEquals(new BytesRef(), @ref);
		dv.get(2, @ref);
		AssertEquals(new BytesRef("boo"), @ref);
		Bits docsWithField = ar.getDocsWithField("dv1");
		Assert.IsTrue(docsWithField.get(0));
		Assert.IsFalse(docsWithField.get(1));
		Assert.IsTrue(docsWithField.get(2));
		ir.close();
		directory.close();
	  }

	  // LUCENE-4853
	  public virtual void TestHugeBinaryValues()
	  {
		Analyzer analyzer = new MockAnalyzer(Random());
		// FSDirectory because SimpleText will consume gobbs of
		// space when storing big binary values:
		Directory d = NewFSDirectory(CreateTempDir("hugeBinaryValues"));
		bool doFixed = Random().nextBoolean();
		int numDocs;
		int fixedLength = 0;
		if (doFixed)
		{
		  // Sometimes make all values fixed length since some
		  // codecs have different code paths for this:
		  numDocs = TestUtil.NextInt(Random(), 10, 20);
		  fixedLength = TestUtil.NextInt(Random(), 65537, 256 * 1024);
		}
		else
		{
		  numDocs = TestUtil.NextInt(Random(), 100, 200);
		}
		IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
		IList<sbyte[]> docBytes = new List<sbyte[]>();
		long totalBytes = 0;
		for (int docID = 0;docID < numDocs;docID++)
		{
		  // we don't use RandomIndexWriter because it might add
		  // more docvalues than we expect !!!!

		  // Must be > 64KB in size to ensure more than 2 pages in
		  // PagedBytes would be needed:
		  int numBytes;
		  if (doFixed)
		  {
			numBytes = fixedLength;
		  }
		  else if (docID == 0 || Random().Next(5) == 3)
		  {
			numBytes = TestUtil.NextInt(Random(), 65537, 3 * 1024 * 1024);
		  }
		  else
		  {
			numBytes = TestUtil.NextInt(Random(), 1, 1024 * 1024);
		  }
		  totalBytes += numBytes;
		  if (totalBytes > 5 * 1024 * 1024)
		  {
			break;
		  }
		  sbyte[] bytes = new sbyte[numBytes];
		  Random().nextBytes(bytes);
		  docBytes.Add(bytes);
		  Document doc = new Document();
		  BytesRef b = new BytesRef(bytes);
		  b.length = bytes.Length;
		  doc.add(new BinaryDocValuesField("field", b));
		  doc.add(new StringField("id", "" + docID, Field.Store.YES));
		  try
		  {
			w.addDocument(doc);
		  }
		  catch (System.ArgumentException iae)
		  {
			if (iae.Message.IndexOf("is too large") == -1)
			{
			  throw iae;
			}
			else
			{
			  // OK: some codecs can't handle binary DV > 32K
			  Assert.IsFalse(CodecAcceptsHugeBinaryValues("field"));
			  w.rollback();
			  d.close();
			  return;
			}
		  }
		}

		DirectoryReader r;
		try
		{
		  r = w.Reader;
		}
		catch (System.ArgumentException iae)
		{
		  if (iae.Message.IndexOf("is too large") == -1)
		  {
			throw iae;
		  }
		  else
		  {
			Assert.IsFalse(CodecAcceptsHugeBinaryValues("field"));

			// OK: some codecs can't handle binary DV > 32K
			w.rollback();
			d.close();
			return;
		  }
		}
		w.close();

		AtomicReader ar = SlowCompositeReaderWrapper.wrap(r);

		BinaryDocValues s = FieldCache.DEFAULT.getTerms(ar, "field", false);
		for (int docID = 0;docID < docBytes.Count;docID++)
		{
		  Document doc = ar.document(docID);
		  BytesRef bytes = new BytesRef();
		  s.get(docID, bytes);
		  sbyte[] expected = docBytes[Convert.ToInt32(doc.get("id"))];
		  AssertEquals(expected.Length, bytes.length);
		  AssertEquals(new BytesRef(expected), bytes);
		}

		Assert.IsTrue(CodecAcceptsHugeBinaryValues("field"));

		ar.close();
		d.close();
	  }

	  // TODO: get this out of here and into the deprecated codecs (4.0, 4.2)
	  public virtual void TestHugeBinaryValueLimit()
	  {
		// We only test DVFormats that have a limit
		AssumeFalse("test requires codec with limits on max binary field length", CodecAcceptsHugeBinaryValues("field"));
		Analyzer analyzer = new MockAnalyzer(Random());
		// FSDirectory because SimpleText will consume gobbs of
		// space when storing big binary values:
		Directory d = NewFSDirectory(CreateTempDir("hugeBinaryValues"));
		bool doFixed = Random().nextBoolean();
		int numDocs;
		int fixedLength = 0;
		if (doFixed)
		{
		  // Sometimes make all values fixed length since some
		  // codecs have different code paths for this:
		  numDocs = TestUtil.NextInt(Random(), 10, 20);
		  fixedLength = Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH;
		}
		else
		{
		  numDocs = TestUtil.NextInt(Random(), 100, 200);
		}
		IndexWriter w = new IndexWriter(d, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
		IList<sbyte[]> docBytes = new List<sbyte[]>();
		long totalBytes = 0;
		for (int docID = 0;docID < numDocs;docID++)
		{
		  // we don't use RandomIndexWriter because it might add
		  // more docvalues than we expect !!!!

		  // Must be > 64KB in size to ensure more than 2 pages in
		  // PagedBytes would be needed:
		  int numBytes;
		  if (doFixed)
		  {
			numBytes = fixedLength;
		  }
		  else if (docID == 0 || Random().Next(5) == 3)
		  {
			numBytes = Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH;
		  }
		  else
		  {
			numBytes = TestUtil.NextInt(Random(), 1, Lucene42DocValuesFormat.MAX_BINARY_FIELD_LENGTH);
		  }
		  totalBytes += numBytes;
		  if (totalBytes > 5 * 1024 * 1024)
		  {
			break;
		  }
		  sbyte[] bytes = new sbyte[numBytes];
		  Random().nextBytes(bytes);
		  docBytes.Add(bytes);
		  Document doc = new Document();
		  BytesRef b = new BytesRef(bytes);
		  b.length = bytes.Length;
		  doc.add(new BinaryDocValuesField("field", b));
		  doc.add(new StringField("id", "" + docID, Field.Store.YES));
		  w.addDocument(doc);
		}

		DirectoryReader r = w.Reader;
		w.close();

		AtomicReader ar = SlowCompositeReaderWrapper.wrap(r);

		BinaryDocValues s = FieldCache.DEFAULT.getTerms(ar, "field", false);
		for (int docID = 0;docID < docBytes.Count;docID++)
		{
		  Document doc = ar.document(docID);
		  BytesRef bytes = new BytesRef();
		  s.get(docID, bytes);
		  sbyte[] expected = docBytes[Convert.ToInt32(doc.get("id"))];
		  AssertEquals(expected.Length, bytes.length);
		  AssertEquals(new BytesRef(expected), bytes);
		}

		ar.close();
		d.close();
	  }

	  /// <summary>
	  /// Tests dv against stored fields with threads (binary/numeric/sorted, no missing) </summary>
	  public virtual void TestThreads()
	  {
		Directory dir = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
		Document doc = new Document();
		Field idField = new StringField("id", "", Field.Store.NO);
		Field storedBinField = new StoredField("storedBin", new sbyte[0]);
		Field dvBinField = new BinaryDocValuesField("dvBin", new BytesRef());
		Field dvSortedField = new SortedDocValuesField("dvSorted", new BytesRef());
		Field storedNumericField = new StoredField("storedNum", "");
		Field dvNumericField = new NumericDocValuesField("dvNum", 0);
		doc.add(idField);
		doc.add(storedBinField);
		doc.add(dvBinField);
		doc.add(dvSortedField);
		doc.add(storedNumericField);
		doc.add(dvNumericField);

		// index some docs
		int numDocs = AtLeast(300);
		for (int i = 0; i < numDocs; i++)
		{
		  idField.StringValue = Convert.ToString(i);
		  int length = TestUtil.NextInt(Random(), 0, 8);
		  sbyte[] buffer = new sbyte[length];
		  Random().nextBytes(buffer);
		  storedBinField.BytesValue = buffer;
		  dvBinField.BytesValue = buffer;
		  dvSortedField.BytesValue = buffer;
		  long numericValue = Random().nextLong();
		  storedNumericField.StringValue = Convert.ToString(numericValue);
		  dvNumericField.LongValue = numericValue;
		  writer.AddDocument(doc);
		  if (Random().Next(31) == 0)
		  {
			writer.Commit();
		  }
		}

		// delete some docs
		int numDeletions = Random().Next(numDocs / 10);
		for (int i = 0; i < numDeletions; i++)
		{
		  int id = Random().Next(numDocs);
		  writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
		}
		writer.Close();

		// compare
		DirectoryReader ir = DirectoryReader.open(dir);
		int numThreads = TestUtil.NextInt(Random(), 2, 7);
		Thread[] threads = new Thread[numThreads];
		CountDownLatch startingGun = new CountDownLatch(1);

		for (int i = 0; i < threads.Length; i++)
		{
		  threads[i] = new ThreadAnonymousInnerClassHelper(this, ir, startingGun);
		  threads[i].Start();
		}
		startingGun.countDown();
		foreach (Thread t in threads)
		{
		  t.Join();
		}
		ir.close();
		dir.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly BaseDocValuesFormatTestCase OuterInstance;

		  private DirectoryReader Ir;
		  private CountDownLatch StartingGun;

		  public ThreadAnonymousInnerClassHelper(BaseDocValuesFormatTestCase outerInstance, DirectoryReader ir, CountDownLatch startingGun)
		  {
			  this.OuterInstance = outerInstance;
			  this.Ir = ir;
			  this.StartingGun = startingGun;
		  }

		  public override void Run()
		  {
			try
			{
			  StartingGun.@await();
			  foreach (AtomicReaderContext context in Ir.leaves())
			  {
				AtomicReader r = context.reader();
				BinaryDocValues binaries = r.getBinaryDocValues("dvBin");
				SortedDocValues sorted = r.getSortedDocValues("dvSorted");
				NumericDocValues numerics = r.getNumericDocValues("dvNum");
				for (int j = 0; j < r.maxDoc(); j++)
				{
				  BytesRef binaryValue = r.document(j).getBinaryValue("storedBin");
				  BytesRef scratch = new BytesRef();
				  binaries.get(j, scratch);
				  outerInstance.AssertEquals(binaryValue, scratch);
				  sorted.get(j, scratch);
				  outerInstance.AssertEquals(binaryValue, scratch);
				  string expected = r.document(j).get("storedNum");
				  outerInstance.AssertEquals(Convert.ToInt64(expected), numerics.get(j));
				}
			  }
			  TestUtil.CheckReader(Ir);
			}
			catch (Exception e)
			{
			  throw new Exception(e);
			}
		  }
	  }

	  /// <summary>
	  /// Tests dv against stored fields with threads (all types + missing) </summary>
	  public virtual void TestThreads2()
	  {
		AssumeTrue("Codec does not support getDocsWithField", DefaultCodecSupportsDocsWithField());
		AssumeTrue("Codec does not support SORTED_SET", DefaultCodecSupportsSortedSet());
		Directory dir = NewDirectory();
		IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
		RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, conf);
		Field idField = new StringField("id", "", Field.Store.NO);
		Field storedBinField = new StoredField("storedBin", new sbyte[0]);
		Field dvBinField = new BinaryDocValuesField("dvBin", new BytesRef());
		Field dvSortedField = new SortedDocValuesField("dvSorted", new BytesRef());
		Field storedNumericField = new StoredField("storedNum", "");
		Field dvNumericField = new NumericDocValuesField("dvNum", 0);

		// index some docs
		int numDocs = AtLeast(300);
		for (int i = 0; i < numDocs; i++)
		{
		  idField.StringValue = Convert.ToString(i);
		  int length = TestUtil.NextInt(Random(), 0, 8);
		  sbyte[] buffer = new sbyte[length];
		  Random().nextBytes(buffer);
		  storedBinField.BytesValue = buffer;
		  dvBinField.BytesValue = buffer;
		  dvSortedField.BytesValue = buffer;
		  long numericValue = Random().nextLong();
		  storedNumericField.StringValue = Convert.ToString(numericValue);
		  dvNumericField.LongValue = numericValue;
		  Document doc = new Document();
		  doc.add(idField);
		  if (Random().Next(4) > 0)
		  {
			doc.add(storedBinField);
			doc.add(dvBinField);
			doc.add(dvSortedField);
		  }
		  if (Random().Next(4) > 0)
		  {
			doc.add(storedNumericField);
			doc.add(dvNumericField);
		  }
		  int numSortedSetFields = Random().Next(3);
		  Set<string> values = new SortedSet<string>();
		  for (int j = 0; j < numSortedSetFields; j++)
		  {
			values.add(TestUtil.RandomSimpleString(Random()));
		  }
		  foreach (string v in values)
		  {
			doc.add(new SortedSetDocValuesField("dvSortedSet", new BytesRef(v)));
			doc.add(new StoredField("storedSortedSet", v));
		  }
		  writer.AddDocument(doc);
		  if (Random().Next(31) == 0)
		  {
			writer.Commit();
		  }
		}

		// delete some docs
		int numDeletions = Random().Next(numDocs / 10);
		for (int i = 0; i < numDeletions; i++)
		{
		  int id = Random().Next(numDocs);
		  writer.DeleteDocuments(new Term("id", Convert.ToString(id)));
		}
		writer.Close();

		// compare
		DirectoryReader ir = DirectoryReader.open(dir);
		int numThreads = TestUtil.NextInt(Random(), 2, 7);
		Thread[] threads = new Thread[numThreads];
		CountDownLatch startingGun = new CountDownLatch(1);

		for (int i = 0; i < threads.Length; i++)
		{
		  threads[i] = new ThreadAnonymousInnerClassHelper2(this, ir, startingGun);
		  threads[i].Start();
		}
		startingGun.countDown();
		foreach (Thread t in threads)
		{
		  t.Join();
		}
		ir.close();
		dir.close();
	  }

	  private class ThreadAnonymousInnerClassHelper2 : System.Threading.Thread
	  {
		  private readonly BaseDocValuesFormatTestCase OuterInstance;

		  private DirectoryReader Ir;
		  private CountDownLatch StartingGun;

		  public ThreadAnonymousInnerClassHelper2(BaseDocValuesFormatTestCase outerInstance, DirectoryReader ir, CountDownLatch startingGun)
		  {
			  this.OuterInstance = outerInstance;
			  this.Ir = ir;
			  this.StartingGun = startingGun;
		  }

		  public override void Run()
		  {
			try
			{
			  StartingGun.@await();
			  foreach (AtomicReaderContext context in Ir.leaves())
			  {
				AtomicReader r = context.reader();
				BinaryDocValues binaries = r.getBinaryDocValues("dvBin");
				Bits binaryBits = r.getDocsWithField("dvBin");
				SortedDocValues sorted = r.getSortedDocValues("dvSorted");
				Bits sortedBits = r.getDocsWithField("dvSorted");
				NumericDocValues numerics = r.getNumericDocValues("dvNum");
				Bits numericBits = r.getDocsWithField("dvNum");
				SortedSetDocValues sortedSet = r.getSortedSetDocValues("dvSortedSet");
				Bits sortedSetBits = r.getDocsWithField("dvSortedSet");
				for (int j = 0; j < r.maxDoc(); j++)
				{
				  BytesRef binaryValue = r.document(j).getBinaryValue("storedBin");
				  if (binaryValue != null)
				  {
					if (binaries != null)
					{
					  BytesRef scratch = new BytesRef();
					  binaries.get(j, scratch);
					  outerInstance.AssertEquals(binaryValue, scratch);
					  sorted.get(j, scratch);
					  outerInstance.AssertEquals(binaryValue, scratch);
					  Assert.IsTrue(binaryBits.get(j));
					  Assert.IsTrue(sortedBits.get(j));
					}
				  }
				  else if (binaries != null)
				  {
					Assert.IsFalse(binaryBits.get(j));
					Assert.IsFalse(sortedBits.get(j));
					outerInstance.AssertEquals(-1, sorted.getOrd(j));
				  }

				  string number = r.document(j).get("storedNum");
				  if (number != null)
				  {
					if (numerics != null)
					{
					  outerInstance.AssertEquals(Convert.ToInt64(number), numerics.get(j));
					}
				  }
				  else if (numerics != null)
				  {
					Assert.IsFalse(numericBits.get(j));
					outerInstance.AssertEquals(0, numerics.get(j));
				  }

				  string[] values = r.document(j).getValues("storedSortedSet");
				  if (values.Length > 0)
				  {
					Assert.IsNotNull(sortedSet);
					sortedSet.Document = j;
					for (int k = 0; k < values.Length; k++)
					{
					  long ord = sortedSet.nextOrd();
					  Assert.IsTrue(ord != SortedSetDocValues.NO_MORE_ORDS);
					  BytesRef value = new BytesRef();
					  sortedSet.lookupOrd(ord, value);
					  outerInstance.AssertEquals(values[k], value.utf8ToString());
					}
					outerInstance.AssertEquals(SortedSetDocValues.NO_MORE_ORDS, sortedSet.nextOrd());
					Assert.IsTrue(sortedSetBits.get(j));
				  }
				  else if (sortedSet != null)
				  {
					sortedSet.Document = j;
					outerInstance.AssertEquals(SortedSetDocValues.NO_MORE_ORDS, sortedSet.nextOrd());
					Assert.IsFalse(sortedSetBits.get(j));
				  }
				}
			  }
			  TestUtil.CheckReader(Ir);
			}
			catch (Exception e)
			{
			  throw new Exception(e);
			}
		  }
	  }

	  // LUCENE-5218
	  public virtual void TestEmptyBinaryValueOnPageSizes()
	  {
		// Test larger and larger power-of-two sized values,
		// followed by empty string value:
		for (int i = 0;i < 20;i++)
		{
		  if (i > 14 && CodecAcceptsHugeBinaryValues("field") == false)
		  {
			break;
		  }
		  Directory dir = NewDirectory();
		  RandomIndexWriter w = new RandomIndexWriter(Random(), dir);
		  BytesRef bytes = new BytesRef();
		  bytes.bytes = new sbyte[1 << i];
		  bytes.length = 1 << i;
		  for (int j = 0;j < 4;j++)
		  {
			Document doc = new Document();
			doc.add(new BinaryDocValuesField("field", bytes));
			w.AddDocument(doc);
		  }
		  Document doc = new Document();
		  doc.add(new StoredField("id", "5"));
		  doc.add(new BinaryDocValuesField("field", new BytesRef()));
		  w.AddDocument(doc);
		  IndexReader r = w.Reader;
		  w.Close();

		  AtomicReader ar = SlowCompositeReaderWrapper.wrap(r);
		  BinaryDocValues values = ar.getBinaryDocValues("field");
		  BytesRef result = new BytesRef();
		  for (int j = 0;j < 5;j++)
		  {
			values.get(0, result);
			Assert.IsTrue(result.length == 0 || result.length == 1 << i);
		  }
		  ar.close();
		  dir.close();
		}
	  }

	  protected internal virtual bool CodecAcceptsHugeBinaryValues(string field)
	  {
		return true;
	  }
	}

}