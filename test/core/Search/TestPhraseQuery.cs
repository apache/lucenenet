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


	using Lucene.Net.Analysis;
	using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
	using Lucene.Net.Document;
	using Lucene.Net.Index;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using Directory = Lucene.Net.Store.Directory;
	using Lucene.Net.Util;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;

	using Seed = com.carrotsearch.randomizedtesting.annotations.Seed;

	/// <summary>
	/// Tests <seealso cref="PhraseQuery"/>.
	/// </summary>
	/// <seealso cref= TestPositionIncrement </seealso>
	/*
	 * Remove ThreadLeaks and run with (Eclipse or command line):
	 * -ea -Drt.seed=AFD1E7E84B35D2B1
	 * to get leaked thread errors.
	 */
	// @ThreadLeaks(linger = 1000, leakedThreadsBelongToSuite = true)
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Seed("AFD1E7E84B35D2B1") public class TestPhraseQuery extends LuceneTestCase
	public class TestPhraseQuery : LuceneTestCase
	{

	  /// <summary>
	  /// threshold for comparing floats </summary>
	  public const float SCORE_COMP_THRESH = 1e-6f;

	  private static IndexSearcher Searcher;
	  private static IndexReader Reader;
	  private PhraseQuery Query;
	  private static Directory Directory;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		Directory = newDirectory();
		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory, analyzer);

		Document doc = new Document();
		doc.add(newTextField("field", "one two three four five", Field.Store.YES));
		doc.add(newTextField("repeated", "this is a repeated field - first part", Field.Store.YES));
		IndexableField repeatedField = newTextField("repeated", "second part of a repeated field", Field.Store.YES);
		doc.add(repeatedField);
		doc.add(newTextField("palindrome", "one two three two one", Field.Store.YES));
		writer.addDocument(doc);

		doc = new Document();
		doc.add(newTextField("nonexist", "phrase exist notexist exist found", Field.Store.YES));
		writer.addDocument(doc);

		doc = new Document();
		doc.add(newTextField("nonexist", "phrase exist notexist exist found", Field.Store.YES));
		writer.addDocument(doc);

		Reader = writer.Reader;
		writer.close();

		Searcher = newSearcher(Reader);
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper()
		  {
		  }

		  public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		  {
			return new TokenStreamComponents(new MockTokenizer(reader, MockTokenizer.WHITESPACE, false));
		  }

		  public override int GetPositionIncrementGap(string fieldName)
		  {
			return 100;
		  }
	  }

	  public override void SetUp()
	  {
		base.setUp();
		Query = new PhraseQuery();
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

	  public virtual void TestNotCloseEnough()
	  {
		Query.Slop = 2;
		Query.add(new Term("field", "one"));
		Query.add(new Term("field", "five"));
		ScoreDoc[] hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);
		QueryUtils.check(random(), Query,Searcher);
	  }

	  public virtual void TestBarelyCloseEnough()
	  {
		Query.Slop = 3;
		Query.add(new Term("field", "one"));
		Query.add(new Term("field", "five"));
		ScoreDoc[] hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		QueryUtils.check(random(), Query,Searcher);
	  }

	  /// <summary>
	  /// Ensures slop of 0 works for exact matches, but not reversed
	  /// </summary>
	  public virtual void TestExact()
	  {
		// slop is zero by default
		Query.add(new Term("field", "four"));
		Query.add(new Term("field", "five"));
		ScoreDoc[] hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("exact match", 1, hits.Length);
		QueryUtils.check(random(), Query,Searcher);


		Query = new PhraseQuery();
		Query.add(new Term("field", "two"));
		Query.add(new Term("field", "one"));
		hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("reverse not exact", 0, hits.Length);
		QueryUtils.check(random(), Query,Searcher);
	  }

	  public virtual void TestSlop1()
	  {
		// Ensures slop of 1 works with terms in order.
		Query.Slop = 1;
		Query.add(new Term("field", "one"));
		Query.add(new Term("field", "two"));
		ScoreDoc[] hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("in order", 1, hits.Length);
		QueryUtils.check(random(), Query,Searcher);


		// Ensures slop of 1 does not work for phrases out of order;
		// must be at least 2.
		Query = new PhraseQuery();
		Query.Slop = 1;
		Query.add(new Term("field", "two"));
		Query.add(new Term("field", "one"));
		hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("reversed, slop not 2 or more", 0, hits.Length);
		QueryUtils.check(random(), Query,Searcher);
	  }

	  /// <summary>
	  /// As long as slop is at least 2, terms can be reversed
	  /// </summary>
	  public virtual void TestOrderDoesntMatter()
	  {
		Query.Slop = 2; // must be at least two for reverse order match
		Query.add(new Term("field", "two"));
		Query.add(new Term("field", "one"));
		ScoreDoc[] hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("just sloppy enough", 1, hits.Length);
		QueryUtils.check(random(), Query,Searcher);


		Query = new PhraseQuery();
		Query.Slop = 2;
		Query.add(new Term("field", "three"));
		Query.add(new Term("field", "one"));
		hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("not sloppy enough", 0, hits.Length);
		QueryUtils.check(random(), Query,Searcher);

	  }

	  /// <summary>
	  /// slop is the total number of positional moves allowed
	  /// to line up a phrase
	  /// </summary>
	  public virtual void TestMulipleTerms()
	  {
		Query.Slop = 2;
		Query.add(new Term("field", "one"));
		Query.add(new Term("field", "three"));
		Query.add(new Term("field", "five"));
		ScoreDoc[] hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("two total moves", 1, hits.Length);
		QueryUtils.check(random(), Query,Searcher);


		Query = new PhraseQuery();
		Query.Slop = 5; // it takes six moves to match this phrase
		Query.add(new Term("field", "five"));
		Query.add(new Term("field", "three"));
		Query.add(new Term("field", "one"));
		hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("slop of 5 not close enough", 0, hits.Length);
		QueryUtils.check(random(), Query,Searcher);


		Query.Slop = 6;
		hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("slop of 6 just right", 1, hits.Length);
		QueryUtils.check(random(), Query,Searcher);

	  }

	  public virtual void TestPhraseQueryWithStopAnalyzer()
	  {
		Directory directory = newDirectory();
		Analyzer stopAnalyzer = new MockAnalyzer(random(), MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET);
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory, newIndexWriterConfig(TEST_VERSION_CURRENT, stopAnalyzer));
		Document doc = new Document();
		doc.add(newTextField("field", "the stop words are here", Field.Store.YES));
		writer.addDocument(doc);
		IndexReader reader = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(reader);

		// valid exact phrase query
		PhraseQuery query = new PhraseQuery();
		query.add(new Term("field","stop"));
		query.add(new Term("field","words"));
		ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		QueryUtils.check(random(), query,searcher);

		reader.close();
		directory.close();
	  }

	  public virtual void TestPhraseQueryInConjunctionScorer()
	  {
		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory);

		Document doc = new Document();
		doc.add(newTextField("source", "marketing info", Field.Store.YES));
		writer.addDocument(doc);

		doc = new Document();
		doc.add(newTextField("contents", "foobar", Field.Store.YES));
		doc.add(newTextField("source", "marketing info", Field.Store.YES));
		writer.addDocument(doc);

		IndexReader reader = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(reader);

		PhraseQuery phraseQuery = new PhraseQuery();
		phraseQuery.add(new Term("source", "marketing"));
		phraseQuery.add(new Term("source", "info"));
		ScoreDoc[] hits = searcher.search(phraseQuery, null, 1000).scoreDocs;
		Assert.AreEqual(2, hits.Length);
		QueryUtils.check(random(), phraseQuery,searcher);


		TermQuery termQuery = new TermQuery(new Term("contents","foobar"));
		BooleanQuery booleanQuery = new BooleanQuery();
		booleanQuery.add(termQuery, BooleanClause.Occur.MUST);
		booleanQuery.add(phraseQuery, BooleanClause.Occur.MUST);
		hits = searcher.search(booleanQuery, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		QueryUtils.check(random(), termQuery,searcher);


		reader.close();

		writer = new RandomIndexWriter(random(), directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE));
		doc = new Document();
		doc.add(newTextField("contents", "map entry woo", Field.Store.YES));
		writer.addDocument(doc);

		doc = new Document();
		doc.add(newTextField("contents", "woo map entry", Field.Store.YES));
		writer.addDocument(doc);

		doc = new Document();
		doc.add(newTextField("contents", "map foobarword entry woo", Field.Store.YES));
		writer.addDocument(doc);

		reader = writer.Reader;
		writer.close();

		searcher = newSearcher(reader);

		termQuery = new TermQuery(new Term("contents","woo"));
		phraseQuery = new PhraseQuery();
		phraseQuery.add(new Term("contents","map"));
		phraseQuery.add(new Term("contents","entry"));

		hits = searcher.search(termQuery, null, 1000).scoreDocs;
		Assert.AreEqual(3, hits.Length);
		hits = searcher.search(phraseQuery, null, 1000).scoreDocs;
		Assert.AreEqual(2, hits.Length);


		booleanQuery = new BooleanQuery();
		booleanQuery.add(termQuery, BooleanClause.Occur.MUST);
		booleanQuery.add(phraseQuery, BooleanClause.Occur.MUST);
		hits = searcher.search(booleanQuery, null, 1000).scoreDocs;
		Assert.AreEqual(2, hits.Length);

		booleanQuery = new BooleanQuery();
		booleanQuery.add(phraseQuery, BooleanClause.Occur.MUST);
		booleanQuery.add(termQuery, BooleanClause.Occur.MUST);
		hits = searcher.search(booleanQuery, null, 1000).scoreDocs;
		Assert.AreEqual(2, hits.Length);
		QueryUtils.check(random(), booleanQuery,searcher);


		reader.close();
		directory.close();
	  }

	  public virtual void TestSlopScoring()
	  {
		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()).setSimilarity(new DefaultSimilarity()));

		Document doc = new Document();
		doc.add(newTextField("field", "foo firstname lastname foo", Field.Store.YES));
		writer.addDocument(doc);

		Document doc2 = new Document();
		doc2.add(newTextField("field", "foo firstname zzz lastname foo", Field.Store.YES));
		writer.addDocument(doc2);

		Document doc3 = new Document();
		doc3.add(newTextField("field", "foo firstname zzz yyy lastname foo", Field.Store.YES));
		writer.addDocument(doc3);

		IndexReader reader = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(reader);
		searcher.Similarity = new DefaultSimilarity();
		PhraseQuery query = new PhraseQuery();
		query.add(new Term("field", "firstname"));
		query.add(new Term("field", "lastname"));
		query.Slop = int.MaxValue;
		ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(3, hits.Length);
		// Make sure that those matches where the terms appear closer to
		// each other get a higher score:
		Assert.AreEqual(0.71, hits[0].score, 0.01);
		Assert.AreEqual(0, hits[0].doc);
		Assert.AreEqual(0.44, hits[1].score, 0.01);
		Assert.AreEqual(1, hits[1].doc);
		Assert.AreEqual(0.31, hits[2].score, 0.01);
		Assert.AreEqual(2, hits[2].doc);
		QueryUtils.check(random(), query,searcher);
		reader.close();
		directory.close();
	  }

	  public virtual void TestToString()
	  {
		PhraseQuery q = new PhraseQuery(); // Query "this hi this is a test is"
		q.add(new Term("field", "hi"), 1);
		q.add(new Term("field", "test"), 5);

		Assert.AreEqual("field:\"? hi ? ? ? test\"", q.ToString());
		q.add(new Term("field", "hello"), 1);
		Assert.AreEqual("field:\"? hi|hello ? ? ? test\"", q.ToString());
	  }

	  public virtual void TestWrappedPhrase()
	  {
		Query.add(new Term("repeated", "first"));
		Query.add(new Term("repeated", "part"));
		Query.add(new Term("repeated", "second"));
		Query.add(new Term("repeated", "part"));
		Query.Slop = 100;

		ScoreDoc[] hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("slop of 100 just right", 1, hits.Length);
		QueryUtils.check(random(), Query,Searcher);

		Query.Slop = 99;

		hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("slop of 99 not enough", 0, hits.Length);
		QueryUtils.check(random(), Query,Searcher);
	  }

	  // work on two docs like this: "phrase exist notexist exist found"
	  public virtual void TestNonExistingPhrase()
	  {
		// phrase without repetitions that exists in 2 docs
		Query.add(new Term("nonexist", "phrase"));
		Query.add(new Term("nonexist", "notexist"));
		Query.add(new Term("nonexist", "found"));
		Query.Slop = 2; // would be found this way

		ScoreDoc[] hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("phrase without repetitions exists in 2 docs", 2, hits.Length);
		QueryUtils.check(random(), Query,Searcher);

		// phrase with repetitions that exists in 2 docs
		Query = new PhraseQuery();
		Query.add(new Term("nonexist", "phrase"));
		Query.add(new Term("nonexist", "exist"));
		Query.add(new Term("nonexist", "exist"));
		Query.Slop = 1; // would be found

		hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("phrase with repetitions exists in two docs", 2, hits.Length);
		QueryUtils.check(random(), Query,Searcher);

		// phrase I with repetitions that does not exist in any doc
		Query = new PhraseQuery();
		Query.add(new Term("nonexist", "phrase"));
		Query.add(new Term("nonexist", "notexist"));
		Query.add(new Term("nonexist", "phrase"));
		Query.Slop = 1000; // would not be found no matter how high the slop is

		hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("nonexisting phrase with repetitions does not exist in any doc", 0, hits.Length);
		QueryUtils.check(random(), Query,Searcher);

		// phrase II with repetitions that does not exist in any doc
		Query = new PhraseQuery();
		Query.add(new Term("nonexist", "phrase"));
		Query.add(new Term("nonexist", "exist"));
		Query.add(new Term("nonexist", "exist"));
		Query.add(new Term("nonexist", "exist"));
		Query.Slop = 1000; // would not be found no matter how high the slop is

		hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("nonexisting phrase with repetitions does not exist in any doc", 0, hits.Length);
		QueryUtils.check(random(), Query,Searcher);

	  }

	  /// <summary>
	  /// Working on a 2 fields like this:
	  ///    Field("field", "one two three four five")
	  ///    Field("palindrome", "one two three two one")
	  /// Phrase of size 2 occuriong twice, once in order and once in reverse, 
	  /// because doc is a palyndrome, is counted twice. 
	  /// Also, in this case order in query does not matter. 
	  /// Also, when an exact match is found, both sloppy scorer and exact scorer scores the same.   
	  /// </summary>
	  public virtual void TestPalyndrome2()
	  {

		// search on non palyndrome, find phrase with no slop, using exact phrase scorer
		Query.Slop = 0; // to use exact phrase scorer
		Query.add(new Term("field", "two"));
		Query.add(new Term("field", "three"));
		ScoreDoc[] hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("phrase found with exact phrase scorer", 1, hits.Length);
		float score0 = hits[0].score;
		//System.out.println("(exact) field: two three: "+score0);
		QueryUtils.check(random(), Query,Searcher);

		// search on non palyndrome, find phrase with slop 2, though no slop required here.
		Query.Slop = 2; // to use sloppy scorer
		hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("just sloppy enough", 1, hits.Length);
		float score1 = hits[0].score;
		//System.out.println("(sloppy) field: two three: "+score1);
		Assert.AreEqual("exact scorer and sloppy scorer score the same when slop does not matter",score0, score1, SCORE_COMP_THRESH);
		QueryUtils.check(random(), Query,Searcher);

		// search ordered in palyndrome, find it twice
		Query = new PhraseQuery();
		Query.Slop = 2; // must be at least two for both ordered and reversed to match
		Query.add(new Term("palindrome", "two"));
		Query.add(new Term("palindrome", "three"));
		hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("just sloppy enough", 1, hits.Length);
		//float score2 = hits[0].score;
		//System.out.println("palindrome: two three: "+score2);
		QueryUtils.check(random(), Query,Searcher);

		//commented out for sloppy-phrase efficiency (issue 736) - see SloppyPhraseScorer.phraseFreq(). 
		//Assert.IsTrue("ordered scores higher in palindrome",score1+SCORE_COMP_THRESH<score2);

		// search reveresed in palyndrome, find it twice
		Query = new PhraseQuery();
		Query.Slop = 2; // must be at least two for both ordered and reversed to match
		Query.add(new Term("palindrome", "three"));
		Query.add(new Term("palindrome", "two"));
		hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("just sloppy enough", 1, hits.Length);
		//float score3 = hits[0].score;
		//System.out.println("palindrome: three two: "+score3);
		QueryUtils.check(random(), Query,Searcher);

		//commented out for sloppy-phrase efficiency (issue 736) - see SloppyPhraseScorer.phraseFreq(). 
		//Assert.IsTrue("reversed scores higher in palindrome",score1+SCORE_COMP_THRESH<score3);
		//Assert.AreEqual("ordered or reversed does not matter",score2, score3, SCORE_COMP_THRESH);
	  }

	  /// <summary>
	  /// Working on a 2 fields like this:
	  ///    Field("field", "one two three four five")
	  ///    Field("palindrome", "one two three two one")
	  /// Phrase of size 3 occuriong twice, once in order and once in reverse, 
	  /// because doc is a palyndrome, is counted twice. 
	  /// Also, in this case order in query does not matter. 
	  /// Also, when an exact match is found, both sloppy scorer and exact scorer scores the same.   
	  /// </summary>
	  public virtual void TestPalyndrome3()
	  {

		// search on non palyndrome, find phrase with no slop, using exact phrase scorer
		Query.Slop = 0; // to use exact phrase scorer
		Query.add(new Term("field", "one"));
		Query.add(new Term("field", "two"));
		Query.add(new Term("field", "three"));
		ScoreDoc[] hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("phrase found with exact phrase scorer", 1, hits.Length);
		float score0 = hits[0].score;
		//System.out.println("(exact) field: one two three: "+score0);
		QueryUtils.check(random(), Query,Searcher);

		// just make sure no exc:
		Searcher.explain(Query, 0);

		// search on non palyndrome, find phrase with slop 3, though no slop required here.
		Query.Slop = 4; // to use sloppy scorer
		hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("just sloppy enough", 1, hits.Length);
		float score1 = hits[0].score;
		//System.out.println("(sloppy) field: one two three: "+score1);
		Assert.AreEqual("exact scorer and sloppy scorer score the same when slop does not matter",score0, score1, SCORE_COMP_THRESH);
		QueryUtils.check(random(), Query,Searcher);

		// search ordered in palyndrome, find it twice
		Query = new PhraseQuery();
		Query.Slop = 4; // must be at least four for both ordered and reversed to match
		Query.add(new Term("palindrome", "one"));
		Query.add(new Term("palindrome", "two"));
		Query.add(new Term("palindrome", "three"));
		hits = Searcher.search(Query, null, 1000).scoreDocs;

		// just make sure no exc:
		Searcher.explain(Query, 0);

		Assert.AreEqual("just sloppy enough", 1, hits.Length);
		//float score2 = hits[0].score;
		//System.out.println("palindrome: one two three: "+score2);
		QueryUtils.check(random(), Query,Searcher);

		//commented out for sloppy-phrase efficiency (issue 736) - see SloppyPhraseScorer.phraseFreq(). 
		//Assert.IsTrue("ordered scores higher in palindrome",score1+SCORE_COMP_THRESH<score2);

		// search reveresed in palyndrome, find it twice
		Query = new PhraseQuery();
		Query.Slop = 4; // must be at least four for both ordered and reversed to match
		Query.add(new Term("palindrome", "three"));
		Query.add(new Term("palindrome", "two"));
		Query.add(new Term("palindrome", "one"));
		hits = Searcher.search(Query, null, 1000).scoreDocs;
		Assert.AreEqual("just sloppy enough", 1, hits.Length);
		//float score3 = hits[0].score;
		//System.out.println("palindrome: three two one: "+score3);
		QueryUtils.check(random(), Query,Searcher);

		//commented out for sloppy-phrase efficiency (issue 736) - see SloppyPhraseScorer.phraseFreq(). 
		//Assert.IsTrue("reversed scores higher in palindrome",score1+SCORE_COMP_THRESH<score3);
		//Assert.AreEqual("ordered or reversed does not matter",score2, score3, SCORE_COMP_THRESH);
	  }

	  // LUCENE-1280
	  public virtual void TestEmptyPhraseQuery()
	  {
		BooleanQuery q2 = new BooleanQuery();
		q2.add(new PhraseQuery(), BooleanClause.Occur.MUST);
		q2.ToString();
	  }

	  /* test that a single term is rewritten to a term query */
	  public virtual void TestRewrite()
	  {
		PhraseQuery pq = new PhraseQuery();
		pq.add(new Term("foo", "bar"));
		Query rewritten = pq.rewrite(Searcher.IndexReader);
		Assert.IsTrue(rewritten is TermQuery);
	  }

	  public virtual void TestRandomPhrases()
	  {
		Directory dir = newDirectory();
		Analyzer analyzer = new MockAnalyzer(random());

		RandomIndexWriter w = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMergePolicy(newLogMergePolicy()));
		IList<IList<string>> docs = new List<IList<string>>();
		Document d = new Document();
		Field f = newTextField("f", "", Field.Store.NO);
		d.add(f);

		Random r = random();

		int NUM_DOCS = atLeast(10);
		for (int i = 0; i < NUM_DOCS; i++)
		{
		  // must be > 4096 so it spans multiple chunks
		  int termCount = TestUtil.Next(random(), 4097, 8200);

		  IList<string> doc = new List<string>();

		  StringBuilder sb = new StringBuilder();
		  while (doc.Count < termCount)
		  {
			if (r.Next(5) == 1 || docs.Count == 0)
			{
			  // make new non-empty-string term
			  string term;
			  while (true)
			  {
				term = TestUtil.randomUnicodeString(r);
				if (term.Length > 0)
				{
				  break;
				}
			  }
			  IOException priorException = null;
			  TokenStream ts = analyzer.tokenStream("ignore", term);
			  try
			  {
				CharTermAttribute termAttr = ts.AddAttribute<CharTermAttribute>();
				ts.reset();
				while (ts.IncrementToken())
				{
				  string text = termAttr.ToString();
				  doc.Add(text);
				  sb.Append(text).Append(' ');
				}
				ts.end();
			  }
			  catch (IOException e)
			  {
				priorException = e;
			  }
			  finally
			  {
				IOUtils.closeWhileHandlingException(priorException, ts);
			  }
			}
			else
			{
			  // pick existing sub-phrase
			  IList<string> lastDoc = docs[r.Next(docs.Count)];
			  int len = TestUtil.Next(r, 1, 10);
			  int start = r.Next(lastDoc.Count - len);
			  for (int k = start;k < start + len;k++)
			  {
				string t = lastDoc[k];
				doc.Add(t);
				sb.Append(t).Append(' ');
			  }
			}
		  }
		  docs.Add(doc);
		  f.StringValue = sb.ToString();
		  w.addDocument(d);
		}

		IndexReader reader = w.Reader;
		IndexSearcher s = newSearcher(reader);
		w.close();

		// now search
		int num = atLeast(10);
		for (int i = 0;i < num;i++)
		{
		  int docID = r.Next(docs.Count);
		  IList<string> doc = docs[docID];

		  int numTerm = TestUtil.Next(r, 2, 20);
		  int start = r.Next(doc.Count - numTerm);
		  PhraseQuery pq = new PhraseQuery();
		  StringBuilder sb = new StringBuilder();
		  for (int t = start;t < start + numTerm;t++)
		  {
			pq.add(new Term("f", doc[t]));
			sb.Append(doc[t]).Append(' ');
		  }

		  TopDocs hits = s.search(pq, NUM_DOCS);
		  bool found = false;
		  for (int j = 0;j < hits.scoreDocs.length;j++)
		  {
			if (hits.scoreDocs[j].doc == docID)
			{
			  found = true;
			  break;
			}
		  }

		  Assert.IsTrue("phrase '" + sb + "' not found; start=" + start, found);
		}

		reader.close();
		dir.close();
	  }

	  public virtual void TestNegativeSlop()
	  {
		PhraseQuery query = new PhraseQuery();
		query.add(new Term("field", "two"));
		query.add(new Term("field", "one"));
		try
		{
		  query.Slop = -2;
		  Assert.Fail("didn't get expected exception");
		}
		catch (System.ArgumentException expected)
		{
		  // expected exception
		}
	  }
	}

}