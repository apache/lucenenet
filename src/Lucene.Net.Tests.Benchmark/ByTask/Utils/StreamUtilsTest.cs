using ICSharpCode.SharpZipLib.BZip2;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Lucene.Net.Benchmarks.ByTask.Utils
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

    public class StreamUtilsTest : BenchmarkTestCase
    {
        private static readonly String TEXT = "Some-Text...";
        private DirectoryInfo testDir;

        [Test]
        public void TestGetInputStreamPlainText()
        {
            assertReadText(rawTextFile("txt"));
            assertReadText(rawTextFile("TXT"));
        }

        [Test]
        public void TestGetInputStreamGzip()
        {
            assertReadText(rawGzipFile("gz"));
            assertReadText(rawGzipFile("gzip"));
            assertReadText(rawGzipFile("GZ"));
            assertReadText(rawGzipFile("GZIP"));
        }

        [Test]
        public void TestGetInputStreamBzip2()
        {
            assertReadText(rawBzip2File("bz2"));
            assertReadText(rawBzip2File("bzip"));
            assertReadText(rawBzip2File("BZ2"));
            assertReadText(rawBzip2File("BZIP"));
        }

        [Test]
        public void TestGetOutputStreamBzip2()
        {
            assertReadText(autoOutFile("bz2"));
            assertReadText(autoOutFile("bzip"));
            assertReadText(autoOutFile("BZ2"));
            assertReadText(autoOutFile("BZIP"));
        }

        [Test]
        public void TestGetOutputStreamGzip()
        {
            assertReadText(autoOutFile("gz"));
            assertReadText(autoOutFile("gzip"));
            assertReadText(autoOutFile("GZ"));
            assertReadText(autoOutFile("GZIP"));
        }

        [Test]
        public void TestGetOutputStreamPlain()
        {
            assertReadText(autoOutFile("txt"));
            assertReadText(autoOutFile("text"));
            assertReadText(autoOutFile("TXT"));
            assertReadText(autoOutFile("TEXT"));
        }

        private FileInfo rawTextFile(String ext)
        {
            FileInfo f = new FileInfo(Path.Combine(testDir.FullName, "testfile." + ext));
            using (TextWriter w = new StreamWriter(new FileStream(f.FullName, FileMode.Create, FileAccess.Write), Encoding.UTF8))
                w.WriteLine(TEXT);
            return f;
        }

        private FileInfo rawGzipFile(String ext)
        {
            FileInfo f = new FileInfo(Path.Combine(testDir.FullName, "testfile." + ext));
            using (Stream os = new GZipStream(new FileStream(f.FullName, FileMode.Create, FileAccess.Write), CompressionMode.Compress)) //new CompressorStreamFactory().createCompressorOutputStream(CompressorStreamFactory.GZIP, new FileOutputStream(f));
                writeText(os);
            return f;
        }

        private FileInfo rawBzip2File(String ext)
        {
            FileInfo f = new FileInfo(Path.Combine(testDir.FullName, "testfile." + ext));
            Stream os = new BZip2OutputStream(new FileStream(f.FullName, FileMode.Create, FileAccess.Write));  // new CompressorStreamFactory().createCompressorOutputStream(CompressorStreamFactory.BZIP2, new FileOutputStream(f));
                writeText(os);
            return f;
        }

        private FileInfo autoOutFile(String ext)
        {
            FileInfo f = new FileInfo(Path.Combine(testDir.FullName, "testfile." + ext));
            Stream os = StreamUtils.GetOutputStream(f);
            writeText(os);
            return f;
        }

        private void writeText(Stream os)
        {
            TextWriter w = new StreamWriter(os, Encoding.UTF8);
            w.WriteLine(TEXT);
            w.Dispose();
        }

        private void assertReadText(FileInfo f)
        {
            Stream ir = StreamUtils.GetInputStream(f);
            TextReader r = new StreamReader(ir, Encoding.UTF8);
            String line = r.ReadLine();
            assertEquals("Wrong text found in " + f.Name, TEXT, line);
            r.Dispose();
        }

        public override void SetUp()
        {
            base.SetUp();
            testDir = new DirectoryInfo(Path.Combine(getWorkDir().FullName, "ContentSourceTest"));
            TestUtil.Rm(testDir);
            //assertTrue(testDir.mkdirs());
            testDir.Create();
            assertTrue(Directory.Exists(testDir.FullName));
        }

        public override void TearDown()
        {
            TestUtil.Rm(testDir);
            base.TearDown();
        }
    }
}
