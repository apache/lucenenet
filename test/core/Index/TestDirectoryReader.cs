using System;
using System.Collections.Generic;
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
	using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using StoredField = Lucene.Net.Document.StoredField;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using FieldCache = Lucene.Net.Search.FieldCache;
	using Directory = Lucene.Net.Store.Directory;
	using NoSuchDirectoryException = Lucene.Net.Store.NoSuchDirectoryException;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using Assume = org.junit.Assume;

	public class TestDirectoryReader : LuceneTestCase
	{

	  public virtual void TestDocument()
	  {
		SegmentReader[] readers = new SegmentReader[2];
		Directory dir = newDirectory();
		Document doc1 = new Document();
		Document doc2 = new Document();
		DocHelper.setupDoc(doc1);
		DocHelper.setupDoc(doc2);
		DocHelper.writeDoc(random(), dir, doc1);
		DocHelper.writeDoc(random(), dir, doc2);
		DirectoryReader reader = DirectoryReader.open(dir);
		Assert.IsTrue(reader != null);
		Assert.IsTrue(reader is StandardDirectoryReader);

		Document newDoc1 = reader.document(0);
		Assert.IsTrue(newDoc1 != null);
		Assert.IsTrue(DocHelper.numFields(newDoc1) == DocHelper.numFields(doc1) - DocHelper.unstored.size());
		Document newDoc2 = reader.document(1);
		Assert.IsTrue(newDoc2 != null);
		Assert.IsTrue(DocHelper.numFields(newDoc2) == DocHelper.numFields(doc2) - DocHelper.unstored.size());
		Terms vector = reader.getTermVectors(0).terms(DocHelper.TEXT_FIELD_2_KEY);
		Assert.IsNotNull(vector);

		reader.close();
		if (readers[0] != null)
		{
			readers[0].close();
		}
		if (readers[1] != null)
		{
			readers[1].close();
		}
		dir.close();
	  }

	  public virtual void TestMultiTermDocs()
	  {
		Directory ramDir1 = newDirectory();
		AddDoc(random(), ramDir1, "test foo", true);
		Directory ramDir2 = newDirectory();
		AddDoc(random(), ramDir2, "test blah", true);
		Directory ramDir3 = newDirectory();
		AddDoc(random(), ramDir3, "test wow", true);

		IndexReader[] readers1 = new IndexReader[]{DirectoryReader.open(ramDir1), DirectoryReader.open(ramDir3)};
		IndexReader[] readers2 = new IndexReader[]{DirectoryReader.open(ramDir1), DirectoryReader.open(ramDir2), DirectoryReader.open(ramDir3)};
		MultiReader mr2 = new MultiReader(readers1);
		MultiReader mr3 = new MultiReader(readers2);

		// test mixing up TermDocs and TermEnums from different readers.
		TermsEnum te2 = MultiFields.getTerms(mr2, "body").iterator(null);
		te2.seekCeil(new BytesRef("wow"));
		DocsEnum td = TestUtil.docs(random(), mr2, "body", te2.term(), MultiFields.getLiveDocs(mr2), null, 0);

		TermsEnum te3 = MultiFields.getTerms(mr3, "body").iterator(null);
		te3.seekCeil(new BytesRef("wow"));
		td = TestUtil.docs(random(), te3, MultiFields.getLiveDocs(mr3), td, 0);

		int ret = 0;

		// this should blow up if we forget to check that the TermEnum is from the same
		// reader as the TermDocs.
		while (td.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
		{
			ret += td.docID();
		}

		// really a dummy assert to ensure that we got some docs and to ensure that
		// nothing is eliminated by hotspot
		Assert.IsTrue(ret > 0);
		readers1[0].close();
		readers1[1].close();
		readers2[0].close();
		readers2[1].close();
		readers2[2].close();
		ramDir1.close();
		ramDir2.close();
		ramDir3.close();
	  }

	  private void AddDoc(Random random, Directory ramDir1, string s, bool create)
	  {
		IndexWriter iw = new IndexWriter(ramDir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).setOpenMode(create ? OpenMode.CREATE : OpenMode.APPEND));
		Document doc = new Document();
		doc.add(newTextField("body", s, Field.Store.NO));
		iw.addDocument(doc);
		iw.close();
	  }

	  public virtual void TestIsCurrent()
	  {
		Directory d = newDirectory();
		IndexWriter writer = new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		AddDocumentWithFields(writer);
		writer.close();
		// set up reader:
		DirectoryReader reader = DirectoryReader.open(d);
		Assert.IsTrue(reader.Current);
		// modify index by adding another document:
		writer = new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));
		AddDocumentWithFields(writer);
		writer.close();
		Assert.IsFalse(reader.Current);
		// re-create index:
		writer = new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE));
		AddDocumentWithFields(writer);
		writer.close();
		Assert.IsFalse(reader.Current);
		reader.close();
		d.close();
	  }

	  /// <summary>
	  /// Tests the IndexReader.getFieldNames implementation </summary>
	  /// <exception cref="Exception"> on error </exception>
	  public virtual void TestGetFieldNames()
	  {
		  Directory d = newDirectory();
		  // set up writer
		  IndexWriter writer = new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		  Document doc = new Document();

		  FieldType customType3 = new FieldType();
		  customType3.Stored = true;

		  doc.add(new StringField("keyword", "test1", Field.Store.YES));
		  doc.add(new TextField("text", "test1", Field.Store.YES));
		  doc.add(new Field("unindexed", "test1", customType3));
		  doc.add(new TextField("unstored","test1", Field.Store.NO));
		  writer.addDocument(doc);

		  writer.close();
		  // set up reader
		  DirectoryReader reader = DirectoryReader.open(d);
		  FieldInfos fieldInfos = MultiFields.getMergedFieldInfos(reader);
		  Assert.IsNotNull(fieldInfos.fieldInfo("keyword"));
		  Assert.IsNotNull(fieldInfos.fieldInfo("text"));
		  Assert.IsNotNull(fieldInfos.fieldInfo("unindexed"));
		  Assert.IsNotNull(fieldInfos.fieldInfo("unstored"));
		  reader.close();
		  // add more documents
		  writer = new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMergePolicy(newLogMergePolicy()));
		  // want to get some more segments here
		  int mergeFactor = ((LogMergePolicy) writer.Config.MergePolicy).MergeFactor;
		  for (int i = 0; i < 5 * mergeFactor; i++)
		  {
			doc = new Document();
			doc.add(new StringField("keyword", "test1", Field.Store.YES));
			doc.add(new TextField("text", "test1", Field.Store.YES));
			doc.add(new Field("unindexed", "test1", customType3));
			doc.add(new TextField("unstored","test1", Field.Store.NO));
			writer.addDocument(doc);
		  }
		  // new fields are in some different segments (we hope)
		  for (int i = 0; i < 5 * mergeFactor; i++)
		  {
			doc = new Document();
			doc.add(new StringField("keyword2", "test1", Field.Store.YES));
			doc.add(new TextField("text2", "test1", Field.Store.YES));
			doc.add(new Field("unindexed2", "test1", customType3));
			doc.add(new TextField("unstored2","test1", Field.Store.NO));
			writer.addDocument(doc);
		  }
		  // new termvector fields

		  FieldType customType5 = new FieldType(TextField.TYPE_STORED);
		  customType5.StoreTermVectors = true;
		  FieldType customType6 = new FieldType(TextField.TYPE_STORED);
		  customType6.StoreTermVectors = true;
		  customType6.StoreTermVectorOffsets = true;
		  FieldType customType7 = new FieldType(TextField.TYPE_STORED);
		  customType7.StoreTermVectors = true;
		  customType7.StoreTermVectorPositions = true;
		  FieldType customType8 = new FieldType(TextField.TYPE_STORED);
		  customType8.StoreTermVectors = true;
		  customType8.StoreTermVectorOffsets = true;
		  customType8.StoreTermVectorPositions = true;

		  for (int i = 0; i < 5 * mergeFactor; i++)
		  {
			doc = new Document();
			doc.add(new TextField("tvnot", "tvnot", Field.Store.YES));
			doc.add(new Field("termvector", "termvector", customType5));
			doc.add(new Field("tvoffset", "tvoffset", customType6));
			doc.add(new Field("tvposition", "tvposition", customType7));
			doc.add(new Field("tvpositionoffset", "tvpositionoffset", customType8));
			writer.addDocument(doc);
		  }

		  writer.close();

		  // verify fields again
		  reader = DirectoryReader.open(d);
		  fieldInfos = MultiFields.getMergedFieldInfos(reader);

		  ICollection<string> allFieldNames = new HashSet<string>();
		  ICollection<string> indexedFieldNames = new HashSet<string>();
		  ICollection<string> notIndexedFieldNames = new HashSet<string>();
		  ICollection<string> tvFieldNames = new HashSet<string>();

		  foreach (FieldInfo fieldInfo in fieldInfos)
		  {
			string name = fieldInfo.name;
			allFieldNames.Add(name);
			if (fieldInfo.Indexed)
			{
			  indexedFieldNames.Add(name);
			}
			else
			{
			  notIndexedFieldNames.Add(name);
			}
			if (fieldInfo.hasVectors())
			{
			  tvFieldNames.Add(name);
			}
		  }

		  Assert.IsTrue(allFieldNames.Contains("keyword"));
		  Assert.IsTrue(allFieldNames.Contains("text"));
		  Assert.IsTrue(allFieldNames.Contains("unindexed"));
		  Assert.IsTrue(allFieldNames.Contains("unstored"));
		  Assert.IsTrue(allFieldNames.Contains("keyword2"));
		  Assert.IsTrue(allFieldNames.Contains("text2"));
		  Assert.IsTrue(allFieldNames.Contains("unindexed2"));
		  Assert.IsTrue(allFieldNames.Contains("unstored2"));
		  Assert.IsTrue(allFieldNames.Contains("tvnot"));
		  Assert.IsTrue(allFieldNames.Contains("termvector"));
		  Assert.IsTrue(allFieldNames.Contains("tvposition"));
		  Assert.IsTrue(allFieldNames.Contains("tvoffset"));
		  Assert.IsTrue(allFieldNames.Contains("tvpositionoffset"));

		  // verify that only indexed fields were returned
		  Assert.AreEqual(11, indexedFieldNames.Count); // 6 original + the 5 termvector fields
		  Assert.IsTrue(indexedFieldNames.Contains("keyword"));
		  Assert.IsTrue(indexedFieldNames.Contains("text"));
		  Assert.IsTrue(indexedFieldNames.Contains("unstored"));
		  Assert.IsTrue(indexedFieldNames.Contains("keyword2"));
		  Assert.IsTrue(indexedFieldNames.Contains("text2"));
		  Assert.IsTrue(indexedFieldNames.Contains("unstored2"));
		  Assert.IsTrue(indexedFieldNames.Contains("tvnot"));
		  Assert.IsTrue(indexedFieldNames.Contains("termvector"));
		  Assert.IsTrue(indexedFieldNames.Contains("tvposition"));
		  Assert.IsTrue(indexedFieldNames.Contains("tvoffset"));
		  Assert.IsTrue(indexedFieldNames.Contains("tvpositionoffset"));

		  // verify that only unindexed fields were returned
		  Assert.AreEqual(2, notIndexedFieldNames.Count); // the following fields
		  Assert.IsTrue(notIndexedFieldNames.Contains("unindexed"));
		  Assert.IsTrue(notIndexedFieldNames.Contains("unindexed2"));

		  // verify index term vector fields  
		  Assert.AreEqual(tvFieldNames.ToString(), 4, tvFieldNames.Count); // 4 field has term vector only
		  Assert.IsTrue(tvFieldNames.Contains("termvector"));

		  reader.close();
		  d.close();
	  }

	public virtual void TestTermVectors()
	{
	  Directory d = newDirectory();
	  // set up writer
	  IndexWriter writer = new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
	  // want to get some more segments here
	  // new termvector fields
	  int mergeFactor = ((LogMergePolicy) writer.Config.MergePolicy).MergeFactor;
	  FieldType customType5 = new FieldType(TextField.TYPE_STORED);
	  customType5.StoreTermVectors = true;
	  FieldType customType6 = new FieldType(TextField.TYPE_STORED);
	  customType6.StoreTermVectors = true;
	  customType6.StoreTermVectorOffsets = true;
	  FieldType customType7 = new FieldType(TextField.TYPE_STORED);
	  customType7.StoreTermVectors = true;
	  customType7.StoreTermVectorPositions = true;
	  FieldType customType8 = new FieldType(TextField.TYPE_STORED);
	  customType8.StoreTermVectors = true;
	  customType8.StoreTermVectorOffsets = true;
	  customType8.StoreTermVectorPositions = true;
	  for (int i = 0; i < 5 * mergeFactor; i++)
	  {
		Document doc = new Document();
		  doc.add(new TextField("tvnot", "one two two three three three", Field.Store.YES));
		  doc.add(new Field("termvector", "one two two three three three", customType5));
		  doc.add(new Field("tvoffset", "one two two three three three", customType6));
		  doc.add(new Field("tvposition", "one two two three three three", customType7));
		  doc.add(new Field("tvpositionoffset", "one two two three three three", customType8));

		  writer.addDocument(doc);
	  }
	  writer.close();
	  d.close();
	}

	internal virtual void AssertTermDocsCount(string msg, IndexReader reader, Term term, int expected)
	{
	  DocsEnum tdocs = TestUtil.docs(random(), reader, term.field(), new BytesRef(term.text()), MultiFields.getLiveDocs(reader), null, 0);
	  int count = 0;
	  if (tdocs != null)
	  {
		while (tdocs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
		{
		  count++;
		}
	  }
	  Assert.AreEqual(msg + ", count mismatch", expected, count);
	}


	  public virtual void TestBinaryFields()
	  {
		Directory dir = newDirectory();
		sbyte[] bin = new sbyte[]{0, 1, 2, 3, 4, 5, 6, 7, 8, 9};

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));

		for (int i = 0; i < 10; i++)
		{
		  AddDoc(writer, "document number " + (i + 1));
		  AddDocumentWithFields(writer);
		  AddDocumentWithDifferentFields(writer);
		  AddDocumentWithTermVectorFields(writer);
		}
		writer.close();
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMergePolicy(newLogMergePolicy()));
		Document doc = new Document();
		doc.add(new StoredField("bin1", bin));
		doc.add(new TextField("junk", "junk text", Field.Store.NO));
		writer.addDocument(doc);
		writer.close();
		DirectoryReader reader = DirectoryReader.open(dir);
		Document doc2 = reader.document(reader.maxDoc() - 1);
		IndexableField[] fields = doc2.getFields("bin1");
		Assert.IsNotNull(fields);
		Assert.AreEqual(1, fields.Length);
		IndexableField b1 = fields[0];
		Assert.IsTrue(b1.binaryValue() != null);
		BytesRef bytesRef = b1.binaryValue();
		Assert.AreEqual(bin.Length, bytesRef.length);
		for (int i = 0; i < bin.Length; i++)
		{
		  Assert.AreEqual(bin[i], bytesRef.bytes[i + bytesRef.offset]);
		}
		reader.close();
		// force merge


		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMergePolicy(newLogMergePolicy()));
		writer.forceMerge(1);
		writer.close();
		reader = DirectoryReader.open(dir);
		doc2 = reader.document(reader.maxDoc() - 1);
		fields = doc2.getFields("bin1");
		Assert.IsNotNull(fields);
		Assert.AreEqual(1, fields.Length);
		b1 = fields[0];
		Assert.IsTrue(b1.binaryValue() != null);
		bytesRef = b1.binaryValue();
		Assert.AreEqual(bin.Length, bytesRef.length);
		for (int i = 0; i < bin.Length; i++)
		{
		  Assert.AreEqual(bin[i], bytesRef.bytes[i + bytesRef.offset]);
		}
		reader.close();
		dir.close();
	  }

	  /* ??? public void testOpenEmptyDirectory() throws IOException{
	    String dirName = "test.empty";
	    File fileDirName = new File(dirName);
	    if (!fileDirName.exists()) {
	      fileDirName.mkdir();
	    }
	    try {
	      DirectoryReader.open(fileDirName);
	      Assert.Fail("opening DirectoryReader on empty directory failed to produce FileNotFoundException/NoSuchFileException");
	    } catch (FileNotFoundException | NoSuchFileException e) {
	      // GOOD
	    }
	    rmDir(fileDirName);
	  }*/

	public virtual void TestFilesOpenClose()
	{
		  // Create initial data set
		  File dirFile = createTempDir("TestIndexReader.testFilesOpenClose");
		  Directory dir = newFSDirectory(dirFile);
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  AddDoc(writer, "test");
		  writer.close();
		  dir.close();

		  // Try to erase the data - this ensures that the writer closed all files
		  TestUtil.rm(dirFile);
		  dir = newFSDirectory(dirFile);

		  // Now create the data set again, just as before
		  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE));
		  AddDoc(writer, "test");
		  writer.close();
		  dir.close();

		  // Now open existing directory and test that reader closes all files
		  dir = newFSDirectory(dirFile);
		  DirectoryReader reader1 = DirectoryReader.open(dir);
		  reader1.close();
		  dir.close();

		  // The following will fail if reader did not close
		  // all files
		  TestUtil.rm(dirFile);
	}

	  public virtual void TestOpenReaderAfterDelete()
	  {
		File dirFile = createTempDir("deletetest");
		Directory dir = newFSDirectory(dirFile);
		try
		{
		  DirectoryReader.open(dir);
		  Assert.Fail("expected FileNotFoundException/NoSuchFileException");
		}
//JAVA TO C# CONVERTER TODO TASK: There is no equivalent in C# to Java 'multi-catch' syntax:
		catch (FileNotFoundException | NoSuchFileException e)
		{
		  // expected
		}

		dirFile.delete();

		// Make sure we still get a CorruptIndexException (not NPE):
		try
		{
		  DirectoryReader.open(dir);
		  Assert.Fail("expected FileNotFoundException/NoSuchFileException");
		}
//JAVA TO C# CONVERTER TODO TASK: There is no equivalent in C# to Java 'multi-catch' syntax:
		catch (FileNotFoundException | NoSuchFileException e)
		{
		  // expected
		}

		dir.close();
	  }

	  internal static void AddDocumentWithFields(IndexWriter writer)
	  {
		  Document doc = new Document();

		  FieldType customType3 = new FieldType();
		  customType3.Stored = true;
		  doc.add(newStringField("keyword", "test1", Field.Store.YES));
		  doc.add(newTextField("text", "test1", Field.Store.YES));
		  doc.add(newField("unindexed", "test1", customType3));
		  doc.add(new TextField("unstored","test1", Field.Store.NO));
		  writer.addDocument(doc);
	  }

	  internal static void AddDocumentWithDifferentFields(IndexWriter writer)
	  {
		Document doc = new Document();

		FieldType customType3 = new FieldType();
		customType3.Stored = true;
		doc.add(newStringField("keyword2", "test1", Field.Store.YES));
		doc.add(newTextField("text2", "test1", Field.Store.YES));
		doc.add(newField("unindexed2", "test1", customType3));
		doc.add(new TextField("unstored2","test1", Field.Store.NO));
		writer.addDocument(doc);
	  }

	  internal static void AddDocumentWithTermVectorFields(IndexWriter writer)
	  {
		  Document doc = new Document();
		  FieldType customType5 = new FieldType(TextField.TYPE_STORED);
		  customType5.StoreTermVectors = true;
		  FieldType customType6 = new FieldType(TextField.TYPE_STORED);
		  customType6.StoreTermVectors = true;
		  customType6.StoreTermVectorOffsets = true;
		  FieldType customType7 = new FieldType(TextField.TYPE_STORED);
		  customType7.StoreTermVectors = true;
		  customType7.StoreTermVectorPositions = true;
		  FieldType customType8 = new FieldType(TextField.TYPE_STORED);
		  customType8.StoreTermVectors = true;
		  customType8.StoreTermVectorOffsets = true;
		  customType8.StoreTermVectorPositions = true;
		  doc.add(newTextField("tvnot", "tvnot", Field.Store.YES));
		  doc.add(newField("termvector","termvector",customType5));
		  doc.add(newField("tvoffset","tvoffset", customType6));
		  doc.add(newField("tvposition","tvposition", customType7));
		  doc.add(newField("tvpositionoffset","tvpositionoffset", customType8));

		  writer.addDocument(doc);
	  }

	  internal static void AddDoc(IndexWriter writer, string value)
	  {
		  Document doc = new Document();
		  doc.add(newTextField("content", value, Field.Store.NO));
		  writer.addDocument(doc);
	  }

	  // TODO: maybe this can reuse the logic of test dueling codecs?
	  public static void AssertIndexEquals(DirectoryReader index1, DirectoryReader index2)
	  {
		Assert.AreEqual("IndexReaders have different values for numDocs.", index1.numDocs(), index2.numDocs());
		Assert.AreEqual("IndexReaders have different values for maxDoc.", index1.maxDoc(), index2.maxDoc());
		Assert.AreEqual("Only one IndexReader has deletions.", index1.hasDeletions(), index2.hasDeletions());
		Assert.AreEqual("Single segment test differs.", index1.leaves().size() == 1, index2.leaves().size() == 1);

		// check field names
		FieldInfos fieldInfos1 = MultiFields.getMergedFieldInfos(index1);
		FieldInfos fieldInfos2 = MultiFields.getMergedFieldInfos(index2);
		Assert.AreEqual("IndexReaders have different numbers of fields.", fieldInfos1.size(), fieldInfos2.size());
		int numFields = fieldInfos1.size();
		for (int fieldID = 0;fieldID < numFields;fieldID++)
		{
		  FieldInfo fieldInfo1 = fieldInfos1.fieldInfo(fieldID);
		  FieldInfo fieldInfo2 = fieldInfos2.fieldInfo(fieldID);
		  Assert.AreEqual("Different field names.", fieldInfo1.name, fieldInfo2.name);
		}

		// check norms
		foreach (FieldInfo fieldInfo in fieldInfos1)
		{
		  string curField = fieldInfo.name;
		  NumericDocValues norms1 = MultiDocValues.getNormValues(index1, curField);
		  NumericDocValues norms2 = MultiDocValues.getNormValues(index2, curField);
		  if (norms1 != null && norms2 != null)
		  {
			// todo: generalize this (like TestDuelingCodecs assert)
			for (int i = 0; i < index1.maxDoc(); i++)
			{
			  Assert.AreEqual("Norm different for doc " + i + " and field '" + curField + "'.", norms1.get(i), norms2.get(i));
			}
		  }
		  else
		  {
			assertNull(norms1);
			assertNull(norms2);
		  }
		}

		// check deletions
		Bits liveDocs1 = MultiFields.getLiveDocs(index1);
		Bits liveDocs2 = MultiFields.getLiveDocs(index2);
		for (int i = 0; i < index1.maxDoc(); i++)
		{
		  Assert.AreEqual("Doc " + i + " only deleted in one index.", liveDocs1 == null || !liveDocs1.get(i), liveDocs2 == null || !liveDocs2.get(i));
		}

		// check stored fields
		for (int i = 0; i < index1.maxDoc(); i++)
		{
		  if (liveDocs1 == null || liveDocs1.get(i))
		  {
			Document doc1 = index1.document(i);
			Document doc2 = index2.document(i);
			IList<IndexableField> field1 = doc1.Fields;
			IList<IndexableField> field2 = doc2.Fields;
			Assert.AreEqual("Different numbers of fields for doc " + i + ".", field1.Count, field2.Count);
			IEnumerator<IndexableField> itField1 = field1.GetEnumerator();
			IEnumerator<IndexableField> itField2 = field2.GetEnumerator();
			while (itField1.MoveNext())
			{
			  Field curField1 = (Field) itField1.Current;
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			  Field curField2 = (Field) itField2.next();
			  Assert.AreEqual("Different fields names for doc " + i + ".", curField1.name(), curField2.name());
			  Assert.AreEqual("Different field values for doc " + i + ".", curField1.stringValue(), curField2.stringValue());
			}
		  }
		}

		// check dictionary and posting lists
		Fields fields1 = MultiFields.getFields(index1);
		Fields fields2 = MultiFields.getFields(index2);
		IEnumerator<string> fenum2 = fields2.GetEnumerator();
		Bits liveDocs = MultiFields.getLiveDocs(index1);
		foreach (string field1 in fields1)
		{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  Assert.AreEqual("Different fields", field1, fenum2.next());
		  Terms terms1 = fields1.terms(field1);
		  if (terms1 == null)
		  {
			assertNull(fields2.terms(field1));
			continue;
		  }
		  TermsEnum enum1 = terms1.iterator(null);

		  Terms terms2 = fields2.terms(field1);
		  Assert.IsNotNull(terms2);
		  TermsEnum enum2 = terms2.iterator(null);

		  while (enum1.next() != null)
		  {
			Assert.AreEqual("Different terms", enum1.term(), enum2.next());
			DocsAndPositionsEnum tp1 = enum1.docsAndPositions(liveDocs, null);
			DocsAndPositionsEnum tp2 = enum2.docsAndPositions(liveDocs, null);

			while (tp1.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			{
			  Assert.IsTrue(tp2.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			  Assert.AreEqual("Different doc id in postinglist of term " + enum1.term() + ".", tp1.docID(), tp2.docID());
			  Assert.AreEqual("Different term frequence in postinglist of term " + enum1.term() + ".", tp1.freq(), tp2.freq());
			  for (int i = 0; i < tp1.freq(); i++)
			  {
				Assert.AreEqual("Different positions in postinglist of term " + enum1.term() + ".", tp1.nextPosition(), tp2.nextPosition());
			  }
			}
		  }
		}
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsFalse(fenum2.hasNext());
	  }

	  public virtual void TestGetIndexCommit()
	  {

		Directory d = newDirectory();

		// set up writer
		IndexWriter writer = new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy(10)));
		for (int i = 0;i < 27;i++)
		{
		  AddDocumentWithFields(writer);
		}
		writer.close();

		SegmentInfos sis = new SegmentInfos();
		sis.read(d);
		DirectoryReader r = DirectoryReader.open(d);
		IndexCommit c = r.IndexCommit;

		Assert.AreEqual(sis.SegmentsFileName, c.SegmentsFileName);

		Assert.IsTrue(c.Equals(r.IndexCommit));

		// Change the index
		writer = new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND).setMaxBufferedDocs(2).setMergePolicy(newLogMergePolicy(10)));
		for (int i = 0;i < 7;i++)
		{
		  AddDocumentWithFields(writer);
		}
		writer.close();

		DirectoryReader r2 = DirectoryReader.openIfChanged(r);
		Assert.IsNotNull(r2);
		Assert.IsFalse(c.Equals(r2.IndexCommit));
		Assert.IsFalse(r2.IndexCommit.SegmentCount == 1);
		r2.close();

		writer = new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.APPEND));
		writer.forceMerge(1);
		writer.close();

		r2 = DirectoryReader.openIfChanged(r);
		Assert.IsNotNull(r2);
		assertNull(DirectoryReader.openIfChanged(r2));
		Assert.AreEqual(1, r2.IndexCommit.SegmentCount);

		r.close();
		r2.close();
		d.close();
	  }

	  internal static Document CreateDocument(string id)
	  {
		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_STORED);
		customType.Tokenized = false;
		customType.OmitNorms = true;

		doc.add(newField("id", id, customType));
		return doc;
	  }

	  // LUCENE-1468 -- make sure on attempting to open an
	  // DirectoryReader on a non-existent directory, you get a
	  // good exception
	  public virtual void TestNoDir()
	  {
		File tempDir = createTempDir("doesnotexist");
		TestUtil.rm(tempDir);
		Directory dir = newFSDirectory(tempDir);
		try
		{
		  DirectoryReader.open(dir);
		  Assert.Fail("did not hit expected exception");
		}
		catch (NoSuchDirectoryException nsde)
		{
		  // expected
		}
		dir.close();
	  }

	  // LUCENE-1509
	  public virtual void TestNoDupCommitFileNames()
	  {

		Directory dir = newDirectory();

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2));
		writer.addDocument(CreateDocument("a"));
		writer.addDocument(CreateDocument("a"));
		writer.addDocument(CreateDocument("a"));
		writer.close();

		ICollection<IndexCommit> commits = DirectoryReader.listCommits(dir);
		foreach (IndexCommit commit in commits)
		{
		  ICollection<string> files = commit.FileNames;
		  HashSet<string> seen = new HashSet<string>();
		  foreach (String fileName in files)
		  {
			Assert.IsTrue("file " + fileName + " was duplicated", !seen.Contains(fileName));
			seen.Add(fileName);
		  }
		}

		dir.close();
	  }

	  // LUCENE-1579: Ensure that on a reopened reader, that any
	  // shared segments reuse the doc values arrays in
	  // FieldCache
	  public virtual void TestFieldCacheReuseAfterReopen()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy(10)));
		Document doc = new Document();
		doc.add(newStringField("number", "17", Field.Store.NO));
		writer.addDocument(doc);
		writer.commit();

		// Open reader1
		DirectoryReader r = DirectoryReader.open(dir);
		AtomicReader r1 = getOnlySegmentReader(r);
		FieldCache.Ints ints = FieldCache.DEFAULT.getInts(r1, "number", false);
		Assert.AreEqual(17, ints.get(0));

		// Add new segment
		writer.addDocument(doc);
		writer.commit();

		// Reopen reader1 --> reader2
		DirectoryReader r2 = DirectoryReader.openIfChanged(r);
		Assert.IsNotNull(r2);
		r.close();
		AtomicReader sub0 = r2.leaves().get(0).reader();
		FieldCache.Ints ints2 = FieldCache.DEFAULT.getInts(sub0, "number", false);
		r2.close();
		Assert.IsTrue(ints == ints2);

		writer.close();
		dir.close();
	  }

	  // LUCENE-1586: getUniqueTermCount
	  public virtual void TestUniqueTermCount()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		doc.add(newTextField("field", "a b c d e f g h i j k l m n o p q r s t u v w x y z", Field.Store.NO));
		doc.add(newTextField("number", "0 1 2 3 4 5 6 7 8 9", Field.Store.NO));
		writer.addDocument(doc);
		writer.addDocument(doc);
		writer.commit();

		DirectoryReader r = DirectoryReader.open(dir);
		AtomicReader r1 = getOnlySegmentReader(r);
		Assert.AreEqual(36, r1.fields().UniqueTermCount);
		writer.addDocument(doc);
		writer.commit();
		DirectoryReader r2 = DirectoryReader.openIfChanged(r);
		Assert.IsNotNull(r2);
		r.close();

		foreach (AtomicReaderContext s in r2.leaves())
		{
		  Assert.AreEqual(36, s.reader().fields().UniqueTermCount);
		}
		r2.close();
		writer.close();
		dir.close();
	  }

	  // LUCENE-1609: don't load terms index
	  public virtual void TestNoTermsIndex()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setCodec(TestUtil.alwaysPostingsFormat(new Lucene41PostingsFormat())));
		Document doc = new Document();
		doc.add(newTextField("field", "a b c d e f g h i j k l m n o p q r s t u v w x y z", Field.Store.NO));
		doc.add(newTextField("number", "0 1 2 3 4 5 6 7 8 9", Field.Store.NO));
		writer.addDocument(doc);
		writer.addDocument(doc);
		writer.close();

		DirectoryReader r = DirectoryReader.open(dir, -1);
		try
		{
		  r.docFreq(new Term("field", "f"));
		  Assert.Fail("did not hit expected exception");
		}
		catch (IllegalStateException ise)
		{
		  // expected
		}

		Assert.AreEqual(-1, ((SegmentReader) r.leaves().get(0).reader()).TermInfosIndexDivisor);
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setCodec(TestUtil.alwaysPostingsFormat(new Lucene41PostingsFormat())).setMergePolicy(newLogMergePolicy(10)));
		writer.addDocument(doc);
		writer.close();

		// LUCENE-1718: ensure re-open carries over no terms index:
		DirectoryReader r2 = DirectoryReader.openIfChanged(r);
		Assert.IsNotNull(r2);
		assertNull(DirectoryReader.openIfChanged(r2));
		r.close();
		IList<AtomicReaderContext> leaves = r2.leaves();
		Assert.AreEqual(2, leaves.Count);
		foreach (AtomicReaderContext ctx in leaves)
		{
		  try
		  {
			ctx.reader().docFreq(new Term("field", "f"));
			Assert.Fail("did not hit expected exception");
		  }
		  catch (IllegalStateException ise)
		  {
			// expected
		  }
		}
		r2.close();
		dir.close();
	  }

	  // LUCENE-2046
	  public virtual void TestPrepareCommitIsCurrent()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		writer.commit();
		Document doc = new Document();
		writer.addDocument(doc);
		DirectoryReader r = DirectoryReader.open(dir);
		Assert.IsTrue(r.Current);
		writer.addDocument(doc);
		writer.prepareCommit();
		Assert.IsTrue(r.Current);
		DirectoryReader r2 = DirectoryReader.openIfChanged(r);
		assertNull(r2);
		writer.commit();
		Assert.IsFalse(r.Current);
		writer.close();
		r.close();
		dir.close();
	  }

	  // LUCENE-2753
	  public virtual void TestListCommits()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, null).setIndexDeletionPolicy(new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy())));
		SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy) writer.Config.IndexDeletionPolicy;
		writer.addDocument(new Document());
		writer.commit();
		sdp.snapshot();
		writer.addDocument(new Document());
		writer.commit();
		sdp.snapshot();
		writer.addDocument(new Document());
		writer.commit();
		sdp.snapshot();
		writer.close();
		long currentGen = 0;
		foreach (IndexCommit ic in DirectoryReader.listCommits(dir))
		{
		  Assert.IsTrue("currentGen=" + currentGen + " commitGen=" + ic.Generation, currentGen < ic.Generation);
		  currentGen = ic.Generation;
		}
		dir.close();
	  }

	  // Make sure totalTermFreq works correctly in the terms
	  // dict cache
	  public virtual void TestTotalTermFreqCached()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document d = new Document();
		d.add(newTextField("f", "a a b", Field.Store.NO));
		writer.addDocument(d);
		DirectoryReader r = writer.Reader;
		writer.close();
		try
		{
		  // Make sure codec impls totalTermFreq (eg PreFlex doesn't)
		  Assume.assumeTrue(r.totalTermFreq(new Term("f", new BytesRef("b"))) != -1);
		  Assert.AreEqual(1, r.totalTermFreq(new Term("f", new BytesRef("b"))));
		  Assert.AreEqual(2, r.totalTermFreq(new Term("f", new BytesRef("a"))));
		  Assert.AreEqual(1, r.totalTermFreq(new Term("f", new BytesRef("b"))));
		}
		finally
		{
		  r.close();
		  dir.close();
		}
	  }

	  public virtual void TestGetSumDocFreq()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document d = new Document();
		d.add(newTextField("f", "a", Field.Store.NO));
		writer.addDocument(d);
		d = new Document();
		d.add(newTextField("f", "b", Field.Store.NO));
		writer.addDocument(d);
		DirectoryReader r = writer.Reader;
		writer.close();
		try
		{
		  // Make sure codec impls getSumDocFreq (eg PreFlex doesn't)
		  Assume.assumeTrue(r.getSumDocFreq("f") != -1);
		  Assert.AreEqual(2, r.getSumDocFreq("f"));
		}
		finally
		{
		  r.close();
		  dir.close();
		}
	  }

	  public virtual void TestGetDocCount()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document d = new Document();
		d.add(newTextField("f", "a", Field.Store.NO));
		writer.addDocument(d);
		d = new Document();
		d.add(newTextField("f", "a", Field.Store.NO));
		writer.addDocument(d);
		DirectoryReader r = writer.Reader;
		writer.close();
		try
		{
		  // Make sure codec impls getSumDocFreq (eg PreFlex doesn't)
		  Assume.assumeTrue(r.getDocCount("f") != -1);
		  Assert.AreEqual(2, r.getDocCount("f"));
		}
		finally
		{
		  r.close();
		  dir.close();
		}
	  }

	  public virtual void TestGetSumTotalTermFreq()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document d = new Document();
		d.add(newTextField("f", "a b b", Field.Store.NO));
		writer.addDocument(d);
		d = new Document();
		d.add(newTextField("f", "a a b", Field.Store.NO));
		writer.addDocument(d);
		DirectoryReader r = writer.Reader;
		writer.close();
		try
		{
		  // Make sure codec impls getSumDocFreq (eg PreFlex doesn't)
		  Assume.assumeTrue(r.getSumTotalTermFreq("f") != -1);
		  Assert.AreEqual(6, r.getSumTotalTermFreq("f"));
		}
		finally
		{
		  r.close();
		  dir.close();
		}
	  }

	  // LUCENE-2474
	  public virtual void TestReaderFinishedListener()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy()));
		((LogMergePolicy) writer.Config.MergePolicy).MergeFactor = 3;
		writer.addDocument(new Document());
		writer.commit();
		writer.addDocument(new Document());
		writer.commit();
		DirectoryReader reader = writer.Reader;
		int[] closeCount = new int[1];
		IndexReader.ReaderClosedListener listener = new ReaderClosedListenerAnonymousInnerClassHelper(this, reader, closeCount);

		reader.addReaderClosedListener(listener);

		reader.close();

		// Close the top reader, its the only one that should be closed
		Assert.AreEqual(1, closeCount[0]);
		writer.close();

		DirectoryReader reader2 = DirectoryReader.open(dir);
		reader2.addReaderClosedListener(listener);

		closeCount[0] = 0;
		reader2.close();
		Assert.AreEqual(1, closeCount[0]);
		dir.close();
	  }

	  private class ReaderClosedListenerAnonymousInnerClassHelper : IndexReader.ReaderClosedListener
	  {
		  private readonly TestDirectoryReader OuterInstance;

		  private DirectoryReader Reader;
		  private int[] CloseCount;

		  public ReaderClosedListenerAnonymousInnerClassHelper(TestDirectoryReader outerInstance, DirectoryReader reader, int[] closeCount)
		  {
			  this.OuterInstance = outerInstance;
			  this.Reader = reader;
			  this.CloseCount = closeCount;
		  }

		  public override void OnClose(IndexReader reader)
		  {
			CloseCount[0]++;
		  }
	  }

	  public virtual void TestOOBDocID()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		writer.addDocument(new Document());
		DirectoryReader r = writer.Reader;
		writer.close();
		r.document(0);
		try
		{
		  r.document(1);
		  Assert.Fail("did not hit exception");
		}
		catch (System.ArgumentException iae)
		{
		  // expected
		}
		r.close();
		dir.close();
	  }

	  public virtual void TestTryIncRef()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		writer.addDocument(new Document());
		writer.commit();
		DirectoryReader r = DirectoryReader.open(dir);
		Assert.IsTrue(r.tryIncRef());
		r.decRef();
		r.close();
		Assert.IsFalse(r.tryIncRef());
		writer.close();
		dir.close();
	  }

	  public virtual void TestStressTryIncRef()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		writer.addDocument(new Document());
		writer.commit();
		DirectoryReader r = DirectoryReader.open(dir);
		int numThreads = atLeast(2);

		IncThread[] threads = new IncThread[numThreads];
		for (int i = 0; i < threads.Length; i++)
		{
		  threads[i] = new IncThread(r, random());
		  threads[i].Start();
		}
		Thread.Sleep(100);

		Assert.IsTrue(r.tryIncRef());
		r.decRef();
		r.close();

		for (int i = 0; i < threads.Length; i++)
		{
		  threads[i].Join();
		  assertNull(threads[i].Failed);
		}
		Assert.IsFalse(r.tryIncRef());
		writer.close();
		dir.close();
	  }

	  internal class IncThread : System.Threading.Thread
	  {
		internal readonly IndexReader ToInc;
		internal readonly Random Random;
		internal Exception Failed;

		internal IncThread(IndexReader toInc, Random random)
		{
		  this.ToInc = toInc;
		  this.Random = random;
		}

		public override void Run()
		{
		  try
		  {
			while (ToInc.tryIncRef())
			{
			  Assert.IsFalse(ToInc.hasDeletions());
			  ToInc.decRef();
			}
			Assert.IsFalse(ToInc.tryIncRef());
		  }
		  catch (Exception e)
		  {
			Failed = e;
		  }
		}
	  }

	  public virtual void TestLoadCertainFields()
	  {
		Directory dir = newDirectory();
		RandomIndexWriter writer = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		doc.add(newStringField("field1", "foobar", Field.Store.YES));
		doc.add(newStringField("field2", "foobaz", Field.Store.YES));
		writer.addDocument(doc);
		DirectoryReader r = writer.Reader;
		writer.close();
		Set<string> fieldsToLoad = new HashSet<string>();
		Assert.AreEqual(0, r.document(0, fieldsToLoad).Fields.size());
		fieldsToLoad.add("field1");
		Document doc2 = r.document(0, fieldsToLoad);
		Assert.AreEqual(1, doc2.Fields.size());
		Assert.AreEqual("foobar", doc2.get("field1"));
		r.close();
		dir.close();
	  }

	  /// @deprecated just to ensure IndexReader static methods work 
	  [Obsolete("just to ensure IndexReader static methods work")]
	  public virtual void TestBackwards()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setCodec(TestUtil.alwaysPostingsFormat(new Lucene41PostingsFormat())));
		Document doc = new Document();
		doc.add(newTextField("field", "a b c d e f g h i j k l m n o p q r s t u v w x y z", Field.Store.NO));
		doc.add(newTextField("number", "0 1 2 3 4 5 6 7 8 9", Field.Store.NO));
		writer.addDocument(doc);

		// open(IndexWriter, boolean)
		DirectoryReader r = IndexReader.open(writer, true);
		Assert.AreEqual(1, r.docFreq(new Term("field", "f")));
		r.close();
		writer.addDocument(doc);
		writer.close();

		// open(Directory)
		r = IndexReader.open(dir);
		Assert.AreEqual(2, r.docFreq(new Term("field", "f")));
		r.close();

		// open(IndexCommit)
		IList<IndexCommit> commits = DirectoryReader.listCommits(dir);
		Assert.AreEqual(1, commits.Count);
		r = IndexReader.open(commits[0]);
		Assert.AreEqual(2, r.docFreq(new Term("field", "f")));
		r.close();

		// open(Directory, int)
		r = IndexReader.open(dir, -1);
		try
		{
		  r.docFreq(new Term("field", "f"));
		  Assert.Fail("did not hit expected exception");
		}
		catch (IllegalStateException ise)
		{
		  // expected
		}
		Assert.AreEqual(-1, ((SegmentReader) r.leaves().get(0).reader()).TermInfosIndexDivisor);
		r.close();

		// open(IndexCommit, int)
		r = IndexReader.open(commits[0], -1);
		try
		{
		  r.docFreq(new Term("field", "f"));
		  Assert.Fail("did not hit expected exception");
		}
		catch (IllegalStateException ise)
		{
		  // expected
		}
		Assert.AreEqual(-1, ((SegmentReader) r.leaves().get(0).reader()).TermInfosIndexDivisor);
		r.close();
		dir.close();
	  }

	  public virtual void TestIndexExistsOnNonExistentDirectory()
	  {
		File tempDir = createTempDir("testIndexExistsOnNonExistentDirectory");
		tempDir.delete();
		Directory dir = newFSDirectory(tempDir);
		Console.WriteLine("dir=" + dir);
		Assert.IsFalse(DirectoryReader.indexExists(dir));
		dir.close();
	  }
	}

}