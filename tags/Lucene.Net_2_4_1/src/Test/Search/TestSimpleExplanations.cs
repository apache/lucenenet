/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

namespace Lucene.Net.Search
{
	
	/// <summary> TestExplanations subclass focusing on basic query types</summary>
	[TestFixture]
	public class TestSimpleExplanations : TestExplanations
	{
		
		// we focus on queries that don't rewrite to other queries.
		// if we get those covered well, then the ones that rewrite should
		// also be covered.
		
		
		/* simple term tests */

		[Test]
		public virtual void  TestT1()
		{
			Qtest("w1", new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestT2()
		{
			Qtest("w1^1000", new int[]{0, 1, 2, 3});
		}
		
		/* MatchAllDocs */
		
		[Test]
		public virtual void  TestMA1()
		{
			Qtest(new MatchAllDocsQuery(), new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestMA2()
		{
			Query q = new MatchAllDocsQuery();
			q.SetBoost(1000);
			Qtest(q, new int[]{0, 1, 2, 3});
		}
		
		/* some simple phrase tests */
		
		[Test]
		public virtual void  TestP1()
		{
			Qtest("\"w1 w2\"", new int[]{0});
		}

		[Test]
		public virtual void  TestP2()
		{
			Qtest("\"w1 w3\"", new int[]{1, 3});
		}

		[Test]
		public virtual void  TestP3()
		{
			Qtest("\"w1 w2\"~1", new int[]{0, 1, 2});
		}

		[Test]
		public virtual void  TestP4()
		{
			Qtest("\"w2 w3\"~1", new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestP5()
		{
			Qtest("\"w3 w2\"~1", new int[]{1, 3});
		}

		[Test]
		public virtual void  TestP6()
		{
			Qtest("\"w3 w2\"~2", new int[]{0, 1, 3});
		}

		[Test]
		public virtual void  TestP7()
		{
			Qtest("\"w3 w2\"~3", new int[]{0, 1, 2, 3});
		}
		
		/* some simple filtered query tests */
		
		[Test]
		public virtual void  TestFQ1()
		{
			Qtest(new FilteredQuery(qp.Parse("w1"), new ItemizedFilter(new int[]{0, 1, 2, 3})), new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestFQ2()
		{
			Qtest(new FilteredQuery(qp.Parse("w1"), new ItemizedFilter(new int[]{0, 2, 3})), new int[]{0, 2, 3});
		}

		[Test]
		public virtual void  TestFQ3()
		{
			Qtest(new FilteredQuery(qp.Parse("xx"), new ItemizedFilter(new int[]{1, 3})), new int[]{3});
		}

		[Test]
		public virtual void  TestFQ4()
		{
			Qtest(new FilteredQuery(qp.Parse("xx^1000"), new ItemizedFilter(new int[]{1, 3})), new int[]{3});
		}

		[Test]
		public virtual void  TestFQ6()
		{
			Query q = new FilteredQuery(qp.Parse("xx"), new ItemizedFilter(new int[]{1, 3}));
			q.SetBoost(1000);
			Qtest(q, new int[]{3});
		}

		[Test]
		public virtual void  TestFQ7()
		{
			Query q = new FilteredQuery(qp.Parse("xx"), new ItemizedFilter(new int[]{1, 3}));
			q.SetBoost(0);
			Qtest(q, new int[]{3});
		}
		
		/* ConstantScoreQueries */
		
		[Test]
		public virtual void  TestCSQ1()
		{
			Query q = new ConstantScoreQuery(new ItemizedFilter(new int[]{0, 1, 2, 3}));
			Qtest(q, new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestCSQ2()
		{
			Query q = new ConstantScoreQuery(new ItemizedFilter(new int[]{1, 3}));
			Qtest(q, new int[]{1, 3});
		}

		[Test]
		public virtual void  TestCSQ3()
		{
			Query q = new ConstantScoreQuery(new ItemizedFilter(new int[]{0, 2}));
			q.SetBoost(1000);
			Qtest(q, new int[]{0, 2});
		}
		
		/* DisjunctionMaxQuery */
		
		[Test]
		public virtual void  TestDMQ1()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.0f);
			q.Add(qp.Parse("w1"));
			q.Add(qp.Parse("w5"));
			Qtest(q, new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestDMQ2()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			q.Add(qp.Parse("w1"));
			q.Add(qp.Parse("w5"));
			Qtest(q, new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestDMQ3()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			q.Add(qp.Parse("QQ"));
			q.Add(qp.Parse("w5"));
			Qtest(q, new int[]{0});
		}

		[Test]
		public virtual void  TestDMQ4()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			q.Add(qp.Parse("QQ"));
			q.Add(qp.Parse("xx"));
			Qtest(q, new int[]{2, 3});
		}

		[Test]
		public virtual void  TestDMQ5()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			q.Add(qp.Parse("yy -QQ"));
			q.Add(qp.Parse("xx"));
			Qtest(q, new int[]{2, 3});
		}

		[Test]
		public virtual void  TestDMQ6()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			q.Add(qp.Parse("-yy w3"));
			q.Add(qp.Parse("xx"));
			Qtest(q, new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestDMQ7()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			q.Add(qp.Parse("-yy w3"));
			q.Add(qp.Parse("w2"));
			Qtest(q, new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestDMQ8()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			q.Add(qp.Parse("yy w5^100"));
			q.Add(qp.Parse("xx^100000"));
			Qtest(q, new int[]{0, 2, 3});
		}

		[Test]
		public virtual void  TestDMQ9()
		{
			DisjunctionMaxQuery q = new DisjunctionMaxQuery(0.5f);
			q.Add(qp.Parse("yy w5^100"));
			q.Add(qp.Parse("xx^0"));
			Qtest(q, new int[]{0, 2, 3});
		}
		
		/* MultiPhraseQuery */
		
		[Test]
		public virtual void  TestMPQ1()
		{
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(Ta(new System.String[]{"w1"}));
			q.Add(Ta(new System.String[]{"w2", "w3", "xx"}));
			Qtest(q, new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestMPQ2()
		{
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(Ta(new System.String[]{"w1"}));
			q.Add(Ta(new System.String[]{"w2", "w3"}));
			Qtest(q, new int[]{0, 1, 3});
		}

		[Test]
		public virtual void  TestMPQ3()
		{
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(Ta(new System.String[]{"w1", "xx"}));
			q.Add(Ta(new System.String[]{"w2", "w3"}));
			Qtest(q, new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestMPQ4()
		{
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(Ta(new System.String[]{"w1"}));
			q.Add(Ta(new System.String[]{"w2"}));
			Qtest(q, new int[]{0});
		}

		[Test]
		public virtual void  TestMPQ5()
		{
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(Ta(new System.String[]{"w1"}));
			q.Add(Ta(new System.String[]{"w2"}));
			q.SetSlop(1);
			Qtest(q, new int[]{0, 1, 2});
		}

		[Test]
		public virtual void  TestMPQ6()
		{
			MultiPhraseQuery q = new MultiPhraseQuery();
			q.Add(Ta(new System.String[]{"w1", "w3"}));
			q.Add(Ta(new System.String[]{"w2"}));
			q.SetSlop(1);
			Qtest(q, new int[]{0, 1, 2, 3});
		}
		
		/* some simple tests of bool queries containing term queries */
		
		[Test]
		public virtual void  TestBQ1()
		{
			Qtest("+w1 +w2", new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestBQ2()
		{
			Qtest("+yy +w3", new int[]{2, 3});
		}

		[Test]
		public virtual void  TestBQ3()
		{
			Qtest("yy +w3", new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestBQ4()
		{
			Qtest("w1 (-xx w2)", new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestBQ5()
		{
			Qtest("w1 (+qq w2)", new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestBQ6()
		{
			Qtest("w1 -(-qq w5)", new int[]{1, 2, 3});
		}

		[Test]
		public virtual void  TestBQ7()
		{
			Qtest("+w1 +(qq (xx -w2) (+w3 +w4))", new int[]{0});
		}

		[Test]
		public virtual void  TestBQ8()
		{
			Qtest("+w1 (qq (xx -w2) (+w3 +w4))", new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestBQ9()
		{
			Qtest("+w1 (qq (-xx w2) -(+w3 +w4))", new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestBQ10()
		{
			Qtest("+w1 +(qq (-xx w2) -(+w3 +w4))", new int[]{1});
		}

		[Test]
		public virtual void  TestBQ11()
		{
			Qtest("w1 w2^1000.0", new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestBQ14()
		{
			BooleanQuery q = new BooleanQuery(true);
			q.Add(qp.Parse("QQQQQ"), BooleanClause.Occur.SHOULD);
			q.Add(qp.Parse("w1"), BooleanClause.Occur.SHOULD);
			Qtest(q, new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestBQ15()
		{
			BooleanQuery q = new BooleanQuery(true);
			q.Add(qp.Parse("QQQQQ"), BooleanClause.Occur.MUST_NOT);
			q.Add(qp.Parse("w1"), BooleanClause.Occur.SHOULD);
			Qtest(q, new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestBQ16()
		{
			BooleanQuery q = new BooleanQuery(true);
			q.Add(qp.Parse("QQQQQ"), BooleanClause.Occur.SHOULD);
			q.Add(qp.Parse("w1 -xx"), BooleanClause.Occur.SHOULD);
			Qtest(q, new int[]{0, 1});
		}

		[Test]
		public virtual void  TestBQ17()
		{
			BooleanQuery q = new BooleanQuery(true);
			q.Add(qp.Parse("w2"), BooleanClause.Occur.SHOULD);
			q.Add(qp.Parse("w1 -xx"), BooleanClause.Occur.SHOULD);
			Qtest(q, new int[]{0, 1, 2, 3});
		}

		[Test]
		public virtual void  TestBQ19()
		{
			Qtest("-yy w3", new int[]{0, 1});
		}
		
		[Test]
		public virtual void  TestBQ20()
		{
			BooleanQuery q = new BooleanQuery();
			q.SetMinimumNumberShouldMatch(2);
			q.Add(qp.Parse("QQQQQ"), BooleanClause.Occur.SHOULD);
			q.Add(qp.Parse("yy"), BooleanClause.Occur.SHOULD);
			q.Add(qp.Parse("zz"), BooleanClause.Occur.SHOULD);
			q.Add(qp.Parse("w5"), BooleanClause.Occur.SHOULD);
			q.Add(qp.Parse("w4"), BooleanClause.Occur.SHOULD);
			
			Qtest(q, new int[]{0, 3});
		}
	}
}