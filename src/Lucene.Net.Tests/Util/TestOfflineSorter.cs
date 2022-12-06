using J2N.Text;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Lucene.Net.Attributes;

namespace Lucene.Net.Util
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

    /// <summary>
    /// Tests for on-disk merge sorting.
    /// </summary>
    [TestFixture]
    public class TestOfflineSorter : LuceneTestCase
    {
        private DirectoryInfo tempDir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            tempDir = CreateTempDir("mergesort");
            DeleteTestFiles();
            tempDir.Create();
        }

        [TearDown]
        public override void TearDown()
        {
            DeleteTestFiles();
            base.TearDown();
        }

        private void DeleteTestFiles()
        {
            if (tempDir != null)
            {
                if (Directory.Exists(tempDir.FullName))
                {
                    foreach (var file in tempDir.GetFiles())
                    {
                        file.Delete();
                    }
                    tempDir.Delete();
                }
            }
        }

        [Test]
        public virtual void TestEmpty()
        {
#pragma warning disable CA1825 // Avoid zero-length array allocations.
            CheckSort(new OfflineSorter(), new byte[][] { });
#pragma warning restore CA1825 // Avoid zero-length array allocations.
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestEmpty_AsStream()
        {
#pragma warning disable CA1825 // Avoid zero-length array allocations.
            CheckSortAsStream(new OfflineSorter(), new byte[][] { });
#pragma warning restore CA1825 // Avoid zero-length array allocations.
        }

        [Test]
        public virtual void TestSingleLine()
        {
#pragma warning disable 612, 618
            CheckSort(new OfflineSorter(), new byte[][] { "Single line only.".GetBytes(IOUtils.CHARSET_UTF_8) });
#pragma warning restore 612, 618
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestSingleLine_AsStream()
        {
#pragma warning disable 612, 618
            CheckSortAsStream(new OfflineSorter(), new byte[][] { "Single line only.".GetBytes(IOUtils.CHARSET_UTF_8) });
#pragma warning restore 612, 618
        }

        [Test]
        public virtual void TestIntermediateMerges()
        {
            // Sort 20 mb worth of data with 1mb buffer, binary merging.
            OfflineSorter.SortInfo info = CheckSort(new OfflineSorter(OfflineSorter.DEFAULT_COMPARER, OfflineSorter.BufferSize.Megabytes(1), OfflineSorter.DefaultTempDir, 2), GenerateRandom((int)OfflineSorter.MB * 20));
            Assert.IsTrue(info.MergeRounds > 10);
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestIntermediateMerges_AsStream()
        {
            // Sort 20 mb worth of data with 1mb buffer, binary merging.
            OfflineSorter.SortInfo info = CheckSortAsStream(new OfflineSorter(OfflineSorter.DEFAULT_COMPARER, OfflineSorter.BufferSize.Megabytes(1), OfflineSorter.DefaultTempDir, 2), GenerateRandom((int)OfflineSorter.MB * 20));
            Assert.IsTrue(info.MergeRounds > 10);
        }

        [Test]
        public virtual void TestSmallRandom()
        {
            // Sort 20 mb worth of data with 1mb buffer.
            OfflineSorter.SortInfo sortInfo = CheckSort(new OfflineSorter(OfflineSorter.DEFAULT_COMPARER, OfflineSorter.BufferSize.Megabytes(1), OfflineSorter.DefaultTempDir, OfflineSorter.MAX_TEMPFILES), GenerateRandom((int)OfflineSorter.MB * 20));
            Assert.AreEqual(1, sortInfo.MergeRounds);
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestSmallRandom_AsStream()
        {
            // Sort 20 mb worth of data with 1mb buffer.
            OfflineSorter.SortInfo sortInfo = CheckSortAsStream(new OfflineSorter(OfflineSorter.DEFAULT_COMPARER, OfflineSorter.BufferSize.Megabytes(1), OfflineSorter.DefaultTempDir, OfflineSorter.MAX_TEMPFILES), GenerateRandom((int)OfflineSorter.MB * 20));
            Assert.AreEqual(1, sortInfo.MergeRounds);
        }

        [Test]
        [Nightly]
        public virtual void TestLargerRandom()
        {
            // Sort 100MB worth of data with 15mb buffer.
            CheckSort(new OfflineSorter(OfflineSorter.DEFAULT_COMPARER, OfflineSorter.BufferSize.Megabytes(16), OfflineSorter.DefaultTempDir, OfflineSorter.MAX_TEMPFILES), GenerateRandom((int)OfflineSorter.MB * 100));
        }

        [Test]
        [Nightly]
        [LuceneNetSpecific]
        public virtual void TestLargerRandom_AsStream()
        {
            // Sort 100MB worth of data with 15mb buffer.
            CheckSortAsStream(new OfflineSorter(OfflineSorter.DEFAULT_COMPARER, OfflineSorter.BufferSize.Megabytes(16), OfflineSorter.DefaultTempDir, OfflineSorter.MAX_TEMPFILES), GenerateRandom((int)OfflineSorter.MB * 100));
        }

        private byte[][] GenerateRandom(int howMuchData)
        {
            JCG.List<byte[]> data = new JCG.List<byte[]>();
            while (howMuchData > 0)
            {
                byte[] current = new byte[Random.Next(256)];
                Random.NextBytes(current);
                data.Add(current);
                howMuchData -= current.Length;
            }
            byte[][] bytes = data.ToArray();
            return bytes;
        }

        internal static readonly IComparer<byte[]> unsignedByteOrderComparer = Comparer<byte[]>.Create((left,right)=> {
            int max = Math.Min(left.Length, right.Length);
            for (int i = 0, j = 0; i < max; i++, j++)
            {
                int diff = (left[i] & 0xff) - (right[j] & 0xff);
                if (diff != 0)
                {
                    return diff;
                }
            }
            return left.Length - right.Length;
        });

        /// <summary>
        /// Check sorting data on an instance of <seealso cref="OfflineSorter"/>.
        /// </summary>
        private OfflineSorter.SortInfo CheckSort(OfflineSorter sort, byte[][] data)
        {
            FileInfo unsorted = WriteAll("unsorted", data);

            Array.Sort(data, unsignedByteOrderComparer);
            FileInfo golden = WriteAll("golden", data);

            FileInfo sorted = new FileInfo(Path.Combine(tempDir.FullName, "sorted"));
            OfflineSorter.SortInfo sortInfo = sort.Sort(unsorted, sorted);
            //System.out.println("Input size [MB]: " + unsorted.Length() / (1024 * 1024));
            //System.out.println(sortInfo);

            AssertFilesIdentical(golden, sorted);
            return sortInfo;
        }

        /// <summary>
        /// Check sorting data on an instance of <seealso cref="OfflineSorter"/>.
        /// </summary>
        private OfflineSorter.SortInfo CheckSortAsStream(OfflineSorter sort, byte[][] data)
        {
            using FileStream unsorted = WriteAllAsStream("unsorted", data);

            Array.Sort(data, unsignedByteOrderComparer);
            using FileStream golden = WriteAllAsStream("golden", data);

            string sortedFile = Path.Combine(tempDir.FullName, "sorted");
            using FileStream sorted = new FileStream(sortedFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, bufferSize: OfflineSorter.DEFAULT_FILESTREAM_BUFFER_SIZE, FileOptions.DeleteOnClose);
            OfflineSorter.SortInfo sortInfo = sort.Sort(unsorted, sorted);
            //System.out.println("Input size [MB]: " + unsorted.Length() / (1024 * 1024));
            //System.out.println(sortInfo);

            AssertFilesIdentical(golden, sorted);
            return sortInfo;
        }


        /// <summary>
        /// Make sure two files are byte-byte identical.
        /// </summary>
        private void AssertFilesIdentical(FileInfo golden, FileInfo sorted)
        {
            Assert.AreEqual(golden.Length, sorted.Length);

            byte[] buf1 = new byte[64 * 1024];
            byte[] buf2 = new byte[64 * 1024];
            int len;
            using Stream is1 = golden.Open(FileMode.Open, FileAccess.Read, FileShare.Delete);
            using Stream is2 = sorted.Open(FileMode.Open, FileAccess.Read, FileShare.Delete);
            while ((len = is1.Read(buf1, 0, buf1.Length)) > 0)
            {
                is2.Read(buf2, 0, len);
                for (int i = 0; i < len; i++)
                {
                    Assert.AreEqual(buf1[i], buf2[i]);
                }
            }
        }

        /// <summary>
        /// Make sure two files are byte-byte identical.
        /// </summary>
        // LUCENENET specific - test using FileStream rather than FileInfo
        private void AssertFilesIdentical(FileStream golden, FileStream sorted)
        {
            Assert.AreEqual(golden.Length, sorted.Length);

            byte[] buf1 = new byte[64 * 1024];
            byte[] buf2 = new byte[64 * 1024];
            int len;
            Stream is1 = golden;
            Stream is2 = sorted;
            while ((len = is1.Read(buf1, 0, buf1.Length)) > 0)
            {
                is2.Read(buf2, 0, len);
                for (int i = 0; i < len; i++)
                {
                    Assert.AreEqual(buf1[i], buf2[i]);
                }
            }
        }

        private FileInfo WriteAll(string name, byte[][] data)
        {
            FileInfo file = new FileInfo(Path.Combine(tempDir.FullName, name));
            using (file.Create()) { }
            OfflineSorter.ByteSequencesWriter w = new OfflineSorter.ByteSequencesWriter(file);
            foreach (byte[] datum in data)
            {
                w.Write(datum);
            }
            w.Dispose();
            return file;
        }

        // LUCENENET specific - test using FileStream rather than FileInfo
        private FileStream WriteAllAsStream(string name, byte[][] data)
        {
            FileInfo file = new FileInfo(Path.Combine(tempDir.FullName, name));
            var stream = new FileStream(file.FullName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, bufferSize: OfflineSorter.DEFAULT_FILESTREAM_BUFFER_SIZE, FileOptions.DeleteOnClose);
            OfflineSorter.ByteSequencesWriter w = new OfflineSorter.ByteSequencesWriter(stream, leaveOpen: true);
            foreach (byte[] datum in data)
            {
                w.Write(datum);
            }
            w.Dispose();
            stream.Position = 0; // LUCENENET specific - reset the position back to the start of the file so we don't need to reopen it.
            return stream;
        }

        [Test]
        public virtual void TestRamBuffer()
        {
            int numIters = AtLeast(10000);
            for (int i = 0; i < numIters; i++)
            {
                OfflineSorter.BufferSize.Megabytes(1 + Random.Next(2047));
            }
            OfflineSorter.BufferSize.Megabytes(2047);
            OfflineSorter.BufferSize.Megabytes(1);

            // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            Assert.Throws<ArgumentOutOfRangeException>(() => OfflineSorter.BufferSize.Megabytes(2048), "max mb is 2047");
            Assert.Throws<ArgumentOutOfRangeException>(() => OfflineSorter.BufferSize.Megabytes(0), "min mb is 0.5");
            Assert.Throws<ArgumentOutOfRangeException>(() => OfflineSorter.BufferSize.Megabytes(-1), "min mb is 0.5");
        }
    }
}