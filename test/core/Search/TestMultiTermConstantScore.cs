using System;

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
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using Directory = Lucene.Net.Store.Directory;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;
	using Test = org.junit.Test;

	using Assert = junit.framework.Assert;

	public class TestMultiTermConstantScore : BaseTestRangeFilter
	{

	  /// <summary>
	  /// threshold for comparing floats </summary>
	  public const float SCORE_COMP_THRESH = 1e-6f;

	  internal static Directory Small;
	  internal static IndexReader Reader;

	  public static void AssertEquals(string m, int e, int a)
	  {
		Assert.Assert.AreEqual(m, e, a);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		string[] data = new string[] {"A 1 2 3 4 5 6", "Z       4 5 6", null, "B   2   4 5 6", "Y     3   5 6", null, "C     3     6", "X       4 5 6"};

		Small = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Small, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)).setMergePolicy(newLogMergePolicy()));

		FieldType customType = new FieldType(TextField.TYPE_STORED);
		customType.Tokenized = false;
		for (int i = 0; i < data.Length; i++)
		{
		  Document doc = new Document();
		  doc.add(newField("id", Convert.ToString(i), customType)); // Field.Keyword("id",String.valueOf(i)));
		  doc.add(newField("all", "all", customType)); // Field.Keyword("all","all"));
		  if (null != data[i])
		  {
			doc.add(newTextField("data", data[i], Field.Store.YES)); // Field.Text("data",data[i]));
		  }
		  writer.addDocument(doc);
		}

		Reader = writer.Reader;
		writer.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass() throws Exception
	  public static void AfterClass()
	  {
		Reader.close();
		Small.close();
		Reader = null;
		Small = null;
	  }

	  /// <summary>
	  /// macro for readability </summary>
	  public static Query Csrq(string f, string l, string h, bool il, bool ih)
	  {
		TermRangeQuery query = TermRangeQuery.newStringRange(f, l, h, il, ih);
		query.RewriteMethod = MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE;
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: query=" + query);
		}
		return query;
	  }

	  public static Query Csrq(string f, string l, string h, bool il, bool ih, MultiTermQuery.RewriteMethod method)
	  {
		TermRangeQuery query = TermRangeQuery.newStringRange(f, l, h, il, ih);
		query.RewriteMethod = method;
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: query=" + query + " method=" + method);
		}
		return query;
	  }

	  /// <summary>
	  /// macro for readability </summary>
	  public static Query Cspq(Term prefix)
	  {
		PrefixQuery query = new PrefixQuery(prefix);
		query.RewriteMethod = MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE;
		return query;
	  }

	  /// <summary>
	  /// macro for readability </summary>
	  public static Query Cswcq(Term wild)
	  {
		WildcardQuery query = new WildcardQuery(wild);
		query.RewriteMethod = MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE;
		return query;
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testBasics() throws java.io.IOException
	  public virtual void TestBasics()
	  {
		QueryUtils.check(Csrq("data", "1", "6", T, T));
		QueryUtils.check(Csrq("data", "A", "Z", T, T));
		QueryUtils.checkUnequal(Csrq("data", "1", "6", T, T), Csrq("data", "A", "Z", T, T));

		QueryUtils.check(Cspq(new Term("data", "p*u?")));
		QueryUtils.checkUnequal(Cspq(new Term("data", "pre*")), Cspq(new Term("data", "pres*")));

		QueryUtils.check(Cswcq(new Term("data", "p")));
		QueryUtils.checkUnequal(Cswcq(new Term("data", "pre*n?t")), Cswcq(new Term("data", "pr*t?j")));
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testEqualScores() throws java.io.IOException
	  public virtual void TestEqualScores()
	  {
		// NOTE: uses index build in *this* setUp

		IndexSearcher search = newSearcher(Reader);

		ScoreDoc[] result;

		// some hits match more terms then others, score should be the same

		result = search.search(Csrq("data", "1", "6", T, T), null, 1000).scoreDocs;
		int numHits = result.Length;
		AssertEquals("wrong number of results", 6, numHits);
		float score = result[0].score;
		for (int i = 1; i < numHits; i++)
		{
		  Assert.AreEqual("score for " + i + " was not the same", score, result[i].score, SCORE_COMP_THRESH);
		}

		result = search.search(Csrq("data", "1", "6", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE), null, 1000).scoreDocs;
		numHits = result.Length;
		AssertEquals("wrong number of results", 6, numHits);
		for (int i = 0; i < numHits; i++)
		{
		  Assert.AreEqual("score for " + i + " was not the same", score, result[i].score, SCORE_COMP_THRESH);
		}

		result = search.search(Csrq("data", "1", "6", T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, 1000).scoreDocs;
		numHits = result.Length;
		AssertEquals("wrong number of results", 6, numHits);
		for (int i = 0; i < numHits; i++)
		{
		  Assert.AreEqual("score for " + i + " was not the same", score, result[i].score, SCORE_COMP_THRESH);
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testEqualScoresWhenNoHits() throws java.io.IOException
	  public virtual void TestEqualScoresWhenNoHits() // Test for LUCENE-5245: Empty MTQ rewrites should have a consistent norm, so always need to return a CSQ!
	  {
		// NOTE: uses index build in *this* setUp

		IndexSearcher search = newSearcher(Reader);

		ScoreDoc[] result;

		TermQuery dummyTerm = new TermQuery(new Term("data", "1"));

		BooleanQuery bq = new BooleanQuery();
		bq.add(dummyTerm, BooleanClause.Occur.SHOULD); // hits one doc
		bq.add(Csrq("data", "#", "#", T, T), BooleanClause.Occur.SHOULD); // hits no docs
		result = search.search(bq, null, 1000).scoreDocs;
		int numHits = result.Length;
		AssertEquals("wrong number of results", 1, numHits);
		float score = result[0].score;
		for (int i = 1; i < numHits; i++)
		{
		  Assert.AreEqual("score for " + i + " was not the same", score, result[i].score, SCORE_COMP_THRESH);
		}

		bq = new BooleanQuery();
		bq.add(dummyTerm, BooleanClause.Occur.SHOULD); // hits one doc
		bq.add(Csrq("data", "#", "#", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE), BooleanClause.Occur.SHOULD); // hits no docs
		result = search.search(bq, null, 1000).scoreDocs;
		numHits = result.Length;
		AssertEquals("wrong number of results", 1, numHits);
		for (int i = 0; i < numHits; i++)
		{
		  Assert.AreEqual("score for " + i + " was not the same", score, result[i].score, SCORE_COMP_THRESH);
		}

		bq = new BooleanQuery();
		bq.add(dummyTerm, BooleanClause.Occur.SHOULD); // hits one doc
		bq.add(Csrq("data", "#", "#", T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), BooleanClause.Occur.SHOULD); // hits no docs
		result = search.search(bq, null, 1000).scoreDocs;
		numHits = result.Length;
		AssertEquals("wrong number of results", 1, numHits);
		for (int i = 0; i < numHits; i++)
		{
		  Assert.AreEqual("score for " + i + " was not the same", score, result[i].score, SCORE_COMP_THRESH);
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testBoost() throws java.io.IOException
	  public virtual void TestBoost()
	  {
		// NOTE: uses index build in *this* setUp

		IndexSearcher search = newSearcher(Reader);

		// test for correct application of query normalization
		// must use a non score normalizing method for this.

		search.Similarity = new DefaultSimilarity();
		Query q = Csrq("data", "1", "6", T, T);
		q.Boost = 100;
		search.search(q, null, new CollectorAnonymousInnerClassHelper(this));

		//
		// Ensure that boosting works to score one clause of a query higher
		// than another.
		//
		Query q1 = Csrq("data", "A", "A", T, T); // matches document #0
		q1.Boost = .1f;
		Query q2 = Csrq("data", "Z", "Z", T, T); // matches document #1
		BooleanQuery bq = new BooleanQuery(true);
		bq.add(q1, BooleanClause.Occur.SHOULD);
		bq.add(q2, BooleanClause.Occur.SHOULD);

		ScoreDoc[] hits = search.search(bq, null, 1000).scoreDocs;
		Assert.Assert.AreEqual(1, hits[0].doc);
		Assert.Assert.AreEqual(0, hits[1].doc);
		Assert.IsTrue(hits[0].score > hits[1].score);

		q1 = Csrq("data", "A", "A", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE); // matches document #0
		q1.Boost = .1f;
		q2 = Csrq("data", "Z", "Z", T, T, MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE); // matches document #1
		bq = new BooleanQuery(true);
		bq.add(q1, BooleanClause.Occur.SHOULD);
		bq.add(q2, BooleanClause.Occur.SHOULD);

		hits = search.search(bq, null, 1000).scoreDocs;
		Assert.Assert.AreEqual(1, hits[0].doc);
		Assert.Assert.AreEqual(0, hits[1].doc);
		Assert.IsTrue(hits[0].score > hits[1].score);

		q1 = Csrq("data", "A", "A", T, T); // matches document #0
		q1.Boost = 10f;
		q2 = Csrq("data", "Z", "Z", T, T); // matches document #1
		bq = new BooleanQuery(true);
		bq.add(q1, BooleanClause.Occur.SHOULD);
		bq.add(q2, BooleanClause.Occur.SHOULD);

		hits = search.search(bq, null, 1000).scoreDocs;
		Assert.Assert.AreEqual(0, hits[0].doc);
		Assert.Assert.AreEqual(1, hits[1].doc);
		Assert.IsTrue(hits[0].score > hits[1].score);
	  }

	  private class CollectorAnonymousInnerClassHelper : Collector
	  {
		  private readonly TestMultiTermConstantScore OuterInstance;

		  public CollectorAnonymousInnerClassHelper(TestMultiTermConstantScore outerInstance)
		  {
			  this.OuterInstance = outerInstance;
			  @base = 0;
		  }

		  private int @base;
		  private Scorer scorer;
		  public override Scorer Scorer
		  {
			  set
			  {
				this.scorer = value;
			  }
		  }
		  public override void Collect(int doc)
		  {
			Assert.AreEqual("score for doc " + (doc + @base) + " was not correct", 1.0f, scorer.score(), SCORE_COMP_THRESH);
		  }
		  public override AtomicReaderContext NextReader
		  {
			  set
			  {
				@base = value.docBase;
			  }
		  }
		  public override bool AcceptsDocsOutOfOrder()
		  {
			return true;
		  }
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testBooleanOrderUnAffected() throws java.io.IOException
	  public virtual void TestBooleanOrderUnAffected()
	  {
		// NOTE: uses index build in *this* setUp

		IndexSearcher search = newSearcher(Reader);

		// first do a regular TermRangeQuery which uses term expansion so
		// docs with more terms in range get higher scores

		Query rq = TermRangeQuery.newStringRange("data", "1", "4", T, T);

		ScoreDoc[] expected = search.search(rq, null, 1000).scoreDocs;
		int numHits = expected.Length;

		// now do a boolean where which also contains a
		// ConstantScoreRangeQuery and make sure hte order is the same

		BooleanQuery q = new BooleanQuery();
		q.add(rq, BooleanClause.Occur.MUST); // T, F);
		q.add(Csrq("data", "1", "6", T, T), BooleanClause.Occur.MUST); // T, F);

		ScoreDoc[] actual = search.search(q, null, 1000).scoreDocs;

		AssertEquals("wrong numebr of hits", numHits, actual.Length);
		for (int i = 0; i < numHits; i++)
		{
		  AssertEquals("mismatch in docid for hit#" + i, expected[i].doc, actual[i].doc);
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRangeQueryId() throws java.io.IOException
	  public virtual void TestRangeQueryId()
	  {
		// NOTE: uses index build in *super* setUp

		IndexReader reader = SignedIndexReader;
		IndexSearcher search = newSearcher(reader);

		if (VERBOSE)
		{
		  Console.WriteLine("TEST: reader=" + reader);
		}

		int medId = ((MaxId - MinId) / 2);

		string minIP = Pad(MinId);
		string maxIP = Pad(MaxId);
		string medIP = Pad(medId);

		int numDocs = reader.numDocs();

		AssertEquals("num of docs", numDocs, 1 + MaxId - MinId);

		ScoreDoc[] result;

		// test id, bounded on both ends

		result = search.search(Csrq("id", minIP, maxIP, T, T), null, numDocs).scoreDocs;
		AssertEquals("find all", numDocs, result.Length);

		result = search.search(Csrq("id", minIP, maxIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).scoreDocs;
		AssertEquals("find all", numDocs, result.Length);

		result = search.search(Csrq("id", minIP, maxIP, T, F), null, numDocs).scoreDocs;
		AssertEquals("all but last", numDocs - 1, result.Length);

		result = search.search(Csrq("id", minIP, maxIP, T, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).scoreDocs;
		AssertEquals("all but last", numDocs - 1, result.Length);

		result = search.search(Csrq("id", minIP, maxIP, F, T), null, numDocs).scoreDocs;
		AssertEquals("all but first", numDocs - 1, result.Length);

		result = search.search(Csrq("id", minIP, maxIP, F, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).scoreDocs;
		AssertEquals("all but first", numDocs - 1, result.Length);

		result = search.search(Csrq("id", minIP, maxIP, F, F), null, numDocs).scoreDocs;
		AssertEquals("all but ends", numDocs - 2, result.Length);

		result = search.search(Csrq("id", minIP, maxIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).scoreDocs;
		AssertEquals("all but ends", numDocs - 2, result.Length);

		result = search.search(Csrq("id", medIP, maxIP, T, T), null, numDocs).scoreDocs;
		AssertEquals("med and up", 1 + MaxId - medId, result.Length);

		result = search.search(Csrq("id", medIP, maxIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).scoreDocs;
		AssertEquals("med and up", 1 + MaxId - medId, result.Length);

		result = search.search(Csrq("id", minIP, medIP, T, T), null, numDocs).scoreDocs;
		AssertEquals("up to med", 1 + medId - MinId, result.Length);

		result = search.search(Csrq("id", minIP, medIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).scoreDocs;
		AssertEquals("up to med", 1 + medId - MinId, result.Length);

		// unbounded id

		result = search.search(Csrq("id", minIP, null, T, F), null, numDocs).scoreDocs;
		AssertEquals("min and up", numDocs, result.Length);

		result = search.search(Csrq("id", null, maxIP, F, T), null, numDocs).scoreDocs;
		AssertEquals("max and down", numDocs, result.Length);

		result = search.search(Csrq("id", minIP, null, F, F), null, numDocs).scoreDocs;
		AssertEquals("not min, but up", numDocs - 1, result.Length);

		result = search.search(Csrq("id", null, maxIP, F, F), null, numDocs).scoreDocs;
		AssertEquals("not max, but down", numDocs - 1, result.Length);

		result = search.search(Csrq("id", medIP, maxIP, T, F), null, numDocs).scoreDocs;
		AssertEquals("med and up, not max", MaxId - medId, result.Length);

		result = search.search(Csrq("id", minIP, medIP, F, T), null, numDocs).scoreDocs;
		AssertEquals("not min, up to med", medId - MinId, result.Length);

		// very small sets

		result = search.search(Csrq("id", minIP, minIP, F, F), null, numDocs).scoreDocs;
		AssertEquals("min,min,F,F", 0, result.Length);

		result = search.search(Csrq("id", minIP, minIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).scoreDocs;
		AssertEquals("min,min,F,F", 0, result.Length);

		result = search.search(Csrq("id", medIP, medIP, F, F), null, numDocs).scoreDocs;
		AssertEquals("med,med,F,F", 0, result.Length);

		result = search.search(Csrq("id", medIP, medIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).scoreDocs;
		AssertEquals("med,med,F,F", 0, result.Length);

		result = search.search(Csrq("id", maxIP, maxIP, F, F), null, numDocs).scoreDocs;
		AssertEquals("max,max,F,F", 0, result.Length);

		result = search.search(Csrq("id", maxIP, maxIP, F, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).scoreDocs;
		AssertEquals("max,max,F,F", 0, result.Length);

		result = search.search(Csrq("id", minIP, minIP, T, T), null, numDocs).scoreDocs;
		AssertEquals("min,min,T,T", 1, result.Length);

		result = search.search(Csrq("id", minIP, minIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).scoreDocs;
		AssertEquals("min,min,T,T", 1, result.Length);

		result = search.search(Csrq("id", null, minIP, F, T), null, numDocs).scoreDocs;
		AssertEquals("nul,min,F,T", 1, result.Length);

		result = search.search(Csrq("id", null, minIP, F, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).scoreDocs;
		AssertEquals("nul,min,F,T", 1, result.Length);

		result = search.search(Csrq("id", maxIP, maxIP, T, T), null, numDocs).scoreDocs;
		AssertEquals("max,max,T,T", 1, result.Length);

		result = search.search(Csrq("id", maxIP, maxIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).scoreDocs;
		AssertEquals("max,max,T,T", 1, result.Length);

		result = search.search(Csrq("id", maxIP, null, T, F), null, numDocs).scoreDocs;
		AssertEquals("max,nul,T,T", 1, result.Length);

		result = search.search(Csrq("id", maxIP, null, T, F, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).scoreDocs;
		AssertEquals("max,nul,T,T", 1, result.Length);

		result = search.search(Csrq("id", medIP, medIP, T, T), null, numDocs).scoreDocs;
		AssertEquals("med,med,T,T", 1, result.Length);

		result = search.search(Csrq("id", medIP, medIP, T, T, MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT), null, numDocs).scoreDocs;
		AssertEquals("med,med,T,T", 1, result.Length);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRangeQueryRand() throws java.io.IOException
	  public virtual void TestRangeQueryRand()
	  {
		// NOTE: uses index build in *super* setUp

		IndexReader reader = SignedIndexReader;
		IndexSearcher search = newSearcher(reader);

		string minRP = Pad(SignedIndexDir.MinR);
		string maxRP = Pad(SignedIndexDir.MaxR);

		int numDocs = reader.numDocs();

		AssertEquals("num of docs", numDocs, 1 + MaxId - MinId);

		ScoreDoc[] result;

		// test extremes, bounded on both ends

		result = search.search(Csrq("rand", minRP, maxRP, T, T), null, numDocs).scoreDocs;
		AssertEquals("find all", numDocs, result.Length);

		result = search.search(Csrq("rand", minRP, maxRP, T, F), null, numDocs).scoreDocs;
		AssertEquals("all but biggest", numDocs - 1, result.Length);

		result = search.search(Csrq("rand", minRP, maxRP, F, T), null, numDocs).scoreDocs;
		AssertEquals("all but smallest", numDocs - 1, result.Length);

		result = search.search(Csrq("rand", minRP, maxRP, F, F), null, numDocs).scoreDocs;
		AssertEquals("all but extremes", numDocs - 2, result.Length);

		// unbounded

		result = search.search(Csrq("rand", minRP, null, T, F), null, numDocs).scoreDocs;
		AssertEquals("smallest and up", numDocs, result.Length);

		result = search.search(Csrq("rand", null, maxRP, F, T), null, numDocs).scoreDocs;
		AssertEquals("biggest and down", numDocs, result.Length);

		result = search.search(Csrq("rand", minRP, null, F, F), null, numDocs).scoreDocs;
		AssertEquals("not smallest, but up", numDocs - 1, result.Length);

		result = search.search(Csrq("rand", null, maxRP, F, F), null, numDocs).scoreDocs;
		AssertEquals("not biggest, but down", numDocs - 1, result.Length);

		// very small sets

		result = search.search(Csrq("rand", minRP, minRP, F, F), null, numDocs).scoreDocs;
		AssertEquals("min,min,F,F", 0, result.Length);
		result = search.search(Csrq("rand", maxRP, maxRP, F, F), null, numDocs).scoreDocs;
		AssertEquals("max,max,F,F", 0, result.Length);

		result = search.search(Csrq("rand", minRP, minRP, T, T), null, numDocs).scoreDocs;
		AssertEquals("min,min,T,T", 1, result.Length);
		result = search.search(Csrq("rand", null, minRP, F, T), null, numDocs).scoreDocs;
		AssertEquals("nul,min,F,T", 1, result.Length);

		result = search.search(Csrq("rand", maxRP, maxRP, T, T), null, numDocs).scoreDocs;
		AssertEquals("max,max,T,T", 1, result.Length);
		result = search.search(Csrq("rand", maxRP, null, T, F), null, numDocs).scoreDocs;
		AssertEquals("max,nul,T,T", 1, result.Length);
	  }
	}

}