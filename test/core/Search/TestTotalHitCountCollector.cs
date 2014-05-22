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
	using StringField = Lucene.Net.Document.StringField;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestTotalHitCountCollector : LuceneTestCase
	{

	  public virtual void TestBasics()
	  {
		Directory indexStore = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), indexStore);
		for (int i = 0; i < 5; i++)
		{
		  Document doc = new Document();
		  doc.add(new StringField("string", "a" + i, Field.Store.NO));
		  doc.add(new StringField("string", "b" + i, Field.Store.NO));
		  writer.addDocument(doc);
		}
		IndexReader reader = writer.Reader;
		writer.close();

		IndexSearcher searcher = newSearcher(reader);
		TotalHitCountCollector c = new TotalHitCountCollector();
		searcher.search(new MatchAllDocsQuery(), null, c);
		Assert.AreEqual(5, c.TotalHits);
		reader.close();
		indexStore.close();
	  }
	}

}