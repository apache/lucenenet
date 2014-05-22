using System.Collections.Generic;
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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using CollectionStatistics = Lucene.Net.Search.CollectionStatistics;
	using TermStatistics = Lucene.Net.Search.TermStatistics;
	using Similarity = Lucene.Net.Search.Similarities.Similarity;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Tests the uniqueTermCount statistic in FieldInvertState
	/// </summary>
	public class TestUniqueTermCount : LuceneTestCase
	{
	  internal Directory Dir;
	  internal IndexReader Reader;
	  /* expected uniqueTermCount values for our documents */
	  internal List<int?> Expected = new List<int?>();

	  public override void SetUp()
	  {
		base.setUp();
		Dir = newDirectory();
		MockAnalyzer analyzer = new MockAnalyzer(random(), MockTokenizer.SIMPLE, true);
		IndexWriterConfig config = newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
		config.MergePolicy = newLogMergePolicy();
		config.Similarity = new TestSimilarity(this);
		RandomIndexWriter writer = new RandomIndexWriter(random(), Dir, config);
		Document doc = new Document();
		Field foo = newTextField("foo", "", Field.Store.NO);
		doc.add(foo);
		for (int i = 0; i < 100; i++)
		{
		  foo.StringValue = AddValue();
		  writer.addDocument(doc);
		}
		Reader = writer.Reader;
		writer.close();
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Dir.close();
		base.tearDown();
	  }

	  public virtual void Test()
	  {
		NumericDocValues fooNorms = MultiDocValues.getNormValues(Reader, "foo");
		Assert.IsNotNull(fooNorms);
		for (int i = 0; i < Reader.maxDoc(); i++)
		{
		  Assert.AreEqual((long)Expected[i], fooNorms.get(i));
		}
	  }

	  /// <summary>
	  /// Makes a bunch of single-char tokens (the max # unique terms will at most be 26).
	  /// puts the # unique terms into expected, to be checked against the norm.
	  /// </summary>
	  private string AddValue()
	  {
		StringBuilder sb = new StringBuilder();
		HashSet<string> terms = new HashSet<string>();
		int num = TestUtil.Next(random(), 0, 255);
		for (int i = 0; i < num; i++)
		{
		  sb.Append(' ');
		  char term = (char) TestUtil.Next(random(), 'a', 'z');
		  sb.Append(term);
		  terms.Add("" + term);
		}
		Expected.Add(terms.Count);
		return sb.ToString();
	  }

	  /// <summary>
	  /// Simple similarity that encodes maxTermFrequency directly
	  /// </summary>
	  internal class TestSimilarity : Similarity
	  {
		  private readonly TestUniqueTermCount OuterInstance;

		  public TestSimilarity(TestUniqueTermCount outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }


		public override long ComputeNorm(FieldInvertState state)
		{
		  return state.UniqueTermCount;
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