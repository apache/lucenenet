using System;
using System.Collections.Generic;

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


	using Codec = Lucene.Net.Codecs.Codec;
	using BinaryDocValuesField = Lucene.Net.Document.BinaryDocValuesField;
	using Document = Lucene.Net.Document.Document;
	using DoubleField = Lucene.Net.Document.DoubleField;
	using Field = Lucene.Net.Document.Field;
	using FloatDocValuesField = Lucene.Net.Document.FloatDocValuesField;
	using FloatField = Lucene.Net.Document.FloatField;
	using IntField = Lucene.Net.Document.IntField;
	using LongField = Lucene.Net.Document.LongField;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using SortedDocValuesField = Lucene.Net.Document.SortedDocValuesField;
	using StoredField = Lucene.Net.Document.StoredField;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using English = Lucene.Net.Util.English;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Tests IndexSearcher's searchAfter() method
	/// </summary>
	public class TestSearchAfter : LuceneTestCase
	{
	  private Directory Dir;
	  private IndexReader Reader;
	  private IndexSearcher Searcher;

	  internal bool SupportsDocValues = Codec.Default.Name.Equals("Lucene3x") == false;
	  private int Iter;
	  private IList<SortField> AllSortFields;

	  public override void SetUp()
	  {
		base.setUp();

		AllSortFields = new List<>(Arrays.asList(new SortField[] {new SortField("byte", SortField.Type.BYTE, false), new SortField("short", SortField.Type.SHORT, false), new SortField("int", SortField.Type.INT, false), new SortField("long", SortField.Type.LONG, false), new SortField("float", SortField.Type.FLOAT, false), new SortField("double", SortField.Type.DOUBLE, false), new SortField("bytes", SortField.Type.STRING, false), new SortField("bytesval", SortField.Type.STRING_VAL, false), new SortField("byte", SortField.Type.BYTE, true), new SortField("short", SortField.Type.SHORT, true), new SortField("int", SortField.Type.INT, true), new SortField("long", SortField.Type.LONG, true), new SortField("float", SortField.Type.FLOAT, true), new SortField("double", SortField.Type.DOUBLE, true), new SortField("bytes", SortField.Type.STRING, true), new SortField("bytesval", SortField.Type.STRING_VAL, true), SortField.FIELD_SCORE, SortField.FIELD_DOC}));

		if (SupportsDocValues)
		{
		  AllSortFields.AddRange(Arrays.asList(new SortField[] {new SortField("intdocvalues", SortField.Type.INT, false), new SortField("floatdocvalues", SortField.Type.FLOAT, false), new SortField("sortedbytesdocvalues", SortField.Type.STRING, false), new SortField("sortedbytesdocvaluesval", SortField.Type.STRING_VAL, false), new SortField("straightbytesdocvalues", SortField.Type.STRING_VAL, false), new SortField("intdocvalues", SortField.Type.INT, true), new SortField("floatdocvalues", SortField.Type.FLOAT, true), new SortField("sortedbytesdocvalues", SortField.Type.STRING, true), new SortField("sortedbytesdocvaluesval", SortField.Type.STRING_VAL, true), new SortField("straightbytesdocvalues", SortField.Type.STRING_VAL, true)}));
		}

		// Also test missing first / last for the "string" sorts:
		foreach (string field in new string[] {"bytes", "sortedbytesdocvalues"})
		{
		  for (int rev = 0;rev < 2;rev++)
		  {
			bool reversed = rev == 0;
			SortField sf = new SortField(field, SortField.Type.STRING, reversed);
			sf.MissingValue = SortField.STRING_FIRST;
			AllSortFields.Add(sf);

			sf = new SortField(field, SortField.Type.STRING, reversed);
			sf.MissingValue = SortField.STRING_LAST;
			AllSortFields.Add(sf);
		  }
		}

		int limit = AllSortFields.Count;
		for (int i = 0;i < limit;i++)
		{
		  SortField sf = AllSortFields[i];
		  if (sf.Type == SortField.Type.INT)
		  {
			SortField sf2 = new SortField(sf.Field, SortField.Type.INT, sf.Reverse);
			sf2.MissingValue = random().Next();
			AllSortFields.Add(sf2);
		  }
		  else if (sf.Type == SortField.Type.LONG)
		  {
			SortField sf2 = new SortField(sf.Field, SortField.Type.LONG, sf.Reverse);
			sf2.MissingValue = random().nextLong();
			AllSortFields.Add(sf2);
		  }
		  else if (sf.Type == SortField.Type.FLOAT)
		  {
			SortField sf2 = new SortField(sf.Field, SortField.Type.FLOAT, sf.Reverse);
			sf2.MissingValue = random().nextFloat();
			AllSortFields.Add(sf2);
		  }
		  else if (sf.Type == SortField.Type.DOUBLE)
		  {
			SortField sf2 = new SortField(sf.Field, SortField.Type.DOUBLE, sf.Reverse);
			sf2.MissingValue = random().NextDouble();
			AllSortFields.Add(sf2);
		  }
		}

		Dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), Dir);
		int numDocs = atLeast(200);
		for (int i = 0; i < numDocs; i++)
		{
		  IList<Field> fields = new List<Field>();
		  fields.Add(newTextField("english", English.intToEnglish(i), Field.Store.NO));
		  fields.Add(newTextField("oddeven", (i % 2 == 0) ? "even" : "odd", Field.Store.NO));
		  fields.Add(newStringField("byte", "" + ((sbyte) random().Next()), Field.Store.NO));
		  fields.Add(newStringField("short", "" + ((short) random().Next()), Field.Store.NO));
		  fields.Add(new IntField("int", random().Next(), Field.Store.NO));
		  fields.Add(new LongField("long", random().nextLong(), Field.Store.NO));

		  fields.Add(new FloatField("float", random().nextFloat(), Field.Store.NO));
		  fields.Add(new DoubleField("double", random().NextDouble(), Field.Store.NO));
		  fields.Add(newStringField("bytes", TestUtil.randomRealisticUnicodeString(random()), Field.Store.NO));
		  fields.Add(newStringField("bytesval", TestUtil.randomRealisticUnicodeString(random()), Field.Store.NO));
		  fields.Add(new DoubleField("double", random().NextDouble(), Field.Store.NO));

		  if (SupportsDocValues)
		  {
			fields.Add(new NumericDocValuesField("intdocvalues", random().Next()));
			fields.Add(new FloatDocValuesField("floatdocvalues", random().nextFloat()));
			fields.Add(new SortedDocValuesField("sortedbytesdocvalues", new BytesRef(TestUtil.randomRealisticUnicodeString(random()))));
			fields.Add(new SortedDocValuesField("sortedbytesdocvaluesval", new BytesRef(TestUtil.randomRealisticUnicodeString(random()))));
			fields.Add(new BinaryDocValuesField("straightbytesdocvalues", new BytesRef(TestUtil.randomRealisticUnicodeString(random()))));
		  }
		  Document document = new Document();
		  document.add(new StoredField("id", "" + i));
		  if (VERBOSE)
		  {
			Console.WriteLine("  add doc id=" + i);
		  }
		  foreach (Field field in fields)
		  {
			// So we are sometimes missing that field:
			if (random().Next(5) != 4)
			{
			  document.add(field);
			  if (VERBOSE)
			  {
				Console.WriteLine("    " + field);
			  }
			}
		  }

		  iw.addDocument(document);

		  if (random().Next(50) == 17)
		  {
			iw.commit();
		  }
		}
		Reader = iw.Reader;
		iw.close();
		Searcher = newSearcher(Reader);
		if (VERBOSE)
		{
		  Console.WriteLine("  searcher=" + Searcher);
		}
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Dir.close();
		base.tearDown();
	  }

	  public virtual void TestQueries()
	  {
		// because the first page has a null 'after', we get a normal collector.
		// so we need to run the test a few times to ensure we will collect multiple
		// pages.
		int n = atLeast(20);
		for (int i = 0; i < n; i++)
		{
		  Filter odd = new QueryWrapperFilter(new TermQuery(new Term("oddeven", "odd")));
		  AssertQuery(new MatchAllDocsQuery(), null);
		  AssertQuery(new TermQuery(new Term("english", "one")), null);
		  AssertQuery(new MatchAllDocsQuery(), odd);
		  AssertQuery(new TermQuery(new Term("english", "four")), odd);
		  BooleanQuery bq = new BooleanQuery();
		  bq.add(new TermQuery(new Term("english", "one")), BooleanClause.Occur.SHOULD);
		  bq.add(new TermQuery(new Term("oddeven", "even")), BooleanClause.Occur.SHOULD);
		  AssertQuery(bq, null);
		}
	  }

	  internal virtual void AssertQuery(Query query, Filter filter)
	  {
		AssertQuery(query, filter, null);
		AssertQuery(query, filter, Sort.RELEVANCE);
		AssertQuery(query, filter, Sort.INDEXORDER);
		foreach (SortField sortField in AllSortFields)
		{
		  AssertQuery(query, filter, new Sort(new SortField[] {sortField}));
		}
		for (int i = 0;i < 20;i++)
		{
		  AssertQuery(query, filter, RandomSort);
		}
	  }

	  internal virtual Sort RandomSort
	  {
		  get
		  {
			SortField[] sortFields = new SortField[TestUtil.Next(random(), 2, 7)];
			for (int i = 0;i < sortFields.Length;i++)
			{
			  sortFields[i] = AllSortFields[random().Next(AllSortFields.Count)];
			}
			return new Sort(sortFields);
		  }
	  }

	  internal virtual void AssertQuery(Query query, Filter filter, Sort sort)
	  {
		int maxDoc = Searcher.IndexReader.maxDoc();
		TopDocs all;
		int pageSize = TestUtil.Next(random(), 1, maxDoc * 2);
		if (VERBOSE)
		{
		  Console.WriteLine("\nassertQuery " + (Iter++) + ": query=" + query + " filter=" + filter + " sort=" + sort + " pageSize=" + pageSize);
		}
		bool doMaxScore = random().nextBoolean();
		bool doScores = random().nextBoolean();
		if (sort == null)
		{
		  all = Searcher.search(query, filter, maxDoc);
		}
		else if (sort == Sort.RELEVANCE)
		{
		  all = Searcher.search(query, filter, maxDoc, sort, true, doMaxScore);
		}
		else
		{
		  all = Searcher.search(query, filter, maxDoc, sort, doScores, doMaxScore);
		}
		if (VERBOSE)
		{
		  Console.WriteLine("  all.totalHits=" + all.totalHits);
		  int upto = 0;
		  foreach (ScoreDoc scoreDoc in all.scoreDocs)
		  {
			Console.WriteLine("    hit " + (upto++) + ": id=" + Searcher.doc(scoreDoc.doc).get("id") + " " + scoreDoc);
		  }
		}
		int pageStart = 0;
		ScoreDoc lastBottom = null;
		while (pageStart < all.totalHits)
		{
		  TopDocs paged;
		  if (sort == null)
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("  iter lastBottom=" + lastBottom);
			}
			paged = Searcher.searchAfter(lastBottom, query, filter, pageSize);
		  }
		  else
		  {
			if (VERBOSE)
			{
			  Console.WriteLine("  iter lastBottom=" + lastBottom);
			}
			if (sort == Sort.RELEVANCE)
			{
			  paged = Searcher.searchAfter(lastBottom, query, filter, pageSize, sort, true, doMaxScore);
			}
			else
			{
			  paged = Searcher.searchAfter(lastBottom, query, filter, pageSize, sort, doScores, doMaxScore);
			}
		  }
		  if (VERBOSE)
		  {
			Console.WriteLine("    " + paged.scoreDocs.length + " hits on page");
		  }

		  if (paged.scoreDocs.length == 0)
		  {
			break;
		  }
		  AssertPage(pageStart, all, paged);
		  pageStart += paged.scoreDocs.length;
		  lastBottom = paged.scoreDocs[paged.scoreDocs.length - 1];
		}
		Assert.AreEqual(all.scoreDocs.length, pageStart);
	  }

	  internal virtual void AssertPage(int pageStart, TopDocs all, TopDocs paged)
	  {
		Assert.AreEqual(all.totalHits, paged.totalHits);
		for (int i = 0; i < paged.scoreDocs.length; i++)
		{
		  ScoreDoc sd1 = all.scoreDocs[pageStart + i];
		  ScoreDoc sd2 = paged.scoreDocs[i];
		  if (VERBOSE)
		  {
			Console.WriteLine("    hit " + (pageStart + i));
			Console.WriteLine("      expected id=" + Searcher.doc(sd1.doc).get("id") + " " + sd1);
			Console.WriteLine("        actual id=" + Searcher.doc(sd2.doc).get("id") + " " + sd2);
		  }
		  Assert.AreEqual(sd1.doc, sd2.doc);
		  Assert.AreEqual(sd1.score, sd2.score, 0f);
		  if (sd1 is FieldDoc)
		  {
			Assert.IsTrue(sd2 is FieldDoc);
			Assert.AreEqual(((FieldDoc) sd1).fields, ((FieldDoc) sd2).fields);
		  }
		}
	  }
	}

}