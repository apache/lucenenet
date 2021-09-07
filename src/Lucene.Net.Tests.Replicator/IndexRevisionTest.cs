using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Replicator
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

    public class IndexRevisionTest : ReplicatorTestCase
    {
        [Test]
        public void TestNoSnapshotDeletionPolicy()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.IndexDeletionPolicy = new KeepOnlyLastCommitDeletionPolicy();
            IndexWriter writer = new IndexWriter(dir, conf);
            try
            {
                assertNotNull(new IndexRevision(writer));
                fail("should have failed when IndexDeletionPolicy is not Snapshot");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
            finally
            {
                IOUtils.Dispose(writer, dir);
            }
        }

        [Test]
        public void TestNoCommit()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.IndexDeletionPolicy = new SnapshotDeletionPolicy(conf.IndexDeletionPolicy);
            IndexWriter writer = new IndexWriter(dir, conf);
            try
            {
                assertNotNull(new IndexRevision(writer));
                fail("should have failed when there are no commits to snapshot");
            }
            catch (Exception e) when (e.IsIllegalStateException())
            {
                // expected
            }
            finally
            {
                IOUtils.Dispose(writer, dir);
            }
        }

        [Test]
        public void TestRevisionRelease()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.IndexDeletionPolicy = new SnapshotDeletionPolicy(conf.IndexDeletionPolicy);
            IndexWriter writer = new IndexWriter(dir, conf);
            try
            {
                writer.AddDocument(new Document());
                writer.Commit();
                IRevision rev1 = new IndexRevision(writer);
                // releasing that revision should not delete the files
                rev1.Release();
                assertTrue(SlowFileExists(dir, IndexFileNames.SEGMENTS + "_1"));

                rev1 = new IndexRevision(writer); // create revision again, so the files are snapshotted
                writer.AddDocument(new Document());
                writer.Commit();
                assertNotNull(new IndexRevision(writer));
                rev1.Release(); // this release should trigger the delete of segments_1
                assertFalse(SlowFileExists(dir, IndexFileNames.SEGMENTS + "_1"));
            }
            finally
            {
                IOUtils.Dispose(writer, dir);
            }
        }

        [Test]
        public void TestSegmentsFileLast()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.IndexDeletionPolicy = new SnapshotDeletionPolicy(conf.IndexDeletionPolicy);
            IndexWriter writer = new IndexWriter(dir, conf);
            try
            {
                writer.AddDocument(new Document());
                writer.Commit();
                IRevision rev = new IndexRevision(writer);
                var sourceFiles = rev.SourceFiles;
                assertEquals(1, sourceFiles.Count);
                var files = sourceFiles.Values.First();
                string lastFile = files.Last().FileName;
                assertTrue(lastFile.StartsWith(IndexFileNames.SEGMENTS, StringComparison.Ordinal) && !lastFile.Equals(IndexFileNames.SEGMENTS_GEN, StringComparison.Ordinal));
            }
            finally
            {
                IOUtils.Dispose(writer, dir);
            }
        }

        [Test]
        public void TestOpen()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.IndexDeletionPolicy = new SnapshotDeletionPolicy(conf.IndexDeletionPolicy);
            IndexWriter writer = new IndexWriter(dir, conf);
            try
            {
                writer.AddDocument(new Document());
                writer.Commit();
                IRevision rev = new IndexRevision(writer);
                var sourceFiles = rev.SourceFiles;
                string source = sourceFiles.Keys.First();
                foreach (RevisionFile file in sourceFiles.Values.First())
                {
                    IndexInput src = dir.OpenInput(file.FileName, IOContext.READ_ONCE);
                    Stream @in = rev.Open(source, file.FileName);
                    assertEquals(src.Length, @in.Length);
                    byte[] srcBytes = new byte[(int)src.Length];
                    byte[] inBytes = new byte[(int)src.Length];
                    int offset = 0;
                    if (Random.nextBoolean())
                    {
                        int skip = Random.Next(10);
                        if (skip >= src.Length)
                        {
                            skip = 0;
                        }
                        @in.Seek(skip, SeekOrigin.Current);
                        src.Seek(skip);
                        offset = skip;
                    }
                    src.ReadBytes(srcBytes, offset, srcBytes.Length - offset);
                    @in.Read(inBytes, offset, inBytes.Length - offset);
                    assertArrayEquals(srcBytes, inBytes);
                    IOUtils.Dispose(src, @in);
                }
            }
            finally
            {
                IOUtils.Dispose(writer, dir);
            }
        }
    }
}