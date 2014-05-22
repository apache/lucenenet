using System;
using System.Diagnostics;

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
	using FieldType = Lucene.Net.Document.FieldType;
	using FloatField = Lucene.Net.Document.FloatField;
	using IntField = Lucene.Net.Document.IntField;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using MultiFields = Lucene.Net.Index.MultiFields;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using NumericUtils = Lucene.Net.Util.NumericUtils;
	using TestNumericUtils = Lucene.Net.Util.TestNumericUtils; // NaN arrays
	using TestUtil = Lucene.Net.Util.TestUtil;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;
	using Test = org.junit.Test;

	public class TestNumericRangeQuery32 : LuceneTestCase
	{
	  // distance of entries
	  private static int Distance;
	  // shift the starting of the values to the left, to also have negative values:
	  private static readonly int StartOffset = - 1 << 15;
	  // number of docs to generate for testing
	  private static int NoDocs;

	  private static Directory Directory = null;
	  private static IndexReader Reader = null;
	  private static IndexSearcher Searcher = null;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		NoDocs = atLeast(4096);
		Distance = (1 << 30) / NoDocs;
		Directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(TestUtil.Next(random(), 100, 1000)).setMergePolicy(newLogMergePolicy()));

		FieldType storedInt = new FieldType(IntField.TYPE_NOT_STORED);
		storedInt.Stored = true;
		storedInt.freeze();

		FieldType storedInt8 = new FieldType(storedInt);
		storedInt8.NumericPrecisionStep = 8;

		FieldType storedInt4 = new FieldType(storedInt);
		storedInt4.NumericPrecisionStep = 4;

		FieldType storedInt2 = new FieldType(storedInt);
		storedInt2.NumericPrecisionStep = 2;

		FieldType storedIntNone = new FieldType(storedInt);
		storedIntNone.NumericPrecisionStep = int.MaxValue;

		FieldType unstoredInt = IntField.TYPE_NOT_STORED;

		FieldType unstoredInt8 = new FieldType(unstoredInt);
		unstoredInt8.NumericPrecisionStep = 8;

		FieldType unstoredInt4 = new FieldType(unstoredInt);
		unstoredInt4.NumericPrecisionStep = 4;

		FieldType unstoredInt2 = new FieldType(unstoredInt);
		unstoredInt2.NumericPrecisionStep = 2;

		IntField field8 = new IntField("field8", 0, storedInt8), field4 = new IntField("field4", 0, storedInt4), field2 = new IntField("field2", 0, storedInt2), fieldNoTrie = new IntField("field" + int.MaxValue, 0, storedIntNone), ascfield8 = new IntField("ascfield8", 0, unstoredInt8), ascfield4 = new IntField("ascfield4", 0, unstoredInt4), ascfield2 = new IntField("ascfield2", 0, unstoredInt2);

		Document doc = new Document();
		// add fields, that have a distance to test general functionality
		doc.add(field8);
		doc.add(field4);
		doc.add(field2);
		doc.add(fieldNoTrie);
		// add ascending fields with a distance of 1, beginning at -noDocs/2 to test the correct splitting of range and inclusive/exclusive
		doc.add(ascfield8);
		doc.add(ascfield4);
		doc.add(ascfield2);

		// Add a series of noDocs docs with increasing int values
		for (int l = 0; l < NoDocs; l++)
		{
		  int val = Distance * l + StartOffset;
		  field8.IntValue = val;
		  field4.IntValue = val;
		  field2.IntValue = val;
		  fieldNoTrie.IntValue = val;

		  val = l - (NoDocs / 2);
		  ascfield8.IntValue = val;
		  ascfield4.IntValue = val;
		  ascfield2.IntValue = val;
		  writer.addDocument(doc);
		}

		Reader = writer.Reader;
		Searcher = newSearcher(Reader);
		writer.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass() throws Exception
	  public static void AfterClass()
	  {
		Searcher = null;
		Reader.close();
		Reader = null;
		Directory.close();
		Directory = null;
	  }

	  public override void SetUp()
	  {
		base.setUp();
		// set the theoretical maximum term count for 8bit (see docs for the number)
		// super.tearDown will restore the default
		BooleanQuery.MaxClauseCount = 3 * 255 * 2 + 255;
	  }

	  /// <summary>
	  /// test for both constant score and boolean query, the other tests only use the constant score mode </summary>
	  private void TestRange(int precisionStep)
	  {
		string field = "field" + precisionStep;
		int count = 3000;
		int lower = (Distance * 3 / 2) + StartOffset, upper = lower + count * Distance + (Distance / 3);
		NumericRangeQuery<int?> q = NumericRangeQuery.newIntRange(field, precisionStep, lower, upper, true, true);
		NumericRangeFilter<int?> f = NumericRangeFilter.newIntRange(field, precisionStep, lower, upper, true, true);
		for (sbyte i = 0; i < 3; i++)
		{
		  TopDocs topDocs;
		  string type;
		  switch (i)
		  {
			case 0:
			  type = " (constant score filter rewrite)";
			  q.RewriteMethod = MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE;
			  topDocs = Searcher.search(q, null, NoDocs, Sort.INDEXORDER);
			  break;
			case 1:
			  type = " (constant score boolean rewrite)";
			  q.RewriteMethod = MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE;
			  topDocs = Searcher.search(q, null, NoDocs, Sort.INDEXORDER);
			  break;
			case 2:
			  type = " (filter)";
			  topDocs = Searcher.search(new MatchAllDocsQuery(), f, NoDocs, Sort.INDEXORDER);
			  break;
			default:
			  return;
		  }
		  ScoreDoc[] sd = topDocs.scoreDocs;
		  Assert.IsNotNull(sd);
		  Assert.AreEqual("Score doc count" + type, count, sd.Length);
		  Document doc = Searcher.doc(sd[0].doc);
		  Assert.AreEqual("First doc" + type, 2 * Distance + StartOffset, (int)doc.getField(field).numericValue());
		  doc = Searcher.doc(sd[sd.Length - 1].doc);
		  Assert.AreEqual("Last doc" + type, (1 + count) * Distance + StartOffset, (int)doc.getField(field).numericValue());
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRange_8bit() throws Exception
	  public virtual void TestRange_8bit()
	  {
		TestRange(8);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRange_4bit() throws Exception
	  public virtual void TestRange_4bit()
	  {
		TestRange(4);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRange_2bit() throws Exception
	  public virtual void TestRange_2bit()
	  {
		TestRange(2);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testInverseRange() throws Exception
	  public virtual void TestInverseRange()
	  {
		AtomicReaderContext context = SlowCompositeReaderWrapper.wrap(Reader).Context;
		NumericRangeFilter<int?> f = NumericRangeFilter.newIntRange("field8", 8, 1000, -1000, true, true);
		assertNull("A inverse range should return the null instance", f.getDocIdSet(context, context.reader().LiveDocs));
		f = NumericRangeFilter.newIntRange("field8", 8, int.MaxValue, null, false, false);
		assertNull("A exclusive range starting with Integer.MAX_VALUE should return the null instance", f.getDocIdSet(context, context.reader().LiveDocs));
		f = NumericRangeFilter.newIntRange("field8", 8, null, int.MinValue, false, false);
		assertNull("A exclusive range ending with Integer.MIN_VALUE should return the null instance", f.getDocIdSet(context, context.reader().LiveDocs));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testOneMatchQuery() throws Exception
	  public virtual void TestOneMatchQuery()
	  {
		NumericRangeQuery<int?> q = NumericRangeQuery.newIntRange("ascfield8", 8, 1000, 1000, true, true);
		TopDocs topDocs = Searcher.search(q, NoDocs);
		ScoreDoc[] sd = topDocs.scoreDocs;
		Assert.IsNotNull(sd);
		Assert.AreEqual("Score doc count", 1, sd.Length);
	  }

	  private void TestLeftOpenRange(int precisionStep)
	  {
		string field = "field" + precisionStep;
		int count = 3000;
		int upper = (count - 1) * Distance + (Distance / 3) + StartOffset;
		NumericRangeQuery<int?> q = NumericRangeQuery.newIntRange(field, precisionStep, null, upper, true, true);
		TopDocs topDocs = Searcher.search(q, null, NoDocs, Sort.INDEXORDER);
		ScoreDoc[] sd = topDocs.scoreDocs;
		Assert.IsNotNull(sd);
		Assert.AreEqual("Score doc count", count, sd.Length);
		Document doc = Searcher.doc(sd[0].doc);
		Assert.AreEqual("First doc", StartOffset, (int)doc.getField(field).numericValue());
		doc = Searcher.doc(sd[sd.Length - 1].doc);
		Assert.AreEqual("Last doc", (count - 1) * Distance + StartOffset, (int)doc.getField(field).numericValue());

		q = NumericRangeQuery.newIntRange(field, precisionStep, null, upper, false, true);
		topDocs = Searcher.search(q, null, NoDocs, Sort.INDEXORDER);
		sd = topDocs.scoreDocs;
		Assert.IsNotNull(sd);
		Assert.AreEqual("Score doc count", count, sd.Length);
		doc = Searcher.doc(sd[0].doc);
		Assert.AreEqual("First doc", StartOffset, (int)doc.getField(field).numericValue());
		doc = Searcher.doc(sd[sd.Length - 1].doc);
		Assert.AreEqual("Last doc", (count - 1) * Distance + StartOffset, (int)doc.getField(field).numericValue());
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testLeftOpenRange_8bit() throws Exception
	  public virtual void TestLeftOpenRange_8bit()
	  {
		TestLeftOpenRange(8);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testLeftOpenRange_4bit() throws Exception
	  public virtual void TestLeftOpenRange_4bit()
	  {
		TestLeftOpenRange(4);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testLeftOpenRange_2bit() throws Exception
	  public virtual void TestLeftOpenRange_2bit()
	  {
		TestLeftOpenRange(2);
	  }

	  private void TestRightOpenRange(int precisionStep)
	  {
		string field = "field" + precisionStep;
		int count = 3000;
		int lower = (count - 1) * Distance + (Distance / 3) + StartOffset;
		NumericRangeQuery<int?> q = NumericRangeQuery.newIntRange(field, precisionStep, lower, null, true, true);
		TopDocs topDocs = Searcher.search(q, null, NoDocs, Sort.INDEXORDER);
		ScoreDoc[] sd = topDocs.scoreDocs;
		Assert.IsNotNull(sd);
		Assert.AreEqual("Score doc count", NoDocs - count, sd.Length);
		Document doc = Searcher.doc(sd[0].doc);
		Assert.AreEqual("First doc", count * Distance + StartOffset, (int)doc.getField(field).numericValue());
		doc = Searcher.doc(sd[sd.Length - 1].doc);
		Assert.AreEqual("Last doc", (NoDocs - 1) * Distance + StartOffset, (int)doc.getField(field).numericValue());

		q = NumericRangeQuery.newIntRange(field, precisionStep, lower, null, true, false);
		topDocs = Searcher.search(q, null, NoDocs, Sort.INDEXORDER);
		sd = topDocs.scoreDocs;
		Assert.IsNotNull(sd);
		Assert.AreEqual("Score doc count", NoDocs - count, sd.Length);
		doc = Searcher.doc(sd[0].doc);
		Assert.AreEqual("First doc", count * Distance + StartOffset, (int)doc.getField(field).numericValue());
		doc = Searcher.doc(sd[sd.Length - 1].doc);
		Assert.AreEqual("Last doc", (NoDocs - 1) * Distance + StartOffset, (int)doc.getField(field).numericValue());
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRightOpenRange_8bit() throws Exception
	  public virtual void TestRightOpenRange_8bit()
	  {
		TestRightOpenRange(8);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRightOpenRange_4bit() throws Exception
	  public virtual void TestRightOpenRange_4bit()
	  {
		TestRightOpenRange(4);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRightOpenRange_2bit() throws Exception
	  public virtual void TestRightOpenRange_2bit()
	  {
		TestRightOpenRange(2);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testInfiniteValues() throws Exception
	  public virtual void TestInfiniteValues()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(new FloatField("float", float.NegativeInfinity, Field.Store.NO));
		doc.add(new IntField("int", int.MinValue, Field.Store.NO));
		writer.addDocument(doc);

		doc = new Document();
		doc.add(new FloatField("float", float.PositiveInfinity, Field.Store.NO));
		doc.add(new IntField("int", int.MaxValue, Field.Store.NO));
		writer.addDocument(doc);

		doc = new Document();
		doc.add(new FloatField("float", 0.0f, Field.Store.NO));
		doc.add(new IntField("int", 0, Field.Store.NO));
		writer.addDocument(doc);

		foreach (float f in TestNumericUtils.FLOAT_NANs)
		{
		  doc = new Document();
		  doc.add(new FloatField("float", f, Field.Store.NO));
		  writer.addDocument(doc);
		}

		writer.close();

		IndexReader r = DirectoryReader.open(dir);
		IndexSearcher s = newSearcher(r);

		Query q = NumericRangeQuery.newIntRange("int", null, null, true, true);
		TopDocs topDocs = s.search(q, 10);
		Assert.AreEqual("Score doc count", 3, topDocs.scoreDocs.length);

		q = NumericRangeQuery.newIntRange("int", null, null, false, false);
		topDocs = s.search(q, 10);
		Assert.AreEqual("Score doc count", 3, topDocs.scoreDocs.length);

		q = NumericRangeQuery.newIntRange("int", int.MinValue, int.MaxValue, true, true);
		topDocs = s.search(q, 10);
		Assert.AreEqual("Score doc count", 3, topDocs.scoreDocs.length);

		q = NumericRangeQuery.newIntRange("int", int.MinValue, int.MaxValue, false, false);
		topDocs = s.search(q, 10);
		Assert.AreEqual("Score doc count", 1, topDocs.scoreDocs.length);

		q = NumericRangeQuery.newFloatRange("float", null, null, true, true);
		topDocs = s.search(q, 10);
		Assert.AreEqual("Score doc count", 3, topDocs.scoreDocs.length);

		q = NumericRangeQuery.newFloatRange("float", null, null, false, false);
		topDocs = s.search(q, 10);
		Assert.AreEqual("Score doc count", 3, topDocs.scoreDocs.length);

		q = NumericRangeQuery.newFloatRange("float", float.NegativeInfinity, float.PositiveInfinity, true, true);
		topDocs = s.search(q, 10);
		Assert.AreEqual("Score doc count", 3, topDocs.scoreDocs.length);

		q = NumericRangeQuery.newFloatRange("float", float.NegativeInfinity, float.PositiveInfinity, false, false);
		topDocs = s.search(q, 10);
		Assert.AreEqual("Score doc count", 1, topDocs.scoreDocs.length);

		q = NumericRangeQuery.newFloatRange("float", float.NaN, float.NaN, true, true);
		topDocs = s.search(q, 10);
		Assert.AreEqual("Score doc count", TestNumericUtils.FLOAT_NANs.Length, topDocs.scoreDocs.length);

		r.close();
		dir.close();
	  }

	  private void TestRandomTrieAndClassicRangeQuery(int precisionStep)
	  {
		string field = "field" + precisionStep;
		int totalTermCountT = 0, totalTermCountC = 0, termCountT , termCountC ;
		int num = TestUtil.Next(random(), 10, 20);
		for (int i = 0; i < num; i++)
		{
		  int lower = (int)(random().NextDouble() * NoDocs * Distance) + StartOffset;
		  int upper = (int)(random().NextDouble() * NoDocs * Distance) + StartOffset;
		  if (lower > upper)
		  {
			int a = lower;
			lower = upper;
			upper = a;
		  }
		  const BytesRef lowerBytes = new BytesRef(NumericUtils.BUF_SIZE_INT), upperBytes = new BytesRef(NumericUtils.BUF_SIZE_INT);
		  NumericUtils.intToPrefixCodedBytes(lower, 0, lowerBytes);
		  NumericUtils.intToPrefixCodedBytes(upper, 0, upperBytes);

		  // test inclusive range
		  NumericRangeQuery<int?> tq = NumericRangeQuery.newIntRange(field, precisionStep, lower, upper, true, true);
		  TermRangeQuery cq = new TermRangeQuery(field, lowerBytes, upperBytes, true, true);
		  TopDocs tTopDocs = Searcher.search(tq, 1);
		  TopDocs cTopDocs = Searcher.search(cq, 1);
		  Assert.AreEqual("Returned count for NumericRangeQuery and TermRangeQuery must be equal", cTopDocs.totalHits, tTopDocs.totalHits);
		  totalTermCountT += termCountT = CountTerms(tq);
		  totalTermCountC += termCountC = CountTerms(cq);
		  CheckTermCounts(precisionStep, termCountT, termCountC);
		  // test exclusive range
		  tq = NumericRangeQuery.newIntRange(field, precisionStep, lower, upper, false, false);
		  cq = new TermRangeQuery(field, lowerBytes, upperBytes, false, false);
		  tTopDocs = Searcher.search(tq, 1);
		  cTopDocs = Searcher.search(cq, 1);
		  Assert.AreEqual("Returned count for NumericRangeQuery and TermRangeQuery must be equal", cTopDocs.totalHits, tTopDocs.totalHits);
		  totalTermCountT += termCountT = CountTerms(tq);
		  totalTermCountC += termCountC = CountTerms(cq);
		  CheckTermCounts(precisionStep, termCountT, termCountC);
		  // test left exclusive range
		  tq = NumericRangeQuery.newIntRange(field, precisionStep, lower, upper, false, true);
		  cq = new TermRangeQuery(field, lowerBytes, upperBytes, false, true);
		  tTopDocs = Searcher.search(tq, 1);
		  cTopDocs = Searcher.search(cq, 1);
		  Assert.AreEqual("Returned count for NumericRangeQuery and TermRangeQuery must be equal", cTopDocs.totalHits, tTopDocs.totalHits);
		  totalTermCountT += termCountT = CountTerms(tq);
		  totalTermCountC += termCountC = CountTerms(cq);
		  CheckTermCounts(precisionStep, termCountT, termCountC);
		  // test right exclusive range
		  tq = NumericRangeQuery.newIntRange(field, precisionStep, lower, upper, true, false);
		  cq = new TermRangeQuery(field, lowerBytes, upperBytes, true, false);
		  tTopDocs = Searcher.search(tq, 1);
		  cTopDocs = Searcher.search(cq, 1);
		  Assert.AreEqual("Returned count for NumericRangeQuery and TermRangeQuery must be equal", cTopDocs.totalHits, tTopDocs.totalHits);
		  totalTermCountT += termCountT = CountTerms(tq);
		  totalTermCountC += termCountC = CountTerms(cq);
		  CheckTermCounts(precisionStep, termCountT, termCountC);
		}

		CheckTermCounts(precisionStep, totalTermCountT, totalTermCountC);
		if (VERBOSE && precisionStep != int.MaxValue)
		{
		  Console.WriteLine("Average number of terms during random search on '" + field + "':");
		  Console.WriteLine(" Numeric query: " + (((double)totalTermCountT) / (num * 4)));
		  Console.WriteLine(" Classical query: " + (((double)totalTermCountC) / (num * 4)));
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testEmptyEnums() throws Exception
	  public virtual void TestEmptyEnums()
	  {
		int count = 3000;
		int lower = (Distance * 3 / 2) + StartOffset, upper = lower + count * Distance + (Distance / 3);
		// test empty enum
		Debug.Assert(lower < upper);
		Assert.IsTrue(0 < CountTerms(NumericRangeQuery.newIntRange("field4", 4, lower, upper, true, true)));
		Assert.AreEqual(0, CountTerms(NumericRangeQuery.newIntRange("field4", 4, upper, lower, true, true)));
		// test empty enum outside of bounds
		lower = Distance * NoDocs + StartOffset;
		upper = 2 * lower;
		Debug.Assert(lower < upper);
		Assert.AreEqual(0, CountTerms(NumericRangeQuery.newIntRange("field4", 4, lower, upper, true, true)));
	  }

	  private int CountTerms(MultiTermQuery q)
	  {
		Terms terms = MultiFields.getTerms(Reader, q.Field);
		if (terms == null)
		{
		  return 0;
		}
		TermsEnum termEnum = q.getTermsEnum(terms);
		Assert.IsNotNull(termEnum);
		int count = 0;
		BytesRef cur , last = null;
		while ((cur = termEnum.next()) != null)
		{
		  count++;
		  if (last != null)
		  {
			Assert.IsTrue(last.compareTo(cur) < 0);
		  }
		  last = BytesRef.deepCopyOf(cur);
		}
		// LUCENE-3314: the results after next() already returned null are undefined,
		// assertNull(termEnum.next());
		return count;
	  }

	  private void CheckTermCounts(int precisionStep, int termCountT, int termCountC)
	  {
		if (precisionStep == int.MaxValue)
		{
		  Assert.AreEqual("Number of terms should be equal for unlimited precStep", termCountC, termCountT);
		}
		else
		{
		  Assert.IsTrue("Number of terms for NRQ should be <= compared to classical TRQ", termCountT <= termCountC);
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRandomTrieAndClassicRangeQuery_8bit() throws Exception
	  public virtual void TestRandomTrieAndClassicRangeQuery_8bit()
	  {
		TestRandomTrieAndClassicRangeQuery(8);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRandomTrieAndClassicRangeQuery_4bit() throws Exception
	  public virtual void TestRandomTrieAndClassicRangeQuery_4bit()
	  {
		TestRandomTrieAndClassicRangeQuery(4);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRandomTrieAndClassicRangeQuery_2bit() throws Exception
	  public virtual void TestRandomTrieAndClassicRangeQuery_2bit()
	  {
		TestRandomTrieAndClassicRangeQuery(2);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRandomTrieAndClassicRangeQuery_NoTrie() throws Exception
	  public virtual void TestRandomTrieAndClassicRangeQuery_NoTrie()
	  {
		TestRandomTrieAndClassicRangeQuery(int.MaxValue);
	  }

	  private void TestRangeSplit(int precisionStep)
	  {
		string field = "ascfield" + precisionStep;
		// 10 random tests
		int num = TestUtil.Next(random(), 10, 20);
		for (int i = 0; i < num; i++)
		{
		  int lower = (int)(random().NextDouble() * NoDocs - NoDocs / 2);
		  int upper = (int)(random().NextDouble() * NoDocs - NoDocs / 2);
		  if (lower > upper)
		  {
			int a = lower;
			lower = upper;
			upper = a;
		  }
		  // test inclusive range
		  Query tq = NumericRangeQuery.newIntRange(field, precisionStep, lower, upper, true, true);
		  TopDocs tTopDocs = Searcher.search(tq, 1);
		  Assert.AreEqual("Returned count of range query must be equal to inclusive range length", upper - lower + 1, tTopDocs.totalHits);
		  // test exclusive range
		  tq = NumericRangeQuery.newIntRange(field, precisionStep, lower, upper, false, false);
		  tTopDocs = Searcher.search(tq, 1);
		  Assert.AreEqual("Returned count of range query must be equal to exclusive range length", Math.Max(upper - lower - 1, 0), tTopDocs.totalHits);
		  // test left exclusive range
		  tq = NumericRangeQuery.newIntRange(field, precisionStep, lower, upper, false, true);
		  tTopDocs = Searcher.search(tq, 1);
		  Assert.AreEqual("Returned count of range query must be equal to half exclusive range length", upper - lower, tTopDocs.totalHits);
		  // test right exclusive range
		  tq = NumericRangeQuery.newIntRange(field, precisionStep, lower, upper, true, false);
		  tTopDocs = Searcher.search(tq, 1);
		  Assert.AreEqual("Returned count of range query must be equal to half exclusive range length", upper - lower, tTopDocs.totalHits);
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRangeSplit_8bit() throws Exception
	  public virtual void TestRangeSplit_8bit()
	  {
		TestRangeSplit(8);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRangeSplit_4bit() throws Exception
	  public virtual void TestRangeSplit_4bit()
	  {
		TestRangeSplit(4);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRangeSplit_2bit() throws Exception
	  public virtual void TestRangeSplit_2bit()
	  {
		TestRangeSplit(2);
	  }

	  /// <summary>
	  /// we fake a float test using int2float conversion of NumericUtils </summary>
	  private void TestFloatRange(int precisionStep)
	  {
		string field = "ascfield" + precisionStep;
		const int lower = -1000, upper = +2000;

		Query tq = NumericRangeQuery.newFloatRange(field, precisionStep, NumericUtils.sortableIntToFloat(lower), NumericUtils.sortableIntToFloat(upper), true, true);
		TopDocs tTopDocs = Searcher.search(tq, 1);
		Assert.AreEqual("Returned count of range query must be equal to inclusive range length", upper - lower + 1, tTopDocs.totalHits);

		Filter tf = NumericRangeFilter.newFloatRange(field, precisionStep, NumericUtils.sortableIntToFloat(lower), NumericUtils.sortableIntToFloat(upper), true, true);
		tTopDocs = Searcher.search(new MatchAllDocsQuery(), tf, 1);
		Assert.AreEqual("Returned count of range filter must be equal to inclusive range length", upper - lower + 1, tTopDocs.totalHits);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFloatRange_8bit() throws Exception
	  public virtual void TestFloatRange_8bit()
	  {
		TestFloatRange(8);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFloatRange_4bit() throws Exception
	  public virtual void TestFloatRange_4bit()
	  {
		TestFloatRange(4);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testFloatRange_2bit() throws Exception
	  public virtual void TestFloatRange_2bit()
	  {
		TestFloatRange(2);
	  }

	  private void TestSorting(int precisionStep)
	  {
		string field = "field" + precisionStep;
		// 10 random tests, the index order is ascending,
		// so using a reverse sort field should retun descending documents
		int num = TestUtil.Next(random(), 10, 20);
		for (int i = 0; i < num; i++)
		{
		  int lower = (int)(random().NextDouble() * NoDocs * Distance) + StartOffset;
		  int upper = (int)(random().NextDouble() * NoDocs * Distance) + StartOffset;
		  if (lower > upper)
		  {
			int a = lower;
			lower = upper;
			upper = a;
		  }
		  Query tq = NumericRangeQuery.newIntRange(field, precisionStep, lower, upper, true, true);
		  TopDocs topDocs = Searcher.search(tq, null, NoDocs, new Sort(new SortField(field, SortField.Type.INT, true)));
		  if (topDocs.totalHits == 0)
		  {
			  continue;
		  }
		  ScoreDoc[] sd = topDocs.scoreDocs;
		  Assert.IsNotNull(sd);
		  int last = (int)Searcher.doc(sd[0].doc).getField(field).numericValue();
		  for (int j = 1; j < sd.Length; j++)
		  {
			int act = (int)Searcher.doc(sd[j].doc).getField(field).numericValue();
			Assert.IsTrue("Docs should be sorted backwards", last > act);
			last = act;
		  }
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSorting_8bit() throws Exception
	  public virtual void TestSorting_8bit()
	  {
		TestSorting(8);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSorting_4bit() throws Exception
	  public virtual void TestSorting_4bit()
	  {
		TestSorting(4);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSorting_2bit() throws Exception
	  public virtual void TestSorting_2bit()
	  {
		TestSorting(2);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testEqualsAndHash() throws Exception
	  public virtual void TestEqualsAndHash()
	  {
		QueryUtils.checkHashEquals(NumericRangeQuery.newIntRange("test1", 4, 10, 20, true, true));
		QueryUtils.checkHashEquals(NumericRangeQuery.newIntRange("test2", 4, 10, 20, false, true));
		QueryUtils.checkHashEquals(NumericRangeQuery.newIntRange("test3", 4, 10, 20, true, false));
		QueryUtils.checkHashEquals(NumericRangeQuery.newIntRange("test4", 4, 10, 20, false, false));
		QueryUtils.checkHashEquals(NumericRangeQuery.newIntRange("test5", 4, 10, null, true, true));
		QueryUtils.checkHashEquals(NumericRangeQuery.newIntRange("test6", 4, null, 20, true, true));
		QueryUtils.checkHashEquals(NumericRangeQuery.newIntRange("test7", 4, null, null, true, true));
		QueryUtils.checkEqual(NumericRangeQuery.newIntRange("test8", 4, 10, 20, true, true), NumericRangeQuery.newIntRange("test8", 4, 10, 20, true, true));
		QueryUtils.checkUnequal(NumericRangeQuery.newIntRange("test9", 4, 10, 20, true, true), NumericRangeQuery.newIntRange("test9", 8, 10, 20, true, true));
		QueryUtils.checkUnequal(NumericRangeQuery.newIntRange("test10a", 4, 10, 20, true, true), NumericRangeQuery.newIntRange("test10b", 4, 10, 20, true, true));
		QueryUtils.checkUnequal(NumericRangeQuery.newIntRange("test11", 4, 10, 20, true, true), NumericRangeQuery.newIntRange("test11", 4, 20, 10, true, true));
		QueryUtils.checkUnequal(NumericRangeQuery.newIntRange("test12", 4, 10, 20, true, true), NumericRangeQuery.newIntRange("test12", 4, 10, 20, false, true));
		QueryUtils.checkUnequal(NumericRangeQuery.newIntRange("test13", 4, 10, 20, true, true), NumericRangeQuery.newFloatRange("test13", 4, 10f, 20f, true, true));
		// the following produces a hash collision, because Long and Integer have the same hashcode, so only test equality:
		Query q1 = NumericRangeQuery.newIntRange("test14", 4, 10, 20, true, true);
		Query q2 = NumericRangeQuery.newLongRange("test14", 4, 10L, 20L, true, true);
		Assert.IsFalse(q1.Equals(q2));
		Assert.IsFalse(q2.Equals(q1));
	  }

	}

}