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
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestNearSpansOrdered : LuceneTestCase
	{
	  protected internal IndexSearcher Searcher;
	  protected internal Directory Directory;
	  protected internal IndexReader Reader;

	  public const string FIELD = "field";

	  public override void TearDown()
	  {
		Reader.close();
		Directory.close();
		base.tearDown();
	  }

	  public override void SetUp()
	  {
		base.setUp();
		Directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
		for (int i = 0; i < DocFields.Length; i++)
		{
		  Document doc = new Document();
		  doc.add(newTextField(FIELD, DocFields[i], Field.Store.NO));
		  writer.addDocument(doc);
		}
		Reader = writer.Reader;
		writer.close();
		Searcher = newSearcher(Reader);
	  }

	  protected internal string[] DocFields = new string[] {"w1 w2 w3 w4 w5", "w1 w3 w2 w3 zz", "w1 xx w2 yy w3", "w1 w3 xx w2 yy w3 zz"};

	  protected internal virtual SpanNearQuery MakeQuery(string s1, string s2, string s3, int slop, bool inOrder)
	  {
		return new SpanNearQuery(new SpanQuery[] {new SpanTermQuery(new Term(FIELD, s1)), new SpanTermQuery(new Term(FIELD, s2)), new SpanTermQuery(new Term(FIELD, s3))}, slop, inOrder);
	  }
	  protected internal virtual SpanNearQuery MakeQuery()
	  {
		return MakeQuery("w1","w2","w3",1,true);
	  }

	  public virtual void TestSpanNearQuery()
	  {
		SpanNearQuery q = MakeQuery();
		CheckHits.checkHits(random(), q, FIELD, Searcher, new int[] {0,1});
	  }

	  public virtual string s(Spans span)
	  {
		return s(span.doc(), span.start(), span.end());
	  }
	  public virtual string s(int doc, int start, int end)
	  {
		return "s(" + doc + "," + start + "," + end + ")";
	  }

	  public virtual void TestNearSpansNext()
	  {
		SpanNearQuery q = MakeQuery();
		Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);
		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(0,0,3), s(span));
		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(1,0,4), s(span));
		Assert.AreEqual(false, span.next());
	  }

	  /// <summary>
	  /// test does not imply that skipTo(doc+1) should work exactly the
	  /// same as next -- it's only applicable in this case since we know doc
	  /// does not contain more than one span
	  /// </summary>
	  public virtual void TestNearSpansSkipToLikeNext()
	  {
		SpanNearQuery q = MakeQuery();
		Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);
		Assert.AreEqual(true, span.skipTo(0));
		Assert.AreEqual(s(0,0,3), s(span));
		Assert.AreEqual(true, span.skipTo(1));
		Assert.AreEqual(s(1,0,4), s(span));
		Assert.AreEqual(false, span.skipTo(2));
	  }

	  public virtual void TestNearSpansNextThenSkipTo()
	  {
		SpanNearQuery q = MakeQuery();
		Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);
		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(0,0,3), s(span));
		Assert.AreEqual(true, span.skipTo(1));
		Assert.AreEqual(s(1,0,4), s(span));
		Assert.AreEqual(false, span.next());
	  }

	  public virtual void TestNearSpansNextThenSkipPast()
	  {
		SpanNearQuery q = MakeQuery();
		Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);
		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(0,0,3), s(span));
		Assert.AreEqual(false, span.skipTo(2));
	  }

	  public virtual void TestNearSpansSkipPast()
	  {
		SpanNearQuery q = MakeQuery();
		Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);
		Assert.AreEqual(false, span.skipTo(2));
	  }

	  public virtual void TestNearSpansSkipTo0()
	  {
		SpanNearQuery q = MakeQuery();
		Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);
		Assert.AreEqual(true, span.skipTo(0));
		Assert.AreEqual(s(0,0,3), s(span));
	  }

	  public virtual void TestNearSpansSkipTo1()
	  {
		SpanNearQuery q = MakeQuery();
		Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);
		Assert.AreEqual(true, span.skipTo(1));
		Assert.AreEqual(s(1,0,4), s(span));
	  }

	  /// <summary>
	  /// not a direct test of NearSpans, but a demonstration of how/when
	  /// this causes problems
	  /// </summary>
	  public virtual void TestSpanNearScorerSkipTo1()
	  {
		SpanNearQuery q = MakeQuery();
		Weight w = Searcher.createNormalizedWeight(q);
		IndexReaderContext topReaderContext = Searcher.TopReaderContext;
		AtomicReaderContext leave = topReaderContext.leaves().get(0);
		Scorer s = w.scorer(leave, leave.reader().LiveDocs);
		Assert.AreEqual(1, s.advance(1));
	  }

	  /// <summary>
	  /// not a direct test of NearSpans, but a demonstration of how/when
	  /// this causes problems
	  /// </summary>
	  public virtual void TestSpanNearScorerExplain()
	  {
		SpanNearQuery q = MakeQuery();
		Explanation e = Searcher.explain(q, 1);
		Assert.IsTrue("Scorer explanation value for doc#1 isn't positive: " + e.ToString(), 0.0f < e.Value);
	  }

	}

}