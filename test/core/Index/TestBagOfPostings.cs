using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

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
	using Lucene3xCodec = Lucene.Net.Codecs.Lucene3x.Lucene3xCodec;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Simple test that adds numeric terms, where each term has the 
	/// docFreq of its integer value, and checks that the docFreq is correct. 
	/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({"Direct", "Memory"}) public class TestBagOfPostings extends Lucene.Net.Util.LuceneTestCase
	public class TestBagOfPostings : LuceneTestCase // at night this makes like 200k/300k docs and will make Direct's heart beat!
	{
	  public virtual void Test()
	  {
		IList<string> postingsList = new List<string>();
		int numTerms = atLeast(300);
		int maxTermsPerDoc = TestUtil.Next(random(), 10, 20);

		bool isSimpleText = "SimpleText".Equals(TestUtil.getPostingsFormat("field"));

		IndexWriterConfig iwc = newIndexWriterConfig(random(), TEST_VERSION_CURRENT, new MockAnalyzer(random()));

		if ((isSimpleText || iwc.MergePolicy is MockRandomMergePolicy) && (TEST_NIGHTLY || RANDOM_MULTIPLIER > 1))
		{
		  // Otherwise test can take way too long (> 2 hours)
		  numTerms /= 2;
		}

		if (VERBOSE)
		{
		  Console.WriteLine("maxTermsPerDoc=" + maxTermsPerDoc);
		  Console.WriteLine("numTerms=" + numTerms);
		}

		for (int i = 0; i < numTerms; i++)
		{
		  string term = Convert.ToString(i);
		  for (int j = 0; j < i; j++)
		  {
			postingsList.Add(term);
		  }
		}
		Collections.shuffle(postingsList, random());

		ConcurrentLinkedQueue<string> postings = new ConcurrentLinkedQueue<string>(postingsList);

		Directory dir = newFSDirectory(createTempDir("bagofpostings"));
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, iwc);

		int threadCount = TestUtil.Next(random(), 1, 5);
		if (VERBOSE)
		{
		  Console.WriteLine("config: " + iw.w.Config);
		  Console.WriteLine("threadCount=" + threadCount);
		}

		Thread[] threads = new Thread[threadCount];
		CountDownLatch startingGun = new CountDownLatch(1);

		for (int threadID = 0;threadID < threadCount;threadID++)
		{
		  threads[threadID] = new ThreadAnonymousInnerClassHelper(this, maxTermsPerDoc, postings, iw, startingGun);
		  threads[threadID].Start();
		}
		startingGun.countDown();
		foreach (Thread t in threads)
		{
		  t.Join();
		}

		iw.forceMerge(1);
		DirectoryReader ir = iw.Reader;
		Assert.AreEqual(1, ir.leaves().size());
		AtomicReader air = ir.leaves().get(0).reader();
		Terms terms = air.terms("field");
		// numTerms-1 because there cannot be a term 0 with 0 postings:
		Assert.AreEqual(numTerms - 1, air.fields().UniqueTermCount);
		if (iwc.Codec is Lucene3xCodec == false)
		{
		  Assert.AreEqual(numTerms - 1, terms.size());
		}
		TermsEnum termsEnum = terms.iterator(null);
		BytesRef term;
		while ((term = termsEnum.next()) != null)
		{
		  int value = Convert.ToInt32(term.utf8ToString());
		  Assert.AreEqual(value, termsEnum.docFreq());
		  // don't really need to check more than this, as CheckIndex
		  // will verify that docFreq == actual number of documents seen
		  // from a docsAndPositionsEnum.
		}
		ir.close();
		iw.close();
		dir.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestBagOfPostings OuterInstance;

		  private int MaxTermsPerDoc;
		  private ConcurrentLinkedQueue<string> Postings;
		  private RandomIndexWriter Iw;
		  private CountDownLatch StartingGun;

		  public ThreadAnonymousInnerClassHelper(TestBagOfPostings outerInstance, int maxTermsPerDoc, ConcurrentLinkedQueue<string> postings, RandomIndexWriter iw, CountDownLatch startingGun)
		  {
			  this.OuterInstance = outerInstance;
			  this.MaxTermsPerDoc = maxTermsPerDoc;
			  this.Postings = postings;
			  this.Iw = iw;
			  this.StartingGun = startingGun;
		  }

		  public override void Run()
		  {
			try
			{
			  Document document = new Document();
			  Field field = newTextField("field", "", Field.Store.NO);
			  document.add(field);
			  StartingGun.@await();
			  while (!Postings.Empty)
			  {
				StringBuilder text = new StringBuilder();
				Set<string> visited = new HashSet<string>();
				for (int i = 0; i < MaxTermsPerDoc; i++)
				{
				  string token = Postings.poll();
				  if (token == null)
				  {
					break;
				  }
				  if (visited.contains(token))
				  {
					// Put it back:
					Postings.add(token);
					break;
				  }
				  text.Append(' ');
				  text.Append(token);
				  visited.add(token);
				}
				field.StringValue = text.ToString();
				Iw.addDocument(document);
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