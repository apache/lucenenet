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

	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using Store = Lucene.Net.Document.Field.Store;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using FieldInvertState = Lucene.Net.Index.FieldInvertState;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Similarity = Lucene.Net.Search.Similarities.Similarity;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestConjunctions : LuceneTestCase
	{
	  internal Analyzer Analyzer;
	  internal Directory Dir;
	  internal IndexReader Reader;
	  internal IndexSearcher Searcher;

	  internal const string F1 = "title";
	  internal const string F2 = "body";

	  public override void SetUp()
	  {
		base.setUp();
		Analyzer = new MockAnalyzer(random());
		Dir = newDirectory();
		IndexWriterConfig config = newIndexWriterConfig(TEST_VERSION_CURRENT, Analyzer);
		config.MergePolicy = newLogMergePolicy(); // we will use docids to validate
		RandomIndexWriter writer = new RandomIndexWriter(random(), Dir, config);
		writer.addDocument(Doc("lucene", "lucene is a very popular search engine library"));
		writer.addDocument(Doc("solr", "solr is a very popular search server and is using lucene"));
		writer.addDocument(Doc("nutch", "nutch is an internet search engine with web crawler and is using lucene and hadoop"));
		Reader = writer.Reader;
		writer.close();
		Searcher = newSearcher(Reader);
		Searcher.Similarity = new TFSimilarity();
	  }

	  internal static Document Doc(string v1, string v2)
	  {
		Document doc = new Document();
		doc.add(new StringField(F1, v1, Store.YES));
		doc.add(new TextField(F2, v2, Store.YES));
		return doc;
	  }

	  public virtual void TestTermConjunctionsWithOmitTF()
	  {
		BooleanQuery bq = new BooleanQuery();
		bq.add(new TermQuery(new Term(F1, "nutch")), BooleanClause.Occur_e.MUST);
		bq.add(new TermQuery(new Term(F2, "is")), BooleanClause.Occur_e.MUST);
		TopDocs td = Searcher.search(bq, 3);
		Assert.AreEqual(1, td.totalHits);
		Assert.AreEqual(3F, td.scoreDocs[0].score, 0.001F); // f1:nutch + f2:is + f2:is
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Dir.close();
		base.tearDown();
	  }

	  // Similarity that returns the TF as score
	  private class TFSimilarity : Similarity
	  {

		public override long ComputeNorm(FieldInvertState state)
		{
		  return 1; // we dont care
		}

		public override SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
		{
		  return new SimWeightAnonymousInnerClassHelper(this);
		}

		private class SimWeightAnonymousInnerClassHelper : SimWeight
		{
			private readonly TFSimilarity OuterInstance;

			public SimWeightAnonymousInnerClassHelper(TFSimilarity outerInstance)
			{
				this.OuterInstance = outerInstance;
			}

			public override float ValueForNormalization
			{
				get
				{
				  return 1; // we don't care
				}
			}
			public override void Normalize(float queryNorm, float topLevelBoost)
			{
			  // we don't care
			}
		}

		public override SimScorer SimScorer(SimWeight weight, AtomicReaderContext context)
		{
		  return new SimScorerAnonymousInnerClassHelper(this);
		}

		private class SimScorerAnonymousInnerClassHelper : SimScorer
		{
			private readonly TFSimilarity OuterInstance;

			public SimScorerAnonymousInnerClassHelper(TFSimilarity outerInstance)
			{
				this.OuterInstance = outerInstance;
			}

			public override float Score(int doc, float freq)
			{
			  return freq;
			}

			public override float ComputeSlopFactor(int distance)
			{
			  return 1F;
			}

			public override float ComputePayloadFactor(int doc, int start, int end, BytesRef payload)
			{
			  return 1F;
			}
		}
	  }
	}

}