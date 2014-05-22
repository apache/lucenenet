using System;

namespace Lucene.Net.Store
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
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using IndexInputSlicer = Lucene.Net.Store.Directory.IndexInputSlicer;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	/// <summary>
	/// Tests MMapDirectory's MultiMMapIndexInput
	/// <p>
	/// Because Java's ByteBuffer uses an int to address the
	/// values, it's necessary to access a file >
	/// Integer.MAX_VALUE in size using multiple byte buffers.
	/// </summary>
	public class TestMultiMMap : LuceneTestCase
	{

	  public override void SetUp()
	  {
		base.setUp();
		assumeTrue("test requires a jre that supports unmapping", MMapDirectory.UNMAP_SUPPORTED);
	  }

	  public virtual void TestCloneSafety()
	  {
		MMapDirectory mmapDir = new MMapDirectory(createTempDir("testCloneSafety"));
		IndexOutput io = mmapDir.createOutput("bytes", newIOContext(random()));
		io.writeVInt(5);
		io.close();
		IndexInput one = mmapDir.openInput("bytes", IOContext.DEFAULT);
		IndexInput two = one.clone();
		IndexInput three = two.clone(); // clone of clone
		one.close();
		try
		{
		  one.readVInt();
		  Assert.Fail("Must throw AlreadyClosedException");
		}
		catch (AlreadyClosedException ignore)
		{
		  // pass
		}
		try
		{
		  two.readVInt();
		  Assert.Fail("Must throw AlreadyClosedException");
		}
		catch (AlreadyClosedException ignore)
		{
		  // pass
		}
		try
		{
		  three.readVInt();
		  Assert.Fail("Must throw AlreadyClosedException");
		}
		catch (AlreadyClosedException ignore)
		{
		  // pass
		}
		two.close();
		three.close();
		// test double close of master:
		one.close();
		mmapDir.close();
	  }

	  public virtual void TestCloneClose()
	  {
		MMapDirectory mmapDir = new MMapDirectory(createTempDir("testCloneClose"));
		IndexOutput io = mmapDir.createOutput("bytes", newIOContext(random()));
		io.writeVInt(5);
		io.close();
		IndexInput one = mmapDir.openInput("bytes", IOContext.DEFAULT);
		IndexInput two = one.clone();
		IndexInput three = two.clone(); // clone of clone
		two.close();
		Assert.AreEqual(5, one.readVInt());
		try
		{
		  two.readVInt();
		  Assert.Fail("Must throw AlreadyClosedException");
		}
		catch (AlreadyClosedException ignore)
		{
		  // pass
		}
		Assert.AreEqual(5, three.readVInt());
		one.close();
		three.close();
		mmapDir.close();
	  }

	  public virtual void TestCloneSliceSafety()
	  {
		MMapDirectory mmapDir = new MMapDirectory(createTempDir("testCloneSliceSafety"));
		IndexOutput io = mmapDir.createOutput("bytes", newIOContext(random()));
		io.writeInt(1);
		io.writeInt(2);
		io.close();
		IndexInputSlicer slicer = mmapDir.createSlicer("bytes", newIOContext(random()));
		IndexInput one = slicer.openSlice("first int", 0, 4);
		IndexInput two = slicer.openSlice("second int", 4, 4);
		IndexInput three = one.clone(); // clone of clone
		IndexInput four = two.clone(); // clone of clone
		slicer.close();
		try
		{
		  one.readInt();
		  Assert.Fail("Must throw AlreadyClosedException");
		}
		catch (AlreadyClosedException ignore)
		{
		  // pass
		}
		try
		{
		  two.readInt();
		  Assert.Fail("Must throw AlreadyClosedException");
		}
		catch (AlreadyClosedException ignore)
		{
		  // pass
		}
		try
		{
		  three.readInt();
		  Assert.Fail("Must throw AlreadyClosedException");
		}
		catch (AlreadyClosedException ignore)
		{
		  // pass
		}
		try
		{
		  four.readInt();
		  Assert.Fail("Must throw AlreadyClosedException");
		}
		catch (AlreadyClosedException ignore)
		{
		  // pass
		}
		one.close();
		two.close();
		three.close();
		four.close();
		// test double-close of slicer:
		slicer.close();
		mmapDir.close();
	  }

	  public virtual void TestCloneSliceClose()
	  {
		MMapDirectory mmapDir = new MMapDirectory(createTempDir("testCloneSliceClose"));
		IndexOutput io = mmapDir.createOutput("bytes", newIOContext(random()));
		io.writeInt(1);
		io.writeInt(2);
		io.close();
		IndexInputSlicer slicer = mmapDir.createSlicer("bytes", newIOContext(random()));
		IndexInput one = slicer.openSlice("first int", 0, 4);
		IndexInput two = slicer.openSlice("second int", 4, 4);
		one.close();
		try
		{
		  one.readInt();
		  Assert.Fail("Must throw AlreadyClosedException");
		}
		catch (AlreadyClosedException ignore)
		{
		  // pass
		}
		Assert.AreEqual(2, two.readInt());
		// reopen a new slice "one":
		one = slicer.openSlice("first int", 0, 4);
		Assert.AreEqual(1, one.readInt());
		one.close();
		two.close();
		slicer.close();
		mmapDir.close();
	  }

	  public virtual void TestSeekZero()
	  {
		for (int i = 0; i < 31; i++)
		{
		  MMapDirectory mmapDir = new MMapDirectory(createTempDir("testSeekZero"), null, 1 << i);
		  IndexOutput io = mmapDir.createOutput("zeroBytes", newIOContext(random()));
		  io.close();
		  IndexInput ii = mmapDir.openInput("zeroBytes", newIOContext(random()));
		  ii.seek(0L);
		  ii.close();
		  mmapDir.close();
		}
	  }

	  public virtual void TestSeekSliceZero()
	  {
		for (int i = 0; i < 31; i++)
		{
		  MMapDirectory mmapDir = new MMapDirectory(createTempDir("testSeekSliceZero"), null, 1 << i);
		  IndexOutput io = mmapDir.createOutput("zeroBytes", newIOContext(random()));
		  io.close();
		  IndexInputSlicer slicer = mmapDir.createSlicer("zeroBytes", newIOContext(random()));
		  IndexInput ii = slicer.openSlice("zero-length slice", 0, 0);
		  ii.seek(0L);
		  ii.close();
		  slicer.close();
		  mmapDir.close();
		}
	  }

	  public virtual void TestSeekEnd()
	  {
		for (int i = 0; i < 17; i++)
		{
		  MMapDirectory mmapDir = new MMapDirectory(createTempDir("testSeekEnd"), null, 1 << i);
		  IndexOutput io = mmapDir.createOutput("bytes", newIOContext(random()));
		  sbyte[] bytes = new sbyte[1 << i];
		  random().nextBytes(bytes);
		  io.writeBytes(bytes, bytes.Length);
		  io.close();
		  IndexInput ii = mmapDir.openInput("bytes", newIOContext(random()));
		  sbyte[] actual = new sbyte[1 << i];
		  ii.readBytes(actual, 0, actual.Length);
		  Assert.AreEqual(new BytesRef(bytes), new BytesRef(actual));
		  ii.seek(1 << i);
		  ii.close();
		  mmapDir.close();
		}
	  }

	  public virtual void TestSeekSliceEnd()
	  {
		for (int i = 0; i < 17; i++)
		{
		  MMapDirectory mmapDir = new MMapDirectory(createTempDir("testSeekSliceEnd"), null, 1 << i);
		  IndexOutput io = mmapDir.createOutput("bytes", newIOContext(random()));
		  sbyte[] bytes = new sbyte[1 << i];
		  random().nextBytes(bytes);
		  io.writeBytes(bytes, bytes.Length);
		  io.close();
		  IndexInputSlicer slicer = mmapDir.createSlicer("bytes", newIOContext(random()));
		  IndexInput ii = slicer.openSlice("full slice", 0, bytes.Length);
		  sbyte[] actual = new sbyte[1 << i];
		  ii.readBytes(actual, 0, actual.Length);
		  Assert.AreEqual(new BytesRef(bytes), new BytesRef(actual));
		  ii.seek(1 << i);
		  ii.close();
		  slicer.close();
		  mmapDir.close();
		}
	  }

	  public virtual void TestSeeking()
	  {
		for (int i = 0; i < 10; i++)
		{
		  MMapDirectory mmapDir = new MMapDirectory(createTempDir("testSeeking"), null, 1 << i);
		  IndexOutput io = mmapDir.createOutput("bytes", newIOContext(random()));
		  sbyte[] bytes = new sbyte[1 << (i + 1)]; // make sure we switch buffers
		  random().nextBytes(bytes);
		  io.writeBytes(bytes, bytes.Length);
		  io.close();
		  IndexInput ii = mmapDir.openInput("bytes", newIOContext(random()));
		  sbyte[] actual = new sbyte[1 << (i + 1)]; // first read all bytes
		  ii.readBytes(actual, 0, actual.Length);
		  Assert.AreEqual(new BytesRef(bytes), new BytesRef(actual));
		  for (int sliceStart = 0; sliceStart < bytes.Length; sliceStart++)
		  {
			for (int sliceLength = 0; sliceLength < bytes.Length - sliceStart; sliceLength++)
			{
			  sbyte[] slice = new sbyte[sliceLength];
			  ii.seek(sliceStart);
			  ii.readBytes(slice, 0, slice.Length);
			  Assert.AreEqual(new BytesRef(bytes, sliceStart, sliceLength), new BytesRef(slice));
			}
		  }
		  ii.close();
		  mmapDir.close();
		}
	  }

	  // note instead of seeking to offset and reading length, this opens slices at the 
	  // the various offset+length and just does readBytes.
	  public virtual void TestSlicedSeeking()
	  {
		for (int i = 0; i < 10; i++)
		{
		  MMapDirectory mmapDir = new MMapDirectory(createTempDir("testSlicedSeeking"), null, 1 << i);
		  IndexOutput io = mmapDir.createOutput("bytes", newIOContext(random()));
		  sbyte[] bytes = new sbyte[1 << (i + 1)]; // make sure we switch buffers
		  random().nextBytes(bytes);
		  io.writeBytes(bytes, bytes.Length);
		  io.close();
		  IndexInput ii = mmapDir.openInput("bytes", newIOContext(random()));
		  sbyte[] actual = new sbyte[1 << (i + 1)]; // first read all bytes
		  ii.readBytes(actual, 0, actual.Length);
		  ii.close();
		  Assert.AreEqual(new BytesRef(bytes), new BytesRef(actual));
		  IndexInputSlicer slicer = mmapDir.createSlicer("bytes", newIOContext(random()));
		  for (int sliceStart = 0; sliceStart < bytes.Length; sliceStart++)
		  {
			for (int sliceLength = 0; sliceLength < bytes.Length - sliceStart; sliceLength++)
			{
			  sbyte[] slice = new sbyte[sliceLength];
			  IndexInput input = slicer.openSlice("bytesSlice", sliceStart, slice.Length);
			  input.readBytes(slice, 0, slice.Length);
			  input.close();
			  Assert.AreEqual(new BytesRef(bytes, sliceStart, sliceLength), new BytesRef(slice));
			}
		  }
		  slicer.close();
		  mmapDir.close();
		}
	  }

	  public virtual void TestRandomChunkSizes()
	  {
		int num = atLeast(10);
		for (int i = 0; i < num; i++)
		{
		  AssertChunking(random(), TestUtil.Next(random(), 20, 100));
		}
	  }

	  private void AssertChunking(Random random, int chunkSize)
	  {
		File path = createTempDir("mmap" + chunkSize);
		MMapDirectory mmapDir = new MMapDirectory(path, null, chunkSize);
		// we will map a lot, try to turn on the unmap hack
		if (MMapDirectory.UNMAP_SUPPORTED)
		{
		  mmapDir.UseUnmap = true;
		}
		MockDirectoryWrapper dir = new MockDirectoryWrapper(random, mmapDir);
		RandomIndexWriter writer = new RandomIndexWriter(random, dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).setMergePolicy(newLogMergePolicy()));
		Document doc = new Document();
		Field docid = newStringField("docid", "0", Field.Store.YES);
		Field junk = newStringField("junk", "", Field.Store.YES);
		doc.add(docid);
		doc.add(junk);

		int numDocs = 100;
		for (int i = 0; i < numDocs; i++)
		{
		  docid.StringValue = "" + i;
		  junk.StringValue = TestUtil.randomUnicodeString(random);
		  writer.addDocument(doc);
		}
		IndexReader reader = writer.Reader;
		writer.close();

		int numAsserts = atLeast(100);
		for (int i = 0; i < numAsserts; i++)
		{
		  int docID = random.Next(numDocs);
		  Assert.AreEqual("" + docID, reader.document(docID).get("docid"));
		}
		reader.close();
		dir.close();
	  }
	}

}