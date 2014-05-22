using System;
using System.Threading;

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
	using MultiFields = Lucene.Net.Index.MultiFields;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using SingleTermsEnum = Lucene.Net.Index.SingleTermsEnum;
	using Term = Lucene.Net.Index.Term;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using Rethrow = Lucene.Net.Util.Rethrow;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using Automaton = Lucene.Net.Util.Automaton.Automaton;
	using AutomatonTestUtil = Lucene.Net.Util.Automaton.AutomatonTestUtil;
	using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;
	using BasicOperations = Lucene.Net.Util.Automaton.BasicOperations;

	public class TestAutomatonQuery : LuceneTestCase
	{
	  private Directory Directory;
	  private IndexReader Reader;
	  private IndexSearcher Searcher;

	  private readonly string FN = "field";

	  public override void SetUp()
	  {
		base.setUp();
		Directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory);
		Document doc = new Document();
		Field titleField = newTextField("title", "some title", Field.Store.NO);
		Field field = newTextField(FN, "this is document one 2345", Field.Store.NO);
		Field footerField = newTextField("footer", "a footer", Field.Store.NO);
		doc.add(titleField);
		doc.add(field);
		doc.add(footerField);
		writer.addDocument(doc);
		field.StringValue = "some text from doc two a short piece 5678.91";
		writer.addDocument(doc);
		field.StringValue = "doc three has some different stuff" + " with numbers 1234 5678.9 and letter b";
		writer.addDocument(doc);
		Reader = writer.Reader;
		Searcher = newSearcher(Reader);
		writer.close();
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Directory.close();
		base.tearDown();
	  }

	  private Term NewTerm(string value)
	  {
		return new Term(FN, value);
	  }

	  private int AutomatonQueryNrHits(AutomatonQuery query)
	  {
		if (VERBOSE)
		{
		  Console.WriteLine("TEST: run aq=" + query);
		}
		return Searcher.search(query, 5).totalHits;
	  }

	  private void AssertAutomatonHits(int expected, Automaton automaton)
	  {
		AutomatonQuery query = new AutomatonQuery(NewTerm("bogus"), automaton);

		query.RewriteMethod = MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE;
		Assert.AreEqual(expected, AutomatonQueryNrHits(query));

		query.RewriteMethod = MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE;
		Assert.AreEqual(expected, AutomatonQueryNrHits(query));

		query.RewriteMethod = MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE;
		Assert.AreEqual(expected, AutomatonQueryNrHits(query));

		query.RewriteMethod = MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT;
		Assert.AreEqual(expected, AutomatonQueryNrHits(query));
	  }

	  /// <summary>
	  /// Test some very simple automata.
	  /// </summary>
	  public virtual void TestBasicAutomata()
	  {
		AssertAutomatonHits(0, BasicAutomata.makeEmpty());
		AssertAutomatonHits(0, BasicAutomata.makeEmptyString());
		AssertAutomatonHits(2, BasicAutomata.makeAnyChar());
		AssertAutomatonHits(3, BasicAutomata.makeAnyString());
		AssertAutomatonHits(2, BasicAutomata.makeString("doc"));
		AssertAutomatonHits(1, BasicAutomata.makeChar('a'));
		AssertAutomatonHits(2, BasicAutomata.makeCharRange('a', 'b'));
		AssertAutomatonHits(2, BasicAutomata.makeInterval(1233, 2346, 0));
		AssertAutomatonHits(1, BasicAutomata.makeInterval(0, 2000, 0));
		AssertAutomatonHits(2, BasicOperations.union(BasicAutomata.makeChar('a'), BasicAutomata.makeChar('b')));
		AssertAutomatonHits(0, BasicOperations.intersection(BasicAutomata.makeChar('a'), BasicAutomata.makeChar('b')));
		AssertAutomatonHits(1, BasicOperations.minus(BasicAutomata.makeCharRange('a', 'b'), BasicAutomata.makeChar('a')));
	  }

	  /// <summary>
	  /// Test that a nondeterministic automaton works correctly. (It should will be
	  /// determinized)
	  /// </summary>
	  public virtual void TestNFA()
	  {
		// accept this or three, the union is an NFA (two transitions for 't' from
		// initial state)
		Automaton nfa = BasicOperations.union(BasicAutomata.makeString("this"), BasicAutomata.makeString("three"));
		AssertAutomatonHits(2, nfa);
	  }

	  public virtual void TestEquals()
	  {
		AutomatonQuery a1 = new AutomatonQuery(NewTerm("foobar"), BasicAutomata.makeString("foobar"));
		// reference to a1
		AutomatonQuery a2 = a1;
		// same as a1 (accepts the same language, same term)
		AutomatonQuery a3 = new AutomatonQuery(NewTerm("foobar"), BasicOperations.concatenate(BasicAutomata.makeString("foo"), BasicAutomata.makeString("bar")));
		// different than a1 (same term, but different language)
		AutomatonQuery a4 = new AutomatonQuery(NewTerm("foobar"), BasicAutomata.makeString("different"));
		// different than a1 (different term, same language)
		AutomatonQuery a5 = new AutomatonQuery(NewTerm("blah"), BasicAutomata.makeString("foobar"));

		Assert.AreEqual(a1.GetHashCode(), a2.GetHashCode());
		Assert.AreEqual(a1, a2);

		Assert.AreEqual(a1.GetHashCode(), a3.GetHashCode());
		Assert.AreEqual(a1, a3);

		// different class
		AutomatonQuery w1 = new WildcardQuery(NewTerm("foobar"));
		// different class
		AutomatonQuery w2 = new RegexpQuery(NewTerm("foobar"));

		Assert.IsFalse(a1.Equals(w1));
		Assert.IsFalse(a1.Equals(w2));
		Assert.IsFalse(w1.Equals(w2));
		Assert.IsFalse(a1.Equals(a4));
		Assert.IsFalse(a1.Equals(a5));
		Assert.IsFalse(a1.Equals(null));
	  }

	  /// <summary>
	  /// Test that rewriting to a single term works as expected, preserves
	  /// MultiTermQuery semantics.
	  /// </summary>
	  public virtual void TestRewriteSingleTerm()
	  {
		AutomatonQuery aq = new AutomatonQuery(NewTerm("bogus"), BasicAutomata.makeString("piece"));
		Terms terms = MultiFields.getTerms(Searcher.IndexReader, FN);
		Assert.IsTrue(aq.getTermsEnum(terms) is SingleTermsEnum);
		Assert.AreEqual(1, AutomatonQueryNrHits(aq));
	  }

	  /// <summary>
	  /// Test that rewriting to a prefix query works as expected, preserves
	  /// MultiTermQuery semantics.
	  /// </summary>
	  public virtual void TestRewritePrefix()
	  {
		Automaton pfx = BasicAutomata.makeString("do");
		pfx.expandSingleton(); // expand singleton representation for testing
		Automaton prefixAutomaton = BasicOperations.concatenate(pfx, BasicAutomata.makeAnyString());
		AutomatonQuery aq = new AutomatonQuery(NewTerm("bogus"), prefixAutomaton);
		Terms terms = MultiFields.getTerms(Searcher.IndexReader, FN);
		Assert.IsTrue(aq.getTermsEnum(terms) is PrefixTermsEnum);
		Assert.AreEqual(3, AutomatonQueryNrHits(aq));
	  }

	  /// <summary>
	  /// Test handling of the empty language
	  /// </summary>
	  public virtual void TestEmptyOptimization()
	  {
		AutomatonQuery aq = new AutomatonQuery(NewTerm("bogus"), BasicAutomata.makeEmpty());
		// not yet available: Assert.IsTrue(aq.getEnum(searcher.getIndexReader())
		// instanceof EmptyTermEnum);
		Terms terms = MultiFields.getTerms(Searcher.IndexReader, FN);
		assertSame(TermsEnum.EMPTY, aq.getTermsEnum(terms));
		Assert.AreEqual(0, AutomatonQueryNrHits(aq));
	  }

	  public virtual void TestHashCodeWithThreads()
	  {
		AutomatonQuery[] queries = new AutomatonQuery[1000];
		for (int i = 0; i < queries.Length; i++)
		{
		  queries[i] = new AutomatonQuery(new Term("bogus", "bogus"), AutomatonTestUtil.randomAutomaton(random()));
		}
		CountDownLatch startingGun = new CountDownLatch(1);
		int numThreads = TestUtil.Next(random(), 2, 5);
		Thread[] threads = new Thread[numThreads];
		for (int threadID = 0; threadID < numThreads; threadID++)
		{
		  Thread thread = new ThreadAnonymousInnerClassHelper(this, queries, startingGun);
		  threads[threadID] = thread;
		  thread.Start();
		}
		startingGun.countDown();
		foreach (Thread thread in threads)
		{
		  thread.Join();
		}
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestAutomatonQuery OuterInstance;

		  private AutomatonQuery[] Queries;
		  private CountDownLatch StartingGun;

		  public ThreadAnonymousInnerClassHelper(TestAutomatonQuery outerInstance, AutomatonQuery[] queries, CountDownLatch startingGun)
		  {
			  this.OuterInstance = outerInstance;
			  this.Queries = queries;
			  this.StartingGun = startingGun;
		  }

		  public override void Run()
		  {
			try
			{
			  StartingGun.@await();
			  for (int i = 0; i < Queries.Length; i++)
			  {
				Queries[i].GetHashCode();
			  }
			}
			catch (Exception e)
			{
			  Rethrow.rethrow(e);
			}
		  }
	  }
	}

}