using System;
using System.Text;

namespace Lucene.Net.Index
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
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using Occur = Lucene.Net.Search.BooleanClause.Occur_e;
	using BooleanQuery = Lucene.Net.Search.BooleanQuery;
	using CollectionStatistics = Lucene.Net.Search.CollectionStatistics;
	using Collector = Lucene.Net.Search.Collector;
	using Explanation = Lucene.Net.Search.Explanation;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using PhraseQuery = Lucene.Net.Search.PhraseQuery;
	using Scorer = Lucene.Net.Search.Scorer;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using TermStatistics = Lucene.Net.Search.TermStatistics;
	using TFIDFSimilarity = Lucene.Net.Search.Similarities.TFIDFSimilarity;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;


	public class TestOmitTf : LuceneTestCase
	{

	  public class SimpleSimilarity : TFIDFSimilarity
	  {
		public override float DecodeNormValue(long norm)
		{
			return norm;
		}
		public override long EncodeNormValue(float f)
		{
			return (long) f;
		}
		public override float QueryNorm(float sumOfSquaredWeights)
		{
			return 1.0f;
		}
		public override float Coord(int overlap, int maxOverlap)
		{
			return 1.0f;
		}
		public override float LengthNorm(FieldInvertState state)
		{
			return state.Boost;
		}
		public override float Tf(float freq)
		{
			return freq;
		}
		public override float SloppyFreq(int distance)
		{
			return 2.0f;
		}
		public override float Idf(long docFreq, long numDocs)
		{
			return 1.0f;
		}
		public override Explanation IdfExplain(CollectionStatistics collectionStats, TermStatistics[] termStats)
		{
		  return new Explanation(1.0f, "Inexplicable");
		}
		public override float ScorePayload(int doc, int start, int end, BytesRef payload)
		{
			return 1.0f;
		}
	  }

	  private static readonly FieldType OmitType = new FieldType(TextField.TYPE_NOT_STORED);
	  private static readonly FieldType NormalType = new FieldType(TextField.TYPE_NOT_STORED);

	  static TestOmitTf()
	  {
		OmitType.IndexOptions = IndexOptions.DOCS_ONLY;
	  }

	  // Tests whether the DocumentWriter correctly enable the
	  // omitTermFreqAndPositions bit in the FieldInfo
	  public virtual void TestOmitTermFreqAndPositions()
	  {
		Directory ram = newDirectory();
		Analyzer analyzer = new MockAnalyzer(random());
		IndexWriter writer = new IndexWriter(ram, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
		Document d = new Document();

		// this field will have Tf
		Field f1 = newField("f1", "this field has term freqs", NormalType);
		d.add(f1);

		// this field will NOT have Tf
		Field f2 = newField("f2", "this field has NO Tf in all docs", OmitType);
		d.add(f2);

		writer.addDocument(d);
		writer.forceMerge(1);
		// now we add another document which has term freq for field f2 and not for f1 and verify if the SegmentMerger
		// keep things constant
		d = new Document();

		// Reverse
		f1 = newField("f1", "this field has term freqs", OmitType);
		d.add(f1);

		f2 = newField("f2", "this field has NO Tf in all docs", NormalType);
		d.add(f2);

		writer.addDocument(d);

		// force merge
		writer.forceMerge(1);
		// flush
		writer.close();

		SegmentReader reader = getOnlySegmentReader(DirectoryReader.open(ram));
		FieldInfos fi = reader.FieldInfos;
		Assert.AreEqual("OmitTermFreqAndPositions field bit should be set.", IndexOptions.DOCS_ONLY, fi.fieldInfo("f1").IndexOptions);
		Assert.AreEqual("OmitTermFreqAndPositions field bit should be set.", IndexOptions.DOCS_ONLY, fi.fieldInfo("f2").IndexOptions);

		reader.close();
		ram.close();
	  }

	  // Tests whether merging of docs that have different
	  // omitTermFreqAndPositions for the same field works
	  public virtual void TestMixedMerge()
	  {
		Directory ram = newDirectory();
		Analyzer analyzer = new MockAnalyzer(random());
		IndexWriter writer = new IndexWriter(ram, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMaxBufferedDocs(3).setMergePolicy(newLogMergePolicy(2)));
		Document d = new Document();

		// this field will have Tf
		Field f1 = newField("f1", "this field has term freqs", NormalType);
		d.add(f1);

		// this field will NOT have Tf
		Field f2 = newField("f2", "this field has NO Tf in all docs", OmitType);
		d.add(f2);

		for (int i = 0;i < 30;i++)
		{
		  writer.addDocument(d);
		}

		// now we add another document which has term freq for field f2 and not for f1 and verify if the SegmentMerger
		// keep things constant
		d = new Document();

		// Reverese
		f1 = newField("f1", "this field has term freqs", OmitType);
		d.add(f1);

		f2 = newField("f2", "this field has NO Tf in all docs", NormalType);
		d.add(f2);

		for (int i = 0;i < 30;i++)
		{
		  writer.addDocument(d);
		}

		// force merge
		writer.forceMerge(1);
		// flush
		writer.close();

		SegmentReader reader = getOnlySegmentReader(DirectoryReader.open(ram));
		FieldInfos fi = reader.FieldInfos;
		Assert.AreEqual("OmitTermFreqAndPositions field bit should be set.", IndexOptions.DOCS_ONLY, fi.fieldInfo("f1").IndexOptions);
		Assert.AreEqual("OmitTermFreqAndPositions field bit should be set.", IndexOptions.DOCS_ONLY, fi.fieldInfo("f2").IndexOptions);

		reader.close();
		ram.close();
	  }

	  // Make sure first adding docs that do not omitTermFreqAndPositions for
	  // field X, then adding docs that do omitTermFreqAndPositions for that same
	  // field, 
	  public virtual void TestMixedRAM()
	  {
		Directory ram = newDirectory();
		Analyzer analyzer = new MockAnalyzer(random());
		IndexWriter writer = new IndexWriter(ram, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMaxBufferedDocs(10).setMergePolicy(newLogMergePolicy(2)));
		Document d = new Document();

		// this field will have Tf
		Field f1 = newField("f1", "this field has term freqs", NormalType);
		d.add(f1);

		// this field will NOT have Tf
		Field f2 = newField("f2", "this field has NO Tf in all docs", OmitType);
		d.add(f2);

		for (int i = 0;i < 5;i++)
		{
		  writer.addDocument(d);
		}

		for (int i = 0;i < 20;i++)
		{
		  writer.addDocument(d);
		}

		// force merge
		writer.forceMerge(1);

		// flush
		writer.close();

		SegmentReader reader = getOnlySegmentReader(DirectoryReader.open(ram));
		FieldInfos fi = reader.FieldInfos;
		Assert.AreEqual("OmitTermFreqAndPositions field bit should not be set.", IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, fi.fieldInfo("f1").IndexOptions);
		Assert.AreEqual("OmitTermFreqAndPositions field bit should be set.", IndexOptions.DOCS_ONLY, fi.fieldInfo("f2").IndexOptions);

		reader.close();
		ram.close();
	  }

	  private void AssertNoPrx(Directory dir)
	  {
		string[] files = dir.listAll();
		for (int i = 0;i < files.Length;i++)
		{
		  Assert.IsFalse(files[i].EndsWith(".prx"));
		  Assert.IsFalse(files[i].EndsWith(".pos"));
		}
	  }

	  // Verifies no *.prx exists when all fields omit term freq:
	  public virtual void TestNoPrxFile()
	  {
		Directory ram = newDirectory();
		Analyzer analyzer = new MockAnalyzer(random());
		IndexWriter writer = new IndexWriter(ram, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMaxBufferedDocs(3).setMergePolicy(newLogMergePolicy()));
		LogMergePolicy lmp = (LogMergePolicy) writer.Config.MergePolicy;
		lmp.MergeFactor = 2;
		lmp.NoCFSRatio = 0.0;
		Document d = new Document();

		Field f1 = newField("f1", "this field has term freqs", OmitType);
		d.add(f1);

		for (int i = 0;i < 30;i++)
		{
		  writer.addDocument(d);
		}

		writer.commit();

		AssertNoPrx(ram);

		// now add some documents with positions, and check
		// there is no prox after full merge
		d = new Document();
		f1 = newTextField("f1", "this field has positions", Field.Store.NO);
		d.add(f1);

		for (int i = 0;i < 30;i++)
		{
		  writer.addDocument(d);
		}

		// force merge
		writer.forceMerge(1);
		// flush
		writer.close();

		AssertNoPrx(ram);
		ram.close();
	  }

	  // Test scores with one field with Term Freqs and one without, otherwise with equal content 
	  public virtual void TestBasic()
	  {
		Directory dir = newDirectory();
		Analyzer analyzer = new MockAnalyzer(random());
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMaxBufferedDocs(2).setSimilarity(new SimpleSimilarity()).setMergePolicy(newLogMergePolicy(2)));

		StringBuilder sb = new StringBuilder(265);
		string term = "term";
		for (int i = 0; i < 30; i++)
		{
		  Document d = new Document();
		  sb.Append(term).Append(" ");
		  string content = sb.ToString();
		  Field noTf = newField("noTf", content + (i % 2 == 0 ? "" : " notf"), OmitType);
		  d.add(noTf);

		  Field tf = newField("tf", content + (i % 2 == 0 ? " tf" : ""), NormalType);
		  d.add(tf);

		  writer.addDocument(d);
		  //System.out.println(d);
		}

		writer.forceMerge(1);
		// flush
		writer.close();

		/*
		 * Verify the index
		 */         
		IndexReader reader = DirectoryReader.open(dir);
		IndexSearcher searcher = newSearcher(reader);
		searcher.Similarity = new SimpleSimilarity();

		Term a = new Term("noTf", term);
		Term b = new Term("tf", term);
		Term c = new Term("noTf", "notf");
		Term d = new Term("tf", "tf");
		TermQuery q1 = new TermQuery(a);
		TermQuery q2 = new TermQuery(b);
		TermQuery q3 = new TermQuery(c);
		TermQuery q4 = new TermQuery(d);

		PhraseQuery pq = new PhraseQuery();
		pq.add(a);
		pq.add(c);
		try
		{
		  searcher.search(pq, 10);
		  Assert.Fail("did not hit expected exception");
		}
		catch (Exception e)
		{
		  Exception cause = e;
		  // If the searcher uses an executor service, the IAE is wrapped into other exceptions
		  while (cause.InnerException != null)
		  {
			cause = cause.InnerException;
		  }
		  if (!(cause is IllegalStateException))
		  {
			throw new AssertionError("Expected an IAE", e);
		  } // else OK because positions are not indexed
		}

		searcher.search(q1, new CountingHitCollectorAnonymousInnerClassHelper(this));
		//System.out.println(CountingHitCollector.getCount());


		searcher.search(q2, new CountingHitCollectorAnonymousInnerClassHelper2(this));
		//System.out.println(CountingHitCollector.getCount());





		searcher.search(q3, new CountingHitCollectorAnonymousInnerClassHelper3(this));
		//System.out.println(CountingHitCollector.getCount());


		searcher.search(q4, new CountingHitCollectorAnonymousInnerClassHelper4(this));
		//System.out.println(CountingHitCollector.getCount());



		BooleanQuery bq = new BooleanQuery();
		bq.add(q1,Occur.MUST);
		bq.add(q4,Occur.MUST);

		searcher.search(bq, new CountingHitCollectorAnonymousInnerClassHelper5(this));
		Assert.AreEqual(15, CountingHitCollector.Count);

		reader.close();
		dir.close();
	  }

	  private class CountingHitCollectorAnonymousInnerClassHelper : CountingHitCollector
	  {
		  private readonly TestOmitTf OuterInstance;

		  public CountingHitCollectorAnonymousInnerClassHelper(TestOmitTf outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  private Scorer scorer;
		  public override sealed Scorer Scorer
		  {
			  set
			  {
				this.scorer = value;
			  }
		  }
		  public override sealed void Collect(int doc)
		  {
			//System.out.println("Q1: Doc=" + doc + " score=" + score);
			float score = scorer.score();
			Assert.IsTrue("got score=" + score, score == 1.0f);
			base.collect(doc);
		  }
	  }

	  private class CountingHitCollectorAnonymousInnerClassHelper2 : CountingHitCollector
	  {
		  private readonly TestOmitTf OuterInstance;

		  public CountingHitCollectorAnonymousInnerClassHelper2(TestOmitTf outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  private Scorer scorer;
		  public override sealed Scorer Scorer
		  {
			  set
			  {
				this.scorer = value;
			  }
		  }
		  public override sealed void Collect(int doc)
		  {
			//System.out.println("Q2: Doc=" + doc + " score=" + score);
			float score = scorer.score();
			Assert.AreEqual(1.0f + doc, score, 0.00001f);
			base.collect(doc);
		  }
	  }

	  private class CountingHitCollectorAnonymousInnerClassHelper3 : CountingHitCollector
	  {
		  private readonly TestOmitTf OuterInstance;

		  public CountingHitCollectorAnonymousInnerClassHelper3(TestOmitTf outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  private Scorer scorer;
		  public override sealed Scorer Scorer
		  {
			  set
			  {
				this.scorer = value;
			  }
		  }
		  public override sealed void Collect(int doc)
		  {
			//System.out.println("Q1: Doc=" + doc + " score=" + score);
			float score = scorer.score();
			Assert.IsTrue(score == 1.0f);
			Assert.IsFalse(doc % 2 == 0);
			base.collect(doc);
		  }
	  }

	  private class CountingHitCollectorAnonymousInnerClassHelper4 : CountingHitCollector
	  {
		  private readonly TestOmitTf OuterInstance;

		  public CountingHitCollectorAnonymousInnerClassHelper4(TestOmitTf outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  private Scorer scorer;
		  public override sealed Scorer Scorer
		  {
			  set
			  {
				this.scorer = value;
			  }
		  }
		  public override sealed void Collect(int doc)
		  {
			float score = scorer.score();
			//System.out.println("Q1: Doc=" + doc + " score=" + score);
			Assert.IsTrue(score == 1.0f);
			Assert.IsTrue(doc % 2 == 0);
			base.collect(doc);
		  }
	  }

	  private class CountingHitCollectorAnonymousInnerClassHelper5 : CountingHitCollector
	  {
		  private readonly TestOmitTf OuterInstance;

		  public CountingHitCollectorAnonymousInnerClassHelper5(TestOmitTf outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override sealed void Collect(int doc)
		  {
			//System.out.println("BQ: Doc=" + doc + " score=" + score);
			base.collect(doc);
		  }
	  }

	  public class CountingHitCollector : Collector
	  {
		internal static int Count_Renamed = 0;
		internal static int Sum_Renamed = 0;
		internal int DocBase = -1;
		internal CountingHitCollector()
		{
			Count_Renamed = 0;
			Sum_Renamed = 0;
		}
		public override Scorer Scorer
		{
			set
			{
			}
		}
		public override void Collect(int doc)
		{
		  Count_Renamed++;
		  Sum_Renamed += doc + DocBase; // use it to avoid any possibility of being merged away
		}

		public static int Count
		{
			get
			{
				return Count_Renamed;
			}
		}
		public static int Sum
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

	  /// <summary>
	  /// test that when freqs are omitted, that totalTermFreq and sumTotalTermFreq are -1 </summary>
	  public virtual void TestStats()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
		ft.IndexOptions = IndexOptions.DOCS_ONLY;
		ft.freeze();
		Field f = newField("foo", "bar", ft);
		doc.add(f);
		iw.addDocument(doc);
		IndexReader ir = iw.Reader;
		iw.close();
		Assert.AreEqual(-1, ir.totalTermFreq(new Term("foo", new BytesRef("bar"))));
		Assert.AreEqual(-1, ir.getSumTotalTermFreq("foo"));
		ir.close();
		dir.close();
	  }
	}

}