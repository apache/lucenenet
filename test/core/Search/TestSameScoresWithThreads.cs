using System;
using System.Collections.Generic;
using System.Threading;

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
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using MultiFields = Lucene.Net.Index.MultiFields;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LineFileDocs = Lucene.Net.Util.LineFileDocs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
    using NUnit.Framework;
    using Lucene.Net.Support;

	public class TestSameScoresWithThreads : LuceneTestCase
	{

	  public virtual void Test()
	  {
		Directory dir = NewDirectory();
		MockAnalyzer analyzer = new MockAnalyzer(Random());
		analyzer.MaxTokenLength = TestUtil.NextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH);
		RandomIndexWriter w = new RandomIndexWriter(Random(), dir, analyzer);
		LineFileDocs docs = new LineFileDocs(Random(), DefaultCodecSupportsDocValues());
		int charsToIndex = AtLeast(100000);
		int charsIndexed = 0;
		//System.out.println("bytesToIndex=" + charsToIndex);
		while (charsIndexed < charsToIndex)
		{
		  Document doc = docs.NextDoc();
		  charsIndexed += doc.Get("body").Length;
		  w.AddDocument(doc);
		  //System.out.println("  bytes=" + charsIndexed + " add: " + doc);
		}
		IndexReader r = w.Reader;
		//System.out.println("numDocs=" + r.NumDocs());
		w.Close();

		IndexSearcher s = NewSearcher(r);
		Terms terms = MultiFields.GetFields(r).Terms("body");
		int termCount = 0;
		TermsEnum termsEnum = terms.Iterator(null);
		while (termsEnum.Next() != null)
		{
		  termCount++;
		}
		Assert.IsTrue(termCount > 0);

		// Target ~10 terms to search:
		double chance = 10.0 / termCount;
		termsEnum = terms.Iterator(termsEnum);
		IDictionary<BytesRef, TopDocs> answers = new Dictionary<BytesRef, TopDocs>();
		while (termsEnum.Next() != null)
		{
		  if (Random().NextDouble() <= chance)
		  {
			BytesRef term = BytesRef.DeepCopyOf(termsEnum.Term());
			answers[term] = s.Search(new TermQuery(new Term("body", term)), 100);
		  }
		}

		if (answers.Count > 0)
		{
		  CountDownLatch startingGun = new CountDownLatch(1);
		  int numThreads = TestUtil.NextInt(Random(), 2, 5);
		  Thread[] threads = new Thread[numThreads];
		  for (int threadID = 0;threadID < numThreads;threadID++)
		  {
			Thread thread = new ThreadAnonymousInnerClassHelper(this, s, answers, startingGun);
			threads[threadID] = thread;
			thread.Start();
		  }
		  startingGun.countDown();
		  foreach (Thread thread in threads)
		  {
			thread.Join();
		  }
		}
		r.Dispose();
		dir.Dispose();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestSameScoresWithThreads OuterInstance;

		  private IndexSearcher s;
		  private IDictionary<BytesRef, TopDocs> Answers;
		  private CountDownLatch StartingGun;

		  public ThreadAnonymousInnerClassHelper(TestSameScoresWithThreads outerInstance, IndexSearcher s, IDictionary<BytesRef, TopDocs> answers, CountDownLatch startingGun)
		  {
			  this.OuterInstance = outerInstance;
			  this.s = s;
			  this.Answers = answers;
			  this.StartingGun = startingGun;
		  }

		  public override void Run()
		  {
			try
			{
			  StartingGun.@await();
			  for (int i = 0;i < 20;i++)
			  {
//JAVA TO C# CONVERTER TODO TASK: There is no .NET Dictionary equivalent to the Java 'entrySet' method:
				IList<KeyValuePair<BytesRef, TopDocs>> shuffled = new List<KeyValuePair<BytesRef, TopDocs>>(Answers.entrySet());
                shuffled = CollectionsHelper.Shuffle(shuffled);
				foreach (KeyValuePair<BytesRef, TopDocs> ent in shuffled)
				{
				  TopDocs actual = s.Search(new TermQuery(new Term("body", ent.Key)), 100);
				  TopDocs expected = ent.Value;
				  Assert.AreEqual(expected.TotalHits, actual.TotalHits);
				  Assert.AreEqual(expected.ScoreDocs.Length, actual.ScoreDocs.Length, "query=" + ent.Key.Utf8ToString());
				  for (int hit = 0;hit < expected.ScoreDocs.Length;hit++)
				  {
					Assert.AreEqual(expected.ScoreDocs[hit].Doc, actual.ScoreDocs[hit].Doc);
					// Floats really should be identical:
					Assert.IsTrue(expected.ScoreDocs[hit].Score == actual.ScoreDocs[hit].Score);
				  }
				}
			  }
			}
			catch (Exception e)
			{
			  throw new Exception(e.Message, e);
			}
		  }
	  }
	}

}