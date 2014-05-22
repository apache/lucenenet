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

	using Term = Lucene.Net.Index.Term;
	using Occur = Lucene.Net.Search.BooleanClause.Occur;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using Lucene.Net.Search.Spans;

	/// <summary>
	/// TestExplanations subclass that builds up super crazy complex queries
	/// on the assumption that if the explanations work out right for them,
	/// they should work for anything.
	/// </summary>
	public class TestComplexExplanations : TestExplanations
	{

	  /// <summary>
	  /// Override the Similarity used in our searcher with one that plays
	  /// nice with boosts of 0.0
	  /// </summary>
	  public override void SetUp()
	  {
		base.setUp();
		Searcher.Similarity = CreateQnorm1Similarity();
	  }

	  public override void TearDown()
	  {
		Searcher.Similarity = IndexSearcher.DefaultSimilarity;
		base.tearDown();
	  }

	  // must be static for weight serialization tests 
	  private static DefaultSimilarity CreateQnorm1Similarity()
	  {
		return new DefaultSimilarityAnonymousInnerClassHelper();
	  }

	  private class DefaultSimilarityAnonymousInnerClassHelper : DefaultSimilarity
	  {
		  public DefaultSimilarityAnonymousInnerClassHelper()
		  {
		  }

		  public override float QueryNorm(float sumOfSquaredWeights)
		  {
			return 1.0f; // / (float) Math.sqrt(1.0f + sumOfSquaredWeights);
		  }
	  }


	  public virtual void Test1()
	  {

		BooleanQuery q = new BooleanQuery();

		PhraseQuery phraseQuery = new PhraseQuery();
		phraseQuery.Slop = 1;
		phraseQuery.add(new Term(FIELD, "w1"));
		phraseQuery.add(new Term(FIELD, "w2"));
		q.add(phraseQuery, Occur.MUST);
		q.add(Snear(St("w2"), Sor("w5","zz"), 4, true), Occur.SHOULD);
		q.add(Snear(Sf("w3",2), St("w2"), St("w3"), 5, true), Occur.SHOULD);

		Query t = new FilteredQuery(new TermQuery(new Term(FIELD, "xx")), new ItemizedFilter(new int[] {1,3}));
		t.Boost = 1000;
		q.add(t, Occur.SHOULD);

		t = new ConstantScoreQuery(new ItemizedFilter(new int[] {0,2}));
		t.Boost = 30;
		q.add(t, Occur.SHOULD);

		DisjunctionMaxQuery dm = new DisjunctionMaxQuery(0.2f);
		dm.add(Snear(St("w2"), Sor("w5","zz"), 4, true));
		dm.add(new TermQuery(new Term(FIELD, "QQ")));

		BooleanQuery xxYYZZ = new BooleanQuery();
		xxYYZZ.add(new TermQuery(new Term(FIELD, "xx")), Occur.SHOULD);
		xxYYZZ.add(new TermQuery(new Term(FIELD, "yy")), Occur.SHOULD);
		xxYYZZ.add(new TermQuery(new Term(FIELD, "zz")), Occur.MUST_NOT);

		dm.add(xxYYZZ);

		BooleanQuery xxW1 = new BooleanQuery();
		xxW1.add(new TermQuery(new Term(FIELD, "xx")), Occur.MUST_NOT);
		xxW1.add(new TermQuery(new Term(FIELD, "w1")), Occur.MUST_NOT);

		dm.add(xxW1);

		DisjunctionMaxQuery dm2 = new DisjunctionMaxQuery(0.5f);
		dm2.add(new TermQuery(new Term(FIELD, "w1")));
		dm2.add(new TermQuery(new Term(FIELD, "w2")));
		dm2.add(new TermQuery(new Term(FIELD, "w3")));
		dm.add(dm2);

		q.add(dm, Occur.SHOULD);

		BooleanQuery b = new BooleanQuery();
		b.MinimumNumberShouldMatch = 2;
		b.add(Snear("w1","w2",1,true), Occur.SHOULD);
		b.add(Snear("w2","w3",1,true), Occur.SHOULD);
		b.add(Snear("w1","w3",3,true), Occur.SHOULD);

		q.add(b, Occur.SHOULD);

		Qtest(q, new int[] {0,1,2});
	  }

	  public virtual void Test2()
	  {

		BooleanQuery q = new BooleanQuery();

		PhraseQuery phraseQuery = new PhraseQuery();
		phraseQuery.Slop = 1;
		phraseQuery.add(new Term(FIELD, "w1"));
		phraseQuery.add(new Term(FIELD, "w2"));
		q.add(phraseQuery, Occur.MUST);
		q.add(Snear(St("w2"), Sor("w5","zz"), 4, true), Occur.SHOULD);
		q.add(Snear(Sf("w3",2), St("w2"), St("w3"), 5, true), Occur.SHOULD);

		Query t = new FilteredQuery(new TermQuery(new Term(FIELD, "xx")), new ItemizedFilter(new int[] {1,3}));
		t.Boost = 1000;
		q.add(t, Occur.SHOULD);

		t = new ConstantScoreQuery(new ItemizedFilter(new int[] {0,2}));
		t.Boost = -20.0f;
		q.add(t, Occur.SHOULD);

		DisjunctionMaxQuery dm = new DisjunctionMaxQuery(0.2f);
		dm.add(Snear(St("w2"), Sor("w5","zz"), 4, true));
		dm.add(new TermQuery(new Term(FIELD, "QQ")));

		BooleanQuery xxYYZZ = new BooleanQuery();
		xxYYZZ.add(new TermQuery(new Term(FIELD, "xx")), Occur.SHOULD);
		xxYYZZ.add(new TermQuery(new Term(FIELD, "yy")), Occur.SHOULD);
		xxYYZZ.add(new TermQuery(new Term(FIELD, "zz")), Occur.MUST_NOT);

		dm.add(xxYYZZ);

		BooleanQuery xxW1 = new BooleanQuery();
		xxW1.add(new TermQuery(new Term(FIELD, "xx")), Occur.MUST_NOT);
		xxW1.add(new TermQuery(new Term(FIELD, "w1")), Occur.MUST_NOT);

		dm.add(xxW1);

		DisjunctionMaxQuery dm2 = new DisjunctionMaxQuery(0.5f);
		dm2.add(new TermQuery(new Term(FIELD, "w1")));
		dm2.add(new TermQuery(new Term(FIELD, "w2")));
		dm2.add(new TermQuery(new Term(FIELD, "w3")));
		dm.add(dm2);

		q.add(dm, Occur.SHOULD);

		BooleanQuery b = new BooleanQuery();
		b.MinimumNumberShouldMatch = 2;
		b.add(Snear("w1","w2",1,true), Occur.SHOULD);
		b.add(Snear("w2","w3",1,true), Occur.SHOULD);
		b.add(Snear("w1","w3",3,true), Occur.SHOULD);
		b.Boost = 0.0f;

		q.add(b, Occur.SHOULD);

		Qtest(q, new int[] {0,1,2});
	  }

	  // :TODO: we really need more crazy complex cases.


	  // //////////////////////////////////////////////////////////////////

	  // The rest of these aren't that complex, but they are <i>somewhat</i>
	  // complex, and they expose weakness in dealing with queries that match
	  // with scores of 0 wrapped in other queries

	  public virtual void TestT3()
	  {
		TermQuery query = new TermQuery(new Term(FIELD, "w1"));
		query.Boost = 0;
		Bqtest(query, new int[] {0,1,2,3});
	  }

	  public virtual void TestMA3()
	  {
		Query q = new MatchAllDocsQuery();
		q.Boost = 0;
		Bqtest(q, new int[] {0,1,2,3});
	  }

	  public virtual void TestFQ5()
	  {
		TermQuery query = new TermQuery(new Term(FIELD, "xx"));
		query.Boost = 0;
		Bqtest(new FilteredQuery(query, new ItemizedFilter(new int[] {1,3})), new int[] {3});
	  }

	  public virtual void TestCSQ4()
	  {
		Query q = new ConstantScoreQuery(new ItemizedFilter(new int[] {3}));
		q.Boost = 0;
		Bqtest(q, new int[] {3});
	  }

	  public virtual void TestDMQ10()
	  {
		DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);

		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(FIELD, "yy")), Occur.SHOULD);
		TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w5"));
		boostedQuery.Boost = 100;
		query.add(boostedQuery, Occur.SHOULD);

		q.add(query);

		TermQuery xxBoostedQuery = new TermQuery(new Term(FIELD, "xx"));
		xxBoostedQuery.Boost = 0;

		q.add(xxBoostedQuery);
		q.Boost = 0.0f;
		Bqtest(q, new int[] {0,2,3});
	  }

	  public virtual void TestMPQ7()
	  {
		MultiPhraseQuery q = new MultiPhraseQuery();
		q.add(Ta(new string[] {"w1"}));
		q.add(Ta(new string[] {"w2"}));
		q.Slop = 1;
		q.Boost = 0.0f;
		Bqtest(q, new int[] {0,1,2});
	  }

	  public virtual void TestBQ12()
	  {
		// NOTE: using qtest not bqtest
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(FIELD, "w1")), Occur.SHOULD);
		TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w2"));
		boostedQuery.Boost = 0;
		query.add(boostedQuery, Occur.SHOULD);

		Qtest(query, new int[] {0,1,2,3});
	  }
	  public virtual void TestBQ13()
	  {
		// NOTE: using qtest not bqtest
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(FIELD, "w1")), Occur.SHOULD);
		TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w5"));
		boostedQuery.Boost = 0;
		query.add(boostedQuery, Occur.MUST_NOT);

		Qtest(query, new int[] {1,2,3});
	  }
	  public virtual void TestBQ18()
	  {
		// NOTE: using qtest not bqtest
		BooleanQuery query = new BooleanQuery();
		TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w1"));
		boostedQuery.Boost = 0;
		query.add(boostedQuery, Occur.MUST);
		query.add(new TermQuery(new Term(FIELD, "w2")), Occur.SHOULD);

		Qtest(query, new int[] {0,1,2,3});
	  }
	  public virtual void TestBQ21()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(FIELD, "w1")), Occur.MUST);
		query.add(new TermQuery(new Term(FIELD, "w2")), Occur.SHOULD);
		query.Boost = 0;

		Bqtest(query, new int[] {0,1,2,3});
	  }
	  public virtual void TestBQ22()
	  {
		BooleanQuery query = new BooleanQuery();
		TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w1"));
		boostedQuery.Boost = 0;
		query.add(boostedQuery, Occur.MUST);
		query.add(new TermQuery(new Term(FIELD, "w2")), Occur.SHOULD);
		query.Boost = 0;

		Bqtest(query, new int[] {0,1,2,3});
	  }

	  public virtual void TestST3()
	  {
		SpanQuery q = St("w1");
		q.Boost = 0;
		Bqtest(q, new int[] {0,1,2,3});
	  }
	  public virtual void TestST6()
	  {
		SpanQuery q = St("xx");
		q.Boost = 0;
		Qtest(q, new int[] {2,3});
	  }

	  public virtual void TestSF3()
	  {
		SpanQuery q = Sf(("w1"),1);
		q.Boost = 0;
		Bqtest(q, new int[] {0,1,2,3});
	  }
	  public virtual void TestSF7()
	  {
		SpanQuery q = Sf(("xx"),3);
		q.Boost = 0;
		Bqtest(q, new int[] {2,3});
	  }

	  public virtual void TestSNot3()
	  {
		SpanQuery q = Snot(Sf("w1",10),St("QQ"));
		q.Boost = 0;
		Bqtest(q, new int[] {0,1,2,3});
	  }
	  public virtual void TestSNot6()
	  {
		SpanQuery q = Snot(Sf("w1",10),St("xx"));
		q.Boost = 0;
		Bqtest(q, new int[] {0,1,2,3});
	  }

	  public virtual void TestSNot8()
	  {
		// NOTE: using qtest not bqtest
		SpanQuery f = Snear("w1","w3",10,true);
		f.Boost = 0;
		SpanQuery q = Snot(f, St("xx"));
		Qtest(q, new int[] {0,1,3});
	  }
	  public virtual void TestSNot9()
	  {
		// NOTE: using qtest not bqtest
		SpanQuery t = St("xx");
		t.Boost = 0;
		SpanQuery q = Snot(Snear("w1","w3",10,true), t);
		Qtest(q, new int[] {0,1,3});
	  }





	}

}