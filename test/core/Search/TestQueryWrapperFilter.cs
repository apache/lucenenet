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

	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Occur = Lucene.Net.Search.BooleanClause.Occur_e;
	using Directory = Lucene.Net.Store.Directory;
	using English = Lucene.Net.Util.English;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestQueryWrapperFilter : LuceneTestCase
	{

	  public virtual void TestBasic()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newTextField("field", "value", Field.Store.NO));
		writer.addDocument(doc);
		IndexReader reader = writer.Reader;
		writer.close();

		TermQuery termQuery = new TermQuery(new Term("field", "value"));

		// should not throw exception with primitive query
		QueryWrapperFilter qwf = new QueryWrapperFilter(termQuery);

		IndexSearcher searcher = newSearcher(reader);
		TopDocs hits = searcher.search(new MatchAllDocsQuery(), qwf, 10);
		Assert.AreEqual(1, hits.totalHits);
		hits = searcher.search(new MatchAllDocsQuery(), new CachingWrapperFilter(qwf), 10);
		Assert.AreEqual(1, hits.totalHits);

		// should not throw exception with complex primitive query
		BooleanQuery booleanQuery = new BooleanQuery();
		booleanQuery.add(termQuery, Occur.MUST);
		booleanQuery.add(new TermQuery(new Term("field", "missing")), Occur.MUST_NOT);
		qwf = new QueryWrapperFilter(termQuery);

		hits = searcher.search(new MatchAllDocsQuery(), qwf, 10);
		Assert.AreEqual(1, hits.totalHits);
		hits = searcher.search(new MatchAllDocsQuery(), new CachingWrapperFilter(qwf), 10);
		Assert.AreEqual(1, hits.totalHits);

		// should not throw exception with non primitive Query (doesn't implement
		// Query#createWeight)
		qwf = new QueryWrapperFilter(new FuzzyQuery(new Term("field", "valu")));

		hits = searcher.search(new MatchAllDocsQuery(), qwf, 10);
		Assert.AreEqual(1, hits.totalHits);
		hits = searcher.search(new MatchAllDocsQuery(), new CachingWrapperFilter(qwf), 10);
		Assert.AreEqual(1, hits.totalHits);

		// test a query with no hits
		termQuery = new TermQuery(new Term("field", "not_exist"));
		qwf = new QueryWrapperFilter(termQuery);
		hits = searcher.search(new MatchAllDocsQuery(), qwf, 10);
		Assert.AreEqual(0, hits.totalHits);
		hits = searcher.search(new MatchAllDocsQuery(), new CachingWrapperFilter(qwf), 10);
		Assert.AreEqual(0, hits.totalHits);
		reader.close();
		dir.close();
	  }

	  public virtual void TestRandom()
	  {
		Directory d = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), d);
		w.w.Config.MaxBufferedDocs = 17;
		int numDocs = atLeast(100);
		Set<string> aDocs = new HashSet<string>();
		for (int i = 0;i < numDocs;i++)
		{
		  Document doc = new Document();
		  string v;
		  if (random().Next(5) == 4)
		  {
			v = "a";
			aDocs.add("" + i);
		  }
		  else
		  {
			v = "b";
		  }
		  Field f = newStringField("field", v, Field.Store.NO);
		  doc.add(f);
		  doc.add(newStringField("id", "" + i, Field.Store.YES));
		  w.addDocument(doc);
		}

		int numDelDocs = atLeast(10);
		for (int i = 0;i < numDelDocs;i++)
		{
		  string delID = "" + random().Next(numDocs);
		  w.deleteDocuments(new Term("id", delID));
		  aDocs.remove(delID);
		}

		IndexReader r = w.Reader;
		w.close();
		TopDocs hits = newSearcher(r).search(new MatchAllDocsQuery(), new QueryWrapperFilter(new TermQuery(new Term("field", "a"))), numDocs);
		Assert.AreEqual(aDocs.size(), hits.totalHits);
		foreach (ScoreDoc sd in hits.scoreDocs)
		{
		  Assert.IsTrue(aDocs.contains(r.document(sd.doc).get("id")));
		}
		r.close();
		d.close();
	  }

	  public virtual void TestThousandDocuments()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		for (int i = 0; i < 1000; i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("field", English.intToEnglish(i), Field.Store.NO));
		  writer.addDocument(doc);
		}

		IndexReader reader = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(reader);

		for (int i = 0; i < 1000; i++)
		{
		  TermQuery termQuery = new TermQuery(new Term("field", English.intToEnglish(i)));
		  QueryWrapperFilter qwf = new QueryWrapperFilter(termQuery);
		  TopDocs td = searcher.search(new MatchAllDocsQuery(), qwf, 10);
		  Assert.AreEqual(1, td.totalHits);
		}

		reader.close();
		dir.close();
	  }
	}

}