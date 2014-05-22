using System;

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
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	/// <summary>
	/// Some tests for <seealso cref="ParallelAtomicReader"/>s with empty indexes
	/// </summary>
	public class TestParallelReaderEmptyIndex : LuceneTestCase
	{

	  /// <summary>
	  /// Creates two empty indexes and wraps a ParallelReader around. Adding this
	  /// reader to a new index should not throw any exception.
	  /// </summary>
	  public virtual void TestEmptyIndex()
	  {
		Directory rd1 = newDirectory();
		IndexWriter iw = new IndexWriter(rd1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		iw.close();
		// create a copy:
		Directory rd2 = newDirectory(rd1);

		Directory rdOut = newDirectory();

		IndexWriter iwOut = new IndexWriter(rdOut, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		ParallelAtomicReader apr = new ParallelAtomicReader(SlowCompositeReaderWrapper.wrap(DirectoryReader.open(rd1)), SlowCompositeReaderWrapper.wrap(DirectoryReader.open(rd2)));

		// When unpatched, Lucene crashes here with a NoSuchElementException (caused by ParallelTermEnum)
		iwOut.addIndexes(apr);
		iwOut.forceMerge(1);

		// 2nd try with a readerless parallel reader
		iwOut.addIndexes(new ParallelAtomicReader());
		iwOut.forceMerge(1);

		ParallelCompositeReader cpr = new ParallelCompositeReader(DirectoryReader.open(rd1), DirectoryReader.open(rd2));

		// When unpatched, Lucene crashes here with a NoSuchElementException (caused by ParallelTermEnum)
		iwOut.addIndexes(cpr);
		iwOut.forceMerge(1);

		// 2nd try with a readerless parallel reader
		iwOut.addIndexes(new ParallelCompositeReader());
		iwOut.forceMerge(1);

		iwOut.close();
		rdOut.close();
		rd1.close();
		rd2.close();
	  }

	  /// <summary>
	  /// this method creates an empty index (numFields=0, numDocs=0) but is marked
	  /// to have TermVectors. Adding this index to another index should not throw
	  /// any exception.
	  /// </summary>
	  public virtual void TestEmptyIndexWithVectors()
	  {
		Directory rd1 = newDirectory();
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: make 1st writer");
		  }
		  IndexWriter iw = new IndexWriter(rd1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  Document doc = new Document();
		  Field idField = newTextField("id", "", Field.Store.NO);
		  doc.add(idField);
		  FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		  customType.StoreTermVectors = true;
		  doc.add(newField("test", "", customType));
		  idField.StringValue = "1";
		  iw.addDocument(doc);
		  doc.add(newTextField("test", "", Field.Store.NO));
		  idField.StringValue = "2";
		  iw.addDocument(doc);
		  iw.close();

		  IndexWriterConfig dontMergeConfig = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setMergePolicy(NoMergePolicy.COMPOUND_FILES);
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: make 2nd writer");
		  }
		  IndexWriter writer = new IndexWriter(rd1, dontMergeConfig);

		  writer.deleteDocuments(new Term("id", "1"));
		  writer.close();
		  IndexReader ir = DirectoryReader.open(rd1);
		  Assert.AreEqual(2, ir.maxDoc());
		  Assert.AreEqual(1, ir.numDocs());
		  ir.close();

		  iw = new IndexWriter(rd1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));
		  iw.forceMerge(1);
		  iw.close();
		}

		Directory rd2 = newDirectory();
		{
		  IndexWriter iw = new IndexWriter(rd2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  Document doc = new Document();
		  iw.addDocument(doc);
		  iw.close();
		}

		Directory rdOut = newDirectory();

		IndexWriter iwOut = new IndexWriter(rdOut, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		DirectoryReader reader1, reader2;
		ParallelAtomicReader pr = new ParallelAtomicReader(SlowCompositeReaderWrapper.wrap(reader1 = DirectoryReader.open(rd1)), SlowCompositeReaderWrapper.wrap(reader2 = DirectoryReader.open(rd2)));

		// When unpatched, Lucene crashes here with an ArrayIndexOutOfBoundsException (caused by TermVectorsWriter)
		iwOut.addIndexes(pr);

		// ParallelReader closes any IndexReader you added to it:
		pr.close();

		// assert subreaders were closed
		Assert.AreEqual(0, reader1.RefCount);
		Assert.AreEqual(0, reader2.RefCount);

		rd1.close();
		rd2.close();

		iwOut.forceMerge(1);
		iwOut.close();

		rdOut.close();
	  }
	}

}