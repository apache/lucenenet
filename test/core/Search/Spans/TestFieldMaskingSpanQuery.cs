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
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using TFIDFSimilarity = Lucene.Net.Search.Similarities.TFIDFSimilarity;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;

	public class TestFieldMaskingSpanQuery : LuceneTestCase
	{

	  protected internal static Document Doc(Field[] fields)
	  {
		Document doc = new Document();
		for (int i = 0; i < fields.Length; i++)
		{
		  doc.add(fields[i]);
		}
		return doc;
	  }

	  protected internal static Field Field(string name, string value)
	  {
		return newTextField(name, value, Field.Store.NO);
	  }

	  protected internal static IndexSearcher Searcher;
	  protected internal static Directory Directory;
	  protected internal static IndexReader Reader;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		Directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));

		writer.addDocument(Doc(new Field[] {Field("id", "0"), Field("gender", "male"), Field("first", "james"), Field("last", "jones")}));

		writer.addDocument(Doc(new Field[] {Field("id", "1"), Field("gender", "male"), Field("first", "james"), Field("last", "smith"), Field("gender", "female"), Field("first", "sally"), Field("last", "jones")}));

		writer.addDocument(Doc(new Field[] {Field("id", "2"), Field("gender", "female"), Field("first", "greta"), Field("last", "jones"), Field("gender", "female"), Field("first", "sally"), Field("last", "smith"), Field("gender", "male"), Field("first", "james"), Field("last", "jones")}));

		writer.addDocument(Doc(new Field[] {Field("id", "3"), Field("gender", "female"), Field("first", "lisa"), Field("last", "jones"), Field("gender", "male"), Field("first", "bob"), Field("last", "costas")}));

		writer.addDocument(Doc(new Field[] {Field("id", "4"), Field("gender", "female"), Field("first", "sally"), Field("last", "smith"), Field("gender", "female"), Field("first", "linda"), Field("last", "dixit"), Field("gender", "male"), Field("first", "bubba"), Field("last", "jones")}));
		Reader = writer.Reader;
		writer.close();
		Searcher = newSearcher(Reader);
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

	  protected internal virtual void Check(SpanQuery q, int[] docs)
	  {
		CheckHits.checkHitCollector(random(), q, null, Searcher, docs);
	  }

	  public virtual void TestRewrite0()
	  {
		SpanQuery q = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally")), "first");
		q.Boost = 8.7654321f;
		SpanQuery qr = (SpanQuery) Searcher.rewrite(q);

		QueryUtils.checkEqual(q, qr);

		Set<Term> terms = new HashSet<Term>();
		qr.extractTerms(terms);
		Assert.AreEqual(1, terms.size());
	  }

	  public virtual void TestRewrite1()
	  {
		// mask an anon SpanQuery class that rewrites to something else.
		SpanQuery q = new FieldMaskingSpanQuery(new SpanTermQueryAnonymousInnerClassHelper(this, new Term("last", "sally")), "first");

		SpanQuery qr = (SpanQuery) Searcher.rewrite(q);

		QueryUtils.checkUnequal(q, qr);

		Set<Term> terms = new HashSet<Term>();
		qr.extractTerms(terms);
		Assert.AreEqual(2, terms.size());
	  }

	  private class SpanTermQueryAnonymousInnerClassHelper : SpanTermQuery
	  {
		  private readonly TestFieldMaskingSpanQuery OuterInstance;

		  public SpanTermQueryAnonymousInnerClassHelper(TestFieldMaskingSpanQuery outerInstance, Term org) : base(Term)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override Query Rewrite(IndexReader reader)
		  {
			return new SpanOrQuery(new SpanTermQuery(new Term("first", "sally")), new SpanTermQuery(new Term("first", "james")));
		  }
	  }

	  public virtual void TestRewrite2()
	  {
		SpanQuery q1 = new SpanTermQuery(new Term("last", "smith"));
		SpanQuery q2 = new SpanTermQuery(new Term("last", "jones"));
		SpanQuery q = new SpanNearQuery(new SpanQuery[] {q1, new FieldMaskingSpanQuery(q2, "last")}, 1, true);
		Query qr = Searcher.rewrite(q);

		QueryUtils.checkEqual(q, qr);

		HashSet<Term> set = new HashSet<Term>();
		qr.extractTerms(set);
		Assert.AreEqual(2, set.Count);
	  }

	  public virtual void TestEquality1()
	  {
		SpanQuery q1 = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally")), "first");
		SpanQuery q2 = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally")), "first");
		SpanQuery q3 = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally")), "XXXXX");
		SpanQuery q4 = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "XXXXX")), "first");
		SpanQuery q5 = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("xXXX", "sally")), "first");
		QueryUtils.checkEqual(q1, q2);
		QueryUtils.checkUnequal(q1, q3);
		QueryUtils.checkUnequal(q1, q4);
		QueryUtils.checkUnequal(q1, q5);

		SpanQuery qA = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally")), "first");
		qA.Boost = 9f;
		SpanQuery qB = new FieldMaskingSpanQuery(new SpanTermQuery(new Term("last", "sally")), "first");
		QueryUtils.checkUnequal(qA, qB);
		qB.Boost = 9f;
		QueryUtils.checkEqual(qA, qB);

	  }

	  public virtual void TestNoop0()
	  {
		SpanQuery q1 = new SpanTermQuery(new Term("last", "sally"));
		SpanQuery q = new FieldMaskingSpanQuery(q1, "first");
		Check(q, new int[] { }); // :EMPTY:
	  }
	  public virtual void TestNoop1()
	  {
		SpanQuery q1 = new SpanTermQuery(new Term("last", "smith"));
		SpanQuery q2 = new SpanTermQuery(new Term("last", "jones"));
		SpanQuery q = new SpanNearQuery(new SpanQuery[] {q1, new FieldMaskingSpanQuery(q2, "last")}, 0, true);
		Check(q, new int[] {1, 2});
		q = new SpanNearQuery(new SpanQuery[] {new FieldMaskingSpanQuery(q1, "last"), new FieldMaskingSpanQuery(q2, "last")}, 0, true);
		Check(q, new int[] {1, 2});
	  }

	  public virtual void TestSimple1()
	  {
		SpanQuery q1 = new SpanTermQuery(new Term("first", "james"));
		SpanQuery q2 = new SpanTermQuery(new Term("last", "jones"));
		SpanQuery q = new SpanNearQuery(new SpanQuery[] {q1, new FieldMaskingSpanQuery(q2, "first")}, -1, false);
		Check(q, new int[] {0, 2});
		q = new SpanNearQuery(new SpanQuery[] {new FieldMaskingSpanQuery(q2, "first"), q1}, -1, false);
		Check(q, new int[] {0, 2});
		q = new SpanNearQuery(new SpanQuery[] {q2, new FieldMaskingSpanQuery(q1, "last")}, -1, false);
		Check(q, new int[] {0, 2});
		q = new SpanNearQuery(new SpanQuery[] {new FieldMaskingSpanQuery(q1, "last"), q2}, -1, false);
		Check(q, new int[] {0, 2});

	  }

	  public virtual void TestSimple2()
	  {
		assumeTrue("Broken scoring: LUCENE-3723", Searcher.Similarity is TFIDFSimilarity);
		SpanQuery q1 = new SpanTermQuery(new Term("gender", "female"));
		SpanQuery q2 = new SpanTermQuery(new Term("last", "smith"));
		SpanQuery q = new SpanNearQuery(new SpanQuery[] {q1, new FieldMaskingSpanQuery(q2, "gender")}, -1, false);
		Check(q, new int[] {2, 4});
		q = new SpanNearQuery(new SpanQuery[] {new FieldMaskingSpanQuery(q1, "id"), new FieldMaskingSpanQuery(q2, "id")}, -1, false);
		Check(q, new int[] {2, 4});
	  }

	  public virtual void TestSpans0()
	  {
		SpanQuery q1 = new SpanTermQuery(new Term("gender", "female"));
		SpanQuery q2 = new SpanTermQuery(new Term("first", "james"));
		SpanQuery q = new SpanOrQuery(q1, new FieldMaskingSpanQuery(q2, "gender"));
		Check(q, new int[] {0, 1, 2, 3, 4});

		Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);

		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(0,0,1), s(span));

		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(1,0,1), s(span));

		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(1,1,2), s(span));

		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(2,0,1), s(span));

		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(2,1,2), s(span));

		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(2,2,3), s(span));

		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(3,0,1), s(span));

		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(4,0,1), s(span));

		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(4,1,2), s(span));

		Assert.AreEqual(false, span.next());
	  }

	  public virtual void TestSpans1()
	  {
		SpanQuery q1 = new SpanTermQuery(new Term("first", "sally"));
		SpanQuery q2 = new SpanTermQuery(new Term("first", "james"));
		SpanQuery qA = new SpanOrQuery(q1, q2);
		SpanQuery qB = new FieldMaskingSpanQuery(qA, "id");

		Check(qA, new int[] {0, 1, 2, 4});
		Check(qB, new int[] {0, 1, 2, 4});

		Spans spanA = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, qA);
		Spans spanB = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, qB);

		while (spanA.next())
		{
		  Assert.IsTrue("spanB not still going", spanB.next());
		  Assert.AreEqual("spanA not equal spanB", s(spanA), s(spanB));
		}
		Assert.IsTrue("spanB still going even tough spanA is done", !(spanB.next()));

	  }

	  public virtual void TestSpans2()
	  {
		assumeTrue("Broken scoring: LUCENE-3723", Searcher.Similarity is TFIDFSimilarity);
		SpanQuery qA1 = new SpanTermQuery(new Term("gender", "female"));
		SpanQuery qA2 = new SpanTermQuery(new Term("first", "james"));
		SpanQuery qA = new SpanOrQuery(qA1, new FieldMaskingSpanQuery(qA2, "gender"));
		SpanQuery qB = new SpanTermQuery(new Term("last", "jones"));
		SpanQuery q = new SpanNearQuery(new SpanQuery[] {new FieldMaskingSpanQuery(qA, "id"), new FieldMaskingSpanQuery(qB, "id")}, -1, false);
		Check(q, new int[] {0, 1, 2, 3});

		Spans span = MultiSpansWrapper.Wrap(Searcher.TopReaderContext, q);

		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(0,0,1), s(span));

		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(1,1,2), s(span));

		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(2,0,1), s(span));

		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(2,2,3), s(span));

		Assert.AreEqual(true, span.next());
		Assert.AreEqual(s(3,0,1), s(span));

		Assert.AreEqual(false, span.next());
	  }

	  public virtual string s(Spans span)
	  {
		return s(span.doc(), span.start(), span.end());
	  }
	  public virtual string s(int doc, int start, int end)
	  {
		return "s(" + doc + "," + start + "," + end + ")";
	  }

	}

}