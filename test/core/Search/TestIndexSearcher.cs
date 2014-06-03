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


	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using NamedThreadFactory = Lucene.Net.Util.NamedThreadFactory;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using Test = org.junit.Test;

	public class TestIndexSearcher : LuceneTestCase
	{
	  internal Directory Dir;
	  internal IndexReader Reader;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), Dir);
		for (int i = 0; i < 100; i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("field", Convert.ToString(i), Field.Store.NO));
		  doc.add(newStringField("field2", Convert.ToString(i % 2 == 0), Field.Store.NO));
		  iw.addDocument(doc);
		}
		Reader = iw.Reader;
		iw.close();
	  }

	  public override void TearDown()
	  {
		base.tearDown();
		Reader.close();
		Dir.close();
	  }

	  // should not throw exception
	  public virtual void TestHugeN()
	  {
		ExecutorService service = new ThreadPoolExecutor(4, 4, 0L, TimeUnit.MILLISECONDS, new LinkedBlockingQueue<Runnable>(), new NamedThreadFactory("TestIndexSearcher"));

		IndexSearcher[] searchers = new IndexSearcher[] {new IndexSearcher(Reader), new IndexSearcher(Reader, service)};
		Query[] queries = new Query[] {new MatchAllDocsQuery(), new TermQuery(new Term("field", "1"))};
		Sort[] sorts = new Sort[] {null, new Sort(new SortField("field2", SortField.Type.STRING))};
		Filter[] filters = new Filter[] {null, new QueryWrapperFilter(new TermQuery(new Term("field2", "true")))};
		ScoreDoc[] afters = new ScoreDoc[] {null, new FieldDoc(0, 0f, new object[] {new BytesRef("boo!")})};

		foreach (IndexSearcher searcher in searchers)
		{
		  foreach (ScoreDoc after in afters)
		  {
			foreach (Query query in queries)
			{
			  foreach (Sort sort in sorts)
			  {
				foreach (Filter filter in filters)
				{
				  searcher.search(query, int.MaxValue);
				  searcher.searchAfter(after, query, int.MaxValue);
				  searcher.search(query, filter, int.MaxValue);
				  searcher.searchAfter(after, query, filter, int.MaxValue);
				  if (sort != null)
				  {
					searcher.search(query, int.MaxValue, sort);
					searcher.search(query, filter, int.MaxValue, sort);
					searcher.search(query, filter, int.MaxValue, sort, true, true);
					searcher.search(query, filter, int.MaxValue, sort, true, false);
					searcher.search(query, filter, int.MaxValue, sort, false, true);
					searcher.search(query, filter, int.MaxValue, sort, false, false);
					searcher.searchAfter(after, query, filter, int.MaxValue, sort);
					searcher.searchAfter(after, query, filter, int.MaxValue, sort, true, true);
					searcher.searchAfter(after, query, filter, int.MaxValue, sort, true, false);
					searcher.searchAfter(after, query, filter, int.MaxValue, sort, false, true);
					searcher.searchAfter(after, query, filter, int.MaxValue, sort, false, false);
				  }
				}
			  }
			}
		  }
		}

		TestUtil.shutdownExecutorService(service);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSearchAfterPassedMaxDoc() throws Exception
	  public virtual void TestSearchAfterPassedMaxDoc()
	  {
		// LUCENE-5128: ensure we get a meaningful message if searchAfter exceeds maxDoc
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);
		w.addDocument(new Document());
		IndexReader r = w.Reader;
		w.close();

		IndexSearcher s = new IndexSearcher(r);
		try
		{
		  s.searchAfter(new ScoreDoc(r.maxDoc(), 0.54f), new MatchAllDocsQuery(), 10);
		  Assert.Fail("should have hit IllegalArgumentException when searchAfter exceeds maxDoc");
		}
		catch (System.ArgumentException e)
		{
		  // ok
		}
		finally
		{
		  IOUtils.Close(r, dir);
		}
	  }

	}

}