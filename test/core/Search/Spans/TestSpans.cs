using System.Collections.Generic;

namespace Lucene.Net.Search.Spans
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
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using ReaderUtil = Lucene.Net.Index.ReaderUtil;
	using Term = Lucene.Net.Index.Term;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using Similarity = Lucene.Net.Search.Similarities.Similarity;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestSpans : LuceneTestCase
	{
	  private IndexSearcher Searcher;
	  private IndexReader Reader;
	  private Directory Directory;

	  public const string Field = "field";

	  public override void SetUp()
	  {
		base.setUp();
		Directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
		for (int i = 0; i < DocFields.Length; i++)
		{
		  Document doc = new Document();
		  doc.add(newTextField(Field, DocFields[i], Field.Store.YES));
		  writer.addDocument(doc);
		}
		Reader = writer.Reader;
		writer.close();
		Searcher = newSearcher(Reader);
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Directory.close();
		base.tearDown();
	  }

	  private string[] DocFields = new string[] {"w1 w2 w3 w4 w5", "w1 w3 w2 w3", "w1 xx w2 yy w3", "w1 w3 xx w2 yy w3", "u2 u2 u1", "u2 xx u2 u1", "u2 u2 xx u1", "u2 xx u2 yy u1", "u2 xx u1 u2", "u2 u1 xx u2", "u1 u2 xx u2", "t1 t2 t1 t3 t2 t3", "s2 s1 s1 xx xx s2 xx s2 xx s1 xx xx xx xx xx s2 xx"};

	  public virtual SpanTermQuery MakeSpanTermQuery(string text)
	  {
		return new SpanTermQuery(new Term(Field, text));
	  }

	  private void CheckHits(Query query, int[] results)
	  {
		CheckHits.checkHits(random(), query, Field, Searcher, results);
	  }

	  private void OrderedSlopTest3SQ(SpanQuery q1, SpanQuery q2, SpanQuery q3, int slop, int[] expectedDocs)
	  {
		bool ordered = true;
		SpanNearQuery snq = new SpanNearQuery(new SpanQuery[]{q1,q2,q3}, slop, ordered);
		CheckHits(snq, expectedDocs);
	  }

	  public virtual void OrderedSlopTest3(int slop, int[] expectedDocs)
	  {
		OrderedSlopTest3SQ(MakeSpanTermQuery("w1"), MakeSpanTermQuery("w2"), MakeSpanTermQuery("w3"), slop, expectedDocs);
	  }

	  public virtual void OrderedSlopTest3Equal(int slop, int[] expectedDocs)
	  {
		OrderedSlopTest3SQ(MakeSpanTermQuery("w1"), MakeSpanTermQuery("w3"), MakeSpanTermQuery("w3"), slop, expectedDocs);
	  }

	  public virtual void OrderedSlopTest1Equal(int slop, int[] expectedDocs)
	  {
		OrderedSlopTest3SQ(MakeSpanTermQuery("u2"), MakeSpanTermQuery("u2"), MakeSpanTermQuery("u1"), slop, expectedDocs);
	  }

	  public virtual void TestSpanNearOrdered01()
	  {
		OrderedSlopTest3(0, new int[] {0});
	  }

	  public virtual void TestSpanNearOrdered02()
	  {
		OrderedSlopTest3(1, new int[] {0,1});
	  }

	  public virtual void TestSpanNearOrdered03()
	  {
		OrderedSlopTest3(2, new int[] {0,1,2});
	  }

	  public virtual void TestSpanNearOrdered04()
	  {
		OrderedSlopTest3(3, new int[] {0,1,2,3});
	  }

	  public virtual void TestSpanNearOrdered05()
	  {
		OrderedSlopTest3(4, new int[] {0,1,2,3});
	  }

	  public virtual void TestSpanNearOrderedEqual01()
	  {
		OrderedSlopTest3Equal(0, new int[] {});
	  }

	  public virtual void TestSpanNearOrderedEqual02()
	  {
		OrderedSlopTest3Equal(1, new int[] {1});
	  }

	  public virtual void TestSpanNearOrderedEqual03()
	  {
		OrderedSlopTest3Equal(2, new int[] {1});
	  }

	  public virtual void TestSpanNearOrderedEqual04()
	  {
		OrderedSlopTest3Equal(3, new int[] {1,3});
	  }

	  public virtual void TestSpanNearOrderedEqual11()
	  {
		OrderedSlopTest1Equal(0, new int[] {4});
	  }

	  public virtual void TestSpanNearOrderedEqual12()
	  {
		OrderedSlopTest1Equal(0, new int[] {4});
	  }

	  public virtual void TestSpanNearOrderedEqual13()
	  {
		OrderedSlopTest1Equal(1, new int[] {4,5,6});
	  }

	  public virtual void TestSpanNearOrderedEqual14()
	  {
		OrderedSlopTest1Equal(2, new int[] {4,5,6,7});
	  }

	  public virtual void TestSpanNearOrderedEqual15()
	  {
		OrderedSlopTest1Equal(3, new int[] {4,5,6,7});
	  }

	  public virtual void TestSpanNearOrderedOverlap()
	  {
		bool ordered = true;
		int slop = 1;
		SpanNearQuery snq = new SpanNearQuery(new SpanQuery[] {MakeSpanTermQuery("t1"), MakeSpanTermQuery("t2"), MakeSpanTermQuery("t3")}, slop, ordered);
		Spans spans = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, snq);

		Assert.IsTrue("first range", spans.next());
		Assert.AreEqual("first doc", 11, spans.doc());
		Assert.AreEqual("first start", 0, spans.start());
		Assert.AreEqual("first end", 4, spans.end());

		Assert.IsTrue("second range", spans.next());
		Assert.AreEqual("second doc", 11, spans.doc());
		Assert.AreEqual("second start", 2, spans.start());
		Assert.AreEqual("second end", 6, spans.end());

		Assert.IsFalse("third range", spans.next());
	  }


	  public virtual void TestSpanNearUnOrdered()
	  {

		//See http://www.gossamer-threads.com/lists/lucene/java-dev/52270 for discussion about this test
		SpanNearQuery snq;
		snq = new SpanNearQuery(new SpanQuery[] {MakeSpanTermQuery("u1"), MakeSpanTermQuery("u2")}, 0, false);
		Spans spans = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, snq);
		Assert.IsTrue("Does not have next and it should", spans.next());
		Assert.AreEqual("doc", 4, spans.doc());
		Assert.AreEqual("start", 1, spans.start());
		Assert.AreEqual("end", 3, spans.end());

		Assert.IsTrue("Does not have next and it should", spans.next());
		Assert.AreEqual("doc", 5, spans.doc());
		Assert.AreEqual("start", 2, spans.start());
		Assert.AreEqual("end", 4, spans.end());

		Assert.IsTrue("Does not have next and it should", spans.next());
		Assert.AreEqual("doc", 8, spans.doc());
		Assert.AreEqual("start", 2, spans.start());
		Assert.AreEqual("end", 4, spans.end());

		Assert.IsTrue("Does not have next and it should", spans.next());
		Assert.AreEqual("doc", 9, spans.doc());
		Assert.AreEqual("start", 0, spans.start());
		Assert.AreEqual("end", 2, spans.end());

		Assert.IsTrue("Does not have next and it should", spans.next());
		Assert.AreEqual("doc", 10, spans.doc());
		Assert.AreEqual("start", 0, spans.start());
		Assert.AreEqual("end", 2, spans.end());
		Assert.IsTrue("Has next and it shouldn't: " + spans.doc(), spans.next() == false);

		SpanNearQuery u1u2 = new SpanNearQuery(new SpanQuery[]{MakeSpanTermQuery("u1"), MakeSpanTermQuery("u2")}, 0, false);
		snq = new SpanNearQuery(new SpanQuery[] {u1u2, MakeSpanTermQuery("u2")}, 1, false);
		spans = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, snq);
		Assert.IsTrue("Does not have next and it should", spans.next());
		Assert.AreEqual("doc", 4, spans.doc());
		Assert.AreEqual("start", 0, spans.start());
		Assert.AreEqual("end", 3, spans.end());

		Assert.IsTrue("Does not have next and it should", spans.next());
		//unordered spans can be subsets
		Assert.AreEqual("doc", 4, spans.doc());
		Assert.AreEqual("start", 1, spans.start());
		Assert.AreEqual("end", 3, spans.end());

		Assert.IsTrue("Does not have next and it should", spans.next());
		Assert.AreEqual("doc", 5, spans.doc());
		Assert.AreEqual("start", 0, spans.start());
		Assert.AreEqual("end", 4, spans.end());

		Assert.IsTrue("Does not have next and it should", spans.next());
		Assert.AreEqual("doc", 5, spans.doc());
		Assert.AreEqual("start", 2, spans.start());
		Assert.AreEqual("end", 4, spans.end());

		Assert.IsTrue("Does not have next and it should", spans.next());
		Assert.AreEqual("doc", 8, spans.doc());
		Assert.AreEqual("start", 0, spans.start());
		Assert.AreEqual("end", 4, spans.end());


		Assert.IsTrue("Does not have next and it should", spans.next());
		Assert.AreEqual("doc", 8, spans.doc());
		Assert.AreEqual("start", 2, spans.start());
		Assert.AreEqual("end", 4, spans.end());

		Assert.IsTrue("Does not have next and it should", spans.next());
		Assert.AreEqual("doc", 9, spans.doc());
		Assert.AreEqual("start", 0, spans.start());
		Assert.AreEqual("end", 2, spans.end());

		Assert.IsTrue("Does not have next and it should", spans.next());
		Assert.AreEqual("doc", 9, spans.doc());
		Assert.AreEqual("start", 0, spans.start());
		Assert.AreEqual("end", 4, spans.end());

		Assert.IsTrue("Does not have next and it should", spans.next());
		Assert.AreEqual("doc", 10, spans.doc());
		Assert.AreEqual("start", 0, spans.start());
		Assert.AreEqual("end", 2, spans.end());

		Assert.IsTrue("Has next and it shouldn't", spans.next() == false);
	  }



	  private Spans OrSpans(string[] terms)
	  {
		SpanQuery[] sqa = new SpanQuery[terms.Length];
		for (int i = 0; i < terms.Length; i++)
		{
		  sqa[i] = MakeSpanTermQuery(terms[i]);
		}
		return MultiSpansWrapper.Wrap(Searcher.TopReaderContext, new SpanOrQuery(sqa));
	  }

	  private void TstNextSpans(Spans spans, int doc, int start, int end)
	  {
		Assert.IsTrue("next", spans.next());
		Assert.AreEqual("doc", doc, spans.doc());
		Assert.AreEqual("start", start, spans.start());
		Assert.AreEqual("end", end, spans.end());
	  }

	  public virtual void TestSpanOrEmpty()
	  {
		Spans spans = OrSpans(new string[0]);
		Assert.IsFalse("empty next", spans.next());

		SpanOrQuery a = new SpanOrQuery();
		SpanOrQuery b = new SpanOrQuery();
		Assert.IsTrue("empty should equal", a.Equals(b));
	  }

	  public virtual void TestSpanOrSingle()
	  {
		Spans spans = OrSpans(new string[] {"w5"});
		TstNextSpans(spans, 0, 4, 5);
		Assert.IsFalse("final next", spans.next());
	  }

	  public virtual void TestSpanOrMovesForward()
	  {
		Spans spans = OrSpans(new string[] {"w1", "xx"});

		spans.next();
		int doc = spans.doc();
		Assert.AreEqual(0, doc);

		spans.skipTo(0);
		doc = spans.doc();

		// LUCENE-1583:
		// according to Spans, a skipTo to the same doc or less
		// should still call next() on the underlying Spans
		Assert.AreEqual(1, doc);

	  }

	  public virtual void TestSpanOrDouble()
	  {
		Spans spans = OrSpans(new string[] {"w5", "yy"});
		TstNextSpans(spans, 0, 4, 5);
		TstNextSpans(spans, 2, 3, 4);
		TstNextSpans(spans, 3, 4, 5);
		TstNextSpans(spans, 7, 3, 4);
		Assert.IsFalse("final next", spans.next());
	  }

	  public virtual void TestSpanOrDoubleSkip()
	  {
		Spans spans = OrSpans(new string[] {"w5", "yy"});
		Assert.IsTrue("initial skipTo", spans.skipTo(3));
		Assert.AreEqual("doc", 3, spans.doc());
		Assert.AreEqual("start", 4, spans.start());
		Assert.AreEqual("end", 5, spans.end());
		TstNextSpans(spans, 7, 3, 4);
		Assert.IsFalse("final next", spans.next());
	  }

	  public virtual void TestSpanOrUnused()
	  {
		Spans spans = OrSpans(new string[] {"w5", "unusedTerm", "yy"});
		TstNextSpans(spans, 0, 4, 5);
		TstNextSpans(spans, 2, 3, 4);
		TstNextSpans(spans, 3, 4, 5);
		TstNextSpans(spans, 7, 3, 4);
		Assert.IsFalse("final next", spans.next());
	  }

	  public virtual void TestSpanOrTripleSameDoc()
	  {
		Spans spans = OrSpans(new string[] {"t1", "t2", "t3"});
		TstNextSpans(spans, 11, 0, 1);
		TstNextSpans(spans, 11, 1, 2);
		TstNextSpans(spans, 11, 2, 3);
		TstNextSpans(spans, 11, 3, 4);
		TstNextSpans(spans, 11, 4, 5);
		TstNextSpans(spans, 11, 5, 6);
		Assert.IsFalse("final next", spans.next());
	  }

	  public virtual void TestSpanScorerZeroSloppyFreq()
	  {
		bool ordered = true;
		int slop = 1;
		IndexReaderContext topReaderContext = Searcher.TopReaderContext;
		IList<AtomicReaderContext> leaves = topReaderContext.leaves();
		int subIndex = ReaderUtil.subIndex(11, leaves);
		for (int i = 0, c = leaves.Count; i < c; i++)
		{
		  AtomicReaderContext ctx = leaves[i];

		  Similarity sim = new DefaultSimilarityAnonymousInnerClassHelper(this);

		  Similarity oldSim = Searcher.Similarity;
		  Scorer spanScorer;
		  try
		  {
			Searcher.Similarity = sim;
			SpanNearQuery snq = new SpanNearQuery(new SpanQuery[] {MakeSpanTermQuery("t1"), MakeSpanTermQuery("t2")}, slop, ordered);

			spanScorer = Searcher.createNormalizedWeight(snq).scorer(ctx, ctx.reader().LiveDocs);
		  }
		  finally
		  {
			Searcher.Similarity = oldSim;
		  }
		  if (i == subIndex)
		  {
			Assert.IsTrue("first doc", spanScorer.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			Assert.AreEqual("first doc number", spanScorer.docID() + ctx.docBase, 11);
			float score = spanScorer.score();
			Assert.IsTrue("first doc score should be zero, " + score, score == 0.0f);
		  }
		  else
		  {
			Assert.IsTrue("no second doc", spanScorer.nextDoc() == DocIdSetIterator.NO_MORE_DOCS);
		  }
		}
	  }

	  private class DefaultSimilarityAnonymousInnerClassHelper : DefaultSimilarity
	  {
		  private readonly TestSpans OuterInstance;

		  public DefaultSimilarityAnonymousInnerClassHelper(TestSpans outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override float SloppyFreq(int distance)
		  {
			return 0.0f;
		  }
	  }

	  // LUCENE-1404
	  private void AddDoc(IndexWriter writer, string id, string text)
	  {
		Document doc = new Document();
		doc.add(newStringField("id", id, Field.Store.YES));
		doc.add(newTextField("text", text, Field.Store.YES));
		writer.addDocument(doc);
	  }

	  // LUCENE-1404
	  private int HitCount(IndexSearcher searcher, string word)
	  {
		return searcher.search(new TermQuery(new Term("text", word)), 10).totalHits;
	  }

	  // LUCENE-1404
	  private SpanQuery CreateSpan(string value)
	  {
		return new SpanTermQuery(new Term("text", value));
	  }

	  // LUCENE-1404
	  private SpanQuery CreateSpan(int slop, bool ordered, SpanQuery[] clauses)
	  {
		return new SpanNearQuery(clauses, slop, ordered);
	  }

	  // LUCENE-1404
	  private SpanQuery CreateSpan(int slop, bool ordered, string term1, string term2)
	  {
		return CreateSpan(slop, ordered, new SpanQuery[] {CreateSpan(term1), CreateSpan(term2)});
	  }

	  // LUCENE-1404
	  public virtual void TestNPESpanQuery()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		// Add documents
		AddDoc(writer, "1", "the big dogs went running to the market");
		AddDoc(writer, "2", "the cat chased the mouse, then the cat ate the mouse quickly");

		// Commit
		writer.close();

		// Get searcher
		IndexReader reader = DirectoryReader.open(dir);
		IndexSearcher searcher = newSearcher(reader);

		// Control (make sure docs indexed)
		Assert.AreEqual(2, HitCount(searcher, "the"));
		Assert.AreEqual(1, HitCount(searcher, "cat"));
		Assert.AreEqual(1, HitCount(searcher, "dogs"));
		Assert.AreEqual(0, HitCount(searcher, "rabbit"));

		// this throws exception (it shouldn't)
		Assert.AreEqual(1, searcher.search(CreateSpan(0, true, new SpanQuery[] {CreateSpan(4, false, "chased", "cat"), CreateSpan("ate")}), 10).totalHits);
		reader.close();
		dir.close();
	  }


	  public virtual void TestSpanNots()
	  {
		 Assert.AreEqual("SpanNotIncludeExcludeSame1", 0, SpanCount("s2", "s2", 0, 0), 0);
		 Assert.AreEqual("SpanNotIncludeExcludeSame2", 0, SpanCount("s2", "s2", 10, 10), 0);

		 //focus on behind
		 Assert.AreEqual("SpanNotS2NotS1_6_0", 1, SpanCount("s2", "s1", 6, 0));
		 Assert.AreEqual("SpanNotS2NotS1_5_0", 2, SpanCount("s2", "s1", 5, 0));
		 Assert.AreEqual("SpanNotS2NotS1_3_0", 3, SpanCount("s2", "s1", 3, 0));
		 Assert.AreEqual("SpanNotS2NotS1_2_0", 4, SpanCount("s2", "s1", 2, 0));
		 Assert.AreEqual("SpanNotS2NotS1_0_0", 4, SpanCount("s2", "s1", 0, 0));

		 //focus on both
		 Assert.AreEqual("SpanNotS2NotS1_3_1", 2, SpanCount("s2", "s1", 3, 1));
		 Assert.AreEqual("SpanNotS2NotS1_2_1", 3, SpanCount("s2", "s1", 2, 1));
		 Assert.AreEqual("SpanNotS2NotS1_1_1", 3, SpanCount("s2", "s1", 1, 1));
		 Assert.AreEqual("SpanNotS2NotS1_10_10", 0, SpanCount("s2", "s1", 10, 10));

		 //focus on ahead
		 Assert.AreEqual("SpanNotS1NotS2_10_10", 0, SpanCount("s1", "s2", 10, 10));
		 Assert.AreEqual("SpanNotS1NotS2_0_1", 3, SpanCount("s1", "s2", 0, 1));
		 Assert.AreEqual("SpanNotS1NotS2_0_2", 3, SpanCount("s1", "s2", 0, 2));
		 Assert.AreEqual("SpanNotS1NotS2_0_3", 2, SpanCount("s1", "s2", 0, 3));
		 Assert.AreEqual("SpanNotS1NotS2_0_4", 1, SpanCount("s1", "s2", 0, 4));
		 Assert.AreEqual("SpanNotS1NotS2_0_8", 0, SpanCount("s1", "s2", 0, 8));

		 //exclude doesn't exist
		 Assert.AreEqual("SpanNotS1NotS3_8_8", 3, SpanCount("s1", "s3", 8, 8));

		 //include doesn't exist
		 Assert.AreEqual("SpanNotS3NotS1_8_8", 0, SpanCount("s3", "s1", 8, 8));

	  }

	  private int SpanCount(string include, string exclude, int pre, int post)
	  {
		 SpanTermQuery iq = new SpanTermQuery(new Term(Field, include));
		 SpanTermQuery eq = new SpanTermQuery(new Term(Field, exclude));
		 SpanNotQuery snq = new SpanNotQuery(iq, eq, pre, post);
		 Spans spans = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, snq);

		 int i = 0;
		 while (spans.next())
		 {
			i++;
		 }
		 return i;
	  }

	}

}