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
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using Similarity = Lucene.Net.Search.Similarities.Similarity;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;
	using Test = org.junit.Test;

	/// <summary>
	/// Test BooleanQuery2 against BooleanQuery by overriding the standard query parser.
	/// this also tests the scoring order of BooleanQuery.
	/// </summary>
	public class TestBoolean2 : LuceneTestCase
	{
	  private static IndexSearcher Searcher;
	  private static IndexSearcher BigSearcher;
	  private static IndexReader Reader;
	  private static IndexReader LittleReader;
	  private static int NUM_EXTRA_DOCS = 6000;

	  public const string Field = "field";
	  private static Directory Directory;
	  private static Directory Dir2;
	  private static int MulFactor;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		Directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
		for (int i = 0; i < DocFields.Length; i++)
		{
		  Document doc = new Document();
		  doc.add(newTextField(Field, DocFields[i], Field.Store.NO));
		  writer.addDocument(doc);
		}
		writer.close();
		LittleReader = DirectoryReader.open(Directory);
		Searcher = newSearcher(LittleReader);
		// this is intentionally using the baseline sim, because it compares against bigSearcher (which uses a random one)
		Searcher.Similarity = new DefaultSimilarity();

		// Make big index
		Dir2 = new MockDirectoryWrapper(random(), new RAMDirectory(Directory, IOContext.DEFAULT));

		// First multiply small test index:
		MulFactor = 1;
		int docCount = 0;
		if (VERBOSE)
		{
		  Console.WriteLine("\nTEST: now copy index...");
		}
		do
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: cycle...");
		  }
		  Directory copy = new MockDirectoryWrapper(random(), new RAMDirectory(Dir2, IOContext.DEFAULT));
		  RandomIndexWriter w = new RandomIndexWriter(random(), Dir2);
		  w.addIndexes(copy);
		  docCount = w.maxDoc();
		  w.close();
		  MulFactor *= 2;
		} while (docCount < 3000);

		RandomIndexWriter w = new RandomIndexWriter(random(), Dir2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(TestUtil.Next(random(), 50, 1000)));
		Document doc = new Document();
		doc.add(newTextField("field2", "xxx", Field.Store.NO));
		for (int i = 0;i < NUM_EXTRA_DOCS / 2;i++)
		{
		  w.addDocument(doc);
		}
		doc = new Document();
		doc.add(newTextField("field2", "big bad bug", Field.Store.NO));
		for (int i = 0;i < NUM_EXTRA_DOCS / 2;i++)
		{
		  w.addDocument(doc);
		}
		Reader = w.Reader;
		BigSearcher = newSearcher(Reader);
		w.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass() throws Exception
	  public static void AfterClass()
	  {
		Reader.close();
		LittleReader.close();
		Dir2.close();
		Directory.close();
		Searcher = null;
		Reader = null;
		LittleReader = null;
		Dir2 = null;
		Directory = null;
		BigSearcher = null;
	  }

	  private static string[] DocFields = new string[] {"w1 w2 w3 w4 w5", "w1 w3 w2 w3", "w1 xx w2 yy w3", "w1 w3 xx w2 yy w3"};

	  public virtual void QueriesTest(Query query, int[] expDocNrs)
	  {
		TopScoreDocCollector collector = TopScoreDocCollector.create(1000, false);
		Searcher.search(query, null, collector);
		ScoreDoc[] hits1 = collector.topDocs().scoreDocs;

		collector = TopScoreDocCollector.create(1000, true);
		Searcher.search(query, null, collector);
		ScoreDoc[] hits2 = collector.topDocs().scoreDocs;

		Assert.AreEqual(MulFactor * collector.totalHits, BigSearcher.search(query, 1).totalHits);

		CheckHits.checkHitsQuery(query, hits1, hits2, expDocNrs);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testQueries01() throws Exception
	  public virtual void TestQueries01()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(Field, "w3")), BooleanClause.Occur.MUST);
		query.add(new TermQuery(new Term(Field, "xx")), BooleanClause.Occur.MUST);
		int[] expDocNrs = new int[] {2,3};
		QueriesTest(query, expDocNrs);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testQueries02() throws Exception
	  public virtual void TestQueries02()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(Field, "w3")), BooleanClause.Occur.MUST);
		query.add(new TermQuery(new Term(Field, "xx")), BooleanClause.Occur.SHOULD);
		int[] expDocNrs = new int[] {2,3,1,0};
		QueriesTest(query, expDocNrs);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testQueries03() throws Exception
	  public virtual void TestQueries03()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(Field, "w3")), BooleanClause.Occur.SHOULD);
		query.add(new TermQuery(new Term(Field, "xx")), BooleanClause.Occur.SHOULD);
		int[] expDocNrs = new int[] {2,3,1,0};
		QueriesTest(query, expDocNrs);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testQueries04() throws Exception
	  public virtual void TestQueries04()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(Field, "w3")), BooleanClause.Occur.SHOULD);
		query.add(new TermQuery(new Term(Field, "xx")), BooleanClause.Occur.MUST_NOT);
		int[] expDocNrs = new int[] {1,0};
		QueriesTest(query, expDocNrs);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testQueries05() throws Exception
	  public virtual void TestQueries05()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(Field, "w3")), BooleanClause.Occur.MUST);
		query.add(new TermQuery(new Term(Field, "xx")), BooleanClause.Occur.MUST_NOT);
		int[] expDocNrs = new int[] {1,0};
		QueriesTest(query, expDocNrs);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testQueries06() throws Exception
	  public virtual void TestQueries06()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(Field, "w3")), BooleanClause.Occur.MUST);
		query.add(new TermQuery(new Term(Field, "xx")), BooleanClause.Occur.MUST_NOT);
		query.add(new TermQuery(new Term(Field, "w5")), BooleanClause.Occur.MUST_NOT);
		int[] expDocNrs = new int[] {1};
		QueriesTest(query, expDocNrs);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testQueries07() throws Exception
	  public virtual void TestQueries07()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(Field, "w3")), BooleanClause.Occur.MUST_NOT);
		query.add(new TermQuery(new Term(Field, "xx")), BooleanClause.Occur.MUST_NOT);
		query.add(new TermQuery(new Term(Field, "w5")), BooleanClause.Occur.MUST_NOT);
		int[] expDocNrs = new int[] {};
		QueriesTest(query, expDocNrs);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testQueries08() throws Exception
	  public virtual void TestQueries08()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(Field, "w3")), BooleanClause.Occur.MUST);
		query.add(new TermQuery(new Term(Field, "xx")), BooleanClause.Occur.SHOULD);
		query.add(new TermQuery(new Term(Field, "w5")), BooleanClause.Occur.MUST_NOT);
		int[] expDocNrs = new int[] {2,3,1};
		QueriesTest(query, expDocNrs);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testQueries09() throws Exception
	  public virtual void TestQueries09()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(Field, "w3")), BooleanClause.Occur.MUST);
		query.add(new TermQuery(new Term(Field, "xx")), BooleanClause.Occur.MUST);
		query.add(new TermQuery(new Term(Field, "w2")), BooleanClause.Occur.MUST);
		query.add(new TermQuery(new Term(Field, "zz")), BooleanClause.Occur.SHOULD);
		int[] expDocNrs = new int[] {2, 3};
		QueriesTest(query, expDocNrs);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testQueries10() throws Exception
	  public virtual void TestQueries10()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(Field, "w3")), BooleanClause.Occur.MUST);
		query.add(new TermQuery(new Term(Field, "xx")), BooleanClause.Occur.MUST);
		query.add(new TermQuery(new Term(Field, "w2")), BooleanClause.Occur.MUST);
		query.add(new TermQuery(new Term(Field, "zz")), BooleanClause.Occur.SHOULD);

		int[] expDocNrs = new int[] {2, 3};
		Similarity oldSimilarity = Searcher.Similarity;
		try
		{
		  Searcher.Similarity = new DefaultSimilarityAnonymousInnerClassHelper(this);
		  QueriesTest(query, expDocNrs);
		}
		finally
		{
		  Searcher.Similarity = oldSimilarity;
		}
	  }

	  private class DefaultSimilarityAnonymousInnerClassHelper : DefaultSimilarity
	  {
		  private readonly TestBoolean2 OuterInstance;

		  public DefaultSimilarityAnonymousInnerClassHelper(TestBoolean2 outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override float Coord(int overlap, int maxOverlap)
		  {
			return overlap / ((float)maxOverlap - 1);
		  }
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRandomQueries() throws Exception
	  public virtual void TestRandomQueries()
	  {
		string[] vals = new string[] {"w1","w2","w3","w4","w5","xx","yy","zzz"};

		int tot = 0;

		BooleanQuery q1 = null;
		try
		{

		  // increase number of iterations for more complete testing
		  int num = atLeast(20);
		  for (int i = 0; i < num; i++)
		  {
			int level = random().Next(3);
			q1 = RandBoolQuery(new Random(random().nextLong()), random().nextBoolean(), level, Field, vals, null);

			// Can't sort by relevance since floating point numbers may not quite
			// match up.
			Sort sort = Sort.INDEXORDER;

			QueryUtils.check(random(), q1,Searcher); // baseline sim
			try
			{
			  // a little hackish, QueryUtils.check is too costly to do on bigSearcher in this loop.
			  Searcher.Similarity = BigSearcher.Similarity; // random sim
			  QueryUtils.check(random(), q1, Searcher);
			}
			finally
			{
			  Searcher.Similarity = new DefaultSimilarity(); // restore
			}

			TopFieldCollector collector = TopFieldCollector.create(sort, 1000, false, true, true, true);

			Searcher.search(q1, null, collector);
			ScoreDoc[] hits1 = collector.topDocs().scoreDocs;

			collector = TopFieldCollector.create(sort, 1000, false, true, true, false);

			Searcher.search(q1, null, collector);
			ScoreDoc[] hits2 = collector.topDocs().scoreDocs;
			tot += hits2.Length;
			CheckHits.checkEqual(q1, hits1, hits2);

			BooleanQuery q3 = new BooleanQuery();
			q3.add(q1, BooleanClause.Occur.SHOULD);
			q3.add(new PrefixQuery(new Term("field2", "b")), BooleanClause.Occur.SHOULD);
			TopDocs hits4 = BigSearcher.search(q3, 1);
			Assert.AreEqual(MulFactor * collector.totalHits + NUM_EXTRA_DOCS / 2, hits4.totalHits);
		  }

		}
		catch (Exception e)
		{
		  // For easier debugging
		  Console.WriteLine("failed query: " + q1);
		  throw e;
		}

		// System.out.println("Total hits:"+tot);
	  }


	  // used to set properties or change every BooleanQuery
	  // generated from randBoolQuery.
	  public interface Callback
	  {
		void PostCreate(BooleanQuery q);
	  }

	  // Random rnd is passed in so that the exact same random query may be created
	  // more than once.
	  public static BooleanQuery RandBoolQuery(Random rnd, bool allowMust, int level, string field, string[] vals, Callback cb)
	  {
		BooleanQuery current = new BooleanQuery(rnd.Next() < 0);
		for (int i = 0; i < rnd.Next(vals.Length) + 1; i++)
		{
		  int qType = 0; // term query
		  if (level > 0)
		  {
			qType = rnd.Next(10);
		  }
		  Query q;
		  if (qType < 3)
		  {
			q = new TermQuery(new Term(field, vals[rnd.Next(vals.Length)]));
		  }
		  else if (qType < 4)
		  {
			Term t1 = new Term(field, vals[rnd.Next(vals.Length)]);
			Term t2 = new Term(field, vals[rnd.Next(vals.Length)]);
			PhraseQuery pq = new PhraseQuery();
			pq.add(t1);
			pq.add(t2);
			pq.Slop = 10; // increase possibility of matching
			q = pq;
		  }
		  else if (qType < 7)
		  {
			q = new WildcardQuery(new Term(field, "w*"));
		  }
		  else
		  {
			q = RandBoolQuery(rnd, allowMust, level - 1, field, vals, cb);
		  }

		  int r = rnd.Next(10);
		  BooleanClause.Occur occur;
		  if (r < 2)
		  {
			occur = BooleanClause.Occur.MUST_NOT;
		  }
		  else if (r < 5)
		  {
			if (allowMust)
			{
			  occur = BooleanClause.Occur.MUST;
			}
			else
			{
			  occur = BooleanClause.Occur.SHOULD;
			}
		  }
		  else
		  {
			occur = BooleanClause.Occur.SHOULD;
		  }

		  current.add(q, occur);
		}
		if (cb != null)
		{
			cb.PostCreate(current);
		}
		return current;
	  }


	}

}