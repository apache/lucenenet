using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    using FileInfo = System.IO.FileInfo;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using BinaryDocValuesField = Lucene.Net.Documents.BinaryDocValuesField;
    using Document = Lucene.Net.Documents.Document;
    using DoubleDocValuesField = Lucene.Net.Documents.DoubleDocValuesField;
    using Field = Lucene.Net.Documents.Field;
    using FieldType = Lucene.Net.Documents.FieldType;
    using FloatDocValuesField = Lucene.Net.Documents.FloatDocValuesField;
    using IntField = Lucene.Net.Documents.IntField;
    using LongField = Lucene.Net.Documents.LongField;
    using NumericDocValuesField = Lucene.Net.Documents.NumericDocValuesField;
    using SortedDocValuesField = Lucene.Net.Documents.SortedDocValuesField;
    using SortedSetDocValuesField = Lucene.Net.Documents.SortedSetDocValuesField;
    using StringField = Lucene.Net.Documents.StringField;
    using TextField = Lucene.Net.Documents.TextField;
    using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions;
    using OpenMode_e = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using FieldCache = Lucene.Net.Search.FieldCache;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using NumericRangeQuery = Lucene.Net.Search.NumericRangeQuery;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using Directory = Lucene.Net.Store.Directory;
    using FSDirectory = Lucene.Net.Store.FSDirectory;
    using NIOFSDirectory = Lucene.Net.Store.NIOFSDirectory;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using SimpleFSDirectory = Lucene.Net.Store.SimpleFSDirectory;
    using Bits = Lucene.Net.Util.Bits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Constants = Lucene.Net.Util.Constants;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
    using StringHelper = Lucene.Net.Util.StringHelper;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /*
      Verify we can read the pre-5.0 file format, do searches
      against it, and add documents to it.
    */
    // note: add this if we make a 4.x impersonator
    // TODO: don't use 4.x codec, its unrealistic since it means
    // we won't even be running the actual code, only the impostor
    // @SuppressCodecs("Lucene4x")
    // Sep codec cannot yet handle the offsets in our 4.x index!
    [SuppressCodecs("Lucene3x", "MockFixedIntBlock", "MockVariableIntBlock", "MockSep", "MockRandom", "Lucene40", "Lucene41", "Appending", "Lucene42", "Lucene45")]
    [TestFixture]
    public class TestBackwardsCompatibility : LuceneTestCase
    {

        // Uncomment these cases & run them on an older Lucene version,
        // to generate indexes to test backwards compatibility.  These
        // indexes will be created under directory /tmp/idx/.
        //
        // However, you must first disable the Lucene TestSecurityManager,
        // which will otherwise disallow writing outside of the build/
        // directory - to do this, comment out the "java.security.manager"
        // <sysproperty> under the "test-macro" <macrodef>.
        //
        // Be sure to create the indexes with the actual format:
        //  ant test -Dtestcase=TestBackwardsCompatibility -Dversion=x.y.z
        //      -Dtests.codec=LuceneXY -Dtests.postingsformat=LuceneXY -Dtests.docvaluesformat=LuceneXY
        //
        // Zip up the generated indexes:
        //
        //    cd /tmp/idx/index.cfs   ; zip index.<VERSION>.cfs.zip *
        //    cd /tmp/idx/index.nocfs ; zip index.<VERSION>.nocfs.zip *
        //
        // Then move those 2 zip files to your trunk checkout and add them
        // to the oldNames array.

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
          // You don't need them to be build for non-4.0 (the test is happy with just one
          // "old" segment format, version is unimportant:
	  
          public void testCreateSingleSegmentCFS() throws IOException {
            createIndex("index.singlesegment.cfs", true, true);
          }
	
          public void testCreateSingleSegmentNoCFS() throws IOException {
            createIndex("index.singlesegment.nocfs", false, true);
          }
	
        */

        /*
        public void testCreateMoreTermsIndex() throws Exception {
          // we use a real directory name that is not cleaned up,
          // because this method is only used to create backwards
          // indexes:
          File indexDir = new File("moreterms");
          TestUtil.rmDir(indexDir);
          Directory dir = NewFSDirectory(indexDir);
	
          LogByteSizeMergePolicy mp = new LogByteSizeMergePolicy();
          mp.SetUseCompoundFile(false);
          mp.setNoCFSRatio(1.0);
          mp.setMaxCFSSegmentSizeMB(Double.POSITIVE_INFINITY);
          MockAnalyzer analyzer = new MockAnalyzer(Random());
          analyzer.setMaxTokenLength(TestUtil.nextInt(Random(), 1, IndexWriter.MAX_TERM_LENGTH));
	
          // TODO: remove randomness
          IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, analyzer)
            .SetMergePolicy(mp);
          conf.SetCodec(Codec.ForName("Lucene40"));
          IndexWriter writer = new IndexWriter(dir, conf);
          LineFileDocs docs = new LineFileDocs(null, true);
          for(int i=0;i<50;i++) {
            writer.AddDocument(docs.NextDoc());
          }
          writer.Dispose();
          dir.Dispose();
	
          // Gives you time to copy the index out!: (there is also
          // a test option to not remove temp dir...):
          Thread.sleep(100000);
        }
        */

        // LUCENENET specific to load resources for this type
        internal const string CURRENT_RESOURCE_DIRECTORY = "Lucene.Net.Tests.core.Index.";

        internal static readonly string[] OldNames = new string[] {
            "40.cfs", "40.nocfs", "41.cfs", "41.nocfs", "42.cfs",
            "42.nocfs", "45.cfs", "45.nocfs", "461.cfs", "461.nocfs"
        };

        internal readonly string[] UnsupportedNames = new string[] {
            "19.cfs", "19.nocfs", "20.cfs", "20.nocfs", "21.cfs",
            "21.nocfs", "22.cfs", "22.nocfs", "23.cfs", "23.nocfs",
            "24.cfs", "24.nocfs", "29.cfs", "29.nocfs"
        };

        internal static readonly string[] OldSingleSegmentNames = new string[] {
            "40.optimized.cfs", "40.optimized.nocfs"
        };

        internal static IDictionary<string, Directory> OldIndexDirs;

        /// <summary>
        /// Randomizes the use of some of hte constructor variations
        /// </summary>
        private IndexUpgrader NewIndexUpgrader(Directory dir)
        {
            bool streamType = Random().NextBoolean();
            int choice = TestUtil.NextInt(Random(), 0, 2);
            switch (choice)
            {
                case 0:
                    return new IndexUpgrader(dir, TEST_VERSION_CURRENT);
                case 1:
                    return new IndexUpgrader(dir, TEST_VERSION_CURRENT, streamType ? null : Console.Error, false);
                case 2:
                    return new IndexUpgrader(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null), false);
                default:
                    Assert.Fail("case statement didn't get updated when random bounds changed");
                    break;
            }
            return null; // never get here
        }

        [OneTimeSetUp]
        public void BeforeClass()
        {
            Assert.IsFalse(OLD_FORMAT_IMPERSONATION_IS_ACTIVE, "test infra is broken!");
            IList<string> names = new List<string>(OldNames.Length + OldSingleSegmentNames.Length);
            names.AddRange(Arrays.AsList(OldNames));
            names.AddRange(Arrays.AsList(OldSingleSegmentNames));
            OldIndexDirs = new Dictionary<string, Directory>();
            foreach (string name in names)
            {
                DirectoryInfo dir = CreateTempDir(name);
                using (Stream zipFileStream = this.GetType().Assembly.GetManifestResourceStream(CURRENT_RESOURCE_DIRECTORY + "index." + name + ".zip"))
                {
                    TestUtil.Unzip(zipFileStream, dir);
                }
                OldIndexDirs[name] = NewFSDirectory(dir);
            }
        }

        [OneTimeTearDown]
        public void AfterClass()
        {
            foreach (Directory d in OldIndexDirs.Values)
            {
                d.Dispose();
            }
            OldIndexDirs = null;
            base.TearDown();
        }

        public override void TearDown()
        {
            // LUCENENET: We don't want our temp directory deleted until after
            // all of the tests in the class run. So we need to override this and
            // call base.TearDown() manually during TestFixtureTearDown
        }

        /// <summary>
        /// this test checks that *only* IndexFormatTooOldExceptions are thrown when you open and operate on too old indexes! </summary>
        [Test]
        public virtual void TestUnsupportedOldIndexes()
        {
            for (int i = 0; i < UnsupportedNames.Length; i++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: index " + UnsupportedNames[i]);
                }
                DirectoryInfo oldIndxeDir = CreateTempDir(UnsupportedNames[i]);
                using (Stream dataFile = this.GetType().Assembly.GetManifestResourceStream(CURRENT_RESOURCE_DIRECTORY + "unsupported." + UnsupportedNames[i] + ".zip"))
                {
                    TestUtil.Unzip(dataFile, oldIndxeDir);
                }
                BaseDirectoryWrapper dir = NewFSDirectory(oldIndxeDir);
                // don't checkindex, these are intentionally not supported
                dir.CheckIndexOnClose = false;

                IndexReader reader = null;
                IndexWriter writer = null;
                try
                {
                    reader = DirectoryReader.Open(dir);
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
                        reader.Dispose();
                    }
                    reader = null;
                }

                try
                {
                    writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
                    Assert.Fail("IndexWriter creation should not pass for " + UnsupportedNames[i]);
                }
                catch (IndexFormatTooOldException e)
                {
                    // pass
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: got expected exc:");
                        Console.WriteLine(e.StackTrace);
                    }
                    // Make sure exc message includes a path=
                    Assert.IsTrue(e.Message.IndexOf("path=\"") != -1, "got exc message: " + e.Message);
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
                        writer.Dispose(false);
                    }
                    writer = null;
                }

                StringBuilder sb = new StringBuilder(1024);
                CheckIndex checker = new CheckIndex(dir);
                CheckIndex.Status indexStatus;
                using (var infoStream = new StringWriter(sb))
                {
                    checker.InfoStream = infoStream;
                    indexStatus = checker.DoCheckIndex();
                }
                Assert.IsFalse(indexStatus.Clean);
                Assert.IsTrue(sb.ToString().Contains(typeof(IndexFormatTooOldException).Name));

                dir.Dispose();
                TestUtil.Rm(oldIndxeDir);
            }
        }

        [Test]
        public virtual void TestFullyMergeOldIndex()
        {
            foreach (string name in OldNames)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: index=" + name);
                }
                Directory dir = NewDirectory(OldIndexDirs[name]);
                IndexWriter w = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
                w.ForceMerge(1);
                w.Dispose();

                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestAddOldIndexes()
        {
            foreach (string name in OldNames)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: old index " + name);
                }
                Directory targetDir = NewDirectory();
                IndexWriter w = new IndexWriter(targetDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
                w.AddIndexes(OldIndexDirs[name]);
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: done adding indices; now close");
                }
                w.Dispose();

                targetDir.Dispose();
            }
        }

        [Test]
        public virtual void TestAddOldIndexesReader()
        {
            foreach (string name in OldNames)
            {
                IndexReader reader = DirectoryReader.Open(OldIndexDirs[name]);

                Directory targetDir = NewDirectory();
                IndexWriter w = new IndexWriter(targetDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
                w.AddIndexes(reader);
                w.Dispose();
                reader.Dispose();

                targetDir.Dispose();
            }
        }

        [Test]
        public virtual void TestSearchOldIndex()
        {
            foreach (string name in OldNames)
            {
                SearchIndex(OldIndexDirs[name], name);
            }
        }

        [Test]
        public virtual void TestIndexOldIndexNoAdds()
        {
            foreach (string name in OldNames)
            {
                Directory dir = NewDirectory(OldIndexDirs[name]);
                ChangeIndexNoAdds(Random(), dir);
                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestIndexOldIndex()
        {
            foreach (string name in OldNames)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: oldName=" + name);
                }
                Directory dir = NewDirectory(OldIndexDirs[name]);
                ChangeIndexWithAdds(Random(), dir, name);
                dir.Dispose();
            }
        }

        private void DoTestHits(ScoreDoc[] hits, int expectedCount, IndexReader reader)
        {
            int hitCount = hits.Length;
            Assert.AreEqual(expectedCount, hitCount, "wrong number of hits");
            for (int i = 0; i < hitCount; i++)
            {
                reader.Document(hits[i].Doc);
                reader.GetTermVectors(hits[i].Doc);
            }
        }

        public virtual void SearchIndex(Directory dir, string oldName)
        {
            //QueryParser parser = new QueryParser("contents", new MockAnalyzer(random));
            //Query query = parser.parse("handle:1");

            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);

            TestUtil.CheckIndex(dir);

            // true if this is a 4.0+ index
            bool is40Index = MultiFields.GetMergedFieldInfos(reader).FieldInfo("content5") != null;
            // true if this is a 4.2+ index
            bool is42Index = MultiFields.GetMergedFieldInfos(reader).FieldInfo("dvSortedSet") != null;

            Debug.Assert(is40Index); // NOTE: currently we can only do this on trunk!

            Bits liveDocs = MultiFields.GetLiveDocs(reader);

            for (int i = 0; i < 35; i++)
            {
                if (liveDocs.Get(i))
                {
                    Document d = reader.Document(i);
                    IList<IndexableField> fields = d.Fields;
                    bool isProxDoc = d.GetField("content3") == null;
                    if (isProxDoc)
                    {
                        int numFields = is40Index ? 7 : 5;
                        Assert.AreEqual(numFields, fields.Count);
                        IndexableField f = d.GetField("id");
                        Assert.AreEqual("" + i, f.StringValue);

                        f = d.GetField("utf8");
                        Assert.AreEqual("Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\ud917\udc17cd", f.StringValue);

                        f = d.GetField("autf8");
                        Assert.AreEqual("Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\ud917\udc17cd", f.StringValue);

                        f = d.GetField("content2");
                        Assert.AreEqual("here is more content with aaa aaa aaa", f.StringValue);

                        f = d.GetField("fie\u2C77ld");
                        Assert.AreEqual("field with non-ascii name", f.StringValue);
                    }

                    Fields tfvFields = reader.GetTermVectors(i);
                    Assert.IsNotNull(tfvFields, "i=" + i);
                    Terms tfv = tfvFields.Terms("utf8");
                    Assert.IsNotNull(tfv, "docID=" + i + " index=" + oldName);
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
                NumericDocValues dvByte = MultiDocValues.GetNumericValues(reader, "dvByte");
                BinaryDocValues dvBytesDerefFixed = MultiDocValues.GetBinaryValues(reader, "dvBytesDerefFixed");
                BinaryDocValues dvBytesDerefVar = MultiDocValues.GetBinaryValues(reader, "dvBytesDerefVar");
                SortedDocValues dvBytesSortedFixed = MultiDocValues.GetSortedValues(reader, "dvBytesSortedFixed");
                SortedDocValues dvBytesSortedVar = MultiDocValues.GetSortedValues(reader, "dvBytesSortedVar");
                BinaryDocValues dvBytesStraightFixed = MultiDocValues.GetBinaryValues(reader, "dvBytesStraightFixed");
                BinaryDocValues dvBytesStraightVar = MultiDocValues.GetBinaryValues(reader, "dvBytesStraightVar");
                NumericDocValues dvDouble = MultiDocValues.GetNumericValues(reader, "dvDouble");
                NumericDocValues dvFloat = MultiDocValues.GetNumericValues(reader, "dvFloat");
                NumericDocValues dvInt = MultiDocValues.GetNumericValues(reader, "dvInt");
                NumericDocValues dvLong = MultiDocValues.GetNumericValues(reader, "dvLong");
                NumericDocValues dvPacked = MultiDocValues.GetNumericValues(reader, "dvPacked");
                NumericDocValues dvShort = MultiDocValues.GetNumericValues(reader, "dvShort");
                SortedSetDocValues dvSortedSet = null;
                if (is42Index)
                {
                    dvSortedSet = MultiDocValues.GetSortedSetValues(reader, "dvSortedSet");
                }

                for (int i = 0; i < 35; i++)
                {
                    int id = Convert.ToInt32(reader.Document(i).Get("id"));
                    Assert.AreEqual(id, dvByte.Get(i));

                    sbyte[] bytes = new sbyte[] { (sbyte)((int)((uint)id >> 24)), (sbyte)((int)((uint)id >> 16)), (sbyte)((int)((uint)id >> 8)), (sbyte)id };
                    BytesRef expectedRef = new BytesRef((byte[])(Array)bytes);
                    BytesRef scratch = new BytesRef();

                    dvBytesDerefFixed.Get(i, scratch);
                    Assert.AreEqual(expectedRef, scratch);
                    dvBytesDerefVar.Get(i, scratch);
                    Assert.AreEqual(expectedRef, scratch);
                    dvBytesSortedFixed.Get(i, scratch);
                    Assert.AreEqual(expectedRef, scratch);
                    dvBytesSortedVar.Get(i, scratch);
                    Assert.AreEqual(expectedRef, scratch);
                    dvBytesStraightFixed.Get(i, scratch);
                    Assert.AreEqual(expectedRef, scratch);
                    dvBytesStraightVar.Get(i, scratch);
                    Assert.AreEqual(expectedRef, scratch);

                    Assert.AreEqual((double)id, BitConverter.Int64BitsToDouble(dvDouble.Get(i)), 0D);
                    Assert.AreEqual((float)id, Number.IntBitsToFloat((int)dvFloat.Get(i)), 0F);
                    Assert.AreEqual(id, dvInt.Get(i));
                    Assert.AreEqual(id, dvLong.Get(i));
                    Assert.AreEqual(id, dvPacked.Get(i));
                    Assert.AreEqual(id, dvShort.Get(i));
                    if (is42Index)
                    {
                        dvSortedSet.Document = i;
                        long ord = dvSortedSet.NextOrd();
                        Assert.AreEqual(SortedSetDocValues.NO_MORE_ORDS, dvSortedSet.NextOrd());
                        dvSortedSet.LookupOrd(ord, scratch);
                        Assert.AreEqual(expectedRef, scratch);
                    }
                }
            }

            ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null, 1000).ScoreDocs;

            // First document should be #0
            Document doc = searcher.IndexReader.Document(hits[0].Doc);
            assertEquals("didn't get the right document first", "0", doc.Get("id"));

            DoTestHits(hits, 34, searcher.IndexReader);

            if (is40Index)
            {
                hits = searcher.Search(new TermQuery(new Term("content5", "aaa")), null, 1000).ScoreDocs;

                DoTestHits(hits, 34, searcher.IndexReader);

                hits = searcher.Search(new TermQuery(new Term("content6", "aaa")), null, 1000).ScoreDocs;

                DoTestHits(hits, 34, searcher.IndexReader);
            }

            hits = searcher.Search(new TermQuery(new Term("utf8", "\u0000")), null, 1000).ScoreDocs;
            Assert.AreEqual(34, hits.Length);
            hits = searcher.Search(new TermQuery(new Term("utf8", "lu\uD834\uDD1Ece\uD834\uDD60ne")), null, 1000).ScoreDocs;
            Assert.AreEqual(34, hits.Length);
            hits = searcher.Search(new TermQuery(new Term("utf8", "ab\ud917\udc17cd")), null, 1000).ScoreDocs;
            Assert.AreEqual(34, hits.Length);

            reader.Dispose();
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
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetOpenMode(OpenMode_e.APPEND).SetMergePolicy(NewLogMergePolicy()));
            // add 10 docs
            for (int i = 0; i < 10; i++)
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
            Assert.AreEqual(expected, writer.NumDocs(), "wrong doc count");
            writer.Dispose();

            // make sure searching sees right # hits
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null, 1000).ScoreDocs;
            Document d = searcher.IndexReader.Document(hits[0].Doc);
            assertEquals("wrong first document", "0", d.Get("id"));
            DoTestHits(hits, 44, searcher.IndexReader);
            reader.Dispose();

            // fully merge
            writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetOpenMode(OpenMode_e.APPEND).SetMergePolicy(NewLogMergePolicy()));
            writer.ForceMerge(1);
            writer.Dispose();

            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);
            hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null, 1000).ScoreDocs;
            Assert.AreEqual(44, hits.Length, "wrong number of hits");
            d = searcher.Doc(hits[0].Doc);
            DoTestHits(hits, 44, searcher.IndexReader);
            assertEquals("wrong first document", "0", d.Get("id"));
            reader.Dispose();
        }

        public virtual void ChangeIndexNoAdds(Random random, Directory dir)
        {
            // make sure searching sees right # hits
            DirectoryReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = NewSearcher(reader);
            ScoreDoc[] hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null, 1000).ScoreDocs;
            Assert.AreEqual(34, hits.Length, "wrong number of hits");
            Document d = searcher.Doc(hits[0].Doc);
            assertEquals("wrong first document", "0", d.Get("id"));
            reader.Dispose();

            // fully merge
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).SetOpenMode(OpenMode_e.APPEND));
            writer.ForceMerge(1);
            writer.Dispose();

            reader = DirectoryReader.Open(dir);
            searcher = NewSearcher(reader);
            hits = searcher.Search(new TermQuery(new Term("content", "aaa")), null, 1000).ScoreDocs;
            Assert.AreEqual(34, hits.Length, "wrong number of hits");
            DoTestHits(hits, 34, searcher.IndexReader);
            reader.Dispose();
        }

        public virtual DirectoryInfo CreateIndex(string dirName, bool doCFS, bool fullyMerged)
        {
            // we use a real directory name that is not cleaned up, because this method is only used to create backwards indexes:
            DirectoryInfo indexDir = new DirectoryInfo(Path.Combine("/tmp/idx/", dirName));
            TestUtil.Rm(indexDir);
            Directory dir = NewFSDirectory(indexDir);
            LogByteSizeMergePolicy mp = new LogByteSizeMergePolicy();
            mp.NoCFSRatio = doCFS ? 1.0 : 0.0;
            mp.MaxCFSSegmentSizeMB = double.PositiveInfinity;
            // TODO: remove randomness
            IndexWriterConfig conf = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetUseCompoundFile(doCFS).SetMaxBufferedDocs(10).SetMergePolicy(mp);
            IndexWriter writer = new IndexWriter(dir, conf);

            for (int i = 0; i < 35; i++)
            {
                AddDoc(writer, i);
            }
            Assert.AreEqual(35, writer.MaxDoc, "wrong doc count");
            if (fullyMerged)
            {
                writer.ForceMerge(1);
            }
            writer.Dispose();

            if (!fullyMerged)
            {
                // open fresh writer so we get no prx file in the added segment
                mp = new LogByteSizeMergePolicy();
                mp.NoCFSRatio = doCFS ? 1.0 : 0.0;
                // TODO: remove randomness
                conf = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetUseCompoundFile(doCFS).SetMaxBufferedDocs(10).SetMergePolicy(mp);
                writer = new IndexWriter(dir, conf);
                AddNoProxDoc(writer);
                writer.Dispose();

                conf = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetUseCompoundFile(doCFS).SetMaxBufferedDocs(10).SetMergePolicy(doCFS ? NoMergePolicy.COMPOUND_FILES : NoMergePolicy.NO_COMPOUND_FILES);
                writer = new IndexWriter(dir, conf);
                Term searchTerm = new Term("id", "7");
                writer.DeleteDocuments(searchTerm);
                writer.Dispose();
            }

            dir.Dispose();

            return indexDir;
        }

        private void AddDoc(IndexWriter writer, int id)
        {
            Document doc = new Document();
            doc.Add(new TextField("content", "aaa", Field.Store.NO));
            doc.Add(new StringField("id", Convert.ToString(id), Field.Store.YES));
            FieldType customType2 = new FieldType(TextField.TYPE_STORED);
            customType2.StoreTermVectors = true;
            customType2.StoreTermVectorPositions = true;
            customType2.StoreTermVectorOffsets = true;
            doc.Add(new Field("autf8", "Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\ud917\udc17cd", customType2));
            doc.Add(new Field("utf8", "Lu\uD834\uDD1Ece\uD834\uDD60ne \u0000 \u2620 ab\ud917\udc17cd", customType2));
            doc.Add(new Field("content2", "here is more content with aaa aaa aaa", customType2));
            doc.Add(new Field("fie\u2C77ld", "field with non-ascii name", customType2));
            // add numeric fields, to test if flex preserves encoding
            doc.Add(new IntField("trieInt", id, Field.Store.NO));
            doc.Add(new LongField("trieLong", (long)id, Field.Store.NO));
            // add docvalues fields
            doc.Add(new NumericDocValuesField("dvByte", (sbyte)id));
            sbyte[] bytes = new sbyte[] { (sbyte)((int)((uint)id >> 24)), (sbyte)((int)((uint)id >> 16)), (sbyte)((int)((uint)id >> 8)), (sbyte)id };
            BytesRef @ref = new BytesRef((byte[])(Array)bytes);
            doc.Add(new BinaryDocValuesField("dvBytesDerefFixed", @ref));
            doc.Add(new BinaryDocValuesField("dvBytesDerefVar", @ref));
            doc.Add(new SortedDocValuesField("dvBytesSortedFixed", @ref));
            doc.Add(new SortedDocValuesField("dvBytesSortedVar", @ref));
            doc.Add(new BinaryDocValuesField("dvBytesStraightFixed", @ref));
            doc.Add(new BinaryDocValuesField("dvBytesStraightVar", @ref));
            doc.Add(new DoubleDocValuesField("dvDouble", (double)id));
            doc.Add(new FloatDocValuesField("dvFloat", (float)id));
            doc.Add(new NumericDocValuesField("dvInt", id));
            doc.Add(new NumericDocValuesField("dvLong", id));
            doc.Add(new NumericDocValuesField("dvPacked", id));
            doc.Add(new NumericDocValuesField("dvShort", (short)id));
            doc.Add(new SortedSetDocValuesField("dvSortedSet", @ref));
            // a field with both offsets and term vectors for a cross-check
            FieldType customType3 = new FieldType(TextField.TYPE_STORED);
            customType3.StoreTermVectors = true;
            customType3.StoreTermVectorPositions = true;
            customType3.StoreTermVectorOffsets = true;
            customType3.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            doc.Add(new Field("content5", "here is more content with aaa aaa aaa", customType3));
            // a field that omits only positions
            FieldType customType4 = new FieldType(TextField.TYPE_STORED);
            customType4.StoreTermVectors = true;
            customType4.StoreTermVectorPositions = false;
            customType4.StoreTermVectorOffsets = true;
            customType4.IndexOptions = IndexOptions.DOCS_AND_FREQS;
            doc.Add(new Field("content6", "here is more content with aaa aaa aaa", customType4));
            // TODO: 
            //   index different norms types via similarity (we use a random one currently?!)
            //   remove any analyzer randomness, explicitly add payloads for certain fields.
            writer.AddDocument(doc);
        }

        private void AddNoProxDoc(IndexWriter writer)
        {
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.IndexOptions = IndexOptions.DOCS_ONLY;
            Field f = new Field("content3", "aaa", customType);
            doc.Add(f);
            FieldType customType2 = new FieldType();
            customType2.Stored = true;
            customType2.IndexOptions = IndexOptions.DOCS_ONLY;
            f = new Field("content4", "aaa", customType2);
            doc.Add(f);
            writer.AddDocument(doc);
        }

        private int CountDocs(DocsEnum docs)
        {
            int count = 0;
            while ((docs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                count++;
            }
            return count;
        }

        // flex: test basics of TermsEnum api on non-flex index
        [Test]
        public virtual void TestNextIntoWrongField()
        {
            foreach (string name in OldNames)
            {
                Directory dir = OldIndexDirs[name];
                IndexReader r = DirectoryReader.Open(dir);
                TermsEnum terms = MultiFields.GetFields(r).Terms("content").Iterator(null);
                BytesRef t = terms.Next();
                Assert.IsNotNull(t);

                // content field only has term aaa:
                Assert.AreEqual("aaa", t.Utf8ToString());
                Assert.IsNull(terms.Next());

                BytesRef aaaTerm = new BytesRef("aaa");

                // should be found exactly
                Assert.AreEqual(TermsEnum.SeekStatus.FOUND, terms.SeekCeil(aaaTerm));
                Assert.AreEqual(35, CountDocs(TestUtil.Docs(Random(), terms, null, null, DocsEnum.FLAG_NONE)));
                Assert.IsNull(terms.Next());

                // should hit end of field
                Assert.AreEqual(TermsEnum.SeekStatus.END, terms.SeekCeil(new BytesRef("bbb")));
                Assert.IsNull(terms.Next());

                // should seek to aaa
                Assert.AreEqual(TermsEnum.SeekStatus.NOT_FOUND, terms.SeekCeil(new BytesRef("a")));
                Assert.IsTrue(terms.Term().BytesEquals(aaaTerm));
                Assert.AreEqual(35, CountDocs(TestUtil.Docs(Random(), terms, null, null, DocsEnum.FLAG_NONE)));
                Assert.IsNull(terms.Next());

                Assert.AreEqual(TermsEnum.SeekStatus.FOUND, terms.SeekCeil(aaaTerm));
                Assert.AreEqual(35, CountDocs(TestUtil.Docs(Random(), terms, null, null, DocsEnum.FLAG_NONE)));
                Assert.IsNull(terms.Next());

                r.Dispose();
            }
        }

        /// <summary>
        /// Test that we didn't forget to bump the current Constants.LUCENE_MAIN_VERSION.
        /// this is important so that we can determine which version of lucene wrote the segment.
        /// </summary>
        [Test]
        public virtual void TestOldVersions()
        {
            // first create a little index with the current code and get the version
            Directory currentDir = NewDirectory();
            RandomIndexWriter riw = new RandomIndexWriter(Random(), currentDir, Similarity, TimeZone);
            riw.AddDocument(new Document());
            riw.Dispose();
            DirectoryReader ir = DirectoryReader.Open(currentDir);
            SegmentReader air = (SegmentReader)ir.Leaves[0].Reader;
            string currentVersion = air.SegmentInfo.Info.Version;
            Assert.IsNotNull(currentVersion); // only 3.0 segments can have a null version
            ir.Dispose();
            currentDir.Dispose();

            IComparer<string> comparator = StringHelper.VersionComparator;

            // now check all the old indexes, their version should be < the current version
            foreach (string name in OldNames)
            {
                Directory dir = OldIndexDirs[name];
                DirectoryReader r = DirectoryReader.Open(dir);
                foreach (AtomicReaderContext context in r.Leaves)
                {
                    air = (SegmentReader)context.Reader;
                    string oldVersion = air.SegmentInfo.Info.Version;
                    Assert.IsNotNull(oldVersion); // only 3.0 segments can have a null version
                    Assert.IsTrue(comparator.Compare(oldVersion, currentVersion) < 0, "current Constants.LUCENE_MAIN_VERSION is <= an old index: did you forget to bump it?!");
                }
                r.Dispose();
            }
        }

        [Test]
        public virtual void TestNumericFields()
        {
            foreach (string name in OldNames)
            {

                Directory dir = OldIndexDirs[name];
                IndexReader reader = DirectoryReader.Open(dir);
                IndexSearcher searcher = NewSearcher(reader);

                for (int id = 10; id < 15; id++)
                {
                    ScoreDoc[] hits = searcher.Search(NumericRangeQuery.NewIntRange("trieInt", 4, Convert.ToInt32(id), Convert.ToInt32(id), true, true), 100).ScoreDocs;
                    Assert.AreEqual(1, hits.Length, "wrong number of hits");
                    Document d = searcher.Doc(hits[0].Doc);
                    Assert.AreEqual(Convert.ToString(id), d.Get("id"));

                    hits = searcher.Search(NumericRangeQuery.NewLongRange("trieLong", 4, Convert.ToInt64(id), Convert.ToInt64(id), true, true), 100).ScoreDocs;
                    Assert.AreEqual(1, hits.Length, "wrong number of hits");
                    d = searcher.Doc(hits[0].Doc);
                    Assert.AreEqual(Convert.ToString(id), d.Get("id"));
                }

                // check that also lower-precision fields are ok
                ScoreDoc[] hits_ = searcher.Search(NumericRangeQuery.NewIntRange("trieInt", 4, int.MinValue, int.MaxValue, false, false), 100).ScoreDocs;
                Assert.AreEqual(34, hits_.Length, "wrong number of hits");

                hits_ = searcher.Search(NumericRangeQuery.NewLongRange("trieLong", 4, long.MinValue, long.MaxValue, false, false), 100).ScoreDocs;
                Assert.AreEqual(34, hits_.Length, "wrong number of hits");

                // check decoding into field cache
                FieldCache.Ints fci = FieldCache.DEFAULT.GetInts(SlowCompositeReaderWrapper.Wrap(searcher.IndexReader), "trieInt", false);
                int maxDoc = searcher.IndexReader.MaxDoc;
                for (int doc = 0; doc < maxDoc; doc++)
                {
                    int val = fci.Get(doc);
                    Assert.IsTrue(val >= 0 && val < 35, "value in id bounds");
                }

                FieldCache.Longs fcl = FieldCache.DEFAULT.GetLongs(SlowCompositeReaderWrapper.Wrap(searcher.IndexReader), "trieLong", false);
                for (int doc = 0; doc < maxDoc; doc++)
                {
                    long val = fcl.Get(doc);
                    Assert.IsTrue(val >= 0L && val < 35L, "value in id bounds");
                }

                reader.Dispose();
            }
        }

        private int CheckAllSegmentsUpgraded(Directory dir)
        {
            SegmentInfos infos = new SegmentInfos();
            infos.Read(dir);
            if (VERBOSE)
            {
                Console.WriteLine("checkAllSegmentsUpgraded: " + infos);
            }
            foreach (SegmentCommitInfo si in infos.Segments)
            {
                Assert.AreEqual(Constants.LUCENE_MAIN_VERSION, si.Info.Version);
            }
            return infos.Size();
        }

        private int GetNumberOfSegments(Directory dir)
        {
            SegmentInfos infos = new SegmentInfos();
            infos.Read(dir);
            return infos.Size();
        }

        [Test]
        public virtual void TestUpgradeOldIndex()
        {
            IList<string> names = new List<string>(OldNames.Length + OldSingleSegmentNames.Length);
            names.AddRange(Arrays.AsList(OldNames));
            names.AddRange(Arrays.AsList(OldSingleSegmentNames));
            foreach (string name in names)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("testUpgradeOldIndex: index=" + name);
                }
                Directory dir = NewDirectory(OldIndexDirs[name]);

                NewIndexUpgrader(dir).Upgrade();

                CheckAllSegmentsUpgraded(dir);

                dir.Dispose();
            }
        }

        [Test]
        public virtual void TestCommandLineArgs()
        {

            foreach (string name in OldIndexDirs.Keys)
            {
                DirectoryInfo dir = CreateTempDir(name);
                using (Stream dataFile = this.GetType().Assembly.GetManifestResourceStream(CURRENT_RESOURCE_DIRECTORY + "index." + name + ".zip"))
                {
                    TestUtil.Unzip(dataFile, dir);
                }

                string path = dir.FullName;

                IList<string> args = new List<string>();
                if (Random().NextBoolean())
                {
                    args.Add("-verbose");
                }
                if (Random().NextBoolean())
                {
                    args.Add("-delete-prior-commits");
                }
                if (Random().NextBoolean())
                {
                    // TODO: need to better randomize this, but ...
                    //  - LuceneTestCase.FS_DIRECTORIES is private
                    //  - newFSDirectory returns BaseDirectoryWrapper
                    //  - BaseDirectoryWrapper doesn't expose delegate
                    Type dirImpl = Random().NextBoolean() ? typeof(SimpleFSDirectory) : typeof(NIOFSDirectory);

                    args.Add("-dir-impl");
                    args.Add(dirImpl.Name);
                }
                args.Add(path);

                IndexUpgrader upgrader = null;
                try
                {
                    upgrader = IndexUpgrader.ParseArgs(args.ToArray());
                }
                catch (Exception e)
                {
                    throw new Exception("unable to parse args: " + args, e);
                }
                upgrader.Upgrade();

                Directory upgradedDir = NewFSDirectory(dir);
                try
                {
                    CheckAllSegmentsUpgraded(upgradedDir);
                }
                finally
                {
                    upgradedDir.Dispose();
                }
            }
        }

        [Test]
        public virtual void TestUpgradeOldSingleSegmentIndexWithAdditions()
        {
            foreach (string name in OldSingleSegmentNames)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("testUpgradeOldSingleSegmentIndexWithAdditions: index=" + name);
                }
                Directory dir = NewDirectory(OldIndexDirs[name]);

                Assert.AreEqual(1, GetNumberOfSegments(dir), "Original index must be single segment");

                // create a bunch of dummy segments
                int id = 40;
                RAMDirectory ramDir = new RAMDirectory();
                for (int i = 0; i < 3; i++)
                {
                    // only use Log- or TieredMergePolicy, to make document addition predictable and not suddenly merge:
                    MergePolicy mp = Random().NextBoolean() ? (MergePolicy)NewLogMergePolicy() : NewTieredMergePolicy();
                    IndexWriterConfig iwc = (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))).SetMergePolicy(mp);
                    IndexWriter w = new IndexWriter(ramDir, iwc);
                    // add few more docs:
                    for (int j = 0; j < RANDOM_MULTIPLIER * Random().Next(30); j++)
                    {
                        AddDoc(w, id++);
                    }
                    w.Dispose(false);
                }

                // add dummy segments (which are all in current
                // version) to single segment index
                MergePolicy mp_ = Random().NextBoolean() ? (MergePolicy)NewLogMergePolicy() : NewTieredMergePolicy();
                IndexWriterConfig iwc_ = (new IndexWriterConfig(TEST_VERSION_CURRENT, null)).SetMergePolicy(mp_);
                IndexWriter iw = new IndexWriter(dir, iwc_);
                iw.AddIndexes(ramDir);
                iw.Dispose(false);

                // determine count of segments in modified index
                int origSegCount = GetNumberOfSegments(dir);

                NewIndexUpgrader(dir).Upgrade();

                int segCount = CheckAllSegmentsUpgraded(dir);
                Assert.AreEqual(origSegCount, segCount, "Index must still contain the same number of segments, as only one segment was upgraded and nothing else merged");

                dir.Dispose();
            }
        }

        public const string MoreTermsIndex = "moreterms.40.zip";

        [Test]
        public virtual void TestMoreTerms()
        {
            DirectoryInfo oldIndexDir = CreateTempDir("moreterms");
            using (Stream dataFile = this.GetType().Assembly.GetManifestResourceStream(CURRENT_RESOURCE_DIRECTORY + MoreTermsIndex))
            {
                TestUtil.Unzip(dataFile, oldIndexDir);
            }
            Directory dir = NewFSDirectory(oldIndexDir);
            // TODO: more tests
            TestUtil.CheckIndex(dir);
            dir.Dispose();
        }
    }
}