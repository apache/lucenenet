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

	/// <summary>
	/// TestExplanations subclass focusing on basic query types
	/// </summary>
	public class TestSimpleExplanations : TestExplanations
	{

	  // we focus on queries that don't rewrite to other queries.
	  // if we get those covered well, then the ones that rewrite should
	  // also be covered.


	  /* simple term tests */

	  public virtual void TestT1()
	  {
		Qtest(new TermQuery(new Term(FIELD, "w1")), new int[] {0,1,2,3});
	  }
	  public virtual void TestT2()
	  {
		TermQuery termQuery = new TermQuery(new Term(FIELD, "w1"));
		termQuery.Boost = 100;
		Qtest(termQuery, new int[] {0,1,2,3});
	  }

	  /* MatchAllDocs */

	  public virtual void TestMA1()
	  {
		Qtest(new MatchAllDocsQuery(), new int[] {0,1,2,3});
	  }
	  public virtual void TestMA2()
	  {
		Query q = new MatchAllDocsQuery();
		q.Boost = 1000;
		Qtest(q, new int[] {0,1,2,3});
	  }

	  /* some simple phrase tests */

	  public virtual void TestP1()
	  {
		PhraseQuery phraseQuery = new PhraseQuery();
		phraseQuery.add(new Term(FIELD, "w1"));
		phraseQuery.add(new Term(FIELD, "w2"));
		Qtest(phraseQuery, new int[] {0});
	  }
	  public virtual void TestP2()
	  {
		PhraseQuery phraseQuery = new PhraseQuery();
		phraseQuery.add(new Term(FIELD, "w1"));
		phraseQuery.add(new Term(FIELD, "w3"));
		Qtest(phraseQuery, new int[] {1,3});
	  }
	  public virtual void TestP3()
	  {
		PhraseQuery phraseQuery = new PhraseQuery();
		phraseQuery.Slop = 1;
		phraseQuery.add(new Term(FIELD, "w1"));
		phraseQuery.add(new Term(FIELD, "w2"));
		Qtest(phraseQuery, new int[] {0,1,2});
	  }
	  public virtual void TestP4()
	  {
		PhraseQuery phraseQuery = new PhraseQuery();
		phraseQuery.Slop = 1;
		phraseQuery.add(new Term(FIELD, "w2"));
		phraseQuery.add(new Term(FIELD, "w3"));
		Qtest(phraseQuery, new int[] {0,1,2,3});
	  }
	  public virtual void TestP5()
	  {
		PhraseQuery phraseQuery = new PhraseQuery();
		phraseQuery.Slop = 1;
		phraseQuery.add(new Term(FIELD, "w3"));
		phraseQuery.add(new Term(FIELD, "w2"));
		Qtest(phraseQuery, new int[] {1,3});
	  }
	  public virtual void TestP6()
	  {
		PhraseQuery phraseQuery = new PhraseQuery();
		phraseQuery.Slop = 2;
		phraseQuery.add(new Term(FIELD, "w3"));
		phraseQuery.add(new Term(FIELD, "w2"));
		Qtest(phraseQuery, new int[] {0,1,3});
	  }
	  public virtual void TestP7()
	  {
		PhraseQuery phraseQuery = new PhraseQuery();
		phraseQuery.Slop = 3;
		phraseQuery.add(new Term(FIELD, "w3"));
		phraseQuery.add(new Term(FIELD, "w2"));
		Qtest(phraseQuery, new int[] {0,1,2,3});
	  }

	  /* some simple filtered query tests */

	  public virtual void TestFQ1()
	  {
		Qtest(new FilteredQuery(new TermQuery(new Term(FIELD, "w1")), new ItemizedFilter(new int[] {0,1,2,3})), new int[] {0,1,2,3});
	  }
	  public virtual void TestFQ2()
	  {
		Qtest(new FilteredQuery(new TermQuery(new Term(FIELD, "w1")), new ItemizedFilter(new int[] {0,2,3})), new int[] {0,2,3});
	  }
	  public virtual void TestFQ3()
	  {
		Qtest(new FilteredQuery(new TermQuery(new Term(FIELD, "xx")), new ItemizedFilter(new int[] {1,3})), new int[] {3});
	  }
	  public virtual void TestFQ4()
	  {
		TermQuery termQuery = new TermQuery(new Term(FIELD, "xx"));
		termQuery.Boost = 1000;
		Qtest(new FilteredQuery(termQuery, new ItemizedFilter(new int[] {1,3})), new int[] {3});
	  }
	  public virtual void TestFQ6()
	  {
		Query q = new FilteredQuery(new TermQuery(new Term(FIELD, "xx")), new ItemizedFilter(new int[] {1,3}));
		q.Boost = 1000;
		Qtest(q, new int[] {3});
	  }

	  /* ConstantScoreQueries */

	  public virtual void TestCSQ1()
	  {
		Query q = new ConstantScoreQuery(new ItemizedFilter(new int[] {0,1,2,3}));
		Qtest(q, new int[] {0,1,2,3});
	  }
	  public virtual void TestCSQ2()
	  {
		Query q = new ConstantScoreQuery(new ItemizedFilter(new int[] {1,3}));
		Qtest(q, new int[] {1,3});
	  }
	  public virtual void TestCSQ3()
	  {
		Query q = new ConstantScoreQuery(new ItemizedFilter(new int[] {0,2}));
		q.Boost = 1000;
		Qtest(q, new int[] {0,2});
	  }

	  /* DisjunctionMaxQuery */

	  public virtual void TestDMQ1()
	  {
		DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.0f);
		q.add(new TermQuery(new Term(FIELD, "w1")));
		q.add(new TermQuery(new Term(FIELD, "w5")));
		Qtest(q, new int[] {0,1,2,3});
	  }
	  public virtual void TestDMQ2()
	  {
		DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
		q.add(new TermQuery(new Term(FIELD, "w1")));
		q.add(new TermQuery(new Term(FIELD, "w5")));
		Qtest(q, new int[] {0,1,2,3});
	  }
	  public virtual void TestDMQ3()
	  {
		DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
		q.add(new TermQuery(new Term(FIELD, "QQ")));
		q.add(new TermQuery(new Term(FIELD, "w5")));
		Qtest(q, new int[] {0});
	  }
	  public virtual void TestDMQ4()
	  {
		DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
		q.add(new TermQuery(new Term(FIELD, "QQ")));
		q.add(new TermQuery(new Term(FIELD, "xx")));
		Qtest(q, new int[] {2,3});
	  }
	  public virtual void TestDMQ5()
	  {
		DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);

		BooleanQuery booleanQuery = new BooleanQuery();
		booleanQuery.add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.SHOULD);
		booleanQuery.add(new TermQuery(new Term(FIELD, "QQ")), BooleanClause.Occur.MUST_NOT);

		q.add(booleanQuery);
		q.add(new TermQuery(new Term(FIELD, "xx")));
		Qtest(q, new int[] {2,3});
	  }
	  public virtual void TestDMQ6()
	  {
		DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);

		BooleanQuery booleanQuery = new BooleanQuery();
		booleanQuery.add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.MUST_NOT);
		booleanQuery.add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.SHOULD);

		q.add(booleanQuery);
		q.add(new TermQuery(new Term(FIELD, "xx")));
		Qtest(q, new int[] {0,1,2,3});
	  }
	  public virtual void TestDMQ7()
	  {
		DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);

		BooleanQuery booleanQuery = new BooleanQuery();
		booleanQuery.add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.MUST_NOT);
		booleanQuery.add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.SHOULD);

		q.add(booleanQuery);
		q.add(new TermQuery(new Term(FIELD, "w2")));
		Qtest(q, new int[] {0,1,2,3});
	  }
	  public virtual void TestDMQ8()
	  {
		DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);

		BooleanQuery booleanQuery = new BooleanQuery();
		booleanQuery.add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.SHOULD);

		TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w5"));
		boostedQuery.Boost = 100;
		booleanQuery.add(boostedQuery, BooleanClause.Occur.SHOULD);
		q.add(booleanQuery);

		TermQuery xxBoostedQuery = new TermQuery(new Term(FIELD, "xx"));
		xxBoostedQuery.Boost = 100000;
		q.add(xxBoostedQuery);

		Qtest(q, new int[] {0,2,3});
	  }
	  public virtual void TestDMQ9()
	  {
		DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);

		BooleanQuery booleanQuery = new BooleanQuery();
		booleanQuery.add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.SHOULD);

		TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w5"));
		boostedQuery.Boost = 100;
		booleanQuery.add(boostedQuery, BooleanClause.Occur.SHOULD);
		q.add(booleanQuery);

		TermQuery xxBoostedQuery = new TermQuery(new Term(FIELD, "xx"));
		xxBoostedQuery.Boost = 0;
		q.add(xxBoostedQuery);

		Qtest(q, new int[] {0,2,3});
	  }

	  /* MultiPhraseQuery */

	  public virtual void TestMPQ1()
	  {
		MultiPhraseQuery q = new MultiPhraseQuery();
		q.add(Ta(new string[] {"w1"}));
		q.add(Ta(new string[] {"w2","w3", "xx"}));
		Qtest(q, new int[] {0,1,2,3});
	  }
	  public virtual void TestMPQ2()
	  {
		MultiPhraseQuery q = new MultiPhraseQuery();
		q.add(Ta(new string[] {"w1"}));
		q.add(Ta(new string[] {"w2","w3"}));
		Qtest(q, new int[] {0,1,3});
	  }
	  public virtual void TestMPQ3()
	  {
		MultiPhraseQuery q = new MultiPhraseQuery();
		q.add(Ta(new string[] {"w1","xx"}));
		q.add(Ta(new string[] {"w2","w3"}));
		Qtest(q, new int[] {0,1,2,3});
	  }
	  public virtual void TestMPQ4()
	  {
		MultiPhraseQuery q = new MultiPhraseQuery();
		q.add(Ta(new string[] {"w1"}));
		q.add(Ta(new string[] {"w2"}));
		Qtest(q, new int[] {0});
	  }
	  public virtual void TestMPQ5()
	  {
		MultiPhraseQuery q = new MultiPhraseQuery();
		q.add(Ta(new string[] {"w1"}));
		q.add(Ta(new string[] {"w2"}));
		q.Slop = 1;
		Qtest(q, new int[] {0,1,2});
	  }
	  public virtual void TestMPQ6()
	  {
		MultiPhraseQuery q = new MultiPhraseQuery();
		q.add(Ta(new string[] {"w1","w3"}));
		q.add(Ta(new string[] {"w2"}));
		q.Slop = 1;
		Qtest(q, new int[] {0,1,2,3});
	  }

	  /* some simple tests of boolean queries containing term queries */

	  public virtual void TestBQ1()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);
		query.add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.MUST);
		Qtest(query, new int[] {0,1,2,3});
	  }
	  public virtual void TestBQ2()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.MUST);
		query.add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.MUST);
		Qtest(query, new int[] {2,3});
	  }
	  public virtual void TestBQ3()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.SHOULD);
		query.add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.MUST);
		Qtest(query, new int[] {0,1,2,3});
	  }
	  public virtual void TestBQ4()
	  {
		BooleanQuery outerQuery = new BooleanQuery();
		outerQuery.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);

		BooleanQuery innerQuery = new BooleanQuery();
		innerQuery.add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT);
		innerQuery.add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.SHOULD);
		outerQuery.add(innerQuery, BooleanClause.Occur.SHOULD);

		Qtest(outerQuery, new int[] {0,1,2,3});
	  }
	  public virtual void TestBQ5()
	  {
		BooleanQuery outerQuery = new BooleanQuery();
		outerQuery.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);

		BooleanQuery innerQuery = new BooleanQuery();
		innerQuery.add(new TermQuery(new Term(FIELD, "qq")), BooleanClause.Occur.MUST);
		innerQuery.add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.SHOULD);
		outerQuery.add(innerQuery, BooleanClause.Occur.SHOULD);

		Qtest(outerQuery, new int[] {0,1,2,3});
	  }
	  public virtual void TestBQ6()
	  {
		BooleanQuery outerQuery = new BooleanQuery();
		outerQuery.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);

		BooleanQuery innerQuery = new BooleanQuery();
		innerQuery.add(new TermQuery(new Term(FIELD, "qq")), BooleanClause.Occur.MUST_NOT);
		innerQuery.add(new TermQuery(new Term(FIELD, "w5")), BooleanClause.Occur.SHOULD);
		outerQuery.add(innerQuery, BooleanClause.Occur.MUST_NOT);

		Qtest(outerQuery, new int[] {1,2,3});
	  }
	  public virtual void TestBQ7()
	  {
		BooleanQuery outerQuery = new BooleanQuery();
		outerQuery.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);

		BooleanQuery innerQuery = new BooleanQuery();
		innerQuery.add(new TermQuery(new Term(FIELD, "qq")), BooleanClause.Occur.SHOULD);

		BooleanQuery childLeft = new BooleanQuery();
		childLeft.add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.SHOULD);
		childLeft.add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.MUST_NOT);
		innerQuery.add(childLeft, BooleanClause.Occur.SHOULD);

		BooleanQuery childRight = new BooleanQuery();
		childRight.add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.MUST);
		childRight.add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.MUST);
		innerQuery.add(childRight, BooleanClause.Occur.SHOULD);

		outerQuery.add(innerQuery, BooleanClause.Occur.MUST);

		Qtest(outerQuery, new int[] {0});
	  }
	  public virtual void TestBQ8()
	  {
		BooleanQuery outerQuery = new BooleanQuery();
		outerQuery.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);

		BooleanQuery innerQuery = new BooleanQuery();
		innerQuery.add(new TermQuery(new Term(FIELD, "qq")), BooleanClause.Occur.SHOULD);

		BooleanQuery childLeft = new BooleanQuery();
		childLeft.add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.SHOULD);
		childLeft.add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.MUST_NOT);
		innerQuery.add(childLeft, BooleanClause.Occur.SHOULD);

		BooleanQuery childRight = new BooleanQuery();
		childRight.add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.MUST);
		childRight.add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.MUST);
		innerQuery.add(childRight, BooleanClause.Occur.SHOULD);

		outerQuery.add(innerQuery, BooleanClause.Occur.SHOULD);

		Qtest(outerQuery, new int[] {0,1,2,3});
	  }
	  public virtual void TestBQ9()
	  {
		BooleanQuery outerQuery = new BooleanQuery();
		outerQuery.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);

		BooleanQuery innerQuery = new BooleanQuery();
		innerQuery.add(new TermQuery(new Term(FIELD, "qq")), BooleanClause.Occur.SHOULD);

		BooleanQuery childLeft = new BooleanQuery();
		childLeft.add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT);
		childLeft.add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.SHOULD);
		innerQuery.add(childLeft, BooleanClause.Occur.SHOULD);

		BooleanQuery childRight = new BooleanQuery();
		childRight.add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.MUST);
		childRight.add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.MUST);
		innerQuery.add(childRight, BooleanClause.Occur.MUST_NOT);

		outerQuery.add(innerQuery, BooleanClause.Occur.SHOULD);

		Qtest(outerQuery, new int[] {0,1,2,3});
	  }
	  public virtual void TestBQ10()
	  {
		BooleanQuery outerQuery = new BooleanQuery();
		outerQuery.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);

		BooleanQuery innerQuery = new BooleanQuery();
		innerQuery.add(new TermQuery(new Term(FIELD, "qq")), BooleanClause.Occur.SHOULD);

		BooleanQuery childLeft = new BooleanQuery();
		childLeft.add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT);
		childLeft.add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.SHOULD);
		innerQuery.add(childLeft, BooleanClause.Occur.SHOULD);

		BooleanQuery childRight = new BooleanQuery();
		childRight.add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.MUST);
		childRight.add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.MUST);
		innerQuery.add(childRight, BooleanClause.Occur.MUST_NOT);

		outerQuery.add(innerQuery, BooleanClause.Occur.MUST);

		Qtest(outerQuery, new int[] {1});
	  }
	  public virtual void TestBQ11()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
		TermQuery boostedQuery = new TermQuery(new Term(FIELD, "w1"));
		boostedQuery.Boost = 1000;
		query.add(boostedQuery, BooleanClause.Occur.SHOULD);

		Qtest(query, new int[] {0,1,2,3});
	  }
	  public virtual void TestBQ14()
	  {
		BooleanQuery q = new BooleanQuery(true);
		q.add(new TermQuery(new Term(FIELD, "QQQQQ")), BooleanClause.Occur.SHOULD);
		q.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
		Qtest(q, new int[] {0,1,2,3});
	  }
	  public virtual void TestBQ15()
	  {
		BooleanQuery q = new BooleanQuery(true);
		q.add(new TermQuery(new Term(FIELD, "QQQQQ")), BooleanClause.Occur.MUST_NOT);
		q.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
		Qtest(q, new int[] {0,1,2,3});
	  }
	  public virtual void TestBQ16()
	  {
		BooleanQuery q = new BooleanQuery(true);
		q.add(new TermQuery(new Term(FIELD, "QQQQQ")), BooleanClause.Occur.SHOULD);

		BooleanQuery booleanQuery = new BooleanQuery();
		booleanQuery.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
		booleanQuery.add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT);

		q.add(booleanQuery, BooleanClause.Occur.SHOULD);
		Qtest(q, new int[] {0,1});
	  }
	  public virtual void TestBQ17()
	  {
		BooleanQuery q = new BooleanQuery(true);
		q.add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.SHOULD);

		BooleanQuery booleanQuery = new BooleanQuery();
		booleanQuery.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);
		booleanQuery.add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT);

		q.add(booleanQuery, BooleanClause.Occur.SHOULD);
		Qtest(q, new int[] {0,1,2,3});
	  }
	  public virtual void TestBQ19()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.MUST_NOT);
		query.add(new TermQuery(new Term(FIELD, "w3")), BooleanClause.Occur.SHOULD);

		Qtest(query, new int[] {0,1});
	  }

	  public virtual void TestBQ20()
	  {
		BooleanQuery q = new BooleanQuery();
		q.MinimumNumberShouldMatch = 2;
		q.add(new TermQuery(new Term(FIELD, "QQQQQ")), BooleanClause.Occur.SHOULD);
		q.add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.SHOULD);
		q.add(new TermQuery(new Term(FIELD, "zz")), BooleanClause.Occur.SHOULD);
		q.add(new TermQuery(new Term(FIELD, "w5")), BooleanClause.Occur.SHOULD);
		q.add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.SHOULD);

		Qtest(q, new int[] {0,3});

	  }

	  /* BQ of TQ: using alt so some fields have zero boost and some don't */

	  public virtual void TestMultiFieldBQ1()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);
		query.add(new TermQuery(new Term(ALTFIELD, "w2")), BooleanClause.Occur.MUST);

		Qtest(query, new int[] {0,1,2,3});
	  }
	  public virtual void TestMultiFieldBQ2()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.MUST);
		query.add(new TermQuery(new Term(ALTFIELD, "w3")), BooleanClause.Occur.MUST);

		Qtest(query, new int[] {2,3});
	  }
	  public virtual void TestMultiFieldBQ3()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(FIELD, "yy")), BooleanClause.Occur.SHOULD);
		query.add(new TermQuery(new Term(ALTFIELD, "w3")), BooleanClause.Occur.MUST);

		Qtest(query, new int[] {0,1,2,3});
	  }
	  public virtual void TestMultiFieldBQ4()
	  {
		BooleanQuery outerQuery = new BooleanQuery();
		outerQuery.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);

		BooleanQuery innerQuery = new BooleanQuery();
		innerQuery.add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT);
		innerQuery.add(new TermQuery(new Term(ALTFIELD, "w2")), BooleanClause.Occur.SHOULD);
		outerQuery.add(innerQuery, BooleanClause.Occur.SHOULD);

		Qtest(outerQuery, new int[] {0,1,2,3});
	  }
	  public virtual void TestMultiFieldBQ5()
	  {
		BooleanQuery outerQuery = new BooleanQuery();
		outerQuery.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);

		BooleanQuery innerQuery = new BooleanQuery();
		innerQuery.add(new TermQuery(new Term(ALTFIELD, "qq")), BooleanClause.Occur.MUST);
		innerQuery.add(new TermQuery(new Term(ALTFIELD, "w2")), BooleanClause.Occur.SHOULD);
		outerQuery.add(innerQuery, BooleanClause.Occur.SHOULD);

		Qtest(outerQuery, new int[] {0,1,2,3});
	  }
	  public virtual void TestMultiFieldBQ6()
	  {
		BooleanQuery outerQuery = new BooleanQuery();
		outerQuery.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.SHOULD);

		BooleanQuery innerQuery = new BooleanQuery();
		innerQuery.add(new TermQuery(new Term(ALTFIELD, "qq")), BooleanClause.Occur.MUST_NOT);
		innerQuery.add(new TermQuery(new Term(ALTFIELD, "w5")), BooleanClause.Occur.SHOULD);
		outerQuery.add(innerQuery, BooleanClause.Occur.MUST_NOT);

		Qtest(outerQuery, new int[] {1,2,3});
	  }
	  public virtual void TestMultiFieldBQ7()
	  {
		BooleanQuery outerQuery = new BooleanQuery();
		outerQuery.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);

		BooleanQuery innerQuery = new BooleanQuery();
		innerQuery.add(new TermQuery(new Term(ALTFIELD, "qq")), BooleanClause.Occur.SHOULD);

		BooleanQuery childLeft = new BooleanQuery();
		childLeft.add(new TermQuery(new Term(ALTFIELD, "xx")), BooleanClause.Occur.SHOULD);
		childLeft.add(new TermQuery(new Term(ALTFIELD, "w2")), BooleanClause.Occur.MUST_NOT);
		innerQuery.add(childLeft, BooleanClause.Occur.SHOULD);

		BooleanQuery childRight = new BooleanQuery();
		childRight.add(new TermQuery(new Term(ALTFIELD, "w3")), BooleanClause.Occur.MUST);
		childRight.add(new TermQuery(new Term(ALTFIELD, "w4")), BooleanClause.Occur.MUST);
		innerQuery.add(childRight, BooleanClause.Occur.SHOULD);

		outerQuery.add(innerQuery, BooleanClause.Occur.MUST);

		Qtest(outerQuery, new int[] {0});
	  }
	  public virtual void TestMultiFieldBQ8()
	  {
		BooleanQuery outerQuery = new BooleanQuery();
		outerQuery.add(new TermQuery(new Term(ALTFIELD, "w1")), BooleanClause.Occur.MUST);

		BooleanQuery innerQuery = new BooleanQuery();
		innerQuery.add(new TermQuery(new Term(FIELD, "qq")), BooleanClause.Occur.SHOULD);

		BooleanQuery childLeft = new BooleanQuery();
		childLeft.add(new TermQuery(new Term(ALTFIELD, "xx")), BooleanClause.Occur.SHOULD);
		childLeft.add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.MUST_NOT);
		innerQuery.add(childLeft, BooleanClause.Occur.SHOULD);

		BooleanQuery childRight = new BooleanQuery();
		childRight.add(new TermQuery(new Term(ALTFIELD, "w3")), BooleanClause.Occur.MUST);
		childRight.add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.MUST);
		innerQuery.add(childRight, BooleanClause.Occur.SHOULD);

		outerQuery.add(innerQuery, BooleanClause.Occur.SHOULD);

		Qtest(outerQuery, new int[] {0,1,2,3});
	  }
	  public virtual void TestMultiFieldBQ9()
	  {
		BooleanQuery outerQuery = new BooleanQuery();
		outerQuery.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);

		BooleanQuery innerQuery = new BooleanQuery();
		innerQuery.add(new TermQuery(new Term(ALTFIELD, "qq")), BooleanClause.Occur.SHOULD);

		BooleanQuery childLeft = new BooleanQuery();
		childLeft.add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT);
		childLeft.add(new TermQuery(new Term(FIELD, "w2")), BooleanClause.Occur.SHOULD);
		innerQuery.add(childLeft, BooleanClause.Occur.SHOULD);

		BooleanQuery childRight = new BooleanQuery();
		childRight.add(new TermQuery(new Term(ALTFIELD, "w3")), BooleanClause.Occur.MUST);
		childRight.add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.MUST);
		innerQuery.add(childRight, BooleanClause.Occur.MUST_NOT);

		outerQuery.add(innerQuery, BooleanClause.Occur.SHOULD);

		Qtest(outerQuery, new int[] {0,1,2,3});
	  }
	  public virtual void TestMultiFieldBQ10()
	  {
		BooleanQuery outerQuery = new BooleanQuery();
		outerQuery.add(new TermQuery(new Term(FIELD, "w1")), BooleanClause.Occur.MUST);

		BooleanQuery innerQuery = new BooleanQuery();
		innerQuery.add(new TermQuery(new Term(ALTFIELD, "qq")), BooleanClause.Occur.SHOULD);

		BooleanQuery childLeft = new BooleanQuery();
		childLeft.add(new TermQuery(new Term(FIELD, "xx")), BooleanClause.Occur.MUST_NOT);
		childLeft.add(new TermQuery(new Term(ALTFIELD, "w2")), BooleanClause.Occur.SHOULD);
		innerQuery.add(childLeft, BooleanClause.Occur.SHOULD);

		BooleanQuery childRight = new BooleanQuery();
		childRight.add(new TermQuery(new Term(ALTFIELD, "w3")), BooleanClause.Occur.MUST);
		childRight.add(new TermQuery(new Term(FIELD, "w4")), BooleanClause.Occur.MUST);
		innerQuery.add(childRight, BooleanClause.Occur.MUST_NOT);

		outerQuery.add(innerQuery, BooleanClause.Occur.MUST);

		Qtest(outerQuery, new int[] {1});
	  }

	  /* BQ of PQ: using alt so some fields have zero boost and some don't */

	  public virtual void TestMultiFieldBQofPQ1()
	  {
		BooleanQuery query = new BooleanQuery();

		PhraseQuery leftChild = new PhraseQuery();
		leftChild.add(new Term(FIELD, "w1"));
		leftChild.add(new Term(FIELD, "w2"));
		query.add(leftChild, BooleanClause.Occur.SHOULD);

		PhraseQuery rightChild = new PhraseQuery();
		rightChild.add(new Term(ALTFIELD, "w1"));
		rightChild.add(new Term(ALTFIELD, "w2"));
		query.add(rightChild, BooleanClause.Occur.SHOULD);

		Qtest(query, new int[] {0});
	  }
	  public virtual void TestMultiFieldBQofPQ2()
	  {
		BooleanQuery query = new BooleanQuery();

		PhraseQuery leftChild = new PhraseQuery();
		leftChild.add(new Term(FIELD, "w1"));
		leftChild.add(new Term(FIELD, "w3"));
		query.add(leftChild, BooleanClause.Occur.SHOULD);

		PhraseQuery rightChild = new PhraseQuery();
		rightChild.add(new Term(ALTFIELD, "w1"));
		rightChild.add(new Term(ALTFIELD, "w3"));
		query.add(rightChild, BooleanClause.Occur.SHOULD);

		Qtest(query, new int[] {1,3});
	  }
	  public virtual void TestMultiFieldBQofPQ3()
	  {
		BooleanQuery query = new BooleanQuery();

		PhraseQuery leftChild = new PhraseQuery();
		leftChild.Slop = 1;
		leftChild.add(new Term(FIELD, "w1"));
		leftChild.add(new Term(FIELD, "w2"));
		query.add(leftChild, BooleanClause.Occur.SHOULD);

		PhraseQuery rightChild = new PhraseQuery();
		rightChild.Slop = 1;
		rightChild.add(new Term(ALTFIELD, "w1"));
		rightChild.add(new Term(ALTFIELD, "w2"));
		query.add(rightChild, BooleanClause.Occur.SHOULD);

		Qtest(query, new int[] {0,1,2});
	  }
	  public virtual void TestMultiFieldBQofPQ4()
	  {
		BooleanQuery query = new BooleanQuery();

		PhraseQuery leftChild = new PhraseQuery();
		leftChild.Slop = 1;
		leftChild.add(new Term(FIELD, "w2"));
		leftChild.add(new Term(FIELD, "w3"));
		query.add(leftChild, BooleanClause.Occur.SHOULD);

		PhraseQuery rightChild = new PhraseQuery();
		rightChild.Slop = 1;
		rightChild.add(new Term(ALTFIELD, "w2"));
		rightChild.add(new Term(ALTFIELD, "w3"));
		query.add(rightChild, BooleanClause.Occur.SHOULD);

		Qtest(query, new int[] {0,1,2,3});
	  }
	  public virtual void TestMultiFieldBQofPQ5()
	  {
		BooleanQuery query = new BooleanQuery();

		PhraseQuery leftChild = new PhraseQuery();
		leftChild.Slop = 1;
		leftChild.add(new Term(FIELD, "w3"));
		leftChild.add(new Term(FIELD, "w2"));
		query.add(leftChild, BooleanClause.Occur.SHOULD);

		PhraseQuery rightChild = new PhraseQuery();
		rightChild.Slop = 1;
		rightChild.add(new Term(ALTFIELD, "w3"));
		rightChild.add(new Term(ALTFIELD, "w2"));
		query.add(rightChild, BooleanClause.Occur.SHOULD);

		Qtest(query, new int[] {1,3});
	  }
	  public virtual void TestMultiFieldBQofPQ6()
	  {
		BooleanQuery query = new BooleanQuery();

		PhraseQuery leftChild = new PhraseQuery();
		leftChild.Slop = 2;
		leftChild.add(new Term(FIELD, "w3"));
		leftChild.add(new Term(FIELD, "w2"));
		query.add(leftChild, BooleanClause.Occur.SHOULD);

		PhraseQuery rightChild = new PhraseQuery();
		rightChild.Slop = 2;
		rightChild.add(new Term(ALTFIELD, "w3"));
		rightChild.add(new Term(ALTFIELD, "w2"));
		query.add(rightChild, BooleanClause.Occur.SHOULD);

		Qtest(query, new int[] {0,1,3});
	  }
	  public virtual void TestMultiFieldBQofPQ7()
	  {
		BooleanQuery query = new BooleanQuery();

		PhraseQuery leftChild = new PhraseQuery();
		leftChild.Slop = 3;
		leftChild.add(new Term(FIELD, "w3"));
		leftChild.add(new Term(FIELD, "w2"));
		query.add(leftChild, BooleanClause.Occur.SHOULD);

		PhraseQuery rightChild = new PhraseQuery();
		rightChild.Slop = 1;
		rightChild.add(new Term(ALTFIELD, "w3"));
		rightChild.add(new Term(ALTFIELD, "w2"));
		query.add(rightChild, BooleanClause.Occur.SHOULD);

		Qtest(query, new int[] {0,1,2,3});
	  }

	}

}