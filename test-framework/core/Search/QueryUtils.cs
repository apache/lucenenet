using System;
using System.Diagnostics;
using System.Collections.Generic;
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


	using Assert = junit.framework.Assert;

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using AllDeletedFilterReader = Lucene.Net.Index.AllDeletedFilterReader;
	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using MultiReader = Lucene.Net.Index.MultiReader;
	using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
	using Directory = Lucene.Net.Store.Directory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using Bits = Lucene.Net.Util.Bits;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Util.LuceneTestCase.TEST_VERSION_CURRENT;

	/// <summary>
	/// Utility class for sanity-checking queries.
	/// </summary>
	public class QueryUtils
	{

	  /// <summary>
	  /// Check the types of things query objects should be able to do. </summary>
	  public static void Check(Query q)
	  {
		CheckHashEquals(q);
	  }

	  /// <summary>
	  /// check very basic hashCode and equals </summary>
	  public static void CheckHashEquals(Query q)
	  {
		Query q2 = q.clone();
		CheckEqual(q,q2);

		Query q3 = q.clone();
		q3.Boost = 7.21792348f;
		CheckUnequal(q,q3);

		// test that a class check is done so that no exception is thrown
		// in the implementation of equals()
		Query whacky = new QueryAnonymousInnerClassHelper();
		whacky.Boost = q.Boost;
		CheckUnequal(q, whacky);

		// null test
		Assert.Assert.IsFalse(q.Equals(null));
	  }

	  private class QueryAnonymousInnerClassHelper : Query
	  {
		  public QueryAnonymousInnerClassHelper()
		  {
		  }

		  public override string ToString(string field)
		  {
			return "My Whacky Query";
		  }
	  }

	  public static void CheckEqual(Query q1, Query q2)
	  {
		Assert.Assert.AreEqual(q1, q2);
		Assert.Assert.AreEqual(q1.GetHashCode(), q2.GetHashCode());
	  }

	  public static void CheckUnequal(Query q1, Query q2)
	  {
		Assert.Assert.IsFalse(q1 + " equal to " + q2, q1.Equals(q2));
		Assert.Assert.IsFalse(q2 + " equal to " + q1, q2.Equals(q1));

		// possible this test can fail on a hash collision... if that
		// happens, please change test to use a different example.
		Assert.Assert.IsTrue(q1.GetHashCode() != q2.GetHashCode());
	  }

	  /// <summary>
	  /// deep check that explanations of a query 'score' correctly </summary>
	  public static void CheckExplanations(Query q, IndexSearcher s)
	  {
		CheckHits.CheckExplanations(q, null, s, true);
	  }

	  /// <summary>
	  /// Various query sanity checks on a searcher, some checks are only done for
	  /// instanceof IndexSearcher.
	  /// </summary>
	  /// <seealso cref= #check(Query) </seealso>
	  /// <seealso cref= #checkFirstSkipTo </seealso>
	  /// <seealso cref= #checkSkipTo </seealso>
	  /// <seealso cref= #checkExplanations </seealso>
	  /// <seealso cref= #checkEqual </seealso>
	  public static void Check(Random random, Query q1, IndexSearcher s)
	  {
		Check(random, q1, s, true);
	  }
	  public static void Check(Random random, Query q1, IndexSearcher s, bool wrap)
	  {
		try
		{
		  Check(q1);
		  if (s != null)
		  {
			CheckFirstSkipTo(q1,s);
			CheckSkipTo(q1,s);
			if (wrap)
			{
			  Check(random, q1, WrapUnderlyingReader(random, s, -1), false);
			  Check(random, q1, WrapUnderlyingReader(random, s, 0), false);
			  Check(random, q1, WrapUnderlyingReader(random, s, +1), false);
			}
			CheckExplanations(q1,s);

			Query q2 = q1.clone();
			CheckEqual(s.rewrite(q1), s.rewrite(q2));
		  }
		}
		catch (IOException e)
		{
		  throw new Exception(e);
		}
	  }

	  public static void PurgeFieldCache(IndexReader r)
	  {
		// this is just a hack, to get an atomic reader that contains all subreaders for insanity checks
		FieldCache.DEFAULT.purgeByCacheKey(SlowCompositeReaderWrapper.wrap(r).CoreCacheKey);
	  }

	  /// <summary>
	  /// this is a MultiReader that can be used for randomly wrapping other readers
	  /// without creating FieldCache insanity.
	  /// The trick is to use an opaque/fake cache key. 
	  /// </summary>
	  public class FCInvisibleMultiReader : MultiReader
	  {
		internal readonly object CacheKey = new object();

		public FCInvisibleMultiReader(params IndexReader[] readers) : base(readers)
		{
		}

		public override object CoreCacheKey
		{
			get
			{
			  return CacheKey;
			}
		}

		public override object CombinedCoreAndDeletesKey
		{
			get
			{
			  return CacheKey;
			}
		}
	  }

	  /// <summary>
	  /// Given an IndexSearcher, returns a new IndexSearcher whose IndexReader 
	  /// is a MultiReader containing the Reader of the original IndexSearcher, 
	  /// as well as several "empty" IndexReaders -- some of which will have 
	  /// deleted documents in them.  this new IndexSearcher should 
	  /// behave exactly the same as the original IndexSearcher. </summary>
	  /// <param name="s"> the searcher to wrap </param>
	  /// <param name="edge"> if negative, s will be the first sub; if 0, s will be in the middle, if positive s will be the last sub </param>
	  public static IndexSearcher WrapUnderlyingReader(Random random, IndexSearcher s, int edge)
	  {

		IndexReader r = s.IndexReader;

		// we can't put deleted docs before the nested reader, because
		// it will throw off the docIds
		IndexReader[] readers = new IndexReader[] {edge < 0 ? r : EmptyReaders[0], EmptyReaders[0], new FCInvisibleMultiReader(edge < 0 ? EmptyReaders[4] : EmptyReaders[0], EmptyReaders[0], 0 == edge ? r : EmptyReaders[0]), 0 < edge ? EmptyReaders[0] : EmptyReaders[7], EmptyReaders[0], new FCInvisibleMultiReader(0 < edge ? EmptyReaders[0] : EmptyReaders[5], EmptyReaders[0], 0 < edge ? r : EmptyReaders[0])};

		IndexSearcher @out = LuceneTestCase.NewSearcher(new FCInvisibleMultiReader(readers));
		@out.Similarity = s.Similarity;
		return @out;
	  }

	  internal static readonly IndexReader[] EmptyReaders = new IndexReader[8];
	  static QueryUtils()
	  {
		try
		{
		  EmptyReaders[0] = new MultiReader();
		  EmptyReaders[4] = MakeEmptyIndex(new Random(0), 4);
		  EmptyReaders[5] = MakeEmptyIndex(new Random(0), 5);
		  EmptyReaders[7] = MakeEmptyIndex(new Random(0), 7);
		}
		catch (IOException ex)
		{
		  throw new Exception(ex);
		}
	  }

	  private static IndexReader MakeEmptyIndex(Random random, int numDocs)
	  {
		Debug.Assert(numDocs > 0);
		Directory d = new MockDirectoryWrapper(random, new RAMDirectory());
		IndexWriter w = new IndexWriter(d, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)));
		for (int i = 0; i < numDocs; i++)
		{
		  w.addDocument(new Document());
		}
		w.forceMerge(1);
		w.commit();
		w.close();
		DirectoryReader reader = DirectoryReader.open(d);
		return new AllDeletedFilterReader(LuceneTestCase.GetOnlySegmentReader(reader));
	  }

	  /// <summary>
	  /// alternate scorer skipTo(),skipTo(),next(),next(),skipTo(),skipTo(), etc
	  /// and ensure a hitcollector receives same docs and scores
	  /// </summary>
	  public static void CheckSkipTo(Query q, IndexSearcher s)
	  {
		//System.out.println("Checking "+q);
		IList<AtomicReaderContext> readerContextArray = s.TopReaderContext.leaves();
		if (s.createNormalizedWeight(q).scoresDocsOutOfOrder()) // in this case order of skipTo() might differ from that of next().
		{
			return;
		}

		const int skip_op = 0;
		const int next_op = 1;
		int[][] orders = new int[][] {new int[] {next_op}, new int[] {skip_op}, new int[] {skip_op, next_op}, new int[] {next_op, skip_op}, new int[] {skip_op, skip_op, next_op, next_op}, new int[] {next_op, next_op, skip_op, skip_op}, new int[] {skip_op, skip_op, skip_op, next_op, next_op}};
		for (int k = 0; k < orders.Length; k++)
		{

			int[] order = orders[k];
			// System.out.print("Order:");for (int i = 0; i < order.length; i++)
			// System.out.print(order[i]==skip_op ? " skip()":" next()");
			// System.out.println();
			int[] opidx = new int[] {0};
			int[] lastDoc = new int[] {-1};

			// FUTURE: ensure scorer.doc()==-1

			const float maxDiff = 1e-5f;
			AtomicReader[] lastReader = new AtomicReader[] {null};

			s.search(q, new CollectorAnonymousInnerClassHelper(q, s, readerContextArray, skip_op, order, opidx, lastDoc, maxDiff, lastReader));

			if (lastReader[0] != null)
			{
			  // confirm that skipping beyond the last doc, on the
			  // previous reader, hits NO_MORE_DOCS
			  AtomicReader previousReader = lastReader[0];
			  IndexSearcher indexSearcher = LuceneTestCase.NewSearcher(previousReader, false);
			  indexSearcher.Similarity = s.Similarity;
			  Weight w = indexSearcher.createNormalizedWeight(q);
			  AtomicReaderContext ctx = previousReader.Context;
			  Scorer scorer = w.scorer(ctx, ctx.reader().LiveDocs);
			  if (scorer != null)
			  {
				bool more = scorer.advance(lastDoc[0] + 1) != DocIdSetIterator.NO_MORE_DOCS;
				Assert.Assert.IsFalse("query's last doc was " + lastDoc[0] + " but skipTo(" + (lastDoc[0] + 1) + ") got to " + scorer.docID(),more);
			  }
			}
		}
	  }

	  private class CollectorAnonymousInnerClassHelper : Collector
	  {
		  private Query q;
		  private IndexSearcher s;
		  private IList<AtomicReaderContext> ReaderContextArray;
		  private int Skip_op;
		  private int[] Order;
		  private int[] Opidx;
		  private int[] LastDoc;
		  private float MaxDiff;
		  private AtomicReader[] LastReader;

		  public CollectorAnonymousInnerClassHelper(Query q, IndexSearcher s, IList<AtomicReaderContext> readerContextArray, int skip_op, int[] order, int[] opidx, int[] lastDoc, float maxDiff, AtomicReader[] lastReader)
		  {
			  this.q = q;
			  this.s = s;
			  this.ReaderContextArray = readerContextArray;
			  this.Skip_op = skip_op;
			  this.Order = order;
			  this.Opidx = opidx;
			  this.LastDoc = lastDoc;
			  this.MaxDiff = maxDiff;
			  this.LastReader = lastReader;
		  }

		  private Scorer sc;
		  private Scorer scorer;
		  private int leafPtr;

		  public override Scorer Scorer
		  {
			  set
			  {
				this.sc = value;
			  }
		  }

		  public override void Collect(int doc)
		  {
			float score = sc.score();
			LastDoc[0] = doc;
			try
			{
			  if (scorer == null)
			  {
				Weight w = s.createNormalizedWeight(q);
				AtomicReaderContext context = ReaderContextArray[leafPtr];
				scorer = w.scorer(context, context.reader().LiveDocs);
			  }

			  int op = Order[(Opidx[0]++) % Order.Length];
			  // System.out.println(op==skip_op ?
			  // "skip("+(sdoc[0]+1)+")":"next()");
			  bool more = op == Skip_op ? scorer.advance(scorer.docID() + 1) != DocIdSetIterator.NO_MORE_DOCS : scorer.nextDoc() != DocIdSetIterator.NO_MORE_DOCS;
			  int scorerDoc = scorer.docID();
			  float scorerScore = scorer.score();
			  float scorerScore2 = scorer.score();
			  float scoreDiff = Math.Abs(score - scorerScore);
			  float scorerDiff = Math.Abs(scorerScore2 - scorerScore);
			  if (!more || doc != scorerDoc || scoreDiff > MaxDiff || scorerDiff > MaxDiff)
			  {
				StringBuilder sbord = new StringBuilder();
				for (int i = 0; i < Order.Length; i++)
				{
				  sbord.Append(Order[i] == Skip_op ? " skip()" : " next()");
				}
				throw new Exception("ERROR matching docs:" + "\n\t" + (doc != scorerDoc ? "--> " : "") + "doc=" + doc + ", scorerDoc=" + scorerDoc + "\n\t" + (!more ? "--> " : "") + "tscorer.more=" + more + "\n\t" + (scoreDiff > MaxDiff ? "--> " : "") + "scorerScore=" + scorerScore + " scoreDiff=" + scoreDiff + " maxDiff=" + MaxDiff + "\n\t" + (scorerDiff > MaxDiff ? "--> " : "") + "scorerScore2=" + scorerScore2 + " scorerDiff=" + scorerDiff + "\n\thitCollector.doc=" + doc + " score=" + score + "\n\t Scorer=" + scorer + "\n\t Query=" + q + "  " + q.GetType().Name + "\n\t Searcher=" + s + "\n\t Order=" + sbord + "\n\t Op=" + (op == Skip_op ? " skip()" : " next()"));
			  }
			}
			catch (IOException e)
			{
			  throw new Exception(e);
			}
		  }

		  public override AtomicReaderContext NextReader
		  {
			  set
			  {
				// confirm that skipping beyond the last doc, on the
				// previous reader, hits NO_MORE_DOCS
				if (LastReader[0] != null)
				{
				  AtomicReader previousReader = LastReader[0];
				  IndexSearcher indexSearcher = LuceneTestCase.NewSearcher(previousReader);
				  indexSearcher.Similarity = s.Similarity;
				  Weight w = indexSearcher.createNormalizedWeight(q);
				  AtomicReaderContext ctx = (AtomicReaderContext)indexSearcher.TopReaderContext;
				  Scorer scorer = w.scorer(ctx, ctx.reader().LiveDocs);
				  if (scorer != null)
				  {
					bool more = scorer.advance(LastDoc[0] + 1) != DocIdSetIterator.NO_MORE_DOCS;
					Assert.Assert.IsFalse("query's last doc was " + LastDoc[0] + " but skipTo(" + (LastDoc[0] + 1) + ") got to " + scorer.docID(),more);
				  }
				  leafPtr++;
				}
				LastReader[0] = value.reader();
				Debug.Assert(ReaderContextArray[leafPtr].reader() == value.reader());
				this.scorer = null;
				LastDoc[0] = -1;
			  }
		  }

		  public override bool AcceptsDocsOutOfOrder()
		  {
			return false;
		  }
	  }

	  /// <summary>
	  /// check that first skip on just created scorers always goes to the right doc </summary>
	  public static void CheckFirstSkipTo(Query q, IndexSearcher s)
	  {
		//System.out.println("checkFirstSkipTo: "+q);
		const float maxDiff = 1e-3f;
		int[] lastDoc = new int[] {-1};
		AtomicReader[] lastReader = new AtomicReader[] {null};
		IList<AtomicReaderContext> context = s.TopReaderContext.leaves();
		s.search(q,new CollectorAnonymousInnerClassHelper2(q, s, maxDiff, lastDoc, lastReader, context));

		if (lastReader[0] != null)
		{
		  // confirm that skipping beyond the last doc, on the
		  // previous reader, hits NO_MORE_DOCS
		  AtomicReader previousReader = lastReader[0];
		  IndexSearcher indexSearcher = LuceneTestCase.NewSearcher(previousReader);
		  indexSearcher.Similarity = s.Similarity;
		  Weight w = indexSearcher.createNormalizedWeight(q);
		  Scorer scorer = w.scorer((AtomicReaderContext)indexSearcher.TopReaderContext, previousReader.LiveDocs);
		  if (scorer != null)
		  {
			bool more = scorer.advance(lastDoc[0] + 1) != DocIdSetIterator.NO_MORE_DOCS;
			Assert.Assert.IsFalse("query's last doc was " + lastDoc[0] + " but skipTo(" + (lastDoc[0] + 1) + ") got to " + scorer.docID(),more);
		  }
		}
	  }

	  private class CollectorAnonymousInnerClassHelper2 : Collector
	  {
		  private Query q;
		  private IndexSearcher s;
		  private float MaxDiff;
		  private int[] LastDoc;
		  private AtomicReader[] LastReader;
		  private IList<AtomicReaderContext> Context;

		  public CollectorAnonymousInnerClassHelper2(Query q, IndexSearcher s, float maxDiff, int[] lastDoc, AtomicReader[] lastReader, IList<AtomicReaderContext> context)
		  {
			  this.q = q;
			  this.s = s;
			  this.MaxDiff = maxDiff;
			  this.LastDoc = lastDoc;
			  this.LastReader = lastReader;
			  this.Context = context;
		  }

		  private Scorer scorer;
		  private int leafPtr;
		  private Bits liveDocs;
		  public override Scorer Scorer
		  {
			  set
			  {
				this.scorer = value;
			  }
		  }
		  public override void Collect(int doc)
		  {
			float score = scorer.score();
			try
			{
			  long startMS = System.currentTimeMillis();
			  for (int i = LastDoc[0] + 1; i <= doc; i++)
			  {
				Weight w = s.createNormalizedWeight(q);
				Scorer scorer = w.scorer(Context[leafPtr], liveDocs);
				Assert.Assert.IsTrue("query collected " + doc + " but skipTo(" + i + ") says no more docs!",scorer.advance(i) != DocIdSetIterator.NO_MORE_DOCS);
				Assert.Assert.AreEqual("query collected " + doc + " but skipTo(" + i + ") got to " + scorer.docID(),doc,scorer.docID());
				float skipToScore = scorer.score();
				Assert.Assert.AreEqual("unstable skipTo(" + i + ") score!",skipToScore,scorer.score(),MaxDiff);
				Assert.Assert.AreEqual("query assigned doc " + doc + " a score of <" + score + "> but skipTo(" + i + ") has <" + skipToScore + ">!",score,skipToScore,MaxDiff);

				// Hurry things along if they are going slow (eg
				// if you got SimpleText codec this will kick in):
				if (i < doc && System.currentTimeMillis() - startMS > 5)
				{
				  i = doc - 1;
				}
			  }
			  LastDoc[0] = doc;
			}
			catch (IOException e)
			{
			  throw new Exception(e);
			}
		  }

		  public override AtomicReaderContext NextReader
		  {
			  set
			  {
				// confirm that skipping beyond the last doc, on the
				// previous reader, hits NO_MORE_DOCS
				if (LastReader[0] != null)
				{
				  AtomicReader previousReader = LastReader[0];
				  IndexSearcher indexSearcher = LuceneTestCase.NewSearcher(previousReader);
				  indexSearcher.Similarity = s.Similarity;
				  Weight w = indexSearcher.createNormalizedWeight(q);
				  Scorer scorer = w.scorer((AtomicReaderContext)indexSearcher.TopReaderContext, previousReader.LiveDocs);
				  if (scorer != null)
				  {
					bool more = scorer.advance(LastDoc[0] + 1) != DocIdSetIterator.NO_MORE_DOCS;
					Assert.Assert.IsFalse("query's last doc was " + LastDoc[0] + " but skipTo(" + (LastDoc[0] + 1) + ") got to " + scorer.docID(),more);
				  }
				  leafPtr++;
				}
    
				LastReader[0] = value.reader();
				LastDoc[0] = -1;
				liveDocs = value.reader().LiveDocs;
			  }
		  }
		  public override bool AcceptsDocsOutOfOrder()
		  {
			return false;
		  }
	  }
	}

}