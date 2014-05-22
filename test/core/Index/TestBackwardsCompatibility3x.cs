using System;
using System.Diagnostics;
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
	using BinaryDocValuesField = Lucene.Net.Document.BinaryDocValuesField;
	using Document = Lucene.Net.Document.Document;
	using DoubleDocValuesField = Lucene.Net.Document.DoubleDocValuesField;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using FloatDocValuesField = Lucene.Net.Document.FloatDocValuesField;
	using IntField = Lucene.Net.Document.IntField;
	using LongField = Lucene.Net.Document.LongField;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using SortedDocValuesField = Lucene.Net.Document.SortedDocValuesField;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;
	using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using FieldCache = Lucene.Net.Search.FieldCache;
	using IndexSearcher = Lucene.Net.Search.IndexSearcher;
	using NumericRangeQuery = Lucene.Net.Search.NumericRangeQuery;
	using PhraseQuery = Lucene.Net.Search.PhraseQuery;
	using ScoreDoc = Lucene.Net.Search.ScoreDoc;
	using TermQuery = Lucene.Net.Search.TermQuery;
	using TopDocs = Lucene.Net.Search.TopDocs;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using Directory = Lucene.Net.Store.Directory;
	using RAMDirectory = Lucene.Net.Store.RAMDirectory;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using Constants = Lucene.Net.Util.Constants;
	using StringHelper = Lucene.Net.Util.StringHelper;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;

	/*
	  Verify we can read the pre-4.0 file format, do searches
	  against it, and add documents to it.
	*/
	// don't use 3.x codec, its unrealistic since it means
	// we won't even be running the actual code, only the impostor
	// Sep codec cannot yet handle the offsets we add when changing indexes!
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs({"Lucene3x", "MockFixedIntBlock", "MockVariableIntBlock", "MockSep", "MockRandom", "Lucene40", "Lucene41", "Appending", "Lucene42", "Lucene45"}) public class TestBackwardsCompatibility3x extends Lucene.Net.Util.LuceneTestCase
	public class TestBackwardsCompatibility3x : LuceneTestCase
	{

	  // Uncomment these cases & run them on an older Lucene
	  // version, to generate an index to test backwards
	  // compatibility.  Then, cd to build/test/index.cfs and
	  // run "zip index.<VERSION>.cfs.zip *"; cd to
	  // build/test/index.nocfs and run "zip
	  // index.<VERSION>.nocfs.zip *".  Then move those 2 zip
	  // files to your trunk checkout and add them to the
	  // oldNames array.

	  /*
	  public void testCreateCFS() throws IOException {
	    createIndex("index.cfs", true, false);
	  }
	
	  public void testCreateNoCFS() throws IOException {
	    createIndex("index.nocfs", false, false);
	  }
	  */

	/*
	  // These are only needed for the special upgrade test to verify
	  // that also single-segment indexes are correctly upgraded by IndexUpgrader.
	  // You don't need them to be build for non-3.1 (the test is happy with just one
	  // "old" segment format, version is unimportant:
	  
	  public void testCreateSingleSegmentCFS() throws IOException {
	    createIndex("index.singlesegment.cfs", true, true);
	  }
	
	  public void testCreateSingleSegmentNoCFS() throws IOException {
	    createIndex("index.singlesegment.nocfs", false, true);
	  }
	
	*/  
	  internal static readonly string[] OldNames = new string[] {"30.cfs", "30.nocfs", "31.cfs", "31.nocfs", "32.cfs", "32.nocfs", "34.cfs", "34.nocfs"};

	  internal readonly string[] UnsupportedNames = new string[] {"19.cfs", "19.nocfs", "20.cfs", "20.nocfs", "21.cfs", "21.nocfs", "22.cfs", "22.nocfs", "23.cfs", "23.nocfs", "24.cfs", "24.nocfs", "29.cfs", "29.nocfs"};

	  internal static readonly string[] OldSingleSegmentNames = new string[] {"31.optimized.cfs", "31.optimized.nocfs"};

	  internal static IDictionary<string, Directory> OldIndexDirs;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass() throws Exception
	  public static void BeforeClass()
	  {
		Assert.IsFalse("test infra is broken!", LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE);
		IList<string> names = new List<string>(OldNames.Length + OldSingleSegmentNames.Length);
		names.AddRange(Arrays.asList(OldNames));
		names.AddRange(Arrays.asList(OldSingleSegmentNames));
		OldIndexDirs = new Dictionary<>();
		foreach (string name in names)
		{
		  File dir = createTempDir(name);
		  File dataFile = new File(typeof(TestBackwardsCompatibility3x).getResource("index." + name + ".zip").toURI());
		  TestUtil.unzip(dataFile, dir);
		  OldIndexDirs[name] = newFSDirectory(dir);
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void afterClass() throws Exception
	  public static void AfterClass()
	  {
		foreach (Directory d in OldIndexDirs.Values)
		{
		  d.close();
		}
		OldIndexDirs = null;
	  }

	  /// <summary>
	  /// this test checks that *only* IndexFormatTooOldExceptions are thrown when you open and operate on too old indexes! </summary>
	  public virtual void TestUnsupportedOldIndexes()
	  {
		for (int i = 0;i < UnsupportedNames.Length;i++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: index " + UnsupportedNames[i]);
		  }
		  File oldIndexDir = createTempDir(UnsupportedNames[i]);
		  TestUtil.unzip(getDataFile("unsupported." + UnsupportedNames[i] + ".zip"), oldIndexDir);
		  BaseDirectoryWrapper dir = newFSDirectory(oldIndexDir);
		  // don't checkindex, these are intentionally not supported
		  dir.CheckIndexOnClose = false;

		  IndexReader reader = null;
		  IndexWriter writer = null;
		  try
		  {
			reader = DirectoryReader.open(dir);
			Assert.Fail("DirectoryReader.open should not pass for " + UnsupportedNames[i]);
		  }
		  catch (IndexFormatTooOldException e)
		  {
			// pass
		  }
		  finally
		  {
			if (reader != null)
			{
				reader.close();
			}
			reader = null;
		  }

		  try
		  {
			writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
			Assert.Fail("IndexWriter creation should not pass for " + UnsupportedNames[i]);
		  }
		  catch (IndexFormatTooOldException e)
		  {
			// pass
			if (VERBOSE)
			{
			  Console.WriteLine("TEST: got expected exc:");
			  e.printStackTrace(System.out);
			}
			// Make sure exc message includes a path=
			Assert.IsTrue("got exc message: " + e.Message, e.Message.IndexOf("path=\"") != -1);
		  }
		  finally
		  {
			// we should fail to open IW, and so it should be null when we get here.
			// However, if the test fails (i.e., IW did not fail on open), we need
			// to close IW. However, if merges are run, IW may throw
			// IndexFormatTooOldException, and we don't want to mask the Assert.Fail()
			// above, so close without waiting for merges.
			if (writer != null)
			{
			  writer.close(false);
			}
			writer = null;
		  }

		  ByteArrayOutputStream bos = new ByteArrayOutputStream(1024);
		  CheckIndex checker = new CheckIndex(dir);
		  checker.InfoStream = new PrintStream(bos, false, "UTF-8");
		  CheckIndex.Status indexStatus = checker.checkIndex();
		  Assert.IsFalse(indexStatus.clean);
		  Assert.IsTrue(bos.ToString("UTF-8").Contains(typeof(IndexFormatTooOldException).Name));

		  dir.close();
		}
	  }

	  public virtual void TestFullyMergeOldIndex()
	  {
		foreach (string name in OldNames)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: index=" + name);
		  }
		  Directory dir = newDirectory(OldIndexDirs[name]);
		  IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  w.forceMerge(1);
		  w.close();

		  dir.close();
		}
	  }

	  public virtual void TestAddOldIndexes()
	  {
		foreach (string name in OldNames)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: old index " + name);
		  }
		  Directory targetDir = newDirectory();
		  IndexWriter w = new IndexWriter(targetDir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  w.addIndexes(OldIndexDirs[name]);
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: done adding indices; now close");
		  }
		  w.close();

		  targetDir.close();
		}
	  }

	  public virtual void TestAddOldIndexesReader()
	  {
		foreach (string name in OldNames)
		{
		  IndexReader reader = DirectoryReader.open(OldIndexDirs[name]);

		  Directory targetDir = newDirectory();
		  IndexWriter w = new IndexWriter(targetDir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
		  w.addIndexes(reader);
		  w.close();
		  reader.close();

		  targetDir.close();
		}
	  }

	  public virtual void TestSearchOldIndex()
	  {
		foreach (string name in OldNames)
		{
		  SearchIndex(OldIndexDirs[name], name);
		}
	  }

	  public virtual void TestIndexOldIndexNoAdds()
	  {
		foreach (string name in OldNames)
		{
		  Directory dir = newDirectory(OldIndexDirs[name]);
		  ChangeIndexNoAdds(random(), dir);
		  dir.close();
		}
	  }

	  public virtual void TestIndexOldIndex()
	  {
		foreach (string name in OldNames)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: oldName=" + name);
		  }
		  Directory dir = newDirectory(OldIndexDirs[name]);
		  ChangeIndexWithAdds(random(), dir, name);
		  dir.close();
		}
	  }

	  /// @deprecated 3.x transition mechanism 
	  [Obsolete("3.x transition mechanism")]
	  public virtual void TestDeleteOldIndex()
	  {
		foreach (string name in OldNames)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: oldName=" + name);
		  }

		  // Try one delete:
		  Directory dir = newDirectory(OldIndexDirs[name]);

		  IndexReader ir = DirectoryReader.open(dir);
		  Assert.AreEqual(35, ir.numDocs());
		  ir.close();

		  IndexWriter iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
		  iw.deleteDocuments(new Term("id", "3"));
		  iw.close();

		  ir = DirectoryReader.open(dir);
		  Assert.AreEqual(34, ir.numDocs());
		  ir.close();

		  // Delete all but 1 document:
		  iw = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null));
		  for (int i = 0;i < 35;i++)
		  {
			iw.deleteDocuments(new Term("id", "" + i));
		  }

		  // Verify NRT reader takes:
		  ir = DirectoryReader.open(iw, true);
		  iw.close();

		  Assert.AreEqual("index " + name, 1, ir.numDocs());
		  ir.close();

		  // Verify non-NRT reader takes:
		  ir = DirectoryReader.open(dir);
		  Assert.AreEqual("index " + name, 1, ir.numDocs());
		  ir.close();

		  dir.close();
		}
	  }

	  private void DoTestHits(ScoreDoc[] hits, int expectedCount, IndexReader reader)
	  {
		int hitCount = hits.Length;
		Assert.AreEqual("wrong number of hits", expectedCount, hitCount);
		for (int i = 0;i < hitCount;i++)
		{
		  reader.document(hits[i].doc);
		  reader.getTermVectors(hits[i].doc);
		}
	  }

	  public virtual void SearchIndex(Directory dir, string oldName)
	  {
		//QueryParser parser = new QueryParser("contents", new MockAnalyzer(random));
		//Query query = parser.parse("handle:1");

		IndexReader reader = DirectoryReader.open(dir);
		IndexSearcher searcher = new IndexSearcher(reader);

		TestUtil.checkIndex(dir);

		// true if this is a 4.0+ index
		bool is40Index = MultiFields.getMergedFieldInfos(reader).fieldInfo("content5") != null;

		Bits liveDocs = MultiFields.getLiveDocs(reader);

		for (int i = 0;i < 35;i++)
		{
		  if (liveDocs.get(i))
		  {
			Document d = reader.document(i);
			IList<IndexableField> fields = d.Fields;
			bool isProxDoc = d.getField("content3") == null;
			if (isProxDoc)
			{
			  int numFields = is40Index ? 7 : 5;
			  Assert.AreEqual(numFields, fields.Count);
			  IndexableField f = d.getField("id");
			  Assert.AreEqual("" + i, f.stringValue());

			  f = d.getField("utf8");
			  Assert.AreEqual("Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\ud917\udc17cd", f.stringValue());

			  f = d.getField("autf8");
			  Assert.AreEqual("Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\ud917\udc17cd", f.stringValue());

			  f = d.getField("content2");
			  Assert.AreEqual("here is more content with aaa aaa aaa", f.stringValue());

			  f = d.getField("fie\u2C77ld");
			  Assert.AreEqual("field with non-ascii name", f.stringValue());
			}

			Fields tfvFields = reader.getTermVectors(i);
			Assert.IsNotNull( tfvFields, "i=" + i);
			Terms tfv = tfvFields.terms("utf8");
			Assert.IsNotNull( tfv, "docID=" + i + " index=" + oldName);
		  }
		  else
		  {
			// Only ID 7 is deleted
			Assert.AreEqual(7, i);
		  }
		}

		if (is40Index)
		{
		  // check docvalues fields
		  NumericDocValues dvByte = MultiDocValues.getNumericValues(reader, "dvByte");
		  BinaryDocValues dvBytesDerefFixed = MultiDocValues.getBinaryValues(reader, "dvBytesDerefFixed");
		  BinaryDocValues dvBytesDerefVar = MultiDocValues.getBinaryValues(reader, "dvBytesDerefVar");
		  SortedDocValues dvBytesSortedFixed = MultiDocValues.getSortedValues(reader, "dvBytesSortedFixed");
		  SortedDocValues dvBytesSortedVar = MultiDocValues.getSortedValues(reader, "dvBytesSortedVar");
		  BinaryDocValues dvBytesStraightFixed = MultiDocValues.getBinaryValues(reader, "dvBytesStraightFixed");
		  BinaryDocValues dvBytesStraightVar = MultiDocValues.getBinaryValues(reader, "dvBytesStraightVar");
		  NumericDocValues dvDouble = MultiDocValues.getNumericValues(reader, "dvDouble");
		  NumericDocValues dvFloat = MultiDocValues.getNumericValues(reader, "dvFloat");
		  NumericDocValues dvInt = MultiDocValues.getNumericValues(reader, "dvInt");
		  NumericDocValues dvLong = MultiDocValues.getNumericValues(reader, "dvLong");
		  NumericDocValues dvPacked = MultiDocValues.getNumericValues(reader, "dvPacked");
		  NumericDocValues dvShort = MultiDocValues.getNumericValues(reader, "dvShort");

		  for (int i = 0;i < 35;i++)
		  {
			int id = Convert.ToInt32(reader.document(i).get("id"));
			Assert.AreEqual(id, dvByte.get(i));

			sbyte[] bytes = new sbyte[] {(sbyte)((int)((uint)id >> 24)), (sbyte)((int)((uint)id >> 16)),(sbyte)((int)((uint)id >> 8)),(sbyte)id};
			BytesRef expectedRef = new BytesRef(bytes);
			BytesRef scratch = new BytesRef();

			dvBytesDerefFixed.get(i, scratch);
			Assert.AreEqual(expectedRef, scratch);
			dvBytesDerefVar.get(i, scratch);
			Assert.AreEqual(expectedRef, scratch);
			dvBytesSortedFixed.get(i, scratch);
			Assert.AreEqual(expectedRef, scratch);
			dvBytesSortedVar.get(i, scratch);
			Assert.AreEqual(expectedRef, scratch);
			dvBytesStraightFixed.get(i, scratch);
			Assert.AreEqual(expectedRef, scratch);
			dvBytesStraightVar.get(i, scratch);
			Assert.AreEqual(expectedRef, scratch);

			Assert.AreEqual((double)id, double.longBitsToDouble(dvDouble.get(i)), 0D);
			Assert.AreEqual((float)id, float.intBitsToFloat((int)dvFloat.get(i)), 0F);
			Assert.AreEqual(id, dvInt.get(i));
			Assert.AreEqual(id, dvLong.get(i));
			Assert.AreEqual(id, dvPacked.get(i));
			Assert.AreEqual(id, dvShort.get(i));
		  }
		}

		ScoreDoc[] hits = searcher.search(new TermQuery(new Term(new string("content"), "aaa")), null, 1000).scoreDocs;

		// First document should be #21 since it's norm was
		// increased:
		Document d = searcher.IndexReader.document(hits[0].doc);
		Assert.AreEqual("didn't get the right document first", "21", d.get("id"));

		DoTestHits(hits, 34, searcher.IndexReader);

		if (is40Index)
		{
		  hits = searcher.search(new TermQuery(new Term(new string("content5"), "aaa")), null, 1000).scoreDocs;

		  DoTestHits(hits, 34, searcher.IndexReader);

		  hits = searcher.search(new TermQuery(new Term(new string("content6"), "aaa")), null, 1000).scoreDocs;

		  DoTestHits(hits, 34, searcher.IndexReader);
		}

		hits = searcher.search(new TermQuery(new Term("utf8", "\u0000")), null, 1000).scoreDocs;
		Assert.AreEqual(34, hits.Length);
		hits = searcher.search(new TermQuery(new Term(new string("utf8"), "Lu\uD834\uDD1Ece\uD834\uDD60ne")), null, 1000).scoreDocs;
		Assert.AreEqual(34, hits.Length);
		hits = searcher.search(new TermQuery(new Term("utf8", "ab\ud917\udc17cd")), null, 1000).scoreDocs;
		Assert.AreEqual(34, hits.Length);

		reader.close();
	  }

	  private int Compare(string name, string v)
	  {
		int v0 = Convert.ToInt32(name.Substring(0, 2));
		int v1 = Convert.ToInt32(v);
		return v0 - v1;
	  }

	  public virtual void ChangeIndexWithAdds(Random random, Directory dir, string origOldName)
	  {
		// open writer
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).setOpenMode(OpenMode.APPEND));
		// add 10 docs
		for (int i = 0;i < 10;i++)
		{
		  AddDoc(writer, 35 + i);
		}

		// make sure writer sees right total -- writer seems not to know about deletes in .del?
		int expected;
		if (Compare(origOldName, "24") < 0)
		{
		  expected = 44;
		}
		else
		{
		  expected = 45;
		}
		Assert.AreEqual("wrong doc count", expected, writer.numDocs());
		writer.close();

		// make sure searching sees right # hits
		IndexReader reader = DirectoryReader.open(dir);
		IndexSearcher searcher = new IndexSearcher(reader);
		ScoreDoc[] hits = searcher.search(new TermQuery(new Term("content", "aaa")), null, 1000).scoreDocs;
		Document d = searcher.IndexReader.document(hits[0].doc);
		Assert.AreEqual("wrong first document", "21", d.get("id"));
		DoTestHits(hits, 44, searcher.IndexReader);
		reader.close();

		// fully merge
		writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).setOpenMode(OpenMode.APPEND));
		writer.forceMerge(1);
		writer.close();

		reader = DirectoryReader.open(dir);
		searcher = new IndexSearcher(reader);
		hits = searcher.search(new TermQuery(new Term("content", "aaa")), null, 1000).scoreDocs;
		Assert.AreEqual("wrong number of hits", 44, hits.Length);
		d = searcher.doc(hits[0].doc);
		DoTestHits(hits, 44, searcher.IndexReader);
		Assert.AreEqual("wrong first document", "21", d.get("id"));
		reader.close();
	  }

	  public virtual void ChangeIndexNoAdds(Random random, Directory dir)
	  {
		// make sure searching sees right # hits
		DirectoryReader reader = DirectoryReader.open(dir);
		IndexSearcher searcher = new IndexSearcher(reader);
		ScoreDoc[] hits = searcher.search(new TermQuery(new Term("content", "aaa")), null, 1000).scoreDocs;
		Assert.AreEqual("wrong number of hits", 34, hits.Length);
		Document d = searcher.doc(hits[0].doc);
		Assert.AreEqual("wrong first document", "21", d.get("id"));
		reader.close();

		// fully merge
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).setOpenMode(OpenMode.APPEND));
		writer.forceMerge(1);
		writer.close();

		reader = DirectoryReader.open(dir);
		searcher = new IndexSearcher(reader);
		hits = searcher.search(new TermQuery(new Term("content", "aaa")), null, 1000).scoreDocs;
		Assert.AreEqual("wrong number of hits", 34, hits.Length);
		DoTestHits(hits, 34, searcher.IndexReader);
		reader.close();
	  }

	  public virtual File CreateIndex(string dirName, bool doCFS, bool fullyMerged)
	  {
		// we use a real directory name that is not cleaned up, because this method is only used to create backwards indexes:
		File indexDir = new File("/tmp/4x", dirName);
		TestUtil.rm(indexDir);
		Directory dir = newFSDirectory(indexDir);
		LogByteSizeMergePolicy mp = new LogByteSizeMergePolicy();
		mp.NoCFSRatio = doCFS ? 1.0 : 0.0;
		mp.MaxCFSSegmentSizeMB = double.PositiveInfinity;
		// TODO: remove randomness
		IndexWriterConfig conf = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setMaxBufferedDocs(10).setMergePolicy(mp).setUseCompoundFile(doCFS);
		IndexWriter writer = new IndexWriter(dir, conf);

		for (int i = 0;i < 35;i++)
		{
		  AddDoc(writer, i);
		}
		Assert.AreEqual("wrong doc count", 35, writer.maxDoc());
		if (fullyMerged)
		{
		  writer.forceMerge(1);
		}
		writer.close();

		if (!fullyMerged)
		{
		  // open fresh writer so we get no prx file in the added segment
		  mp = new LogByteSizeMergePolicy();
		  mp.NoCFSRatio = doCFS ? 1.0 : 0.0;
		  // TODO: remove randomness
		  conf = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setMaxBufferedDocs(10).setMergePolicy(mp).setUseCompoundFile(doCFS);
		  writer = new IndexWriter(dir, conf);
		  AddNoProxDoc(writer);
		  writer.close();

		  writer = new IndexWriter(dir, conf.setMergePolicy(doCFS ? NoMergePolicy.COMPOUND_FILES : NoMergePolicy.NO_COMPOUND_FILES));
		  Term searchTerm = new Term("id", "7");
		  writer.deleteDocuments(searchTerm);
		  writer.close();
		}

		dir.close();

		return indexDir;
	  }

	  private void AddDoc(IndexWriter writer, int id)
	  {
		Document doc = new Document();
		doc.add(new TextField("content", "aaa", Field.Store.NO));
		doc.add(new StringField("id", Convert.ToString(id), Field.Store.YES));
		FieldType customType2 = new FieldType(TextField.TYPE_STORED);
		customType2.StoreTermVectors = true;
		customType2.StoreTermVectorPositions = true;
		customType2.StoreTermVectorOffsets = true;
		doc.add(new Field("autf8", "Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\ud917\udc17cd", customType2));
		doc.add(new Field("utf8", "Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\ud917\udc17cd", customType2));
		doc.add(new Field("content2", "here is more content with aaa aaa aaa", customType2));
		doc.add(new Field("fie\u2C77ld", "field with non-ascii name", customType2));
		// add numeric fields, to test if flex preserves encoding
		doc.add(new IntField("trieInt", id, Field.Store.NO));
		doc.add(new LongField("trieLong", (long) id, Field.Store.NO));
		// add docvalues fields
		doc.add(new NumericDocValuesField("dvByte", (sbyte) id));
		sbyte[] bytes = new sbyte[] {(sbyte)((int)((uint)id >> 24)), (sbyte)((int)((uint)id >> 16)),(sbyte)((int)((uint)id >> 8)),(sbyte)id};
		BytesRef @ref = new BytesRef(bytes);
		doc.add(new BinaryDocValuesField("dvBytesDerefFixed", @ref));
		doc.add(new BinaryDocValuesField("dvBytesDerefVar", @ref));
		doc.add(new SortedDocValuesField("dvBytesSortedFixed", @ref));
		doc.add(new SortedDocValuesField("dvBytesSortedVar", @ref));
		doc.add(new BinaryDocValuesField("dvBytesStraightFixed", @ref));
		doc.add(new BinaryDocValuesField("dvBytesStraightVar", @ref));
		doc.add(new DoubleDocValuesField("dvDouble", (double)id));
		doc.add(new FloatDocValuesField("dvFloat", (float)id));
		doc.add(new NumericDocValuesField("dvInt", id));
		doc.add(new NumericDocValuesField("dvLong", id));
		doc.add(new NumericDocValuesField("dvPacked", id));
		doc.add(new NumericDocValuesField("dvShort", (short)id));
		// a field with both offsets and term vectors for a cross-check
		FieldType customType3 = new FieldType(TextField.TYPE_STORED);
		customType3.StoreTermVectors = true;
		customType3.StoreTermVectorPositions = true;
		customType3.StoreTermVectorOffsets = true;
		customType3.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
		doc.add(new Field("content5", "here is more content with aaa aaa aaa", customType3));
		// a field that omits only positions
		FieldType customType4 = new FieldType(TextField.TYPE_STORED);
		customType4.StoreTermVectors = true;
		customType4.StoreTermVectorPositions = false;
		customType4.StoreTermVectorOffsets = true;
		customType4.IndexOptions = IndexOptions.DOCS_AND_FREQS;
		doc.add(new Field("content6", "here is more content with aaa aaa aaa", customType4));
		// TODO: 
		//   index different norms types via similarity (we use a random one currently?!)
		//   remove any analyzer randomness, explicitly add payloads for certain fields.
		writer.addDocument(doc);
	  }

	  private void AddNoProxDoc(IndexWriter writer)
	  {
		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_STORED);
		customType.IndexOptions = IndexOptions.DOCS_ONLY;
		Field f = new Field("content3", "aaa", customType);
		doc.add(f);
		FieldType customType2 = new FieldType();
		customType2.Stored = true;
		customType2.IndexOptions = IndexOptions.DOCS_ONLY;
		f = new Field("content4", "aaa", customType2);
		doc.add(f);
		writer.addDocument(doc);
	  }

	  private int CountDocs(DocsEnum docs)
	  {
		int count = 0;
		while ((docs.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
		{
		  count++;
		}
		return count;
	  }

	  // flex: test basics of TermsEnum api on non-flex index
	  public virtual void TestNextIntoWrongField()
	  {
		foreach (string name in OldNames)
		{
		  Directory dir = OldIndexDirs[name];
		  IndexReader r = DirectoryReader.open(dir);
		  TermsEnum terms = MultiFields.getFields(r).terms("content").iterator(null);
		  BytesRef t = terms.next();
		  Assert.IsNotNull(t);

		  // content field only has term aaa:
		  Assert.AreEqual("aaa", t.utf8ToString());
		  assertNull(terms.next());

		  BytesRef aaaTerm = new BytesRef("aaa");

		  // should be found exactly
		  Assert.AreEqual(TermsEnum.SeekStatus.FOUND, terms.seekCeil(aaaTerm));
		  Assert.AreEqual(35, CountDocs(TestUtil.docs(random(), terms, null, null, 0)));
		  assertNull(terms.next());

		  // should hit end of field
		  Assert.AreEqual(TermsEnum.SeekStatus.END, terms.seekCeil(new BytesRef("bbb")));
		  assertNull(terms.next());

		  // should seek to aaa
		  Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, terms.seekCeil(new BytesRef("a")));
		  Assert.IsTrue(terms.term().bytesEquals(aaaTerm));
		  Assert.AreEqual(35, CountDocs(TestUtil.docs(random(), terms, null, null, 0)));
		  assertNull(terms.next());

		  Assert.AreEqual(TermsEnum.SeekStatus.FOUND, terms.seekCeil(aaaTerm));
		  Assert.AreEqual(35, CountDocs(TestUtil.docs(random(), terms,null, null, 0)));
		  assertNull(terms.next());

		  r.close();
		}
	  }

	  /// <summary>
	  /// Test that we didn't forget to bump the current Constants.LUCENE_MAIN_VERSION.
	  /// this is important so that we can determine which version of lucene wrote the segment.
	  /// </summary>
	  public virtual void TestOldVersions()
	  {
		// first create a little index with the current code and get the version
		Directory currentDir = newDirectory();
		RandomIndexWriter riw = new RandomIndexWriter(random(), currentDir);
		riw.addDocument(new Document());
		riw.close();
		DirectoryReader ir = DirectoryReader.open(currentDir);
		SegmentReader air = (SegmentReader)ir.leaves().get(0).reader();
		string currentVersion = air.SegmentInfo.info.Version;
		Assert.IsNotNull(currentVersion); // only 3.0 segments can have a null version
		ir.close();
		currentDir.close();

		IComparer<string> comparator = StringHelper.VersionComparator;

		// now check all the old indexes, their version should be < the current version
		foreach (string name in OldNames)
		{
		  Directory dir = OldIndexDirs[name];
		  DirectoryReader r = DirectoryReader.open(dir);
		  foreach (AtomicReaderContext context in r.leaves())
		  {
			air = (SegmentReader) context.reader();
			string oldVersion = air.SegmentInfo.info.Version;
			// TODO: does preflex codec actually set "3.0" here? this is safe to do I think.
			// Assert.IsNotNull(oldVersion);
			Assert.IsTrue("current Constants.LUCENE_MAIN_VERSION is <= an old index: did you forget to bump it?!", oldVersion == null || comparator.Compare(oldVersion, currentVersion) < 0);
		  }
		  r.close();
		}
	  }

	  public virtual void TestNumericFields()
	  {
		foreach (string name in OldNames)
		{

		  Directory dir = OldIndexDirs[name];
		  IndexReader reader = DirectoryReader.open(dir);
		  IndexSearcher searcher = new IndexSearcher(reader);

		  for (int id = 10; id < 15; id++)
		  {
			ScoreDoc[] hits = searcher.search(NumericRangeQuery.newIntRange("trieInt", 4, Convert.ToInt32(id), Convert.ToInt32(id), true, true), 100).scoreDocs;
			Assert.AreEqual("wrong number of hits", 1, hits.Length);
			Document d = searcher.doc(hits[0].doc);
			Assert.AreEqual(Convert.ToString(id), d.get("id"));

			hits = searcher.search(NumericRangeQuery.newLongRange("trieLong", 4, Convert.ToInt64(id), Convert.ToInt64(id), true, true), 100).scoreDocs;
			Assert.AreEqual("wrong number of hits", 1, hits.Length);
			d = searcher.doc(hits[0].doc);
			Assert.AreEqual(Convert.ToString(id), d.get("id"));
		  }

		  // check that also lower-precision fields are ok
		  ScoreDoc[] hits = searcher.search(NumericRangeQuery.newIntRange("trieInt", 4, int.MinValue, int.MaxValue, false, false), 100).scoreDocs;
		  Assert.AreEqual("wrong number of hits", 34, hits.Length);

		  hits = searcher.search(NumericRangeQuery.newLongRange("trieLong", 4, long.MinValue, long.MaxValue, false, false), 100).scoreDocs;
		  Assert.AreEqual("wrong number of hits", 34, hits.Length);

		  // check decoding into field cache
		  FieldCache.Ints fci = FieldCache.DEFAULT.getInts(SlowCompositeReaderWrapper.wrap(searcher.IndexReader), "trieInt", false);
		  int maxDoc = searcher.IndexReader.maxDoc();
		  for (int doc = 0;doc < maxDoc;doc++)
		  {
			int val = fci.get(doc);
			Assert.IsTrue("value in id bounds", val >= 0 && val < 35);
		  }

		  FieldCache.Longs fcl = FieldCache.DEFAULT.getLongs(SlowCompositeReaderWrapper.wrap(searcher.IndexReader), "trieLong", false);
		  for (int doc = 0;doc < maxDoc;doc++)
		  {
			long val = fcl.get(doc);
			Assert.IsTrue("value in id bounds", val >= 0L && val < 35L);
		  }

		  reader.close();
		}
	  }

	  private int CheckAllSegmentsUpgraded(Directory dir)
	  {
		SegmentInfos infos = new SegmentInfos();
		infos.read(dir);
		if (VERBOSE)
		{
		  Console.WriteLine("checkAllSegmentsUpgraded: " + infos);
		}
		foreach (SegmentCommitInfo si in infos)
		{
		  Assert.AreEqual(Constants.LUCENE_MAIN_VERSION, si.info.Version);
		}
		return infos.size();
	  }

	  private int GetNumberOfSegments(Directory dir)
	  {
		SegmentInfos infos = new SegmentInfos();
		infos.read(dir);
		return infos.size();
	  }

	  public virtual void TestUpgradeOldIndex()
	  {
		IList<string> names = new List<string>(OldNames.Length + OldSingleSegmentNames.Length);
		names.AddRange(Arrays.asList(OldNames));
		names.AddRange(Arrays.asList(OldSingleSegmentNames));
		foreach (string name in names)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("testUpgradeOldIndex: index=" + name);
		  }
		  Directory dir = newDirectory(OldIndexDirs[name]);

		  (new IndexUpgrader(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, null), false)).upgrade();

		  CheckAllSegmentsUpgraded(dir);

		  dir.close();
		}
	  }

	  public virtual void TestUpgradeOldSingleSegmentIndexWithAdditions()
	  {
		foreach (string name in OldSingleSegmentNames)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("testUpgradeOldSingleSegmentIndexWithAdditions: index=" + name);
		  }
		  Directory dir = newDirectory(OldIndexDirs[name]);

		  Assert.AreEqual("Original index must be single segment", 1, GetNumberOfSegments(dir));

		  // create a bunch of dummy segments
		  int id = 40;
		  RAMDirectory ramDir = new RAMDirectory();
		  for (int i = 0; i < 3; i++)
		  {
			// only use Log- or TieredMergePolicy, to make document addition predictable and not suddenly merge:
			MergePolicy mp = random().nextBoolean() ? newLogMergePolicy() : newTieredMergePolicy();
			IndexWriterConfig iwc = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))).setMergePolicy(mp);
			IndexWriter w = new IndexWriter(ramDir, iwc);
			// add few more docs:
			for (int j = 0; j < RANDOM_MULTIPLIER * random().Next(30); j++)
			{
			  AddDoc(w, id++);
			}
			w.close(false);
		  }

		  // add dummy segments (which are all in current
		  // version) to single segment index
		  MergePolicy mp = random().nextBoolean() ? newLogMergePolicy() : newTieredMergePolicy();
		  IndexWriterConfig iwc = (new IndexWriterConfig(TEST_VERSION_CURRENT, null)).setMergePolicy(mp);
		  IndexWriter w = new IndexWriter(dir, iwc);
		  w.addIndexes(ramDir);
		  w.close(false);

		  // determine count of segments in modified index
		  int origSegCount = GetNumberOfSegments(dir);

		  (new IndexUpgrader(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, null), false)).upgrade();

		  int segCount = CheckAllSegmentsUpgraded(dir);
		  Assert.AreEqual("Index must still contain the same number of segments, as only one segment was upgraded and nothing else merged", origSegCount, segCount);

		  dir.close();
		}
	  }

	  public const string SurrogatesIndexName = "index.36.surrogates.zip";

	  public virtual void TestSurrogates()
	  {
		File oldIndexDir = createTempDir("surrogates");
		TestUtil.unzip(getDataFile(SurrogatesIndexName), oldIndexDir);
		Directory dir = newFSDirectory(oldIndexDir);
		// TODO: more tests
		TestUtil.checkIndex(dir);
		dir.close();
	  }

	  /* 
	   * Index with negative positions (LUCENE-1542)
	   * Created with this code, using a 2.4.0 jar, then upgraded with 3.6 upgrader:
	   *
	   * public class CreateBogusIndexes {
	   *   public static void main(String args[]) throws Exception {
	   *     Directory d = FSDirectory.getDirectory("/tmp/bogus24");
	   *     IndexWriter iw = new IndexWriter(d, new StandardAnalyzer());
	   *     Document doc = new Document();
	   *     Token brokenToken = new Token("broken", 0, 3);
	   *     brokenToken.setPositionIncrement(0);
	   *     Token okToken = new Token("ok", 0, 2);
	   *     doc.add(new Field("field1", new CannedTokenStream(brokenToken), Field.TermVector.NO));
	   *     doc.add(new Field("field2", new CannedTokenStream(brokenToken), Field.TermVector.WITH_POSITIONS));
	   *     doc.add(new Field("field3", new CannedTokenStream(brokenToken, okToken), Field.TermVector.NO));
	   *     doc.add(new Field("field4", new CannedTokenStream(brokenToken, okToken), Field.TermVector.WITH_POSITIONS));
	   *     iw.addDocument(doc);
	   *     doc = new Document();
	   *     doc.add(new Field("field1", "just more text, not broken", Field.Store.NO, Field.Index.ANALYZED));
	   *     doc.add(new Field("field2", "just more text, not broken", Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS));
	   *     doc.add(new Field("field3", "just more text, not broken", Field.Store.NO, Field.Index.ANALYZED));
	   *     doc.add(new Field("field4", "just more text, not broken", Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS));
	   *     iw.addDocument(doc);
	   *     iw.close();
	   *     d.close();
	   *   }
	   * 
	   *   static class CannedTokenStream extends TokenStream {
	   *     private final Token[] tokens;
	   *     private int upto = 0;
	   *  
	   *     CannedTokenStream(Token... tokens) {
	   *       this.tokens = tokens;
	   *     }
	   *  
	   *     @Override
	   *     public Token next() {
	   *       if (upto < tokens.length) {
	   *         return tokens[upto++];
	   *       } else {
	   *         return null;
	   *       }
	   *     }
	   *   }
	   * }
	   */
	  public const string Bogus24IndexName = "bogus24.upgraded.to.36.zip";

	  public virtual void TestNegativePositions()
	  {
		File oldIndexDir = createTempDir("negatives");
		TestUtil.unzip(getDataFile(Bogus24IndexName), oldIndexDir);
		Directory dir = newFSDirectory(oldIndexDir);
		DirectoryReader ir = DirectoryReader.open(dir);
		IndexSearcher @is = new IndexSearcher(ir);
		PhraseQuery pq = new PhraseQuery();
		pq.add(new Term("field3", "more"));
		pq.add(new Term("field3", "text"));
		TopDocs td = @is.search(pq, 10);
		Assert.AreEqual(1, td.totalHits);
		AtomicReader wrapper = SlowCompositeReaderWrapper.wrap(ir);
		DocsAndPositionsEnum de = wrapper.termPositionsEnum(new Term("field3", "broken"));
		Debug.Assert(de != null);
		Assert.AreEqual(0, de.nextDoc());
		Assert.AreEqual(0, de.nextPosition());
		ir.close();
		TestUtil.checkIndex(dir);
		dir.close();
	  }
	}

}