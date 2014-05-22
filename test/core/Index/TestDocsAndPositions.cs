using System;
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
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestDocsAndPositions : LuceneTestCase
	{
	  private string FieldName;

	  public override void SetUp()
	  {
		base.setUp();
		FieldName = "field" + random().Next();
	  }

	  /// <summary>
	  /// Simple testcase for <seealso cref="DocsAndPositionsEnum"/>
	  /// </summary>
	  public virtual void TestPositionsSimple()
	  {
		Directory directory = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), directory, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		for (int i = 0; i < 39; i++)
		{
		  Document doc = new Document();
		  FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		  customType.OmitNorms = true;
		  doc.add(newField(FieldName, "1 2 3 4 5 6 7 8 9 10 " + "1 2 3 4 5 6 7 8 9 10 " + "1 2 3 4 5 6 7 8 9 10 " + "1 2 3 4 5 6 7 8 9 10", customType));
		  writer.addDocument(doc);
		}
		IndexReader reader = writer.Reader;
		writer.close();

		int num = atLeast(13);
		for (int i = 0; i < num; i++)
		{
		  BytesRef bytes = new BytesRef("1");
		  IndexReaderContext topReaderContext = reader.Context;
		  foreach (AtomicReaderContext atomicReaderContext in topReaderContext.leaves())
		  {
			DocsAndPositionsEnum docsAndPosEnum = GetDocsAndPositions(atomicReaderContext.reader(), bytes, null);
			Assert.IsNotNull(docsAndPosEnum);
			if (atomicReaderContext.reader().maxDoc() == 0)
			{
			  continue;
			}
			int advance = docsAndPosEnum.advance(random().Next(atomicReaderContext.reader().maxDoc()));
			do
			{
			  string msg = "Advanced to: " + advance + " current doc: " + docsAndPosEnum.docID(); // TODO: + " usePayloads: " + usePayload;
			  Assert.AreEqual(msg, 4, docsAndPosEnum.freq());
			  Assert.AreEqual(msg, 0, docsAndPosEnum.nextPosition());
			  Assert.AreEqual(msg, 4, docsAndPosEnum.freq());
			  Assert.AreEqual(msg, 10, docsAndPosEnum.nextPosition());
			  Assert.AreEqual(msg, 4, docsAndPosEnum.freq());
			  Assert.AreEqual(msg, 20, docsAndPosEnum.nextPosition());
			  Assert.AreEqual(msg, 4, docsAndPosEnum.freq());
			  Assert.AreEqual(msg, 30, docsAndPosEnum.nextPosition());
			} while (docsAndPosEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		  }
		}
		reader.close();
		directory.close();
	  }

	  public virtual DocsAndPositionsEnum GetDocsAndPositions(AtomicReader reader, BytesRef bytes, Bits liveDocs)
	  {
		Terms terms = reader.terms(FieldName);
		if (terms != null)
		{
		  TermsEnum te = terms.iterator(null);
		  if (te.seekExact(bytes))
		  {
			return te.docsAndPositions(liveDocs, null);
		  }
		}
		return null;
	  }

	  /// <summary>
	  /// this test indexes random numbers within a range into a field and checks
	  /// their occurrences by searching for a number from that range selected at
	  /// random. All positions for that number are saved up front and compared to
	  /// the enums positions.
	  /// </summary>
	  public virtual void TestRandomPositions()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
		int numDocs = atLeast(47);
		int max = 1051;
		int term = random().Next(max);
		int?[][] positionsInDoc = new int?[numDocs][];
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.OmitNorms = true;
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  List<int?> positions = new List<int?>();
		  StringBuilder builder = new StringBuilder();
		  int num = atLeast(131);
		  for (int j = 0; j < num; j++)
		  {
			int nextInt = random().Next(max);
			builder.Append(nextInt).Append(" ");
			if (nextInt == term)
			{
			  positions.Add(Convert.ToInt32(j));
			}
		  }
		  if (positions.Count == 0)
		  {
			builder.Append(term);
			positions.Add(num);
		  }
		  doc.add(newField(FieldName, builder.ToString(), customType));
		  positionsInDoc[i] = positions.ToArray();
		  writer.addDocument(doc);
		}

		IndexReader reader = writer.Reader;
		writer.close();

		int num = atLeast(13);
		for (int i = 0; i < num; i++)
		{
		  BytesRef bytes = new BytesRef("" + term);
		  IndexReaderContext topReaderContext = reader.Context;
		  foreach (AtomicReaderContext atomicReaderContext in topReaderContext.leaves())
		  {
			DocsAndPositionsEnum docsAndPosEnum = GetDocsAndPositions(atomicReaderContext.reader(), bytes, null);
			Assert.IsNotNull(docsAndPosEnum);
			int initDoc = 0;
			int maxDoc = atomicReaderContext.reader().maxDoc();
			// initially advance or do next doc
			if (random().nextBoolean())
			{
			  initDoc = docsAndPosEnum.nextDoc();
			}
			else
			{
			  initDoc = docsAndPosEnum.advance(random().Next(maxDoc));
			}
			// now run through the scorer and check if all positions are there...
			do
			{
			  int docID = docsAndPosEnum.docID();
			  if (docID == DocIdSetIterator.NO_MORE_DOCS)
			  {
				break;
			  }
			  int?[] pos = positionsInDoc[atomicReaderContext.docBase + docID];
			  Assert.AreEqual(pos.Length, docsAndPosEnum.freq());
			  // number of positions read should be random - don't read all of them
			  // allways
			  int howMany = random().Next(20) == 0 ? pos.Length - random().Next(pos.Length) : pos.Length;
			  for (int j = 0; j < howMany; j++)
			  {
				Assert.AreEqual("iteration: " + i + " initDoc: " + initDoc + " doc: " + docID + " base: " + atomicReaderContext.docBase + " positions: " + Arrays.ToString(pos), (int)pos[j], docsAndPosEnum.nextPosition()); /* TODO: + " usePayloads: "
	                + usePayload*/
			  }

			  if (random().Next(10) == 0) // once is a while advance
			  {
				if (docsAndPosEnum.advance(docID + 1 + random().Next((maxDoc - docID))) == DocIdSetIterator.NO_MORE_DOCS)
				{
				  break;
				}
			  }

			} while (docsAndPosEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		  }

		}
		reader.close();
		dir.close();
	  }

	  public virtual void TestRandomDocs()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
		int numDocs = atLeast(49);
		int max = 15678;
		int term = random().Next(max);
		int[] freqInDoc = new int[numDocs];
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.OmitNorms = true;
		for (int i = 0; i < numDocs; i++)
		{
		  Document doc = new Document();
		  StringBuilder builder = new StringBuilder();
		  for (int j = 0; j < 199; j++)
		  {
			int nextInt = random().Next(max);
			builder.Append(nextInt).Append(' ');
			if (nextInt == term)
			{
			  freqInDoc[i]++;
			}
		  }
		  doc.add(newField(FieldName, builder.ToString(), customType));
		  writer.addDocument(doc);
		}

		IndexReader reader = writer.Reader;
		writer.close();

		int num = atLeast(13);
		for (int i = 0; i < num; i++)
		{
		  BytesRef bytes = new BytesRef("" + term);
		  IndexReaderContext topReaderContext = reader.Context;
		  foreach (AtomicReaderContext context in topReaderContext.leaves())
		  {
			int maxDoc = context.reader().maxDoc();
			DocsEnum docsEnum = TestUtil.docs(random(), context.reader(), FieldName, bytes, null, null, DocsEnum.FLAG_FREQS);
			if (FindNext(freqInDoc, context.docBase, context.docBase + maxDoc) == int.MaxValue)
			{
			  assertNull(docsEnum);
			  continue;
			}
			Assert.IsNotNull(docsEnum);
			docsEnum.nextDoc();
			for (int j = 0; j < maxDoc; j++)
			{
			  if (freqInDoc[context.docBase + j] != 0)
			  {
				Assert.AreEqual(j, docsEnum.docID());
				Assert.AreEqual(docsEnum.freq(), freqInDoc[context.docBase + j]);
				if (i % 2 == 0 && random().Next(10) == 0)
				{
				  int next = FindNext(freqInDoc, context.docBase + j + 1, context.docBase + maxDoc) - context.docBase;
				  int advancedTo = docsEnum.advance(next);
				  if (next >= maxDoc)
				  {
					Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, advancedTo);
				  }
				  else
				  {
					Assert.IsTrue("advanced to: " + advancedTo + " but should be <= " + next, next >= advancedTo);
				  }
				}
				else
				{
				  docsEnum.nextDoc();
				}
			  }
			}
			Assert.AreEqual("docBase: " + context.docBase + " maxDoc: " + maxDoc + " " + docsEnum.GetType(), DocIdSetIterator.NO_MORE_DOCS, docsEnum.docID());
		  }

		}

		reader.close();
		dir.close();
	  }

	  private static int FindNext(int[] docs, int pos, int max)
	  {
		for (int i = pos; i < max; i++)
		{
		  if (docs[i] != 0)
		  {
			return i;
		  }
		}
		return int.MaxValue;
	  }

	  /// <summary>
	  /// tests retrieval of positions for terms that have a large number of
	  /// occurrences to force test of buffer refill during positions iteration.
	  /// </summary>
	  public virtual void TestLargeNumberOfPositions()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		int howMany = 1000;
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.OmitNorms = true;
		for (int i = 0; i < 39; i++)
		{
		  Document doc = new Document();
		  StringBuilder builder = new StringBuilder();
		  for (int j = 0; j < howMany; j++)
		  {
			if (j % 2 == 0)
			{
			  builder.Append("even ");
			}
			else
			{
			  builder.Append("odd ");
			}
		  }
		  doc.add(newField(FieldName, builder.ToString(), customType));
		  writer.addDocument(doc);
		}

		// now do searches
		IndexReader reader = writer.Reader;
		writer.close();

		int num = atLeast(13);
		for (int i = 0; i < num; i++)
		{
		  BytesRef bytes = new BytesRef("even");

		  IndexReaderContext topReaderContext = reader.Context;
		  foreach (AtomicReaderContext atomicReaderContext in topReaderContext.leaves())
		  {
			DocsAndPositionsEnum docsAndPosEnum = GetDocsAndPositions(atomicReaderContext.reader(), bytes, null);
			Assert.IsNotNull(docsAndPosEnum);

			int initDoc = 0;
			int maxDoc = atomicReaderContext.reader().maxDoc();
			// initially advance or do next doc
			if (random().nextBoolean())
			{
			  initDoc = docsAndPosEnum.nextDoc();
			}
			else
			{
			  initDoc = docsAndPosEnum.advance(random().Next(maxDoc));
			}
			string msg = "Iteration: " + i + " initDoc: " + initDoc; // TODO: + " payloads: " + usePayload;
			Assert.AreEqual(howMany / 2, docsAndPosEnum.freq());
			for (int j = 0; j < howMany; j += 2)
			{
			  Assert.AreEqual("position missmatch index: " + j + " with freq: " + docsAndPosEnum.freq() + " -- " + msg, j, docsAndPosEnum.nextPosition());
			}
		  }
		}
		reader.close();
		dir.close();
	  }

	  public virtual void TestDocsEnumStart()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newStringField("foo", "bar", Field.Store.NO));
		writer.addDocument(doc);
		DirectoryReader reader = writer.Reader;
		AtomicReader r = getOnlySegmentReader(reader);
		DocsEnum disi = TestUtil.docs(random(), r, "foo", new BytesRef("bar"), null, null, DocsEnum.FLAG_NONE);
		int docid = disi.docID();
		Assert.AreEqual(-1, docid);
		Assert.IsTrue(disi.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);

		// now reuse and check again
		TermsEnum te = r.terms("foo").iterator(null);
		Assert.IsTrue(te.seekExact(new BytesRef("bar")));
		disi = TestUtil.docs(random(), te, null, disi, DocsEnum.FLAG_NONE);
		docid = disi.docID();
		Assert.AreEqual(-1, docid);
		Assert.IsTrue(disi.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		writer.close();
		r.close();
		dir.close();
	  }

	  public virtual void TestDocsAndPositionsEnumStart()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newTextField("foo", "bar", Field.Store.NO));
		writer.addDocument(doc);
		DirectoryReader reader = writer.Reader;
		AtomicReader r = getOnlySegmentReader(reader);
		DocsAndPositionsEnum disi = r.termPositionsEnum(new Term("foo", "bar"));
		int docid = disi.docID();
		Assert.AreEqual(-1, docid);
		Assert.IsTrue(disi.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);

		// now reuse and check again
		TermsEnum te = r.terms("foo").iterator(null);
		Assert.IsTrue(te.seekExact(new BytesRef("bar")));
		disi = te.docsAndPositions(null, disi);
		docid = disi.docID();
		Assert.AreEqual(-1, docid);
		Assert.IsTrue(disi.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		writer.close();
		r.close();
		dir.close();
	  }
	}

}