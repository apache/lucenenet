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
	using StringField = Lucene.Net.Document.StringField;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using SerialMergeScheduler = Lucene.Net.Index.SerialMergeScheduler;
	using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using FixedBitSet = Lucene.Net.Util.FixedBitSet;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestCachingWrapperFilter : LuceneTestCase
	{
	  internal Directory Dir;
	  internal DirectoryReader Ir;
	  internal IndexSearcher @is;
	  internal RandomIndexWriter Iw;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
		Iw = new RandomIndexWriter(random(), Dir);
		Document doc = new Document();
		Field idField = new StringField("id", "", Field.Store.NO);
		doc.add(idField);
		// add 500 docs with id 0..499
		for (int i = 0; i < 500; i++)
		{
		  idField.StringValue = Convert.ToString(i);
		  Iw.addDocument(doc);
		}
		// delete 20 of them
		for (int i = 0; i < 20; i++)
		{
		  Iw.deleteDocuments(new Term("id", Convert.ToString(random().Next(Iw.maxDoc()))));
		}
		Ir = Iw.Reader;
		@is = newSearcher(Ir);
	  }

	  public override void TearDown()
	  {
		IOUtils.close(Iw, Ir, Dir);
		base.tearDown();
	  }

	  private void AssertFilterEquals(Filter f1, Filter f2)
	  {
		Query query = new MatchAllDocsQuery();
		TopDocs hits1 = @is.search(query, f1, Ir.maxDoc());
		TopDocs hits2 = @is.search(query, f2, Ir.maxDoc());
		Assert.AreEqual(hits1.totalHits, hits2.totalHits);
		CheckHits.checkEqual(query, hits1.scoreDocs, hits2.scoreDocs);
		// now do it again to confirm caching works
		TopDocs hits3 = @is.search(query, f1, Ir.maxDoc());
		TopDocs hits4 = @is.search(query, f2, Ir.maxDoc());
		Assert.AreEqual(hits3.totalHits, hits4.totalHits);
		CheckHits.checkEqual(query, hits3.scoreDocs, hits4.scoreDocs);
	  }

	  /// <summary>
	  /// test null iterator </summary>
	  public virtual void TestEmpty()
	  {
		Query query = new BooleanQuery();
		Filter expected = new QueryWrapperFilter(query);
		Filter actual = new CachingWrapperFilter(expected);
		AssertFilterEquals(expected, actual);
	  }

	  /// <summary>
	  /// test iterator returns NO_MORE_DOCS </summary>
	  public virtual void TestEmpty2()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term("id", "0")), BooleanClause.Occur_e.MUST);
		query.add(new TermQuery(new Term("id", "0")), BooleanClause.Occur_e.MUST_NOT);
		Filter expected = new QueryWrapperFilter(query);
		Filter actual = new CachingWrapperFilter(expected);
		AssertFilterEquals(expected, actual);
	  }

	  /// <summary>
	  /// test null docidset </summary>
	  public virtual void TestEmpty3()
	  {
		Filter expected = new PrefixFilter(new Term("bogusField", "bogusVal"));
		Filter actual = new CachingWrapperFilter(expected);
		AssertFilterEquals(expected, actual);
	  }

	  /// <summary>
	  /// test iterator returns single document </summary>
	  public virtual void TestSingle()
	  {
		for (int i = 0; i < 10; i++)
		{
		  int id = random().Next(Ir.maxDoc());
		  Query query = new TermQuery(new Term("id", Convert.ToString(id)));
		  Filter expected = new QueryWrapperFilter(query);
		  Filter actual = new CachingWrapperFilter(expected);
		  AssertFilterEquals(expected, actual);
		}
	  }

	  /// <summary>
	  /// test sparse filters (match single documents) </summary>
	  public virtual void TestSparse()
	  {
		for (int i = 0; i < 10; i++)
		{
		  int id_start = random().Next(Ir.maxDoc() - 1);
		  int id_end = id_start + 1;
		  Query query = TermRangeQuery.newStringRange("id", Convert.ToString(id_start), Convert.ToString(id_end), true, true);
		  Filter expected = new QueryWrapperFilter(query);
		  Filter actual = new CachingWrapperFilter(expected);
		  AssertFilterEquals(expected, actual);
		}
	  }

	  /// <summary>
	  /// test dense filters (match entire index) </summary>
	  public virtual void TestDense()
	  {
		Query query = new MatchAllDocsQuery();
		Filter expected = new QueryWrapperFilter(query);
		Filter actual = new CachingWrapperFilter(expected);
		AssertFilterEquals(expected, actual);
	  }

	  public virtual void TestCachingWorks()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		writer.close();

		IndexReader reader = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir));
		AtomicReaderContext context = (AtomicReaderContext) reader.Context;
		MockFilter filter = new MockFilter();
		CachingWrapperFilter cacher = new CachingWrapperFilter(filter);

		// first time, nested filter is called
		DocIdSet strongRef = cacher.getDocIdSet(context, context.reader().LiveDocs);
		Assert.IsTrue("first time", filter.WasCalled());

		// make sure no exception if cache is holding the wrong docIdSet
		cacher.getDocIdSet(context, context.reader().LiveDocs);

		// second time, nested filter should not be called
		filter.Clear();
		cacher.getDocIdSet(context, context.reader().LiveDocs);
		Assert.IsFalse("second time", filter.WasCalled());

		reader.close();
		dir.close();
	  }

	  public virtual void TestNullDocIdSet()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		writer.close();

		IndexReader reader = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir));
		AtomicReaderContext context = (AtomicReaderContext) reader.Context;

		Filter filter = new FilterAnonymousInnerClassHelper(this, context);
		CachingWrapperFilter cacher = new CachingWrapperFilter(filter);

		// the caching filter should return the empty set constant
		assertNull(cacher.getDocIdSet(context, context.reader().LiveDocs));

		reader.close();
		dir.close();
	  }

	  private class FilterAnonymousInnerClassHelper : Filter
	  {
		  private readonly TestCachingWrapperFilter OuterInstance;

		  private AtomicReaderContext Context;

		  public FilterAnonymousInnerClassHelper(TestCachingWrapperFilter outerInstance, AtomicReaderContext context)
		  {
			  this.OuterInstance = outerInstance;
			  this.Context = context;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			return null;
		  }
	  }

	  public virtual void TestNullDocIdSetIterator()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		writer.close();

		IndexReader reader = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir));
		AtomicReaderContext context = (AtomicReaderContext) reader.Context;

		Filter filter = new FilterAnonymousInnerClassHelper2(this, context);
		CachingWrapperFilter cacher = new CachingWrapperFilter(filter);

		// the caching filter should return the empty set constant
		assertNull(cacher.getDocIdSet(context, context.reader().LiveDocs));

		reader.close();
		dir.close();
	  }

	  private class FilterAnonymousInnerClassHelper2 : Filter
	  {
		  private readonly TestCachingWrapperFilter OuterInstance;

		  private AtomicReaderContext Context;

		  public FilterAnonymousInnerClassHelper2(TestCachingWrapperFilter outerInstance, AtomicReaderContext context)
		  {
			  this.OuterInstance = outerInstance;
			  this.Context = context;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			return new DocIdSetAnonymousInnerClassHelper(this);
		  }

		  private class DocIdSetAnonymousInnerClassHelper : DocIdSet
		  {
			  private readonly FilterAnonymousInnerClassHelper2 OuterInstance;

			  public DocIdSetAnonymousInnerClassHelper(FilterAnonymousInnerClassHelper2 outerInstance)
			  {
				  this.outerInstance = outerInstance;
			  }

			  public override DocIdSetIterator Iterator()
			  {
				return null;
			  }
		  }
	  }

	  private static void AssertDocIdSetCacheable(IndexReader reader, Filter filter, bool shouldCacheable)
	  {
		Assert.IsTrue(reader.Context is AtomicReaderContext);
		AtomicReaderContext context = (AtomicReaderContext) reader.Context;
		CachingWrapperFilter cacher = new CachingWrapperFilter(filter);
		DocIdSet originalSet = filter.getDocIdSet(context, context.reader().LiveDocs);
		DocIdSet cachedSet = cacher.getDocIdSet(context, context.reader().LiveDocs);
		if (originalSet == null)
		{
		  assertNull(cachedSet);
		}
		if (cachedSet == null)
		{
		  Assert.IsTrue(originalSet == null || originalSet.GetEnumerator() == null);
		}
		else
		{
		  Assert.IsTrue(cachedSet.Cacheable);
		  Assert.AreEqual(shouldCacheable, originalSet.Cacheable);
		  //System.out.println("Original: "+originalSet.getClass().getName()+" -- cached: "+cachedSet.getClass().getName());
		  if (originalSet.Cacheable)
		  {
			Assert.AreEqual("Cached DocIdSet must be of same class like uncached, if cacheable", originalSet.GetType(), cachedSet.GetType());
		  }
		  else
		  {
			Assert.IsTrue("Cached DocIdSet must be an FixedBitSet if the original one was not cacheable", cachedSet is FixedBitSet || cachedSet == null);
		  }
		}
	  }

	  public virtual void TestIsCacheAble()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		writer.addDocument(new Document());
		writer.close();

		IndexReader reader = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir));

		// not cacheable:
		AssertDocIdSetCacheable(reader, new QueryWrapperFilter(new TermQuery(new Term("test","value"))), false);
		// returns default empty docidset, always cacheable:
		AssertDocIdSetCacheable(reader, NumericRangeFilter.newIntRange("test", Convert.ToInt32(10000), Convert.ToInt32(-10000), true, true), true);
		// is cacheable:
		AssertDocIdSetCacheable(reader, FieldCacheRangeFilter.newIntRange("test", Convert.ToInt32(10), Convert.ToInt32(20), true, true), true);
		// a fixedbitset filter is always cacheable
		AssertDocIdSetCacheable(reader, new FilterAnonymousInnerClassHelper3(this), true);

		reader.close();
		dir.close();
	  }

	  private class FilterAnonymousInnerClassHelper3 : Filter
	  {
		  private readonly TestCachingWrapperFilter OuterInstance;

		  public FilterAnonymousInnerClassHelper3(TestCachingWrapperFilter outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			return new FixedBitSet(context.reader().maxDoc());
		  }
	  }

	  public virtual void TestEnforceDeletions()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergeScheduler(new SerialMergeScheduler()).setMergePolicy(newLogMergePolicy(10)));
				// asserts below requires no unexpected merges:

		// NOTE: cannot use writer.getReader because RIW (on
		// flipping a coin) may give us a newly opened reader,
		// but we use .reopen on this reader below and expect to
		// (must) get an NRT reader:
		DirectoryReader reader = DirectoryReader.open(writer.w, true);
		// same reason we don't wrap?
		IndexSearcher searcher = newSearcher(reader, false);

		// add a doc, refresh the reader, and check that it's there
		Document doc = new Document();
		doc.add(newStringField("id", "1", Field.Store.YES));
		writer.addDocument(doc);

		reader = RefreshReader(reader);
		searcher = newSearcher(reader, false);

		TopDocs docs = searcher.search(new MatchAllDocsQuery(), 1);
		Assert.AreEqual("Should find a hit...", 1, docs.totalHits);

		Filter startFilter = new QueryWrapperFilter(new TermQuery(new Term("id", "1")));

		CachingWrapperFilter filter = new CachingWrapperFilter(startFilter);

		docs = searcher.search(new MatchAllDocsQuery(), filter, 1);
		Assert.IsTrue(filter.sizeInBytes() > 0);

		Assert.AreEqual("[query + filter] Should find a hit...", 1, docs.totalHits);

		Query constantScore = new ConstantScoreQuery(filter);
		docs = searcher.search(constantScore, 1);
		Assert.AreEqual("[just filter] Should find a hit...", 1, docs.totalHits);

		// make sure we get a cache hit when we reopen reader
		// that had no change to deletions

		// fake delete (deletes nothing):
		writer.deleteDocuments(new Term("foo", "bar"));

		IndexReader oldReader = reader;
		reader = RefreshReader(reader);
		Assert.IsTrue(reader == oldReader);
		int missCount = filter.missCount;
		docs = searcher.search(constantScore, 1);
		Assert.AreEqual("[just filter] Should find a hit...", 1, docs.totalHits);

		// cache hit:
		Assert.AreEqual(missCount, filter.missCount);

		// now delete the doc, refresh the reader, and see that it's not there
		writer.deleteDocuments(new Term("id", "1"));

		// NOTE: important to hold ref here so GC doesn't clear
		// the cache entry!  Else the assert below may sometimes
		// fail:
		oldReader = reader;
		reader = RefreshReader(reader);

		searcher = newSearcher(reader, false);

		missCount = filter.missCount;
		docs = searcher.search(new MatchAllDocsQuery(), filter, 1);
		Assert.AreEqual("[query + filter] Should *not* find a hit...", 0, docs.totalHits);

		// cache hit
		Assert.AreEqual(missCount, filter.missCount);
		docs = searcher.search(constantScore, 1);
		Assert.AreEqual("[just filter] Should *not* find a hit...", 0, docs.totalHits);

		// apply deletes dynamically:
		filter = new CachingWrapperFilter(startFilter);
		writer.addDocument(doc);
		reader = RefreshReader(reader);
		searcher = newSearcher(reader, false);

		docs = searcher.search(new MatchAllDocsQuery(), filter, 1);
		Assert.AreEqual("[query + filter] Should find a hit...", 1, docs.totalHits);
		missCount = filter.missCount;
		Assert.IsTrue(missCount > 0);
		constantScore = new ConstantScoreQuery(filter);
		docs = searcher.search(constantScore, 1);
		Assert.AreEqual("[just filter] Should find a hit...", 1, docs.totalHits);
		Assert.AreEqual(missCount, filter.missCount);

		writer.addDocument(doc);

		// NOTE: important to hold ref here so GC doesn't clear
		// the cache entry!  Else the assert below may sometimes
		// fail:
		oldReader = reader;

		reader = RefreshReader(reader);
		searcher = newSearcher(reader, false);

		docs = searcher.search(new MatchAllDocsQuery(), filter, 1);
		Assert.AreEqual("[query + filter] Should find 2 hits...", 2, docs.totalHits);
		Assert.IsTrue(filter.missCount > missCount);
		missCount = filter.missCount;

		constantScore = new ConstantScoreQuery(filter);
		docs = searcher.search(constantScore, 1);
		Assert.AreEqual("[just filter] Should find a hit...", 2, docs.totalHits);
		Assert.AreEqual(missCount, filter.missCount);

		// now delete the doc, refresh the reader, and see that it's not there
		writer.deleteDocuments(new Term("id", "1"));

		reader = RefreshReader(reader);
		searcher = newSearcher(reader, false);

		docs = searcher.search(new MatchAllDocsQuery(), filter, 1);
		Assert.AreEqual("[query + filter] Should *not* find a hit...", 0, docs.totalHits);
		// CWF reused the same entry (it dynamically applied the deletes):
		Assert.AreEqual(missCount, filter.missCount);

		docs = searcher.search(constantScore, 1);
		Assert.AreEqual("[just filter] Should *not* find a hit...", 0, docs.totalHits);
		// CWF reused the same entry (it dynamically applied the deletes):
		Assert.AreEqual(missCount, filter.missCount);

		// NOTE: silliness to make sure JRE does not eliminate
		// our holding onto oldReader to prevent
		// CachingWrapperFilter's WeakHashMap from dropping the
		// entry:
		Assert.IsTrue(oldReader != null);

		reader.close();
		writer.close();
		dir.close();
	  }

	  private static DirectoryReader RefreshReader(DirectoryReader reader)
	  {
		DirectoryReader oldReader = reader;
		reader = DirectoryReader.openIfChanged(reader);
		if (reader != null)
		{
		  oldReader.close();
		  return reader;
		}
		else
		{
		  return oldReader;
		}
	  }

	}

}