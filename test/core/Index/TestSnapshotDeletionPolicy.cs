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
	using Document = Lucene.Net.Document.Document;
	using FieldType = Lucene.Net.Document.FieldType;
	using TextField = Lucene.Net.Document.TextField;
	using Directory = Lucene.Net.Store.Directory;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using ThreadInterruptedException = Lucene.Net.Util.ThreadInterruptedException;
	using Test = org.junit.Test;

	//
	// this was developed for Lucene In Action,
	// http://lucenebook.com
	//

	public class TestSnapshotDeletionPolicy : LuceneTestCase
	{
	  public const string INDEX_PATH = "test.snapshots";

	  protected internal virtual IndexWriterConfig GetConfig(Random random, IndexDeletionPolicy dp)
	  {
		IndexWriterConfig conf = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random));
		if (dp != null)
		{
		  conf.IndexDeletionPolicy = dp;
		}
		return conf;
	  }

	  protected internal virtual void CheckSnapshotExists(Directory dir, IndexCommit c)
	  {
		string segFileName = c.SegmentsFileName;
		Assert.IsTrue("segments file not found in directory: " + segFileName, slowFileExists(dir, segFileName));
	  }

	  protected internal virtual void CheckMaxDoc(IndexCommit commit, int expectedMaxDoc)
	  {
		IndexReader reader = DirectoryReader.open(commit);
		try
		{
		  Assert.AreEqual(expectedMaxDoc, reader.maxDoc());
		}
		finally
		{
		  reader.close();
		}
	  }

	  protected internal IList<IndexCommit> Snapshots = new List<IndexCommit>();

	  protected internal virtual void PrepareIndexAndSnapshots(SnapshotDeletionPolicy sdp, IndexWriter writer, int numSnapshots)
	  {
		for (int i = 0; i < numSnapshots; i++)
		{
		  // create dummy document to trigger commit.
		  writer.addDocument(new Document());
		  writer.commit();
		  Snapshots.Add(sdp.snapshot());
		}
	  }

	  protected internal virtual SnapshotDeletionPolicy DeletionPolicy
	  {
		  get
		  {
			return new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy());
		  }
	  }

	  protected internal virtual void AssertSnapshotExists(Directory dir, SnapshotDeletionPolicy sdp, int numSnapshots, bool checkIndexCommitSame)
	  {
		for (int i = 0; i < numSnapshots; i++)
		{
		  IndexCommit snapshot = Snapshots[i];
		  CheckMaxDoc(snapshot, i + 1);
		  CheckSnapshotExists(dir, snapshot);
		  if (checkIndexCommitSame)
		  {
			assertSame(snapshot, sdp.getIndexCommit(snapshot.Generation));
		  }
		  else
		  {
			Assert.AreEqual(snapshot.Generation, sdp.getIndexCommit(snapshot.Generation).Generation);
		  }
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSnapshotDeletionPolicy() throws Exception
	  public virtual void TestSnapshotDeletionPolicy()
	  {
		Directory fsDir = newDirectory();
		RunTest(random(), fsDir);
		fsDir.close();
	  }

	  private void RunTest(Random random, Directory dir)
	  {
		// Run for ~1 seconds
		long stopTime = System.currentTimeMillis() + 1000;

		SnapshotDeletionPolicy dp = DeletionPolicy;
		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)).setIndexDeletionPolicy(dp).setMaxBufferedDocs(2));

		// Verify we catch misuse:
		try
		{
		  dp.snapshot();
		  Assert.Fail("did not hit exception");
		}
		catch (IllegalStateException ise)
		{
		  // expected
		}
		dp = (SnapshotDeletionPolicy) writer.Config.IndexDeletionPolicy;
		writer.commit();

		Thread t = new ThreadAnonymousInnerClassHelper(this, stopTime, writer);

		t.Start();

		// While the above indexing thread is running, take many
		// backups:
		do
		{
		  BackupIndex(dir, dp);
		  Thread.Sleep(20);
		} while (t.IsAlive);

		t.Join();

		// Add one more document to force writer to commit a
		// final segment, so deletion policy has a chance to
		// delete again:
		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorOffsets = true;
		doc.add(newField("content", "aaa", customType));
		writer.addDocument(doc);

		// Make sure we don't have any leftover files in the
		// directory:
		writer.close();
		TestIndexWriter.AssertNoUnreferencedFiles(dir, "some files were not deleted but should have been");
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestSnapshotDeletionPolicy OuterInstance;

		  private long StopTime;
		  private IndexWriter Writer;

		  public ThreadAnonymousInnerClassHelper(TestSnapshotDeletionPolicy outerInstance, long stopTime, IndexWriter writer)
		  {
			  this.OuterInstance = outerInstance;
			  this.StopTime = stopTime;
			  this.Writer = writer;
		  }

		  public override void Run()
		  {
			Document doc = new Document();
			FieldType customType = new FieldType(TextField.TYPE_STORED);
			customType.StoreTermVectors = true;
			customType.StoreTermVectorPositions = true;
			customType.StoreTermVectorOffsets = true;
			doc.add(newField("content", "aaa", customType));
			do
			{
			  for (int i = 0;i < 27;i++)
			  {
				try
				{
				  Writer.addDocument(doc);
				}
				catch (Exception t)
				{
				  t.printStackTrace(System.out);
				  Assert.Fail("addDocument failed");
				}
				if (i % 2 == 0)
				{
				  try
				  {
					Writer.commit();
				  }
				  catch (Exception e)
				  {
					throw new Exception(e);
				  }
				}
			  }
			  try
			  {
				Thread.Sleep(1);
			  }
			  catch (InterruptedException ie)
			  {
				throw new ThreadInterruptedException(ie);
			  }
			} while (System.currentTimeMillis() < StopTime);
		  }
	  }

	  /// <summary>
	  /// Example showing how to use the SnapshotDeletionPolicy to take a backup.
	  /// this method does not really do a backup; instead, it reads every byte of
	  /// every file just to test that the files indeed exist and are readable even
	  /// while the index is changing.
	  /// </summary>
	  public virtual void BackupIndex(Directory dir, SnapshotDeletionPolicy dp)
	  {
		// To backup an index we first take a snapshot:
		IndexCommit snapshot = dp.snapshot();
		try
		{
		  CopyFiles(dir, snapshot);
		}
		finally
		{
		  // Make sure to release the snapshot, otherwise these
		  // files will never be deleted during this IndexWriter
		  // session:
		  dp.release(snapshot);
		}
	  }

	  private void CopyFiles(Directory dir, IndexCommit cp)
	  {

		// While we hold the snapshot, and nomatter how long
		// we take to do the backup, the IndexWriter will
		// never delete the files in the snapshot:
		ICollection<string> files = cp.FileNames;
		foreach (String fileName in files)
		{
		  // NOTE: in a real backup you would not use
		  // readFile; you would need to use something else
		  // that copies the file to a backup location.  this
		  // could even be a spawned shell process (eg "tar",
		  // "zip") that takes the list of files and builds a
		  // backup.
		  ReadFile(dir, fileName);
		}
	  }

	  internal sbyte[] Buffer = new sbyte[4096];

	  private void ReadFile(Directory dir, string name)
	  {
		IndexInput input = dir.openInput(name, newIOContext(random()));
		try
		{
		  long size = dir.fileLength(name);
		  long bytesLeft = size;
		  while (bytesLeft > 0)
		  {
			int numToRead;
			if (bytesLeft < Buffer.Length)
			{
			  numToRead = (int) bytesLeft;
			}
			else
			{
			  numToRead = Buffer.Length;
			}
			input.readBytes(Buffer, 0, numToRead, false);
			bytesLeft -= numToRead;
		  }
		  // Don't do this in your real backups!  this is just
		  // to force a backup to take a somewhat long time, to
		  // make sure we are exercising the fact that the
		  // IndexWriter should not delete this file even when I
		  // take my time reading it.
		  Thread.Sleep(1);
		}
		finally
		{
		  input.close();
		}
	  }


//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testBasicSnapshots() throws Exception
	  public virtual void TestBasicSnapshots()
	  {
		int numSnapshots = 3;

		// Create 3 snapshots: snapshot0, snapshot1, snapshot2
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, GetConfig(random(), DeletionPolicy));
		SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy) writer.Config.IndexDeletionPolicy;
		PrepareIndexAndSnapshots(sdp, writer, numSnapshots);
		writer.close();

		Assert.AreEqual(numSnapshots, sdp.Snapshots.size());
		Assert.AreEqual(numSnapshots, sdp.SnapshotCount);
		AssertSnapshotExists(dir, sdp, numSnapshots, true);

		// open a reader on a snapshot - should succeed.
		DirectoryReader.open(Snapshots[0]).close();

		// open a new IndexWriter w/ no snapshots to keep and assert that all snapshots are gone.
		sdp = DeletionPolicy;
		writer = new IndexWriter(dir, GetConfig(random(), sdp));
		writer.deleteUnusedFiles();
		writer.close();
		Assert.AreEqual("no snapshots should exist", 1, DirectoryReader.listCommits(dir).size());
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testMultiThreadedSnapshotting() throws Exception
	  public virtual void TestMultiThreadedSnapshotting()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, GetConfig(random(), DeletionPolicy));
		SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy) writer.Config.IndexDeletionPolicy;

		Thread[] threads = new Thread[10];
		IndexCommit[] snapshots = new IndexCommit[threads.Length];
		for (int i = 0; i < threads.Length; i++)
		{
		  int finalI = i;
		  threads[i] = new ThreadAnonymousInnerClassHelper2(this, writer, sdp, snapshots, finalI);
		  threads[i].Name = "t" + i;
		}

		foreach (Thread t in threads)
		{
		  t.Start();
		}

		foreach (Thread t in threads)
		{
		  t.Join();
		}

		// Do one last commit, so that after we release all snapshots, we stay w/ one commit
		writer.addDocument(new Document());
		writer.commit();

		for (int i = 0;i < threads.Length;i++)
		{
		  sdp.release(snapshots[i]);
		  writer.deleteUnusedFiles();
		}
		Assert.AreEqual(1, DirectoryReader.listCommits(dir).size());
		writer.close();
		dir.close();
	  }

	  private class ThreadAnonymousInnerClassHelper2 : System.Threading.Thread
	  {
		  private readonly TestSnapshotDeletionPolicy OuterInstance;

		  private IndexWriter Writer;
		  private SnapshotDeletionPolicy Sdp;
		  private IndexCommit[] Snapshots;
		  private int FinalI;

		  public ThreadAnonymousInnerClassHelper2(TestSnapshotDeletionPolicy outerInstance, IndexWriter writer, SnapshotDeletionPolicy sdp, IndexCommit[] snapshots, int finalI)
		  {
			  this.OuterInstance = outerInstance;
			  this.Writer = writer;
			  this.Sdp = sdp;
			  this.Snapshots = snapshots;
			  this.FinalI = finalI;
		  }

		  public override void Run()
		  {
			try
			{
			  Writer.addDocument(new Document());
			  Writer.commit();
			  Snapshots[FinalI] = Sdp.snapshot();
			}
			catch (Exception e)
			{
			  throw new Exception(e);
			}
		  }
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testRollbackToOldSnapshot() throws Exception
	  public virtual void TestRollbackToOldSnapshot()
	  {
		int numSnapshots = 2;
		Directory dir = newDirectory();

		SnapshotDeletionPolicy sdp = DeletionPolicy;
		IndexWriter writer = new IndexWriter(dir, GetConfig(random(), sdp));
		PrepareIndexAndSnapshots(sdp, writer, numSnapshots);
		writer.close();

		// now open the writer on "snapshot0" - make sure it succeeds
		writer = new IndexWriter(dir, GetConfig(random(), sdp).setIndexCommit(Snapshots[0]));
		// this does the actual rollback
		writer.commit();
		writer.deleteUnusedFiles();
		AssertSnapshotExists(dir, sdp, numSnapshots - 1, false);
		writer.close();

		// but 'snapshot1' files will still exist (need to release snapshot before they can be deleted).
		string segFileName = Snapshots[1].SegmentsFileName;
		Assert.IsTrue("snapshot files should exist in the directory: " + segFileName, slowFileExists(dir, segFileName));

		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testReleaseSnapshot() throws Exception
	  public virtual void TestReleaseSnapshot()
	  {
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, GetConfig(random(), DeletionPolicy));
		SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy) writer.Config.IndexDeletionPolicy;
		PrepareIndexAndSnapshots(sdp, writer, 1);

		// Create another commit - we must do that, because otherwise the "snapshot"
		// files will still remain in the index, since it's the last commit.
		writer.addDocument(new Document());
		writer.commit();

		// Release
		string segFileName = Snapshots[0].SegmentsFileName;
		sdp.release(Snapshots[0]);
		writer.deleteUnusedFiles();
		writer.close();
		Assert.IsFalse("segments file should not be found in dirctory: " + segFileName, slowFileExists(dir, segFileName));
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSnapshotLastCommitTwice() throws Exception
	  public virtual void TestSnapshotLastCommitTwice()
	  {
		Directory dir = newDirectory();

		IndexWriter writer = new IndexWriter(dir, GetConfig(random(), DeletionPolicy));
		SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy) writer.Config.IndexDeletionPolicy;
		writer.addDocument(new Document());
		writer.commit();

		IndexCommit s1 = sdp.snapshot();
		IndexCommit s2 = sdp.snapshot();
		assertSame(s1, s2); // should be the same instance

		// create another commit
		writer.addDocument(new Document());
		writer.commit();

		// release "s1" should not delete "s2"
		sdp.release(s1);
		writer.deleteUnusedFiles();
		CheckSnapshotExists(dir, s2);

		writer.close();
		dir.close();
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testMissingCommits() throws Exception
	  public virtual void TestMissingCommits()
	  {
		// Tests the behavior of SDP when commits that are given at ctor are missing
		// on onInit().
		Directory dir = newDirectory();
		IndexWriter writer = new IndexWriter(dir, GetConfig(random(), DeletionPolicy));
		SnapshotDeletionPolicy sdp = (SnapshotDeletionPolicy) writer.Config.IndexDeletionPolicy;
		writer.addDocument(new Document());
		writer.commit();
		IndexCommit s1 = sdp.snapshot();

		// create another commit, not snapshotted.
		writer.addDocument(new Document());
		writer.close();

		// open a new writer w/ KeepOnlyLastCommit policy, so it will delete "s1"
		// commit.
		(new IndexWriter(dir, GetConfig(random(), null))).close();

		Assert.IsFalse("snapshotted commit should not exist", slowFileExists(dir, s1.SegmentsFileName));
		dir.close();
	  }
	}

}