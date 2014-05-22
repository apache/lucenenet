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

	public class TestSameScoresWithThreads : LuceneTestCase
	{

	  public virtual void Test()
	  {
		Directory dir = newDirectory();
		MockAnalyzer analyzer = new MockAnalyzer(random());
		analyzer.MaxTokenLength = TestUtil.Next(random(), 1, IndexWriter.MAX_TERM_LENGTH);
		RandomIndexWriter w = new RandomIndexWriter(random(), dir, analyzer);
		LineFileDocs docs = new LineFileDocs(random(), defaultCodecSupportsDocValues());
		int charsToIndex = atLeast(100000);
		int charsIndexed = 0;
		//System.out.println("bytesToIndex=" + charsToIndex);
		while (charsIndexed < charsToIndex)
		{
		  Document doc = docs.nextDoc();
		  charsIndexed += doc.get("body").length();
		  w.addDocument(doc);
		  //System.out.println("  bytes=" + charsIndexed + " add: " + doc);
		}
		IndexReader r = w.Reader;
		//System.out.println("numDocs=" + r.numDocs());
		w.close();

		IndexSearcher s = newSearcher(r);
		Terms terms = MultiFields.getFields(r).terms("body");
		int termCount = 0;
		TermsEnum termsEnum = terms.iterator(null);
		while (termsEnum.next() != null)
		{
		  termCount++;
		}
		Assert.IsTrue(termCount > 0);

		// Target ~10 terms to search:
		double chance = 10.0 / termCount;
		termsEnum = terms.iterator(termsEnum);
		IDictionary<BytesRef, TopDocs> answers = new Dictionary<BytesRef, TopDocs>();
		while (termsEnum.next() != null)
		{
		  if (random().NextDouble() <= chance)
		  {
			BytesRef term = BytesRef.deepCopyOf(termsEnum.term());
			answers[term] = s.search(new TermQuery(new Term("body", term)), 100);
		  }
		}

		if (answers.Count > 0)
		{
		  CountDownLatch startingGun = new CountDownLatch(1);
		  int numThreads = TestUtil.Next(random(), 2, 5);
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
		r.close();
		dir.close();
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
				Collections.shuffle(shuffled);
				foreach (KeyValuePair<BytesRef, TopDocs> ent in shuffled)
				{
				  TopDocs actual = s.search(new TermQuery(new Term("body", ent.Key)), 100);
				  TopDocs expected = ent.Value;
				  Assert.AreEqual(expected.totalHits, actual.totalHits);
				  Assert.AreEqual("query=" + ent.Key.utf8ToString(), expected.scoreDocs.length, actual.scoreDocs.length);
				  for (int hit = 0;hit < expected.scoreDocs.length;hit++)
				  {
					Assert.AreEqual(expected.scoreDocs[hit].doc, actual.scoreDocs[hit].doc);
					// Floats really should be identical:
					Assert.IsTrue(expected.scoreDocs[hit].score == actual.scoreDocs[hit].score);
				  }
				}
			  }
			}
			catch (Exception e)
			{
			  throw new Exception(e);
			}
		  }
	  }
	}

}