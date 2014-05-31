using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace Lucene.Net.Index
{

	/// <summary>
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// 
	///     http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>


	using Assert = junit.framework.Assert;

	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using OffsetAttribute = Lucene.Net.Analysis.Tokenattributes.OffsetAttribute;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using Directory = Lucene.Net.Store.Directory;
	using Lucene.Net.Util;

	public class TestStressIndexing2 : LuceneTestCase
	{
	  internal static int MaxFields = 4;
	  internal static int BigFieldSize = 10;
	  internal static bool SameFieldOrder = false;
	  internal static int MergeFactor = 3;
	  internal static int MaxBufferedDocs = 3;
	  internal static int Seed = 0;

	  public sealed class YieldTestPoint : RandomIndexWriter.TestPoint
	  {
		  private readonly TestStressIndexing2 OuterInstance;

		  public YieldTestPoint(TestStressIndexing2 outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }


		public override void Apply(string name)
		{
		  //      if (name.equals("startCommit")) {
		  if (random().Next(4) == 2)
		  {
			Thread.@yield();
		  }
		}
	  }
	//  
	  public virtual void TestRandomIWReader()
	  {
		Directory dir = newDirectory();

		// TODO: verify equals using IW.getReader
		DocsAndWriter dw = IndexRandomIWReader(5, 3, 100, dir);
		DirectoryReader reader = dw.Writer.Reader;
		dw.Writer.commit();
		VerifyEquals(random(), reader, dir, "id");
		reader.close();
		dw.Writer.close();
		dir.close();
	  }

	  public virtual void TestRandom()
	  {
		Directory dir1 = newDirectory();
		Directory dir2 = newDirectory();
		// mergeFactor=2; maxBufferedDocs=2; Map docs = indexRandom(1, 3, 2, dir1);
		int maxThreadStates = 1 + random().Next(10);
		bool doReaderPooling = random().nextBoolean();
		IDictionary<string, Document> docs = IndexRandom(5, 3, 100, dir1, maxThreadStates, doReaderPooling);
		IndexSerial(random(), docs, dir2);

		// verifying verify
		// verifyEquals(dir1, dir1, "id");
		// verifyEquals(dir2, dir2, "id");

		VerifyEquals(dir1, dir2, "id");
		dir1.close();
		dir2.close();
	  }

	  public virtual void TestMultiConfig()
	  {
		// test lots of smaller different params together

		int num = atLeast(3);
		for (int i = 0; i < num; i++) // increase iterations for better testing
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("\n\nTEST: top iter=" + i);
		  }
		  SameFieldOrder = random().nextBoolean();
		  MergeFactor = random().Next(3) + 2;
		  MaxBufferedDocs = random().Next(3) + 2;
		  int maxThreadStates = 1 + random().Next(10);
		  bool doReaderPooling = random().nextBoolean();
		  Seed++;

		  int nThreads = random().Next(5) + 1;
		  int iter = random().Next(5) + 1;
		  int range = random().Next(20) + 1;
		  Directory dir1 = newDirectory();
		  Directory dir2 = newDirectory();
		  if (VERBOSE)
		  {
			Console.WriteLine("  nThreads=" + nThreads + " iter=" + iter + " range=" + range + " doPooling=" + doReaderPooling + " maxThreadStates=" + maxThreadStates + " sameFieldOrder=" + SameFieldOrder + " mergeFactor=" + MergeFactor + " maxBufferedDocs=" + MaxBufferedDocs);
		  }
		  IDictionary<string, Document> docs = IndexRandom(nThreads, iter, range, dir1, maxThreadStates, doReaderPooling);
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: index serial");
		  }
		  IndexSerial(random(), docs, dir2);
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: verify");
		  }
		  VerifyEquals(dir1, dir2, "id");
		  dir1.close();
		  dir2.close();
		}
	  }


	  internal static Term IdTerm = new Term("id","");
	  internal IndexingThread[] Threads;
	  internal static IComparer<IndexableField> fieldNameComparator = new ComparatorAnonymousInnerClassHelper();

	  private class ComparatorAnonymousInnerClassHelper : IComparer<IndexableField>
	  {
		  public ComparatorAnonymousInnerClassHelper()
		  {
		  }

		  public virtual int Compare(IndexableField o1, IndexableField o2)
		  {
			return o1.name().compareTo(o2.name());
		  }
	  }

	  // this test avoids using any extra synchronization in the multiple
	  // indexing threads to test that IndexWriter does correctly synchronize
	  // everything.

	  public class DocsAndWriter
	  {
		internal IDictionary<string, Document> Docs;
		internal IndexWriter Writer;
	  }

	  public virtual DocsAndWriter IndexRandomIWReader(int nThreads, int iterations, int range, Directory dir)
	  {
		IDictionary<string, Document> docs = new Dictionary<string, Document>();
		IndexWriter w = RandomIndexWriter.mockIndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setRAMBufferSizeMB(0.1).setMaxBufferedDocs(MaxBufferedDocs).setMergePolicy(newLogMergePolicy()), new YieldTestPoint(this));
		w.commit();
		LogMergePolicy lmp = (LogMergePolicy) w.Config.MergePolicy;
		lmp.NoCFSRatio = 0.0;
		lmp.MergeFactor = MergeFactor;
		/// <summary>
		///*
		///    w.setMaxMergeDocs(Integer.MAX_VALUE);
		///    w.setMaxFieldLength(10000);
		///    w.setRAMBufferSizeMB(1);
		///    w.setMergeFactor(10);
		/// **
		/// </summary>

		Threads = new IndexingThread[nThreads];
		for (int i = 0; i < Threads.Length; i++)
		{
		  IndexingThread th = new IndexingThread(this);
		  th.w = w;
		  th.@base = 1000000 * i;
		  th.Range = range;
		  th.Iterations = iterations;
		  Threads[i] = th;
		}

		for (int i = 0; i < Threads.Length; i++)
		{
		  Threads[i].Start();
		}
		for (int i = 0; i < Threads.Length; i++)
		{
		  Threads[i].Join();
		}

		// w.forceMerge(1);
		//w.close();    

		for (int i = 0; i < Threads.Length; i++)
		{
		  IndexingThread th = Threads[i];
		  lock (th)
		  {
//JAVA TO C# CONVERTER TODO TASK: There is no .NET Dictionary equivalent to the Java 'putAll' method:
			docs.putAll(th.Docs);
		  }
		}

		TestUtil.checkIndex(dir);
		DocsAndWriter dw = new DocsAndWriter();
		dw.Docs = docs;
		dw.Writer = w;
		return dw;
	  }

	  public virtual IDictionary<string, Document> IndexRandom(int nThreads, int iterations, int range, Directory dir, int maxThreadStates, bool doReaderPooling)
	  {
		IDictionary<string, Document> docs = new Dictionary<string, Document>();
		IndexWriter w = RandomIndexWriter.mockIndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE).setRAMBufferSizeMB(0.1).setMaxBufferedDocs(MaxBufferedDocs).setIndexerThreadPool(new ThreadAffinityDocumentsWriterThreadPool(maxThreadStates)).setReaderPooling(doReaderPooling).setMergePolicy(newLogMergePolicy()), new YieldTestPoint(this));
		LogMergePolicy lmp = (LogMergePolicy) w.Config.MergePolicy;
		lmp.NoCFSRatio = 0.0;
		lmp.MergeFactor = MergeFactor;

		Threads = new IndexingThread[nThreads];
		for (int i = 0; i < Threads.Length; i++)
		{
		  IndexingThread th = new IndexingThread(this);
		  th.w = w;
		  th.@base = 1000000 * i;
		  th.Range = range;
		  th.Iterations = iterations;
		  Threads[i] = th;
		}

		for (int i = 0; i < Threads.Length; i++)
		{
		  Threads[i].Start();
		}
		for (int i = 0; i < Threads.Length; i++)
		{
		  Threads[i].Join();
		}

		//w.forceMerge(1);
		w.close();

		for (int i = 0; i < Threads.Length; i++)
		{
		  IndexingThread th = Threads[i];
		  lock (th)
		  {
//JAVA TO C# CONVERTER TODO TASK: There is no .NET Dictionary equivalent to the Java 'putAll' method:
			docs.putAll(th.Docs);
		  }
		}

		//System.out.println("TEST: checkindex");
		TestUtil.checkIndex(dir);

		return docs;
	  }


	  public static void IndexSerial(Random random, IDictionary<string, Document> docs, Directory dir)
	  {
		IndexWriter w = new IndexWriter(dir, LuceneTestCase.newIndexWriterConfig(random, TEST_VERSION_CURRENT, new MockAnalyzer(random)).setMergePolicy(newLogMergePolicy()));

		// index all docs in a single thread
		IEnumerator<Document> iter = docs.Values.GetEnumerator();
		while (iter.MoveNext())
		{
		  Document d = iter.Current;
		  List<IndexableField> fields = new List<IndexableField>();
		  fields.AddRange(d.Fields);
		  // put fields in same order each time
		  fields.Sort(fieldNameComparator);

		  Document d1 = new Document();
		  for (int i = 0; i < fields.Count; i++)
		  {
			d1.add(fields[i]);
		  }
		  w.addDocument(d1);
		  // System.out.println("indexing "+d1);
		}

		w.close();
	  }

	  public virtual void VerifyEquals(Random r, DirectoryReader r1, Directory dir2, string idField)
	  {
		DirectoryReader r2 = DirectoryReader.open(dir2);
		VerifyEquals(r1, r2, idField);
		r2.close();
	  }

	  public virtual void VerifyEquals(Directory dir1, Directory dir2, string idField)
	  {
		DirectoryReader r1 = DirectoryReader.open(dir1);
		DirectoryReader r2 = DirectoryReader.open(dir2);
		VerifyEquals(r1, r2, idField);
		r1.close();
		r2.close();
	  }

	  private static void PrintDocs(DirectoryReader r)
	  {
		foreach (AtomicReaderContext ctx in r.leaves())
		{
		  // TODO: improve this
		  AtomicReader sub = ctx.reader();
		  Bits liveDocs = sub.LiveDocs;
		  Console.WriteLine("  " + ((SegmentReader) sub).SegmentInfo);
		  for (int docID = 0;docID < sub.maxDoc();docID++)
		  {
			Document doc = sub.document(docID);
			if (liveDocs == null || liveDocs.get(docID))
			{
			  Console.WriteLine("    docID=" + docID + " id:" + doc.get("id"));
			}
			else
			{
			  Console.WriteLine("    DEL docID=" + docID + " id:" + doc.get("id"));
			}
		  }
		}
	  }


	  public virtual void VerifyEquals(DirectoryReader r1, DirectoryReader r2, string idField)
	  {
		if (VERBOSE)
		{
		  Console.WriteLine("\nr1 docs:");
		  PrintDocs(r1);
		  Console.WriteLine("\nr2 docs:");
		  PrintDocs(r2);
		}
		if (r1.numDocs() != r2.numDocs())
		{
		  Debug.Assert(false, "r1.numDocs()=" + r1.numDocs() + " vs r2.numDocs()=" + r2.numDocs());
		}
		bool hasDeletes = !(r1.maxDoc() == r2.maxDoc() && r1.numDocs() == r1.maxDoc());

		int[] r2r1 = new int[r2.maxDoc()]; // r2 id to r1 id mapping

		// create mapping from id2 space to id2 based on idField
		Fields f1 = MultiFields.getFields(r1);
		if (f1 == null)
		{
		  // make sure r2 is empty
		  assertNull(MultiFields.getFields(r2));
		  return;
		}
		Terms terms1 = f1.terms(idField);
		if (terms1 == null)
		{
		  Assert.IsTrue(MultiFields.getFields(r2) == null || MultiFields.getFields(r2).terms(idField) == null);
		  return;
		}
		TermsEnum termsEnum = terms1.iterator(null);

		Bits liveDocs1 = MultiFields.getLiveDocs(r1);
		Bits liveDocs2 = MultiFields.getLiveDocs(r2);

		Fields fields = MultiFields.getFields(r2);
		if (fields == null)
		{
		  // make sure r1 is in fact empty (eg has only all
		  // deleted docs):
		  Bits liveDocs = MultiFields.getLiveDocs(r1);
		  DocsEnum docs = null;
		  while (termsEnum.next() != null)
		  {
			docs = TestUtil.docs(random(), termsEnum, liveDocs, docs, DocsEnum.FLAG_NONE);
			while (docs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			{
			  Assert.Fail("r1 is not empty but r2 is");
			}
		  }
		  return;
		}
		Terms terms2 = fields.terms(idField);
		TermsEnum termsEnum2 = terms2.iterator(null);

		DocsEnum termDocs1 = null;
		DocsEnum termDocs2 = null;

		while (true)
		{
		  BytesRef term = termsEnum.next();
		  //System.out.println("TEST: match id term=" + term);
		  if (term == null)
		  {
			break;
		  }

		  termDocs1 = TestUtil.docs(random(), termsEnum, liveDocs1, termDocs1, DocsEnum.FLAG_NONE);
		  if (termsEnum2.seekExact(term))
		  {
			termDocs2 = TestUtil.docs(random(), termsEnum2, liveDocs2, termDocs2, DocsEnum.FLAG_NONE);
		  }
		  else
		  {
			termDocs2 = null;
		  }

		  if (termDocs1.nextDoc() == DocIdSetIterator.NO_MORE_DOCS)
		  {
			// this doc is deleted and wasn't replaced
			Assert.IsTrue(termDocs2 == null || termDocs2.nextDoc() == DocIdSetIterator.NO_MORE_DOCS);
			continue;
		  }

		  int id1 = termDocs1.docID();
		  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, termDocs1.nextDoc());

		  Assert.IsTrue(termDocs2.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		  int id2 = termDocs2.docID();
		  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, termDocs2.nextDoc());

		  r2r1[id2] = id1;

		  // verify stored fields are equivalent
		  try
		  {
			VerifyEquals(r1.document(id1), r2.document(id2));
		  }
		  catch (Exception t)
		  {
			Console.WriteLine("FAILED id=" + term + " id1=" + id1 + " id2=" + id2 + " term=" + term);
			Console.WriteLine("  d1=" + r1.document(id1));
			Console.WriteLine("  d2=" + r2.document(id2));
			throw t;
		  }

		  try
		  {
			// verify term vectors are equivalent        
			VerifyEquals(r1.getTermVectors(id1), r2.getTermVectors(id2));
		  }
		  catch (Exception e)
		  {
			Console.WriteLine("FAILED id=" + term + " id1=" + id1 + " id2=" + id2);
			Fields tv1 = r1.getTermVectors(id1);
			Console.WriteLine("  d1=" + tv1);
			if (tv1 != null)
			{
			  DocsAndPositionsEnum dpEnum = null;
			  DocsEnum dEnum = null;
			  foreach (string field in tv1)
			  {
				Console.WriteLine("    " + field + ":");
				Terms terms3 = tv1.terms(field);
				Assert.IsNotNull(terms3);
				TermsEnum termsEnum3 = terms3.iterator(null);
				BytesRef term2;
				while ((term2 = termsEnum3.next()) != null)
				{
				  Console.WriteLine("      " + term2.utf8ToString() + ": freq=" + termsEnum3.totalTermFreq());
				  dpEnum = termsEnum3.docsAndPositions(null, dpEnum);
				  if (dpEnum != null)
				  {
					Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
					int freq = dpEnum.freq();
					Console.WriteLine("        doc=" + dpEnum.docID() + " freq=" + freq);
					for (int posUpto = 0;posUpto < freq;posUpto++)
					{
					  Console.WriteLine("          pos=" + dpEnum.nextPosition());
					}
				  }
				  else
				  {
					dEnum = TestUtil.docs(random(), termsEnum3, null, dEnum, DocsEnum.FLAG_FREQS);
					Assert.IsNotNull(dEnum);
					Assert.IsTrue(dEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
					int freq = dEnum.freq();
					Console.WriteLine("        doc=" + dEnum.docID() + " freq=" + freq);
				  }
				}
			  }
			}

			Fields tv2 = r2.getTermVectors(id2);
			Console.WriteLine("  d2=" + tv2);
			if (tv2 != null)
			{
			  DocsAndPositionsEnum dpEnum = null;
			  DocsEnum dEnum = null;
			  foreach (string field in tv2)
			  {
				Console.WriteLine("    " + field + ":");
				Terms terms3 = tv2.terms(field);
				Assert.IsNotNull(terms3);
				TermsEnum termsEnum3 = terms3.iterator(null);
				BytesRef term2;
				while ((term2 = termsEnum3.next()) != null)
				{
				  Console.WriteLine("      " + term2.utf8ToString() + ": freq=" + termsEnum3.totalTermFreq());
				  dpEnum = termsEnum3.docsAndPositions(null, dpEnum);
				  if (dpEnum != null)
				  {
					Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
					int freq = dpEnum.freq();
					Console.WriteLine("        doc=" + dpEnum.docID() + " freq=" + freq);
					for (int posUpto = 0;posUpto < freq;posUpto++)
					{
					  Console.WriteLine("          pos=" + dpEnum.nextPosition());
					}
				  }
				  else
				  {
					dEnum = TestUtil.docs(random(), termsEnum3, null, dEnum, DocsEnum.FLAG_FREQS);
					Assert.IsNotNull(dEnum);
					Assert.IsTrue(dEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
					int freq = dEnum.freq();
					Console.WriteLine("        doc=" + dEnum.docID() + " freq=" + freq);
				  }
				}
			  }
			}

			throw e;
		  }
		}

		//System.out.println("TEST: done match id");

		// Verify postings
		//System.out.println("TEST: create te1");
		Fields fields1 = MultiFields.getFields(r1);
		IEnumerator<string> fields1Enum = fields1.GetEnumerator();
		Fields fields2 = MultiFields.getFields(r2);
		IEnumerator<string> fields2Enum = fields2.GetEnumerator();


		string field1 = null, field2 = null;
		TermsEnum termsEnum1 = null;
		termsEnum2 = null;
		DocsEnum docs1 = null, docs2 = null;

		// pack both doc and freq into single element for easy sorting
		long[] info1 = new long[r1.numDocs()];
		long[] info2 = new long[r2.numDocs()];

		for (;;)
		{
		  BytesRef term1 = null, term2 = null;

		  // iterate until we get some docs
		  int len1;
		  for (;;)
		  {
			len1 = 0;
			if (termsEnum1 == null)
			{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			  if (!fields1Enum.hasNext())
			  {
				break;
			  }
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			  field1 = fields1Enum.next();
			  Terms terms = fields1.terms(field1);
			  if (terms == null)
			  {
				continue;
			  }
			  termsEnum1 = terms.iterator(null);
			}
			term1 = termsEnum1.next();
			if (term1 == null)
			{
			  // no more terms in this field
			  termsEnum1 = null;
			  continue;
			}

			//System.out.println("TEST: term1=" + term1);
			docs1 = TestUtil.docs(random(), termsEnum1, liveDocs1, docs1, DocsEnum.FLAG_FREQS);
			while (docs1.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			{
			  int d = docs1.docID();
			  int f = docs1.freq();
			  info1[len1] = (((long)d) << 32) | f;
			  len1++;
			}
			if (len1 > 0)
			{
				break;
			}
		  }

		  // iterate until we get some docs
		  int len2;
		  for (;;)
		  {
			len2 = 0;
			if (termsEnum2 == null)
			{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			  if (!fields2Enum.hasNext())
			  {
				break;
			  }
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			  field2 = fields2Enum.next();
			  Terms terms = fields2.terms(field2);
			  if (terms == null)
			  {
				continue;
			  }
			  termsEnum2 = terms.iterator(null);
			}
			term2 = termsEnum2.next();
			if (term2 == null)
			{
			  // no more terms in this field
			  termsEnum2 = null;
			  continue;
			}

			//System.out.println("TEST: term1=" + term1);
			docs2 = TestUtil.docs(random(), termsEnum2, liveDocs2, docs2, DocsEnum.FLAG_FREQS);
			while (docs2.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			{
			  int d = r2r1[docs2.docID()];
			  int f = docs2.freq();
			  info2[len2] = (((long)d) << 32) | f;
			  len2++;
			}
			if (len2 > 0)
			{
				break;
			}
		  }

		  Assert.AreEqual(len1, len2);
		  if (len1 == 0) // no more terms
		  {
			  break;
		  }

		  Assert.AreEqual(field1, field2);
		  Assert.IsTrue(term1.bytesEquals(term2));

		  if (!hasDeletes)
		  {
			Assert.AreEqual(termsEnum1.docFreq(), termsEnum2.docFreq());
		  }

		  Assert.AreEqual("len1=" + len1 + " len2=" + len2 + " deletes?=" + hasDeletes, term1, term2);

		  // sort info2 to get it into ascending docid
		  Arrays.sort(info2, 0, len2);

		  // now compare
		  for (int i = 0; i < len1; i++)
		  {
			Assert.AreEqual("i=" + i + " len=" + len1 + " d1=" + ((long)((ulong)info1[i] >> 32)) + " f1=" + (info1[i] & int.MaxValue) + " d2=" + ((long)((ulong)info2[i] >> 32)) + " f2=" + (info2[i] & int.MaxValue) + " field=" + field1 + " term=" + term1.utf8ToString(), info1[i], info2[i]);
		  }
		}
	  }

	  public static void VerifyEquals(Document d1, Document d2)
	  {
		IList<IndexableField> ff1 = d1.Fields;
		IList<IndexableField> ff2 = d2.Fields;

		ff1.Sort(fieldNameComparator);
		ff2.Sort(fieldNameComparator);

		Assert.AreEqual(ff1 + " : " + ff2, ff1.Count, ff2.Count);

		for (int i = 0; i < ff1.Count; i++)
		{
		  IndexableField f1 = ff1[i];
		  IndexableField f2 = ff2[i];
		  if (f1.binaryValue() != null)
		  {
			assert(f2.binaryValue() != null);
		  }
		  else
		  {
			string s1 = f1.stringValue();
			string s2 = f2.stringValue();
			Assert.AreEqual(ff1 + " : " + ff2, s1,s2);
		  }
		}
	  }

	  public static void VerifyEquals(Fields d1, Fields d2)
	  {
		if (d1 == null)
		{
		  Assert.IsTrue(d2 == null || d2.size() == 0);
		  return;
		}
		Assert.IsTrue(d2 != null);

		IEnumerator<string> fieldsEnum2 = d2.GetEnumerator();

		foreach (string field1 in d1)
		{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  string field2 = fieldsEnum2.next();
		  Assert.AreEqual(field1, field2);

		  Terms terms1 = d1.terms(field1);
		  Assert.IsNotNull(terms1);
		  TermsEnum termsEnum1 = terms1.iterator(null);

		  Terms terms2 = d2.terms(field2);
		  Assert.IsNotNull(terms2);
		  TermsEnum termsEnum2 = terms2.iterator(null);

		  DocsAndPositionsEnum dpEnum1 = null;
		  DocsAndPositionsEnum dpEnum2 = null;
		  DocsEnum dEnum1 = null;
		  DocsEnum dEnum2 = null;

		  BytesRef term1;
		  while ((term1 = termsEnum1.next()) != null)
		  {
			BytesRef term2 = termsEnum2.next();
			Assert.AreEqual(term1, term2);
			Assert.AreEqual(termsEnum1.totalTermFreq(), termsEnum2.totalTermFreq());

			dpEnum1 = termsEnum1.docsAndPositions(null, dpEnum1);
			dpEnum2 = termsEnum2.docsAndPositions(null, dpEnum2);
			if (dpEnum1 != null)
			{
			  Assert.IsNotNull(dpEnum2);
			  int docID1 = dpEnum1.nextDoc();
			  dpEnum2.nextDoc();
			  // docIDs are not supposed to be equal
			  //int docID2 = dpEnum2.nextDoc();
			  //Assert.AreEqual(docID1, docID2);
			  Assert.IsTrue(docID1 != DocIdSetIterator.NO_MORE_DOCS);

			  int freq1 = dpEnum1.freq();
			  int freq2 = dpEnum2.freq();
			  Assert.AreEqual(freq1, freq2);
			  OffsetAttribute offsetAtt1 = dpEnum1.attributes().HasAttribute(typeof(OffsetAttribute)) ? dpEnum1.attributes().getAttribute(typeof(OffsetAttribute)) : null;
			  OffsetAttribute offsetAtt2 = dpEnum2.attributes().HasAttribute(typeof(OffsetAttribute)) ? dpEnum2.attributes().getAttribute(typeof(OffsetAttribute)) : null;

			  if (offsetAtt1 != null)
			  {
				Assert.IsNotNull(offsetAtt2);
			  }
			  else
			  {
				assertNull(offsetAtt2);
			  }

			  for (int posUpto = 0;posUpto < freq1;posUpto++)
			  {
				int pos1 = dpEnum1.nextPosition();
				int pos2 = dpEnum2.nextPosition();
				Assert.AreEqual(pos1, pos2);
				if (offsetAtt1 != null)
				{
				  Assert.AreEqual(offsetAtt1.StartOffset(), offsetAtt2.StartOffset());
				  Assert.AreEqual(offsetAtt1.EndOffset(), offsetAtt2.EndOffset());
				}
			  }
			  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum1.nextDoc());
			  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum2.nextDoc());
			}
			else
			{
			  dEnum1 = TestUtil.docs(random(), termsEnum1, null, dEnum1, DocsEnum.FLAG_FREQS);
			  dEnum2 = TestUtil.docs(random(), termsEnum2, null, dEnum2, DocsEnum.FLAG_FREQS);
			  Assert.IsNotNull(dEnum1);
			  Assert.IsNotNull(dEnum2);
			  int docID1 = dEnum1.nextDoc();
			  dEnum2.nextDoc();
			  // docIDs are not supposed to be equal
			  //int docID2 = dEnum2.nextDoc();
			  //Assert.AreEqual(docID1, docID2);
			  Assert.IsTrue(docID1 != DocIdSetIterator.NO_MORE_DOCS);
			  int freq1 = dEnum1.freq();
			  int freq2 = dEnum2.freq();
			  Assert.AreEqual(freq1, freq2);
			  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dEnum1.nextDoc());
			  Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dEnum2.nextDoc());
			}
		  }

		  assertNull(termsEnum2.next());
		}
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsFalse(fieldsEnum2.hasNext());
	  }

	  private class IndexingThread : System.Threading.Thread
	  {
		  private readonly TestStressIndexing2 OuterInstance;

		  public IndexingThread(TestStressIndexing2 outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		internal IndexWriter w;
		internal int @base;
		internal int Range;
		internal int Iterations;
		internal IDictionary<string, Document> Docs = new Dictionary<string, Document>();
		internal Random r;

		public virtual int NextInt(int lim)
		{
		  return r.Next(lim);
		}

		// start is inclusive and end is exclusive
		public virtual int NextInt(int start, int end)
		{
		  return start + r.Next(end - start);
		}

		internal char[] Buffer = new char[100];

		internal virtual int AddUTF8Token(int start)
		{
		  int end = start + NextInt(20);
		  if (Buffer.Length < 1 + end)
		  {
			char[] newBuffer = new char[(int)((1 + end) * 1.25)];
			Array.Copy(Buffer, 0, newBuffer, 0, Buffer.Length);
			Buffer = newBuffer;
		  }

		  for (int i = start;i < end;i++)
		  {
			int t = NextInt(5);
			if (0 == t && i < end - 1)
			{
			  // Make a surrogate pair
			  // High surrogate
			  Buffer[i++] = (char) NextInt(0xd800, 0xdc00);
			  // Low surrogate
			  Buffer[i] = (char) NextInt(0xdc00, 0xe000);
			}
			else if (t <= 1)
			{
			  Buffer[i] = (char) NextInt(0x80);
			}
			else if (2 == t)
			{
			  Buffer[i] = (char) NextInt(0x80, 0x800);
			}
			else if (3 == t)
			{
			  Buffer[i] = (char) NextInt(0x800, 0xd800);
			}
			else if (4 == t)
			{
			  Buffer[i] = (char) NextInt(0xe000, 0xffff);
			}
		  }
		  Buffer[end] = ' ';
		  return 1 + end;
		}

		public virtual string GetString(int nTokens)
		{
		  nTokens = nTokens != 0 ? nTokens : r.Next(4) + 1;

		  // Half the time make a random UTF8 string
		  if (r.nextBoolean())
		  {
			return GetUTF8String(nTokens);
		  }

		  // avoid StringBuffer because it adds extra synchronization.
		  char[] arr = new char[nTokens * 2];
		  for (int i = 0; i < nTokens; i++)
		  {
			arr[i * 2] = (char)('A' + r.Next(10));
			arr[i * 2 + 1] = ' ';
		  }
		  return new string(arr);
		}

		public virtual string GetUTF8String(int nTokens)
		{
		  int upto = 0;
		  Arrays.fill(Buffer, (char) 0);
		  for (int i = 0;i < nTokens;i++)
		  {
			upto = AddUTF8Token(upto);
		  }
		  return new string(Buffer, 0, upto);
		}

		public virtual string IdString
		{
			get
			{
			  return Convert.ToString(@base + NextInt(Range));
			}
		}

		public virtual void IndexDoc()
		{
		  Document d = new Document();

		  FieldType customType1 = new FieldType(TextField.TYPE_STORED);
		  customType1.Tokenized = false;
		  customType1.OmitNorms = true;

		  List<Field> fields = new List<Field>();
		  string idString = IdString;
		  Field idField = newField("id", idString, customType1);
		  fields.Add(idField);

		  int nFields = NextInt(MaxFields);
		  for (int i = 0; i < nFields; i++)
		  {

			FieldType customType = new FieldType();
			switch (NextInt(4))
			{
			case 0:
			  break;
			case 1:
			  customType.StoreTermVectors = true;
			  break;
			case 2:
			  customType.StoreTermVectors = true;
			  customType.StoreTermVectorPositions = true;
			  break;
			case 3:
			  customType.StoreTermVectors = true;
			  customType.StoreTermVectorOffsets = true;
			  break;
			}

			switch (NextInt(4))
			{
			  case 0:
				customType.Stored = true;
				customType.OmitNorms = true;
				customType.Indexed = true;
				fields.Add(newField("f" + NextInt(100), GetString(1), customType));
				break;
			  case 1:
				customType.Indexed = true;
				customType.Tokenized = true;
				fields.Add(newField("f" + NextInt(100), GetString(0), customType));
				break;
			  case 2:
				customType.Stored = true;
				customType.StoreTermVectors = false;
				customType.StoreTermVectorOffsets = false;
				customType.StoreTermVectorPositions = false;
				fields.Add(newField("f" + NextInt(100), GetString(0), customType));
				break;
			  case 3:
				customType.Stored = true;
				customType.Indexed = true;
				customType.Tokenized = true;
				fields.Add(newField("f" + NextInt(100), GetString(BigFieldSize), customType));
				break;
			}
		  }

		  if (SameFieldOrder)
		  {
			fields.Sort(fieldNameComparator);
		  }
		  else
		  {
			// random placement of id field also
			Collections.swap(fields,NextInt(fields.Count), 0);
		  }

		  for (int i = 0; i < fields.Count; i++)
		  {
			d.add(fields[i]);
		  }
		  if (VERBOSE)
		  {
			Console.WriteLine(Thread.CurrentThread.Name + ": indexing id:" + idString);
		  }
		  w.updateDocument(new Term("id", idString), d);
		  //System.out.println(Thread.currentThread().getName() + ": indexing "+d);
		  Docs[idString] = d;
		}

		public virtual void DeleteDoc()
		{
		  string idString = IdString;
		  if (VERBOSE)
		  {
			Console.WriteLine(Thread.CurrentThread.Name + ": del id:" + idString);
		  }
		  w.deleteDocuments(new Term("id", idString));
		  Docs.Remove(idString);
		}

		public virtual void DeleteByQuery()
		{
		  string idString = IdString;
		  if (VERBOSE)
		  {
			Console.WriteLine(Thread.CurrentThread.Name + ": del query id:" + idString);
		  }
		  w.deleteDocuments(new TermQuery(new Term("id", idString)));
		  Docs.Remove(idString);
		}

		public override void Run()
		{
		  try
		  {
			r = new Random(@base + Range + Seed);
			for (int i = 0; i < Iterations; i++)
			{
			  int what = NextInt(100);
			  if (what < 5)
			  {
				DeleteDoc();
			  }
			  else if (what < 10)
			  {
				DeleteByQuery();
			  }
			  else
			  {
				IndexDoc();
			  }
			}
		  }
		  catch (Exception e)
		  {
			Console.WriteLine(e.ToString());
			Console.Write(e.StackTrace);
			Assert.Assert.Fail(e.ToString());
		  }

		  lock (this)
		  {
			Docs.Count;
		  }
		}
	  }
	}

}