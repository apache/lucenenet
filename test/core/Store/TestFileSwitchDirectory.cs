using System.Collections.Generic;

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
	using Codec = Lucene.Net.Codecs.Codec;
	using Lucene40StoredFieldsWriter = Lucene.Net.Codecs.Lucene40.Lucene40StoredFieldsWriter;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriter = Lucene.Net.Index.IndexWriter;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using TestIndexWriterReader = Lucene.Net.Index.TestIndexWriterReader;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestFileSwitchDirectory : LuceneTestCase
	{
	  /// <summary>
	  /// Test if writing doc stores to disk and everything else to ram works.
	  /// </summary>
	  public virtual void TestBasic()
	  {
		Set<string> fileExtensions = new HashSet<string>();
		fileExtensions.add(Lucene40StoredFieldsWriter.FIELDS_EXTENSION);
		fileExtensions.add(Lucene40StoredFieldsWriter.FIELDS_INDEX_EXTENSION);

		MockDirectoryWrapper primaryDir = new MockDirectoryWrapper(random(), new RAMDirectory());
		primaryDir.CheckIndexOnClose = false; // only part of an index
		MockDirectoryWrapper secondaryDir = new MockDirectoryWrapper(random(), new RAMDirectory());
		secondaryDir.CheckIndexOnClose = false; // only part of an index

		FileSwitchDirectory fsd = new FileSwitchDirectory(fileExtensions, primaryDir, secondaryDir, true);
		// for now we wire Lucene40Codec because we rely upon its specific impl
		bool oldValue = OLD_FORMAT_IMPERSONATION_IS_ACTIVE;
		OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
		IndexWriter writer = new IndexWriter(fsd, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setMergePolicy(newLogMergePolicy(false)).setCodec(Codec.forName("Lucene40")).setUseCompoundFile(false));
		TestIndexWriterReader.CreateIndexNoClose(true, "ram", writer);
		IndexReader reader = DirectoryReader.open(writer, true);
		Assert.AreEqual(100, reader.maxDoc());
		writer.commit();
		// we should see only fdx,fdt files here
		string[] files = primaryDir.listAll();
		Assert.IsTrue(files.Length > 0);
		for (int x = 0; x < files.Length; x++)
		{
		  string ext = FileSwitchDirectory.getExtension(files[x]);
		  Assert.IsTrue(fileExtensions.contains(ext));
		}
		files = secondaryDir.listAll();
		Assert.IsTrue(files.Length > 0);
		// we should not see fdx,fdt files here
		for (int x = 0; x < files.Length; x++)
		{
		  string ext = FileSwitchDirectory.getExtension(files[x]);
		  Assert.IsFalse(fileExtensions.contains(ext));
		}
		reader.close();
		writer.close();

		files = fsd.listAll();
		for (int i = 0;i < files.Length;i++)
		{
		  Assert.IsNotNull(files[i]);
		}
		fsd.close();
		OLD_FORMAT_IMPERSONATION_IS_ACTIVE = oldValue;
	  }

	  private Directory NewFSSwitchDirectory(Set<string> primaryExtensions)
	  {
		File primDir = createTempDir("foo");
		File secondDir = createTempDir("bar");
		return NewFSSwitchDirectory(primDir, secondDir, primaryExtensions);
	  }

	  private Directory NewFSSwitchDirectory(File aDir, File bDir, Set<string> primaryExtensions)
	  {
		Directory a = new SimpleFSDirectory(aDir);
		Directory b = new SimpleFSDirectory(bDir);
		FileSwitchDirectory switchDir = new FileSwitchDirectory(primaryExtensions, a, b, true);
		return new MockDirectoryWrapper(random(), switchDir);
	  }

	  // LUCENE-3380 -- make sure we get exception if the directory really does not exist.
	  public virtual void TestNoDir()
	  {
		File primDir = createTempDir("foo");
		File secondDir = createTempDir("bar");
		TestUtil.rm(primDir);
		TestUtil.rm(secondDir);
		Directory dir = NewFSSwitchDirectory(primDir, secondDir, Collections.emptySet<string>());
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

	  // LUCENE-3380 test that we can add a file, and then when we call list() we get it back
	  public virtual void TestDirectoryFilter()
	  {
		Directory dir = NewFSSwitchDirectory(Collections.emptySet<string>());
		string name = "file";
		try
		{
		  dir.createOutput(name, newIOContext(random())).close();
		  Assert.IsTrue(slowFileExists(dir, name));
		  Assert.IsTrue(Arrays.asList(dir.listAll()).contains(name));
		}
		finally
		{
		  dir.close();
		}
	  }

	  // LUCENE-3380 test that delegate compound files correctly.
	  public virtual void TestCompoundFileAppendTwice()
	  {
		Directory newDir = NewFSSwitchDirectory(Collections.singleton("cfs"));
		CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), true);
		CreateSequenceFile(newDir, "d1", (sbyte) 0, 15);
		IndexOutput @out = csw.createOutput("d.xyz", newIOContext(random()));
		@out.writeInt(0);
		@out.close();
		Assert.AreEqual(1, csw.listAll().length);
		Assert.AreEqual("d.xyz", csw.listAll()[0]);

		csw.close();

		CompoundFileDirectory cfr = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), false);
		Assert.AreEqual(1, cfr.listAll().length);
		Assert.AreEqual("d.xyz", cfr.listAll()[0]);
		cfr.close();
		newDir.close();
	  }

	  /// <summary>
	  /// Creates a file of the specified size with sequential data. The first
	  ///  byte is written as the start byte provided. All subsequent bytes are
	  ///  computed as start + offset where offset is the number of the byte.
	  /// </summary>
	  private void CreateSequenceFile(Directory dir, string name, sbyte start, int size)
	  {
		  IndexOutput os = dir.createOutput(name, newIOContext(random()));
		  for (int i = 0; i < size; i++)
		  {
			  os.writeByte(start);
			  start++;
		  }
		  os.close();
	  }

	}

}