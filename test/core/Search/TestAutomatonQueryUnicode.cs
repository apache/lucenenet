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
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using Automaton = Lucene.Net.Util.Automaton.Automaton;
	using RegExp = Lucene.Net.Util.Automaton.RegExp;

	/// <summary>
	/// Test the automaton query for several unicode corner cases,
	/// specifically enumerating strings/indexes containing supplementary characters,
	/// and the differences between UTF-8/UTF-32 and UTF-16 binary sort order.
	/// </summary>
	public class TestAutomatonQueryUnicode : LuceneTestCase
	{
	  private IndexReader Reader;
	  private IndexSearcher Searcher;
	  private Directory Directory;

	  private readonly string FN = "field";

	  public override void SetUp()
	  {
		base.setUp();
		Directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory);
		Document doc = new Document();
		Field titleField = newTextField("title", "some title", Field.Store.NO);
		Field field = newTextField(FN, "", Field.Store.NO);
		Field footerField = newTextField("footer", "a footer", Field.Store.NO);
		doc.add(titleField);
		doc.add(field);
		doc.add(footerField);
		field.StringValue = "\uD866\uDF05abcdef";
		writer.addDocument(doc);
		field.StringValue = "\uD866\uDF06ghijkl";
		writer.addDocument(doc);
		// this sorts before the previous two in UTF-8/UTF-32, but after in UTF-16!!!
		field.StringValue = "\uFB94mnopqr";
		writer.addDocument(doc);
		field.StringValue = "\uFB95stuvwx"; // this one too.
		writer.addDocument(doc);
		field.StringValue = "a\uFFFCbc";
		writer.addDocument(doc);
		field.StringValue = "a\uFFFDbc";
		writer.addDocument(doc);
		field.StringValue = "a\uFFFEbc";
		writer.addDocument(doc);
		field.StringValue = "a\uFB94bc";
		writer.addDocument(doc);
		field.StringValue = "bacadaba";
		writer.addDocument(doc);
		field.StringValue = "\uFFFD";
		writer.addDocument(doc);
		field.StringValue = "\uFFFD\uD866\uDF05";
		writer.addDocument(doc);
		field.StringValue = "\uFFFD\uFFFD";
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
	  /// Test that AutomatonQuery interacts with lucene's sort order correctly.
	  /// 
	  /// this expression matches something either starting with the arabic
	  /// presentation forms block, or a supplementary character.
	  /// </summary>
	  public virtual void TestSortOrder()
	  {
		Automaton a = (new RegExp("((\uD866\uDF05)|\uFB94).*")).toAutomaton();
		AssertAutomatonHits(2, a);
	  }
	}

}