using System;
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
	using SortedSetDocValuesField = Lucene.Net.Document.SortedSetDocValuesField;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using AutomatonTestUtil = Lucene.Net.Util.Automaton.AutomatonTestUtil;
	using RegExp = Lucene.Net.Util.Automaton.RegExp;
	using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

	/// <summary>
	/// Tests the DocTermOrdsRewriteMethod
	/// </summary>
	public class TestDocTermOrdsRewriteMethod : LuceneTestCase
	{
	  protected internal IndexSearcher Searcher1;
	  protected internal IndexSearcher Searcher2;
	  private IndexReader Reader;
	  private Directory Dir;
	  protected internal string FieldName;

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
		FieldName = random().nextBoolean() ? "field" : ""; // sometimes use an empty string as field name
		RandomIndexWriter writer = new RandomIndexWriter(random(), Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.KEYWORD, false)).setMaxBufferedDocs(TestUtil.Next(random(), 50, 1000)));
		IList<string> terms = new List<string>();
		int num = atLeast(200);
		for (int i = 0; i < num; i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("id", Convert.ToString(i), Field.Store.NO));
		  int numTerms = random().Next(4);
		  for (int j = 0; j < numTerms; j++)
		  {
			string s = TestUtil.randomUnicodeString(random());
			doc.add(newStringField(FieldName, s, Field.Store.NO));
			// if the default codec doesn't support sortedset, we will uninvert at search time
			if (defaultCodecSupportsSortedSet())
			{
			  doc.add(new SortedSetDocValuesField(FieldName, new BytesRef(s)));
			}
			terms.Add(s);
		  }
		  writer.addDocument(doc);
		}

		if (VERBOSE)
		{
		  // utf16 order
		  terms.Sort();
		  Console.WriteLine("UTF16 order:");
		  foreach (string s in terms)
		  {
			Console.WriteLine("  " + UnicodeUtil.toHexString(s));
		  }
		}

		int numDeletions = random().Next(num / 10);
		for (int i = 0; i < numDeletions; i++)
		{
		  writer.deleteDocuments(new Term("id", Convert.ToString(random().Next(num))));
		}

		Reader = writer.Reader;
		Searcher1 = newSearcher(Reader);
		Searcher2 = newSearcher(Reader);
		writer.close();
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Dir.close();
		base.tearDown();
	  }

	  /// <summary>
	  /// test a bunch of random regular expressions </summary>
	  public virtual void TestRegexps()
	  {
		int num = atLeast(1000);
		for (int i = 0; i < num; i++)
		{
		  string reg = AutomatonTestUtil.randomRegexp(random());
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: regexp=" + reg);
		  }
		  AssertSame(reg);
		}
	  }

	  /// <summary>
	  /// check that the # of hits is the same as if the query
	  /// is run against the inverted index
	  /// </summary>
	  protected internal virtual void AssertSame(string regexp)
	  {
		RegexpQuery docValues = new RegexpQuery(new Term(FieldName, regexp), RegExp.NONE);
		docValues.RewriteMethod = new DocTermOrdsRewriteMethod();
		RegexpQuery inverted = new RegexpQuery(new Term(FieldName, regexp), RegExp.NONE);

		TopDocs invertedDocs = Searcher1.search(inverted, 25);
		TopDocs docValuesDocs = Searcher2.search(docValues, 25);

		CheckHits.checkEqual(inverted, invertedDocs.scoreDocs, docValuesDocs.scoreDocs);
	  }

	  public virtual void TestEquals()
	  {
		RegexpQuery a1 = new RegexpQuery(new Term(FieldName, "[aA]"), RegExp.NONE);
		RegexpQuery a2 = new RegexpQuery(new Term(FieldName, "[aA]"), RegExp.NONE);
		RegexpQuery b = new RegexpQuery(new Term(FieldName, "[bB]"), RegExp.NONE);
		Assert.AreEqual(a1, a2);
		Assert.IsFalse(a1.Equals(b));

		a1.RewriteMethod = new DocTermOrdsRewriteMethod();
		a2.RewriteMethod = new DocTermOrdsRewriteMethod();
		b.RewriteMethod = new DocTermOrdsRewriteMethod();
		Assert.AreEqual(a1, a2);
		Assert.IsFalse(a1.Equals(b));
		QueryUtils.check(a1);
	  }
	}

}