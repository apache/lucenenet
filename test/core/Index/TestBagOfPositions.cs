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
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;
	using Directory = Lucene.Net.Store.Directory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Simple test that adds numeric terms, where each term has the 
	/// totalTermFreq of its integer value, and checks that the totalTermFreq is correct. 
	/// </summary>
	// TODO: somehow factor this with BagOfPostings? its almost the same
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({"Direct", "Memory", "Lucene3x"}) public class TestBagOfPositions extends Lucene.Net.Util.LuceneTestCase
	public class TestBagOfPositions : LuceneTestCase // at night this makes like 200k/300k docs and will make Direct's heart beat!
													  // Lucene3x doesnt have totalTermFreq, so the test isn't interesting there.
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

		Directory dir = newFSDirectory(createTempDir("bagofpositions"));

		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, iwc);

		int threadCount = TestUtil.Next(random(), 1, 5);
		if (VERBOSE)
		{
		  Console.WriteLine("config: " + iw.w.Config);
		  Console.WriteLine("threadCount=" + threadCount);
		}

		Field prototype = newTextField("field", "", Field.Store.NO);
		FieldType fieldType = new FieldType(prototype.fieldType());
		if (random().nextBoolean())
		{
		  fieldType.OmitNorms = true;
		}
		int options = random().Next(3);
		if (options == 0)
		{
		  fieldType.IndexOptions = IndexOptions.DOCS_AND_FREQS; // we dont actually need positions
		  fieldType.StoreTermVectors = true; // but enforce term vectors when we do this so we check SOMETHING
		}
		else if (options == 1 && !doesntSupportOffsets.contains(TestUtil.getPostingsFormat("field")))
		{
		  fieldType.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
		}
		// else just positions

		Thread[] threads = new Thread[threadCount];
		CountDownLatch startingGun = new CountDownLatch(1);

		for (int threadID = 0;threadID < threadCount;threadID++)
		{
		  Random threadRandom = new Random(random().nextLong());
		  Document document = new Document();
		  Field field = new Field("field", "", fieldType);
		  document.add(field);
		  threads[threadID] = new ThreadAnonymousInnerClassHelper(this, numTerms, maxTermsPerDoc, postings, iw, startingGun, threadRandom, document, field);
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
		Assert.AreEqual(numTerms - 1, terms.size());
		TermsEnum termsEnum = terms.iterator(null);
		BytesRef term;
		while ((term = termsEnum.next()) != null)
		{
		  int value = Convert.ToInt32(term.utf8ToString());
		  Assert.AreEqual(value, termsEnum.totalTermFreq());
		  // don't really need to check more than this, as CheckIndex
		  // will verify that totalTermFreq == total number of positions seen
		  // from a docsAndPositionsEnum.
		}
		ir.close();
		iw.close();
		dir.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestBagOfPositions OuterInstance;

		  private int NumTerms;
		  private int MaxTermsPerDoc;
		  private ConcurrentLinkedQueue<string> Postings;
		  private RandomIndexWriter Iw;
		  private CountDownLatch StartingGun;
		  private Random ThreadRandom;
		  private Document Document;
		  private Field Field;

		  public ThreadAnonymousInnerClassHelper(TestBagOfPositions outerInstance, int numTerms, int maxTermsPerDoc, ConcurrentLinkedQueue<string> postings, RandomIndexWriter iw, CountDownLatch startingGun, Random threadRandom, Document document, Field field)
		  {
			  this.OuterInstance = outerInstance;
			  this.NumTerms = numTerms;
			  this.MaxTermsPerDoc = maxTermsPerDoc;
			  this.Postings = postings;
			  this.Iw = iw;
			  this.StartingGun = startingGun;
			  this.ThreadRandom = threadRandom;
			  this.Document = document;
			  this.Field = field;
		  }

		  public override void Run()
		  {
			try
			{
			  StartingGun.@await();
			  while (!Postings.Empty)
			  {
				StringBuilder text = new StringBuilder();
				int numTerms = ThreadRandom.Next(MaxTermsPerDoc);
				for (int i = 0; i < numTerms; i++)
				{
				  string token = Postings.poll();
				  if (token == null)
				  {
					break;
				  }
				  text.Append(' ');
				  text.Append(token);
				}
				Field.StringValue = text.ToString();
				Iw.addDocument(Document);
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