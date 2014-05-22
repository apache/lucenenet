using System;

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
	using Lucene.Net.Document;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	/// <summary>
	/// Document boost unit test.
	/// 
	/// 
	/// </summary>
	public class TestDocBoost : LuceneTestCase
	{

	  public virtual void TestDocBoost()
	  {
		Directory store = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), store, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));

		Field f1 = newTextField("field", "word", Field.Store.YES);
		Field f2 = newTextField("field", "word", Field.Store.YES);
		f2.Boost = 2.0f;

		Document d1 = new Document();
		Document d2 = new Document();

		d1.add(f1); // boost = 1
		d2.add(f2); // boost = 2

		writer.addDocument(d1);
		writer.addDocument(d2);

		IndexReader reader = writer.Reader;
		writer.close();

		float[] scores = new float[4];

		IndexSearcher searcher = newSearcher(reader);
		searcher.search(new TermQuery(new Term("field", "word")), new CollectorAnonymousInnerClassHelper(this, scores));

		float lastScore = 0.0f;

		for (int i = 0; i < 2; i++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine(searcher.explain(new TermQuery(new Term("field", "word")), i));
		  }
		  Assert.IsTrue("score: " + scores[i] + " should be > lastScore: " + lastScore, scores[i] > lastScore);
		  lastScore = scores[i];
		}

		reader.close();
		store.close();
	  }

	  private class CollectorAnonymousInnerClassHelper : Collector
	  {
		  private readonly TestDocBoost OuterInstance;

		  private float[] Scores;

		  public CollectorAnonymousInnerClassHelper(TestDocBoost outerInstance, float[] scores)
		  {
			  this.OuterInstance = outerInstance;
			  this.Scores = scores;
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
			Scores[doc + @base] = scorer.score();
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
	}

}