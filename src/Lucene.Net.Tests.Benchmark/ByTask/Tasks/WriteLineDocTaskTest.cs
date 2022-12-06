using ICSharpCode.SharpZipLib.BZip2;
using J2N.Text;
using J2N.Threading;
using Lucene.Net.Benchmarks.ByTask.Feeds;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Documents;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.ByTask.Tasks
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
    /// Tests the functionality of {@link WriteLineDocTask}.
    /// </summary>
    public class WriteLineDocTaskTest : BenchmarkTestCase
    {
        // class has to be public so that Class.forName.newInstance() will work
        public sealed class WriteLineDocMaker : DocMaker
        {

            public override Document MakeDocument()
            {
                Document doc = new Document();
                doc.Add(new StringField(BODY_FIELD, "body", Field.Store.NO));
                doc.Add(new StringField(TITLE_FIELD, "title", Field.Store.NO));
                doc.Add(new StringField(DATE_FIELD, "date", Field.Store.NO));
                return doc;
            }

        }

        // class has to be public so that Class.forName.newInstance() will work
        public sealed class NewLinesDocMaker : DocMaker
        {

            public override Document MakeDocument()
            {
                Document doc = new Document();
                doc.Add(new StringField(BODY_FIELD, "body\r\ntext\ttwo", Field.Store.NO));
                doc.Add(new StringField(TITLE_FIELD, "title\r\ntext", Field.Store.NO));
                doc.Add(new StringField(DATE_FIELD, "date\r\ntext", Field.Store.NO));
                return doc;
            }

        }

        // class has to be public so that Class.forName.newInstance() will work
        public sealed class NoBodyDocMaker : DocMaker
        {
            public override Document MakeDocument()
            {
                Document doc = new Document();
                doc.Add(new StringField(TITLE_FIELD, "title", Field.Store.NO));
                doc.Add(new StringField(DATE_FIELD, "date", Field.Store.NO));
                return doc;
            }
        }

        // class has to be public so that Class.forName.newInstance() will work
        public sealed class NoTitleDocMaker : DocMaker
        {
            public override Document MakeDocument()
            {
                Document doc = new Document();
                doc.Add(new StringField(BODY_FIELD, "body", Field.Store.NO));
                doc.Add(new StringField(DATE_FIELD, "date", Field.Store.NO));
                return doc;
            }
        }

        // class has to be public so that Class.forName.newInstance() will work
        public sealed class JustDateDocMaker : DocMaker
        {
            public override Document MakeDocument()
            {
                Document doc = new Document();
                doc.Add(new StringField(DATE_FIELD, "date", Field.Store.NO));
                return doc;
            }
        }

        // class has to be public so that Class.forName.newInstance() will work
        // same as JustDate just that this one is treated as legal
        public sealed class LegalJustDateDocMaker : DocMaker
        {
            public override Document MakeDocument()
            {
                Document doc = new Document();
                doc.Add(new StringField(DATE_FIELD, "date", Field.Store.NO));
                return doc;
            }
        }

        // class has to be public so that Class.forName.newInstance() will work
        public sealed class EmptyDocMaker : DocMaker
        {
            public override Document MakeDocument()
            {
                return new Document();
            }
        }

        // class has to be public so that Class.forName.newInstance() will work
        public sealed class ThreadingDocMaker : DocMaker
        {

            public override Document MakeDocument()
            {
                Document doc = new Document();
                String name = Thread.CurrentThread.Name;
                doc.Add(new StringField(BODY_FIELD, "body_" + name, Field.Store.NO));
                doc.Add(new StringField(TITLE_FIELD, "title_" + name, Field.Store.NO));
                doc.Add(new StringField(DATE_FIELD, "date_" + name, Field.Store.NO));
                return doc;
            }

        }

        private PerfRunData createPerfRunData(FileInfo file,
                                              bool allowEmptyDocs,
                                              String docMakerName)
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            props["doc.maker"] = docMakerName;
            props["line.file.out"] = file.FullName;
            props["directory"] = "RAMDirectory"; // no accidental FS dir.
            if (allowEmptyDocs)
            {
                props["sufficient.fields"] = ",";
            }
            if (typeof(LegalJustDateDocMaker).Equals(Type.GetType(docMakerName)))
            {
                props["line.fields"] = DocMaker.DATE_FIELD;
                props["sufficient.fields"] = DocMaker.DATE_FIELD;
            }
            Config config = new Config(props);
            return new PerfRunData(config);
        }

        private void doReadTest(FileInfo file, FileType fileType, String expTitle,
                                String expDate, String expBody)
        {
            Stream input = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            switch (fileType)
            {
                case FileType.BZIP2:
                    input = new BZip2InputStream(input); 
                    break;
                case FileType.GZIP:
                    input = new GZipStream(input, CompressionMode.Decompress);   
                    break;
                case FileType.PLAIN:
                    break; // nothing to do
                default:
                    assertFalse("Unknown file type!", true); //fail, should not happen
                    break;
            }
            TextReader br = new StreamReader(input, Encoding.UTF8);
            try
            {
                String line = br.ReadLine();
                assertHeaderLine(line);
                line = br.ReadLine();
                assertNotNull(line);
                String[] parts = line.Split(WriteLineDocTask.SEP).TrimEnd();
                int numExpParts = expBody is null ? 2 : 3;
                assertEquals(numExpParts, parts.Length);
                assertEquals(expTitle, parts[0]);
                assertEquals(expDate, parts[1]);
                if (expBody != null)
                {
                    assertEquals(expBody, parts[2]);
                }
                assertNull(br.ReadLine());
            }
            finally
            {
                br.Dispose();
            }
        }

        internal static void assertHeaderLine(String line)
        {
            assertTrue("First line should be a header line", line != null && line.StartsWith(WriteLineDocTask.FIELDS_HEADER_INDICATOR, StringComparison.Ordinal));
        }

        /* Tests WriteLineDocTask with a bzip2 format. */
        [Test]
        public void TestBZip2()
        {

            // Create a document in bz2 format.
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line.bz2"));
            PerfRunData runData = createPerfRunData(file, false, typeof(WriteLineDocMaker).AssemblyQualifiedName);
            WriteLineDocTask wldt = new WriteLineDocTask(runData);
            wldt.DoLogic();
            wldt.Dispose();


            doReadTest(file, FileType.BZIP2, "title", "date", "body");
        }

        /* Tests WriteLineDocTask with a gzip format. */
        [Test]
        public void TestGZip()
        {

            // Create a document in gz format.
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line.gz"));
            PerfRunData runData = createPerfRunData(file, false, typeof(WriteLineDocMaker).AssemblyQualifiedName);
            WriteLineDocTask wldt = new WriteLineDocTask(runData);
            wldt.DoLogic();
            wldt.Dispose();


            doReadTest(file, FileType.GZIP, "title", "date", "body");
        }

        [Test]
        public void TestRegularFile()
        {

            // Create a document in regular format.
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line"));
            PerfRunData runData = createPerfRunData(file, false, typeof(WriteLineDocMaker).AssemblyQualifiedName);
            WriteLineDocTask wldt = new WriteLineDocTask(runData);
            wldt.DoLogic();
            wldt.Dispose();


            doReadTest(file, FileType.PLAIN, "title", "date", "body");
        }

        [Test]
        public void TestCharsReplace()
        {
            // WriteLineDocTask replaced only \t characters w/ a space, since that's its
            // separator char. However, it didn't replace newline characters, which
            // resulted in errors in LineDocSource.
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line"));
            PerfRunData runData = createPerfRunData(file, false, typeof(NewLinesDocMaker).AssemblyQualifiedName);
            WriteLineDocTask wldt = new WriteLineDocTask(runData);
            wldt.DoLogic();
            wldt.Dispose();


            doReadTest(file, FileType.PLAIN, "title text", "date text", "body text two");
        }

        [Test]
        public void TestEmptyBody()
        {
            // WriteLineDocTask threw away documents w/ no BODY element, even if they
            // had a TITLE element (LUCENE-1755). It should throw away documents if they
            // don't have BODY nor TITLE
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line"));
            PerfRunData runData = createPerfRunData(file, false, typeof(NoBodyDocMaker).AssemblyQualifiedName);
            WriteLineDocTask wldt = new WriteLineDocTask(runData);
            wldt.DoLogic();
            wldt.Dispose();


            doReadTest(file, FileType.PLAIN, "title", "date", null);
        }

        [Test]
        public void TestEmptyTitle()
        {
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line"));
            PerfRunData runData = createPerfRunData(file, false, typeof(NoTitleDocMaker).AssemblyQualifiedName);
            WriteLineDocTask wldt = new WriteLineDocTask(runData);
            wldt.DoLogic();
            wldt.Dispose();


            doReadTest(file, FileType.PLAIN, "", "date", "body");
        }

        /** Fail by default when there's only date */
        [Test]
        public void TestJustDate()
        {
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line"));
            PerfRunData runData = createPerfRunData(file, false, typeof(JustDateDocMaker).AssemblyQualifiedName);
            WriteLineDocTask wldt = new WriteLineDocTask(runData);
            wldt.DoLogic();
            wldt.Dispose();

            TextReader br = new StreamReader(new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8);
            try
            {
                String line = br.ReadLine();
                assertHeaderLine(line);
                line = br.ReadLine();
                assertNull(line);
            }
            finally
            {
                br.Dispose();
            }
        }

        [Test]
        public void TestLegalJustDate()
        {
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line"));
            PerfRunData runData = createPerfRunData(file, false, typeof(LegalJustDateDocMaker).AssemblyQualifiedName);
            WriteLineDocTask wldt = new WriteLineDocTask(runData);
            wldt.DoLogic();
            wldt.Dispose();

            TextReader br = new StreamReader(new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8);
            try
            {
                String line = br.ReadLine();
                assertHeaderLine(line);
                line = br.ReadLine();
                assertNotNull(line);
            }
            finally
            {
                br.Dispose();
            }
        }

        [Test]
        public void TestEmptyDoc()
        {
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line"));
            PerfRunData runData = createPerfRunData(file, true, typeof(EmptyDocMaker).AssemblyQualifiedName);
            WriteLineDocTask wldt = new WriteLineDocTask(runData);
            wldt.DoLogic();
            wldt.Dispose();

            TextReader br = new StreamReader(new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8);
            try
            {
                String line = br.ReadLine();
                assertHeaderLine(line);
                line = br.ReadLine();
                assertNotNull(line);
            }
            finally
            {
                br.Dispose();
            }
        }
        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly WriteLineDocTask wldt;
            public ThreadAnonymousClass(string name, WriteLineDocTask wldt)
                : base(name)
            {
                this.wldt = wldt;
            }

            public override void Run()
            {
                try
                {
                    wldt.DoLogic();
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }

        [Test]
        public void TestMultiThreaded()
        {
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line"));
            PerfRunData runData = createPerfRunData(file, false, typeof(ThreadingDocMaker).AssemblyQualifiedName);
            ThreadJob[] threads = new ThreadJob[10];
            using (WriteLineDocTask wldt = new WriteLineDocTask(runData))
            {
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i] = new ThreadAnonymousClass("t" + i, wldt);
                }

                foreach (ThreadJob t in threads) t.Start();
                foreach (ThreadJob t in threads) t.Join();

            } // wldt.Dispose();

            ISet<String> ids = new JCG.HashSet<string>();
            TextReader br = new StreamReader(new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.None), Encoding.UTF8);
            try
            {
                String line = br.ReadLine();
                assertHeaderLine(line); // header line is written once, no matter how many threads there are
                for (int i = 0; i < threads.Length; i++)
                {
                    line = br.ReadLine();
                    assertNotNull($"line for index {i} is missing", line); // LUCENENET specific - ensure the line is there before splitting
                    String[] parts = line.Split(WriteLineDocTask.SEP).TrimEnd();
                    assertEquals(line, 3, parts.Length);
                    // check that all thread names written are the same in the same line
                    String tname = parts[0].Substring(parts[0].IndexOf('_'));
                    ids.add(tname);
                    assertEquals(tname, parts[1].Substring(parts[1].IndexOf('_')));
                    assertEquals(tname, parts[2].Substring(parts[2].IndexOf('_')));
                }
                // only threads.length lines should exist
                assertNull(br.ReadLine());
                assertEquals(threads.Length, ids.size());
            }
            finally
            {
                br.Dispose();
            }
        }
    }
}
