using Lucene.Net.Documents;
using Lucene.Net.Store;
using NUnit.Framework;
using System;
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

    using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using SimpleFSDirectory = Lucene.Net.Store.SimpleFSDirectory;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestCompoundFile : LuceneTestCase
    {
        private Directory dir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            DirectoryInfo file = CreateTempDir("testIndex");
            // use a simple FSDir here, to be sure to have SimpleFSInputs
            dir = new SimpleFSDirectory(file, null);
        }

        [TearDown]
        public override void TearDown()
        {
            dir.Dispose();
            base.TearDown();
        }

        /// <summary>
        /// Creates a file of the specified size with random data. </summary>
        private void CreateRandomFile(Directory dir, string name, int size)
        {
            IndexOutput os = dir.CreateOutput(name, NewIOContext(Random));
            for (int i = 0; i < size; i++)
            {
                var b = (byte)(Random.NextDouble() * 256); // LUCENENET: Using Random, since Math.random() doesn't exist in .NET, and it seems to make sense to make this repeatable.
                os.WriteByte(b);
            }
            os.Dispose();
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

        private void AssertSameStreams(string msg, IndexInput expected, IndexInput test)
        {
            Assert.IsNotNull(expected, msg + " null expected");
            Assert.IsNotNull(test, msg + " null test");
            Assert.AreEqual(expected.Length, test.Length, msg + " length");
            Assert.AreEqual(expected.Position, test.Position, msg + " position"); // LUCENENET specific: Renamed from getFilePointer() to match FileStream

            var expectedBuffer = new byte[512];
            var testBuffer = new byte[expectedBuffer.Length];

            long remainder = expected.Length - expected.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            while (remainder > 0)
            {
                int readLen = (int)Math.Min(remainder, expectedBuffer.Length);
                expected.ReadBytes(expectedBuffer, 0, readLen);
                test.ReadBytes(testBuffer, 0, readLen);
                AssertEqualArrays(msg + ", remainder " + remainder, expectedBuffer, testBuffer, 0, readLen);
                remainder -= readLen;
            }
        }

        private void AssertSameStreams(string msg, IndexInput expected, IndexInput actual, long seekTo)
        {
            if (seekTo >= 0 && seekTo < expected.Length)
            {
                expected.Seek(seekTo);
                actual.Seek(seekTo);
                AssertSameStreams(msg + ", seek(mid)", expected, actual);
            }
        }

        private void AssertSameSeekBehavior(string msg, IndexInput expected, IndexInput actual)
        {
            // seek to 0
            long point = 0;
            AssertSameStreams(msg + ", seek(0)", expected, actual, point);

            // seek to middle
            point = expected.Length / 2L;
            AssertSameStreams(msg + ", seek(mid)", expected, actual, point);

            // seek to end - 2
            point = expected.Length - 2;
            AssertSameStreams(msg + ", seek(end-2)", expected, actual, point);

            // seek to end - 1
            point = expected.Length - 1;
            AssertSameStreams(msg + ", seek(end-1)", expected, actual, point);

            // seek to the end
            point = expected.Length;
            AssertSameStreams(msg + ", seek(end)", expected, actual, point);

            // seek past end
            point = expected.Length + 1;
            AssertSameStreams(msg + ", seek(end+1)", expected, actual, point);
        }

        private void AssertEqualArrays(string msg, byte[] expected, byte[] test, int start, int len)
        {
            Assert.IsNotNull(expected, msg + " null expected");
            Assert.IsNotNull(test, msg + " null test");

            for (int i = start; i < len; i++)
            {
                Assert.AreEqual(expected[i], test[i], msg + " " + i);
            }
        }

        // ===========================================================
        //  Tests of the basic CompoundFile functionality
        // ===========================================================

        /// <summary>
        /// this test creates compound file based on a single file.
        ///  Files of different sizes are tested: 0, 1, 10, 100 bytes.
        /// </summary>
        [Test]
        public virtual void TestSingleFile()
        {
            int[] data = new int[] { 0, 1, 10, 100 };
            for (int i = 0; i < data.Length; i++)
            {
                string name = "t" + data[i];
                CreateSequenceFile(dir, name, (sbyte)0, data[i]);
                CompoundFileDirectory csw = new CompoundFileDirectory(dir, name + ".cfs", NewIOContext(Random), true);
                dir.Copy(csw, name, name, NewIOContext(Random));
                csw.Dispose();

                CompoundFileDirectory csr = new CompoundFileDirectory(dir, name + ".cfs", NewIOContext(Random), false);
                IndexInput expected = dir.OpenInput(name, NewIOContext(Random));
                IndexInput actual = csr.OpenInput(name, NewIOContext(Random));
                AssertSameStreams(name, expected, actual);
                AssertSameSeekBehavior(name, expected, actual);
                expected.Dispose();
                actual.Dispose();
                csr.Dispose();
            }
        }

        /// <summary>
        /// this test creates compound file based on two files.
        ///
        /// </summary>
        [Test]
        public virtual void TestTwoFiles()
        {
            CreateSequenceFile(dir, "d1", (sbyte)0, 15);
            CreateSequenceFile(dir, "d2", (sbyte)0, 114);

            CompoundFileDirectory csw = new CompoundFileDirectory(dir, "d.cfs", NewIOContext(Random), true);
            dir.Copy(csw, "d1", "d1", NewIOContext(Random));
            dir.Copy(csw, "d2", "d2", NewIOContext(Random));
            csw.Dispose();

            CompoundFileDirectory csr = new CompoundFileDirectory(dir, "d.cfs", NewIOContext(Random), false);
            IndexInput expected = dir.OpenInput("d1", NewIOContext(Random));
            IndexInput actual = csr.OpenInput("d1", NewIOContext(Random));
            AssertSameStreams("d1", expected, actual);
            AssertSameSeekBehavior("d1", expected, actual);
            expected.Dispose();
            actual.Dispose();

            expected = dir.OpenInput("d2", NewIOContext(Random));
            actual = csr.OpenInput("d2", NewIOContext(Random));
            AssertSameStreams("d2", expected, actual);
            AssertSameSeekBehavior("d2", expected, actual);
            expected.Dispose();
            actual.Dispose();
            csr.Dispose();
        }

        /// <summary>
        /// this test creates a compound file based on a large number of files of
        ///  various length. The file content is generated randomly. The sizes range
        ///  from 0 to 1Mb. Some of the sizes are selected to test the buffering
        ///  logic in the file reading code. For this the chunk variable is set to
        ///  the length of the buffer used internally by the compound file logic.
        /// </summary>
        [Test]
        public virtual void TestRandomFiles()
        {
            // Setup the test segment
            string segment = "test";
            int chunk = 1024; // internal buffer size used by the stream
            CreateRandomFile(dir, segment + ".zero", 0);
            CreateRandomFile(dir, segment + ".one", 1);
            CreateRandomFile(dir, segment + ".ten", 10);
            CreateRandomFile(dir, segment + ".hundred", 100);
            CreateRandomFile(dir, segment + ".big1", chunk);
            CreateRandomFile(dir, segment + ".big2", chunk - 1);
            CreateRandomFile(dir, segment + ".big3", chunk + 1);
            CreateRandomFile(dir, segment + ".big4", 3 * chunk);
            CreateRandomFile(dir, segment + ".big5", 3 * chunk - 1);
            CreateRandomFile(dir, segment + ".big6", 3 * chunk + 1);
            CreateRandomFile(dir, segment + ".big7", 1000 * chunk);

            // Setup extraneous files
            CreateRandomFile(dir, "onetwothree", 100);
            CreateRandomFile(dir, segment + ".notIn", 50);
            CreateRandomFile(dir, segment + ".notIn2", 51);

            // Now test
            CompoundFileDirectory csw = new CompoundFileDirectory(dir, "test.cfs", NewIOContext(Random), true);
            string[] data = new string[] { ".zero", ".one", ".ten", ".hundred", ".big1", ".big2", ".big3", ".big4", ".big5", ".big6", ".big7" };
            for (int i = 0; i < data.Length; i++)
            {
                string fileName = segment + data[i];
                dir.Copy(csw, fileName, fileName, NewIOContext(Random));
            }
            csw.Dispose();

            CompoundFileDirectory csr = new CompoundFileDirectory(dir, "test.cfs", NewIOContext(Random), false);
            for (int i = 0; i < data.Length; i++)
            {
                IndexInput check = dir.OpenInput(segment + data[i], NewIOContext(Random));
                IndexInput test = csr.OpenInput(segment + data[i], NewIOContext(Random));
                AssertSameStreams(data[i], check, test);
                AssertSameSeekBehavior(data[i], check, test);
                test.Dispose();
                check.Dispose();
            }
            csr.Dispose();
        }

        /// <summary>
        /// Setup a larger compound file with a number of components, each of
        ///  which is a sequential file (so that we can easily tell that we are
        ///  reading in the right byte). The methods sets up 20 files - f0 to f19,
        ///  the size of each file is 1000 bytes.
        /// </summary>
        private void SetUp_2()
        {
            CompoundFileDirectory cw = new CompoundFileDirectory(dir, "f.comp", NewIOContext(Random), true);
            for (int i = 0; i < 20; i++)
            {
                CreateSequenceFile(dir, "f" + i, (sbyte)0, 2000);
                string fileName = "f" + i;
                dir.Copy(cw, fileName, fileName, NewIOContext(Random));
            }
            cw.Dispose();
        }

        [Test]
        public virtual void TestReadAfterClose()
        {
            Demo_FSIndexInputBug(dir, "test");
        }

        private void Demo_FSIndexInputBug(Directory fsdir, string file)
        {
            // Setup the test file - we need more than 1024 bytes
            IndexOutput os = fsdir.CreateOutput(file, IOContext.DEFAULT);
            for (int i = 0; i < 2000; i++)
            {
                os.WriteByte((byte)i);
            }
            os.Dispose();

            IndexInput @in = fsdir.OpenInput(file, IOContext.DEFAULT);

            // this read primes the buffer in IndexInput
            @in.ReadByte();

            // Close the file
            @in.Dispose();

            // ERROR: this call should fail, but succeeds because the buffer
            // is still filled
            @in.ReadByte();

            // ERROR: this call should fail, but succeeds for some reason as well
            @in.Seek(1099);

            try
            {
                // OK: this call correctly fails. We are now past the 1024 internal
                // buffer, so an actual IO is attempted, which fails
                @in.ReadByte();
                Assert.Fail("expected readByte() to throw exception");
            }
            catch (Exception e) when (e.IsIOException())
            {
                // expected exception
            }
        }

        [Test]
        public virtual void TestClonedStreamsClosing()
        {
            SetUp_2();
            CompoundFileDirectory cr = new CompoundFileDirectory(dir, "f.comp", NewIOContext(Random), false);

            // basic clone
            IndexInput expected = dir.OpenInput("f11", NewIOContext(Random));

            // this test only works for FSIndexInput
            Assert.IsTrue(TestHelper.IsSimpleFSIndexInput(expected));
            Assert.IsTrue(TestHelper.IsSimpleFSIndexInputOpen(expected));

            IndexInput one = cr.OpenInput("f11", NewIOContext(Random));

            IndexInput two = (IndexInput)one.Clone();

            AssertSameStreams("basic clone one", expected, one);
            expected.Seek(0);
            AssertSameStreams("basic clone two", expected, two);

            // Now close the first stream
            one.Dispose();

            // The following should really fail since we couldn't expect to
            // access a file once close has been called on it (regardless of
            // buffering and/or clone magic)
            expected.Seek(0);
            two.Seek(0);
            AssertSameStreams("basic clone two/2", expected, two);

            // Now close the compound reader
            cr.Dispose();

            // The following may also fail since the compound stream is closed
            expected.Seek(0);
            two.Seek(0);
            //assertSameStreams("basic clone two/3", expected, two);

            // Now close the second clone
            two.Dispose();
            expected.Seek(0);
            two.Seek(0);
            //assertSameStreams("basic clone two/4", expected, two);

            expected.Dispose();
        }

        /// <summary>
        /// this test opens two files from a compound stream and verifies that
        ///  their file positions are independent of each other.
        /// </summary>
        [Test]
        public virtual void TestRandomAccess()
        {
            SetUp_2();
            CompoundFileDirectory cr = new CompoundFileDirectory(dir, "f.comp", NewIOContext(Random), false);

            // Open two files
            IndexInput e1 = dir.OpenInput("f11", NewIOContext(Random));
            IndexInput e2 = dir.OpenInput("f3", NewIOContext(Random));

            IndexInput a1 = cr.OpenInput("f11", NewIOContext(Random));
            IndexInput a2 = dir.OpenInput("f3", NewIOContext(Random));

            // Seek the first pair
            e1.Seek(100);
            a1.Seek(100);
            Assert.AreEqual(100, e1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            Assert.AreEqual(100, a1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            byte be1 = e1.ReadByte();
            byte ba1 = a1.ReadByte();
            Assert.AreEqual(be1, ba1);

            // Now seek the second pair
            e2.Seek(1027);
            a2.Seek(1027);
            Assert.AreEqual(1027, e2.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            Assert.AreEqual(1027, a2.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            byte be2 = e2.ReadByte();
            byte ba2 = a2.ReadByte();
            Assert.AreEqual(be2, ba2);

            // Now make sure the first one didn't move
            Assert.AreEqual(101, e1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            Assert.AreEqual(101, a1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            be1 = e1.ReadByte();
            ba1 = a1.ReadByte();
            Assert.AreEqual(be1, ba1);

            // Now more the first one again, past the buffer length
            e1.Seek(1910);
            a1.Seek(1910);
            Assert.AreEqual(1910, e1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            Assert.AreEqual(1910, a1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            be1 = e1.ReadByte();
            ba1 = a1.ReadByte();
            Assert.AreEqual(be1, ba1);

            // Now make sure the second set didn't move
            Assert.AreEqual(1028, e2.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            Assert.AreEqual(1028, a2.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            be2 = e2.ReadByte();
            ba2 = a2.ReadByte();
            Assert.AreEqual(be2, ba2);

            // Move the second set back, again cross the buffer size
            e2.Seek(17);
            a2.Seek(17);
            Assert.AreEqual(17, e2.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            Assert.AreEqual(17, a2.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            be2 = e2.ReadByte();
            ba2 = a2.ReadByte();
            Assert.AreEqual(be2, ba2);

            // Finally, make sure the first set didn't move
            // Now make sure the first one didn't move
            Assert.AreEqual(1911, e1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            Assert.AreEqual(1911, a1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            be1 = e1.ReadByte();
            ba1 = a1.ReadByte();
            Assert.AreEqual(be1, ba1);

            e1.Dispose();
            e2.Dispose();
            a1.Dispose();
            a2.Dispose();
            cr.Dispose();
        }

        /// <summary>
        /// this test opens two files from a compound stream and verifies that
        ///  their file positions are independent of each other.
        /// </summary>
        [Test]
        public virtual void TestRandomAccessClones()
        {
            SetUp_2();
            CompoundFileDirectory cr = new CompoundFileDirectory(dir, "f.comp", NewIOContext(Random), false);

            // Open two files
            IndexInput e1 = cr.OpenInput("f11", NewIOContext(Random));
            IndexInput e2 = cr.OpenInput("f3", NewIOContext(Random));

            IndexInput a1 = (IndexInput)e1.Clone();
            IndexInput a2 = (IndexInput)e2.Clone();

            // Seek the first pair
            e1.Seek(100);
            a1.Seek(100);
            Assert.AreEqual(100, e1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            Assert.AreEqual(100, a1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            byte be1 = e1.ReadByte();
            byte ba1 = a1.ReadByte();
            Assert.AreEqual(be1, ba1);

            // Now seek the second pair
            e2.Seek(1027);
            a2.Seek(1027);
            Assert.AreEqual(1027, e2.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            Assert.AreEqual(1027, a2.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            byte be2 = e2.ReadByte();
            byte ba2 = a2.ReadByte();
            Assert.AreEqual(be2, ba2);

            // Now make sure the first one didn't move
            Assert.AreEqual(101, e1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            Assert.AreEqual(101, a1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            be1 = e1.ReadByte();
            ba1 = a1.ReadByte();
            Assert.AreEqual(be1, ba1);

            // Now more the first one again, past the buffer length
            e1.Seek(1910);
            a1.Seek(1910);
            Assert.AreEqual(1910, e1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            Assert.AreEqual(1910, a1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            be1 = e1.ReadByte();
            ba1 = a1.ReadByte();
            Assert.AreEqual(be1, ba1);

            // Now make sure the second set didn't move
            Assert.AreEqual(1028, e2.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            Assert.AreEqual(1028, a2.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            be2 = e2.ReadByte();
            ba2 = a2.ReadByte();
            Assert.AreEqual(be2, ba2);

            // Move the second set back, again cross the buffer size
            e2.Seek(17);
            a2.Seek(17);
            Assert.AreEqual(17, e2.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            Assert.AreEqual(17, a2.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            be2 = e2.ReadByte();
            ba2 = a2.ReadByte();
            Assert.AreEqual(be2, ba2);

            // Finally, make sure the first set didn't move
            // Now make sure the first one didn't move
            Assert.AreEqual(1911, e1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            Assert.AreEqual(1911, a1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            be1 = e1.ReadByte();
            ba1 = a1.ReadByte();
            Assert.AreEqual(be1, ba1);

            e1.Dispose();
            e2.Dispose();
            a1.Dispose();
            a2.Dispose();
            cr.Dispose();
        }

        [Test]
        public virtual void TestFileNotFound()
        {
            SetUp_2();
            CompoundFileDirectory cr = new CompoundFileDirectory(dir, "f.comp", NewIOContext(Random), false);

            // Open two files
            try
            {
                cr.OpenInput("bogus", NewIOContext(Random));
                Assert.Fail("File not found");
            }
            catch (Exception e) when (e.IsIOException())
            {
                /* success */
                //System.out.println("SUCCESS: File Not Found: " + e);
            }

            cr.Dispose();
        }

        [Test]
        public virtual void TestReadPastEOF()
        {
            SetUp_2();
            var cr = new CompoundFileDirectory(dir, "f.comp", NewIOContext(Random), false);
            IndexInput @is = cr.OpenInput("f2", NewIOContext(Random));
            @is.Seek(@is.Length - 10);
            var b = new byte[100];
            @is.ReadBytes(b, 0, 10);

            try
            {
                @is.ReadByte();
                Assert.Fail("Single byte read past end of file");
            }
            catch (Exception e) when (e.IsIOException())
            {
                /* success */
                //System.out.println("SUCCESS: single byte read past end of file: " + e);
            }

            @is.Seek(@is.Length - 10);
            try
            {
                @is.ReadBytes(b, 0, 50);
                Assert.Fail("Block read past end of file");
            }
            catch (Exception e) when (e.IsIOException())
            {
                /* success */
                //System.out.println("SUCCESS: block read past end of file: " + e);
            }

            @is.Dispose();
            cr.Dispose();
        }

        /// <summary>
        /// this test that writes larger than the size of the buffer output
        /// will correctly increment the file pointer.
        /// </summary>
        [Test]
        public virtual void TestLargeWrites()
        {
            IndexOutput os = dir.CreateOutput("testBufferStart.txt", NewIOContext(Random));
            
            var largeBuf = new byte[2048];
            for (int i = 0; i < largeBuf.Length; i++)
            {
                largeBuf[i] = (byte)(Random.NextDouble() * 256); // LUCENENET: Using Random, since Math.random() doesn't exist in .NET, and it seems to make sense to make this repeatable.
            }

            long currentPos = os.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            os.WriteBytes(largeBuf, largeBuf.Length);

            try
            {
                Assert.AreEqual(currentPos + largeBuf.Length, os.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
            finally
            {
                os.Dispose();
            }
        }

        [Test]
        public virtual void TestAddExternalFile()
        {
            CreateSequenceFile(dir, "d1", (sbyte)0, 15);

            Directory newDir = NewDirectory();
            CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext(Random), true);
            dir.Copy(csw, "d1", "d1", NewIOContext(Random));
            csw.Dispose();

            CompoundFileDirectory csr = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext(Random), false);
            IndexInput expected = dir.OpenInput("d1", NewIOContext(Random));
            IndexInput actual = csr.OpenInput("d1", NewIOContext(Random));
            AssertSameStreams("d1", expected, actual);
            AssertSameSeekBehavior("d1", expected, actual);
            expected.Dispose();
            actual.Dispose();
            csr.Dispose();

            newDir.Dispose();
        }

        [Test]
        public virtual void TestAppend()
        {
            Directory newDir = NewDirectory();
            CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext(Random), true);
            int size = 5 + Random.Next(128);
            for (int j = 0; j < 2; j++)
            {
                IndexOutput os = csw.CreateOutput("seg_" + j + "_foo.txt", NewIOContext(Random));
                for (int i = 0; i < size; i++)
                {
                    os.WriteInt32(i * j);
                }
                os.Dispose();
                string[] listAll = newDir.ListAll();
                Assert.AreEqual(1, listAll.Length);
                Assert.AreEqual("d.cfs", listAll[0]);
            }
            CreateSequenceFile(dir, "d1", (sbyte)0, 15);
            dir.Copy(csw, "d1", "d1", NewIOContext(Random));
            string[] listAll_ = newDir.ListAll();
            Assert.AreEqual(1, listAll_.Length);
            Assert.AreEqual("d.cfs", listAll_[0]);
            csw.Dispose();
            CompoundFileDirectory csr = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext(Random), false);
            for (int j = 0; j < 2; j++)
            {
                IndexInput openInput = csr.OpenInput("seg_" + j + "_foo.txt", NewIOContext(Random));
                Assert.AreEqual(size * 4, openInput.Length);
                for (int i = 0; i < size; i++)
                {
                    Assert.AreEqual(i * j, openInput.ReadInt32());
                }

                openInput.Dispose();
            }
            IndexInput expected = dir.OpenInput("d1", NewIOContext(Random));
            IndexInput actual = csr.OpenInput("d1", NewIOContext(Random));
            AssertSameStreams("d1", expected, actual);
            AssertSameSeekBehavior("d1", expected, actual);
            expected.Dispose();
            actual.Dispose();
            csr.Dispose();
            newDir.Dispose();
        }

        [Test]
        public virtual void TestAppendTwice()
        {
            Directory newDir = NewDirectory();
            CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext(Random), true);
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

        [Test]
        public virtual void TestEmptyCFS()
        {
            Directory newDir = NewDirectory();
            CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext(Random), true);
            csw.Dispose();

            CompoundFileDirectory csr = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext(Random), false);
            Assert.AreEqual(0, csr.ListAll().Length);
            csr.Dispose();

            newDir.Dispose();
        }

        [Test]
        public virtual void TestReadNestedCFP()
        {
            Directory newDir = NewDirectory();
            CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext(Random), true);
            CompoundFileDirectory nested = new CompoundFileDirectory(newDir, "b.cfs", NewIOContext(Random), true);
            IndexOutput @out = nested.CreateOutput("b.xyz", NewIOContext(Random));
            IndexOutput out1 = nested.CreateOutput("b_1.xyz", NewIOContext(Random));
            @out.WriteInt32(0);
            out1.WriteInt32(1);
            @out.Dispose();
            out1.Dispose();
            nested.Dispose();
            newDir.Copy(csw, "b.cfs", "b.cfs", NewIOContext(Random));
            newDir.Copy(csw, "b.cfe", "b.cfe", NewIOContext(Random));
            newDir.DeleteFile("b.cfs");
            newDir.DeleteFile("b.cfe");
            csw.Dispose();

            Assert.AreEqual(2, newDir.ListAll().Length);
            csw = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext(Random), false);

            Assert.AreEqual(2, csw.ListAll().Length);
            nested = new CompoundFileDirectory(csw, "b.cfs", NewIOContext(Random), false);

            Assert.AreEqual(2, nested.ListAll().Length);
            IndexInput openInput = nested.OpenInput("b.xyz", NewIOContext(Random));
            Assert.AreEqual(0, openInput.ReadInt32());
            openInput.Dispose();
            openInput = nested.OpenInput("b_1.xyz", NewIOContext(Random));
            Assert.AreEqual(1, openInput.ReadInt32());
            openInput.Dispose();
            nested.Dispose();
            csw.Dispose();
            newDir.Dispose();
        }

        [Test]
        public virtual void TestDoubleClose()
        {
            Directory newDir = NewDirectory();
            CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext(Random), true);
            IndexOutput @out = csw.CreateOutput("d.xyz", NewIOContext(Random));
            @out.WriteInt32(0);
            @out.Dispose();

            csw.Dispose();
            // close a second time - must have no effect according to IDisposable
            csw.Dispose();

            csw = new CompoundFileDirectory(newDir, "d.cfs", NewIOContext(Random), false);
            IndexInput openInput = csw.OpenInput("d.xyz", NewIOContext(Random));
            Assert.AreEqual(0, openInput.ReadInt32());
            openInput.Dispose();
            csw.Dispose();
            // close a second time - must have no effect according to IDisposable
            csw.Dispose();

            newDir.Dispose();
        }

        // Make sure we don't somehow use more than 1 descriptor
        // when reading a CFS with many subs:
        [Test]
        public virtual void TestManySubFiles()
        {
            Directory d = NewFSDirectory(CreateTempDir("CFSManySubFiles"));
            int FILE_COUNT = AtLeast(500);

            for (int fileIdx = 0; fileIdx < FILE_COUNT; fileIdx++)
            {
                IndexOutput @out = d.CreateOutput("file." + fileIdx, NewIOContext(Random));
                @out.WriteByte((byte)fileIdx);
                @out.Dispose();
            }

            CompoundFileDirectory cfd = new CompoundFileDirectory(d, "c.cfs", NewIOContext(Random), true);
            for (int fileIdx = 0; fileIdx < FILE_COUNT; fileIdx++)
            {
                string fileName = "file." + fileIdx;
                d.Copy(cfd, fileName, fileName, NewIOContext(Random));
            }
            cfd.Dispose();

            IndexInput[] ins = new IndexInput[FILE_COUNT];
            CompoundFileDirectory cfr = new CompoundFileDirectory(d, "c.cfs", NewIOContext(Random), false);
            for (int fileIdx = 0; fileIdx < FILE_COUNT; fileIdx++)
            {
                ins[fileIdx] = cfr.OpenInput("file." + fileIdx, NewIOContext(Random));
            }

            for (int fileIdx = 0; fileIdx < FILE_COUNT; fileIdx++)
            {
                Assert.AreEqual((byte)fileIdx, ins[fileIdx].ReadByte());
            }

            for (int fileIdx = 0; fileIdx < FILE_COUNT; fileIdx++)
            {
                ins[fileIdx].Dispose();
            }
            cfr.Dispose();
            d.Dispose();
        }

        [Test]
        public virtual void TestListAll()
        {
            Directory dir = NewDirectory();
            // riw should sometimes create docvalues fields, etc
            RandomIndexWriter riw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            // these fields should sometimes get term vectors, etc
            Field idField = NewStringField("id", "", Field.Store.NO);
            Field bodyField = NewTextField("body", "", Field.Store.NO);
            doc.Add(idField);
            doc.Add(bodyField);
            for (int i = 0; i < 100; i++)
            {
                idField.SetStringValue(Convert.ToString(i));
                bodyField.SetStringValue(TestUtil.RandomUnicodeString(Random));
                riw.AddDocument(doc);
                if (Random.Next(7) == 0)
                {
                    riw.Commit();
                }
            }
            riw.Dispose();
            CheckFiles(dir);
            dir.Dispose();
        }

        // checks that we can open all files returned by listAll!
        private void CheckFiles(Directory dir)
        {
            foreach (string file in dir.ListAll())
            {
                if (file.EndsWith(IndexFileNames.COMPOUND_FILE_EXTENSION, StringComparison.Ordinal))
                {
                    CompoundFileDirectory cfsDir = new CompoundFileDirectory(dir, file, NewIOContext(Random), false);
                    CheckFiles(cfsDir); // recurse into cfs
                    cfsDir.Dispose();
                }
                IndexInput @in = null;
                bool success = false;
                try
                {
                    @in = dir.OpenInput(file, NewIOContext(Random));
                    success = true;
                }
                finally
                {
                    if (success)
                    {
                        IOUtils.Dispose(@in);
                    }
                    else
                    {
                        IOUtils.DisposeWhileHandlingException(@in);
                    }
                }
            }
        }
    }
}