using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
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

    public class IndexAndTaxonomyRevisionTest : ReplicatorTestCase
    {
        private Document NewDocument(ITaxonomyWriter taxoWriter)
        {
            FacetsConfig config = new FacetsConfig();
            Document doc = new Document();
            doc.Add(new FacetField("A", "1"));
            return config.Build(taxoWriter, doc);
        }

        [Test]
        public void TestNoCommit()
        {
            Directory indexDir = NewDirectory();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.IndexDeletionPolicy = new SnapshotDeletionPolicy(conf.IndexDeletionPolicy);
            IndexWriter indexWriter = new IndexWriter(indexDir, conf);

            Directory taxoDir = NewDirectory();
            // LUCENENET specific - changed to use SnapshotDirectoryTaxonomyWriterFactory
            var indexWriterFactory = new SnapshotDirectoryTaxonomyIndexWriterFactory();
            var taxoWriter = new DirectoryTaxonomyWriter(indexWriterFactory, taxoDir);
            try
            {
                assertNotNull(new IndexAndTaxonomyRevision(indexWriter, indexWriterFactory));
                fail("should have failed when there are no commits to snapshot");
            }
            catch (Exception e) when (e.IsIllegalStateException())
            {
                // expected
            }
            finally
            {
                IOUtils.Dispose(indexWriter, taxoWriter, taxoDir, indexDir);
            }
        }

        [Test]
        public void TestRevisionRelease()
        {
            Directory indexDir = NewDirectory();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.IndexDeletionPolicy = new SnapshotDeletionPolicy(conf.IndexDeletionPolicy);
            IndexWriter indexWriter = new IndexWriter(indexDir, conf);

            Directory taxoDir = NewDirectory();
            // LUCENENET specific - changed to use SnapshotDirectoryTaxonomyWriterFactory
            var indexWriterFactory = new SnapshotDirectoryTaxonomyIndexWriterFactory();
            var taxoWriter = new DirectoryTaxonomyWriter(indexWriterFactory, taxoDir);
            try
            {
                indexWriter.AddDocument(NewDocument(taxoWriter));
                indexWriter.Commit();
                taxoWriter.Commit();
                IRevision rev1 = new IndexAndTaxonomyRevision(indexWriter, indexWriterFactory);
                // releasing that revision should not delete the files
                rev1.Release();
                assertTrue(SlowFileExists(indexDir, IndexFileNames.SEGMENTS + "_1"));
                assertTrue(SlowFileExists(taxoDir, IndexFileNames.SEGMENTS + "_1"));

                rev1 = new IndexAndTaxonomyRevision(indexWriter, indexWriterFactory); // create revision again, so the files are snapshotted
                indexWriter.AddDocument(NewDocument(taxoWriter));
                indexWriter.Commit();
                taxoWriter.Commit();
                assertNotNull(new IndexAndTaxonomyRevision(indexWriter, indexWriterFactory)); // this should not fail, since there is a commit
                rev1.Release(); // this release should trigger the delete of segments_1
                assertFalse(SlowFileExists(indexDir, IndexFileNames.SEGMENTS + "_1"));
            }
            finally
            {
                IOUtils.Dispose(indexWriter, taxoWriter, taxoDir, indexDir);
            }
        }

        [Test]
        public void TestSegmentsFileLast()
        {
            Directory indexDir = NewDirectory();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.IndexDeletionPolicy = new SnapshotDeletionPolicy(conf.IndexDeletionPolicy);
            IndexWriter indexWriter = new IndexWriter(indexDir, conf);

            Directory taxoDir = NewDirectory();
            // LUCENENET specific - changed to use SnapshotDirectoryTaxonomyWriterFactory
            var indexWriterFactory = new SnapshotDirectoryTaxonomyIndexWriterFactory();
            var taxoWriter = new DirectoryTaxonomyWriter(indexWriterFactory, taxoDir);
            try
            {
                indexWriter.AddDocument(NewDocument(taxoWriter));
                indexWriter.Commit();
                taxoWriter.Commit();
                IRevision rev = new IndexAndTaxonomyRevision(indexWriter, indexWriterFactory);
                var sourceFiles = rev.SourceFiles;
                assertEquals(2, sourceFiles.Count);
                foreach (var files in sourceFiles.Values)
                {
                    string lastFile = files.Last().FileName;
                    assertTrue(lastFile.StartsWith(IndexFileNames.SEGMENTS, StringComparison.Ordinal) && !lastFile.Equals(IndexFileNames.SEGMENTS_GEN, StringComparison.Ordinal));
                }
            }
            finally
            {
                IOUtils.Dispose(indexWriter, taxoWriter, taxoDir, indexDir);
            }
        }

        [Test]
        public void TestOpen()
        {
            Directory indexDir = NewDirectory();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
            conf.IndexDeletionPolicy = new SnapshotDeletionPolicy(conf.IndexDeletionPolicy);
            IndexWriter indexWriter = new IndexWriter(indexDir, conf);

            Directory taxoDir = NewDirectory();
            // LUCENENET specific - changed to use SnapshotDirectoryTaxonomyWriterFactory
            var indexWriterFactory = new SnapshotDirectoryTaxonomyIndexWriterFactory();
            var taxoWriter = new DirectoryTaxonomyWriter(indexWriterFactory, taxoDir);
            try
            {
                indexWriter.AddDocument(NewDocument(taxoWriter));
                indexWriter.Commit();
                taxoWriter.Commit();
                IRevision rev = new IndexAndTaxonomyRevision(indexWriter, indexWriterFactory);
                foreach (var e in rev.SourceFiles)
                {
                    string source = e.Key;
                    Directory dir = source.Equals(IndexAndTaxonomyRevision.INDEX_SOURCE, StringComparison.Ordinal) ? indexDir : taxoDir;
                    foreach (RevisionFile file in e.Value)
                    {
                        using IndexInput src = dir.OpenInput(file.FileName, IOContext.READ_ONCE);
                        using Stream @in = rev.Open(source, file.FileName);
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
                    }
                }
            }
            finally
            {
                IOUtils.Dispose(indexWriter, taxoWriter, taxoDir, indexDir);
            }
        }
    }
}