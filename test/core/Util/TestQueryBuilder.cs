namespace Lucene.Net.Util
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
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using TokenFilter = Lucene.Net.Analysis.TokenFilter;
	using TokenStream = Lucene.Net.Analysis.TokenStream;
	using Tokenizer = Lucene.Net.Analysis.Tokenizer;
	using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
	using PositionIncrementAttribute = Lucene.Net.Analysis.Tokenattributes.PositionIncrementAttribute;
	using Term = Lucene.Net.Index.Term;
	using BooleanClause = Lucene.Net.Search.BooleanClause;
	using BooleanQuery = Lucene.Net.Search.BooleanQuery;
	using MultiPhraseQuery = Lucene.Net.Search.MultiPhraseQuery;
	using PhraseQuery = Lucene.Net.Search.PhraseQuery;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using CharacterRunAutomaton = Lucene.Net.Util.Automaton.CharacterRunAutomaton;
	using RegExp = Lucene.Net.Util.Automaton.RegExp;

	public class TestQueryBuilder : LuceneTestCase
	{

	  public virtual void TestTerm()
	  {
		TermQuery expected = new TermQuery(new Term("field", "test"));
		QueryBuilder builder = new QueryBuilder(new MockAnalyzer(random()));
		Assert.AreEqual(expected, builder.createBooleanQuery("field", "test"));
	  }

	  public virtual void TestBoolean()
	  {
		BooleanQuery expected = new BooleanQuery();
		expected.add(new TermQuery(new Term("field", "foo")), BooleanClause.Occur.SHOULD);
		expected.add(new TermQuery(new Term("field", "bar")), BooleanClause.Occur.SHOULD);
		QueryBuilder builder = new QueryBuilder(new MockAnalyzer(random()));
		Assert.AreEqual(expected, builder.createBooleanQuery("field", "foo bar"));
	  }

	  public virtual void TestBooleanMust()
	  {
		BooleanQuery expected = new BooleanQuery();
		expected.add(new TermQuery(new Term("field", "foo")), BooleanClause.Occur.MUST);
		expected.add(new TermQuery(new Term("field", "bar")), BooleanClause.Occur.MUST);
		QueryBuilder builder = new QueryBuilder(new MockAnalyzer(random()));
		Assert.AreEqual(expected, builder.createBooleanQuery("field", "foo bar", BooleanClause.Occur.MUST));
	  }

	  public virtual void TestMinShouldMatchNone()
	  {
		QueryBuilder builder = new QueryBuilder(new MockAnalyzer(random()));
		Assert.AreEqual(builder.createBooleanQuery("field", "one two three four"), builder.createMinShouldMatchQuery("field", "one two three four", 0f));
	  }

	  public virtual void TestMinShouldMatchAll()
	  {
		QueryBuilder builder = new QueryBuilder(new MockAnalyzer(random()));
		Assert.AreEqual(builder.createBooleanQuery("field", "one two three four", BooleanClause.Occur.MUST), builder.createMinShouldMatchQuery("field", "one two three four", 1f));
	  }

	  public virtual void TestMinShouldMatch()
	  {
		BooleanQuery expected = new BooleanQuery();
		expected.add(new TermQuery(new Term("field", "one")), BooleanClause.Occur.SHOULD);
		expected.add(new TermQuery(new Term("field", "two")), BooleanClause.Occur.SHOULD);
		expected.add(new TermQuery(new Term("field", "three")), BooleanClause.Occur.SHOULD);
		expected.add(new TermQuery(new Term("field", "four")), BooleanClause.Occur.SHOULD);
		expected.MinimumNumberShouldMatch = 0;

		QueryBuilder builder = new QueryBuilder(new MockAnalyzer(random()));
		Assert.AreEqual(expected, builder.createMinShouldMatchQuery("field", "one two three four", 0.1f));
		Assert.AreEqual(expected, builder.createMinShouldMatchQuery("field", "one two three four", 0.24f));

		expected.MinimumNumberShouldMatch = 1;
		Assert.AreEqual(expected, builder.createMinShouldMatchQuery("field", "one two three four", 0.25f));
		Assert.AreEqual(expected, builder.createMinShouldMatchQuery("field", "one two three four", 0.49f));

		expected.MinimumNumberShouldMatch = 2;
		Assert.AreEqual(expected, builder.createMinShouldMatchQuery("field", "one two three four", 0.5f));
		Assert.AreEqual(expected, builder.createMinShouldMatchQuery("field", "one two three four", 0.74f));

		expected.MinimumNumberShouldMatch = 3;
		Assert.AreEqual(expected, builder.createMinShouldMatchQuery("field", "one two three four", 0.75f));
		Assert.AreEqual(expected, builder.createMinShouldMatchQuery("field", "one two three four", 0.99f));
	  }

	  public virtual void TestPhraseQueryPositionIncrements()
	  {
		PhraseQuery expected = new PhraseQuery();
		expected.add(new Term("field", "1"));
		expected.add(new Term("field", "2"), 2);

		CharacterRunAutomaton stopList = new CharacterRunAutomaton((new RegExp("[sS][tT][oO][pP]")).toAutomaton());

		Analyzer analyzer = new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false, stopList);

		QueryBuilder builder = new QueryBuilder(analyzer);
		Assert.AreEqual(expected, builder.createPhraseQuery("field", "1 stop 2"));
	  }

	  public virtual void TestEmpty()
	  {
		QueryBuilder builder = new QueryBuilder(new MockAnalyzer(random()));
		assertNull(builder.createBooleanQuery("field", ""));
	  }

	  /// <summary>
	  /// adds synonym of "dog" for "dogs". </summary>
	  internal class MockSynonymAnalyzer : Analyzer
	  {
		protected internal override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		{
		  MockTokenizer tokenizer = new MockTokenizer(reader);
		  return new TokenStreamComponents(tokenizer, new MockSynonymFilter(tokenizer));
		}
	  }

	  /// <summary>
	  /// adds synonym of "dog" for "dogs".
	  /// </summary>
	  protected internal class MockSynonymFilter : TokenFilter
	  {
		internal CharTermAttribute TermAtt = addAttribute(typeof(CharTermAttribute));
		internal PositionIncrementAttribute PosIncAtt = addAttribute(typeof(PositionIncrementAttribute));
		internal bool AddSynonym = false;

		public MockSynonymFilter(TokenStream input) : base(input)
		{
		}

		public override bool IncrementToken()
		{
		  if (AddSynonym) // inject our synonym
		  {
			ClearAttributes();
			TermAtt.SetEmpty().append("dog");
			PosIncAtt.PositionIncrement = 0;
			AddSynonym = false;
			return true;
		  }

		  if (input.IncrementToken())
		  {
			AddSynonym = TermAtt.ToString().Equals("dogs");
			return true;
		  }
		  else
		  {
			return false;
		  }
		}
	  }

	  /// <summary>
	  /// simple synonyms test </summary>
	  public virtual void TestSynonyms()
	  {
		BooleanQuery expected = new BooleanQuery(true);
		expected.add(new TermQuery(new Term("field", "dogs")), BooleanClause.Occur.SHOULD);
		expected.add(new TermQuery(new Term("field", "dog")), BooleanClause.Occur.SHOULD);
		QueryBuilder builder = new QueryBuilder(new MockSynonymAnalyzer());
		Assert.AreEqual(expected, builder.createBooleanQuery("field", "dogs"));
		Assert.AreEqual(expected, builder.createPhraseQuery("field", "dogs"));
		Assert.AreEqual(expected, builder.createBooleanQuery("field", "dogs", BooleanClause.Occur.MUST));
		Assert.AreEqual(expected, builder.createPhraseQuery("field", "dogs"));
	  }

	  /// <summary>
	  /// forms multiphrase query </summary>
	  public virtual void TestSynonymsPhrase()
	  {
		MultiPhraseQuery expected = new MultiPhraseQuery();
		expected.add(new Term("field", "old"));
		expected.add(new Term[] {new Term("field", "dogs"), new Term("field", "dog")});
		QueryBuilder builder = new QueryBuilder(new MockSynonymAnalyzer());
		Assert.AreEqual(expected, builder.createPhraseQuery("field", "old dogs"));
	  }

	  protected internal class SimpleCJKTokenizer : Tokenizer
	  {
		internal CharTermAttribute TermAtt = addAttribute(typeof(CharTermAttribute));

		public SimpleCJKTokenizer(Reader input) : base(input)
		{
		}

		public override bool IncrementToken()
		{
		  int ch = input.read();
		  if (ch < 0)
		  {
			return false;
		  }
		  ClearAttributes();
		  TermAtt.SetEmpty().append((char) ch);
		  return true;
		}
	  }

	  private class SimpleCJKAnalyzer : Analyzer
	  {
		  private readonly TestQueryBuilder OuterInstance;

		  public SimpleCJKAnalyzer(TestQueryBuilder outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		{
		  return new TokenStreamComponents(new SimpleCJKTokenizer(reader));
		}
	  }

	  public virtual void TestCJKTerm()
	  {
		// individual CJK chars as terms
		SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer(this);

		BooleanQuery expected = new BooleanQuery();
		expected.add(new TermQuery(new Term("field", "中")), BooleanClause.Occur.SHOULD);
		expected.add(new TermQuery(new Term("field", "国")), BooleanClause.Occur.SHOULD);

		QueryBuilder builder = new QueryBuilder(analyzer);
		Assert.AreEqual(expected, builder.createBooleanQuery("field", "中国"));
	  }

	  public virtual void TestCJKPhrase()
	  {
		// individual CJK chars as terms
		SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer(this);

		PhraseQuery expected = new PhraseQuery();
		expected.add(new Term("field", "中"));
		expected.add(new Term("field", "国"));

		QueryBuilder builder = new QueryBuilder(analyzer);
		Assert.AreEqual(expected, builder.createPhraseQuery("field", "中国"));
	  }

	  public virtual void TestCJKSloppyPhrase()
	  {
		// individual CJK chars as terms
		SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer(this);

		PhraseQuery expected = new PhraseQuery();
		expected.Slop = 3;
		expected.add(new Term("field", "中"));
		expected.add(new Term("field", "国"));

		QueryBuilder builder = new QueryBuilder(analyzer);
		Assert.AreEqual(expected, builder.createPhraseQuery("field", "中国", 3));
	  }

	  /// <summary>
	  /// adds synonym of "國" for "国".
	  /// </summary>
	  protected internal class MockCJKSynonymFilter : TokenFilter
	  {
		internal CharTermAttribute TermAtt = addAttribute(typeof(CharTermAttribute));
		internal PositionIncrementAttribute PosIncAtt = addAttribute(typeof(PositionIncrementAttribute));
		internal bool AddSynonym = false;

		public MockCJKSynonymFilter(TokenStream input) : base(input)
		{
		}

		public override bool IncrementToken()
		{
		  if (AddSynonym) // inject our synonym
		  {
			ClearAttributes();
			TermAtt.SetEmpty().append("國");
			PosIncAtt.PositionIncrement = 0;
			AddSynonym = false;
			return true;
		  }

		  if (input.IncrementToken())
		  {
			AddSynonym = TermAtt.ToString().Equals("国");
			return true;
		  }
		  else
		  {
			return false;
		  }
		}
	  }

	  internal class MockCJKSynonymAnalyzer : Analyzer
	  {
		protected internal override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		{
		  Tokenizer tokenizer = new SimpleCJKTokenizer(reader);
		  return new TokenStreamComponents(tokenizer, new MockCJKSynonymFilter(tokenizer));
		}
	  }

	  /// <summary>
	  /// simple CJK synonym test </summary>
	  public virtual void TestCJKSynonym()
	  {
		BooleanQuery expected = new BooleanQuery(true);
		expected.add(new TermQuery(new Term("field", "国")), BooleanClause.Occur.SHOULD);
		expected.add(new TermQuery(new Term("field", "國")), BooleanClause.Occur.SHOULD);
		QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
		Assert.AreEqual(expected, builder.createBooleanQuery("field", "国"));
		Assert.AreEqual(expected, builder.createPhraseQuery("field", "国"));
		Assert.AreEqual(expected, builder.createBooleanQuery("field", "国", BooleanClause.Occur.MUST));
	  }

	  /// <summary>
	  /// synonyms with default OR operator </summary>
	  public virtual void TestCJKSynonymsOR()
	  {
		BooleanQuery expected = new BooleanQuery();
		expected.add(new TermQuery(new Term("field", "中")), BooleanClause.Occur.SHOULD);
		BooleanQuery inner = new BooleanQuery(true);
		inner.add(new TermQuery(new Term("field", "国")), BooleanClause.Occur.SHOULD);
		inner.add(new TermQuery(new Term("field", "國")), BooleanClause.Occur.SHOULD);
		expected.add(inner, BooleanClause.Occur.SHOULD);
		QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
		Assert.AreEqual(expected, builder.createBooleanQuery("field", "中国"));
	  }

	  /// <summary>
	  /// more complex synonyms with default OR operator </summary>
	  public virtual void TestCJKSynonymsOR2()
	  {
		BooleanQuery expected = new BooleanQuery();
		expected.add(new TermQuery(new Term("field", "中")), BooleanClause.Occur.SHOULD);
		BooleanQuery inner = new BooleanQuery(true);
		inner.add(new TermQuery(new Term("field", "国")), BooleanClause.Occur.SHOULD);
		inner.add(new TermQuery(new Term("field", "國")), BooleanClause.Occur.SHOULD);
		expected.add(inner, BooleanClause.Occur.SHOULD);
		BooleanQuery inner2 = new BooleanQuery(true);
		inner2.add(new TermQuery(new Term("field", "国")), BooleanClause.Occur.SHOULD);
		inner2.add(new TermQuery(new Term("field", "國")), BooleanClause.Occur.SHOULD);
		expected.add(inner2, BooleanClause.Occur.SHOULD);
		QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
		Assert.AreEqual(expected, builder.createBooleanQuery("field", "中国国"));
	  }

	  /// <summary>
	  /// synonyms with default AND operator </summary>
	  public virtual void TestCJKSynonymsAND()
	  {
		BooleanQuery expected = new BooleanQuery();
		expected.add(new TermQuery(new Term("field", "中")), BooleanClause.Occur.MUST);
		BooleanQuery inner = new BooleanQuery(true);
		inner.add(new TermQuery(new Term("field", "国")), BooleanClause.Occur.SHOULD);
		inner.add(new TermQuery(new Term("field", "國")), BooleanClause.Occur.SHOULD);
		expected.add(inner, BooleanClause.Occur.MUST);
		QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
		Assert.AreEqual(expected, builder.createBooleanQuery("field", "中国", BooleanClause.Occur.MUST));
	  }

	  /// <summary>
	  /// more complex synonyms with default AND operator </summary>
	  public virtual void TestCJKSynonymsAND2()
	  {
		BooleanQuery expected = new BooleanQuery();
		expected.add(new TermQuery(new Term("field", "中")), BooleanClause.Occur.MUST);
		BooleanQuery inner = new BooleanQuery(true);
		inner.add(new TermQuery(new Term("field", "国")), BooleanClause.Occur.SHOULD);
		inner.add(new TermQuery(new Term("field", "國")), BooleanClause.Occur.SHOULD);
		expected.add(inner, BooleanClause.Occur.MUST);
		BooleanQuery inner2 = new BooleanQuery(true);
		inner2.add(new TermQuery(new Term("field", "国")), BooleanClause.Occur.SHOULD);
		inner2.add(new TermQuery(new Term("field", "國")), BooleanClause.Occur.SHOULD);
		expected.add(inner2, BooleanClause.Occur.MUST);
		QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
		Assert.AreEqual(expected, builder.createBooleanQuery("field", "中国国", BooleanClause.Occur.MUST));
	  }

	  /// <summary>
	  /// forms multiphrase query </summary>
	  public virtual void TestCJKSynonymsPhrase()
	  {
		MultiPhraseQuery expected = new MultiPhraseQuery();
		expected.add(new Term("field", "中"));
		expected.add(new Term[] {new Term("field", "国"), new Term("field", "國")});
		QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
		Assert.AreEqual(expected, builder.createPhraseQuery("field", "中国"));
		expected.Slop = 3;
		Assert.AreEqual(expected, builder.createPhraseQuery("field", "中国", 3));
	  }
	}

}