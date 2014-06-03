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

	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using CachingTokenFilter = Lucene.Net.Analysis.CachingTokenFilter;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using MockTokenFilter = Lucene.Net.Analysis.MockTokenFilter;
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using TokenStream = Lucene.Net.Analysis.TokenStream;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using Directory = Lucene.Net.Store.Directory;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	/// <summary>
	/// tests for writing term vectors </summary>
	public class TestTermVectorsWriter : LuceneTestCase
	{
	  // LUCENE-1442
	  public virtual void TestDoubleOffsetCounting()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorOffsets = true;
		Field f = newField("field", "abcd", customType);
		doc.add(f);
		doc.add(f);
		Field f2 = newField("field", "", customType);
		doc.add(f2);
		doc.add(f);
		w.addDocument(doc);
		w.close();

		IndexReader r = DirectoryReader.open(dir);
		Terms vector = r.getTermVectors(0).terms("field");
		Assert.IsNotNull(vector);
		TermsEnum termsEnum = vector.iterator(null);
		Assert.IsNotNull(termsEnum.next());
		Assert.AreEqual("", termsEnum.term().utf8ToString());

		// Token "" occurred once
		Assert.AreEqual(1, termsEnum.totalTermFreq());

		DocsAndPositionsEnum dpEnum = termsEnum.docsAndPositions(null, null);
		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		dpEnum.nextPosition();
		Assert.AreEqual(8, dpEnum.StartOffset());
		Assert.AreEqual(8, dpEnum.EndOffset());
		Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.nextDoc());

		// Token "abcd" occurred three times
		Assert.AreEqual(new BytesRef("abcd"), termsEnum.next());
		dpEnum = termsEnum.docsAndPositions(null, dpEnum);
		Assert.AreEqual(3, termsEnum.totalTermFreq());

		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		dpEnum.nextPosition();
		Assert.AreEqual(0, dpEnum.StartOffset());
		Assert.AreEqual(4, dpEnum.EndOffset());

		dpEnum.nextPosition();
		Assert.AreEqual(4, dpEnum.StartOffset());
		Assert.AreEqual(8, dpEnum.EndOffset());

		dpEnum.nextPosition();
		Assert.AreEqual(8, dpEnum.StartOffset());
		Assert.AreEqual(12, dpEnum.EndOffset());

		Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.nextDoc());
		assertNull(termsEnum.next());
		r.close();
		dir.close();
	  }

	  // LUCENE-1442
	  public virtual void TestDoubleOffsetCounting2()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorOffsets = true;
		Field f = newField("field", "abcd", customType);
		doc.add(f);
		doc.add(f);
		w.addDocument(doc);
		w.close();

		IndexReader r = DirectoryReader.open(dir);
		TermsEnum termsEnum = r.getTermVectors(0).terms("field").iterator(null);
		Assert.IsNotNull(termsEnum.next());
		DocsAndPositionsEnum dpEnum = termsEnum.docsAndPositions(null, null);
		Assert.AreEqual(2, termsEnum.totalTermFreq());

		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		dpEnum.nextPosition();
		Assert.AreEqual(0, dpEnum.StartOffset());
		Assert.AreEqual(4, dpEnum.EndOffset());

		dpEnum.nextPosition();
		Assert.AreEqual(5, dpEnum.StartOffset());
		Assert.AreEqual(9, dpEnum.EndOffset());
		Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.nextDoc());

		r.close();
		dir.close();
	  }

	  // LUCENE-1448
	  public virtual void TestEndOffsetPositionCharAnalyzer()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorOffsets = true;
		Field f = newField("field", "abcd   ", customType);
		doc.add(f);
		doc.add(f);
		w.addDocument(doc);
		w.close();

		IndexReader r = DirectoryReader.open(dir);
		TermsEnum termsEnum = r.getTermVectors(0).terms("field").iterator(null);
		Assert.IsNotNull(termsEnum.next());
		DocsAndPositionsEnum dpEnum = termsEnum.docsAndPositions(null, null);
		Assert.AreEqual(2, termsEnum.totalTermFreq());

		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		dpEnum.nextPosition();
		Assert.AreEqual(0, dpEnum.StartOffset());
		Assert.AreEqual(4, dpEnum.EndOffset());

		dpEnum.nextPosition();
		Assert.AreEqual(8, dpEnum.StartOffset());
		Assert.AreEqual(12, dpEnum.EndOffset());
		Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.nextDoc());

		r.close();
		dir.close();
	  }

	  // LUCENE-1448
	  public virtual void TestEndOffsetPositionWithCachingTokenFilter()
	  {
		Directory dir = newDirectory();
		Analyzer analyzer = new MockAnalyzer(random());
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
		Document doc = new Document();
		IOException priorException = null;
		TokenStream stream = analyzer.tokenStream("field", "abcd   ");
		try
		{
		  stream.reset(); // TODO: weird to reset before wrapping with CachingTokenFilter... correct?
		  TokenStream cachedStream = new CachingTokenFilter(stream);
		  FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		  customType.StoreTermVectors = true;
		  customType.StoreTermVectorPositions = true;
		  customType.StoreTermVectorOffsets = true;
		  Field f = new Field("field", cachedStream, customType);
		  doc.add(f);
		  doc.add(f);
		  w.addDocument(doc);
		}
		catch (IOException e)
		{
		  priorException = e;
		}
		finally
		{
		  IOUtils.CloseWhileHandlingException(priorException, stream);
		}
		w.close();

		IndexReader r = DirectoryReader.open(dir);
		TermsEnum termsEnum = r.getTermVectors(0).terms("field").iterator(null);
		Assert.IsNotNull(termsEnum.next());
		DocsAndPositionsEnum dpEnum = termsEnum.docsAndPositions(null, null);
		Assert.AreEqual(2, termsEnum.totalTermFreq());

		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		dpEnum.nextPosition();
		Assert.AreEqual(0, dpEnum.StartOffset());
		Assert.AreEqual(4, dpEnum.EndOffset());

		dpEnum.nextPosition();
		Assert.AreEqual(8, dpEnum.StartOffset());
		Assert.AreEqual(12, dpEnum.EndOffset());
		Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.nextDoc());

		r.close();
		dir.close();
	  }

	  // LUCENE-1448
	  public virtual void TestEndOffsetPositionStopFilter()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random(), MockTokenizer.SIMPLE, true, MockTokenFilter.ENGLISH_STOPSET)));
		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorOffsets = true;
		Field f = newField("field", "abcd the", customType);
		doc.add(f);
		doc.add(f);
		w.addDocument(doc);
		w.close();

		IndexReader r = DirectoryReader.open(dir);
		TermsEnum termsEnum = r.getTermVectors(0).terms("field").iterator(null);
		Assert.IsNotNull(termsEnum.next());
		DocsAndPositionsEnum dpEnum = termsEnum.docsAndPositions(null, null);
		Assert.AreEqual(2, termsEnum.totalTermFreq());

		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		dpEnum.nextPosition();
		Assert.AreEqual(0, dpEnum.StartOffset());
		Assert.AreEqual(4, dpEnum.EndOffset());

		dpEnum.nextPosition();
		Assert.AreEqual(9, dpEnum.StartOffset());
		Assert.AreEqual(13, dpEnum.EndOffset());
		Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, dpEnum.nextDoc());

		r.close();
		dir.close();
	  }

	  // LUCENE-1448
	  public virtual void TestEndOffsetPositionStandard()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorOffsets = true;
		Field f = newField("field", "abcd the  ", customType);
		Field f2 = newField("field", "crunch man", customType);
		doc.add(f);
		doc.add(f2);
		w.addDocument(doc);
		w.close();

		IndexReader r = DirectoryReader.open(dir);
		TermsEnum termsEnum = r.getTermVectors(0).terms("field").iterator(null);
		Assert.IsNotNull(termsEnum.next());
		DocsAndPositionsEnum dpEnum = termsEnum.docsAndPositions(null, null);

		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		dpEnum.nextPosition();
		Assert.AreEqual(0, dpEnum.StartOffset());
		Assert.AreEqual(4, dpEnum.EndOffset());

		Assert.IsNotNull(termsEnum.next());
		dpEnum = termsEnum.docsAndPositions(null, dpEnum);
		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		dpEnum.nextPosition();
		Assert.AreEqual(11, dpEnum.StartOffset());
		Assert.AreEqual(17, dpEnum.EndOffset());

		Assert.IsNotNull(termsEnum.next());
		dpEnum = termsEnum.docsAndPositions(null, dpEnum);
		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		dpEnum.nextPosition();
		Assert.AreEqual(18, dpEnum.StartOffset());
		Assert.AreEqual(21, dpEnum.EndOffset());

		r.close();
		dir.close();
	  }

	  // LUCENE-1448
	  public virtual void TestEndOffsetPositionStandardEmptyField()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorOffsets = true;
		Field f = newField("field", "", customType);
		Field f2 = newField("field", "crunch man", customType);
		doc.add(f);
		doc.add(f2);
		w.addDocument(doc);
		w.close();

		IndexReader r = DirectoryReader.open(dir);
		TermsEnum termsEnum = r.getTermVectors(0).terms("field").iterator(null);
		Assert.IsNotNull(termsEnum.next());
		DocsAndPositionsEnum dpEnum = termsEnum.docsAndPositions(null, null);

		Assert.AreEqual(1, (int) termsEnum.totalTermFreq());
		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		dpEnum.nextPosition();
		Assert.AreEqual(1, dpEnum.StartOffset());
		Assert.AreEqual(7, dpEnum.EndOffset());

		Assert.IsNotNull(termsEnum.next());
		dpEnum = termsEnum.docsAndPositions(null, dpEnum);
		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		dpEnum.nextPosition();
		Assert.AreEqual(8, dpEnum.StartOffset());
		Assert.AreEqual(11, dpEnum.EndOffset());

		r.close();
		dir.close();
	  }

	  // LUCENE-1448
	  public virtual void TestEndOffsetPositionStandardEmptyField2()
	  {
		Directory dir = newDirectory();
		IndexWriter w = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorOffsets = true;

		Field f = newField("field", "abcd", customType);
		doc.add(f);
		doc.add(newField("field", "", customType));

		Field f2 = newField("field", "crunch", customType);
		doc.add(f2);

		w.addDocument(doc);
		w.close();

		IndexReader r = DirectoryReader.open(dir);
		TermsEnum termsEnum = r.getTermVectors(0).terms("field").iterator(null);
		Assert.IsNotNull(termsEnum.next());
		DocsAndPositionsEnum dpEnum = termsEnum.docsAndPositions(null, null);

		Assert.AreEqual(1, (int) termsEnum.totalTermFreq());
		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		dpEnum.nextPosition();
		Assert.AreEqual(0, dpEnum.StartOffset());
		Assert.AreEqual(4, dpEnum.EndOffset());

		Assert.IsNotNull(termsEnum.next());
		dpEnum = termsEnum.docsAndPositions(null, dpEnum);
		Assert.IsTrue(dpEnum.nextDoc() != DocIdSetIterator.NO_MORE_DOCS);
		dpEnum.nextPosition();
		Assert.AreEqual(6, dpEnum.StartOffset());
		Assert.AreEqual(12, dpEnum.EndOffset());


		r.close();
		dir.close();
	  }

	  // LUCENE-1168
	  public virtual void TestTermVectorCorruption()
	  {

		Directory dir = newDirectory();
		for (int iter = 0;iter < 2;iter++)
		{
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).setMergeScheduler(new SerialMergeScheduler()).setMergePolicy(new LogDocMergePolicy()));

		  Document document = new Document();
		  FieldType customType = new FieldType();
		  customType.Stored = true;

		  Field storedField = newField("stored", "stored", customType);
		  document.add(storedField);
		  writer.addDocument(document);
		  writer.addDocument(document);

		  document = new Document();
		  document.add(storedField);
		  FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
		  customType2.StoreTermVectors = true;
		  customType2.StoreTermVectorPositions = true;
		  customType2.StoreTermVectorOffsets = true;
		  Field termVectorField = newField("termVector", "termVector", customType2);

		  document.add(termVectorField);
		  writer.addDocument(document);
		  writer.forceMerge(1);
		  writer.close();

		  IndexReader reader = DirectoryReader.open(dir);
		  for (int i = 0;i < reader.numDocs();i++)
		  {
			reader.document(i);
			reader.getTermVectors(i);
		  }
		  reader.close();

		  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).setMergeScheduler(new SerialMergeScheduler()).setMergePolicy(new LogDocMergePolicy()));

		  Directory[] indexDirs = new Directory[] {new MockDirectoryWrapper(random(), new RAMDirectory(dir, newIOContext(random())))};
		  writer.addIndexes(indexDirs);
		  writer.forceMerge(1);
		  writer.close();
		}
		dir.close();
	  }

	  // LUCENE-1168
	  public virtual void TestTermVectorCorruption2()
	  {
		Directory dir = newDirectory();
		for (int iter = 0;iter < 2;iter++)
		{
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).setMergeScheduler(new SerialMergeScheduler()).setMergePolicy(new LogDocMergePolicy()));

		  Document document = new Document();

		  FieldType customType = new FieldType();
		  customType.Stored = true;

		  Field storedField = newField("stored", "stored", customType);
		  document.add(storedField);
		  writer.addDocument(document);
		  writer.addDocument(document);

		  document = new Document();
		  document.add(storedField);
		  FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
		  customType2.StoreTermVectors = true;
		  customType2.StoreTermVectorPositions = true;
		  customType2.StoreTermVectorOffsets = true;
		  Field termVectorField = newField("termVector", "termVector", customType2);
		  document.add(termVectorField);
		  writer.addDocument(document);
		  writer.forceMerge(1);
		  writer.close();

		  IndexReader reader = DirectoryReader.open(dir);
		  assertNull(reader.getTermVectors(0));
		  assertNull(reader.getTermVectors(1));
		  Assert.IsNotNull(reader.getTermVectors(2));
		  reader.close();
		}
		dir.close();
	  }

	  // LUCENE-1168
	  public virtual void TestTermVectorCorruption3()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).setMergeScheduler(new SerialMergeScheduler()).setMergePolicy(new LogDocMergePolicy()));

		Document document = new Document();
		FieldType customType = new FieldType();
		customType.Stored = true;

		Field storedField = newField("stored", "stored", customType);
		document.add(storedField);
		FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
		customType2.StoreTermVectors = true;
		customType2.StoreTermVectorPositions = true;
		customType2.StoreTermVectorOffsets = true;
		Field termVectorField = newField("termVector", "termVector", customType2);
		document.add(termVectorField);
		for (int i = 0;i < 10;i++)
		{
		  writer.addDocument(document);
		}
		writer.close();

		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH).setMergeScheduler(new SerialMergeScheduler()).setMergePolicy(new LogDocMergePolicy()));
		for (int i = 0;i < 6;i++)
		{
		  writer.addDocument(document);
		}

		writer.forceMerge(1);
		writer.close();

		IndexReader reader = DirectoryReader.open(dir);
		for (int i = 0;i < 10;i++)
		{
		  reader.getTermVectors(i);
		  reader.document(i);
		}
		reader.close();
		dir.close();
	  }

	  // LUCENE-1008
	  public virtual void TestNoTermVectorAfterTermVector()
	  {
		Directory dir = newDirectory();
		IndexWriter iw = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document document = new Document();
		FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
		customType2.StoreTermVectors = true;
		customType2.StoreTermVectorPositions = true;
		customType2.StoreTermVectorOffsets = true;
		document.add(newField("tvtest", "a b c", customType2));
		iw.addDocument(document);
		document = new Document();
		document.add(newTextField("tvtest", "x y z", Field.Store.NO));
		iw.addDocument(document);
		// Make first segment
		iw.commit();

		FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;
		document.add(newField("tvtest", "a b c", customType));
		iw.addDocument(document);
		// Make 2nd segment
		iw.commit();

		iw.forceMerge(1);
		iw.close();
		dir.close();
	  }

	  // LUCENE-1010
	  public virtual void TestNoTermVectorAfterTermVectorMerge()
	  {
		Directory dir = newDirectory();
		IndexWriter iw = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		Document document = new Document();
		FieldType customType = new FieldType(StringField.TYPE_NOT_STORED);
		customType.StoreTermVectors = true;
		document.add(newField("tvtest", "a b c", customType));
		iw.addDocument(document);
		iw.commit();

		document = new Document();
		document.add(newTextField("tvtest", "x y z", Field.Store.NO));
		iw.addDocument(document);
		// Make first segment
		iw.commit();

		iw.forceMerge(1);

		FieldType customType2 = new FieldType(StringField.TYPE_NOT_STORED);
		customType2.StoreTermVectors = true;
		document.add(newField("tvtest", "a b c", customType2));
		iw.addDocument(document);
		// Make 2nd segment
		iw.commit();
		iw.forceMerge(1);

		iw.close();
		dir.close();
	  }
	}

}