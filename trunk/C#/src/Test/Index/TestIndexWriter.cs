/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Hits = Lucene.Net.Search.Hits;
using TermQuery = Lucene.Net.Search.TermQuery;
using Directory = Lucene.Net.Store.Directory;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using MockRAMDirectory = Lucene.Net.Store.MockRAMDirectory;
using LockFactory = Lucene.Net.Store.LockFactory;
using Lock = Lucene.Net.Store.Lock;
using SingleInstanceLockFactory = Lucene.Net.Store.SingleInstanceLockFactory;

namespace Lucene.Net.Index
{
	
	
	/// <author>  goller
	/// </author>
    /// <version>  $Id: TestIndexWriter.java 387550 2006-03-21 15:36:32Z yonik $
    /// </version>
	[TestFixture]
    public class TestIndexWriter
	{
        [Serializable]
        public class MyRAMDirectory : RAMDirectory
        {
            private void  InitBlock(TestIndexWriter enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private TestIndexWriter enclosingInstance;
            public TestIndexWriter Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }
				
            }
            private LockFactory myLockFactory;
            internal MyRAMDirectory(TestIndexWriter enclosingInstance)
            {
                InitBlock(enclosingInstance);
                lockFactory = null;
                myLockFactory = new SingleInstanceLockFactory();
            }
            public override Lock MakeLock(System.String name)
            {
                return myLockFactory.MakeLock(name);
            }
        }

		[Test]
        public virtual void  TestDocCount()
		{
			Directory dir = new RAMDirectory();
			
			IndexWriter writer = null;
			IndexReader reader = null;
			int i;
			
            IndexWriter.SetDefaultWriteLockTimeout(2000);
            Assert.AreEqual(2000, IndexWriter.GetDefaultWriteLockTimeout());
			
            writer = new IndexWriter(dir, new WhitespaceAnalyzer());
			
            IndexWriter.SetDefaultWriteLockTimeout(1000);
			
            // add 100 documents
			for (i = 0; i < 100; i++)
			{
				AddDoc(writer);
			}
			Assert.AreEqual(100, writer.DocCount());
			writer.Close();
			
			// delete 40 documents
			reader = IndexReader.Open(dir);
			for (i = 0; i < 40; i++)
			{
				reader.DeleteDocument(i);
			}
			reader.Close();
			
			// test doc count before segments are merged/index is optimized
			writer = new IndexWriter(dir, new WhitespaceAnalyzer());
			Assert.AreEqual(100, writer.DocCount());
			writer.Close();
			
			reader = IndexReader.Open(dir);
			Assert.AreEqual(100, reader.MaxDoc());
			Assert.AreEqual(60, reader.NumDocs());
			reader.Close();
			
			// optimize the index and check that the new doc count is correct
			writer = new IndexWriter(dir, new WhitespaceAnalyzer());
			writer.Optimize();
			Assert.AreEqual(60, writer.DocCount());
			writer.Close();
			
			// check that the index reader gives the same numbers.
			reader = IndexReader.Open(dir);
			Assert.AreEqual(60, reader.MaxDoc());
			Assert.AreEqual(60, reader.NumDocs());
			reader.Close();
			
            // make sure opening a new index for create over
            // this existing one works correctly:
            writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
            Assert.AreEqual(0, writer.DocCount());
            writer.Close();
        }
		
		private void  AddDoc(IndexWriter writer)
		{
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
			doc.Add(new Field("content", "aaa", Field.Store.NO, Field.Index.TOKENIZED));
			writer.AddDocument(doc);
		}
		
        private void  AddDocWithIndex(IndexWriter writer, int index)
        {
            Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
            doc.Add(new Field("content", "aaa " + index, Field.Store.YES, Field.Index.TOKENIZED));
            doc.Add(new Field("id", "" + index, Field.Store.YES, Field.Index.TOKENIZED));
            writer.AddDocument(doc);
        }
		
        /*
        Test: make sure when we run out of disk space or hit
        random IOExceptions in any of the addIndexes(*) calls
        that 1) index is not corrupt (searcher can open/search
        it) and 2) transactional semantics are followed:
        either all or none of the incoming documents were in
        fact added.
        */
        [Test]
        public virtual void  TestAddIndexOnDiskFull()
        {
			
            int START_COUNT = 57;
            int NUM_DIR = 50;
            int END_COUNT = START_COUNT + NUM_DIR * 25;
			
            bool debug = false;
			
            // Build up a bunch of dirs that have indexes which we
            // will then merge together by calling addIndexes(*):
            Directory[] dirs = new Directory[NUM_DIR];
            long inputDiskUsage = 0;
            for (int i = 0; i < NUM_DIR; i++)
            {
                dirs[i] = new RAMDirectory();
                IndexWriter writer = new IndexWriter(dirs[i], new WhitespaceAnalyzer(), true);
                for (int j = 0; j < 25; j++)
                {
                    AddDocWithIndex(writer, 25 * i + j);
                }
                writer.Close();
                System.String[] files = dirs[i].List();
                for (int j = 0; j < files.Length; j++)
                {
                    inputDiskUsage += dirs[i].FileLength(files[j]);
                }
            }
			
            // Now, build a starting index that has START_COUNT docs.  We
            // will then try to addIndexes into a copy of this:
            RAMDirectory startDir = new RAMDirectory();
            IndexWriter writer2 = new IndexWriter(startDir, new WhitespaceAnalyzer(), true);
            for (int j = 0; j < START_COUNT; j++)
            {
                AddDocWithIndex(writer2, j);
            }
            writer2.Close();
			
            // Make sure starting index seems to be working properly:
            Term searchTerm = new Term("content", "aaa");
            IndexReader reader = IndexReader.Open(startDir);
            Assert.AreEqual(57, reader.DocFreq(searchTerm), "first docFreq");
			
            IndexSearcher searcher = new IndexSearcher(reader);
            Hits hits = searcher.Search(new TermQuery(searchTerm));
            Assert.AreEqual(57, hits.Length(), "first number of hits");
            searcher.Close();
            reader.Close();
			
            // Iterate with larger and larger amounts of free
            // disk space.  With little free disk space,
            // addIndexes will certainly run out of space &
            // fail.  Verify that when this happens, index is
            // not corrupt and index in fact has added no
            // documents.  Then, we increase disk space by 1000
            // bytes each iteration.  At some point there is
            // enough free disk space and addIndexes should
            // succeed and index should show all documents were
            // added.
			
            // String[] files = startDir.list();
            long diskUsage = startDir.SizeInBytes();
			
            long startDiskUsage = 0;
            System.String[] files2 = startDir.List();
            for (int i = 0; i < files2.Length; i++)
            {
                startDiskUsage += startDir.FileLength(files2[i]);
            }
			
            for (int method = 0; method < 3; method++)
            {
				
                // Start with 100 bytes more than we are currently using:
                long diskFree = diskUsage + 100;
				
                bool success = false;
                bool done = false;
				
                System.String methodName;
                if (0 == method)
                {
                    methodName = "addIndexes(Directory[])";
                }
                else if (1 == method)
                {
                    methodName = "addIndexes(IndexReader[])";
                }
                else
                {
                    methodName = "addIndexesNoOptimize(Directory[])";
                }
				
                System.String testName = "disk full test for method " + methodName + " with disk full at " + diskFree + " bytes";
				
                int cycleCount = 0;
				
                while (!done)
                {
					
                    cycleCount++;
					
                    // Make a new dir that will enforce disk usage:
                    MockRAMDirectory dir = new MockRAMDirectory(startDir);
                    writer2 = new IndexWriter(dir, new WhitespaceAnalyzer(), false);
                    System.IO.IOException err = null;
					
                    for (int x = 0; x < 2; x++)
                    {
						
                        // Two loops: first time, limit disk space &
                        // throw random IOExceptions; second time, no
                        // disk space limit:
						
                        double rate = 0.05;
                        double diskRatio = ((double) diskFree) / diskUsage;
                        long thisDiskFree;
						
                        if (0 == x)
                        {
                            thisDiskFree = diskFree;
                            if (diskRatio >= 2.0)
                            {
                                rate /= 2;
                            }
                            if (diskRatio >= 4.0)
                            {
                                rate /= 2;
                            }
                            if (diskRatio >= 6.0)
                            {
                                rate = 0.0;
                            }
                            if (debug)
                            {
                                System.Console.Out.WriteLine("\ncycle: " + methodName + ": " + diskFree + " bytes");
                            }
                        }
                        else
                        {
                            thisDiskFree = 0;
                            rate = 0.0;
                            if (debug)
                            {
                                System.Console.Out.WriteLine("\ncycle: " + methodName + ", same writer: unlimited disk space");
                            }
                        }
						
                        dir.SetMaxSizeInBytes(thisDiskFree);
                        dir.SetRandomIOExceptionRate(rate, diskFree);
						
                        try
                        {
							
                            if (0 == method)
                            {
                                writer2.AddIndexes(dirs);
                            }
                            else if (1 == method)
                            {
                                IndexReader[] readers = new IndexReader[dirs.Length];
                                for (int i = 0; i < dirs.Length; i++)
                                {
                                    readers[i] = IndexReader.Open(dirs[i]);
                                }
                                try
                                {
                                    writer2.AddIndexes(readers);
                                }
                                finally
                                {
                                    for (int i = 0; i < dirs.Length; i++)
                                    {
                                        readers[i].Close();
                                    }
                                }
                            }
                            else
                            {
                                writer2.AddIndexesNoOptimize(dirs);
                            }
							
                            success = true;
                            if (debug)
                            {
                                System.Console.Out.WriteLine("  success!");
                            }
							
                            if (0 == x)
                            {
                                done = true;
                            }
                        }
                        catch (System.IO.IOException e)
                        {
                            success = false;
                            err = e;
                            if (debug)
                            {
                                System.Console.Out.WriteLine("  hit IOException: " + e);
                            }
							
                            if (1 == x)
                            {
                                System.Console.Error.WriteLine(e.StackTrace);
                                Assert.Fail(methodName + " hit IOException after disk space was freed up");
                            }
                        }
						
                        // Whether we succeeded or failed, check that all
                        // un-referenced files were in fact deleted (ie,
                        // we did not create garbage).  Just create a
                        // new IndexFileDeleter, have it delete
                        // unreferenced files, then verify that in fact
                        // no files were deleted:
                        System.String[] startFiles = dir.List();
                        SegmentInfos infos = new SegmentInfos();
                        infos.Read(dir);
                        IndexFileDeleter d = new IndexFileDeleter(infos, dir);
                        d.FindDeletableFiles();
                        d.DeleteFiles();
                        System.String[] endFiles = dir.List();
						
                        System.Array.Sort(startFiles);
                        System.Array.Sort(endFiles);
						
                        /*
                        for(int i=0;i<startFiles.length;i++) {
                        System.out.println("  " + i + ": " + startFiles[i]);
                        }
                        */
						
                        if (Test.SupportClass.Compare.CompareStringArrays(startFiles, endFiles) == false)
                        {
                            System.String successStr;
                            if (success)
                            {
                                successStr = "success";
                            }
                            else
                            {
                                successStr = "IOException";
                                System.Console.Error.WriteLine(err.StackTrace);
                            }
                            Assert.Fail(methodName + " failed to delete unreferenced files after " + successStr + " (" + diskFree + " bytes): before delete:\n    " + ArrayToString(startFiles) + "\n  after delete:\n    " + ArrayToString(endFiles));
                        }
						
                        if (debug)
                        {
                            System.Console.Out.WriteLine("  now test readers");
                        }
						
                        // Finally, verify index is not corrupt, and, if
                        // we succeeded, we see all docs added, and if we
                        // failed, we see either all docs or no docs added
                        // (transactional semantics):
                        try
                        {
                            reader = IndexReader.Open(dir);
                        }
                        catch (System.IO.IOException e)
                        {
                            System.Console.Error.WriteLine(e.StackTrace);
                            Assert.Fail(testName + ": exception when creating IndexReader: " + e);
                        }
                        int result = reader.DocFreq(searchTerm);
                        if (success)
                        {
                            if (result != END_COUNT)
                            {
                                Assert.Fail(testName + ": method did not throw exception but docFreq('aaa') is " + result + " instead of expected " + END_COUNT);
                            }
                        }
                        else
                        {
                            // On hitting exception we still may have added
                            // all docs:
                            if (result != START_COUNT && result != END_COUNT)
                            {
                                System.Console.Error.WriteLine(err.StackTrace);
                                Assert.Fail(testName + ": method did throw exception but docFreq('aaa') is " + result + " instead of expected " + START_COUNT + " or " + END_COUNT);
                            }
                        }
						
                        searcher = new IndexSearcher(reader);
                        try
                        {
                            hits = searcher.Search(new TermQuery(searchTerm));
                        }
                        catch (System.IO.IOException e)
                        {
                            System.Console.Error.WriteLine(e.StackTrace);
                            Assert.Fail(testName + ": exception when searching: " + e);
                        }
                        int result2 = hits.Length();
                        if (success)
                        {
                            if (result2 != result)
                            {
                                Assert.Fail(testName + ": method did not throw exception but hits.length for search on term 'aaa' is " + result2 + " instead of expected " + result);
                            }
                        }
                        else
                        {
                            // On hitting exception we still may have added
                            // all docs:
                            if (result2 != result)
                            {
                                System.Console.Error.WriteLine(err.StackTrace);
                                Assert.Fail(testName + ": method did throw exception but hits.length for search on term 'aaa' is " + result2 + " instead of expected " + result);
                            }
                        }
						
                        searcher.Close();
                        reader.Close();
                        if (debug)
                        {
                            System.Console.Out.WriteLine("  count is " + result);
                        }
						
                        if (result == END_COUNT)
                        {
                            break;
                        }
                    }
					
                    // Javadocs state that temp free Directory space
                    // required is at most 2X total input size of
                    // indices so let's make sure:
                    Assert.IsTrue((dir.GetMaxUsedSizeInBytes() - startDiskUsage) < 2 * (startDiskUsage + inputDiskUsage), "max free Directory space required exceeded 1X the total input index sizes during " + methodName + ": max temp usage = " + (dir.GetMaxUsedSizeInBytes() - startDiskUsage) + " bytes; " + "starting disk usage = " + startDiskUsage + " bytes; " + "input index disk usage = " + inputDiskUsage + " bytes");
					
                    writer2.Close();
                    dir.Close();
					
                    // Try again with 1000 more bytes of free space:
                    diskFree += 1000;
                }
            }
			
            startDir.Close();
        }
		
        /// <summary> Make sure optimize doesn't use any more than 1X
        /// starting index size as its temporary free space
        /// required.
        /// </summary>
        [Test]
        public virtual void  TestOptimizeTempSpaceUsage()
        {
			
            MockRAMDirectory dir = new MockRAMDirectory();
            IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
            for (int j = 0; j < 500; j++)
            {
                AddDocWithIndex(writer, j);
            }
            writer.Close();
			
            long startDiskUsage = 0;
            System.String[] files = dir.List();
            for (int i = 0; i < files.Length; i++)
            {
                startDiskUsage += dir.FileLength(files[i]);
            }
			
            dir.ResetMaxUsedSizeInBytes();
            writer = new IndexWriter(dir, new WhitespaceAnalyzer(), false);
            writer.Optimize();
            writer.Close();
            long maxDiskUsage = dir.GetMaxUsedSizeInBytes();
			
            Assert.IsTrue(maxDiskUsage <= 2 * startDiskUsage, "optimized used too much temporary space: starting usage was " + startDiskUsage + " bytes; max temp usage was " + maxDiskUsage + " but should have been " + (2 * startDiskUsage) + " (= 2X starting usage)");
        }
		
        public System.String ArrayToString(System.String[] l)
        {
            System.String s = "";
            for (int i = 0; i < l.Length; i++)
            {
                if (i > 0)
                {
                    s += "\n    ";
                }
                s += l[i];
            }
            return s;
        }
		
        // Make sure we can open an index for create even when a
        // reader holds it open (this fails pre lock-less
        // commits on windows):
        [Test]
        public virtual void  TestCreateWithReader()
        {
            System.String tempDir = System.IO.Path.GetTempPath();
            if (tempDir == null)
                throw new System.IO.IOException("java.io.tmpdir undefined, cannot run test");
            System.IO.FileInfo indexDir = new System.IO.FileInfo(tempDir + "\\" + "lucenetestindexwriter");
			
            try
            {
                Directory dir = FSDirectory.GetDirectory(indexDir);
				
                // add one document & Close writer
                IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
                AddDoc(writer);
                writer.Close();
				
                // now open reader:
                IndexReader reader = IndexReader.Open(dir);
                Assert.AreEqual(reader.NumDocs(), 1, "should be one document");
				
                // now open index for create:
                writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
                Assert.AreEqual(writer.DocCount(), 0, "should be zero documents");
                AddDoc(writer);
                writer.Close();
				
                Assert.AreEqual(reader.NumDocs(), 1, "should be one document");
                IndexReader reader2 = IndexReader.Open(dir);
                Assert.AreEqual(reader2.NumDocs(), 1, "should be one document");
                reader.Close();
                reader2.Close();
            }
            finally
            {
                RmDir(indexDir);
            }
        }
		
		
        // Same test as above, but use IndexWriter constructor
        // that takes File:
        [Test]
        public virtual void  TestCreateWithReader2()
        {
            System.String tempDir = System.IO.Path.GetTempPath();
            if (tempDir == null)
                throw new System.IO.IOException("java.io.tmpdir undefined, cannot run test");
            System.IO.FileInfo indexDir = new System.IO.FileInfo(System.IO.Path.Combine(tempDir, "lucenetestindexwriter"));
            try
            {
                // add one document & Close writer
                IndexWriter writer = new IndexWriter(indexDir, new WhitespaceAnalyzer(), true);
                AddDoc(writer);
                writer.Close();
				
                // now open reader:
                IndexReader reader = IndexReader.Open(indexDir);
                Assert.AreEqual(reader.NumDocs(), 1, "should be one document");
				
                // now open index for create:
                writer = new IndexWriter(indexDir, new WhitespaceAnalyzer(), true);
                Assert.AreEqual(writer.DocCount(), 0, "should be zero documents");
                AddDoc(writer);
                writer.Close();
				
                Assert.AreEqual(reader.NumDocs(), 1, "should be one document");
                IndexReader reader2 = IndexReader.Open(indexDir);
                Assert.AreEqual(reader2.NumDocs(), 1, "should be one document");
                reader.Close();
                reader2.Close();
            }
            finally
            {
                RmDir(indexDir);
            }
        }
		
        // Same test as above, but use IndexWriter constructor
        // that takes String:
        [Test]
        public virtual void  TestCreateWithReader3()
        {
            System.String tempDir = SupportClass.AppSettings.Get("tempDir", "");
            if (tempDir == null)
                throw new System.IO.IOException("java.io.tmpdir undefined, cannot run test");
			
            System.String dirName = tempDir + "/lucenetestindexwriter";
            try
            {
				
                // add one document & Close writer
                IndexWriter writer = new IndexWriter(dirName, new WhitespaceAnalyzer(), true);
                AddDoc(writer);
                writer.Close();
				
                // now open reader:
                IndexReader reader = IndexReader.Open(dirName);
                Assert.AreEqual(reader.NumDocs(), 1, "should be one document");
				
                // now open index for create:
                writer = new IndexWriter(dirName, new WhitespaceAnalyzer(), true);
                Assert.AreEqual(writer.DocCount(), 0, "should be zero documents");
                AddDoc(writer);
                writer.Close();
				
                Assert.AreEqual(reader.NumDocs(), 1, "should be one document");
                IndexReader reader2 = IndexReader.Open(dirName);
                Assert.AreEqual(reader2.NumDocs(), 1, "should be one document");
                reader.Close();
                reader2.Close();
            }
            finally
            {
                RmDir(new System.IO.FileInfo(dirName));
            }
        }
		
        // Simulate a writer that crashed while writing segments
        // file: make sure we can still open the index (ie,
        // gracefully fallback to the previous segments file),
        // and that we can add to the index:
        [Test]
        public virtual void  TestSimulatedCrashedWriter()
        {
            Directory dir = new RAMDirectory();
			
            IndexWriter writer = null;
			
            writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			
            // add 100 documents
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }
			
            // Close
            writer.Close();
			
            long gen = SegmentInfos.GetCurrentSegmentGeneration(dir);
            Assert.IsTrue(gen > 1, "segment generation should be > 1 but got " + gen);
			
            // Make the next segments file, with last byte
            // missing, to simulate a writer that crashed while
            // writing segments file:
            System.String fileNameIn = SegmentInfos.GetCurrentSegmentFileName(dir);
            System.String fileNameOut = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", 1 + gen);
            IndexInput in_Renamed = dir.OpenInput(fileNameIn);
            IndexOutput out_Renamed = dir.CreateOutput(fileNameOut);
            long length = in_Renamed.Length();
            for (int i = 0; i < length - 1; i++)
            {
                out_Renamed.WriteByte(in_Renamed.ReadByte());
            }
            in_Renamed.Close();
            out_Renamed.Close();
			
            IndexReader reader = null;
            try
            {
                reader = IndexReader.Open(dir);
            }
            catch (System.Exception e)
            {
                Assert.Fail("reader failed to open on a crashed index");
            }
            reader.Close();
			
            try
            {
                writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
            }
            catch (System.Exception e)
            {
                Assert.Fail("writer failed to open on a crashed index");
            }
			
            // add 100 documents
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }
			
            // Close
            writer.Close();
        }
		
        // Simulate a corrupt index by removing last byte of
        // latest segments file and make sure we get an
        // IOException trying to open the index:
        [Test]
        public virtual void  TestSimulatedCorruptIndex1()
        {
            Directory dir = new RAMDirectory();
			
            IndexWriter writer = null;
			
            writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			
            // add 100 documents
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }
			
            // Close
            writer.Close();
			
            long gen = SegmentInfos.GetCurrentSegmentGeneration(dir);
            Assert.IsTrue(gen > 1, "segment generation should be > 1 but got " + gen);
			
            System.String fileNameIn = SegmentInfos.GetCurrentSegmentFileName(dir);
            System.String fileNameOut = IndexFileNames.FileNameFromGeneration(IndexFileNames.SEGMENTS, "", 1 + gen);
            IndexInput in_Renamed = dir.OpenInput(fileNameIn);
            IndexOutput out_Renamed = dir.CreateOutput(fileNameOut);
            long length = in_Renamed.Length();
            for (int i = 0; i < length - 1; i++)
            {
                out_Renamed.WriteByte(in_Renamed.ReadByte());
            }
            in_Renamed.Close();
            out_Renamed.Close();
            dir.DeleteFile(fileNameIn);
			
            IndexReader reader = null;
            try
            {
                reader = IndexReader.Open(dir);
                Assert.Fail("reader did not hit IOException on opening a corrupt index");
            }
            catch (System.Exception e)
            {
            }
            if (reader != null)
            {
                reader.Close();
            }
        }
		
        // Simulate a corrupt index by removing one of the cfs
        // files and make sure we get an IOException trying to
        // open the index:
        [Test]
        public virtual void  TestSimulatedCorruptIndex2()
        {
            Directory dir = new RAMDirectory();
			
            IndexWriter writer = null;
			
            writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
			
            // add 100 documents
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }
			
            // Close
            writer.Close();
			
            long gen = SegmentInfos.GetCurrentSegmentGeneration(dir);
            Assert.IsTrue(gen > 1, "segment generation should be > 1 but got " + gen);
			
            System.String[] files = dir.List();
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].EndsWith(".cfs"))
                {
                    dir.DeleteFile(files[i]);
                    break;
                }
            }
			
            IndexReader reader = null;
            try
            {
                reader = IndexReader.Open(dir);
                Assert.Fail("reader did not hit IOException on opening a corrupt index");
            }
            catch (System.Exception e)
            {
            }
            if (reader != null)
            {
                reader.Close();
            }
        }
		
        // Make sure that a Directory implementation that does
        // not use LockFactory at all (ie overrides makeLock and
        // implements its own private locking) works OK.  This
        // was raised on java-dev as loss of backwards
        // compatibility.
        [Test]
        public virtual void  TestNullLockFactory()
        {
			
			
            Directory dir = new MyRAMDirectory(this);
            IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
            for (int i = 0; i < 100; i++)
            {
                AddDoc(writer);
            }
            writer.Close();
            IndexReader reader = IndexReader.Open(dir);
            Term searchTerm = new Term("content", "aaa");
            IndexSearcher searcher = new IndexSearcher(dir);
            Hits hits = searcher.Search(new TermQuery(searchTerm));
            Assert.AreEqual(100, hits.Length(), "did not get right number of hits");
            writer.Close();
			
            writer = new IndexWriter(dir, new WhitespaceAnalyzer(), true);
            writer.Close();
			
            dir.Close();
        }
		
        private void  RmDir(System.IO.FileInfo dir)
        {
            System.IO.FileInfo[] files = SupportClass.FileSupport.GetFiles(dir);
            if (files != null)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    bool tmpBool;
                    if (System.IO.File.Exists(files[i].FullName))
                    {
                        System.IO.File.Delete(files[i].FullName);
                        tmpBool = true;
                    }
                    else if (System.IO.Directory.Exists(files[i].FullName))
                    {
                        System.IO.Directory.Delete(files[i].FullName);
                        tmpBool = true;
                    }
                    else
                        tmpBool = false;
                    bool generatedAux = tmpBool;
                }
            }
            bool tmpBool2;
            if (System.IO.File.Exists(dir.FullName))
            {
                System.IO.File.Delete(dir.FullName);
                tmpBool2 = true;
            }
            else if (System.IO.Directory.Exists(dir.FullName))
            {
                System.IO.Directory.Delete(dir.FullName);
                tmpBool2 = true;
            }
            else
                tmpBool2 = false;
            bool generatedAux2 = tmpBool2;
        }
    }
}