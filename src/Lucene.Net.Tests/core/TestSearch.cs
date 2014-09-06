using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

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


	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	using Lucene.Net.Store;
	using Lucene.Net.Document;
	using Lucene.Net.Analysis;
	using Lucene.Net.Index;
	using Lucene.Net.Search;

	/// <summary>
	/// JUnit adaptation of an older test case SearchTest. </summary>
	public class TestSearch : LuceneTestCase
	{

	  public virtual void TestNegativeQueryBoost()
	  {
		Query q = new TermQuery(new Term("foo", "bar"));
		q.Boost = -42f;
		Assert.AreEqual(-42f, q.Boost, 0.0f);

		Directory directory = newDirectory();
		try
		{
		  Analyzer analyzer = new MockAnalyzer(random());
		  IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);

		  IndexWriter writer = new IndexWriter(directory, conf);
		  try
		  {
			Document d = new Document();
			d.add(newTextField("foo", "bar", Field.Store.YES));
			writer.addDocument(d);
		  }
		  finally
		  {
			writer.close();
		  }

		  IndexReader reader = DirectoryReader.open(directory);
		  try
		  {
			IndexSearcher searcher = newSearcher(reader);

			ScoreDoc[] hits = searcher.search(q, null, 1000).scoreDocs;
			Assert.AreEqual(1, hits.Length);
			Assert.IsTrue("score is not negative: " + hits[0].score, hits[0].score < 0);

			Explanation explain = searcher.explain(q, hits[0].doc);
			Assert.AreEqual("score doesn't match explanation", hits[0].score, explain.Value, 0.001f);
			Assert.IsTrue("explain doesn't think doc is a match", explain.Match);

		  }
		  finally
		  {
			reader.close();
		  }
		}
		finally
		{
		  directory.close();
		}

	  }

		/// <summary>
		/// this test performs a number of searches. It also compares output
		///  of searches using multi-file index segments with single-file
		///  index segments.
		/// 
		///  TODO: someone should check that the results of the searches are
		///        still correct by adding assert statements. Right now, the test
		///        passes if the results are the same between multi-file and
		///        single-file formats, even if the results are wrong.
		/// </summary>
		public virtual void TestSearch()
		{
		  StringWriter sw = new StringWriter();
		  PrintWriter pw = new PrintWriter(sw, true);
		  DoTestSearch(random(), pw, false);
		  pw.close();
		  sw.close();
		  string multiFileOutput = sw.ToString();
		  //System.out.println(multiFileOutput);

		  sw = new StringWriter();
		  pw = new PrintWriter(sw, true);
		  DoTestSearch(random(), pw, true);
		  pw.close();
		  sw.close();
		  string singleFileOutput = sw.ToString();

		  Assert.AreEqual(multiFileOutput, singleFileOutput);
		}


		private void DoTestSearch(Random random, PrintWriter @out, bool useCompoundFile)
		{
		  Directory directory = newDirectory();
		  Analyzer analyzer = new MockAnalyzer(random);
		  IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		  MergePolicy mp = conf.MergePolicy;
		  mp.NoCFSRatio = useCompoundFile ? 1.0 : 0.0;
		  IndexWriter writer = new IndexWriter(directory, conf);

		  string[] docs = new string[] {"a b c d e", "a b c d e a b c d e", "a b c d e f g h i j", "a c e", "e c a", "a c e a c e", "a c e a b c"};
		  for (int j = 0; j < docs.Length; j++)
		  {
			Document d = new Document();
			d.add(newTextField("contents", docs[j], Field.Store.YES));
			d.add(newStringField("id", "" + j, Field.Store.NO));
			writer.addDocument(d);
		  }
		  writer.close();

		  IndexReader reader = DirectoryReader.open(directory);
		  IndexSearcher searcher = newSearcher(reader);

		  ScoreDoc[] hits = null;

		  Sort sort = new Sort(SortField.FIELD_SCORE, new SortField("id", SortField.Type.INT));

		  foreach (Query query in BuildQueries())
		  {
			@out.println("Query: " + query.ToString("contents"));
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: query=" + query);
			}

			hits = searcher.search(query, null, 1000, sort).scoreDocs;

			@out.println(hits.Length + " total results");
			for (int i = 0 ; i < hits.Length && i < 10; i++)
			{
			  Document d = searcher.doc(hits[i].doc);
			  @out.println(i + " " + hits[i].score + " " + d.get("contents"));
			}
		  }
		  reader.close();
		  directory.close();
		}

	  private IList<Query> BuildQueries()
	  {
		IList<Query> queries = new List<Query>();

		BooleanQuery booleanAB = new BooleanQuery();
		booleanAB.add(new TermQuery(new Term("contents", "a")), BooleanClause.Occur_e.SHOULD);
		booleanAB.add(new TermQuery(new Term("contents", "b")), BooleanClause.Occur_e.SHOULD);
		queries.Add(booleanAB);

		PhraseQuery phraseAB = new PhraseQuery();
		phraseAB.add(new Term("contents", "a"));
		phraseAB.add(new Term("contents", "b"));
		queries.Add(phraseAB);

		PhraseQuery phraseABC = new PhraseQuery();
		phraseABC.add(new Term("contents", "a"));
		phraseABC.add(new Term("contents", "b"));
		phraseABC.add(new Term("contents", "c"));
		queries.Add(phraseABC);

		BooleanQuery booleanAC = new BooleanQuery();
		booleanAC.add(new TermQuery(new Term("contents", "a")), BooleanClause.Occur_e.SHOULD);
		booleanAC.add(new TermQuery(new Term("contents", "c")), BooleanClause.Occur_e.SHOULD);
		queries.Add(booleanAC);

		PhraseQuery phraseAC = new PhraseQuery();
		phraseAC.add(new Term("contents", "a"));
		phraseAC.add(new Term("contents", "c"));
		queries.Add(phraseAC);

		PhraseQuery phraseACE = new PhraseQuery();
		phraseACE.add(new Term("contents", "a"));
		phraseACE.add(new Term("contents", "c"));
		phraseACE.add(new Term("contents", "e"));
		queries.Add(phraseACE);

		return queries;
	  }
	}

}