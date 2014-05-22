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


	using Lucene.Net.Analysis;
	using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using MultiFields = Lucene.Net.Index.MultiFields;
	using Terms = Lucene.Net.Index.Terms;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;


	public class TestTermRangeQuery : LuceneTestCase
	{

	  private int DocCount = 0;
	  private Directory Dir;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
	  }

	  public override void TearDown()
	  {
		Dir.close();
		base.tearDown();
	  }

	  public virtual void TestExclusive()
	  {
		Query query = TermRangeQuery.newStringRange("content", "A", "C", false, false);
		InitializeIndex(new string[] {"A", "B", "C", "D"});
		IndexReader reader = DirectoryReader.open(Dir);
		IndexSearcher searcher = newSearcher(reader);
		ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual("A,B,C,D, only B in range", 1, hits.Length);
		reader.close();

		InitializeIndex(new string[] {"A", "B", "D"});
		reader = DirectoryReader.open(Dir);
		searcher = newSearcher(reader);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual("A,B,D, only B in range", 1, hits.Length);
		reader.close();

		AddDoc("C");
		reader = DirectoryReader.open(Dir);
		searcher = newSearcher(reader);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual("C added, still only B in range", 1, hits.Length);
		reader.close();
	  }

	  public virtual void TestInclusive()
	  {
		Query query = TermRangeQuery.newStringRange("content", "A", "C", true, true);

		InitializeIndex(new string[]{"A", "B", "C", "D"});
		IndexReader reader = DirectoryReader.open(Dir);
		IndexSearcher searcher = newSearcher(reader);
		ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual("A,B,C,D - A,B,C in range", 3, hits.Length);
		reader.close();

		InitializeIndex(new string[]{"A", "B", "D"});
		reader = DirectoryReader.open(Dir);
		searcher = newSearcher(reader);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual("A,B,D - A and B in range", 2, hits.Length);
		reader.close();

		AddDoc("C");
		reader = DirectoryReader.open(Dir);
		searcher = newSearcher(reader);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual("C added - A, B, C in range", 3, hits.Length);
		reader.close();
	  }

	  public virtual void TestAllDocs()
	  {
		InitializeIndex(new string[]{"A", "B", "C", "D"});
		IndexReader reader = DirectoryReader.open(Dir);
		IndexSearcher searcher = newSearcher(reader);
		TermRangeQuery query = new TermRangeQuery("content", null, null, true, true);
		Terms terms = MultiFields.getTerms(searcher.IndexReader, "content");
		Assert.IsFalse(query.getTermsEnum(terms) is TermRangeTermsEnum);
		Assert.AreEqual(4, searcher.search(query, null, 1000).scoreDocs.length);
		query = new TermRangeQuery("content", null, null, false, false);
		Assert.IsFalse(query.getTermsEnum(terms) is TermRangeTermsEnum);
		Assert.AreEqual(4, searcher.search(query, null, 1000).scoreDocs.length);
		query = TermRangeQuery.newStringRange("content", "", null, true, false);
		Assert.IsFalse(query.getTermsEnum(terms) is TermRangeTermsEnum);
		Assert.AreEqual(4, searcher.search(query, null, 1000).scoreDocs.length);
		// and now anothe one
		query = TermRangeQuery.newStringRange("content", "B", null, true, false);
		Assert.IsTrue(query.getTermsEnum(terms) is TermRangeTermsEnum);
		Assert.AreEqual(3, searcher.search(query, null, 1000).scoreDocs.length);
		reader.close();
	  }

	  /// <summary>
	  /// this test should not be here, but it tests the fuzzy query rewrite mode (TOP_TERMS_SCORING_BOOLEAN_REWRITE)
	  /// with constant score and checks, that only the lower end of terms is put into the range 
	  /// </summary>
	  public virtual void TestTopTermsRewrite()
	  {
		InitializeIndex(new string[]{"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K"});

		IndexReader reader = DirectoryReader.open(Dir);
		IndexSearcher searcher = newSearcher(reader);
		TermRangeQuery query = TermRangeQuery.newStringRange("content", "B", "J", true, true);
		checkBooleanTerms(searcher, query, "B", "C", "D", "E", "F", "G", "H", "I", "J");

		int savedClauseCount = BooleanQuery.MaxClauseCount;
		try
		{
		  BooleanQuery.MaxClauseCount = 3;
		  checkBooleanTerms(searcher, query, "B", "C", "D");
		}
		finally
		{
		  BooleanQuery.MaxClauseCount = savedClauseCount;
		}
		reader.close();
	  }

	  private void CheckBooleanTerms(IndexSearcher searcher, TermRangeQuery query, params string[] terms)
	  {
		query.RewriteMethod = new MultiTermQuery.TopTermsScoringBooleanQueryRewrite(50);
		BooleanQuery bq = (BooleanQuery) searcher.rewrite(query);
		Set<string> allowedTerms = asSet(terms);
		Assert.AreEqual(allowedTerms.size(), bq.clauses().size());
		foreach (BooleanClause c in bq.clauses())
		{
		  Assert.IsTrue(c.Query is TermQuery);
		  TermQuery tq = (TermQuery) c.Query;
		  string term = tq.Term.text();
		  Assert.IsTrue("invalid term: " + term, allowedTerms.contains(term));
		  allowedTerms.remove(term); // remove to fail on double terms
		}
		Assert.AreEqual(0, allowedTerms.size());
	  }

	  public virtual void TestEqualsHashcode()
	  {
		Query query = TermRangeQuery.newStringRange("content", "A", "C", true, true);

		query.Boost = 1.0f;
		Query other = TermRangeQuery.newStringRange("content", "A", "C", true, true);
		other.Boost = 1.0f;

		Assert.AreEqual("query equals itself is true", query, query);
		Assert.AreEqual("equivalent queries are equal", query, other);
		Assert.AreEqual("hashcode must return same value when equals is true", query.GetHashCode(), other.GetHashCode());

		other.Boost = 2.0f;
		Assert.IsFalse("Different boost queries are not equal", query.Equals(other));

		other = TermRangeQuery.newStringRange("notcontent", "A", "C", true, true);
		Assert.IsFalse("Different fields are not equal", query.Equals(other));

		other = TermRangeQuery.newStringRange("content", "X", "C", true, true);
		Assert.IsFalse("Different lower terms are not equal", query.Equals(other));

		other = TermRangeQuery.newStringRange("content", "A", "Z", true, true);
		Assert.IsFalse("Different upper terms are not equal", query.Equals(other));

		query = TermRangeQuery.newStringRange("content", null, "C", true, true);
		other = TermRangeQuery.newStringRange("content", null, "C", true, true);
		Assert.AreEqual("equivalent queries with null lowerterms are equal()", query, other);
		Assert.AreEqual("hashcode must return same value when equals is true", query.GetHashCode(), other.GetHashCode());

		query = TermRangeQuery.newStringRange("content", "C", null, true, true);
		other = TermRangeQuery.newStringRange("content", "C", null, true, true);
		Assert.AreEqual("equivalent queries with null upperterms are equal()", query, other);
		Assert.AreEqual("hashcode returns same value", query.GetHashCode(), other.GetHashCode());

		query = TermRangeQuery.newStringRange("content", null, "C", true, true);
		other = TermRangeQuery.newStringRange("content", "C", null, true, true);
		Assert.IsFalse("queries with different upper and lower terms are not equal", query.Equals(other));

		query = TermRangeQuery.newStringRange("content", "A", "C", false, false);
		other = TermRangeQuery.newStringRange("content", "A", "C", true, true);
		Assert.IsFalse("queries with different inclusive are not equal", query.Equals(other));
	  }

	  private class SingleCharAnalyzer : Analyzer
	  {

		private class SingleCharTokenizer : Tokenizer
		{
		  internal char[] Buffer = new char[1];
		  internal bool Done = false;
		  internal CharTermAttribute TermAtt;

		  public SingleCharTokenizer(Reader r) : base(r)
		  {
			TermAtt = addAttribute(typeof(CharTermAttribute));
		  }

		  public override bool IncrementToken()
		  {
			if (Done)
			{
			  return false;
			}
			else
			{
			  int count = input.read(Buffer);
			  ClearAttributes();
			  Done = true;
			  if (count == 1)
			  {
				TermAtt.copyBuffer(Buffer, 0, 1);
			  }
			  return true;
			}
		  }

		  public override void Reset()
		  {
			base.reset();
			Done = false;
		  }
		}

		public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		{
		  return new TokenStreamComponents(new SingleCharTokenizer(reader));
		}
	  }

	  private void InitializeIndex(string[] values)
	  {
		InitializeIndex(values, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false));
	  }

	  private void InitializeIndex(string[] values, Analyzer analyzer)
	  {
		IndexWriter writer = new IndexWriter(Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setOpenMode(OpenMode.CREATE));
		for (int i = 0; i < values.Length; i++)
		{
		  InsertDoc(writer, values[i]);
		}
		writer.close();
	  }

	  // shouldnt create an analyzer for every doc?
	  private void AddDoc(string content)
	  {
		IndexWriter writer = new IndexWriter(Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.WHITESPACE, false)).setOpenMode(OpenMode.APPEND));
		InsertDoc(writer, content);
		writer.close();
	  }

	  private void InsertDoc(IndexWriter writer, string content)
	  {
		Document doc = new Document();

		doc.add(newStringField("id", "id" + DocCount, Field.Store.YES));
		doc.add(newTextField("content", content, Field.Store.NO));

		writer.addDocument(doc);
		DocCount++;
	  }

	  // LUCENE-38
	  public virtual void TestExclusiveLowerNull()
	  {
		Analyzer analyzer = new SingleCharAnalyzer();
		//http://issues.apache.org/jira/browse/LUCENE-38
		Query query = TermRangeQuery.newStringRange("content", null, "C", false, false);
		InitializeIndex(new string[] {"A", "B", "", "C", "D"}, analyzer);
		IndexReader reader = DirectoryReader.open(Dir);
		IndexSearcher searcher = newSearcher(reader);
		int numHits = searcher.search(query, null, 1000).totalHits;
		// When Lucene-38 is fixed, use the assert on the next line:
		Assert.AreEqual("A,B,<empty string>,C,D => A, B & <empty string> are in range", 3, numHits);
		// until Lucene-38 is fixed, use this assert:
		//Assert.AreEqual("A,B,<empty string>,C,D => A, B & <empty string> are in range", 2, hits.length());

		reader.close();
		InitializeIndex(new string[] {"A", "B", "", "D"}, analyzer);
		reader = DirectoryReader.open(Dir);
		searcher = newSearcher(reader);
		numHits = searcher.search(query, null, 1000).totalHits;
		// When Lucene-38 is fixed, use the assert on the next line:
		Assert.AreEqual("A,B,<empty string>,D => A, B & <empty string> are in range", 3, numHits);
		// until Lucene-38 is fixed, use this assert:
		//Assert.AreEqual("A,B,<empty string>,D => A, B & <empty string> are in range", 2, hits.length());
		reader.close();
		AddDoc("C");
		reader = DirectoryReader.open(Dir);
		searcher = newSearcher(reader);
		numHits = searcher.search(query, null, 1000).totalHits;
		// When Lucene-38 is fixed, use the assert on the next line:
		Assert.AreEqual("C added, still A, B & <empty string> are in range", 3, numHits);
		// until Lucene-38 is fixed, use this assert
		//Assert.AreEqual("C added, still A, B & <empty string> are in range", 2, hits.length());
		reader.close();
	  }

	  // LUCENE-38
	  public virtual void TestInclusiveLowerNull()
	  {
		//http://issues.apache.org/jira/browse/LUCENE-38
		Analyzer analyzer = new SingleCharAnalyzer();
		Query query = TermRangeQuery.newStringRange("content", null, "C", true, true);
		InitializeIndex(new string[]{"A", "B", "","C", "D"}, analyzer);
		IndexReader reader = DirectoryReader.open(Dir);
		IndexSearcher searcher = newSearcher(reader);
		int numHits = searcher.search(query, null, 1000).totalHits;
		// When Lucene-38 is fixed, use the assert on the next line:
		Assert.AreEqual("A,B,<empty string>,C,D => A,B,<empty string>,C in range", 4, numHits);
		// until Lucene-38 is fixed, use this assert
		//Assert.AreEqual("A,B,<empty string>,C,D => A,B,<empty string>,C in range", 3, hits.length());
		reader.close();
		InitializeIndex(new string[]{"A", "B", "", "D"}, analyzer);
		reader = DirectoryReader.open(Dir);
		searcher = newSearcher(reader);
		numHits = searcher.search(query, null, 1000).totalHits;
		// When Lucene-38 is fixed, use the assert on the next line:
		Assert.AreEqual("A,B,<empty string>,D - A, B and <empty string> in range", 3, numHits);
		// until Lucene-38 is fixed, use this assert
		//Assert.AreEqual("A,B,<empty string>,D => A, B and <empty string> in range", 2, hits.length());
		reader.close();
		AddDoc("C");
		reader = DirectoryReader.open(Dir);
		searcher = newSearcher(reader);
		numHits = searcher.search(query, null, 1000).totalHits;
		// When Lucene-38 is fixed, use the assert on the next line:
		Assert.AreEqual("C added => A,B,<empty string>,C in range", 4, numHits);
		// until Lucene-38 is fixed, use this assert
		//Assert.AreEqual("C added => A,B,<empty string>,C in range", 3, hits.length());
		 reader.close();
	  }
	}

}