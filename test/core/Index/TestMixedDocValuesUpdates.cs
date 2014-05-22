using System;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Index
{


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using BinaryDocValuesField = Lucene.Net.Document.BinaryDocValuesField;
	using Document = Lucene.Net.Document.Document;
	using Store = Lucene.Net.Document.Field.Store;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using StringField = Lucene.Net.Document.StringField;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using TestUtil = Lucene.Net.Util.TestUtil;

	using RandomPicks = com.carrotsearch.randomizedtesting.generators.RandomPicks;

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

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({"Appending","Lucene3x","Lucene40","Lucene41","Lucene42","Lucene45"}) public class TestMixedDocValuesUpdates extends Lucene.Net.Util.LuceneTestCase
	public class TestMixedDocValuesUpdates : LuceneTestCase
	{

	  public virtual void TestManyReopensAndFields()
	  {
		Directory dir = newDirectory();
		Random random = random();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random));
		LogMergePolicy lmp = newLogMergePolicy();
		lmp.MergeFactor = 3; // merge often
		conf.MergePolicy = lmp;
		IndexWriter writer = new IndexWriter(dir, conf);

		bool isNRT = random.nextBoolean();
		DirectoryReader reader;
		if (isNRT)
		{
		  reader = DirectoryReader.open(writer, true);
		}
		else
		{
		  writer.commit();
		  reader = DirectoryReader.open(dir);
		}

		int numFields = random.Next(4) + 3; // 3-7
		int numNDVFields = random.Next(numFields / 2) + 1; // 1-3
		long[] fieldValues = new long[numFields];
		bool[] fieldHasValue = new bool[numFields];
		Arrays.fill(fieldHasValue, true);
		for (int i = 0; i < fieldValues.Length; i++)
		{
		  fieldValues[i] = 1;
		}

		int numRounds = atLeast(15);
		int docID = 0;
		for (int i = 0; i < numRounds; i++)
		{
		  int numDocs = atLeast(5);
	//      System.out.println("[" + Thread.currentThread().getName() + "]: round=" + i + ", numDocs=" + numDocs);
		  for (int j = 0; j < numDocs; j++)
		  {
			Document doc = new Document();
			doc.add(new StringField("id", "doc-" + docID, Store.NO));
			doc.add(new StringField("key", "all", Store.NO)); // update key
			// add all fields with their current value
			for (int f = 0; f < fieldValues.Length; f++)
			{
			  if (f < numNDVFields)
			  {
				doc.add(new NumericDocValuesField("f" + f, fieldValues[f]));
			  }
			  else
			  {
				doc.add(new BinaryDocValuesField("f" + f, TestBinaryDocValuesUpdates.ToBytes(fieldValues[f])));
			  }
			}
			writer.addDocument(doc);
			++docID;
		  }

		  // if field's value was unset before, unset it from all new added documents too
		  for (int field = 0; field < fieldHasValue.Length; field++)
		  {
			if (!fieldHasValue[field])
			{
			  if (field < numNDVFields)
			  {
				writer.updateNumericDocValue(new Term("key", "all"), "f" + field, null);
			  }
			  else
			  {
				writer.updateBinaryDocValue(new Term("key", "all"), "f" + field, null);
			  }
			}
		  }

		  int fieldIdx = random.Next(fieldValues.Length);
		  string updateField = "f" + fieldIdx;
		  if (random.nextBoolean())
		  {
	//        System.out.println("[" + Thread.currentThread().getName() + "]: unset field '" + updateField + "'");
			fieldHasValue[fieldIdx] = false;
			if (fieldIdx < numNDVFields)
			{
			  writer.updateNumericDocValue(new Term("key", "all"), updateField, null);
			}
			else
			{
			  writer.updateBinaryDocValue(new Term("key", "all"), updateField, null);
			}
		  }
		  else
		  {
			fieldHasValue[fieldIdx] = true;
			if (fieldIdx < numNDVFields)
			{
			  writer.updateNumericDocValue(new Term("key", "all"), updateField, ++fieldValues[fieldIdx]);
			}
			else
			{
			  writer.updateBinaryDocValue(new Term("key", "all"), updateField, TestBinaryDocValuesUpdates.ToBytes(++fieldValues[fieldIdx]));
			}
	//        System.out.println("[" + Thread.currentThread().getName() + "]: updated field '" + updateField + "' to value " + fieldValues[fieldIdx]);
		  }

		  if (random.NextDouble() < 0.2)
		  {
			int deleteDoc = random.Next(docID); // might also delete an already deleted document, ok!
			writer.deleteDocuments(new Term("id", "doc-" + deleteDoc));
	//        System.out.println("[" + Thread.currentThread().getName() + "]: deleted document: doc-" + deleteDoc);
		  }

		  // verify reader
		  if (!isNRT)
		  {
			writer.commit();
		  }

	//      System.out.println("[" + Thread.currentThread().getName() + "]: reopen reader: " + reader);
		  DirectoryReader newReader = DirectoryReader.openIfChanged(reader);
		  Assert.IsNotNull(newReader);
		  reader.close();
		  reader = newReader;
	//      System.out.println("[" + Thread.currentThread().getName() + "]: reopened reader: " + reader);
		  Assert.IsTrue(reader.numDocs() > 0); // we delete at most one document per round
		  BytesRef scratch = new BytesRef();
		  foreach (AtomicReaderContext context in reader.leaves())
		  {
			AtomicReader r = context.reader();
	//        System.out.println(((SegmentReader) r).getSegmentName());
			Bits liveDocs = r.LiveDocs;
			for (int field = 0; field < fieldValues.Length; field++)
			{
			  string f = "f" + field;
			  BinaryDocValues bdv = r.getBinaryDocValues(f);
			  NumericDocValues ndv = r.getNumericDocValues(f);
			  Bits docsWithField = r.getDocsWithField(f);
			  if (field < numNDVFields)
			  {
				Assert.IsNotNull(ndv);
				assertNull(bdv);
			  }
			  else
			  {
				assertNull(ndv);
				Assert.IsNotNull(bdv);
			  }
			  int maxDoc = r.maxDoc();
			  for (int doc = 0; doc < maxDoc; doc++)
			  {
				if (liveDocs == null || liveDocs.get(doc))
				{
	//              System.out.println("doc=" + (doc + context.docBase) + " f='" + f + "' vslue=" + getValue(bdv, doc, scratch));
				  if (fieldHasValue[field])
				  {
					Assert.IsTrue(docsWithField.get(doc));
					if (field < numNDVFields)
					{
					  Assert.AreEqual("invalid value for doc=" + doc + ", field=" + f + ", reader=" + r, fieldValues[field], ndv.get(doc));
					}
					else
					{
					  Assert.AreEqual("invalid value for doc=" + doc + ", field=" + f + ", reader=" + r, fieldValues[field], TestBinaryDocValuesUpdates.GetValue(bdv, doc, scratch));
					}
				  }
				  else
				  {
					Assert.IsFalse(docsWithField.get(doc));
				  }
				}
			  }
			}
		  }
	//      System.out.println();
		}

		IOUtils.close(writer, reader, dir);
	  }

	  public virtual void TestStressMultiThreading()
	  {
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		IndexWriter writer = new IndexWriter(dir, conf);

		// create index
		int numThreads = TestUtil.Next(random(), 3, 6);
		int numDocs = atLeast(2000);
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  doc.add(new StringField("id", "doc" + i, Store.NO));
		  double group = random().NextDouble();
		  string g;
		  if (group < 0.1)
		  {
			  g = "g0";
		  }
		  else if (group < 0.5)
		  {
			  g = "g1";
		  }
		  else if (group < 0.8)
		  {
			  g = "g2";
		  }
		  else
		  {
			  g = "g3";
		  }
		  doc.add(new StringField("updKey", g, Store.NO));
		  for (int j = 0; j < numThreads; j++)
		  {
			long value = random().Next();
			doc.add(new BinaryDocValuesField("f" + j, TestBinaryDocValuesUpdates.ToBytes(value)));
			doc.add(new NumericDocValuesField("cf" + j, value * 2)); // control, always updated to f * 2
		  }
		  writer.addDocument(doc);
		}

		CountDownLatch done = new CountDownLatch(numThreads);
		AtomicInteger numUpdates = new AtomicInteger(atLeast(100));

		// same thread updates a field as well as reopens
		Thread[] threads = new Thread[numThreads];
		for (int i = 0; i < threads.Length; i++)
		{
		  string f = "f" + i;
		  string cf = "cf" + i;
		  threads[i] = new ThreadAnonymousInnerClassHelper(this, "UpdateThread-" + i, writer, numDocs, done, numUpdates, f, cf);
		}

		foreach (Thread t in threads)
		{
			t.Start();
		}
		done.@await();
		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		BytesRef scratch = new BytesRef();
		foreach (AtomicReaderContext context in reader.leaves())
		{
		  AtomicReader r = context.reader();
		  for (int i = 0; i < numThreads; i++)
		  {
			BinaryDocValues bdv = r.getBinaryDocValues("f" + i);
			NumericDocValues control = r.getNumericDocValues("cf" + i);
			Bits docsWithBdv = r.getDocsWithField("f" + i);
			Bits docsWithControl = r.getDocsWithField("cf" + i);
			Bits liveDocs = r.LiveDocs;
			for (int j = 0; j < r.maxDoc(); j++)
			{
			  if (liveDocs == null || liveDocs.get(j))
			  {
				Assert.AreEqual(docsWithBdv.get(j), docsWithControl.get(j));
				if (docsWithBdv.get(j))
				{
				  long ctrlValue = control.get(j);
				  long bdvValue = TestBinaryDocValuesUpdates.GetValue(bdv, j, scratch) * 2;
	//              if (ctrlValue != bdvValue) {
	//                System.out.println("seg=" + r + ", f=f" + i + ", doc=" + j + ", group=" + r.document(j).get("updKey") + ", ctrlValue=" + ctrlValue + ", bdvBytes=" + scratch);
	//              }
				  Assert.AreEqual(ctrlValue, bdvValue);
				}
			  }
			}
		  }
		}
		reader.close();

		dir.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestMixedDocValuesUpdates OuterInstance;

		  private IndexWriter Writer;
		  private int NumDocs;
		  private CountDownLatch Done;
		  private AtomicInteger NumUpdates;
		  private string f;
		  private string Cf;

		  public ThreadAnonymousInnerClassHelper(TestMixedDocValuesUpdates outerInstance, string "UpdateThread-" + i, IndexWriter writer, int numDocs, CountDownLatch done, AtomicInteger numUpdates, string f, string cf) : base("UpdateThread-" + i)
		  {
			  this.OuterInstance = outerInstance;
			  this.Writer = writer;
			  this.NumDocs = numDocs;
			  this.Done = done;
			  this.NumUpdates = numUpdates;
			  this.f = f;
			  this.Cf = cf;
		  }

		  public override void Run()
		  {
			DirectoryReader reader = null;
			bool success = false;
			try
			{
			  Random random = random();
			  while (NumUpdates.AndDecrement > 0)
			  {
				double group = random.NextDouble();
				Term t;
				if (group < 0.1)
				{
					t = new Term("updKey", "g0");
				}
				else if (group < 0.5)
				{
					t = new Term("updKey", "g1");
				}
				else if (group < 0.8)
				{
					t = new Term("updKey", "g2");
				}
				else
				{
					t = new Term("updKey", "g3");
				}
	  //              System.out.println("[" + Thread.currentThread().getName() + "] numUpdates=" + numUpdates + " updateTerm=" + t);
				if (random.nextBoolean()) // sometimes unset a value
				{
	  //                System.err.println("[" + Thread.currentThread().getName() + "] t=" + t + ", f=" + f + ", updValue=UNSET");
				  Writer.updateBinaryDocValue(t, f, null);
				  Writer.updateNumericDocValue(t, Cf, null);
				}
				else
				{
				  long updValue = random.Next();
	  //                System.err.println("[" + Thread.currentThread().getName() + "] t=" + t + ", f=" + f + ", updValue=" + updValue);
				  Writer.updateBinaryDocValue(t, f, TestBinaryDocValuesUpdates.ToBytes(updValue));
				  Writer.updateNumericDocValue(t, Cf, updValue * 2);
				}

				if (random.NextDouble() < 0.2)
				{
				  // delete a random document
				  int doc = random.Next(NumDocs);
	  //                System.out.println("[" + Thread.currentThread().getName() + "] deleteDoc=doc" + doc);
				  Writer.deleteDocuments(new Term("id", "doc" + doc));
				}

				if (random.NextDouble() < 0.05) // commit every 20 updates on average
				{
	  //                  System.out.println("[" + Thread.currentThread().getName() + "] commit");
				  Writer.commit();
				}

				if (random.NextDouble() < 0.1) // reopen NRT reader (apply updates), on average once every 10 updates
				{
				  if (reader == null)
				  {
	  //                  System.out.println("[" + Thread.currentThread().getName() + "] open NRT");
					reader = DirectoryReader.open(Writer, true);
				  }
				  else
				  {
	  //                  System.out.println("[" + Thread.currentThread().getName() + "] reopen NRT");
					DirectoryReader r2 = DirectoryReader.openIfChanged(reader, Writer, true);
					if (r2 != null)
					{
					  reader.close();
					  reader = r2;
					}
				  }
				}
			  }
	  //            System.out.println("[" + Thread.currentThread().getName() + "] DONE");
			  success = true;
			}
			catch (IOException e)
			{
			  throw new Exception(e);
			}
			finally
			{
			  if (reader != null)
			  {
				try
				{
				  reader.close();
				}
				catch (IOException e)
				{
				  if (success) // suppress this exception only if there was another exception
				  {
					throw new Exception(e);
				  }
				}
			  }
			  Done.countDown();
			}
		  }
	  }

	  public virtual void TestUpdateDifferentDocsInDifferentGens()
	  {
		// update same document multiple times across generations
		Directory dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		conf.MaxBufferedDocs = 4;
		IndexWriter writer = new IndexWriter(dir, conf);
		int numDocs = atLeast(10);
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  doc.add(new StringField("id", "doc" + i, Store.NO));
		  long value = random().Next();
		  doc.add(new BinaryDocValuesField("f", TestBinaryDocValuesUpdates.ToBytes(value)));
		  doc.add(new NumericDocValuesField("cf", value * 2));
		  writer.addDocument(doc);
		}

		int numGens = atLeast(5);
		BytesRef scratch = new BytesRef();
		for (int i = 0; i < numGens; i++)
		{
		  int doc = random().Next(numDocs);
		  Term t = new Term("id", "doc" + doc);
		  long value = random().nextLong();
		  writer.updateBinaryDocValue(t, "f", TestBinaryDocValuesUpdates.ToBytes(value));
		  writer.updateNumericDocValue(t, "cf", value * 2);
		  DirectoryReader reader = DirectoryReader.open(writer, true);
		  foreach (AtomicReaderContext context in reader.leaves())
		  {
			AtomicReader r = context.reader();
			BinaryDocValues fbdv = r.getBinaryDocValues("f");
			NumericDocValues cfndv = r.getNumericDocValues("cf");
			for (int j = 0; j < r.maxDoc(); j++)
			{
			  Assert.AreEqual(cfndv.get(j), TestBinaryDocValuesUpdates.GetValue(fbdv, j, scratch) * 2);
			}
		  }
		  reader.close();
		}
		writer.close();
		dir.close();
	  }

	  public virtual void TestTonsOfUpdates()
	  {
		// LUCENE-5248: make sure that when there are many updates, we don't use too much RAM
		Directory dir = newDirectory();
		Random random = random();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random));
		conf.RAMBufferSizeMB = IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB;
		conf.MaxBufferedDocs = IndexWriterConfig.DISABLE_AUTO_FLUSH; // don't flush by doc
		IndexWriter writer = new IndexWriter(dir, conf);

		// test data: lots of documents (few 10Ks) and lots of update terms (few hundreds)
		int numDocs = atLeast(20000);
		int numBinaryFields = atLeast(5);
		int numTerms = TestUtil.Next(random, 10, 100); // terms should affect many docs
		Set<string> updateTerms = new HashSet<string>();
		while (updateTerms.size() < numTerms)
		{
		  updateTerms.add(TestUtil.randomSimpleString(random));
		}

	//    System.out.println("numDocs=" + numDocs + " numBinaryFields=" + numBinaryFields + " numTerms=" + numTerms);

		// build a large index with many BDV fields and update terms
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  int numUpdateTerms = TestUtil.Next(random, 1, numTerms / 10);
		  for (int j = 0; j < numUpdateTerms; j++)
		  {
			doc.add(new StringField("upd", RandomPicks.randomFrom(random, updateTerms), Store.NO));
		  }
		  for (int j = 0; j < numBinaryFields; j++)
		  {
			long val = random.Next();
			doc.add(new BinaryDocValuesField("f" + j, TestBinaryDocValuesUpdates.ToBytes(val)));
			doc.add(new NumericDocValuesField("cf" + j, val * 2));
		  }
		  writer.addDocument(doc);
		}

		writer.commit(); // commit so there's something to apply to

		// set to flush every 2048 bytes (approximately every 12 updates), so we get
		// many flushes during binary updates
		writer.Config.RAMBufferSizeMB = 2048.0 / 1024 / 1024;
		int numUpdates = atLeast(100);
	//    System.out.println("numUpdates=" + numUpdates);
		for (int i = 0; i < numUpdates; i++)
		{
		  int field = random.Next(numBinaryFields);
		  Term updateTerm = new Term("upd", RandomPicks.randomFrom(random, updateTerms));
		  long value = random.Next();
		  writer.updateBinaryDocValue(updateTerm, "f" + field, TestBinaryDocValuesUpdates.ToBytes(value));
		  writer.updateNumericDocValue(updateTerm, "cf" + field, value * 2);
		}

		writer.close();

		DirectoryReader reader = DirectoryReader.open(dir);
		BytesRef scratch = new BytesRef();
		foreach (AtomicReaderContext context in reader.leaves())
		{
		  for (int i = 0; i < numBinaryFields; i++)
		  {
			AtomicReader r = context.reader();
			BinaryDocValues f = r.getBinaryDocValues("f" + i);
			NumericDocValues cf = r.getNumericDocValues("cf" + i);
			for (int j = 0; j < r.maxDoc(); j++)
			{
			  Assert.AreEqual("reader=" + r + ", field=f" + i + ", doc=" + j, cf.get(j), TestBinaryDocValuesUpdates.GetValue(f, j, scratch) * 2);
			}
		  }
		}
		reader.close();

		dir.close();
	  }

	}

}