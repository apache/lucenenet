using ICSharpCode.SharpZipLib.BZip2;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Benchmarks.ByTask.Tasks;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
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
    /// Tests the functionality of {@link LineDocSource}.
    /// </summary>
    public class LineDocSourceTest : BenchmarkTestCase
    {
        //private static final CompressorStreamFactory csFactory = new CompressorStreamFactory();

        private void createBZ2LineFile(FileInfo file, bool addHeader)
        {
            Stream @out = new FileStream(file.FullName, FileMode.Create, FileAccess.Write);
            @out = new BZip2OutputStream(@out); // csFactory.createCompressorOutputStream("bzip2", @out);
            TextWriter writer = new StreamWriter(@out, Encoding.UTF8);
            writeDocsToFile(writer, addHeader, null);
            writer.Dispose();
        }

        private void writeDocsToFile(TextWriter writer, bool addHeader, IDictionary<string, string> otherFields)
        {
            if (addHeader)
            {
                writer.Write(WriteLineDocTask.FIELDS_HEADER_INDICATOR);
                writer.Write(WriteLineDocTask.SEP);
                writer.Write(DocMaker.TITLE_FIELD);
                writer.Write(WriteLineDocTask.SEP);
                writer.Write(DocMaker.DATE_FIELD);
                writer.Write(WriteLineDocTask.SEP);
                writer.Write(DocMaker.BODY_FIELD);
                if (otherFields != null)
                {
                    // additional field names in the header 
                    foreach (Object fn in otherFields.Keys)
                    {
                        writer.Write(WriteLineDocTask.SEP);
                        writer.Write(fn.toString());
                    }
                }
                writer.WriteLine();
            }
            StringBuilder doc = new StringBuilder();
            doc.append("title").append(WriteLineDocTask.SEP).append("date").append(WriteLineDocTask.SEP).append(DocMaker.BODY_FIELD);
            if (otherFields != null)
            {
                // additional field values in the doc line 
                foreach (Object fv in otherFields.Values)
                {
                    doc.append(WriteLineDocTask.SEP).append(fv.toString());
                }
            }
            writer.Write(doc.toString());
            writer.WriteLine();
        }

        private void createRegularLineFile(FileInfo file, bool addHeader)
        {
            Stream @out = new FileStream(file.FullName, FileMode.Create, FileAccess.Write);
            TextWriter writer = new StreamWriter(@out, Encoding.UTF8);
            writeDocsToFile(writer, addHeader, null);
            writer.Dispose();
        }

        private void createRegularLineFileWithMoreFields(FileInfo file, params String[] extraFields)
        {
            Stream @out = new FileStream(file.FullName, FileMode.Create, FileAccess.Write);
            TextWriter writer = new StreamWriter(@out, Encoding.UTF8);
            Dictionary<string, string> p = new Dictionary<string, string>();
            foreach (String f in extraFields)
            {
                p[f] = f;
            }
            writeDocsToFile(writer, true, p);
            writer.Dispose();
        }

        private void doIndexAndSearchTest(FileInfo file, Type lineParserClass, String storedField)
        {
            doIndexAndSearchTestWithRepeats(file, lineParserClass, 1, storedField); // no extra repetitions
            doIndexAndSearchTestWithRepeats(file, lineParserClass, 2, storedField); // 1 extra repetition
            doIndexAndSearchTestWithRepeats(file, lineParserClass, 4, storedField); // 3 extra repetitions
        }

        private void doIndexAndSearchTestWithRepeats(FileInfo file,
            Type lineParserClass, int numAdds, String storedField)
        {

            IndexReader reader = null;
            IndexSearcher searcher = null;
            PerfRunData runData = null;
            try
            {
                Dictionary<string, string> props = new Dictionary<string, string>();

                // LineDocSource specific settings.
                props["docs.file"] = file.FullName;
                if (lineParserClass != null)
                {
                    props["line.parser"] = lineParserClass.AssemblyQualifiedName;
                }

                // Indexing configuration.
                props["analyzer"] = typeof(WhitespaceAnalyzer).AssemblyQualifiedName;
                props["content.source"] = typeof(LineDocSource).AssemblyQualifiedName;
                props["directory"] = "RAMDirectory";
                props["doc.stored"] = "true";
                props["doc.index.props"] = "true";

                // Create PerfRunData
                Config config = new Config(props);
                runData = new PerfRunData(config);

                TaskSequence tasks = new TaskSequence(runData, "testBzip2", null, false);
                tasks.AddTask(new CreateIndexTask(runData));
                for (int i = 0; i < numAdds; i++)
                {
                    tasks.AddTask(new AddDocTask(runData));
                }
                tasks.AddTask(new CloseIndexTask(runData));
                try
                {
                    tasks.DoLogic();
                }
                finally
                {
                    tasks.Dispose();
                }

                reader = DirectoryReader.Open(runData.Directory);
                searcher = NewSearcher(reader);
                TopDocs td = searcher.Search(new TermQuery(new Term("body", "body")), 10);
                assertEquals(numAdds, td.TotalHits);
                assertNotNull(td.ScoreDocs[0]);

                if (storedField is null)
                {
                    storedField = DocMaker.BODY_FIELD; // added to all docs and satisfies field-name == value
                }
                assertEquals("Wrong field value", storedField, searcher.Doc(0).Get(storedField));
            }
            finally
            {
                IOUtils.Dispose(reader, runData);
            }

        }

        /* Tests LineDocSource with a bzip2 input stream. */
        [Test]
        public void TestBZip2()
        {
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line.bz2"));
            createBZ2LineFile(file, true);
            doIndexAndSearchTest(file, null, null);
        }

        [Test]
        public void TestBZip2NoHeaderLine()
        {
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line.bz2"));
            createBZ2LineFile(file, false);
            doIndexAndSearchTest(file, null, null);
        }

        [Test]
        public void TestRegularFile()
        {
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line"));
            createRegularLineFile(file, true);
            doIndexAndSearchTest(file, null, null);
        }

        [Test]
        public void TestRegularFileSpecialHeader()
        {
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line"));
            createRegularLineFile(file, true);
            doIndexAndSearchTest(file, typeof(HeaderLineParser), null);
        }

        [Test]
        public void TestRegularFileNoHeaderLine()
        {
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line"));
            createRegularLineFile(file, false);
            doIndexAndSearchTest(file, null, null);
        }

        [Test]
        public void TestInvalidFormat()
        {
            String[]
            testCases = new String[] {
                "", // empty line
                "title", // just title
                "title" + WriteLineDocTask.SEP, // title + SEP
                "title" + WriteLineDocTask.SEP + "body", // title + SEP + body
                                                        // note that title + SEP + body + SEP is a valid line, which results in an
                                                        // empty body
            };

            for (int i = 0; i < testCases.Length; i++)
            {
                FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line"));
                TextWriter writer = new StreamWriter(new FileStream(file.FullName, FileMode.Create, FileAccess.Write), Encoding.UTF8);
                writer.Write(testCases[i]);
                writer.WriteLine();
                writer.Dispose();
                try
                {
                    doIndexAndSearchTest(file, null, null);
                    fail("Some exception should have been thrown for: [" + testCases[i] + "]");
                }
                catch (Exception e) when (e.IsException())
                {
                    // expected.
                }
            }
        }

        /** Doc Name is not part of the default header */
        [Test]
        public void TestWithDocsName()
        {
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line"));
            createRegularLineFileWithMoreFields(file, DocMaker.NAME_FIELD);
            doIndexAndSearchTest(file, null, DocMaker.NAME_FIELD);
        }

        /** Use fields names that are not defined in Docmaker and so will go to Properties */
        [Test]
        public void TestWithProperties()
        {
            FileInfo file = new FileInfo(Path.Combine(getWorkDir().FullName, "one-line"));
            String specialField = "mySpecialField";
            createRegularLineFileWithMoreFields(file, specialField);
            doIndexAndSearchTest(file, null, specialField);
        }
    }
}
