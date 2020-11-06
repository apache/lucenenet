using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Documents;
using Lucene.Net.Support.IO;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JCG = J2N.Collections.Generic;
using static Lucene.Net.Benchmarks.ByTask.Feeds.TrecDocParser;

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

    public class TrecContentSourceTest : LuceneTestCase
    {
        /** A TrecDocMaker which works on a String and not files. */
        private class StringableTrecSource : TrecContentSource
        {


            private String docs = null;

            public StringableTrecSource(String docs, bool forever)
            {
                this.docs = docs;
                this.m_forever = forever;
            }

            internal override void OpenNextFile()
            {
                if (reader != null)
                {
                    if (!m_forever)
                    {
                        throw new NoMoreDataException();
                    }
                    ++iteration;
                }

                reader = new StringReader(docs);
            }

            public override void SetConfig(Config config)
            {
                htmlParser = new DemoHTMLParser();
            }
        }

        private void assertDocData(DocData dd, String expName, String expTitle,
                                   String expBody, DateTime? expDate)
        {
            assertNotNull(dd);
            assertEquals(expName, dd.Name);
            assertEquals(expTitle, dd.Title);
            assertTrue(dd.Body.IndexOf(expBody, StringComparison.Ordinal) != -1);
            DateTime? date = dd.Date != null ? DateTools.StringToDate(dd.Date) : (DateTime?)null;
            assertEquals(expDate, date);
        }

        private void assertNoMoreDataException(StringableTrecSource stdm)
        {
            bool thrown = false;
            try
            {
                stdm.GetNextDocData(null);
            }
#pragma warning disable 168
            catch (NoMoreDataException e)
#pragma warning restore 168
            {
                thrown = true;
            }
            assertTrue("Expecting NoMoreDataException", thrown);
        }

        [Test]
        public void TestOneDocument()
        {
            String docs = "<DOC>\r\n" +
                          "<DOCNO>TEST-000</DOCNO>\r\n" +
                          "<DOCHDR>\r\n" +
                          "http://lucene.apache.org.trecdocmaker.test\r\n" +
                          "HTTP/1.1 200 OK\r\n" +
                          "Date: Sun, 11 Jan 2009 08:00:00 GMT\r\n" +
                          "Server: Apache/1.3.27 (Unix)\r\n" +
                          "Last-Modified: Sun, 11 Jan 2009 08:00:00 GMT\r\n" +
                          "Content-Length: 614\r\n" +
                          "Connection: close\r\n" +
                          "Content-Type: text/html\r\n" +
                          "</DOCHDR>\r\n" +
                          "<html>\r\n" +
                          "\r\n" +
                          "<head>\r\n" +
                          "<title>\r\n" +
                          "TEST-000 title\r\n" +
                          "</title>\r\n" +
                          "</head>\r\n" +
                          "\r\n" +
                          "<body>\r\n" +
                          "TEST-000 text\r\n" +
                          "\r\n" +
                          "</body>\r\n" +
                          "\r\n" +
                          "</DOC>";
            StringableTrecSource source = new StringableTrecSource(docs, false);
            source.SetConfig(null);

            DocData dd = source.GetNextDocData(new DocData());
            assertDocData(dd, "TEST-000_0", "TEST-000 title", "TEST-000 text", source
                .ParseDate("Sun, 11 Jan 2009 08:00:00 GMT"));


            assertNoMoreDataException(source);
        }

        [Test]
        public void TestTwoDocuments()
        {
            String docs = "<DOC>\r\n" +
                          "<DOCNO>TEST-000</DOCNO>\r\n" +
                          "<DOCHDR>\r\n" +
                          "http://lucene.apache.org.trecdocmaker.test\r\n" +
                          "HTTP/1.1 200 OK\r\n" +
                          "Date: Sun, 11 Jan 2009 08:00:00 GMT\r\n" +
                          "Server: Apache/1.3.27 (Unix)\r\n" +
                          "Last-Modified: Sun, 11 Jan 2009 08:00:00 GMT\r\n" +
                          "Content-Length: 614\r\n" +
                          "Connection: close\r\n" +
                          "Content-Type: text/html\r\n" +
                          "</DOCHDR>\r\n" +
                          "<html>\r\n" +
                          "\r\n" +
                          "<head>\r\n" +
                          "<title>\r\n" +
                          "TEST-000 title\r\n" +
                          "</title>\r\n" +
                          "</head>\r\n" +
                          "\r\n" +
                          "<body>\r\n" +
                          "TEST-000 text\r\n" +
                          "\r\n" +
                          "</body>\r\n" +
                          "\r\n" +
                          "</DOC>\r\n" +
                          "<DOC>\r\n" +
                          "<DOCNO>TEST-001</DOCNO>\r\n" +
                          "<DOCHDR>\r\n" +
                          "http://lucene.apache.org.trecdocmaker.test\r\n" +
                          "HTTP/1.1 200 OK\r\n" +
                          "Date: Sun, 11 Jan 2009 08:01:00 GMT\r\n" +
                          "Server: Apache/1.3.27 (Unix)\r\n" +
                          "Last-Modified: Sun, 11 Jan 2008 08:01:00 GMT\r\n" +
                          "Content-Length: 614\r\n" +
                          "Connection: close\r\n" +
                          "Content-Type: text/html\r\n" +
                          "</DOCHDR>\r\n" +
                          "<html>\r\n" +
                          "\r\n" +
                          "<head>\r\n" +
                          "<title>\r\n" +
                          "TEST-001 title\r\n" +
                          "</title>\r\n" +
                          "<meta name=\"date\" content=\"Tue&#44; 09 Dec 2003 22&#58;39&#58;08 GMT\">" +
                          "</head>\r\n" +
                          "\r\n" +
                          "<body>\r\n" +
                          "TEST-001 text\r\n" +
                          "\r\n" +
                          "</body>\r\n" +
                          "\r\n" +
                          "</DOC>";
            StringableTrecSource source = new StringableTrecSource(docs, false);
            source.SetConfig(null);

            DocData dd = source.GetNextDocData(new DocData());
            assertDocData(dd, "TEST-000_0", "TEST-000 title", "TEST-000 text", source
                .ParseDate("Sun, 11 Jan 2009 08:00:00 GMT"));

            dd = source.GetNextDocData(dd);
            assertDocData(dd, "TEST-001_0", "TEST-001 title", "TEST-001 text", source
                .ParseDate("Tue, 09 Dec 2003 22:39:08 GMT"));


            assertNoMoreDataException(source);
        }

        // If a Date: attribute is missing, make sure the document is not skipped, but
        // rather that null Data is assigned.
        [Test]
        public void TestMissingDate()
        {
            String docs = "<DOC>\r\n" +
                          "<DOCNO>TEST-000</DOCNO>\r\n" +
                          "<DOCHDR>\r\n" +
                          "http://lucene.apache.org.trecdocmaker.test\r\n" +
                          "HTTP/1.1 200 OK\r\n" +
                          "Server: Apache/1.3.27 (Unix)\r\n" +
                          "Last-Modified: Sun, 11 Jan 2009 08:00:00 GMT\r\n" +
                          "Content-Length: 614\r\n" +
                          "Connection: close\r\n" +
                          "Content-Type: text/html\r\n" +
                          "</DOCHDR>\r\n" +
                          "<html>\r\n" +
                          "\r\n" +
                          "<head>\r\n" +
                          "<title>\r\n" +
                          "TEST-000 title\r\n" +
                          "</title>\r\n" +
                          "</head>\r\n" +
                          "\r\n" +
                          "<body>\r\n" +
                          "TEST-000 text\r\n" +
                          "\r\n" +
                          "</body>\r\n" +
                          "\r\n" +
                          "</DOC>\r\n" +
                          "<DOC>\r\n" +
                          "<DOCNO>TEST-001</DOCNO>\r\n" +
                          "<DOCHDR>\r\n" +
                          "http://lucene.apache.org.trecdocmaker.test\r\n" +
                          "HTTP/1.1 200 OK\r\n" +
                          "Date: Sun, 11 Jan 2009 08:01:00 GMT\r\n" +
                          "Server: Apache/1.3.27 (Unix)\r\n" +
                          "Last-Modified: Sun, 11 Jan 2009 08:01:00 GMT\r\n" +
                          "Content-Length: 614\r\n" +
                          "Connection: close\r\n" +
                          "Content-Type: text/html\r\n" +
                          "</DOCHDR>\r\n" +
                          "<html>\r\n" +
                          "\r\n" +
                          "<head>\r\n" +
                          "<title>\r\n" +
                          "TEST-001 title\r\n" +
                          "</title>\r\n" +
                          "</head>\r\n" +
                          "\r\n" +
                          "<body>\r\n" +
                          "TEST-001 text\r\n" +
                          "\r\n" +
                          "</body>\r\n" +
                          "\r\n" +
                          "</DOC>";
            StringableTrecSource source = new StringableTrecSource(docs, false);
            source.SetConfig(null);

            DocData dd = source.GetNextDocData(new DocData());
            assertDocData(dd, "TEST-000_0", "TEST-000 title", "TEST-000 text", null);

            dd = source.GetNextDocData(dd);
            assertDocData(dd, "TEST-001_0", "TEST-001 title", "TEST-001 text", source
                .ParseDate("Sun, 11 Jan 2009 08:01:00 GMT"));


            assertNoMoreDataException(source);
        }

        // When a 'bad date' is input (unparsable date), make sure the DocData date is
        // assigned null.
        [Test]
        public void TestBadDate()
        {
            String docs = "<DOC>\r\n" +
                          "<DOCNO>TEST-000</DOCNO>\r\n" +
                          "<DOCHDR>\r\n" +
                          "http://lucene.apache.org.trecdocmaker.test\r\n" +
                          "HTTP/1.1 200 OK\r\n" +
                          "Date: Bad Date\r\n" +
                          "Server: Apache/1.3.27 (Unix)\r\n" +
                          "Last-Modified: Sun, 11 Jan 2009 08:00:00 GMT\r\n" +
                          "Content-Length: 614\r\n" +
                          "Connection: close\r\n" +
                          "Content-Type: text/html\r\n" +
                          "</DOCHDR>\r\n" +
                          "<html>\r\n" +
                          "\r\n" +
                          "<head>\r\n" +
                          "<title>\r\n" +
                          "TEST-000 title\r\n" +
                          "</title>\r\n" +
                          "</head>\r\n" +
                          "\r\n" +
                          "<body>\r\n" +
                          "TEST-000 text\r\n" +
                          "\r\n" +
                          "</body>\r\n" +
                          "\r\n" +
                          "</DOC>";
            StringableTrecSource source = new StringableTrecSource(docs, false);
            source.SetConfig(null);

            DocData dd = source.GetNextDocData(new DocData());
            assertDocData(dd, "TEST-000_0", "TEST-000 title", "TEST-000 text", null);


            assertNoMoreDataException(source);
        }

        [Test]
        public void TestForever()
        {
            String docs = "<DOC>\r\n" +
                          "<DOCNO>TEST-000</DOCNO>\r\n" +
                          //"<docno>TEST-000</docno>\r\n" +
                          "<DOCHDR>\r\n" +
                          "http://lucene.apache.org.trecdocmaker.test\r\n" +
                          "HTTP/1.1 200 OK\r\n" +
                          "Date: Sun, 11 Jan 2009 08:00:00 GMT\r\n" +
                          "Server: Apache/1.3.27 (Unix)\r\n" +
                          "Last-Modified: Sun, 11 Jan 2009 08:00:00 GMT\r\n" +
                          "Content-Length: 614\r\n" +
                          "Connection: close\r\n" +
                          "Content-Type: text/html\r\n" +
                          "</DOCHDR>\r\n" +
                          "<html>\r\n" +
                          "\r\n" +
                          "<head>\r\n" +
                          "<title>\r\n" +
                          "TEST-000 title\r\n" +
                          "</title>\r\n" +
                          "</head>\r\n" +
                          "\r\n" +
                          "<body>\r\n" +
                          "TEST-000 text\r\n" +
                          "\r\n" +
                          "</body>\r\n" +
                          "\r\n" +
                          "</DOC>";
            StringableTrecSource source = new StringableTrecSource(docs, true);
            source.SetConfig(null);

            DocData dd = source.GetNextDocData(new DocData());
            assertDocData(dd, "TEST-000_0", "TEST-000 title", "TEST-000 text", source
                .ParseDate("Sun, 11 Jan 2009 08:00:00 GMT"));

            // same document, but the second iteration changes the name.
            dd = source.GetNextDocData(dd);
            assertDocData(dd, "TEST-000_1", "TEST-000 title", "TEST-000 text", source
                .ParseDate("Sun, 11 Jan 2009 08:00:00 GMT"));
            source.Dispose();

            // Don't test that NoMoreDataException is thrown, since the forever flag is
            // turned on.
        }

        /** 
         * Open a trec content source over a directory with files of all trec path types and all
         * supported formats - bzip, gzip, txt. 
         */
        [Test]
        public void TestTrecFeedDirAllTypes()
        {
            DirectoryInfo dataDir = CreateTempDir("trecFeedAllTypes");
            using (var stream = GetDataFile("trecdocs.zip"))
                TestUtil.Unzip(stream, dataDir);
            using TrecContentSource tcs = new TrecContentSource();
            Dictionary<string, string> props = new Dictionary<string, string>();
            props["print.props"] = "false";
            props["content.source.verbose"] = "false";
            props["content.source.excludeIteration"] = "true";
            props["doc.maker.forever"] = "false";
            props["docs.dir"] = dataDir.GetCanonicalPath().Replace('\\', '/');
            props["trec.doc.parser"] = typeof(TrecParserByPath).AssemblyQualifiedName;
            props["content.source.forever"] = "false";
            tcs.SetConfig(new Config(props));
            tcs.ResetInputs();
            DocData dd = new DocData();
            int n = 0;
            bool gotExpectedException = false;
            // LUCENENET specific - skip our UNKNOWN element.
            var pathTypes = ((ParsePathType[])Enum.GetValues(typeof(ParsePathType))).Where(x => x != ParsePathType.UNKNOWN).ToArray();
            ISet<ParsePathType> unseenTypes = new JCG.HashSet<ParsePathType>(pathTypes);
            try
            {
                while (n < 100)
                { // arbiterary limit to prevent looping forever in case of test failure
                    dd = tcs.GetNextDocData(dd);
                    ++n;
                    assertNotNull("doc data " + n + " should not be null!", dd);
                    unseenTypes.Remove(tcs.currPathType);
                    switch (tcs.currPathType)
                    {
                        case ParsePathType.GOV2:
                            assertDocData(dd, "TEST-000", "TEST-000 title", "TEST-000 text", tcs.ParseDate("Sun, 11 Jan 2009 08:00:00 GMT"));
                            break;
                        case ParsePathType.FBIS:
                            assertDocData(dd, "TEST-001", "TEST-001 Title", "TEST-001 text", tcs.ParseDate("1 January 1991"));
                            break;
                        case ParsePathType.FR94:
                            // no title extraction in this source for now
                            assertDocData(dd, "TEST-002", null, "DEPARTMENT OF SOMETHING", tcs.ParseDate("February 3, 1994"));
                            break;
                        case ParsePathType.FT:
                            assertDocData(dd, "TEST-003", "Test-003 title", "Some pub text", tcs.ParseDate("980424"));
                            break;
                        case ParsePathType.LATIMES:
                            assertDocData(dd, "TEST-004", "Test-004 Title", "Some paragraph", tcs.ParseDate("January 17, 1997, Sunday"));
                            break;
                        default:
                            assertTrue("Should never get here!", false);
                            break;
                    }
                }
            }
#pragma warning disable 168
            catch (NoMoreDataException e)
#pragma warning restore 168
            {
                gotExpectedException = true;
            }
            assertTrue("Should have gotten NoMoreDataException!", gotExpectedException);
            assertEquals("Wrong number of documents created by source!", 5, n);
            assertTrue("Did not see all types!", unseenTypes.Count == 0);
        }
    }
}
