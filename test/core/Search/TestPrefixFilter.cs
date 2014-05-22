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

	using Field = Lucene.Net.Document.Field;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using Directory = Lucene.Net.Store.Directory;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Document = Lucene.Net.Document.Document;

	/// <summary>
	/// Tests <seealso cref="PrefixFilter"/> class.
	/// 
	/// </summary>
	public class TestPrefixFilter : LuceneTestCase
	{
	  public virtual void TestPrefixFilter()
	  {
		Directory directory = newDirectory();

		string[] categories = new string[] {"/Computers/Linux", "/Computers/Mac/One", "/Computers/Mac/Two", "/Computers/Windows"};
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory);
		for (int i = 0; i < categories.Length; i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("category", categories[i], Field.Store.YES));
		  writer.addDocument(doc);
		}
		IndexReader reader = writer.Reader;

		// PrefixFilter combined with ConstantScoreQuery
		PrefixFilter filter = new PrefixFilter(new Term("category", "/Computers"));
		Query query = new ConstantScoreQuery(filter);
		IndexSearcher searcher = newSearcher(reader);
		ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(4, hits.Length);

		// test middle of values
		filter = new PrefixFilter(new Term("category", "/Computers/Mac"));
		query = new ConstantScoreQuery(filter);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(2, hits.Length);

		// test start of values
		filter = new PrefixFilter(new Term("category", "/Computers/Linux"));
		query = new ConstantScoreQuery(filter);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);

		// test end of values
		filter = new PrefixFilter(new Term("category", "/Computers/Windows"));
		query = new ConstantScoreQuery(filter);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);

		// test non-existant
		filter = new PrefixFilter(new Term("category", "/Computers/ObsoleteOS"));
		query = new ConstantScoreQuery(filter);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);

		// test non-existant, before values
		filter = new PrefixFilter(new Term("category", "/Computers/AAA"));
		query = new ConstantScoreQuery(filter);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);

		// test non-existant, after values
		filter = new PrefixFilter(new Term("category", "/Computers/ZZZ"));
		query = new ConstantScoreQuery(filter);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);

		// test zero length prefix
		filter = new PrefixFilter(new Term("category", ""));
		query = new ConstantScoreQuery(filter);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(4, hits.Length);

		// test non existent field
		filter = new PrefixFilter(new Term("nonexistantfield", "/Computers"));
		query = new ConstantScoreQuery(filter);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);

		writer.close();
		reader.close();
		directory.close();
	  }
	}

}