using Lucene.Net.Index.Extensions;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

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

    using Codec = Lucene.Net.Codecs.Codec;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using Lucene40StoredFieldsWriter = Lucene.Net.Codecs.Lucene40.Lucene40StoredFieldsWriter;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TestIndexWriterReader = Lucene.Net.Index.TestIndexWriterReader;

    [TestFixture]
    public class TestFileSwitchDirectory : LuceneTestCase
    {
        /// <summary>
        /// Test if writing doc stores to disk and everything else to ram works.
        /// </summary>
        [Test]
        public virtual void TestBasic()
        {
            ISet<string> fileExtensions = new JCG.HashSet<string>();
            fileExtensions.Add(Lucene40StoredFieldsWriter.FIELDS_EXTENSION);
            fileExtensions.Add(Lucene40StoredFieldsWriter.FIELDS_INDEX_EXTENSION);

            MockDirectoryWrapper primaryDir = new MockDirectoryWrapper(Random, new RAMDirectory());
            primaryDir.CheckIndexOnDispose = false; // only part of an index
            MockDirectoryWrapper secondaryDir = new MockDirectoryWrapper(Random, new RAMDirectory());
            secondaryDir.CheckIndexOnDispose = false; // only part of an index

            FileSwitchDirectory fsd = new FileSwitchDirectory(fileExtensions, primaryDir, secondaryDir, true);
            // for now we wire Lucene40Codec because we rely upon its specific impl
            bool oldValue = OldFormatImpersonationIsActive;
            OldFormatImpersonationIsActive = true;
            IndexWriter writer = new IndexWriter(fsd, (new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))).SetMergePolicy(NewLogMergePolicy(false)).SetCodec(Codec.ForName("Lucene40")).SetUseCompoundFile(false));
            TestIndexWriterReader.CreateIndexNoClose(true, "ram", writer);
            IndexReader reader = DirectoryReader.Open(writer, true);
            Assert.AreEqual(100, reader.MaxDoc);
            writer.Commit();
            // we should see only fdx,fdt files here
            string[] files = primaryDir.ListAll();
            Assert.IsTrue(files.Length > 0);
            for (int x = 0; x < files.Length; x++)
            {
                string ext = FileSwitchDirectory.GetExtension(files[x]);
                Assert.IsTrue(fileExtensions.Contains(ext));
            }
            files = secondaryDir.ListAll();
            Assert.IsTrue(files.Length > 0);
            // we should not see fdx,fdt files here
            for (int x = 0; x < files.Length; x++)
            {
                string ext = FileSwitchDirectory.GetExtension(files[x]);
                Assert.IsFalse(fileExtensions.Contains(ext));
            }
            reader.Dispose();
            writer.Dispose();

            files = fsd.ListAll();
            for (int i = 0; i < files.Length; i++)
            {
                Assert.IsNotNull(files[i]);
            }
            fsd.Dispose();
            OldFormatImpersonationIsActive = oldValue;
        }

        private Directory NewFSSwitchDirectory(ISet<string> primaryExtensions)
        {
            DirectoryInfo primDir = CreateTempDir("foo");
            DirectoryInfo secondDir = CreateTempDir("bar");
            return NewFSSwitchDirectory(primDir, secondDir, primaryExtensions);
        }

        private Directory NewFSSwitchDirectory(DirectoryInfo aDir, DirectoryInfo bDir, ISet<string> primaryExtensions)
        {
            Directory a = new SimpleFSDirectory(aDir);
            Directory b = new SimpleFSDirectory(bDir);
            FileSwitchDirectory switchDir = new FileSwitchDirectory(primaryExtensions, a, b, true);
            return new MockDirectoryWrapper(Random, switchDir);
        }

        // LUCENE-3380 -- make sure we get exception if the directory really does not exist.
        [Test]
        public virtual void TestNoDir()
        {
            DirectoryInfo primDir = CreateTempDir("foo");
            DirectoryInfo secondDir = CreateTempDir("bar");
            System.IO.Directory.Delete(primDir.FullName, true);
            System.IO.Directory.Delete(secondDir.FullName, true);
            using Directory dir = NewFSSwitchDirectory(primDir, secondDir, Collections.EmptySet<string>());
            try
            {
                DirectoryReader.Open(dir);
                Assert.Fail("did not hit expected exception");
            }
            catch (Exception nsde) when (nsde.IsNoSuchDirectoryException())
            {
                // expected
            }
        }

        private static bool ContainsFile(Directory directory, string file) // LUCENENET specific method to prevent having to use Arrays.AsList(), which creates unnecessary memory allocations
        {
            return Array.IndexOf(directory.ListAll(), file) > -1;
        }

        // LUCENE-3380 test that we can add a file, and then when we call list() we get it back
        [Test]
        public virtual void TestDirectoryFilter()
        {
            Directory dir = NewFSSwitchDirectory(Collections.EmptySet<string>());
            string name = "file";
            try
            {
                dir.CreateOutput(name, NewIOContext(Random)).Dispose();
                Assert.IsTrue(SlowFileExists(dir, name));
                Assert.IsTrue(ContainsFile(dir, name));
            }
            finally
            {
                dir.Dispose();
            }
        }

        // LUCENE-3380 test that delegate compound files correctly.
        [Test]
        public virtual void TestCompoundFileAppendTwice()
        {
            Directory newDir = NewFSSwitchDirectory(new JCG.HashSet<string> { "cfs" });
            var csw = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext(Random), true);
            CreateSequenceFile(newDir, "d1", (sbyte)0, 15);
            IndexOutput @out = csw.CreateOutput("d.xyz", NewIOContext(Random));
            @out.WriteInt32(0);
            @out.Dispose();
            Assert.AreEqual(1, csw.ListAll().Length);
            Assert.AreEqual("d.xyz", csw.ListAll()[0]);

            csw.Dispose();

            CompoundFileDirectory cfr = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext(Random), false);
            Assert.AreEqual(1, cfr.ListAll().Length);
            Assert.AreEqual("d.xyz", cfr.ListAll()[0]);
            cfr.Dispose();
            newDir.Dispose();
        }

        /// <summary>
        /// Creates a file of the specified size with sequential data. The first
        ///  byte is written as the start byte provided. All subsequent bytes are
        ///  computed as start + offset where offset is the number of the byte.
        /// </summary>
        private void CreateSequenceFile(Directory dir, string name, sbyte start, int size)
        {
            IndexOutput os = dir.CreateOutput(name, NewIOContext(Random));
            for (int i = 0; i < size; i++)
            {
                os.WriteByte((byte)start);
                start++;
            }
            os.Dispose();
        }
    }
}