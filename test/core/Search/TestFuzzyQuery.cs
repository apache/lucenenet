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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using MultiReader = Lucene.Net.Index.MultiReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	/// <summary>
	/// Tests <seealso cref="FuzzyQuery"/>.
	/// 
	/// </summary>
	public class TestFuzzyQuery : LuceneTestCase
	{

	  public virtual void TestFuzziness()
	  {
		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory);
		AddDoc("aaaaa", writer);
		AddDoc("aaaab", writer);
		AddDoc("aaabb", writer);
		AddDoc("aabbb", writer);
		AddDoc("abbbb", writer);
		AddDoc("bbbbb", writer);
		AddDoc("ddddd", writer);

		IndexReader reader = writer.Reader;
		IndexSearcher searcher = newSearcher(reader);
		writer.close();

		FuzzyQuery query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits, 0);
		ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(3, hits.Length);

		// same with prefix
		query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits, 1);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(3, hits.Length);
		query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits, 2);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(3, hits.Length);
		query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits, 3);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(3, hits.Length);
		query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits, 4);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(2, hits.Length);
		query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits, 5);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits, 6);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);

		// test scoring
		query = new FuzzyQuery(new Term("field", "bbbbb"), FuzzyQuery.defaultMaxEdits, 0);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual("3 documents should match", 3, hits.Length);
		IList<string> order = Arrays.asList("bbbbb","abbbb","aabbb");
		for (int i = 0; i < hits.Length; i++)
		{
		  string term = searcher.doc(hits[i].doc).get("field");
		  //System.out.println(hits[i].score);
		  Assert.AreEqual(order[i], term);
		}

		// test pq size by supplying maxExpansions=2
		// this query would normally return 3 documents, because 3 terms match (see above):
		query = new FuzzyQuery(new Term("field", "bbbbb"), FuzzyQuery.defaultMaxEdits, 0, 2, false);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual("only 2 documents should match", 2, hits.Length);
		order = Arrays.asList("bbbbb","abbbb");
		for (int i = 0; i < hits.Length; i++)
		{
		  string term = searcher.doc(hits[i].doc).get("field");
		  //System.out.println(hits[i].score);
		  Assert.AreEqual(order[i], term);
		}

		// not similar enough:
		query = new FuzzyQuery(new Term("field", "xxxxx"), FuzzyQuery.defaultMaxEdits, 0);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);
		query = new FuzzyQuery(new Term("field", "aaccc"), FuzzyQuery.defaultMaxEdits, 0); // edit distance to "aaaaa" = 3
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);

		// query identical to a word in the index:
		query = new FuzzyQuery(new Term("field", "aaaaa"), FuzzyQuery.defaultMaxEdits, 0);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(3, hits.Length);
		Assert.AreEqual(searcher.doc(hits[0].doc).get("field"), ("aaaaa"));
		// default allows for up to two edits:
		Assert.AreEqual(searcher.doc(hits[1].doc).get("field"), ("aaaab"));
		Assert.AreEqual(searcher.doc(hits[2].doc).get("field"), ("aaabb"));

		// query similar to a word in the index:
		query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.defaultMaxEdits, 0);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(3, hits.Length);
		Assert.AreEqual(searcher.doc(hits[0].doc).get("field"), ("aaaaa"));
		Assert.AreEqual(searcher.doc(hits[1].doc).get("field"), ("aaaab"));
		Assert.AreEqual(searcher.doc(hits[2].doc).get("field"), ("aaabb"));

		// now with prefix
		query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.defaultMaxEdits, 1);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(3, hits.Length);
		Assert.AreEqual(searcher.doc(hits[0].doc).get("field"), ("aaaaa"));
		Assert.AreEqual(searcher.doc(hits[1].doc).get("field"), ("aaaab"));
		Assert.AreEqual(searcher.doc(hits[2].doc).get("field"), ("aaabb"));
		query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.defaultMaxEdits, 2);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(3, hits.Length);
		Assert.AreEqual(searcher.doc(hits[0].doc).get("field"), ("aaaaa"));
		Assert.AreEqual(searcher.doc(hits[1].doc).get("field"), ("aaaab"));
		Assert.AreEqual(searcher.doc(hits[2].doc).get("field"), ("aaabb"));
		query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.defaultMaxEdits, 3);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(3, hits.Length);
		Assert.AreEqual(searcher.doc(hits[0].doc).get("field"), ("aaaaa"));
		Assert.AreEqual(searcher.doc(hits[1].doc).get("field"), ("aaaab"));
		Assert.AreEqual(searcher.doc(hits[2].doc).get("field"), ("aaabb"));
		query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.defaultMaxEdits, 4);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(2, hits.Length);
		Assert.AreEqual(searcher.doc(hits[0].doc).get("field"), ("aaaaa"));
		Assert.AreEqual(searcher.doc(hits[1].doc).get("field"), ("aaaab"));
		query = new FuzzyQuery(new Term("field", "aaaac"), FuzzyQuery.defaultMaxEdits, 5);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);


		query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.defaultMaxEdits, 0);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		Assert.AreEqual(searcher.doc(hits[0].doc).get("field"), ("ddddd"));

		// now with prefix
		query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.defaultMaxEdits, 1);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		Assert.AreEqual(searcher.doc(hits[0].doc).get("field"), ("ddddd"));
		query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.defaultMaxEdits, 2);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		Assert.AreEqual(searcher.doc(hits[0].doc).get("field"), ("ddddd"));
		query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.defaultMaxEdits, 3);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		Assert.AreEqual(searcher.doc(hits[0].doc).get("field"), ("ddddd"));
		query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.defaultMaxEdits, 4);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		Assert.AreEqual(searcher.doc(hits[0].doc).get("field"), ("ddddd"));
		query = new FuzzyQuery(new Term("field", "ddddX"), FuzzyQuery.defaultMaxEdits, 5);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);


		// different field = no match:
		query = new FuzzyQuery(new Term("anotherfield", "ddddX"), FuzzyQuery.defaultMaxEdits, 0);
		hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(0, hits.Length);

		reader.close();
		directory.close();
	  }

	  public virtual void Test2()
	  {
		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory, new MockAnalyzer(random(), MockTokenizer.KEYWORD, false));
		AddDoc("LANGE", writer);
		AddDoc("LUETH", writer);
		AddDoc("PIRSING", writer);
		AddDoc("RIEGEL", writer);
		AddDoc("TRZECZIAK", writer);
		AddDoc("WALKER", writer);
		AddDoc("WBR", writer);
		AddDoc("WE", writer);
		AddDoc("WEB", writer);
		AddDoc("WEBE", writer);
		AddDoc("WEBER", writer);
		AddDoc("WEBERE", writer);
		AddDoc("WEBREE", writer);
		AddDoc("WEBEREI", writer);
		AddDoc("WBRE", writer);
		AddDoc("WITTKOPF", writer);
		AddDoc("WOJNAROWSKI", writer);
		AddDoc("WRICKE", writer);

		IndexReader reader = writer.Reader;
		IndexSearcher searcher = newSearcher(reader);
		writer.close();

		FuzzyQuery query = new FuzzyQuery(new Term("field", "WEBER"), 2, 1);
		//query.setRewriteMethod(FuzzyQuery.SCORING_BOOLEAN_QUERY_REWRITE);
		ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(8, hits.Length);

		reader.close();
		directory.close();
	  }

	  /// <summary>
	  /// MultiTermQuery provides (via attribute) information about which values
	  /// must be competitive to enter the priority queue. 
	  /// 
	  /// FuzzyQuery optimizes itself around this information, if the attribute
	  /// is not implemented correctly, there will be problems!
	  /// </summary>
	  public virtual void TestTieBreaker()
	  {
		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory);
		AddDoc("a123456", writer);
		AddDoc("c123456", writer);
		AddDoc("d123456", writer);
		AddDoc("e123456", writer);

		Directory directory2 = newDirectory();
		RandomIndexWriter writer2 = new RandomIndexWriter(random(), directory2);
		AddDoc("a123456", writer2);
		AddDoc("b123456", writer2);
		AddDoc("b123456", writer2);
		AddDoc("b123456", writer2);
		AddDoc("c123456", writer2);
		AddDoc("f123456", writer2);

		IndexReader ir1 = writer.Reader;
		IndexReader ir2 = writer2.Reader;

		MultiReader mr = new MultiReader(ir1, ir2);
		IndexSearcher searcher = newSearcher(mr);
		FuzzyQuery fq = new FuzzyQuery(new Term("field", "z123456"), 1, 0, 2, false);
		TopDocs docs = searcher.search(fq, 2);
		Assert.AreEqual(5, docs.totalHits); // 5 docs, from the a and b's
		mr.close();
		ir1.close();
		ir2.close();
		writer.close();
		writer2.close();
		directory.close();
		directory2.close();
	  }

	  /// <summary>
	  /// Test the TopTermsBoostOnlyBooleanQueryRewrite rewrite method. </summary>
	  public virtual void TestBoostOnlyRewrite()
	  {
		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory);
		AddDoc("Lucene", writer);
		AddDoc("Lucene", writer);
		AddDoc("Lucenne", writer);

		IndexReader reader = writer.Reader;
		IndexSearcher searcher = newSearcher(reader);
		writer.close();

		FuzzyQuery query = new FuzzyQuery(new Term("field", "lucene"));
		query.RewriteMethod = new MultiTermQuery.TopTermsBoostOnlyBooleanQueryRewrite(50);
		ScoreDoc[] hits = searcher.search(query, null, 1000).scoreDocs;
		Assert.AreEqual(3, hits.Length);
		// normally, 'Lucenne' would be the first result as IDF will skew the score.
		Assert.AreEqual("Lucene", reader.document(hits[0].doc).get("field"));
		Assert.AreEqual("Lucene", reader.document(hits[1].doc).get("field"));
		Assert.AreEqual("Lucenne", reader.document(hits[2].doc).get("field"));
		reader.close();
		directory.close();
	  }

	  public virtual void TestGiga()
	  {

		MockAnalyzer analyzer = new MockAnalyzer(random());
		Directory index = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), index);

		AddDoc("Lucene in Action", w);
		AddDoc("Lucene for Dummies", w);

		//addDoc("Giga", w);
		AddDoc("Giga byte", w);

		AddDoc("ManagingGigabytesManagingGigabyte", w);
		AddDoc("ManagingGigabytesManagingGigabytes", w);

		AddDoc("The Art of Computer Science", w);
		AddDoc("J. K. Rowling", w);
		AddDoc("JK Rowling", w);
		AddDoc("Joanne K Roling", w);
		AddDoc("Bruce Willis", w);
		AddDoc("Willis bruce", w);
		AddDoc("Brute willis", w);
		AddDoc("B. willis", w);
		IndexReader r = w.Reader;
		w.close();

		Query q = new FuzzyQuery(new Term("field", "giga"), 0);

		// 3. search
		IndexSearcher searcher = newSearcher(r);
		ScoreDoc[] hits = searcher.search(q, 10).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		Assert.AreEqual("Giga byte", searcher.doc(hits[0].doc).get("field"));
		r.close();
		index.close();
	  }

	  public virtual void TestDistanceAsEditsSearching()
	  {
		Directory index = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), index);
		AddDoc("foobar", w);
		AddDoc("test", w);
		AddDoc("working", w);
		IndexReader reader = w.Reader;
		IndexSearcher searcher = newSearcher(reader);
		w.close();

		FuzzyQuery q = new FuzzyQuery(new Term("field", "fouba"), 2);
		ScoreDoc[] hits = searcher.search(q, 10).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		Assert.AreEqual("foobar", searcher.doc(hits[0].doc).get("field"));

		q = new FuzzyQuery(new Term("field", "foubara"), 2);
		hits = searcher.search(q, 10).scoreDocs;
		Assert.AreEqual(1, hits.Length);
		Assert.AreEqual("foobar", searcher.doc(hits[0].doc).get("field"));

		try
		{
		  q = new FuzzyQuery(new Term("field", "t"), 3);
		  Assert.Fail();
		}
		catch (System.ArgumentException expected)
		{
		  // expected
		}

		reader.close();
		index.close();
	  }

	  private void AddDoc(string text, RandomIndexWriter writer)
	  {
		Document doc = new Document();
		doc.add(newTextField("field", text, Field.Store.YES));
		writer.addDocument(doc);
	  }
	}

}