namespace org.apache.lucene
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
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using Term = Lucene.Net.Index.Term;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Lucene.Net.Search;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	/// <summary>
	/// A very simple demo used in the API documentation (src/java/overview.html).
	/// 
	/// Please try to keep src/java/overview.html up-to-date when making changes
	/// to this class.
	/// </summary>
	public class TestDemo : LuceneTestCase
	{

	  public virtual void TestDemo()
	  {
		Analyzer analyzer = new MockAnalyzer(random());

		// Store the index in memory:
		Directory directory = newDirectory();
		// To store an index on disk, use this instead:
		// Directory directory = FSDirectory.open(new File("/tmp/testindex"));
		RandomIndexWriter iwriter = new RandomIndexWriter(random(), directory, analyzer);
		Document doc = new Document();
		string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
		string text = "this is the text to be indexed. " + longTerm;
		doc.add(newTextField("fieldname", text, Field.Store.YES));
		iwriter.addDocument(doc);
		iwriter.close();

		// Now search the index:
		IndexReader ireader = DirectoryReader.open(directory); // read-only=true
		IndexSearcher isearcher = newSearcher(ireader);

		Assert.AreEqual(1, isearcher.search(new TermQuery(new Term("fieldname", longTerm)), 1).totalHits);
		Query query = new TermQuery(new Term("fieldname", "text"));
		TopDocs hits = isearcher.search(query, null, 1);
		Assert.AreEqual(1, hits.totalHits);
		// Iterate through the results:
		for (int i = 0; i < hits.scoreDocs.length; i++)
		{
		  Document hitDoc = isearcher.doc(hits.scoreDocs[i].doc);
		  Assert.AreEqual(text, hitDoc.get("fieldname"));
		}

		// Test simple phrase query
		PhraseQuery phraseQuery = new PhraseQuery();
		phraseQuery.add(new Term("fieldname", "to"));
		phraseQuery.add(new Term("fieldname", "be"));
		Assert.AreEqual(1, isearcher.search(phraseQuery, null, 1).totalHits);

		ireader.close();
		directory.close();
	  }
	}

}