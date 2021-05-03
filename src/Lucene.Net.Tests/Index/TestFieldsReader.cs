using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

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

    using BaseDirectory = Lucene.Net.Store.BaseDirectory;
    using BufferedIndexInput = Lucene.Net.Store.BufferedIndexInput;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using DocumentStoredFieldVisitor = DocumentStoredFieldVisitor;
    using Field = Field;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;

    [TestFixture]
    public class TestFieldsReader : LuceneTestCase
    {
        private static Directory dir;
        private static Document testDoc;
        private static FieldInfos.Builder fieldInfos = null;

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewIndexWriterConfig is no longer static.
        /// </summary>
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            testDoc = new Document();
            fieldInfos = new FieldInfos.Builder();
            DocHelper.SetupDoc(testDoc);
            foreach (IIndexableField field in testDoc)
            {
                fieldInfos.AddOrUpdate(field.Name, field.IndexableFieldType);
            }
            dir = NewDirectory();
            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy());
            conf.MergePolicy.NoCFSRatio = 0.0;
            IndexWriter writer = new IndexWriter(dir, conf);
            writer.AddDocument(testDoc);
            writer.Dispose();
            FaultyIndexInput.doFail = false;
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            dir.Dispose();
            dir = null;
            fieldInfos = null;
            testDoc = null;
            base.AfterClass();
        }

        [Test]
        public virtual void Test()
        {
            Assert.IsTrue(dir != null);
            Assert.IsTrue(fieldInfos != null);
            IndexReader reader = DirectoryReader.Open(dir);
            Document doc = reader.Document(0);
            Assert.IsTrue(doc != null);
            Assert.IsTrue(doc.GetField(DocHelper.TEXT_FIELD_1_KEY) != null);

            Field field = (Field)doc.GetField(DocHelper.TEXT_FIELD_2_KEY);
            Assert.IsTrue(field != null);
            Assert.IsTrue(field.IndexableFieldType.StoreTermVectors);

            Assert.IsFalse(field.IndexableFieldType.OmitNorms);
            Assert.IsTrue(field.IndexableFieldType.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);

            field = (Field)doc.GetField(DocHelper.TEXT_FIELD_3_KEY);
            Assert.IsTrue(field != null);
            Assert.IsFalse(field.IndexableFieldType.StoreTermVectors);
            Assert.IsTrue(field.IndexableFieldType.OmitNorms);
            Assert.IsTrue(field.IndexableFieldType.IndexOptions == IndexOptions.DOCS_AND_FREQS_AND_POSITIONS);

            field = (Field)doc.GetField(DocHelper.NO_TF_KEY);
            Assert.IsTrue(field != null);
            Assert.IsFalse(field.IndexableFieldType.StoreTermVectors);
            Assert.IsFalse(field.IndexableFieldType.OmitNorms);
            Assert.IsTrue(field.IndexableFieldType.IndexOptions == IndexOptions.DOCS_ONLY);

            DocumentStoredFieldVisitor visitor = new DocumentStoredFieldVisitor(DocHelper.TEXT_FIELD_3_KEY);
            reader.Document(0, visitor);
            IList<IIndexableField> fields = visitor.Document.Fields;
            Assert.AreEqual(1, fields.Count);
            Assert.AreEqual(DocHelper.TEXT_FIELD_3_KEY, fields[0].Name);
            reader.Dispose();
        }

        public class FaultyFSDirectory : BaseDirectory
        {
            internal Directory fsDir;

            public FaultyFSDirectory(DirectoryInfo dir)
            {
                fsDir = NewFSDirectory(dir);
                m_lockFactory = fsDir.LockFactory;
            }

            public override IndexInput OpenInput(string name, IOContext context)
            {
                return new FaultyIndexInput(fsDir.OpenInput(name, context));
            }

            public override string[] ListAll()
            {
                return fsDir.ListAll();
            }

            [Obsolete("this method will be removed in 5.0")]
            public override bool FileExists(string name)
            {
                return fsDir.FileExists(name);
            }

            public override void DeleteFile(string name)
            {
                fsDir.DeleteFile(name);
            }

            public override long FileLength(string name)
            {
                return fsDir.FileLength(name);
            }

            public override IndexOutput CreateOutput(string name, IOContext context)
            {
                return fsDir.CreateOutput(name, context);
            }

            public override void Sync(ICollection<string> names)
            {
                fsDir.Sync(names);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    fsDir.Dispose();
                }
            }
        }

        private class FaultyIndexInput : BufferedIndexInput
        {
            internal IndexInput @delegate;
            internal static bool doFail;
            internal int count;

            internal FaultyIndexInput(IndexInput @delegate)
                : base("FaultyIndexInput(" + @delegate + ")", BufferedIndexInput.BUFFER_SIZE)
            {
                this.@delegate = @delegate;
            }

            internal virtual void SimOutage()
            {
                if (doFail && count++ % 2 == 1)
                {
                    throw new IOException("Simulated network outage");
                }
            }

            protected override void ReadInternal(byte[] b, int offset, int length)
            {
                SimOutage();
                @delegate.Seek(Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                @delegate.ReadBytes(b, offset, length);
            }

            protected override void SeekInternal(long pos)
            {
            }

            public override long Length => @delegate.Length;

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    @delegate.Dispose();
                }
            }

            public override object Clone()
            {
                FaultyIndexInput i = new FaultyIndexInput((IndexInput)@delegate.Clone());
                // seek the clone to our current position
                try
                {
                    i.Seek(Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create(e);
                }
                return i;
            }
        }

        // LUCENE-1262
        [Test]
        public virtual void TestExceptions()
        {
            DirectoryInfo indexDir = CreateTempDir("testfieldswriterexceptions");

            try
            {
                Directory dir = new FaultyFSDirectory(indexDir);
                IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetOpenMode(OpenMode.CREATE);
                IndexWriter writer = new IndexWriter(dir, iwc);
                for (int i = 0; i < 2; i++)
                {
                    writer.AddDocument(testDoc);
                }
                writer.ForceMerge(1);
                writer.Dispose();

                IndexReader reader = DirectoryReader.Open(dir);

                FaultyIndexInput.doFail = true;

                bool exc = false;

                for (int i = 0; i < 2; i++)
                {
                    try
                    {
                        reader.Document(i);
                    }
                    catch (Exception ioe) when (ioe.IsIOException())
                    {
                        // expected
                        exc = true;
                    }
                    try
                    {
                        reader.Document(i);
                    }
                    catch (Exception ioe) when (ioe.IsIOException())
                    {
                        // expected
                        exc = true;
                    }
                }
                Assert.IsTrue(exc);
                reader.Dispose();
                dir.Dispose();
            }
            finally
            {
                System.IO.Directory.Delete(indexDir.FullName, true);
            }
        }
    }
}