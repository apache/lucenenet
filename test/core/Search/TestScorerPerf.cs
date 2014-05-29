using System;
using System.Collections;

namespace Lucene.Net.Search
{

	using Field = Lucene.Net.Document.Field;
	using Bits = Lucene.Net.Util.Bits;
	using DocIdBitSet = Lucene.Net.Util.DocIdBitSet;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;


	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using Term = Lucene.Net.Index.Term;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using Directory = Lucene.Net.Store.Directory;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;

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

	public class TestScorerPerf : LuceneTestCase
	{
	  internal bool Validate = true; // set to false when doing performance testing

	  internal BitArray[] Sets;
	  internal Term[] Terms;
	  internal IndexSearcher s;
	  internal IndexReader r;
	  internal Directory d;

	  // TODO: this should be setUp()....
	  public virtual void CreateDummySearcher()
	  {
		  // Create a dummy index with nothing in it.
		// this could possibly fail if Lucene starts checking for docid ranges...
		d = newDirectory();
		IndexWriter iw = new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		iw.addDocument(new Document());
		iw.close();
		r = DirectoryReader.open(d);
		s = newSearcher(r);
	  }

	  public virtual void CreateRandomTerms(int nDocs, int nTerms, double power, Directory dir)
	  {
		int[] freq = new int[nTerms];
		Terms = new Term[nTerms];
		for (int i = 0; i < nTerms; i++)
		{
		  int f = (nTerms + 1) - i; // make first terms less frequent
		  freq[i] = (int)Math.Ceiling(Math.Pow(f,power));
		  Terms[i] = new Term("f",char.ToString((char)('A' + i)));
		}

		IndexWriter iw = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE));
		for (int i = 0; i < nDocs; i++)
		{
		  Document d = new Document();
		  for (int j = 0; j < nTerms; j++)
		  {
			if (random().Next(freq[j]) == 0)
			{
			  d.add(newStringField("f", Terms[j].text(), Field.Store.NO));
			  //System.out.println(d);
			}
		  }
		  iw.addDocument(d);
		}
		iw.forceMerge(1);
		iw.close();
	  }


	  public virtual BitArray RandBitSet(int sz, int numBitsToSet)
	  {
		BitArray set = new BitArray(sz);
		for (int i = 0; i < numBitsToSet; i++)
		{
		  set.Set(random().Next(sz), true);
		}
		return set;
	  }

	  public virtual BitArray[] RandBitSets(int numSets, int setSize)
	  {
		BitArray[] sets = new BitArray[numSets];
		for (int i = 0; i < sets.Length; i++)
		{
		  sets[i] = RandBitSet(setSize, random().Next(setSize));
		}
		return sets;
	  }

	  public class CountingHitCollector : Collector
	  {
		internal int Count_Renamed = 0;
		internal int Sum_Renamed = 0;
		protected internal int DocBase = 0;

		public override Scorer Scorer
		{
			set
			{
			}
		}
		public override void Collect(int doc)
		{
		  Count_Renamed++;
		  Sum_Renamed += DocBase + doc; // use it to avoid any possibility of being eliminated by hotspot
		}

		public virtual int Count
		{
			get
			{
				return Count_Renamed;
			}
		}
		public virtual int Sum
		{
			get
			{
				return Sum_Renamed;
			}
		}

		public override AtomicReaderContext NextReader
		{
			set
			{
			  DocBase = value.docBase;
			}
		}
		public override bool AcceptsDocsOutOfOrder()
		{
		  return true;
		}
	  }


	  public class MatchingHitCollector : CountingHitCollector
	  {
		internal BitArray Answer;
		internal int Pos = -1;
		public MatchingHitCollector(BitArray answer)
		{
		  this.Answer = answer;
		}

		public virtual void Collect(int doc, float score)
		{

		  Pos = Answer.nextSetBit(Pos + 1);
		  if (Pos != doc + DocBase)
		  {
			throw new Exception("Expected doc " + Pos + " but got " + doc + DocBase);
		  }
		  base.Collect(doc);
		}
	  }


	  internal virtual BitArray AddClause(BooleanQuery bq, BitArray result)
	  {
		BitArray rnd = Sets[random().Next(Sets.Length)];
		Query q = new ConstantScoreQuery(new FilterAnonymousInnerClassHelper(this, rnd));
		bq.add(q, BooleanClause.Occur_e.MUST);
		if (Validate)
		{
		  if (result == null)
		  {
			  result = (BitArray)rnd.clone();
		  }
		  else
		  {
			  result = result.And(rnd);
		  }
		}
		return result;
	  }

	  private class FilterAnonymousInnerClassHelper : Filter
	  {
		  private readonly TestScorerPerf OuterInstance;

		  private BitArray Rnd;

		  public FilterAnonymousInnerClassHelper(TestScorerPerf outerInstance, BitArray rnd)
		  {
			  this.OuterInstance = outerInstance;
			  this.Rnd = rnd;
		  }

		  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
		  {
			assertNull("acceptDocs should be null, as we have an index without deletions", acceptDocs);
			return new DocIdBitSet(Rnd);
		  }
	  }


	  public virtual int DoConjunctions(int iter, int maxClauses)
	  {
		int ret = 0;

		for (int i = 0; i < iter; i++)
		{
		  int nClauses = random().Next(maxClauses - 1) + 2; // min 2 clauses
		  BooleanQuery bq = new BooleanQuery();
		  BitArray result = null;
		  for (int j = 0; j < nClauses; j++)
		  {
			result = AddClause(bq,result);
		  }

		  CountingHitCollector hc = Validate ? new MatchingHitCollector(result) : new CountingHitCollector();
		  s.search(bq, hc);
		  ret += hc.Sum;

		  if (Validate)
		  {
			  Assert.AreEqual(result.cardinality(), hc.Count);
		  }
		  // System.out.println(hc.getCount());
		}

		return ret;
	  }

	  public virtual int DoNestedConjunctions(int iter, int maxOuterClauses, int maxClauses)
	  {
		int ret = 0;
		long nMatches = 0;

		for (int i = 0; i < iter; i++)
		{
		  int oClauses = random().Next(maxOuterClauses - 1) + 2;
		  BooleanQuery oq = new BooleanQuery();
		  BitArray result = null;

		  for (int o = 0; o < oClauses; o++)
		  {

		  int nClauses = random().Next(maxClauses - 1) + 2; // min 2 clauses
		  BooleanQuery bq = new BooleanQuery();
		  for (int j = 0; j < nClauses; j++)
		  {
			result = AddClause(bq,result);
		  }

		  oq.add(bq, BooleanClause.Occur_e.MUST);
		  } // outer

		  CountingHitCollector hc = Validate ? new MatchingHitCollector(result) : new CountingHitCollector();
		  s.search(oq, hc);
		  nMatches += hc.Count;
		  ret += hc.Sum;
		  if (Validate)
		  {
			  Assert.AreEqual(result.cardinality(), hc.Count);
		  }
		  // System.out.println(hc.getCount());
		}
		if (VERBOSE)
		{
			Console.WriteLine("Average number of matches=" + (nMatches / iter));
		}
		return ret;
	  }


	  public virtual int DoTermConjunctions(IndexSearcher s, int termsInIndex, int maxClauses, int iter)
	  {
		int ret = 0;

		long nMatches = 0;
		for (int i = 0; i < iter; i++)
		{
		  int nClauses = random().Next(maxClauses - 1) + 2; // min 2 clauses
		  BooleanQuery bq = new BooleanQuery();
		  BitArray termflag = new BitArray(termsInIndex);
		  for (int j = 0; j < nClauses; j++)
		  {
			int tnum;
			// don't pick same clause twice
			tnum = random().Next(termsInIndex);
			if (termflag.Get(tnum))
			{
				tnum = termflag.nextClearBit(tnum);
			}
			if (tnum < 0 || tnum >= termsInIndex)
			{
				tnum = termflag.nextClearBit(0);
			}
			termflag.Set(tnum, true);
			Query tq = new TermQuery(Terms[tnum]);
			bq.add(tq, BooleanClause.Occur_e.MUST);
		  }

		  CountingHitCollector hc = new CountingHitCollector();
		  s.search(bq, hc);
		  nMatches += hc.Count;
		  ret += hc.Sum;
		}
		if (VERBOSE)
		{
			Console.WriteLine("Average number of matches=" + (nMatches / iter));
		}

		return ret;
	  }


	  public virtual int DoNestedTermConjunctions(IndexSearcher s, int termsInIndex, int maxOuterClauses, int maxClauses, int iter)
	  {
		int ret = 0;
		long nMatches = 0;
		for (int i = 0; i < iter; i++)
		{
		  int oClauses = random().Next(maxOuterClauses - 1) + 2;
		  BooleanQuery oq = new BooleanQuery();
		  for (int o = 0; o < oClauses; o++)
		  {

		  int nClauses = random().Next(maxClauses - 1) + 2; // min 2 clauses
		  BooleanQuery bq = new BooleanQuery();
		  BitArray termflag = new BitArray(termsInIndex);
		  for (int j = 0; j < nClauses; j++)
		  {
			int tnum;
			// don't pick same clause twice
			tnum = random().Next(termsInIndex);
			if (termflag.Get(tnum))
			{
				tnum = termflag.nextClearBit(tnum);
			}
			if (tnum < 0 || tnum >= 25)
			{
				tnum = termflag.nextClearBit(0);
			}
			termflag.Set(tnum, true);
			Query tq = new TermQuery(Terms[tnum]);
			bq.add(tq, BooleanClause.Occur_e.MUST);
		  } // inner

		  oq.add(bq, BooleanClause.Occur_e.MUST);
		  } // outer


		  CountingHitCollector hc = new CountingHitCollector();
		  s.search(oq, hc);
		  nMatches += hc.Count;
		  ret += hc.Sum;
		}
		if (VERBOSE)
		{
			Console.WriteLine("Average number of matches=" + (nMatches / iter));
		}
		return ret;
	  }


		public virtual int DoSloppyPhrase(IndexSearcher s, int termsInIndex, int maxClauses, int iter)
		{
		int ret = 0;

		for (int i = 0; i < iter; i++)
		{
		  int nClauses = random().Next(maxClauses - 1) + 2; // min 2 clauses
		  PhraseQuery q = new PhraseQuery();
		  for (int j = 0; j < nClauses; j++)
		  {
			int tnum = random().Next(termsInIndex);
			q.add(new Term("f",char.ToString((char)(tnum + 'A'))), j);
		  }
		  q.Slop = termsInIndex; // this could be random too

		  CountingHitCollector hc = new CountingHitCollector();
		  s.search(q, hc);
		  ret += hc.Sum;
		}

		return ret;
		}


	  public virtual void TestConjunctions()
	  {
		// test many small sets... the bugs will be found on boundary conditions
		CreateDummySearcher();
		Validate = true;
		Sets = RandBitSets(atLeast(1000), atLeast(10));
		DoConjunctions(atLeast(10000), atLeast(5));
		DoNestedConjunctions(atLeast(10000), atLeast(3), atLeast(3));
		r.close();
		d.close();
	  }

	  /// <summary>
	  ///*
	  /// int bigIter=10;
	  /// 
	  /// public void testConjunctionPerf() throws Exception {
	  ///  r = newRandom();
	  ///  createDummySearcher();
	  ///  validate=false;
	  ///  sets=randBitSets(32,1000000);
	  ///  for (int i=0; i<bigIter; i++) {
	  ///    long start = System.currentTimeMillis();
	  ///    doConjunctions(500,6);
	  ///    long end = System.currentTimeMillis();
	  ///    if (VERBOSE) System.out.println("milliseconds="+(end-start));
	  ///  }
	  ///  s.close();
	  /// }
	  /// 
	  /// public void testNestedConjunctionPerf() throws Exception {
	  ///  r = newRandom();
	  ///  createDummySearcher();
	  ///  validate=false;
	  ///  sets=randBitSets(32,1000000);
	  ///  for (int i=0; i<bigIter; i++) {
	  ///    long start = System.currentTimeMillis();
	  ///    doNestedConjunctions(500,3,3);
	  ///    long end = System.currentTimeMillis();
	  ///    if (VERBOSE) System.out.println("milliseconds="+(end-start));
	  ///  }
	  ///  s.close();
	  /// }
	  /// 
	  /// 
	  /// public void testConjunctionTerms() throws Exception {
	  ///  r = newRandom();
	  ///  validate=false;
	  ///  RAMDirectory dir = new RAMDirectory();
	  ///  if (VERBOSE) System.out.println("Creating index");
	  ///  createRandomTerms(100000,25,.5, dir);
	  ///  s = newSearcher(dir, true);
	  ///  if (VERBOSE) System.out.println("Starting performance test");
	  ///  for (int i=0; i<bigIter; i++) {
	  ///    long start = System.currentTimeMillis();
	  ///    doTermConjunctions(s,25,5,1000);
	  ///    long end = System.currentTimeMillis();
	  ///    if (VERBOSE) System.out.println("milliseconds="+(end-start));
	  ///  }
	  ///  s.close();
	  /// }
	  /// 
	  /// public void testNestedConjunctionTerms() throws Exception {
	  ///  r = newRandom();
	  ///  validate=false;    
	  ///  RAMDirectory dir = new RAMDirectory();
	  ///  if (VERBOSE) System.out.println("Creating index");
	  ///  createRandomTerms(100000,25,.2, dir);
	  ///  s = newSearcher(dir, true);
	  ///  if (VERBOSE) System.out.println("Starting performance test");
	  ///  for (int i=0; i<bigIter; i++) {
	  ///    long start = System.currentTimeMillis();
	  ///    doNestedTermConjunctions(s,25,3,3,200);
	  ///    long end = System.currentTimeMillis();
	  ///    if (VERBOSE) System.out.println("milliseconds="+(end-start));
	  ///  }
	  ///  s.close();
	  /// }
	  /// 
	  /// 
	  /// public void testSloppyPhrasePerf() throws Exception {
	  ///  r = newRandom();
	  ///  validate=false;    
	  ///  RAMDirectory dir = new RAMDirectory();
	  ///  if (VERBOSE) System.out.println("Creating index");
	  ///  createRandomTerms(100000,25,2,dir);
	  ///  s = newSearcher(dir, true);
	  ///  if (VERBOSE) System.out.println("Starting performance test");
	  ///  for (int i=0; i<bigIter; i++) {
	  ///    long start = System.currentTimeMillis();
	  ///    doSloppyPhrase(s,25,2,1000);
	  ///    long end = System.currentTimeMillis();
	  ///    if (VERBOSE) System.out.println("milliseconds="+(end-start));
	  ///  }
	  ///  s.close();
	  /// }
	  /// **
	  /// </summary>


	}

}