using System;
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
	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using Directory = Lucene.Net.Store.Directory;
	using Document = Lucene.Net.Document.Document;
	using FieldType = Lucene.Net.Document.FieldType;
	using StringField = Lucene.Net.Document.StringField;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using English = Lucene.Net.Util.English;

	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using BeforeClass = org.junit.BeforeClass;

	public class TestThreadedForceMerge : LuceneTestCase
	{

	  private static Analyzer ANALYZER;

	  private const int NUM_THREADS = 3;
	  //private final static int NUM_THREADS = 5;

	  private const int NUM_ITER = 1;

	  private const int NUM_ITER2 = 1;

	  private volatile bool Failed;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void setup()
	  public static void Setup()
	  {
		ANALYZER = new MockAnalyzer(random(), MockTokenizer.SIMPLE, true);
	  }

	  private void SetFailed()
	  {
		Failed = true;
	  }

	  public virtual void RunTest(Random random, Directory directory)
	  {

		IndexWriter writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, ANALYZER).setOpenMode(OpenMode.CREATE).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy()));

		for (int iter = 0;iter < NUM_ITER;iter++)
		{
		  int iterFinal = iter;

		  ((LogMergePolicy) writer.Config.MergePolicy).MergeFactor = 1000;

		  FieldType customType = new FieldType(StringField.TYPE_STORED);
		  customType.OmitNorms = true;

		  for (int i = 0;i < 200;i++)
		  {
			Document d = new Document();
			d.add(newField("id", Convert.ToString(i), customType));
			d.add(newField("contents", English.intToEnglish(i), customType));
			writer.addDocument(d);
		  }

		  ((LogMergePolicy) writer.Config.MergePolicy).MergeFactor = 4;

		  Thread[] threads = new Thread[NUM_THREADS];

		  for (int i = 0;i < NUM_THREADS;i++)
		  {
			int iFinal = i;
			IndexWriter writerFinal = writer;
			threads[i] = new ThreadAnonymousInnerClassHelper(this, iterFinal, customType, iFinal, writerFinal);
		  }

		  for (int i = 0;i < NUM_THREADS;i++)
		  {
			threads[i].Start();
		  }

		  for (int i = 0;i < NUM_THREADS;i++)
		  {
			threads[i].Join();
		  }

		  Assert.IsTrue(!Failed);

		  int expectedDocCount = (int)((1 + iter) * (200 + 8 * NUM_ITER2 * (NUM_THREADS / 2.0) * (1 + NUM_THREADS)));

		  Assert.AreEqual("index=" + writer.segString() + " numDocs=" + writer.numDocs() + " maxDoc=" + writer.maxDoc() + " config=" + writer.Config, expectedDocCount, writer.numDocs());
		  Assert.AreEqual("index=" + writer.segString() + " numDocs=" + writer.numDocs() + " maxDoc=" + writer.maxDoc() + " config=" + writer.Config, expectedDocCount, writer.maxDoc());

		  writer.close();
		  writer = new IndexWriter(directory, newIndexWriterConfig(TEST_VERSION_CURRENT, ANALYZER).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(2));

		  DirectoryReader reader = DirectoryReader.open(directory);
		  Assert.AreEqual("reader=" + reader, 1, reader.leaves().size());
		  Assert.AreEqual(expectedDocCount, reader.numDocs());
		  reader.close();
		}
		writer.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestThreadedForceMerge OuterInstance;

		  private int IterFinal;
		  private FieldType CustomType;
		  private int IFinal;
		  private IndexWriter WriterFinal;

		  public ThreadAnonymousInnerClassHelper(TestThreadedForceMerge outerInstance, int iterFinal, FieldType customType, int iFinal, IndexWriter writerFinal)
		  {
			  this.OuterInstance = outerInstance;
			  this.IterFinal = iterFinal;
			  this.CustomType = customType;
			  this.IFinal = iFinal;
			  this.WriterFinal = writerFinal;
		  }

		  public override void Run()
		  {
			try
			{
			  for (int j = 0;j < NUM_ITER2;j++)
			  {
				WriterFinal.forceMerge(1, false);
				for (int k = 0;k < 17 * (1 + IFinal);k++)
				{
				  Document d = new Document();
				  d.add(newField("id", IterFinal + "_" + IFinal + "_" + j + "_" + k, CustomType));
				  d.add(newField("contents", English.intToEnglish(IFinal + k), CustomType));
				  WriterFinal.addDocument(d);
				}
				for (int k = 0;k < 9 * (1 + IFinal);k++)
				{
				  WriterFinal.deleteDocuments(new Term("id", IterFinal + "_" + IFinal + "_" + j + "_" + k));
				}
				WriterFinal.forceMerge(1);
			  }
			}
			catch (Exception t)
			{
			  outerInstance.SetFailed();
			  Console.WriteLine(Thread.CurrentThread.Name + ": hit exception");
			  t.printStackTrace(System.out);
			}
		  }
	  }

	  /*
	    Run above stress test against RAMDirectory and then
	    FSDirectory.
	  */
	  public virtual void TestThreadedForceMerge()
	  {
		Directory directory = newDirectory();
		RunTest(random(), directory);
		directory.close();
	  }
	}

}