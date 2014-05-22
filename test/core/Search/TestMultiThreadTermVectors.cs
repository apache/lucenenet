using System;
using System.Diagnostics;
using System.Text;
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
	using Lucene.Net.Document;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using Fields = Lucene.Net.Index.Fields;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using Directory = Lucene.Net.Store.Directory;
	using English = Lucene.Net.Util.English;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	public class TestMultiThreadTermVectors : LuceneTestCase
	{
	  private Directory Directory;
	  public int NumDocs = 100;
	  public int NumThreads = 3;

	  public override void SetUp()
	  {
		base.setUp();
		Directory = newDirectory();
		IndexWriter writer = new IndexWriter(Directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
		//writer.setNoCFSRatio(0.0);
		//writer.infoStream = System.out;
		FieldType customType = new FieldType(TextField.TYPE_STORED);
		customType.Tokenized = false;
		customType.StoreTermVectors = true;
		for (int i = 0; i < NumDocs; i++)
		{
		  Document doc = new Document();
		  Field fld = newField("field", English.intToEnglish(i), customType);
		  doc.add(fld);
		  writer.addDocument(doc);
		}
		writer.close();

	  }

	  public override void TearDown()
	  {
		Directory.close();
		base.tearDown();
	  }

	  public virtual void Test()
	  {

		IndexReader reader = null;

		try
		{
		  reader = DirectoryReader.open(Directory);
		  for (int i = 1; i <= NumThreads; i++)
		  {
			TestTermPositionVectors(reader, i);
		  }


		}
		catch (IOException ioe)
		{
		  Assert.Fail(ioe.Message);
		}
		finally
		{
		  if (reader != null)
		  {
			try
			{
			  /// <summary>
			  /// close the opened reader </summary>
			  reader.close();
			}
			catch (IOException ioe)
			{
			  Console.WriteLine(ioe.ToString());
			  Console.Write(ioe.StackTrace);
			}
		  }
		}
	  }

	  public virtual void TestTermPositionVectors(IndexReader reader, int threadCount)
	  {
		MultiThreadTermVectorsReader[] mtr = new MultiThreadTermVectorsReader[threadCount];
		for (int i = 0; i < threadCount; i++)
		{
		  mtr[i] = new MultiThreadTermVectorsReader();
		  mtr[i].Init(reader);
		}


		/// <summary>
		/// run until all threads finished </summary>
		int threadsAlive = mtr.Length;
		while (threadsAlive > 0)
		{
			//System.out.println("Threads alive");
			Thread.Sleep(10);
			threadsAlive = mtr.Length;
			for (int i = 0; i < mtr.Length; i++)
			{
			  if (mtr[i].Alive == true)
			  {
				break;
			  }

			  threadsAlive--;
			}
		}

		long totalTime = 0L;
		for (int i = 0; i < mtr.Length; i++)
		{
		  totalTime += mtr[i].TimeElapsed;
		  mtr[i] = null;
		}

		//System.out.println("threadcount: " + mtr.length + " average term vector time: " + totalTime/mtr.length);

	  }

	}

	internal class MultiThreadTermVectorsReader : Runnable
	{

	  private IndexReader Reader = null;
	  private Thread t = null;

	  private readonly int RunsToDo = 100;
	  internal long TimeElapsed = 0;


	  public virtual void Init(IndexReader reader)
	  {
		this.Reader = reader;
		TimeElapsed = 0;
		t = new Thread(this);
		t.Start();
	  }

	  public virtual bool Alive
	  {
		  get
		  {
			if (t == null)
			{
				return false;
			}
    
			return t.IsAlive;
		  }
	  }

	  public override void Run()
	  {
		  try
		  {
			// run the test 100 times
			for (int i = 0; i < RunsToDo; i++)
			{
			  TestTermVectors();
			}
		  }
		  catch (Exception e)
		  {
			Console.WriteLine(e.ToString());
			Console.Write(e.StackTrace);
		  }
		  return;
	  }

	  private void TestTermVectors()
	  {
		// check:
		int numDocs = Reader.numDocs();
		long start = 0L;
		for (int docId = 0; docId < numDocs; docId++)
		{
		  start = System.currentTimeMillis();
		  Fields vectors = Reader.getTermVectors(docId);
		  TimeElapsed += System.currentTimeMillis() - start;

		  // verify vectors result
		  VerifyVectors(vectors, docId);

		  start = System.currentTimeMillis();
		  Terms vector = Reader.getTermVectors(docId).terms("field");
		  TimeElapsed += System.currentTimeMillis() - start;

		  VerifyVector(vector.iterator(null), docId);
		}
	  }

	  private void VerifyVectors(Fields vectors, int num)
	  {
		foreach (string field in vectors)
		{
		  Terms terms = vectors.terms(field);
		  Debug.Assert(terms != null);
		  VerifyVector(terms.iterator(null), num);
		}
	  }

	  private void VerifyVector(TermsEnum vector, int num)
	  {
		StringBuilder temp = new StringBuilder();
		while (vector.next() != null)
		{
		  temp.Append(vector.term().utf8ToString());
		}
		if (!English.intToEnglish(num).Trim().Equals(temp.ToString().Trim()))
		{
			Console.WriteLine("wrong term result");
		}
	  }
	}

}