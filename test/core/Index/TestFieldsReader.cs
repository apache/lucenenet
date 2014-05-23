using System;
using System.Collections.Generic;

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
	using DocumentStoredFieldVisitor = Lucene.Net.Document.DocumentStoredFieldVisitor;
	using Field = Lucene.Net.Document.Field;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using BaseDirectory = Lucene.Net.Store.BaseDirectory;
	using BufferedIndexInput = Lucene.Net.Store.BufferedIndexInput;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;

	public class TestFieldsReader : LuceneTestCase
	{
	  private static Directory Dir;
	  private static Document TestDoc;
	  private static FieldInfos.Builder FieldInfos = null;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		TestDoc = new Document();
		FieldInfos = new FieldInfos.Builder();
		DocHelper.setupDoc(TestDoc);
		foreach (IndexableField field in TestDoc)
		{
		  FieldInfos.addOrUpdate(field.name(), field.fieldType());
		}
		Dir = newDirectory();
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMergePolicy(newLogMergePolicy());
		conf.MergePolicy.NoCFSRatio = 0.0;
		IndexWriter writer = new IndexWriter(Dir, conf);
		writer.addDocument(TestDoc);
		writer.close();
		FaultyIndexInput.DoFail = false;
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass() throws Exception
	  public static void AfterClass()
	  {
		Dir.close();
		Dir = null;
		FieldInfos = null;
		TestDoc = null;
	  }

	  public virtual void Test()
	  {
		Assert.IsTrue(Dir != null);
		Assert.IsTrue(FieldInfos != null);
		IndexReader reader = DirectoryReader.open(Dir);
		Document doc = reader.document(0);
		Assert.IsTrue(doc != null);
		Assert.IsTrue(doc.getField(DocHelper.TEXT_FIELD_1_KEY) != null);

		Field field = (Field) doc.getField(DocHelper.TEXT_FIELD_2_KEY);
		Assert.IsTrue(field != null);
		Assert.IsTrue(field.fieldType().storeTermVectors());

		Assert.IsFalse(field.fieldType().omitNorms());
		Assert.IsTrue(field.fieldType().indexOptions() == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);

		field = (Field) doc.getField(DocHelper.TEXT_FIELD_3_KEY);
		Assert.IsTrue(field != null);
		Assert.IsFalse(field.fieldType().storeTermVectors());
		Assert.IsTrue(field.fieldType().omitNorms());
		Assert.IsTrue(field.fieldType().indexOptions() == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);

		field = (Field) doc.getField(DocHelper.NO_TF_KEY);
		Assert.IsTrue(field != null);
		Assert.IsFalse(field.fieldType().storeTermVectors());
		Assert.IsFalse(field.fieldType().omitNorms());
		Assert.IsTrue(field.fieldType().indexOptions() == IndexOptions.DOCS_ONLY);

		DocumentStoredFieldVisitor visitor = new DocumentStoredFieldVisitor(DocHelper.TEXT_FIELD_3_KEY);
		reader.document(0, visitor);
		IList<IndexableField> fields = visitor.Document.Fields;
		Assert.AreEqual(1, fields.Count);
		Assert.AreEqual(DocHelper.TEXT_FIELD_3_KEY, fields[0].name());
		reader.close();
	  }


	  public class FaultyFSDirectory : BaseDirectory
	  {

		internal Directory FsDir;

		public FaultyFSDirectory(File dir)
		{
		  FsDir = newFSDirectory(dir);
		  lockFactory = FsDir.LockFactory;
		}
		public override IndexInput OpenInput(string name, IOContext context)
		{
		  return new FaultyIndexInput(FsDir.openInput(name, context));
		}
		public override string[] ListAll()
		{
		  return FsDir.listAll();
		}
		public override bool FileExists(string name)
		{
		  return FsDir.fileExists(name);
		}
		public override void DeleteFile(string name)
		{
		  FsDir.deleteFile(name);
		}
		public override long FileLength(string name)
		{
		  return FsDir.fileLength(name);
		}
		public override IndexOutput CreateOutput(string name, IOContext context)
		{
		  return FsDir.createOutput(name, context);
		}
		public override void Sync(ICollection<string> names)
		{
		  FsDir.sync(names);
		}
		public override void Close()
		{
		  FsDir.close();
		}
	  }

	  private class FaultyIndexInput : BufferedIndexInput
	  {
		internal IndexInput @delegate;
		internal static bool DoFail;
		internal int Count;
		internal FaultyIndexInput(IndexInput @delegate) : base("FaultyIndexInput(" + @delegate + ")", BufferedIndexInput.BUFFER_SIZE)
		{
		  this.@delegate = @delegate;
		}
		internal virtual void SimOutage()
		{
		  if (DoFail && Count++ % 2 == 1)
		  {
			throw new IOException("Simulated network outage");
		  }
		}
		public override void ReadInternal(sbyte[] b, int offset, int length)
		{
		  SimOutage();
		  @delegate.seek(FilePointer);
		  @delegate.readBytes(b, offset, length);
		}
		public override void SeekInternal(long pos)
		{
		}
		public override long Length()
		{
		  return @delegate.length();
		}
		public override void Close()
		{
		  @delegate.close();
		}
		public override FaultyIndexInput Clone()
		{
		  FaultyIndexInput i = new FaultyIndexInput(@delegate.clone());
		  // seek the clone to our current position
		  try
		  {
			i.seek(FilePointer);
		  }
		  catch (IOException e)
		  {
			throw new Exception();
		  }
		  return i;
		}
	  }

	  // LUCENE-1262
	  public virtual void TestExceptions()
	  {
		File indexDir = createTempDir("testfieldswriterexceptions");

		try
		{
		  Directory dir = new FaultyFSDirectory(indexDir);
		  IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setOpenMode(OpenMode.CREATE);
		  IndexWriter writer = new IndexWriter(dir, iwc);
		  for (int i = 0;i < 2;i++)
		  {
			writer.addDocument(TestDoc);
		  }
		  writer.forceMerge(1);
		  writer.close();

		  IndexReader reader = DirectoryReader.open(dir);

		  FaultyIndexInput.DoFail = true;

		  bool exc = false;

		  for (int i = 0;i < 2;i++)
		  {
			try
			{
			  reader.document(i);
			}
			catch (IOException ioe)
			{
			  // expected
			  exc = true;
			}
			try
			{
			  reader.document(i);
			}
			catch (IOException ioe)
			{
			  // expected
			  exc = true;
			}
		  }
		  Assert.IsTrue(exc);
		  reader.close();
		  dir.close();
		}
		finally
		{
		  TestUtil.rm(indexDir);
		}

	  }
	}

}