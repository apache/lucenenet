using System.Collections.Generic;

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

	using Document = Lucene.Net.Document.Document;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Directory = Lucene.Net.Store.Directory;


	/// <summary>
	/// A basic unit test for FieldCacheTermsFilter
	/// </summary>
	/// <seealso cref= Lucene.Net.Search.FieldCacheTermsFilter </seealso>
	public class TestFieldCacheTermsFilter : LuceneTestCase
	{
	  public virtual void TestMissingTerms()
	  {
		string fieldName = "field1";
		Directory rd = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), rd);
		for (int i = 0; i < 100; i++)
		{
		  Document doc = new Document();
		  int term = i * 10; //terms are units of 10;
		  doc.add(newStringField(fieldName, "" + term, Field.Store.YES));
		  w.addDocument(doc);
		}
		IndexReader reader = w.Reader;
		w.close();

		IndexSearcher searcher = newSearcher(reader);
		int numDocs = reader.numDocs();
		ScoreDoc[] results;
		MatchAllDocsQuery q = new MatchAllDocsQuery();

		IList<string> terms = new List<string>();
		terms.Add("5");
		results = searcher.search(q, new FieldCacheTermsFilter(fieldName, terms.ToArray()), numDocs).scoreDocs;
		Assert.AreEqual("Must match nothing", 0, results.Length);

		terms = new List<>();
		terms.Add("10");
		results = searcher.search(q, new FieldCacheTermsFilter(fieldName, terms.ToArray()), numDocs).scoreDocs;
		Assert.AreEqual("Must match 1", 1, results.Length);

		terms = new List<>();
		terms.Add("10");
		terms.Add("20");
		results = searcher.search(q, new FieldCacheTermsFilter(fieldName, terms.ToArray()), numDocs).scoreDocs;
		Assert.AreEqual("Must match 2", 2, results.Length);

		reader.close();
		rd.close();
	  }
	}

}