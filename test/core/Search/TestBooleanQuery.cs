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


	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using TextField = Lucene.Net.Document.TextField;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using MultiReader = Lucene.Net.Index.MultiReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
	using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using NamedThreadFactory = Lucene.Net.Util.NamedThreadFactory;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestBooleanQuery : LuceneTestCase
	{

	  public virtual void TestEquality()
	  {
		BooleanQuery bq1 = new BooleanQuery();
		bq1.add(new TermQuery(new Term("field", "value1")), BooleanClause.Occur_e.SHOULD);
		bq1.add(new TermQuery(new Term("field", "value2")), BooleanClause.Occur_e.SHOULD);
		BooleanQuery nested1 = new BooleanQuery();
		nested1.add(new TermQuery(new Term("field", "nestedvalue1")), BooleanClause.Occur_e.SHOULD);
		nested1.add(new TermQuery(new Term("field", "nestedvalue2")), BooleanClause.Occur_e.SHOULD);
		bq1.add(nested1, BooleanClause.Occur_e.SHOULD);

		BooleanQuery bq2 = new BooleanQuery();
		bq2.add(new TermQuery(new Term("field", "value1")), BooleanClause.Occur_e.SHOULD);
		bq2.add(new TermQuery(new Term("field", "value2")), BooleanClause.Occur_e.SHOULD);
		BooleanQuery nested2 = new BooleanQuery();
		nested2.add(new TermQuery(new Term("field", "nestedvalue1")), BooleanClause.Occur_e.SHOULD);
		nested2.add(new TermQuery(new Term("field", "nestedvalue2")), BooleanClause.Occur_e.SHOULD);
		bq2.add(nested2, BooleanClause.Occur_e.SHOULD);

		Assert.AreEqual(bq1, bq2);
	  }

	  public virtual void TestException()
	  {
		try
		{
		  BooleanQuery.MaxClauseCount = 0;
		  Assert.Fail();
		}
		catch (System.ArgumentException e)
		{
		  // okay
		}
	  }

	  // LUCENE-1630
	  public virtual void TestNullOrSubScorer()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newTextField("field", "a b c d", Field.Store.NO));
		w.addDocument(doc);

		IndexReader r = w.Reader;
		IndexSearcher s = newSearcher(r);
		// this test relies upon coord being the default implementation,
		// otherwise scores are different!
		s.Similarity = new DefaultSimilarity();

		BooleanQuery q = new BooleanQuery();
		q.add(new TermQuery(new Term("field", "a")), BooleanClause.Occur_e.SHOULD);

		// LUCENE-2617: make sure that a term not in the index still contributes to the score via coord factor
		float score = s.search(q, 10).MaxScore;
		Query subQuery = new TermQuery(new Term("field", "not_in_index"));
		subQuery.Boost = 0;
		q.add(subQuery, BooleanClause.Occur_e.SHOULD);
		float score2 = s.search(q, 10).MaxScore;
		Assert.AreEqual(score * .5F, score2, 1e-6);

		// LUCENE-2617: make sure that a clause not in the index still contributes to the score via coord factor
		BooleanQuery qq = q.clone();
		PhraseQuery phrase = new PhraseQuery();
		phrase.add(new Term("field", "not_in_index"));
		phrase.add(new Term("field", "another_not_in_index"));
		phrase.Boost = 0;
		qq.add(phrase, BooleanClause.Occur_e.SHOULD);
		score2 = s.search(qq, 10).MaxScore;
		Assert.AreEqual(score * (1 / 3F), score2, 1e-6);

		// now test BooleanScorer2
		subQuery = new TermQuery(new Term("field", "b"));
		subQuery.Boost = 0;
		q.add(subQuery, BooleanClause.Occur_e.MUST);
		score2 = s.search(q, 10).MaxScore;
		Assert.AreEqual(score * (2 / 3F), score2, 1e-6);

		// PhraseQuery w/ no terms added returns a null scorer
		PhraseQuery pq = new PhraseQuery();
		q.add(pq, BooleanClause.Occur_e.SHOULD);
		Assert.AreEqual(1, s.search(q, 10).totalHits);

		// A required clause which returns null scorer should return null scorer to
		// IndexSearcher.
		q = new BooleanQuery();
		pq = new PhraseQuery();
		q.add(new TermQuery(new Term("field", "a")), BooleanClause.Occur_e.SHOULD);
		q.add(pq, BooleanClause.Occur_e.MUST);
		Assert.AreEqual(0, s.search(q, 10).totalHits);

		DisjunctionMaxQuery dmq = new DisjunctionMaxQuery(1.0f);
		dmq.add(new TermQuery(new Term("field", "a")));
		dmq.add(pq);
		Assert.AreEqual(1, s.search(dmq, 10).totalHits);

		r.close();
		w.close();
		dir.close();
	  }

	  public virtual void TestDeMorgan()
	  {
		Directory dir1 = newDirectory();
		RandomIndexWriter iw1 = new RandomIndexWriter(random(), dir1);
		Document doc1 = new Document();
		doc1.add(newTextField("field", "foo bar", Field.Store.NO));
		iw1.addDocument(doc1);
		IndexReader reader1 = iw1.Reader;
		iw1.close();

		Directory dir2 = newDirectory();
		RandomIndexWriter iw2 = new RandomIndexWriter(random(), dir2);
		Document doc2 = new Document();
		doc2.add(newTextField("field", "foo baz", Field.Store.NO));
		iw2.addDocument(doc2);
		IndexReader reader2 = iw2.Reader;
		iw2.close();

		BooleanQuery query = new BooleanQuery(); // Query: +foo -ba*
		query.add(new TermQuery(new Term("field", "foo")), BooleanClause.Occur_e.MUST);
		WildcardQuery wildcardQuery = new WildcardQuery(new Term("field", "ba*"));
		wildcardQuery.RewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
		query.add(wildcardQuery, BooleanClause.Occur_e.MUST_NOT);

		MultiReader multireader = new MultiReader(reader1, reader2);
		IndexSearcher searcher = newSearcher(multireader);
		Assert.AreEqual(0, searcher.search(query, 10).totalHits);

		ExecutorService es = Executors.newCachedThreadPool(new NamedThreadFactory("NRT search threads"));
		searcher = new IndexSearcher(multireader, es);
		if (VERBOSE)
		{
		  Console.WriteLine("rewritten form: " + searcher.rewrite(query));
		}
		Assert.AreEqual(0, searcher.search(query, 10).totalHits);
		es.shutdown();
		es.awaitTermination(1, TimeUnit.SECONDS);

		multireader.close();
		reader1.close();
		reader2.close();
		dir1.close();
		dir2.close();
	  }

	  public virtual void TestBS2DisjunctionNextVsAdvance()
	  {
		Directory d = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), d);
		int numDocs = atLeast(300);
		for (int docUpto = 0;docUpto < numDocs;docUpto++)
		{
		  string contents = "a";
		  if (random().Next(20) <= 16)
		  {
			contents += " b";
		  }
		  if (random().Next(20) <= 8)
		  {
			contents += " c";
		  }
		  if (random().Next(20) <= 4)
		  {
			contents += " d";
		  }
		  if (random().Next(20) <= 2)
		  {
			contents += " e";
		  }
		  if (random().Next(20) <= 1)
		  {
			contents += " f";
		  }
		  Document doc = new Document();
		  doc.add(new TextField("field", contents, Field.Store.NO));
		  w.addDocument(doc);
		}
		w.forceMerge(1);
		IndexReader r = w.Reader;
		IndexSearcher s = newSearcher(r);
		w.close();

		for (int iter = 0;iter < 10 * RANDOM_MULTIPLIER;iter++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("iter=" + iter);
		  }
		  IList<string> terms = new List<string>(Arrays.asList("a", "b", "c", "d", "e", "f"));
		  int numTerms = TestUtil.Next(random(), 1, terms.Count);
		  while (terms.Count > numTerms)
		  {
			terms.Remove(random().Next(terms.Count));
		  }

		  if (VERBOSE)
		  {
			Console.WriteLine("  terms=" + terms);
		  }

		  BooleanQuery q = new BooleanQuery();
		  foreach (string term in terms)
		  {
			q.add(new BooleanClause(new TermQuery(new Term("field", term)), BooleanClause.Occur_e.SHOULD));
		  }

		  Weight weight = s.createNormalizedWeight(q);

		  Scorer scorer = weight.scorer(s.leafContexts.get(0), null);

		  // First pass: just use .nextDoc() to gather all hits
		  IList<ScoreDoc> hits = new List<ScoreDoc>();
		  while (scorer.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
		  {
			hits.Add(new ScoreDoc(scorer.docID(), scorer.score()));
		  }

		  if (VERBOSE)
		  {
			Console.WriteLine("  " + hits.Count + " hits");
		  }

		  // Now, randomly next/advance through the list and
		  // verify exact match:
		  for (int iter2 = 0;iter2 < 10;iter2++)
		  {

			weight = s.createNormalizedWeight(q);
			scorer = weight.scorer(s.leafContexts.get(0), null);

			if (VERBOSE)
			{
			  Console.WriteLine("  iter2=" + iter2);
			}

			int upto = -1;
			while (upto < hits.Count)
			{
			  int nextUpto;
			  int nextDoc;
			  int left = hits.Count - upto;
			  if (left == 1 || random().nextBoolean())
			  {
				// next
				nextUpto = 1 + upto;
				nextDoc = scorer.nextDoc();
			  }
			  else
			  {
				// advance
				int inc = TestUtil.Next(random(), 1, left - 1);
				nextUpto = inc + upto;
				nextDoc = scorer.advance(hits[nextUpto].doc);
			  }

			  if (nextUpto == hits.Count)
			  {
				Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, nextDoc);
			  }
			  else
			  {
				ScoreDoc hit = hits[nextUpto];
				Assert.AreEqual(hit.doc, nextDoc);
				// Test for precise float equality:
				Assert.IsTrue("doc " + hit.doc + " has wrong score: expected=" + hit.score + " actual=" + scorer.score(), hit.score == scorer.score());
			  }
			  upto = nextUpto;
			}
		  }
		}

		r.close();
		d.close();
	  }

	  // LUCENE-4477 / LUCENE-4401:
	  public virtual void TestBooleanSpanQuery()
	  {
		bool failed = false;
		int hits = 0;
		Directory directory = newDirectory();
		Analyzer indexerAnalyzer = new MockAnalyzer(random());

		IndexWriterConfig config = new IndexWriterConfig(TEST_VERSION_CURRENT, indexerAnalyzer);
		IndexWriter writer = new IndexWriter(directory, config);
		string FIELD = "content";
		Document d = new Document();
		d.add(new TextField(FIELD, "clockwork orange", Field.Store.YES));
		writer.addDocument(d);
		writer.close();

		IndexReader indexReader = DirectoryReader.open(directory);
		IndexSearcher searcher = newSearcher(indexReader);

		BooleanQuery query = new BooleanQuery();
		SpanQuery sq1 = new SpanTermQuery(new Term(FIELD, "clockwork"));
		SpanQuery sq2 = new SpanTermQuery(new Term(FIELD, "clckwork"));
		query.add(sq1, BooleanClause.Occur_e.SHOULD);
		query.add(sq2, BooleanClause.Occur_e.SHOULD);
		TopScoreDocCollector collector = TopScoreDocCollector.create(1000, true);
		searcher.search(query, collector);
		hits = collector.topDocs().scoreDocs.length;
		foreach (ScoreDoc scoreDoc in collector.topDocs().scoreDocs)
		{
		  Console.WriteLine(scoreDoc.doc);
		}
		indexReader.close();
		Assert.AreEqual("Bug in boolean query composed of span queries", failed, false);
		Assert.AreEqual("Bug in boolean query composed of span queries", hits, 1);
		directory.close();
	  }

	  // LUCENE-5487
	  public virtual void TestInOrderWithMinShouldMatch()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newTextField("field", "some text here", Field.Store.NO));
		w.addDocument(doc);
		IndexReader r = w.Reader;
		w.close();
		IndexSearcher s = new IndexSearcherAnonymousInnerClassHelper(this, r);
		BooleanQuery bq = new BooleanQuery();
		bq.add(new TermQuery(new Term("field", "some")), BooleanClause.Occur_e.SHOULD);
		bq.add(new TermQuery(new Term("field", "text")), BooleanClause.Occur_e.SHOULD);
		bq.add(new TermQuery(new Term("field", "here")), BooleanClause.Occur_e.SHOULD);
		bq.MinimumNumberShouldMatch = 2;
		s.search(bq, 10);
		r.close();
		dir.close();
	  }

	  private class IndexSearcherAnonymousInnerClassHelper : IndexSearcher
	  {
		  private readonly TestBooleanQuery OuterInstance;

		  public IndexSearcherAnonymousInnerClassHelper(TestBooleanQuery outerInstance, IndexReader r) : base(r)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override void Search(IList<AtomicReaderContext> leaves, Weight weight, Collector collector)
		  {
			Assert.AreEqual(-1, collector.GetType().Name.IndexOf("OutOfOrder"));
			base.search(leaves, weight, collector);
		  }
	  }

	}

}