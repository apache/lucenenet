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
namespace Lucene.Net.Search
{

	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using Term = Lucene.Net.Index.Term;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using Directory = Lucene.Net.Store.Directory;

	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	/// <summary>
	/// Tests MatchAllDocsQuery.
	/// 
	/// </summary>
	public class TestMatchAllDocsQuery : LuceneTestCase
	{
	  private Analyzer Analyzer;

	  public override void SetUp()
	  {
		base.setUp();
		Analyzer = new MockAnalyzer(random());
	  }

	  public virtual void TestQuery()
	  {
		Directory dir = newDirectory();
		IndexWriter iw = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, Analyzer).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy()));
		AddDoc("one", iw, 1f);
		AddDoc("two", iw, 20f);
		AddDoc("three four", iw, 300f);
		IndexReader ir = DirectoryReader.open(iw, true);

		IndexSearcher @is = newSearcher(ir);
		ScoreDoc[] hits;

		hits = @is.search(new MatchAllDocsQuery(), null, 1000).scoreDocs;
		Assert.AreEqual(3, hits.Length);
		Assert.AreEqual("one", @is.doc(hits[0].doc).get("key"));
		Assert.AreEqual("two", @is.doc(hits[1].doc).get("key"));
		Assert.AreEqual("three four", @is.doc(hits[2].doc).get("key"));

		// some artificial queries to trigger the use of skipTo():

		BooleanQuery bq = new BooleanQuery();
		bq.add(new MatchAllDocsQuery(), BooleanClause.Occur_e.MUST);
		bq.add(new MatchAllDocsQuery(), BooleanClause.Occur_e.MUST);
		hits = @is.search(bq, null, 1000).scoreDocs;
		Assert.AreEqual(3, hits.Length);

		bq = new BooleanQuery();
		bq.add(new MatchAllDocsQuery(), BooleanClause.Occur_e.MUST);
		bq.add(new TermQuery(new Term("key", "three")), BooleanClause.Occur_e.MUST);
		hits = @is.search(bq, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);

		iw.deleteDocuments(new Term("key", "one"));
		ir.close();
		ir = DirectoryReader.open(iw, true);
		@is = newSearcher(ir);

		hits = @is.search(new MatchAllDocsQuery(), null, 1000).scoreDocs;
		Assert.AreEqual(2, hits.Length);

		iw.close();
		ir.close();
		dir.close();
	  }

	  public virtual void TestEquals()
	  {
		Query q1 = new MatchAllDocsQuery();
		Query q2 = new MatchAllDocsQuery();
		Assert.IsTrue(q1.Equals(q2));
		q1.Boost = 1.5f;
		Assert.IsFalse(q1.Equals(q2));
	  }

	  private void AddDoc(string text, IndexWriter iw, float boost)
	  {
		Document doc = new Document();
		Field f = newTextField("key", text, Field.Store.YES);
		f.Boost = boost;
		doc.add(f);
		iw.addDocument(doc);
	  }

	}

}