using System;
using System.Text;

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
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Occur = Lucene.Net.Search.BooleanClause.Occur_e;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using SpanNearQuery = Lucene.Net.Search.Spans.SpanNearQuery;
	using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
	using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using TestUtil = Lucene.Net.Util.TestUtil;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs("Lucene3x") public class TestQueryRescorer extends Lucene.Net.Util.LuceneTestCase
	public class TestQueryRescorer : LuceneTestCase
	{

	  private IndexSearcher GetSearcher(IndexReader r)
	  {
		IndexSearcher searcher = newSearcher(r);

		// We rely on more tokens = lower score:
		searcher.Similarity = new DefaultSimilarity();

		return searcher;
	  }

	  public virtual void TestBasic()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);

		Document doc = new Document();
		doc.add(newStringField("id", "0", Field.Store.YES));
		doc.add(newTextField("field", "wizard the the the the the oz", Field.Store.NO));
		w.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("id", "1", Field.Store.YES));
		// 1 extra token, but wizard and oz are close;
		doc.add(newTextField("field", "wizard oz the the the the the the", Field.Store.NO));
		w.addDocument(doc);
		IndexReader r = w.Reader;
		w.close();

		// Do ordinary BooleanQuery:
		BooleanQuery bq = new BooleanQuery();
		bq.add(new TermQuery(new Term("field", "wizard")), Occur.SHOULD);
		bq.add(new TermQuery(new Term("field", "oz")), Occur.SHOULD);
		IndexSearcher searcher = GetSearcher(r);
		searcher.Similarity = new DefaultSimilarity();

		TopDocs hits = searcher.search(bq, 10);
		Assert.AreEqual(2, hits.totalHits);
		Assert.AreEqual("0", searcher.doc(hits.scoreDocs[0].doc).get("id"));
		Assert.AreEqual("1", searcher.doc(hits.scoreDocs[1].doc).get("id"));

		// Now, resort using PhraseQuery:
		PhraseQuery pq = new PhraseQuery();
		pq.Slop = 5;
		pq.add(new Term("field", "wizard"));
		pq.add(new Term("field", "oz"));

		TopDocs hits2 = QueryRescorer.rescore(searcher, hits, pq, 2.0, 10);

		// Resorting changed the order:
		Assert.AreEqual(2, hits2.totalHits);
		Assert.AreEqual("1", searcher.doc(hits2.scoreDocs[0].doc).get("id"));
		Assert.AreEqual("0", searcher.doc(hits2.scoreDocs[1].doc).get("id"));

		// Resort using SpanNearQuery:
		SpanTermQuery t1 = new SpanTermQuery(new Term("field", "wizard"));
		SpanTermQuery t2 = new SpanTermQuery(new Term("field", "oz"));
		SpanNearQuery snq = new SpanNearQuery(new SpanQuery[] {t1, t2}, 0, true);

		TopDocs hits3 = QueryRescorer.rescore(searcher, hits, snq, 2.0, 10);

		// Resorting changed the order:
		Assert.AreEqual(2, hits3.totalHits);
		Assert.AreEqual("1", searcher.doc(hits3.scoreDocs[0].doc).get("id"));
		Assert.AreEqual("0", searcher.doc(hits3.scoreDocs[1].doc).get("id"));

		r.close();
		dir.close();
	  }

	  public virtual void TestCustomCombine()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);

		Document doc = new Document();
		doc.add(newStringField("id", "0", Field.Store.YES));
		doc.add(newTextField("field", "wizard the the the the the oz", Field.Store.NO));
		w.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("id", "1", Field.Store.YES));
		// 1 extra token, but wizard and oz are close;
		doc.add(newTextField("field", "wizard oz the the the the the the", Field.Store.NO));
		w.addDocument(doc);
		IndexReader r = w.Reader;
		w.close();

		// Do ordinary BooleanQuery:
		BooleanQuery bq = new BooleanQuery();
		bq.add(new TermQuery(new Term("field", "wizard")), Occur.SHOULD);
		bq.add(new TermQuery(new Term("field", "oz")), Occur.SHOULD);
		IndexSearcher searcher = GetSearcher(r);

		TopDocs hits = searcher.search(bq, 10);
		Assert.AreEqual(2, hits.totalHits);
		Assert.AreEqual("0", searcher.doc(hits.scoreDocs[0].doc).get("id"));
		Assert.AreEqual("1", searcher.doc(hits.scoreDocs[1].doc).get("id"));

		// Now, resort using PhraseQuery, but with an
		// opposite-world combine:
		PhraseQuery pq = new PhraseQuery();
		pq.Slop = 5;
		pq.add(new Term("field", "wizard"));
		pq.add(new Term("field", "oz"));

		TopDocs hits2 = new QueryRescorerAnonymousInnerClassHelper(this, pq)
		  .rescore(searcher, hits, 10);

		// Resorting didn't change the order:
		Assert.AreEqual(2, hits2.totalHits);
		Assert.AreEqual("0", searcher.doc(hits2.scoreDocs[0].doc).get("id"));
		Assert.AreEqual("1", searcher.doc(hits2.scoreDocs[1].doc).get("id"));

		r.close();
		dir.close();
	  }

	  private class QueryRescorerAnonymousInnerClassHelper : QueryRescorer
	  {
		  private readonly TestQueryRescorer OuterInstance;

		  public QueryRescorerAnonymousInnerClassHelper(TestQueryRescorer outerInstance, PhraseQuery pq) : base(pq)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override float Combine(float firstPassScore, bool secondPassMatches, float secondPassScore)
		  {
			float score = firstPassScore;
			if (secondPassMatches)
			{
			  score -= (float)(2.0 * secondPassScore);
			}
			return score;
		  }
	  }

	  public virtual void TestExplain()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);

		Document doc = new Document();
		doc.add(newStringField("id", "0", Field.Store.YES));
		doc.add(newTextField("field", "wizard the the the the the oz", Field.Store.NO));
		w.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("id", "1", Field.Store.YES));
		// 1 extra token, but wizard and oz are close;
		doc.add(newTextField("field", "wizard oz the the the the the the", Field.Store.NO));
		w.addDocument(doc);
		IndexReader r = w.Reader;
		w.close();

		// Do ordinary BooleanQuery:
		BooleanQuery bq = new BooleanQuery();
		bq.add(new TermQuery(new Term("field", "wizard")), Occur.SHOULD);
		bq.add(new TermQuery(new Term("field", "oz")), Occur.SHOULD);
		IndexSearcher searcher = GetSearcher(r);

		TopDocs hits = searcher.search(bq, 10);
		Assert.AreEqual(2, hits.totalHits);
		Assert.AreEqual("0", searcher.doc(hits.scoreDocs[0].doc).get("id"));
		Assert.AreEqual("1", searcher.doc(hits.scoreDocs[1].doc).get("id"));

		// Now, resort using PhraseQuery:
		PhraseQuery pq = new PhraseQuery();
		pq.add(new Term("field", "wizard"));
		pq.add(new Term("field", "oz"));

		Rescorer rescorer = new QueryRescorerAnonymousInnerClassHelper2(this, pq);

		TopDocs hits2 = rescorer.rescore(searcher, hits, 10);

		// Resorting changed the order:
		Assert.AreEqual(2, hits2.totalHits);
		Assert.AreEqual("1", searcher.doc(hits2.scoreDocs[0].doc).get("id"));
		Assert.AreEqual("0", searcher.doc(hits2.scoreDocs[1].doc).get("id"));

		int docID = hits2.scoreDocs[0].doc;
		Explanation explain = rescorer.explain(searcher, searcher.explain(bq, docID), docID);
		string s = explain.ToString();
		Assert.IsTrue(s.Contains("TestQueryRescorer$"));
		Assert.IsTrue(s.Contains("combined first and second pass score"));
		Assert.IsTrue(s.Contains("first pass score"));
		Assert.IsTrue(s.Contains("= second pass score"));
		Assert.AreEqual(hits2.scoreDocs[0].score, explain.Value, 0.0f);

		docID = hits2.scoreDocs[1].doc;
		explain = rescorer.explain(searcher, searcher.explain(bq, docID), docID);
		s = explain.ToString();
		Assert.IsTrue(s.Contains("TestQueryRescorer$"));
		Assert.IsTrue(s.Contains("combined first and second pass score"));
		Assert.IsTrue(s.Contains("first pass score"));
		Assert.IsTrue(s.Contains("no second pass score"));
		Assert.IsFalse(s.Contains("= second pass score"));
		Assert.IsTrue(s.Contains("NON-MATCH"));
		Assert.AreEqual(hits2.scoreDocs[1].score, explain.Value, 0.0f);

		r.close();
		dir.close();
	  }

	  private class QueryRescorerAnonymousInnerClassHelper2 : QueryRescorer
	  {
		  private readonly TestQueryRescorer OuterInstance;

		  public QueryRescorerAnonymousInnerClassHelper2(TestQueryRescorer outerInstance, PhraseQuery pq) : base(pq)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override float Combine(float firstPassScore, bool secondPassMatches, float secondPassScore)
		  {
			float score = firstPassScore;
			if (secondPassMatches)
			{
			  score += (float)(2.0 * secondPassScore);
			}
			return score;
		  }
	  }

	  public virtual void TestMissingSecondPassScore()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);

		Document doc = new Document();
		doc.add(newStringField("id", "0", Field.Store.YES));
		doc.add(newTextField("field", "wizard the the the the the oz", Field.Store.NO));
		w.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("id", "1", Field.Store.YES));
		// 1 extra token, but wizard and oz are close;
		doc.add(newTextField("field", "wizard oz the the the the the the", Field.Store.NO));
		w.addDocument(doc);
		IndexReader r = w.Reader;
		w.close();

		// Do ordinary BooleanQuery:
		BooleanQuery bq = new BooleanQuery();
		bq.add(new TermQuery(new Term("field", "wizard")), Occur.SHOULD);
		bq.add(new TermQuery(new Term("field", "oz")), Occur.SHOULD);
		IndexSearcher searcher = GetSearcher(r);

		TopDocs hits = searcher.search(bq, 10);
		Assert.AreEqual(2, hits.totalHits);
		Assert.AreEqual("0", searcher.doc(hits.scoreDocs[0].doc).get("id"));
		Assert.AreEqual("1", searcher.doc(hits.scoreDocs[1].doc).get("id"));

		// Now, resort using PhraseQuery, no slop:
		PhraseQuery pq = new PhraseQuery();
		pq.add(new Term("field", "wizard"));
		pq.add(new Term("field", "oz"));

		TopDocs hits2 = QueryRescorer.rescore(searcher, hits, pq, 2.0, 10);

		// Resorting changed the order:
		Assert.AreEqual(2, hits2.totalHits);
		Assert.AreEqual("1", searcher.doc(hits2.scoreDocs[0].doc).get("id"));
		Assert.AreEqual("0", searcher.doc(hits2.scoreDocs[1].doc).get("id"));

		// Resort using SpanNearQuery:
		SpanTermQuery t1 = new SpanTermQuery(new Term("field", "wizard"));
		SpanTermQuery t2 = new SpanTermQuery(new Term("field", "oz"));
		SpanNearQuery snq = new SpanNearQuery(new SpanQuery[] {t1, t2}, 0, true);

		TopDocs hits3 = QueryRescorer.rescore(searcher, hits, snq, 2.0, 10);

		// Resorting changed the order:
		Assert.AreEqual(2, hits3.totalHits);
		Assert.AreEqual("1", searcher.doc(hits3.scoreDocs[0].doc).get("id"));
		Assert.AreEqual("0", searcher.doc(hits3.scoreDocs[1].doc).get("id"));

		r.close();
		dir.close();
	  }

	  public virtual void TestRandom()
	  {
		Directory dir = newDirectory();
		int numDocs = atLeast(1000);
		RandomIndexWriter w = new RandomIndexWriter(random(), dir);

		int[] idToNum = new int[numDocs];
		int maxValue = TestUtil.Next(random(), 10, 1000000);
		for (int i = 0;i < numDocs;i++)
		{
		  Document doc = new Document();
		  doc.add(newStringField("id", "" + i, Field.Store.YES));
		  int numTokens = TestUtil.Next(random(), 1, 10);
		  StringBuilder b = new StringBuilder();
		  for (int j = 0;j < numTokens;j++)
		  {
			b.Append("a ");
		  }
		  doc.add(newTextField("field", b.ToString(), Field.Store.NO));
		  idToNum[i] = random().Next(maxValue);
		  doc.add(new NumericDocValuesField("num", idToNum[i]));
		  w.addDocument(doc);
		}
		IndexReader r = w.Reader;
		w.close();

		IndexSearcher s = newSearcher(r);
		int numHits = TestUtil.Next(random(), 1, numDocs);
		bool reverse = random().nextBoolean();

		//System.out.println("numHits=" + numHits + " reverse=" + reverse);
		TopDocs hits = s.search(new TermQuery(new Term("field", "a")), numHits);

		TopDocs hits2 = new QueryRescorerAnonymousInnerClassHelper3(this, new FixedScoreQuery(idToNum, reverse))
		  .rescore(s, hits, numHits);

		int?[] expected = new int?[numHits];
		for (int i = 0;i < numHits;i++)
		{
		  expected[i] = hits.scoreDocs[i].doc;
		}

		int reverseInt = reverse ? - 1 : 1;

		Arrays.sort(expected, new ComparatorAnonymousInnerClassHelper(this, idToNum, r, reverseInt));

		bool fail = false;
		for (int i = 0;i < numHits;i++)
		{
		  //System.out.println("expected=" + expected[i] + " vs " + hits2.scoreDocs[i].doc + " v=" + idToNum[Integer.parseInt(r.document(expected[i]).get("id"))]);
		  if ((int)expected[i] != hits2.scoreDocs[i].doc)
		  {
			//System.out.println("  diff!");
			fail = true;
		  }
		}
		Assert.IsFalse(fail);

		r.close();
		dir.close();
	  }

	  private class QueryRescorerAnonymousInnerClassHelper3 : QueryRescorer
	  {
		  private readonly TestQueryRescorer OuterInstance;

		  public QueryRescorerAnonymousInnerClassHelper3(TestQueryRescorer outerInstance, FixedScoreQuery new) : base(new FixedScoreQuery)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override float Combine(float firstPassScore, bool secondPassMatches, float secondPassScore)
		  {
			return secondPassScore;
		  }
	  }

	  private class ComparatorAnonymousInnerClassHelper : IComparer<int?>
	  {
		  private readonly TestQueryRescorer OuterInstance;

		  private int[] IdToNum;
		  private IndexReader r;
		  private int ReverseInt;

		  public ComparatorAnonymousInnerClassHelper(TestQueryRescorer outerInstance, int[] idToNum, IndexReader r, int reverseInt)
		  {
			  this.OuterInstance = outerInstance;
			  this.IdToNum = idToNum;
			  this.r = r;
			  this.ReverseInt = reverseInt;
		  }

		  public virtual int Compare(int? a, int? b)
		  {
			try
			{
			  int av = IdToNum[Convert.ToInt32(r.document(a).get("id"))];
			  int bv = IdToNum[Convert.ToInt32(r.document(b).get("id"))];
			  if (av < bv)
			  {
				return -ReverseInt;
			  }
			  else if (bv < av)
			  {
				return ReverseInt;
			  }
			  else
			  {
				// Tie break by docID, ascending
				return a - b;
			  }
			}
			catch (IOException ioe)
			{
			  throw new Exception(ioe);
			}
		  }
	  }

	  /// <summary>
	  /// Just assigns score == idToNum[doc("id")] for each doc. </summary>
	  private class FixedScoreQuery : Query
	  {
		internal readonly int[] IdToNum;
		internal readonly bool Reverse;

		public FixedScoreQuery(int[] idToNum, bool reverse)
		{
		  this.IdToNum = idToNum;
		  this.Reverse = reverse;
		}

		public override Weight CreateWeight(IndexSearcher searcher)
		{

		  return new WeightAnonymousInnerClassHelper(this);
		}

		private class WeightAnonymousInnerClassHelper : Weight
		{
			private readonly FixedScoreQuery OuterInstance;

			public WeightAnonymousInnerClassHelper(FixedScoreQuery outerInstance)
			{
				this.OuterInstance = outerInstance;
			}


			public override Query Query
			{
				get
				{
				  return OuterInstance;
				}
			}

			public override float ValueForNormalization
			{
				get
				{
				  return 1.0f;
				}
			}

			public override void Normalize(float queryNorm, float topLevelBoost)
			{
			}

			public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
			{

			  return new ScorerAnonymousInnerClassHelper(this, context);
			}

			private class ScorerAnonymousInnerClassHelper : Scorer
			{
				private readonly WeightAnonymousInnerClassHelper OuterInstance;

				private AtomicReaderContext Context;

				public ScorerAnonymousInnerClassHelper(WeightAnonymousInnerClassHelper outerInstance, AtomicReaderContext context) : base(null)
				{
					this.outerInstance = outerInstance;
					this.Context = context;
					docID = -1;
				}

				internal int docID;

				public override int DocID()
				{
				  return docID;
				}

				public override int Freq()
				{
				  return 1;
				}

				public override long Cost()
				{
				  return 1;
				}

				public override int NextDoc()
				{
				  docID++;
				  if (docID >= Context.reader().maxDoc())
				  {
					return NO_MORE_DOCS;
				  }
				  return docID;
				}

				public override int Advance(int target)
				{
				  docID = target;
				  return docID;
				}

				public override float Score()
				{
				  int num = OuterInstance.OuterInstance.IdToNum[Convert.ToInt32(Context.reader().document(docID).get("id"))];
				  if (OuterInstance.OuterInstance.Reverse)
				  {
					//System.out.println("score doc=" + docID + " num=" + num);
					return num;
				  }
				  else
				  {
					//System.out.println("score doc=" + docID + " num=" + -num);
					return -num;
				  }
				}
			}

			public override Explanation Explain(AtomicReaderContext context, int doc)
			{
			  return null;
			}
		}

		public override void ExtractTerms(Set<Term> terms)
		{
		}

		public override string ToString(string field)
		{
		  return "FixedScoreQuery " + IdToNum.Length + " ids; reverse=" + Reverse;
		}

		public override bool Equals(object o)
		{
		  if ((o is FixedScoreQuery) == false)
		  {
			return false;
		  }
		  FixedScoreQuery other = (FixedScoreQuery) o;
		  return float.floatToIntBits(Boost) == float.floatToIntBits(other.Boost) && Reverse == other.Reverse && Arrays.Equals(IdToNum, other.IdToNum);
		}

		public override Query Clone()
		{
		  return new FixedScoreQuery(IdToNum, Reverse);
		}

		public override int HashCode()
		{
		  int PRIME = 31;
		  int hash = base.GetHashCode();
		  if (Reverse)
		  {
			hash = PRIME * hash + 3623;
		  }
		  hash = PRIME * hash + Arrays.GetHashCode(IdToNum);
		  return hash;
		}
	  }
	}

}