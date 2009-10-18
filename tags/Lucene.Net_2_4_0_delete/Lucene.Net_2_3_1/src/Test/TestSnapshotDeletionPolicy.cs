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

using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexCommitPoint = Lucene.Net.Index.IndexCommitPoint;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using KeepOnlyLastCommitDeletionPolicy = Lucene.Net.Index.KeepOnlyLastCommitDeletionPolicy;
using SnapshotDeletionPolicy = Lucene.Net.Index.SnapshotDeletionPolicy;
using Directory = Lucene.Net.Store.Directory;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using IndexInput = Lucene.Net.Store.IndexInput;
using MockRAMDirectory = Lucene.Net.Store.MockRAMDirectory;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using _TestUtil = Lucene.Net.Util._TestUtil;
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using TestIndexWriter = Lucene.Net.Index.TestIndexWriter;

namespace Lucene.Net
{
	
	//
	// This was developed for Lucene In Action,
	// http://lucenebook.com
	//
	
	[TestFixture]
	public class TestSnapshotDeletionPolicy : LuceneTestCase
	{
		private class AnonymousClassThread : SupportClass.ThreadClass
		{
			public AnonymousClassThread(long stopTime, Lucene.Net.Index.IndexWriter writer, TestSnapshotDeletionPolicy enclosingInstance)
			{
				InitBlock(stopTime, writer, enclosingInstance);
			}
			private void  InitBlock(long stopTime, Lucene.Net.Index.IndexWriter writer, TestSnapshotDeletionPolicy enclosingInstance)
			{
				this.stopTime = stopTime;
				this.writer = writer;
				this.enclosingInstance = enclosingInstance;
			}
			private long stopTime;
			private Lucene.Net.Index.IndexWriter writer;
			private TestSnapshotDeletionPolicy enclosingInstance;
			public TestSnapshotDeletionPolicy Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			override public void  Run()
			{
				Document doc = new Document();
				doc.Add(new Field("content", "aaa", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
				while ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 < stopTime)
				{
					for (int i = 0; i < 27; i++)
					{
						try
						{
							writer.AddDocument(doc);
						}
						catch (System.IO.IOException cie)
						{
							System.SystemException re = new System.SystemException("addDocument failed", cie);
							throw re;
						}
					}
					try
					{
						System.Threading.Thread.Sleep(new System.TimeSpan((System.Int64) 10000 * 1));
					}
					catch (System.Threading.ThreadInterruptedException)
					{
						SupportClass.ThreadClass.Current().Interrupt();
					}
				}
			}
		}
		public const System.String INDEX_PATH = "test.snapshots";
		
		[Test]
		public virtual void  TestSnapshotDeletionPolicy_Renamed_Method()
		{
			System.IO.FileInfo dir = new System.IO.FileInfo(System.IO.Path.Combine(SupportClass.AppSettings.Get("tempDir", ""), INDEX_PATH));
			try
			{
				Directory fsDir = FSDirectory.GetDirectory(dir);
				RunTest(fsDir);
				fsDir.Close();
			}
			finally
			{
				_TestUtil.RmDir(dir);
			}
			
			MockRAMDirectory dir2 = new MockRAMDirectory();
			RunTest(dir2);
		}
		
		private void  RunTest(Directory dir)
		{
			// Run for ~7 seconds
			long stopTime = (System.DateTime.Now.Ticks - 621355968000000000) / 10000 + 7000;
			
			SnapshotDeletionPolicy dp = new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy());
			IndexWriter writer = new IndexWriter(dir, true, new StandardAnalyzer(), dp);
			
			// Force frequent commits
			writer.SetMaxBufferedDocs(2);
			
			SupportClass.ThreadClass t = new AnonymousClassThread(stopTime, writer, this);
			
			t.Start();
			
			// While the above indexing thread is running, take many
			// backups:
			while ((System.DateTime.Now.Ticks - 621355968000000000) / 10000 < stopTime)
			{
				BackupIndex(dir, dp);
				try
				{
					System.Threading.Thread.Sleep(new System.TimeSpan((System.Int64) 10000 * 20));
				}
				catch (System.Threading.ThreadInterruptedException)
				{
					SupportClass.ThreadClass.Current().Interrupt();
				}
				if (!t.IsAlive)
					break;
			}
			
			try
			{
				t.Join();
			}
			catch (System.Threading.ThreadInterruptedException)
			{
				SupportClass.ThreadClass.Current().Interrupt();
			}
			
			// Add one more document to force writer to commit a
			// final segment, so deletion policy has a chance to
			// delete again:
			Document doc = new Document();
			doc.Add(new Field("content", "aaa", Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
			writer.AddDocument(doc);
			
			// Make sure we don't have any leftover files in the
			// directory:
			writer.Close();
			TestIndexWriter.AssertNoUnreferencedFiles(dir, "some files were not deleted but should have been");
		}
		
		/// <summary>Example showing how to use the SnapshotDeletionPolicy
		/// to take a backup.  This method does not really do a
		/// backup; instead, it reads every byte of every file
		/// just to test that the files indeed exist and are
		/// readable even while the index is changing. 
		/// </summary>
		public virtual void  BackupIndex(Directory dir, SnapshotDeletionPolicy dp)
		{
			
			// To backup an index we first take a snapshot:
			IndexCommitPoint cp = dp.Snapshot();
			try
			{
				
				// While we hold the snapshot, and nomatter how long
				// we take to do the backup, the IndexWriter will
				// never delete the files in the snapshot:
				System.Collections.ICollection files = cp.GetFileNames();
				System.Collections.IEnumerator it = files.GetEnumerator();
				while (it.MoveNext())
				{
					System.String fileName = (System.String) it.Current;
					// NOTE: in a real backup you would not use
					// readFile; you would need to use something else
					// that copies the file to a backup location.  This
					// could even be a spawned shell process (eg "tar",
					// "zip") that takes the list of files and builds a
					// backup.
					ReadFile(dir, fileName);
				}
			}
			finally
			{
				// Make sure to release the snapshot, otherwise these
				// files will never be deleted during this IndexWriter
				// session:
				dp.Release();
			}
		}
		
		internal byte[] buffer = new byte[4096];
		
		private void  ReadFile(Directory dir, System.String name)
		{
			IndexInput input = dir.OpenInput(name);
			try
			{
				long size = dir.FileLength(name);
				long bytesLeft = size;
				while (bytesLeft > 0)
				{
					int numToRead;
					if (bytesLeft < buffer.Length)
						numToRead = (int) bytesLeft;
					else
						numToRead = buffer.Length;
					input.ReadBytes(buffer, 0, numToRead, false);
					bytesLeft -= numToRead;
				}
				// Don't do this in your real backups!  This is just
				// to force a backup to take a somewhat long time, to
				// make sure we are exercising the fact that the
				// IndexWriter should not delete this file even when I
				// take my time reading it.
				try
				{
					System.Threading.Thread.Sleep(new System.TimeSpan((System.Int64) 10000 * 1));
				}
				catch (System.Threading.ThreadInterruptedException)
				{
					SupportClass.ThreadClass.Current().Interrupt();
				}
			}
			finally
			{
				input.Close();
			}
		}
	}
}