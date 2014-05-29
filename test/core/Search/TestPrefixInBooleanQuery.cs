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
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;


	/// <summary>
	/// https://issues.apache.org/jira/browse/LUCENE-1974
	/// 
	/// represent the bug of 
	/// 
	///    BooleanScorer.score(Collector collector, int max, int firstDocID)
	/// 
	/// Line 273, end=8192, subScorerDocID=11378, then more got false?
	/// </summary>
	public class TestPrefixInBooleanQuery : LuceneTestCase
	{

	  private const string FIELD = "name";
	  private static Directory Directory;
	  private static IndexReader Reader;
	  private static IndexSearcher Searcher;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		Directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory);

		Document doc = new Document();
		Field field = newStringField(FIELD, "meaninglessnames", Field.Store.NO);
		doc.add(field);

		for (int i = 0; i < 5137; ++i)
		{
		  writer.addDocument(doc);
		}

		field.StringValue = "tangfulin";
		writer.addDocument(doc);

		field.StringValue = "meaninglessnames";
		for (int i = 5138; i < 11377; ++i)
		{
		  writer.addDocument(doc);
		}

		field.StringValue = "tangfulin";
		writer.addDocument(doc);

		Reader = writer.Reader;
		Searcher = newSearcher(Reader);
		writer.close();
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

	  public virtual void TestPrefixQuery()
	  {
		Query query = new PrefixQuery(new Term(FIELD, "tang"));
		Assert.AreEqual("Number of matched documents", 2, Searcher.search(query, null, 1000).totalHits);
	  }
	  public virtual void TestTermQuery()
	  {
		Query query = new TermQuery(new Term(FIELD, "tangfulin"));
		Assert.AreEqual("Number of matched documents", 2, Searcher.search(query, null, 1000).totalHits);
	  }
	  public virtual void TestTermBooleanQuery()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term(FIELD, "tangfulin")), BooleanClause.Occur_e.SHOULD);
		query.add(new TermQuery(new Term(FIELD, "notexistnames")), BooleanClause.Occur_e.SHOULD);
		Assert.AreEqual("Number of matched documents", 2, Searcher.search(query, null, 1000).totalHits);

	  }
	  public virtual void TestPrefixBooleanQuery()
	  {
		BooleanQuery query = new BooleanQuery();
		query.add(new PrefixQuery(new Term(FIELD, "tang")), BooleanClause.Occur_e.SHOULD);
		query.add(new TermQuery(new Term(FIELD, "notexistnames")), BooleanClause.Occur_e.SHOULD);
		Assert.AreEqual("Number of matched documents", 2, Searcher.search(query, null, 1000).totalHits);
	  }
	}

}