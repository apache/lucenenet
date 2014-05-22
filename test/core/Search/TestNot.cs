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
	using Term = Lucene.Net.Index.Term;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Directory = Lucene.Net.Store.Directory;
	using Document = Lucene.Net.Document.Document;

	/// <summary>
	/// Similarity unit test.
	/// 
	/// 
	/// </summary>
	public class TestNot : LuceneTestCase
	{

	  public virtual void TestNot()
	  {
		Directory store = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), store);

		Document d1 = new Document();
		d1.add(newTextField("field", "a b", Field.Store.YES));

		writer.addDocument(d1);
		IndexReader reader = writer.Reader;

		IndexSearcher searcher = newSearcher(reader);

		BooleanQuery query = new BooleanQuery();
		query.add(new TermQuery(new Term("field", "a")), BooleanClause.Occur.SHOULD);
		query.add(new TermQuery(new Term("field", "b")), BooleanClause.Occur.MUST_NOT);

		ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);
		writer.close();
		reader.close();
		store.close();
	  }
	}

}