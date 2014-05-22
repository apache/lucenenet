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

	using Term = Lucene.Net.Index.Term;
	using Occur = Lucene.Net.Search.BooleanClause.Occur;

	/// <summary>
	/// Basic equivalence tests for span queries
	/// </summary>
	public class TestSpanSearchEquivalence : SearchEquivalenceTestBase
	{

	  // TODO: we could go a little crazy for a lot of these,
	  // but these are just simple minimal cases in case something 
	  // goes horribly wrong. Put more intense tests elsewhere.

	  /// <summary>
	  /// SpanTermQuery(A) = TermQuery(A) </summary>
	  public virtual void TestSpanTermVersusTerm()
	  {
		Term t1 = randomTerm();
		assertSameSet(new TermQuery(t1), new SpanTermQuery(t1));
	  }

	  /// <summary>
	  /// SpanOrQuery(A, B) = (A B) </summary>
	  public virtual void TestSpanOrVersusBoolean()
	  {
		Term t1 = randomTerm();
		Term t2 = randomTerm();
		BooleanQuery q1 = new BooleanQuery();
		q1.add(new TermQuery(t1), Occur.SHOULD);
		q1.add(new TermQuery(t2), Occur.SHOULD);
		SpanOrQuery q2 = new SpanOrQuery(new SpanTermQuery(t1), new SpanTermQuery(t2));
		assertSameSet(q1, q2);
	  }

	  /// <summary>
	  /// SpanNotQuery(A, B) ⊆ SpanTermQuery(A) </summary>
	  public virtual void TestSpanNotVersusSpanTerm()
	  {
		Term t1 = randomTerm();
		Term t2 = randomTerm();
		assertSubsetOf(new SpanNotQuery(new SpanTermQuery(t1), new SpanTermQuery(t2)), new SpanTermQuery(t1));
	  }

	  /// <summary>
	  /// SpanFirstQuery(A, 10) ⊆ SpanTermQuery(A) </summary>
	  public virtual void TestSpanFirstVersusSpanTerm()
	  {
		Term t1 = randomTerm();
		assertSubsetOf(new SpanFirstQuery(new SpanTermQuery(t1), 10), new SpanTermQuery(t1));
	  }

	  /// <summary>
	  /// SpanNearQuery([A, B], 0, true) = "A B" </summary>
	  public virtual void TestSpanNearVersusPhrase()
	  {
		Term t1 = randomTerm();
		Term t2 = randomTerm();
		SpanQuery[] subquery = new SpanQuery[] {new SpanTermQuery(t1), new SpanTermQuery(t2)};
		SpanNearQuery q1 = new SpanNearQuery(subquery, 0, true);
		PhraseQuery q2 = new PhraseQuery();
		q2.add(t1);
		q2.add(t2);
		assertSameSet(q1, q2);
	  }

	  /// <summary>
	  /// SpanNearQuery([A, B], ∞, false) = +A +B </summary>
	  public virtual void TestSpanNearVersusBooleanAnd()
	  {
		Term t1 = randomTerm();
		Term t2 = randomTerm();
		SpanQuery[] subquery = new SpanQuery[] {new SpanTermQuery(t1), new SpanTermQuery(t2)};
		SpanNearQuery q1 = new SpanNearQuery(subquery, int.MaxValue, false);
		BooleanQuery q2 = new BooleanQuery();
		q2.add(new TermQuery(t1), Occur.MUST);
		q2.add(new TermQuery(t2), Occur.MUST);
		assertSameSet(q1, q2);
	  }

	  /// <summary>
	  /// SpanNearQuery([A B], 0, false) ⊆ SpanNearQuery([A B], 1, false) </summary>
	  public virtual void TestSpanNearVersusSloppySpanNear()
	  {
		Term t1 = randomTerm();
		Term t2 = randomTerm();
		SpanQuery[] subquery = new SpanQuery[] {new SpanTermQuery(t1), new SpanTermQuery(t2)};
		SpanNearQuery q1 = new SpanNearQuery(subquery, 0, false);
		SpanNearQuery q2 = new SpanNearQuery(subquery, 1, false);
		assertSubsetOf(q1, q2);
	  }

	  /// <summary>
	  /// SpanNearQuery([A B], 3, true) ⊆ SpanNearQuery([A B], 3, false) </summary>
	  public virtual void TestSpanNearInOrderVersusOutOfOrder()
	  {
		Term t1 = randomTerm();
		Term t2 = randomTerm();
		SpanQuery[] subquery = new SpanQuery[] {new SpanTermQuery(t1), new SpanTermQuery(t2)};
		SpanNearQuery q1 = new SpanNearQuery(subquery, 3, true);
		SpanNearQuery q2 = new SpanNearQuery(subquery, 3, false);
		assertSubsetOf(q1, q2);
	  }
	}

}