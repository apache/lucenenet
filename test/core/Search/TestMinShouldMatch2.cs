using System.Diagnostics;
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


	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using SortedSetDocValuesField = Lucene.Net.Document.SortedSetDocValuesField;
	using StringField = Lucene.Net.Document.StringField;
	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
	using Term = Lucene.Net.Index.Term;
	using TermContext = Lucene.Net.Index.TermContext;
	using BooleanWeight = Lucene.Net.Search.BooleanQuery.BooleanWeight;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using SimScorer = Lucene.Net.Search.Similarities.Similarity.SimScorer;
	using SimWeight = Lucene.Net.Search.Similarities.Similarity.SimWeight;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;

	/// <summary>
	/// tests BooleanScorer2's minShouldMatch </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({"Appending", "Lucene3x", "Lucene40", "Lucene41"}) public class TestMinShouldMatch2 extends Lucene.Net.Util.LuceneTestCase
	public class TestMinShouldMatch2 : LuceneTestCase
	{
	  internal static Directory Dir;
	  internal static DirectoryReader r;
	  internal static AtomicReader Reader;
	  internal static IndexSearcher Searcher;

	  internal static readonly string[] AlwaysTerms = new string[] {"a"};
	  internal static readonly string[] CommonTerms = new string[] {"b", "c", "d"};
	  internal static readonly string[] MediumTerms = new string[] {"e", "f", "g"};
	  internal static readonly string[] RareTerms = new string[] {"h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z"};

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		Dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), Dir);
		int numDocs = atLeast(300);
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();

		  AddSome(doc, AlwaysTerms);

		  if (random().Next(100) < 90)
		  {
			AddSome(doc, CommonTerms);
		  }
		  if (random().Next(100) < 50)
		  {
			AddSome(doc, MediumTerms);
		  }
		  if (random().Next(100) < 10)
		  {
			AddSome(doc, RareTerms);
		  }
		  iw.addDocument(doc);
		}
		iw.forceMerge(1);
		iw.close();
		r = DirectoryReader.open(Dir);
		Reader = getOnlySegmentReader(r);
		Searcher = new IndexSearcher(Reader);
		Searcher.Similarity = new DefaultSimilarityAnonymousInnerClassHelper();
	  }

	  private class DefaultSimilarityAnonymousInnerClassHelper : DefaultSimilarity
	  {
		  public DefaultSimilarityAnonymousInnerClassHelper()
		  {
		  }

		  public override float QueryNorm(float sumOfSquaredWeights)
		  {
			return 1; // we disable queryNorm, both for debugging and ease of impl
		  }
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass() throws Exception
	  public static void AfterClass()
	  {
		Reader.close();
		Dir.close();
		Searcher = null;
		Reader = null;
		r = null;
		Dir = null;
	  }

	  private static void AddSome(Document doc, string[] values)
	  {
		IList<string> list = Arrays.asList(values);
		Collections.shuffle(list, random());
		int howMany = TestUtil.Next(random(), 1, list.Count);
		for (int i = 0; i < howMany; i++)
		{
		  doc.add(new StringField("field", list[i], Field.Store.NO));
		  doc.add(new SortedSetDocValuesField("dv", new BytesRef(list[i])));
		}
	  }

	  private Scorer Scorer(string[] values, int minShouldMatch, bool slow)
	  {
		BooleanQuery bq = new BooleanQuery();
		foreach (string value in values)
		{
		  bq.add(new TermQuery(new Term("field", value)), BooleanClause.Occur_e.SHOULD);
		}
		bq.MinimumNumberShouldMatch = minShouldMatch;

		BooleanWeight weight = (BooleanWeight) Searcher.createNormalizedWeight(bq);

		if (slow)
		{
		  return new SlowMinShouldMatchScorer(weight, Reader, Searcher);
		}
		else
		{
		  return weight.scorer(Reader.Context, null);
		}
	  }

	  private void AssertNext(Scorer expected, Scorer actual)
	  {
		if (actual == null)
		{
		  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, expected.nextDoc());
		  return;
		}
		int doc;
		while ((doc = expected.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
		{
		  Assert.AreEqual(doc, actual.nextDoc());
		  Assert.AreEqual(expected.freq(), actual.freq());
		  float expectedScore = expected.score();
		  float actualScore = actual.score();
		  Assert.AreEqual(expectedScore, actualScore, CheckHits.explainToleranceDelta(expectedScore, actualScore));
		}
		Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, actual.nextDoc());
	  }

	  private void AssertAdvance(Scorer expected, Scorer actual, int amount)
	  {
		if (actual == null)
		{
		  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, expected.nextDoc());
		  return;
		}
		int prevDoc = 0;
		int doc;
		while ((doc = expected.advance(prevDoc + amount)) != DocIdSetIterator.NO_MORE_DOCS)
		{
		  Assert.AreEqual(doc, actual.advance(prevDoc + amount));
		  Assert.AreEqual(expected.freq(), actual.freq());
		  float expectedScore = expected.score();
		  float actualScore = actual.score();
		  Assert.AreEqual(expectedScore, actualScore, CheckHits.explainToleranceDelta(expectedScore, actualScore));
		  prevDoc = doc;
		}
		Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, actual.advance(prevDoc + amount));
	  }

	  /// <summary>
	  /// simple test for next(): minShouldMatch=2 on 3 terms (one common, one medium, one rare) </summary>
	  public virtual void TestNextCMR2()
	  {
		for (int common = 0; common < CommonTerms.Length; common++)
		{
		  for (int medium = 0; medium < MediumTerms.Length; medium++)
		  {
			for (int rare = 0; rare < RareTerms.Length; rare++)
			{
			  Scorer expected = Scorer(new string[] {CommonTerms[common], MediumTerms[medium], RareTerms[rare]}, 2, true);
			  Scorer actual = Scorer(new string[] {CommonTerms[common], MediumTerms[medium], RareTerms[rare]}, 2, false);
			  AssertNext(expected, actual);
			}
		  }
		}
	  }

	  /// <summary>
	  /// simple test for advance(): minShouldMatch=2 on 3 terms (one common, one medium, one rare) </summary>
	  public virtual void TestAdvanceCMR2()
	  {
		for (int amount = 25; amount < 200; amount += 25)
		{
		  for (int common = 0; common < CommonTerms.Length; common++)
		  {
			for (int medium = 0; medium < MediumTerms.Length; medium++)
			{
			  for (int rare = 0; rare < RareTerms.Length; rare++)
			  {
				Scorer expected = Scorer(new string[] {CommonTerms[common], MediumTerms[medium], RareTerms[rare]}, 2, true);
				Scorer actual = Scorer(new string[] {CommonTerms[common], MediumTerms[medium], RareTerms[rare]}, 2, false);
				AssertAdvance(expected, actual, amount);
			  }
			}
		  }
		}
	  }

	  /// <summary>
	  /// test next with giant bq of all terms with varying minShouldMatch </summary>
	  public virtual void TestNextAllTerms()
	  {
		IList<string> termsList = new List<string>();
		termsList.AddRange(Arrays.asList(CommonTerms));
		termsList.AddRange(Arrays.asList(MediumTerms));
		termsList.AddRange(Arrays.asList(RareTerms));
		string[] terms = termsList.ToArray();

		for (int minNrShouldMatch = 1; minNrShouldMatch <= terms.Length; minNrShouldMatch++)
		{
		  Scorer expected = Scorer(terms, minNrShouldMatch, true);
		  Scorer actual = Scorer(terms, minNrShouldMatch, false);
		  AssertNext(expected, actual);
		}
	  }

	  /// <summary>
	  /// test advance with giant bq of all terms with varying minShouldMatch </summary>
	  public virtual void TestAdvanceAllTerms()
	  {
		IList<string> termsList = new List<string>();
		termsList.AddRange(Arrays.asList(CommonTerms));
		termsList.AddRange(Arrays.asList(MediumTerms));
		termsList.AddRange(Arrays.asList(RareTerms));
		string[] terms = termsList.ToArray();

		for (int amount = 25; amount < 200; amount += 25)
		{
		  for (int minNrShouldMatch = 1; minNrShouldMatch <= terms.Length; minNrShouldMatch++)
		  {
			Scorer expected = Scorer(terms, minNrShouldMatch, true);
			Scorer actual = Scorer(terms, minNrShouldMatch, false);
			AssertAdvance(expected, actual, amount);
		  }
		}
	  }

	  /// <summary>
	  /// test next with varying numbers of terms with varying minShouldMatch </summary>
	  public virtual void TestNextVaryingNumberOfTerms()
	  {
		IList<string> termsList = new List<string>();
		termsList.AddRange(Arrays.asList(CommonTerms));
		termsList.AddRange(Arrays.asList(MediumTerms));
		termsList.AddRange(Arrays.asList(RareTerms));
		Collections.shuffle(termsList, random());
		for (int numTerms = 2; numTerms <= termsList.Count; numTerms++)
		{
		  string[] terms = termsList.subList(0, numTerms).toArray(new string[0]);
		  for (int minNrShouldMatch = 1; minNrShouldMatch <= terms.Length; minNrShouldMatch++)
		  {
			Scorer expected = Scorer(terms, minNrShouldMatch, true);
			Scorer actual = Scorer(terms, minNrShouldMatch, false);
			AssertNext(expected, actual);
		  }
		}
	  }

	  /// <summary>
	  /// test advance with varying numbers of terms with varying minShouldMatch </summary>
	  public virtual void TestAdvanceVaryingNumberOfTerms()
	  {
		IList<string> termsList = new List<string>();
		termsList.AddRange(Arrays.asList(CommonTerms));
		termsList.AddRange(Arrays.asList(MediumTerms));
		termsList.AddRange(Arrays.asList(RareTerms));
		Collections.shuffle(termsList, random());

		for (int amount = 25; amount < 200; amount += 25)
		{
		  for (int numTerms = 2; numTerms <= termsList.Count; numTerms++)
		  {
			string[] terms = termsList.subList(0, numTerms).toArray(new string[0]);
			for (int minNrShouldMatch = 1; minNrShouldMatch <= terms.Length; minNrShouldMatch++)
			{
			  Scorer expected = Scorer(terms, minNrShouldMatch, true);
			  Scorer actual = Scorer(terms, minNrShouldMatch, false);
			  AssertAdvance(expected, actual, amount);
			}
		  }
		}
	  }

	  // TODO: more tests

	  // a slow min-should match scorer that uses a docvalues field.
	  // later, we can make debugging easier as it can record the set of ords it currently matched
	  // and e.g. print out their values and so on for the document
	  internal class SlowMinShouldMatchScorer : Scorer
	  {
		internal int CurrentDoc = -1; // current docid
		internal int CurrentMatched = -1; // current number of terms matched

		internal readonly SortedSetDocValues Dv;
		internal readonly int MaxDoc;

		internal readonly Set<long?> Ords = new HashSet<long?>();
		internal readonly SimScorer[] Sims;
		internal readonly int MinNrShouldMatch;

		internal double Score_Renamed = float.NaN;

		internal SlowMinShouldMatchScorer(BooleanWeight weight, AtomicReader reader, IndexSearcher searcher) : base(weight)
		{
		  this.Dv = reader.getSortedSetDocValues("dv");
		  this.MaxDoc = reader.maxDoc();
		  BooleanQuery bq = (BooleanQuery) weight.Query;
		  this.MinNrShouldMatch = bq.MinimumNumberShouldMatch;
		  this.Sims = new SimScorer[(int)Dv.ValueCount];
		  foreach (BooleanClause clause in bq.Clauses)
		  {
			Debug.Assert(!clause.Prohibited);
			Debug.Assert(!clause.Required);
			Term term = ((TermQuery)clause.Query).Term;
			long ord = Dv.lookupTerm(term.bytes());
			if (ord >= 0)
			{
			  bool success = Ords.add(ord);
			  Debug.Assert(success); // no dups
			  TermContext context = TermContext.build(reader.Context, term);
			  SimWeight w = weight.similarity.computeWeight(1f, searcher.collectionStatistics("field"), searcher.termStatistics(term, context));
			  w.ValueForNormalization; // ignored
			  w.normalize(1F, 1F);
			  Sims[(int)ord] = weight.similarity.simScorer(w, reader.Context);
			}
		  }
		}

		public override float Score()
		{
		  Debug.Assert(Score_Renamed != 0, CurrentMatched);
		  return (float)Score_Renamed * ((BooleanWeight) weight).coord(CurrentMatched, ((BooleanWeight) weight).maxCoord);
		}

		public override int Freq()
		{
		  return CurrentMatched;
		}

		public override int DocID()
		{
		  return CurrentDoc;
		}

		public override int NextDoc()
		{
		  Debug.Assert(CurrentDoc != NO_MORE_DOCS);
		  for (CurrentDoc = CurrentDoc + 1; CurrentDoc < MaxDoc; CurrentDoc++)
		  {
			CurrentMatched = 0;
			Score_Renamed = 0;
			Dv.Document = CurrentDoc;
			long ord;
			while ((ord = Dv.nextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
			{
			  if (Ords.contains(ord))
			  {
				CurrentMatched++;
				Score_Renamed += Sims[(int)ord].score(CurrentDoc, 1);
			  }
			}
			if (CurrentMatched >= MinNrShouldMatch)
			{
			  return CurrentDoc;
			}
		  }
		  return CurrentDoc = NO_MORE_DOCS;
		}

		public override int Advance(int target)
		{
		  int doc;
		  while ((doc = NextDoc()) < target)
		  {
		  }
		  return doc;
		}

		public override long Cost()
		{
		  return MaxDoc;
		}
	  }
	}

}