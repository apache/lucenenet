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
	using Lucene.Net.Document;
	using Lucene.Net.Index;
	using Occur = Lucene.Net.Search.BooleanClause.Occur_e;
	using ChildScorer = Lucene.Net.Search.Scorer.ChildScorer;
	using Lucene.Net.Store;
	using Lucene.Net.Util;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;
	using Test = org.junit.Test;

	public class TestSubScorerFreqs : LuceneTestCase
	{

	  private static Directory Dir;
	  private static IndexSearcher s;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void makeIndex() throws Exception
	  public static void MakeIndex()
	  {
		Dir = new RAMDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
		// make sure we have more than one segment occationally
		int num = atLeast(31);
		for (int i = 0; i < num; i++)
		{
		  Document doc = new Document();
		  doc.add(newTextField("f", "a b c d b c d c d d", Field.Store.NO));
		  w.addDocument(doc);

		  doc = new Document();
		  doc.add(newTextField("f", "a b c d", Field.Store.NO));
		  w.addDocument(doc);
		}

		s = newSearcher(w.Reader);
		w.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void finish() throws Exception
	  public static void Finish()
	  {
		s.IndexReader.close();
		s = null;
		Dir.close();
		Dir = null;
	  }

	  private class CountingCollector : Collector
	  {
		internal readonly Collector Other;
		internal int DocBase;

		public readonly IDictionary<int?, IDictionary<Query, float?>> DocCounts = new Dictionary<int?, IDictionary<Query, float?>>();

		internal readonly IDictionary<Query, Scorer> SubScorers = new Dictionary<Query, Scorer>();
		internal readonly Set<string> Relationships;

		public CountingCollector(Collector other) : this(other, new HashSet<>("MUST", "SHOULD", "MUST_NOT"))
		{
		}

		public CountingCollector(Collector other, Set<string> relationships)
		{
		  this.Other = other;
		  this.Relationships = relationships;
		}

		public override Scorer Scorer
		{
			set
			{
			  Other.Scorer = value;
			  SubScorers.Clear();
			  SetSubScorers(value, "TOP");
			}
		}

		public virtual void SetSubScorers(Scorer scorer, string relationship)
		{
		  foreach (ChildScorer child in scorer.Children)
		  {
			if (scorer is AssertingScorer || Relationships.contains(child.relationship))
			{
			  SetSubScorers(child.child, child.relationship);
			}
		  }
		  SubScorers[scorer.Weight.Query] = scorer;
		}

		public override void Collect(int doc)
		{
		  IDictionary<Query, float?> freqs = new Dictionary<Query, float?>();
		  foreach (KeyValuePair<Query, Scorer> ent in SubScorers)
		  {
			Scorer value = ent.Value;
			int matchId = value.docID();
			freqs[ent.Key] = matchId == doc ? value.freq() : 0.0f;
		  }
		  DocCounts[doc + DocBase] = freqs;
		  Other.collect(doc);
		}

		public override AtomicReaderContext NextReader
		{
			set
			{
			  DocBase = value.docBase;
			  Other.NextReader = value;
			}
		}

		public override bool AcceptsDocsOutOfOrder()
		{
		  return Other.acceptsDocsOutOfOrder();
		}
	  }

	  private const float FLOAT_TOLERANCE = 0.00001F;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testTermQuery() throws Exception
	  public virtual void TestTermQuery()
	  {
		TermQuery q = new TermQuery(new Term("f", "d"));
		CountingCollector c = new CountingCollector(TopScoreDocCollector.create(10, true));
		s.search(q, null, c);
		int maxDocs = s.IndexReader.maxDoc();
		Assert.AreEqual(maxDocs, c.DocCounts.Count);
		for (int i = 0; i < maxDocs; i++)
		{
		  IDictionary<Query, float?> doc0 = c.DocCounts[i];
		  Assert.AreEqual(1, doc0.Count);
		  Assert.AreEqual(4.0F, doc0[q], FLOAT_TOLERANCE);

		  IDictionary<Query, float?> doc1 = c.DocCounts[++i];
		  Assert.AreEqual(1, doc1.Count);
		  Assert.AreEqual(1.0F, doc1[q], FLOAT_TOLERANCE);
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testBooleanQuery() throws Exception
	  public virtual void TestBooleanQuery()
	  {
		TermQuery aQuery = new TermQuery(new Term("f", "a"));
		TermQuery dQuery = new TermQuery(new Term("f", "d"));
		TermQuery cQuery = new TermQuery(new Term("f", "c"));
		TermQuery yQuery = new TermQuery(new Term("f", "y"));

		BooleanQuery query = new BooleanQuery();
		BooleanQuery inner = new BooleanQuery();

		inner.add(cQuery, Occur.SHOULD);
		inner.add(yQuery, Occur.MUST_NOT);
		query.add(inner, Occur.MUST);
		query.add(aQuery, Occur.MUST);
		query.add(dQuery, Occur.MUST);

		// Only needed in Java6; Java7+ has a @SafeVarargs annotated Arrays#asList()!
		// see http://docs.oracle.com/javase/7/docs/api/java/lang/SafeVarargs.html
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unchecked") final Iterable<Set<String>> occurList = Arrays.asList(Collections.singleton("MUST"), new HashSet<>(Arrays.asList("MUST", "SHOULD"))
		IEnumerable<Set<string>> occurList = Arrays.asList(Collections.singleton("MUST"), new HashSet<Set<string>>("MUST", "SHOULD")
	   );

		foreach (Set<string> occur in occurList)
		{
		  CountingCollector c = new CountingCollector(TopScoreDocCollector.create(10, true), occur);
		  s.search(query, null, c);
		  int maxDocs = s.IndexReader.maxDoc();
		  Assert.AreEqual(maxDocs, c.DocCounts.Count);
		  bool includeOptional = occur.contains("SHOULD");
		  for (int i = 0; i < maxDocs; i++)
		  {
			IDictionary<Query, float?> doc0 = c.DocCounts[i];
			Assert.AreEqual(includeOptional ? 5 : 4, doc0.Count);
			Assert.AreEqual(1.0F, doc0[aQuery], FLOAT_TOLERANCE);
			Assert.AreEqual(4.0F, doc0[dQuery], FLOAT_TOLERANCE);
			if (includeOptional)
			{
			  Assert.AreEqual(3.0F, doc0[cQuery], FLOAT_TOLERANCE);
			}

			IDictionary<Query, float?> doc1 = c.DocCounts[++i];
			Assert.AreEqual(includeOptional ? 5 : 4, doc1.Count);
			Assert.AreEqual(1.0F, doc1[aQuery], FLOAT_TOLERANCE);
			Assert.AreEqual(1.0F, doc1[dQuery], FLOAT_TOLERANCE);
			if (includeOptional)
			{
			  Assert.AreEqual(1.0F, doc1[cQuery], FLOAT_TOLERANCE);
			}
		  }
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testPhraseQuery() throws Exception
	  public virtual void TestPhraseQuery()
	  {
		PhraseQuery q = new PhraseQuery();
		q.add(new Term("f", "b"));
		q.add(new Term("f", "c"));
		CountingCollector c = new CountingCollector(TopScoreDocCollector.create(10, true));
		s.search(q, null, c);
		int maxDocs = s.IndexReader.maxDoc();
		Assert.AreEqual(maxDocs, c.DocCounts.Count);
		for (int i = 0; i < maxDocs; i++)
		{
		  IDictionary<Query, float?> doc0 = c.DocCounts[i];
		  Assert.AreEqual(1, doc0.Count);
		  Assert.AreEqual(2.0F, doc0[q], FLOAT_TOLERANCE);

		  IDictionary<Query, float?> doc1 = c.DocCounts[++i];
		  Assert.AreEqual(1, doc1.Count);
		  Assert.AreEqual(1.0F, doc1[q], FLOAT_TOLERANCE);
		}

	  }
	}

}