using System;

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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using TextField = Lucene.Net.Document.TextField;
	using CollectionStatistics = Lucene.Net.Search.CollectionStatistics;
	using TermStatistics = Lucene.Net.Search.TermStatistics;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using PerFieldSimilarityWrapper = Lucene.Net.Search.Similarities.PerFieldSimilarityWrapper;
	using Similarity = Lucene.Net.Search.Similarities.Similarity;
	using TFIDFSimilarity = Lucene.Net.Search.Similarities.TFIDFSimilarity;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LineFileDocs = Lucene.Net.Util.LineFileDocs;
	using Slow = Lucene.Net.Util.LuceneTestCase.Slow;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Test that norms info is preserved during index life - including
	/// separate norms, addDocument, addIndexes, forceMerge.
	/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({ "Memory", "Direct", "SimpleText" }) @Slow public class TestNorms extends Lucene.Net.Util.LuceneTestCase
	public class TestNorms : LuceneTestCase
	{
	  internal readonly string ByteTestField = "normsTestByte";

	  internal class CustomNormEncodingSimilarity : TFIDFSimilarity
	  {
		  private readonly TestNorms OuterInstance;

		  public CustomNormEncodingSimilarity(TestNorms outerInstance)
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

		public override float LengthNorm(FieldInvertState state)
		{
		  return state.Length;
		}

		public override float Coord(int overlap, int maxOverlap)
		{
			return 0;
		}
		public override float QueryNorm(float sumOfSquaredWeights)
		{
			return 0;
		}
		public override float Tf(float freq)
		{
			return 0;
		}
		public override float Idf(long docFreq, long numDocs)
		{
			return 0;
		}
		public override float SloppyFreq(int distance)
		{
			return 0;
		}
		public override float ScorePayload(int doc, int start, int end, BytesRef payload)
		{
			return 0;
		}
	  }

	  // LUCENE-1260
	  public virtual void TestCustomEncoder()
	  {
		Directory dir = newDirectory();
		MockAnalyzer analyzer = new MockAnalyzer(random());

		IndexWriterConfig config = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		config.Similarity = new CustomNormEncodingSimilarity(this);
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir, config);
		Document doc = new Document();
		Field foo = newTextField("foo", "", Field.Store.NO);
		Field bar = newTextField("bar", "", Field.Store.NO);
		doc.add(foo);
		doc.add(bar);

		for (int i = 0; i < 100; i++)
		{
		  bar.StringValue = "singleton";
		  writer.addDocument(doc);
		}

		IndexReader reader = writer.Reader;
		writer.close();

		NumericDocValues fooNorms = MultiDocValues.getNormValues(reader, "foo");
		for (int i = 0; i < reader.maxDoc(); i++)
		{
		  Assert.AreEqual(0, fooNorms.get(i));
		}

		NumericDocValues barNorms = MultiDocValues.getNormValues(reader, "bar");
		for (int i = 0; i < reader.maxDoc(); i++)
		{
		  Assert.AreEqual(1, barNorms.get(i));
		}

		reader.close();
		dir.close();
	  }

	  public virtual void TestMaxByteNorms()
	  {
		Directory dir = newFSDirectory(createTempDir("TestNorms.testMaxByteNorms"));
		BuildIndex(dir);
		AtomicReader open = SlowCompositeReaderWrapper.wrap(DirectoryReader.open(dir));
		NumericDocValues normValues = open.getNormValues(ByteTestField);
		Assert.IsNotNull(normValues);
		for (int i = 0; i < open.maxDoc(); i++)
		{
		  Document document = open.document(i);
		  int expected = Convert.ToInt32(document.get(ByteTestField));
		  Assert.AreEqual(expected, normValues.get(i) & 0xff);
		}
		open.close();
		dir.close();
	  }

	  // TODO: create a testNormsNotPresent ourselves by adding/deleting/merging docs

	  public virtual void BuildIndex(Directory dir)
	  {
		Random random = random();
		MockAnalyzer analyzer = new MockAnalyzer(random());
		analyzer.MaxTokenLength = TestUtil.Next(random(), 1, IndexWriter.MAX_TERM_LENGTH);
		IndexWriterConfig config = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		Similarity provider = new MySimProvider(this);
		config.Similarity = provider;
		RandomIndexWriter writer = new RandomIndexWriter(random, dir, config);
		LineFileDocs docs = new LineFileDocs(random, defaultCodecSupportsDocValues());
		int num = atLeast(100);
		for (int i = 0; i < num; i++)
		{
		  Document doc = docs.nextDoc();
		  int boost = random().Next(255);
		  Field f = new TextField(ByteTestField, "" + boost, Field.Store.YES);
		  f.Boost = boost;
		  doc.add(f);
		  writer.addDocument(doc);
		  doc.removeField(ByteTestField);
		  if (rarely())
		  {
			writer.commit();
		  }
		}
		writer.commit();
		writer.close();
		docs.close();
	  }


	  public class MySimProvider : PerFieldSimilarityWrapper
	  {
		  private readonly TestNorms OuterInstance;

		  public MySimProvider(TestNorms outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		internal Similarity @delegate = new DefaultSimilarity();

		public override float QueryNorm(float sumOfSquaredWeights)
		{

		  return @delegate.queryNorm(sumOfSquaredWeights);
		}

		public override Similarity Get(string field)
		{
		  if (outerInstance.ByteTestField.Equals(field))
		  {
			return new ByteEncodingBoostSimilarity();
		  }
		  else
		  {
			return @delegate;
		  }
		}

		public override float Coord(int overlap, int maxOverlap)
		{
		  return @delegate.coord(overlap, maxOverlap);
		}
	  }


	  public class ByteEncodingBoostSimilarity : Similarity
	  {

		public override long ComputeNorm(FieldInvertState state)
		{
		  int boost = (int) state.Boost;
		  return (sbyte) boost;
		}

		public override SimWeight ComputeWeight(float queryBoost, CollectionStatistics collectionStats, params TermStatistics[] termStats)
		{
		  throw new System.NotSupportedException();
		}

		public override SimScorer SimScorer(SimWeight weight, AtomicReaderContext context)
		{
		  throw new System.NotSupportedException();
		}
	  }
	}

}