using System;

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

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using MultiReader = Lucene.Net.Index.MultiReader;
	using Term = Lucene.Net.Index.Term;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Directory = Lucene.Net.Store.Directory;
	using AttributeSource = Lucene.Net.Util.AttributeSource;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;

	public class TestMultiTermQueryRewrites : LuceneTestCase
	{

	  internal static Directory Dir, Sdir1, Sdir2;
	  internal static IndexReader Reader, MultiReader, MultiReaderDupls;
	  internal static IndexSearcher Searcher, MultiSearcher, MultiSearcherDupls;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		Dir = newDirectory();
		Sdir1 = newDirectory();
		Sdir2 = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Dir, new MockAnalyzer(random()));
		RandomIndexWriter swriter1 = new RandomIndexWriter(random(), Sdir1, new MockAnalyzer(random()));
		RandomIndexWriter swriter2 = new RandomIndexWriter(random(), Sdir2, new MockAnalyzer(random()));

		for (int i = 0; i < 10; i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("data", Convert.ToString(i), Field.Store.NO));
		  writer.addDocument(doc);
		  ((i % 2 == 0) ? swriter1 : swriter2).addDocument(doc);
		}
		writer.forceMerge(1);
		swriter1.forceMerge(1);
		swriter2.forceMerge(1);
		writer.close();
		swriter1.close();
		swriter2.close();

		Reader = DirectoryReader.open(Dir);
		Searcher = newSearcher(Reader);

		MultiReader = new MultiReader(new IndexReader[] {DirectoryReader.open(Sdir1), DirectoryReader.open(Sdir2)}, true);
		MultiSearcher = newSearcher(MultiReader);

		MultiReaderDupls = new MultiReader(new IndexReader[] {DirectoryReader.open(Sdir1), DirectoryReader.open(Dir)}, true);
		MultiSearcherDupls = newSearcher(MultiReaderDupls);
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass() throws Exception
	  public static void AfterClass()
	  {
		Reader.close();
		MultiReader.close();
		MultiReaderDupls.close();
		Dir.close();
		Sdir1.close();
		Sdir2.close();
		Reader = MultiReader = MultiReaderDupls = null;
		Searcher = MultiSearcher = MultiSearcherDupls = null;
		Dir = Sdir1 = Sdir2 = null;
	  }

	  private Query ExtractInnerQuery(Query q)
	  {
		if (q is ConstantScoreQuery)
		{
		  // wrapped as ConstantScoreQuery
		  q = ((ConstantScoreQuery) q).Query;
		}
		return q;
	  }

	  private Term ExtractTerm(Query q)
	  {
		q = ExtractInnerQuery(q);
		return ((TermQuery) q).Term;
	  }

	  private void CheckBooleanQueryOrder(Query q)
	  {
		q = ExtractInnerQuery(q);
		BooleanQuery bq = (BooleanQuery) q;
		Term last = null, act ;
		foreach (BooleanClause clause in bq.clauses())
		{
		  act = ExtractTerm(clause.Query);
		  if (last != null)
		  {
			Assert.IsTrue("sort order of terms in BQ violated", last.compareTo(act) < 0);
		  }
		  last = act;
		}
	  }

	  private void CheckDuplicateTerms(MultiTermQuery.RewriteMethod method)
	  {
		MultiTermQuery mtq = TermRangeQuery.newStringRange("data", "2", "7", true, true);
		mtq.RewriteMethod = method;
		Query q1 = Searcher.rewrite(mtq);
		Query q2 = MultiSearcher.rewrite(mtq);
		Query q3 = MultiSearcherDupls.rewrite(mtq);
		if (VERBOSE)
		{
		  Console.WriteLine();
		  Console.WriteLine("single segment: " + q1);
		  Console.WriteLine("multi segment: " + q2);
		  Console.WriteLine("multi segment with duplicates: " + q3);
		}
		Assert.AreEqual("The multi-segment case must produce same rewritten query", q1, q2);
		Assert.AreEqual("The multi-segment case with duplicates must produce same rewritten query", q1, q3);
		CheckBooleanQueryOrder(q1);
		CheckBooleanQueryOrder(q2);
		CheckBooleanQueryOrder(q3);
	  }

	  public virtual void TestRewritesWithDuplicateTerms()
	  {
		CheckDuplicateTerms(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);

		CheckDuplicateTerms(MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE);

		// use a large PQ here to only test duplicate terms and dont mix up when all scores are equal
		CheckDuplicateTerms(new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(1024));
		CheckDuplicateTerms(new MultiTermQuery.TopTermsBoostOnlyBooleanQueryRewrite(1024));

		// Test auto rewrite (but only boolean mode), so we set the limits to large values to always get a BQ
		MultiTermQuery.ConstantScoreAutoRewrite rewrite = new MultiTermQuery.ConstantScoreAutoRewrite();
		rewrite.TermCountCutoff = int.MaxValue;
		rewrite.DocCountPercent = 100.0;
		CheckDuplicateTerms(rewrite);
	  }

	  private void CheckBooleanQueryBoosts(BooleanQuery bq)
	  {
		foreach (BooleanClause clause in bq.clauses())
		{
		  TermQuery mtq = (TermQuery) clause.Query;
		  Assert.AreEqual("Parallel sorting of boosts in rewrite mode broken", Convert.ToSingle(mtq.Term.text()), mtq.Boost, 0);
		}
	  }

	  private void CheckBoosts(MultiTermQuery.RewriteMethod method)
	  {
		MultiTermQuery mtq = new MultiTermQueryAnonymousInnerClassHelper(this);
		mtq.RewriteMethod = method;
		Query q1 = Searcher.rewrite(mtq);
		Query q2 = MultiSearcher.rewrite(mtq);
		Query q3 = MultiSearcherDupls.rewrite(mtq);
		if (VERBOSE)
		{
		  Console.WriteLine();
		  Console.WriteLine("single segment: " + q1);
		  Console.WriteLine("multi segment: " + q2);
		  Console.WriteLine("multi segment with duplicates: " + q3);
		}
		Assert.AreEqual("The multi-segment case must produce same rewritten query", q1, q2);
		Assert.AreEqual("The multi-segment case with duplicates must produce same rewritten query", q1, q3);
		CheckBooleanQueryBoosts((BooleanQuery) q1);
		CheckBooleanQueryBoosts((BooleanQuery) q2);
		CheckBooleanQueryBoosts((BooleanQuery) q3);
	  }

	  private class MultiTermQueryAnonymousInnerClassHelper : MultiTermQuery
	  {
		  private readonly TestMultiTermQueryRewrites OuterInstance;

		  public MultiTermQueryAnonymousInnerClassHelper(TestMultiTermQueryRewrites outerInstance) : base("data")
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
		  {
			return new TermRangeTermsEnumAnonymousInnerClassHelper(this, terms.iterator(null), new BytesRef("2"), new BytesRef("7"));
		  }

		  private class TermRangeTermsEnumAnonymousInnerClassHelper : TermRangeTermsEnum
		  {
			  private readonly MultiTermQueryAnonymousInnerClassHelper OuterInstance;

			  public TermRangeTermsEnumAnonymousInnerClassHelper(MultiTermQueryAnonymousInnerClassHelper outerInstance, UnknownType iterator, BytesRef org, BytesRef org) : base(iterator, BytesRef, BytesRef, true, true)
			  {
				  this.outerInstance = outerInstance;
				  boostAtt = attributes().addAttribute(typeof(BoostAttribute));
			  }

			  internal readonly BoostAttribute boostAtt;

			  protected internal override AcceptStatus Accept(BytesRef term)
			  {
				boostAtt.Boost = Convert.ToSingle(term.utf8ToString());
				return base.accept(term);
			  }
		  }

		  public override string ToString(string field)
		  {
			return "dummy";
		  }
	  }

	  public virtual void TestBoosts()
	  {
		CheckBoosts(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);

		// use a large PQ here to only test boosts and dont mix up when all scores are equal
		CheckBoosts(new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(1024));
	  }

	  private void CheckMaxClauseLimitation(MultiTermQuery.RewriteMethod method)
	  {
		int savedMaxClauseCount = BooleanQuery.MaxClauseCount;
		BooleanQuery.MaxClauseCount = 3;

		MultiTermQuery mtq = TermRangeQuery.newStringRange("data", "2", "7", true, true);
		mtq.RewriteMethod = method;
		try
		{
		  MultiSearcherDupls.rewrite(mtq);
		  Assert.Fail("Should throw BooleanQuery.TooManyClauses");
		}
		catch (BooleanQuery.TooManyClauses e)
		{
		  //  Maybe remove this assert in later versions, when internal API changes:
		  Assert.AreEqual("Should throw BooleanQuery.TooManyClauses with a stacktrace containing checkMaxClauseCount()", "checkMaxClauseCount", e.StackTrace[0].MethodName);
		}
		finally
		{
		  BooleanQuery.MaxClauseCount = savedMaxClauseCount;
		}
	  }

	  private void CheckNoMaxClauseLimitation(MultiTermQuery.RewriteMethod method)
	  {
		int savedMaxClauseCount = BooleanQuery.MaxClauseCount;
		BooleanQuery.MaxClauseCount = 3;

		MultiTermQuery mtq = TermRangeQuery.newStringRange("data", "2", "7", true, true);
		mtq.RewriteMethod = method;
		try
		{
		  MultiSearcherDupls.rewrite(mtq);
		}
		finally
		{
		  BooleanQuery.MaxClauseCount = savedMaxClauseCount;
		}
	  }

	  public virtual void TestMaxClauseLimitations()
	  {
		CheckMaxClauseLimitation(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
		CheckMaxClauseLimitation(MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE);

		CheckNoMaxClauseLimitation(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
		CheckNoMaxClauseLimitation(MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT);
		CheckNoMaxClauseLimitation(new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(1024));
		CheckNoMaxClauseLimitation(new MultiTermQuery.TopTermsBoostOnlyBooleanQueryRewrite(1024));
	  }

	}

}