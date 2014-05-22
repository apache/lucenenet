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
	using AtomicReader = Lucene.Net.Index.AtomicReader;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using FieldInvertState = Lucene.Net.Index.FieldInvertState;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using NumericDocValues = Lucene.Net.Index.NumericDocValues;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
	using Term = Lucene.Net.Index.Term;
	using PerFieldSimilarityWrapper = Lucene.Net.Search.Similarities.PerFieldSimilarityWrapper;
	using Similarity = Lucene.Net.Search.Similarities.Similarity;
	using TFIDFSimilarity = Lucene.Net.Search.Similarities.TFIDFSimilarity;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestSimilarityProvider : LuceneTestCase
	{
	  private Directory Directory;
	  private DirectoryReader Reader;
	  private IndexSearcher Searcher;

	  public override void SetUp()
	  {
		base.setUp();
		Directory = newDirectory();
		PerFieldSimilarityWrapper sim = new ExampleSimilarityProvider(this);
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setSimilarity(sim);
		RandomIndexWriter iw = new RandomIndexWriter(random(), Directory, iwc);
		Document doc = new Document();
		Field field = newTextField("foo", "", Field.Store.NO);
		doc.add(field);
		Field field2 = newTextField("bar", "", Field.Store.NO);
		doc.add(field2);

		field.StringValue = "quick brown fox";
		field2.StringValue = "quick brown fox";
		iw.addDocument(doc);
		field.StringValue = "jumps over lazy brown dog";
		field2.StringValue = "jumps over lazy brown dog";
		iw.addDocument(doc);
		Reader = iw.Reader;
		iw.close();
		Searcher = newSearcher(Reader);
		Searcher.Similarity = sim;
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Directory.close();
		base.tearDown();
	  }

	  public virtual void TestBasics()
	  {
		// sanity check of norms writer
		// TODO: generalize
		AtomicReader slow = SlowCompositeReaderWrapper.wrap(Reader);
		NumericDocValues fooNorms = slow.getNormValues("foo");
		NumericDocValues barNorms = slow.getNormValues("bar");
		for (int i = 0; i < slow.maxDoc(); i++)
		{
		  Assert.IsFalse(fooNorms.get(i) == barNorms.get(i));
		}

		// sanity check of searching
		TopDocs foodocs = Searcher.search(new TermQuery(new Term("foo", "brown")), 10);
		Assert.IsTrue(foodocs.totalHits > 0);
		TopDocs bardocs = Searcher.search(new TermQuery(new Term("bar", "brown")), 10);
		Assert.IsTrue(bardocs.totalHits > 0);
		Assert.IsTrue(foodocs.scoreDocs[0].score < bardocs.scoreDocs[0].score);
	  }

	  private class ExampleSimilarityProvider : PerFieldSimilarityWrapper
	  {
		  private readonly TestSimilarityProvider OuterInstance;

		  public ExampleSimilarityProvider(TestSimilarityProvider outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		internal Similarity Sim1 = new Sim1(OuterInstance);
		internal Similarity Sim2 = new Sim2(OuterInstance);

		public override Similarity Get(string field)
		{
		  if (field.Equals("foo"))
		  {
			return Sim1;
		  }
		  else
		  {
			return Sim2;
		  }
		}
	  }

	  private class Sim1 : TFIDFSimilarity
	  {
		  private readonly TestSimilarityProvider OuterInstance;

		  public Sim1(TestSimilarityProvider outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }


		public override long EncodeNormValue(float f)
		{
		  return (long) f;
		}

		public override float DecodeNormValue(long norm)
		{
		  return norm;
		}

		public override float Coord(int overlap, int maxOverlap)
		{
		  return 1f;
		}

		public override float QueryNorm(float sumOfSquaredWeights)
		{
		  return 1f;
		}

		public override float LengthNorm(FieldInvertState state)
		{
		  return 1f;
		}

		public override float SloppyFreq(int distance)
		{
		  return 1f;
		}

		public override float Tf(float freq)
		{
		  return 1f;
		}

		public override float Idf(long docFreq, long numDocs)
		{
		  return 1f;
		}

		public override float ScorePayload(int doc, int start, int end, BytesRef payload)
		{
		  return 1f;
		}
	  }

	  private class Sim2 : TFIDFSimilarity
	  {
		  private readonly TestSimilarityProvider OuterInstance;

		  public Sim2(TestSimilarityProvider outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }


		public override long EncodeNormValue(float f)
		{
		  return (long) f;
		}

		public override float DecodeNormValue(long norm)
		{
		  return norm;
		}

		public override float Coord(int overlap, int maxOverlap)
		{
		  return 1f;
		}

		public override float QueryNorm(float sumOfSquaredWeights)
		{
		  return 1f;
		}

		public override float LengthNorm(FieldInvertState state)
		{
		  return 10f;
		}

		public override float SloppyFreq(int distance)
		{
		  return 10f;
		}

		public override float Tf(float freq)
		{
		  return 10f;
		}

		public override float Idf(long docFreq, long numDocs)
		{
		  return 10f;
		}

		public override float ScorePayload(int doc, int start, int end, BytesRef payload)
		{
		  return 1f;
		}
	  }
	}

}