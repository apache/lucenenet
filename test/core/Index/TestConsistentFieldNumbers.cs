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
	using StoredField = Lucene.Net.Document.StoredField;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using Directory = Lucene.Net.Store.Directory;
	using FailOnNonBulkMergesInfoStream = Lucene.Net.Util.FailOnNonBulkMergesInfoStream;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using Test = org.junit.Test;

	public class TestConsistentFieldNumbers : LuceneTestCase
	{

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSameFieldNumbersAcrossSegments() throws Exception
	  public virtual void TestSameFieldNumbersAcrossSegments()
	  {
		for (int i = 0; i < 2; i++)
		{
		  Directory dir = newDirectory();
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(NoMergePolicy.COMPOUND_FILES));

		  Document d1 = new Document();
		  d1.add(new StringField("f1", "first field", Field.Store.YES));
		  d1.add(new StringField("f2", "second field", Field.Store.YES));
		  writer.addDocument(d1);

		  if (i == 1)
		  {
			writer.close();
			writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(NoMergePolicy.COMPOUND_FILES));
		  }
		  else
		  {
			writer.commit();
		  }

		  Document d2 = new Document();
		  FieldType customType2 = new FieldType(TextField.TYPE_STORED);
		  customType2.StoreTermVectors = true;
		  d2.add(new TextField("f2", "second field", Field.Store.NO));
		  d2.add(new Field("f1", "first field", customType2));
		  d2.add(new TextField("f3", "third field", Field.Store.NO));
		  d2.add(new TextField("f4", "fourth field", Field.Store.NO));
		  writer.addDocument(d2);

		  writer.close();

		  SegmentInfos sis = new SegmentInfos();
		  sis.read(dir);
		  Assert.AreEqual(2, sis.size());

		  FieldInfos fis1 = SegmentReader.readFieldInfos(sis.info(0));
		  FieldInfos fis2 = SegmentReader.readFieldInfos(sis.info(1));

		  Assert.AreEqual("f1", fis1.fieldInfo(0).name);
		  Assert.AreEqual("f2", fis1.fieldInfo(1).name);
		  Assert.AreEqual("f1", fis2.fieldInfo(0).name);
		  Assert.AreEqual("f2", fis2.fieldInfo(1).name);
		  Assert.AreEqual("f3", fis2.fieldInfo(2).name);
		  Assert.AreEqual("f4", fis2.fieldInfo(3).name);

		  writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  writer.forceMerge(1);
		  writer.close();

		  sis = new SegmentInfos();
		  sis.read(dir);
		  Assert.AreEqual(1, sis.size());

		  FieldInfos fis3 = SegmentReader.readFieldInfos(sis.info(0));

		  Assert.AreEqual("f1", fis3.fieldInfo(0).name);
		  Assert.AreEqual("f2", fis3.fieldInfo(1).name);
		  Assert.AreEqual("f3", fis3.fieldInfo(2).name);
		  Assert.AreEqual("f4", fis3.fieldInfo(3).name);


		  dir.close();
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testAddIndexes() throws Exception
	  public virtual void TestAddIndexes()
	  {
		Directory dir1 = newDirectory();
		Directory dir2 = newDirectory();
		IndexWriter writer = new IndexWriter(dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(NoMergePolicy.COMPOUND_FILES));

		Document d1 = new Document();
		d1.add(new TextField("f1", "first field", Field.Store.YES));
		d1.add(new TextField("f2", "second field", Field.Store.YES));
		writer.addDocument(d1);

		writer.close();
		writer = new IndexWriter(dir2, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(NoMergePolicy.COMPOUND_FILES));

		Document d2 = new Document();
		FieldType customType2 = new FieldType(TextField.TYPE_STORED);
		customType2.StoreTermVectors = true;
		d2.add(new TextField("f2", "second field", Field.Store.YES));
		d2.add(new Field("f1", "first field", customType2));
		d2.add(new TextField("f3", "third field", Field.Store.YES));
		d2.add(new TextField("f4", "fourth field", Field.Store.YES));
		writer.addDocument(d2);

		writer.close();

		writer = new IndexWriter(dir1, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(NoMergePolicy.COMPOUND_FILES));
		writer.addIndexes(dir2);
		writer.close();

		SegmentInfos sis = new SegmentInfos();
		sis.read(dir1);
		Assert.AreEqual(2, sis.size());

		FieldInfos fis1 = SegmentReader.readFieldInfos(sis.info(0));
		FieldInfos fis2 = SegmentReader.readFieldInfos(sis.info(1));

		Assert.AreEqual("f1", fis1.fieldInfo(0).name);
		Assert.AreEqual("f2", fis1.fieldInfo(1).name);
		// make sure the ordering of the "external" segment is preserved
		Assert.AreEqual("f2", fis2.fieldInfo(0).name);
		Assert.AreEqual("f1", fis2.fieldInfo(1).name);
		Assert.AreEqual("f3", fis2.fieldInfo(2).name);
		Assert.AreEqual("f4", fis2.fieldInfo(3).name);

		dir1.close();
		dir2.close();
	  }

	  public virtual void TestFieldNumberGaps()
	  {
		int numIters = atLeast(13);
		for (int i = 0; i < numIters; i++)
		{
		  Directory dir = newDirectory();
		  {
			IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(NoMergePolicy.NO_COMPOUND_FILES));
			Document d = new Document();
			d.add(new TextField("f1", "d1 first field", Field.Store.YES));
			d.add(new TextField("f2", "d1 second field", Field.Store.YES));
			writer.addDocument(d);
			writer.close();
			SegmentInfos sis = new SegmentInfos();
			sis.read(dir);
			Assert.AreEqual(1, sis.size());
			FieldInfos fis1 = SegmentReader.readFieldInfos(sis.info(0));
			Assert.AreEqual("f1", fis1.fieldInfo(0).name);
			Assert.AreEqual("f2", fis1.fieldInfo(1).name);
		  }


		  {
			IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(random().nextBoolean() ? NoMergePolicy.NO_COMPOUND_FILES : NoMergePolicy.COMPOUND_FILES));
			Document d = new Document();
			d.add(new TextField("f1", "d2 first field", Field.Store.YES));
			d.add(new StoredField("f3", new sbyte[] {1, 2, 3}));
			writer.addDocument(d);
			writer.close();
			SegmentInfos sis = new SegmentInfos();
			sis.read(dir);
			Assert.AreEqual(2, sis.size());
			FieldInfos fis1 = SegmentReader.readFieldInfos(sis.info(0));
			FieldInfos fis2 = SegmentReader.readFieldInfos(sis.info(1));
			Assert.AreEqual("f1", fis1.fieldInfo(0).name);
			Assert.AreEqual("f2", fis1.fieldInfo(1).name);
			Assert.AreEqual("f1", fis2.fieldInfo(0).name);
			assertNull(fis2.fieldInfo(1));
			Assert.AreEqual("f3", fis2.fieldInfo(2).name);
		  }

		  {
			IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(random().nextBoolean() ? NoMergePolicy.NO_COMPOUND_FILES : NoMergePolicy.COMPOUND_FILES));
			Document d = new Document();
			d.add(new TextField("f1", "d3 first field", Field.Store.YES));
			d.add(new TextField("f2", "d3 second field", Field.Store.YES));
			d.add(new StoredField("f3", new sbyte[] {1, 2, 3, 4, 5}));
			writer.addDocument(d);
			writer.close();
			SegmentInfos sis = new SegmentInfos();
			sis.read(dir);
			Assert.AreEqual(3, sis.size());
			FieldInfos fis1 = SegmentReader.readFieldInfos(sis.info(0));
			FieldInfos fis2 = SegmentReader.readFieldInfos(sis.info(1));
			FieldInfos fis3 = SegmentReader.readFieldInfos(sis.info(2));
			Assert.AreEqual("f1", fis1.fieldInfo(0).name);
			Assert.AreEqual("f2", fis1.fieldInfo(1).name);
			Assert.AreEqual("f1", fis2.fieldInfo(0).name);
			assertNull(fis2.fieldInfo(1));
			Assert.AreEqual("f3", fis2.fieldInfo(2).name);
			Assert.AreEqual("f1", fis3.fieldInfo(0).name);
			Assert.AreEqual("f2", fis3.fieldInfo(1).name);
			Assert.AreEqual("f3", fis3.fieldInfo(2).name);
		  }

		  {
			IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(random().nextBoolean() ? NoMergePolicy.NO_COMPOUND_FILES : NoMergePolicy.COMPOUND_FILES));
			writer.deleteDocuments(new Term("f1", "d1"));
			// nuke the first segment entirely so that the segment with gaps is
			// loaded first!
			writer.forceMergeDeletes();
			writer.close();
		  }

		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(new LogByteSizeMergePolicy()).setInfoStream(new FailOnNonBulkMergesInfoStream()));
		  writer.forceMerge(1);
		  writer.close();

		  SegmentInfos sis = new SegmentInfos();
		  sis.read(dir);
		  Assert.AreEqual(1, sis.size());
		  FieldInfos fis1 = SegmentReader.readFieldInfos(sis.info(0));
		  Assert.AreEqual("f1", fis1.fieldInfo(0).name);
		  Assert.AreEqual("f2", fis1.fieldInfo(1).name);
		  Assert.AreEqual("f3", fis1.fieldInfo(2).name);
		  dir.close();
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testManyFields() throws Exception
	  public virtual void TestManyFields()
	  {
		int NUM_DOCS = atLeast(200);
		int MAX_FIELDS = atLeast(50);

//JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
//ORIGINAL LINE: int[][] docs = new int[NUM_DOCS][4];
		int[][] docs = RectangularArrays.ReturnRectangularIntArray(NUM_DOCS, 4);
		for (int i = 0; i < docs.Length; i++)
		{
		  for (int j = 0; j < docs[i].Length;j++)
		  {
			docs[i][j] = random().Next(MAX_FIELDS);
		  }
		}

		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));

		for (int i = 0; i < NUM_DOCS; i++)
		{
		  Document d = new Document();
		  for (int j = 0; j < docs[i].Length; j++)
		  {
			d.add(GetField(docs[i][j]));
		  }

		  writer.addDocument(d);
		}

		writer.forceMerge(1);
		writer.close();

		SegmentInfos sis = new SegmentInfos();
		sis.read(dir);
		foreach (SegmentCommitInfo si in sis)
		{
		  FieldInfos fis = SegmentReader.readFieldInfos(si);

		  foreach (FieldInfo fi in fis)
		  {
			Field expected = GetField(Convert.ToInt32(fi.name));
			Assert.AreEqual(expected.fieldType().indexed(), fi.Indexed);
			Assert.AreEqual(expected.fieldType().storeTermVectors(), fi.hasVectors());
		  }
		}

		dir.close();
	  }

	  private Field GetField(int number)
	  {
		int mode = number % 16;
		string fieldName = "" + number;
		FieldType customType = new FieldType(TextField.TYPE_STORED);

		FieldType customType2 = new FieldType(TextField.TYPE_STORED);
		customType2.Tokenized = false;

		FieldType customType3 = new FieldType(TextField.TYPE_NOT_STORED);
		customType3.Tokenized = false;

		FieldType customType4 = new FieldType(TextField.TYPE_NOT_STORED);
		customType4.Tokenized = false;
		customType4.StoreTermVectors = true;
		customType4.StoreTermVectorOffsets = true;

		FieldType customType5 = new FieldType(TextField.TYPE_NOT_STORED);
		customType5.StoreTermVectors = true;
		customType5.StoreTermVectorOffsets = true;

		FieldType customType6 = new FieldType(TextField.TYPE_STORED);
		customType6.Tokenized = false;
		customType6.StoreTermVectors = true;
		customType6.StoreTermVectorOffsets = true;

		FieldType customType7 = new FieldType(TextField.TYPE_NOT_STORED);
		customType7.Tokenized = false;
		customType7.StoreTermVectors = true;
		customType7.StoreTermVectorOffsets = true;

		FieldType customType8 = new FieldType(TextField.TYPE_STORED);
		customType8.Tokenized = false;
		customType8.StoreTermVectors = true;
		customType8.StoreTermVectorPositions = true;

		FieldType customType9 = new FieldType(TextField.TYPE_NOT_STORED);
		customType9.StoreTermVectors = true;
		customType9.StoreTermVectorPositions = true;

		FieldType customType10 = new FieldType(TextField.TYPE_STORED);
		customType10.Tokenized = false;
		customType10.StoreTermVectors = true;
		customType10.StoreTermVectorPositions = true;

		FieldType customType11 = new FieldType(TextField.TYPE_NOT_STORED);
		customType11.Tokenized = false;
		customType11.StoreTermVectors = true;
		customType11.StoreTermVectorPositions = true;

		FieldType customType12 = new FieldType(TextField.TYPE_STORED);
		customType12.StoreTermVectors = true;
		customType12.StoreTermVectorOffsets = true;
		customType12.StoreTermVectorPositions = true;

		FieldType customType13 = new FieldType(TextField.TYPE_NOT_STORED);
		customType13.StoreTermVectors = true;
		customType13.StoreTermVectorOffsets = true;
		customType13.StoreTermVectorPositions = true;

		FieldType customType14 = new FieldType(TextField.TYPE_STORED);
		customType14.Tokenized = false;
		customType14.StoreTermVectors = true;
		customType14.StoreTermVectorOffsets = true;
		customType14.StoreTermVectorPositions = true;

		FieldType customType15 = new FieldType(TextField.TYPE_NOT_STORED);
		customType15.Tokenized = false;
		customType15.StoreTermVectors = true;
		customType15.StoreTermVectorOffsets = true;
		customType15.StoreTermVectorPositions = true;

		switch (mode)
		{
		  case 0:
			  return new Field(fieldName, "some text", customType);
		  case 1:
			  return new TextField(fieldName, "some text", Field.Store.NO);
		  case 2:
			  return new Field(fieldName, "some text", customType2);
		  case 3:
			  return new Field(fieldName, "some text", customType3);
		  case 4:
			  return new Field(fieldName, "some text", customType4);
		  case 5:
			  return new Field(fieldName, "some text", customType5);
		  case 6:
			  return new Field(fieldName, "some text", customType6);
		  case 7:
			  return new Field(fieldName, "some text", customType7);
		  case 8:
			  return new Field(fieldName, "some text", customType8);
		  case 9:
			  return new Field(fieldName, "some text", customType9);
		  case 10:
			  return new Field(fieldName, "some text", customType10);
		  case 11:
			  return new Field(fieldName, "some text", customType11);
		  case 12:
			  return new Field(fieldName, "some text", customType12);
		  case 13:
			  return new Field(fieldName, "some text", customType13);
		  case 14:
			  return new Field(fieldName, "some text", customType14);
		  case 15:
			  return new Field(fieldName, "some text", customType15);
		  default:
			  return null;
		}
	  }
	}

}