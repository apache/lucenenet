using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Support;
using NUnit.Framework;
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


    //LUCENE PORT NOTE: The corresponding file was left out of the port due to being experimental on not porting properly
    
    //using BufferSize = Lucene.Net.Util.OfflineSorter.BufferSize;
    //using ByteSequencesWriter = Lucene.Net.Util.OfflineSorter.ByteSequencesWriter;
    //using SortInfo = Lucene.Net.Util.OfflineSorter.SortInfo;

    /// <summary>
    /// Tests for on-disk merge sorting.
    /// </summary>
    [TestFixture]
    public class TestOfflineSorter : LuceneTestCase
    {
        private DirectoryInfo TempDir;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            TempDir = CreateTempDir("mergesort");
            DeleteTestFiles();
            TempDir.Create();
        }

        [TearDown]
        public override void TearDown()
        {
            DeleteTestFiles();
            base.TearDown();
        }

        private void DeleteTestFiles()
        {
            if (TempDir != null)
            {
                if (Directory.Exists(TempDir.FullName))
                {
                    foreach (var file in TempDir.GetFiles())
                    {
                        file.Delete();
                    }
                    TempDir.Delete();
                }
            }
        }

        [Test]
        public virtual void TestEmpty()
        {
            CheckSort(new OfflineSorter(), new byte[][] { });
        }

        [Test]
        public virtual void TestSingleLine()
        {
            CheckSort(new OfflineSorter(), new byte[][] { "Single line only.".GetBytes(IOUtils.CHARSET_UTF_8) });
        }

        [Test, LongRunningTest, MaxTime(120000)]
        public virtual void TestIntermediateMerges()
        {
            // Sort 20 mb worth of data with 1mb buffer, binary merging.
            OfflineSorter.SortInfo info = CheckSort(new OfflineSorter(OfflineSorter.DEFAULT_COMPARATOR, OfflineSorter.BufferSize.Megabytes(1), OfflineSorter.DefaultTempDir(), 2), GenerateRandom((int)OfflineSorter.MB * 20));
            Assert.IsTrue(info.MergeRounds > 10);
        }

        [Test, MaxTime(120000), LongRunningTest]
        public virtual void TestSmallRandom()
        {
            // Sort 20 mb worth of data with 1mb buffer.
            OfflineSorter.SortInfo sortInfo = CheckSort(new OfflineSorter(OfflineSorter.DEFAULT_COMPARATOR, OfflineSorter.BufferSize.Megabytes(1), OfflineSorter.DefaultTempDir(), OfflineSorter.MAX_TEMPFILES), GenerateRandom((int)OfflineSorter.MB * 20));
            Assert.AreEqual(1, sortInfo.MergeRounds);
        }

        [Test, MaxTime(300000), LongRunningTest]
        public virtual void TestLargerRandom()
        {
            // Sort 100MB worth of data with 15mb buffer.
            CheckSort(new OfflineSorter(OfflineSorter.DEFAULT_COMPARATOR, OfflineSorter.BufferSize.Megabytes(16), OfflineSorter.DefaultTempDir(), OfflineSorter.MAX_TEMPFILES), GenerateRandom((int)OfflineSorter.MB * 100));
        }

        private byte[][] GenerateRandom(int howMuchData)
        {
            List<byte[]> data = new List<byte[]>();
            while (howMuchData > 0)
            {
                byte[] current = new byte[Random().Next(256)];
                Random().NextBytes((byte[])(Array)current);
                data.Add(current);
                howMuchData -= current.Length;
            }
            byte[][] bytes = data.ToArray();
            return bytes;
        }

        internal static readonly IComparer<byte[]> unsignedByteOrderComparator = new ComparatorAnonymousInnerClassHelper();

        private class ComparatorAnonymousInnerClassHelper : IComparer<byte[]>
        {
            public ComparatorAnonymousInnerClassHelper()
            {
            }

            public virtual int Compare(byte[] left, byte[] right)
            {
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
            }
        }
        /// <summary>
        /// Check sorting data on an instance of <seealso cref="OfflineSorter"/>.
        /// </summary>
        private OfflineSorter.SortInfo CheckSort(OfflineSorter sort, byte[][] data)
        {
            FileInfo unsorted = WriteAll("unsorted", data);

            Array.Sort(data, unsignedByteOrderComparator);
            FileInfo golden = WriteAll("golden", data);

            FileInfo sorted = new FileInfo(Path.Combine(TempDir.FullName, "sorted"));
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
            //DataInputStream is1 = new DataInputStream(new FileInputStream(golden));
            //DataInputStream is2 = new DataInputStream(new FileInputStream(sorted));
            using (Stream is1 = golden.Open(FileMode.Open, FileAccess.Read, FileShare.Delete))
            {
                using (Stream is2 = sorted.Open(FileMode.Open, FileAccess.Read, FileShare.Delete))
                {
                    while ((len = is1.Read(buf1, 0, buf1.Length)) > 0)
                    {
                        is2.Read(buf2, 0, len);
                        for (int i = 0; i < len; i++)
                        {
                            Assert.AreEqual(buf1[i], buf2[i]);
                        }
                    }
                    //IOUtils.Close(is1, is2);
                }
            }
        }

        private FileInfo WriteAll(string name, byte[][] data)
        {
            FileInfo file = new FileInfo(Path.Combine(TempDir.FullName, name));
            using (file.Create()) { }
            OfflineSorter.ByteSequencesWriter w = new OfflineSorter.ByteSequencesWriter(file);
            foreach (byte[] datum in data)
            {
                w.Write(datum);
            }
            w.Dispose();
            return file;
        }

        [Test]
        public virtual void TestRamBuffer()
        {
            int numIters = AtLeast(10000);
            for (int i = 0; i < numIters; i++)
            {
                OfflineSorter.BufferSize.Megabytes(1 + Random().Next(2047));
            }
            OfflineSorter.BufferSize.Megabytes(2047);
            OfflineSorter.BufferSize.Megabytes(1);

            try
            {
                OfflineSorter.BufferSize.Megabytes(2048);
                Assert.Fail("max mb is 2047");
            }
            catch (System.ArgumentException e)
            {
            }

            try
            {
                OfflineSorter.BufferSize.Megabytes(0);
                Assert.Fail("min mb is 0.5");
            }
            catch (System.ArgumentException e)
            {
            }

            try
            {
                OfflineSorter.BufferSize.Megabytes(-1);
                Assert.Fail("min mb is 0.5");
            }
            catch (System.ArgumentException e)
            {
            }
        }
    }
}