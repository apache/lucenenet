using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using OpenMode = Lucene.Net.Index.OpenMode;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;

    [TestFixture]
    public class TestBufferedIndexInput : LuceneTestCase
    {
        private static void WriteBytes(FileInfo aFile, long size)
        {
            using FileStream ostream = new FileStream(aFile.FullName, FileMode.Create);
            for (int i = 0; i < size; i++)
            {
                ostream.WriteByte(Byten(i));
            }
            ostream.Flush();
        }

        private const long TEST_FILE_LENGTH = 100 * 1024;

        // Call readByte() repeatedly, past the buffer boundary, and see that it
        // is working as expected.
        // Our input comes from a dynamically generated/ "file" - see
        // MyBufferedIndexInput below.
        [Test]
        public virtual void TestReadByte()
        {
            MyBufferedIndexInput input = new MyBufferedIndexInput();
            for (int i = 0; i < BufferedIndexInput.BUFFER_SIZE * 10; i++)
            {
                Assert.AreEqual(input.ReadByte(), Byten(i));
            }
        }

        // Call readBytes() repeatedly, with various chunk sizes (from 1 byte to
        // larger than the buffer size), and see that it returns the bytes we expect.
        // Our input comes from a dynamically generated "file" -
        // see MyBufferedIndexInput below.
        [Test]
        public virtual void TestReadBytes()
        {
            MyBufferedIndexInput input = new MyBufferedIndexInput();
            RunReadBytes(input, BufferedIndexInput.BUFFER_SIZE, Random);
        }

        private void RunReadBytesAndClose(IndexInput input, int bufferSize, Random r)
        {
            try
            {
                RunReadBytes(input, bufferSize, r);
            }
            finally
            {
                input.Dispose();
            }
        }

        private void RunReadBytes(IndexInput input, int bufferSize, Random r)
        {
            int pos = 0;
            // gradually increasing size:
            for (int size = 1; size < bufferSize * 10; size = size + size / 200 + 1)
            {
                CheckReadBytes(input, size, pos);
                pos += size;
                if (pos >= TEST_FILE_LENGTH)
                {
                    // wrap
                    pos = 0;
                    input.Seek(0L);
                }
            }
            // wildly fluctuating size:
            for (long i = 0; i < 100; i++)
            {
                int size = r.Next(10000);
                CheckReadBytes(input, 1 + size, pos);
                pos += 1 + size;
                if (pos >= TEST_FILE_LENGTH)
                {
                    // wrap
                    pos = 0;
                    input.Seek(0L);
                }
            }
            // constant small size (7 bytes):
            for (int i = 0; i < bufferSize; i++)
            {
                CheckReadBytes(input, 7, pos);
                pos += 7;
                if (pos >= TEST_FILE_LENGTH)
                {
                    // wrap
                    pos = 0;
                    input.Seek(0L);
                }
            }
        }

        private byte[] buffer = new byte[10];

        private void CheckReadBytes(IndexInput input, int size, int pos)
        {
            // Just to see that "offset" is treated properly in readBytes(), we
            // add an arbitrary offset at the beginning of the array
            int offset = size % 10; // arbitrary
            buffer = ArrayUtil.Grow(buffer, offset + size);
            Assert.AreEqual(pos, input.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            long left = TEST_FILE_LENGTH - input.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            if (left <= 0)
            {
                return;
            }
            else if (left < size)
            {
                size = (int)left;
            }
            input.ReadBytes(buffer, offset, size);
            Assert.AreEqual(pos + size, input.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            for (int i = 0; i < size; i++)
            {
                Assert.AreEqual(Byten(pos + i), (byte)buffer[offset + i], "pos=" + i + " filepos=" + (pos + i));
            }
        }

        // this tests that attempts to readBytes() past an EOF will fail, while
        // reads up to the EOF will succeed. The EOF is determined by the
        // BufferedIndexInput's arbitrary length() value.
        [Test]
        public virtual void TestEOF()
        {
            MyBufferedIndexInput input = new MyBufferedIndexInput(1024);
            // see that we can read all the bytes at one go:
            CheckReadBytes(input, (int)input.Length, 0);
            // go back and see that we can't read more than that, for small and
            // large overflows:
            int pos = (int)input.Length - 10;
            input.Seek(pos);
            CheckReadBytes(input, 10, pos);
            input.Seek(pos);
            try
            {
                CheckReadBytes(input, 11, pos);
                Assert.Fail("Block read past end of file");
            }
            catch (Exception e) when (e.IsIOException())
            {
                /* success */
            }
            input.Seek(pos);
            try
            {
                CheckReadBytes(input, 50, pos);
                Assert.Fail("Block read past end of file");
            }
            catch (Exception e) when (e.IsIOException())
            {
                /* success */
            }
            input.Seek(pos);
            try
            {
                CheckReadBytes(input, 100000, pos);
                Assert.Fail("Block read past end of file");
            }
            catch (Exception e) when (e.IsIOException())
            {
                /* success */
            }
        }

        // byten emulates a file - byten(n) returns the n'th byte in that file.
        // MyBufferedIndexInput reads this "file".
        private static byte Byten(long n)
        {
            return (byte)(n * n % 256);
        }

        private class MyBufferedIndexInput : BufferedIndexInput
        {
            private long pos;
            private readonly long len;

            public MyBufferedIndexInput(long len)
                : base("MyBufferedIndexInput(len=" + len + ")", BufferedIndexInput.BUFFER_SIZE)
            {
                this.len = len;
                this.pos = 0;
            }

            public MyBufferedIndexInput()
                : this(long.MaxValue)
            {
                // an infinite file
            }

            protected override void ReadInternal(byte[] b, int offset, int length)
            {
                for (int i = offset; i < offset + length; i++)
                {
                    b[i] = Byten(pos++);
                }
            }

            protected override void SeekInternal(long pos)
            {
                this.pos = pos;
            }

            protected override void Dispose(bool disposing)
            {
            }

            public override long Length => len;
        }

        [Test]
        public virtual void TestSetBufferSize()
        {
            var indexDir = CreateTempDir("testSetBufferSize");
            var dir = new MockFSDirectory(indexDir, Random);
            try
            {
                var writer = new IndexWriter(
                    dir,
                    new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                        .SetOpenMode(OpenMode.CREATE)
                        .SetMergePolicy(NewLogMergePolicy(false)));
                    
                for (int i = 0; i < 37; i++)
                {
                    var doc = new Document();
                    doc.Add(NewTextField("content", "aaa bbb ccc ddd" + i, Field.Store.YES));
                    doc.Add(NewTextField("id", "" + i, Field.Store.YES));
                    writer.AddDocument(doc);
                }

                dir.allIndexInputs.Clear();

                IndexReader reader = DirectoryReader.Open(writer, true);
                var aaa = new Term("content", "aaa");
                var bbb = new Term("content", "bbb");
                reader.Dispose();

                dir.TweakBufferSizes();
                writer.DeleteDocuments(new Term("id", "0"));
                reader = DirectoryReader.Open(writer, true);
                var searcher = NewSearcher(reader);
                var hits = searcher.Search(new TermQuery(bbb), null, 1000).ScoreDocs;
                dir.TweakBufferSizes();
                Assert.AreEqual(36, hits.Length);

                reader.Dispose();

                dir.TweakBufferSizes();
                writer.DeleteDocuments(new Term("id", "4"));
                reader = DirectoryReader.Open(writer, true);
                searcher = NewSearcher(reader);

                hits = searcher.Search(new TermQuery(bbb), null, 1000).ScoreDocs;
                dir.TweakBufferSizes();
                Assert.AreEqual(35, hits.Length);
                dir.TweakBufferSizes();
                hits = searcher.Search(new TermQuery(new Term("id", "33")), null, 1000).ScoreDocs;
                dir.TweakBufferSizes();
                Assert.AreEqual(1, hits.Length);
                hits = searcher.Search(new TermQuery(aaa), null, 1000).ScoreDocs;
                dir.TweakBufferSizes();
                Assert.AreEqual(35, hits.Length);
                writer.Dispose();
                reader.Dispose();
            }
            finally
            {
                indexDir.Delete(true);
            }
        }

        private class MockFSDirectory : BaseDirectory
        {
            internal readonly IList<IndexInput> allIndexInputs = new JCG.List<IndexInput>();
            private readonly Random rand;
            private readonly Directory dir;

            public MockFSDirectory(DirectoryInfo path, Random rand)
            {
                this.rand = rand;
                SetLockFactory(NoLockFactory.GetNoLockFactory());
                dir = new SimpleFSDirectory(path, null);
            }

            public virtual void TweakBufferSizes()
            {
                //int count = 0;
                foreach (IndexInput ip in allIndexInputs)
                {
                    BufferedIndexInput bii = (BufferedIndexInput)ip;
                    int bufferSize = 1024 + Math.Abs(rand.Next() % 32768);
                    bii.SetBufferSize(bufferSize);
                    //count++;
                }
                //System.out.println("tweak'd " + count + " buffer sizes");
            }

            public override IndexInput OpenInput(string name, IOContext context)
            {
                // Make random changes to buffer size
                //bufferSize = 1+Math.abs(rand.nextInt() % 10);
                IndexInput f = dir.OpenInput(name, context);
                allIndexInputs.Add(f);
                return f;
            }

            public override IndexOutput CreateOutput(string name, IOContext context)
            {
                return dir.CreateOutput(name, context);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    dir.Dispose();
                }
            }

            public override void DeleteFile(string name)
            {
                dir.DeleteFile(name);
            }

            [Obsolete("this method will be removed in 5.0")]
            public override bool FileExists(string name)
            {
                return dir.FileExists(name);
            }

            public override string[] ListAll()
            {
                return dir.ListAll();
            }

            public override void Sync(ICollection<string> names)
            {
                dir.Sync(names);
            }

            public override long FileLength(string name)
            {
                return dir.FileLength(name);
            }
        }
    }
}