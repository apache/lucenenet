using System;
using System.Collections;

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
	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using DocsEnum = Lucene.Net.Index.DocsEnum;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Occur = Lucene.Net.Search.BooleanClause.Occur_e;
	using FilterStrategy = Lucene.Net.Search.FilteredQuery.FilterStrategy;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using DocIdBitSet = Lucene.Net.Util.DocIdBitSet;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// FilteredQuery JUnit tests.
	/// 
	/// <p>Created: Apr 21, 2004 1:21:46 PM
	/// 
	/// 
	/// @since   1.4
	/// </summary>
	public class TestFilteredQuery : LuceneTestCase
	{

	  private IndexSearcher Searcher;
	  private IndexReader Reader;
	  private Directory Directory;
	  private Query Query;
	  private Filter Filter;

	  public override void SetUp()
	  {
		base.setUp();
		Directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));

		Document doc = new Document();
		doc.add(newTextField("field", "one two three four five", Field.Store.YES));
		doc.add(newTextField("sorter", "b", Field.Store.YES));
		writer.addDocument(doc);

		doc = new Document();
		doc.add(newTextField("field", "one two three four", Field.Store.YES));
		doc.add(newTextField("sorter", "d", Field.Store.YES));
		writer.addDocument(doc);

		doc = new Document();
		doc.add(newTextField("field", "one two three y", Field.Store.YES));
		doc.add(newTextField("sorter", "a", Field.Store.YES));
		writer.addDocument(doc);

		doc = new Document();
		doc.add(newTextField("field", "one two x", Field.Store.YES));
		doc.add(newTextField("sorter", "c", Field.Store.YES));
		writer.addDocument(doc);

		// tests here require single segment (eg try seed
		// 8239472272678419952L), because SingleDocTestFilter(x)
		// blindly accepts that docID in any sub-segment
		writer.forceMerge(1);

		Reader = writer.Reader;
		writer.close();

		Searcher = newSearcher(Reader);

		Query = new TermQuery(new Term("field", "three"));
		Filter = NewStaticFilterB();
	  }

	  // must be static for serialization tests
	  private static Filter NewStaticFilterB()
	  {
		return new FilterAnonymousInnerClassHelper();
	  }

	  private class FilterAnonymousInnerClassHelper : Filter
	  {
		  public FilterAnonymousInnerClassHelper()
		  {
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			if (acceptDocs == null)
			{
				acceptDocs = new Bits.MatchAllBits(5);
			}
			BitArray bitset = new BitArray(5);
			if (acceptDocs.get(1))
			{
				bitset.Set(1, true);
			}
			if (acceptDocs.get(3))
			{
				bitset.Set(3, true);
			}
			return new DocIdBitSet(bitset);
		  }
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Directory.close();
		base.tearDown();
	  }

	  public virtual void TestFilteredQuery()
	  {
		// force the filter to be executed as bits
		TFilteredQuery(true);
		// force the filter to be executed as iterator
		TFilteredQuery(false);
	  }

	  private void TFilteredQuery(bool useRandomAccess)
	  {
		Query filteredquery = new FilteredQuery(Query, Filter, RandomFilterStrategy(random(), useRandomAccess));
		ScoreDoc[] hits = Searcher.search(filteredquery, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		Assert.AreEqual(1, hits[0].doc);
		QueryUtils.check(random(), filteredquery,Searcher);

		hits = Searcher.search(filteredquery, null, 1000, new Sort(new SortField("sorter", SortField.Type.STRING))).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		Assert.AreEqual(1, hits[0].doc);

		filteredquery = new FilteredQuery(new TermQuery(new Term("field", "one")), Filter, RandomFilterStrategy(random(), useRandomAccess));
		hits = Searcher.search(filteredquery, null, 1000).scoreDocs;
		Assert.AreEqual(2, hits.Length);
		QueryUtils.check(random(), filteredquery,Searcher);

		filteredquery = new FilteredQuery(new MatchAllDocsQuery(), Filter, RandomFilterStrategy(random(), useRandomAccess));
		hits = Searcher.search(filteredquery, null, 1000).scoreDocs;
		Assert.AreEqual(2, hits.Length);
		QueryUtils.check(random(), filteredquery,Searcher);

		filteredquery = new FilteredQuery(new TermQuery(new Term("field", "x")), Filter, RandomFilterStrategy(random(), useRandomAccess));
		hits = Searcher.search(filteredquery, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		Assert.AreEqual(3, hits[0].doc);
		QueryUtils.check(random(), filteredquery,Searcher);

		filteredquery = new FilteredQuery(new TermQuery(new Term("field", "y")), Filter, RandomFilterStrategy(random(), useRandomAccess));
		hits = Searcher.search(filteredquery, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);
		QueryUtils.check(random(), filteredquery,Searcher);

		// test boost
		Filter f = NewStaticFilterA();

		float boost = 2.5f;
		BooleanQuery bq1 = new BooleanQuery();
		TermQuery tq = new TermQuery(new Term("field", "one"));
		tq.Boost = boost;
		bq1.add(tq, Occur.MUST);
		bq1.add(new TermQuery(new Term("field", "five")), Occur.MUST);

		BooleanQuery bq2 = new BooleanQuery();
		tq = new TermQuery(new Term("field", "one"));
		filteredquery = new FilteredQuery(tq, f, RandomFilterStrategy(random(), useRandomAccess));
		filteredquery.Boost = boost;
		bq2.add(filteredquery, Occur.MUST);
		bq2.add(new TermQuery(new Term("field", "five")), Occur.MUST);
		AssertScoreEquals(bq1, bq2);

		Assert.AreEqual(boost, filteredquery.Boost, 0);
		Assert.AreEqual(1.0f, tq.Boost, 0); // the boost value of the underlying query shouldn't have changed
	  }

	  // must be static for serialization tests 
	  private static Filter NewStaticFilterA()
	  {
		return new FilterAnonymousInnerClassHelper2();
	  }

	  private class FilterAnonymousInnerClassHelper2 : Filter
	  {
		  public FilterAnonymousInnerClassHelper2()
		  {
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			assertNull("acceptDocs should be null, as we have an index without deletions", acceptDocs);
			BitArray bitset = new BitArray(5);
			bitset.Set(0, 5);
			return new DocIdBitSet(bitset);
		  }
	  }

	  /// <summary>
	  /// Tests whether the scores of the two queries are the same.
	  /// </summary>
	  public virtual void AssertScoreEquals(Query q1, Query q2)
	  {
		ScoreDoc[] hits1 = Searcher.search(q1, null, 1000).scoreDocs;
		ScoreDoc[] hits2 = Searcher.search(q2, null, 1000).scoreDocs;

		Assert.AreEqual(hits1.Length, hits2.Length);

		for (int i = 0; i < hits1.Length; i++)
		{
		  Assert.AreEqual(hits1[i].score, hits2[i].score, 0.000001f);
		}
	  }

	  /// <summary>
	  /// this tests FilteredQuery's rewrite correctness
	  /// </summary>
	  public virtual void TestRangeQuery()
	  {
		// force the filter to be executed as bits
		TRangeQuery(true);
		TRangeQuery(false);
	  }

	  private void TRangeQuery(bool useRandomAccess)
	  {
		TermRangeQuery rq = TermRangeQuery.newStringRange("sorter", "b", "d", true, true);

		Query filteredquery = new FilteredQuery(rq, Filter, RandomFilterStrategy(random(), useRandomAccess));
		ScoreDoc[] hits = Searcher.search(filteredquery, null, 1000).scoreDocs;
		Assert.AreEqual(2, hits.Length);
		QueryUtils.check(random(), filteredquery,Searcher);
	  }

	  public virtual void TestBooleanMUST()
	  {
		// force the filter to be executed as bits
		TBooleanMUST(true);
		// force the filter to be executed as iterator
		TBooleanMUST(false);
	  }

	  private void TBooleanMUST(bool useRandomAccess)
	  {
		BooleanQuery bq = new BooleanQuery();
		Query query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter(0), RandomFilterStrategy(random(), useRandomAccess));
		bq.add(query, BooleanClause.Occur_e.MUST);
		query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter(1), RandomFilterStrategy(random(), useRandomAccess));
		bq.add(query, BooleanClause.Occur_e.MUST);
		ScoreDoc[] hits = Searcher.search(bq, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);
		QueryUtils.check(random(), query,Searcher);
	  }

	  public virtual void TestBooleanSHOULD()
	  {
		// force the filter to be executed as bits
		TBooleanSHOULD(true);
		// force the filter to be executed as iterator
		TBooleanSHOULD(false);
	  }

	  private void TBooleanSHOULD(bool useRandomAccess)
	  {
		BooleanQuery bq = new BooleanQuery();
		Query query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter(0), RandomFilterStrategy(random(), useRandomAccess));
		bq.add(query, BooleanClause.Occur_e.SHOULD);
		query = new FilteredQuery(new TermQuery(new Term("field", "one")), new SingleDocTestFilter(1), RandomFilterStrategy(random(), useRandomAccess));
		bq.add(query, BooleanClause.Occur_e.SHOULD);
		ScoreDoc[] hits = Searcher.search(bq, null, 1000).scoreDocs;
		Assert.AreEqual(2, hits.Length);
		QueryUtils.check(random(), query,Searcher);
	  }

	  // Make sure BooleanQuery, which does out-of-order
	  // scoring, inside FilteredQuery, works
	  public virtual void TestBoolean2()
	  {
		// force the filter to be executed as bits
		TBoolean2(true);
		// force the filter to be executed as iterator
		TBoolean2(false);
	  }

	  private void TBoolean2(bool useRandomAccess)
	  {
		BooleanQuery bq = new BooleanQuery();
		Query query = new FilteredQuery(bq, new SingleDocTestFilter(0), RandomFilterStrategy(random(), useRandomAccess));
		bq.add(new TermQuery(new Term("field", "one")), BooleanClause.Occur_e.SHOULD);
		bq.add(new TermQuery(new Term("field", "two")), BooleanClause.Occur_e.SHOULD);
		ScoreDoc[] hits = Searcher.search(query, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		QueryUtils.check(random(), query, Searcher);
	  }

	  public virtual void TestChainedFilters()
	  {
		// force the filter to be executed as bits
		TChainedFilters(true);
		// force the filter to be executed as iterator
		TChainedFilters(false);
	  }

	  private void TChainedFilters(bool useRandomAccess)
	  {
		Query query = new FilteredQuery(new FilteredQuery(new MatchAllDocsQuery(), new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("field", "three")))), RandomFilterStrategy(random(), useRandomAccess)), new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("field", "four")))), RandomFilterStrategy(random(), useRandomAccess));
		ScoreDoc[] hits = Searcher.search(query, 10).scoreDocs;
		Assert.AreEqual(2, hits.Length);
		QueryUtils.check(random(), query, Searcher);

		// one more:
		query = new FilteredQuery(query, new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("field", "five")))), RandomFilterStrategy(random(), useRandomAccess));
		hits = Searcher.search(query, 10).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		QueryUtils.check(random(), query, Searcher);
	  }

	  public virtual void TestEqualsHashcode()
	  {
		// some tests before, if the used queries and filters work:
		Assert.AreEqual(new PrefixFilter(new Term("field", "o")), new PrefixFilter(new Term("field", "o")));
		Assert.IsFalse((new PrefixFilter(new Term("field", "a"))).Equals(new PrefixFilter(new Term("field", "o"))));
		QueryUtils.checkHashEquals(new TermQuery(new Term("field", "one")));
		QueryUtils.checkUnequal(new TermQuery(new Term("field", "one")), new TermQuery(new Term("field", "two"))
	   );
		// now test FilteredQuery equals/hashcode:
		QueryUtils.checkHashEquals(new FilteredQuery(new TermQuery(new Term("field", "one")), new PrefixFilter(new Term("field", "o"))));
		QueryUtils.checkUnequal(new FilteredQuery(new TermQuery(new Term("field", "one")), new PrefixFilter(new Term("field", "o"))), new FilteredQuery(new TermQuery(new Term("field", "two")), new PrefixFilter(new Term("field", "o")))
	   );
		QueryUtils.checkUnequal(new FilteredQuery(new TermQuery(new Term("field", "one")), new PrefixFilter(new Term("field", "a"))), new FilteredQuery(new TermQuery(new Term("field", "one")), new PrefixFilter(new Term("field", "o")))
	   );
	  }

	  public virtual void TestInvalidArguments()
	  {
		try
		{
		  new FilteredQuery(null, null);
		  Assert.Fail("Should throw IllegalArgumentException");
		}
		catch (System.ArgumentException iae)
		{
		  // pass
		}
		try
		{
		  new FilteredQuery(new TermQuery(new Term("field", "one")), null);
		  Assert.Fail("Should throw IllegalArgumentException");
		}
		catch (System.ArgumentException iae)
		{
		  // pass
		}
		try
		{
		  new FilteredQuery(null, new PrefixFilter(new Term("field", "o")));
		  Assert.Fail("Should throw IllegalArgumentException");
		}
		catch (System.ArgumentException iae)
		{
		  // pass
		}
	  }

	  private FilterStrategy RandomFilterStrategy()
	  {
		return RandomFilterStrategy(random(), true);
	  }

	  private void AssertRewrite(FilteredQuery fq, Type clazz)
	  {
		// assign crazy boost to FQ
		float boost = random().nextFloat() * 100.0f;
		fq.Boost = boost;


		// assign crazy boost to inner
		float innerBoost = random().nextFloat() * 100.0f;
		fq.Query.Boost = innerBoost;

		// check the class and boosts of rewritten query
		Query rewritten = Searcher.rewrite(fq);
		Assert.IsTrue("is not instance of " + clazz.Name, clazz.IsInstanceOfType(rewritten));
		if (rewritten is FilteredQuery)
		{
		  Assert.AreEqual(boost, rewritten.Boost, 1.E-5f);
		  Assert.AreEqual(innerBoost, ((FilteredQuery) rewritten).Query.Boost, 1.E-5f);
		  Assert.AreEqual(fq.FilterStrategy, ((FilteredQuery) rewritten).FilterStrategy);
		}
		else
		{
		  Assert.AreEqual(boost * innerBoost, rewritten.Boost, 1.E-5f);
		}

		// check that the original query was not modified
		Assert.AreEqual(boost, fq.Boost, 1.E-5f);
		Assert.AreEqual(innerBoost, fq.Query.Boost, 1.E-5f);
	  }

	  public virtual void TestRewrite()
	  {
		AssertRewrite(new FilteredQuery(new TermQuery(new Term("field", "one")), new PrefixFilter(new Term("field", "o")), RandomFilterStrategy()), typeof(FilteredQuery));
		AssertRewrite(new FilteredQuery(new PrefixQuery(new Term("field", "one")), new PrefixFilter(new Term("field", "o")), RandomFilterStrategy()), typeof(FilteredQuery));
	  }

	  public virtual void TestGetFilterStrategy()
	  {
		FilterStrategy randomFilterStrategy = RandomFilterStrategy();
		FilteredQuery filteredQuery = new FilteredQuery(new TermQuery(new Term("field", "one")), new PrefixFilter(new Term("field", "o")), randomFilterStrategy);
		assertSame(randomFilterStrategy, filteredQuery.FilterStrategy);
	  }

	  private static FilteredQuery.FilterStrategy RandomFilterStrategy(Random random, bool useRandomAccess)
	  {
		if (useRandomAccess)
		{
		  return new RandomAccessFilterStrategyAnonymousInnerClassHelper();
		}
		return TestUtil.randomFilterStrategy(random);
	  }

	  private class RandomAccessFilterStrategyAnonymousInnerClassHelper : FilteredQuery.RandomAccessFilterStrategy
	  {
		  public RandomAccessFilterStrategyAnonymousInnerClassHelper()
		  {
		  }

		  protected internal override bool UseRandomAccess(Bits bits, int firstFilterDoc)
		  {
			return true;
		  }
	  }

	  /*
	   * Test if the QueryFirst strategy calls the bits only if the document has
	   * been matched by the query and not otherwise
	   */
	  public virtual void TestQueryFirstFilterStrategy()
	  {
		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		int numDocs = atLeast(50);
		int totalDocsWithZero = 0;
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  int num = random().Next(5);
		  if (num == 0)
		  {
			totalDocsWithZero++;
		  }
		  doc.add(newTextField("field", "" + num, Field.Store.YES));
		  writer.addDocument(doc);
		}
		IndexReader reader = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(reader);
		Query query = new FilteredQuery(new TermQuery(new Term("field", "0")), new FilterAnonymousInnerClassHelper3(this, reader), FilteredQuery.QUERY_FIRST_FILTER_STRATEGY);

		TopDocs search = searcher.search(query, 10);
		Assert.AreEqual(totalDocsWithZero, search.totalHits);
		IOUtils.Close(reader, writer, directory);

	  }

	  private class FilterAnonymousInnerClassHelper3 : Filter
	  {
		  private readonly TestFilteredQuery OuterInstance;

		  private IndexReader Reader;

		  public FilterAnonymousInnerClassHelper3(TestFilteredQuery outerInstance, IndexReader reader)
		  {
			  this.OuterInstance = outerInstance;
			  this.Reader = reader;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			bool nullBitset = random().Next(10) == 5;
			AtomicReader reader = context.reader();
			DocsEnum termDocsEnum = reader.termDocsEnum(new Term("field", "0"));
			if (termDocsEnum == null)
			{
			  return null; // no docs -- return null
			}
			BitArray bitSet = new BitArray(reader.maxDoc());
			int d;
			while ((d = termDocsEnum.nextDoc()) != DocsEnum.NO_MORE_DOCS)
			{
			  bitSet.Set(d, true);
			}
			return new DocIdSetAnonymousInnerClassHelper(this, nullBitset, reader, bitSet);
		  }

		  private class DocIdSetAnonymousInnerClassHelper : DocIdSet
		  {
			  private readonly FilterAnonymousInnerClassHelper3 OuterInstance;

			  private bool NullBitset;
			  private AtomicReader Reader;
			  private BitArray BitSet;

			  public DocIdSetAnonymousInnerClassHelper(FilterAnonymousInnerClassHelper3 outerInstance, bool nullBitset, AtomicReader reader, BitArray bitSet)
			  {
				  this.outerInstance = outerInstance;
				  this.NullBitset = nullBitset;
				  this.Reader = reader;
				  this.BitSet = bitSet;
			  }


			  public override Bits Bits()
			  {
				if (NullBitset)
				{
				  return null;
				}
				return new BitsAnonymousInnerClassHelper(this);
			  }

			  private class BitsAnonymousInnerClassHelper : Bits
			  {
				  private readonly DocIdSetAnonymousInnerClassHelper OuterInstance;

				  public BitsAnonymousInnerClassHelper(DocIdSetAnonymousInnerClassHelper outerInstance)
				  {
					  this.outerInstance = outerInstance;
				  }


				  public override bool Get(int index)
				  {
					Assert.IsTrue("filter was called for a non-matching doc", OuterInstance.BitSet.Get(index));
					return OuterInstance.BitSet.Get(index);
				  }

				  public override int Length()
				  {
					return OuterInstance.BitSet.length();
				  }

			  }

			  public override DocIdSetIterator Iterator()
			  {
				Assert.IsTrue("iterator should not be called if bitset is present", NullBitset);
				return Reader.termDocsEnum(new Term("field", "0"));
			  }

		  }
	  }

	  /*
	   * Test if the leapfrog strategy works correctly in terms
	   * of advancing / next the right thing first
	   */
	  public virtual void TestLeapFrogStrategy()
	  {
		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		int numDocs = atLeast(50);
		int totalDocsWithZero = 0;
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  int num = random().Next(10);
		  if (num == 0)
		  {
			totalDocsWithZero++;
		  }
		  doc.add(newTextField("field", "" + num, Field.Store.YES));
		  writer.addDocument(doc);
		}
		IndexReader reader = writer.Reader;
		writer.close();
		bool queryFirst = random().nextBoolean();
		IndexSearcher searcher = newSearcher(reader);
		Query query = new FilteredQuery(new TermQuery(new Term("field", "0")), new FilterAnonymousInnerClassHelper4(this, queryFirst), queryFirst ? FilteredQuery.LEAP_FROG_QUERY_FIRST_STRATEGY : random()
			  .nextBoolean() ? FilteredQuery.RANDOM_ACCESS_FILTER_STRATEGY : FilteredQuery.LEAP_FROG_FILTER_FIRST_STRATEGY); // if filterFirst, we can use random here since bits are null

		TopDocs search = searcher.search(query, 10);
		Assert.AreEqual(totalDocsWithZero, search.totalHits);
		IOUtils.Close(reader, writer, directory);

	  }

	  private class FilterAnonymousInnerClassHelper4 : Filter
	  {
		  private readonly TestFilteredQuery OuterInstance;

		  private bool QueryFirst;

		  public FilterAnonymousInnerClassHelper4(TestFilteredQuery outerInstance, bool queryFirst)
		  {
			  this.OuterInstance = outerInstance;
			  this.QueryFirst = queryFirst;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			return new DocIdSetAnonymousInnerClassHelper2(this, context);
		  }

		  private class DocIdSetAnonymousInnerClassHelper2 : DocIdSet
		  {
			  private readonly FilterAnonymousInnerClassHelper4 OuterInstance;

			  private AtomicReaderContext Context;

			  public DocIdSetAnonymousInnerClassHelper2(FilterAnonymousInnerClassHelper4 outerInstance, AtomicReaderContext context)
			  {
				  this.outerInstance = outerInstance;
				  this.Context = context;
			  }


			  public override Bits Bits()
			  {
				 return null;
			  }
			  public override DocIdSetIterator Iterator()
			  {
				DocsEnum termDocsEnum = Context.reader().termDocsEnum(new Term("field", "0"));
				if (termDocsEnum == null)
				{
				  return null;
				}
				return new DocIdSetIteratorAnonymousInnerClassHelper(this, termDocsEnum);
			  }

			  private class DocIdSetIteratorAnonymousInnerClassHelper : DocIdSetIterator
			  {
				  private readonly DocIdSetAnonymousInnerClassHelper2 OuterInstance;

				  private DocsEnum TermDocsEnum;

				  public DocIdSetIteratorAnonymousInnerClassHelper(DocIdSetAnonymousInnerClassHelper2 outerInstance, DocsEnum termDocsEnum)
				  {
					  this.outerInstance = outerInstance;
					  this.TermDocsEnum = termDocsEnum;
				  }

				  internal bool nextCalled;
				  internal bool advanceCalled;
				  public override int NextDoc()
				  {
					Assert.IsTrue("queryFirst: " + OuterInstance.OuterInstance.QueryFirst + " advanced: " + advanceCalled + " next: " + nextCalled, nextCalled || advanceCalled ^ !OuterInstance.OuterInstance.QueryFirst);
					nextCalled = true;
					return TermDocsEnum.nextDoc();
				  }

				  public override int DocID()
				  {
					return TermDocsEnum.docID();
				  }

				  public override int Advance(int target)
				  {
					Assert.IsTrue("queryFirst: " + OuterInstance.OuterInstance.QueryFirst + " advanced: " + advanceCalled + " next: " + nextCalled, advanceCalled || nextCalled ^ OuterInstance.OuterInstance.QueryFirst);
					advanceCalled = true;
					return TermDocsEnum.advance(target);
				  }

				  public override long Cost()
				  {
					return TermDocsEnum.cost();
				  }
			  }

		  }
	  }
	}





}