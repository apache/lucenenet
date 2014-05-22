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
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
	using Term = Lucene.Net.Index.Term;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestTermScorer : LuceneTestCase
	{
	  protected internal Directory Directory;
	  private const string FIELD = "field";

	  protected internal string[] Values = new string[] {"all", "dogs dogs", "like", "playing", "fetch", "all"};
	  protected internal IndexSearcher IndexSearcher;
	  protected internal IndexReader IndexReader;

	  public override void SetUp()
	  {
		base.setUp();
		Directory = newDirectory();

		RandomIndexWriter writer = new RandomIndexWriter(random(), Directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()).setSimilarity(new DefaultSimilarity()));
		for (int i = 0; i < Values.Length; i++)
		{
		  Document doc = new Document();
		  doc.add(newTextField(FIELD, Values[i], Field.Store.YES));
		  writer.addDocument(doc);
		}
		IndexReader = SlowCompositeReaderWrapper.wrap(writer.Reader);
		writer.close();
		IndexSearcher = newSearcher(IndexReader);
		IndexSearcher.Similarity = new DefaultSimilarity();
	  }

	  public override void TearDown()
	  {
		IndexReader.close();
		Directory.close();
		base.tearDown();
	  }

	  public virtual void Test()
	  {

		Term allTerm = new Term(FIELD, "all");
		TermQuery termQuery = new TermQuery(allTerm);

		Weight weight = IndexSearcher.createNormalizedWeight(termQuery);
		Assert.IsTrue(IndexSearcher.TopReaderContext is AtomicReaderContext);
		AtomicReaderContext context = (AtomicReaderContext)IndexSearcher.TopReaderContext;
		BulkScorer ts = weight.bulkScorer(context, true, context.reader().LiveDocs);
		// we have 2 documents with the term all in them, one document for all the
		// other values
		IList<TestHit> docs = new List<TestHit>();
		// must call next first

		ts.score(new CollectorAnonymousInnerClassHelper(this, context, docs));
		Assert.IsTrue("docs Size: " + docs.Count + " is not: " + 2, docs.Count == 2);
		TestHit doc0 = docs[0];
		TestHit doc5 = docs[1];
		// The scores should be the same
		Assert.IsTrue(doc0.Score + " does not equal: " + doc5.Score, doc0.Score == doc5.Score);
		/*
		 * Score should be (based on Default Sim.: All floats are approximate tf = 1
		 * numDocs = 6 docFreq(all) = 2 idf = ln(6/3) + 1 = 1.693147 idf ^ 2 =
		 * 2.8667 boost = 1 lengthNorm = 1 //there is 1 term in every document coord
		 * = 1 sumOfSquaredWeights = (idf * boost) ^ 2 = 1.693147 ^ 2 = 2.8667
		 * queryNorm = 1 / (sumOfSquaredWeights)^0.5 = 1 /(1.693147) = 0.590
		 * 
		 * score = 1 * 2.8667 * 1 * 1 * 0.590 = 1.69
		 */
		Assert.IsTrue(doc0.Score + " does not equal: " + 1.6931472f, doc0.Score == 1.6931472f);
	  }

	  private class CollectorAnonymousInnerClassHelper : Collector
	  {
		  private readonly TestTermScorer OuterInstance;

		  private AtomicReaderContext Context;
		  private IList<TestHit> Docs;

		  public CollectorAnonymousInnerClassHelper(TestTermScorer outerInstance, AtomicReaderContext context, IList<TestHit> docs)
		  {
			  this.OuterInstance = outerInstance;
			  this.Context = context;
			  this.Docs = docs;
			  @base = 0;
		  }

		  private int @base;
		  private Scorer scorer;

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
			doc = doc + @base;
			Docs.Add(new TestHit(OuterInstance, doc, score));
			Assert.IsTrue("score " + score + " is not greater than 0", score > 0);
			Assert.IsTrue("Doc: " + doc + " does not equal 0 or doc does not equal 5", doc == 0 || doc == 5);
		  }

		  public override AtomicReaderContext NextReader
		  {
			  set
			  {
				@base = value.docBase;
			  }
		  }

		  public override bool AcceptsDocsOutOfOrder()
		  {
			return true;
		  }
	  }

	  public virtual void TestNext()
	  {

		Term allTerm = new Term(FIELD, "all");
		TermQuery termQuery = new TermQuery(allTerm);

		Weight weight = IndexSearcher.createNormalizedWeight(termQuery);
		Assert.IsTrue(IndexSearcher.TopReaderContext is AtomicReaderContext);
		AtomicReaderContext context = (AtomicReaderContext) IndexSearcher.TopReaderContext;
		Scorer ts = weight.scorer(context, context.reader().LiveDocs);
		Assert.IsTrue("next did not return a doc", ts.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.IsTrue("score is not correct", ts.score() == 1.6931472f);
		Assert.IsTrue("next did not return a doc", ts.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		Assert.IsTrue("score is not correct", ts.score() == 1.6931472f);
		Assert.IsTrue("next returned a doc and it should not have", ts.nextDoc() == DocIdSetIterator.NO_MORE_DOCS);
	  }

	  public virtual void TestAdvance()
	  {

		Term allTerm = new Term(FIELD, "all");
		TermQuery termQuery = new TermQuery(allTerm);

		Weight weight = IndexSearcher.createNormalizedWeight(termQuery);
		Assert.IsTrue(IndexSearcher.TopReaderContext is AtomicReaderContext);
		AtomicReaderContext context = (AtomicReaderContext) IndexSearcher.TopReaderContext;
		Scorer ts = weight.scorer(context, context.reader().LiveDocs);
		Assert.IsTrue("Didn't skip", ts.advance(3) != DocIdSetIterator.NO_MORE_DOCS);
		// The next doc should be doc 5
		Assert.IsTrue("doc should be number 5", ts.docID() == 5);
	  }

	  private class TestHit
	  {
		  private readonly TestTermScorer OuterInstance;

		public int Doc;
		public float Score;

		public TestHit(TestTermScorer outerInstance, int doc, float score)
		{
			this.OuterInstance = outerInstance;
		  this.Doc = doc;
		  this.Score = score;
		}

		public override string ToString()
		{
		  return "TestHit{" + "doc=" + Doc + ", score=" + Score + "}";
		}
	  }

	}

}