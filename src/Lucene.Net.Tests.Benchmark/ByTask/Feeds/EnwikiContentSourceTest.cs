using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
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

    public class EnwikiContentSourceTest : LuceneTestCase
    {
        /** An EnwikiContentSource which works on a String and not files. */
        private class StringableEnwikiSource : EnwikiContentSource
        {


            private readonly String docs;

            public StringableEnwikiSource(String docs)
            {
                this.docs = docs;
            }

            protected override Stream OpenInputStream()
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(docs));
            }

        }

        private void assertDocData(DocData dd, String expName, String expTitle, String expBody, String expDate)
        {
            assertNotNull(dd);
            assertEquals(expName, dd.Name);
            assertEquals(expTitle, dd.Title);
            assertEquals(expBody, dd.Body);
            assertEquals(expDate, dd.Date);
        }

        private void assertNoMoreDataException(EnwikiContentSource stdm)
        {
            try
            {
                stdm.GetNextDocData(null);
                fail("Expecting NoMoreDataException");
            }
#pragma warning disable 168
            catch (NoMoreDataException e)
#pragma warning restore 168
            {
                // expected
            }
        }

        private readonly String PAGE1 =
              "  <page>\r\n" +
              "    <title>Title1</title>\r\n" +
              "    <ns>0</ns>\r\n" +
              "    <id>1</id>\r\n" +
              "    <revision>\r\n" +
              "      <id>11</id>\r\n" +
              "      <parentid>111</parentid>\r\n" +
              "      <timestamp>2011-09-14T11:35:09Z</timestamp>\r\n" +
              "      <contributor>\r\n" +
              "      <username>Mister1111</username>\r\n" +
              "        <id>1111</id>\r\n" +
              "      </contributor>\r\n" +
              "      <minor />\r\n" +
              "      <comment>/* Never mind */</comment>\r\n" +
              "      <text>Some text 1 here</text>\r\n" +
              "    </revision>\r\n" +
              "  </page>\r\n";

        private readonly String PAGE2 =
            "  <page>\r\n" +
                "    <title>Title2</title>\r\n" +
                "    <ns>0</ns>\r\n" +
                "    <id>2</id>\r\n" +
                "    <revision>\r\n" +
                "      <id>22</id>\r\n" +
                "      <parentid>222</parentid>\r\n" +
                "      <timestamp>2022-09-14T22:35:09Z</timestamp>\r\n" +
                "      <contributor>\r\n" +
                "      <username>Mister2222</username>\r\n" +
                "        <id>2222</id>\r\n" +
                "      </contributor>\r\n" +
                "      <minor />\r\n" +
                "      <comment>/* Never mind */</comment>\r\n" +
                "      <text>Some text 2 here</text>\r\n" +
                "    </revision>\r\n" +
                "  </page>\r\n";

        [Test]
        public void TestOneDocument()
        {
            String docs =
                "<mediawiki>\r\n" +
                    PAGE1 +
                "</mediawiki>";

            EnwikiContentSource source = createContentSource(docs, false);

            DocData dd = source.GetNextDocData(new DocData());
            assertDocData(dd, "1", "Title1", "Some text 1 here", "14-SEP-2011 11:35:09.000");


            assertNoMoreDataException(source);
        }

        private EnwikiContentSource createContentSource(String docs, bool forever)
        {

            Dictionary<string, string> props = new Dictionary<string, string>();
            props["print.props"] = "false";
            props["content.source.forever"] = forever.ToString();
            Config config = new Config(props);

            EnwikiContentSource source = new StringableEnwikiSource(docs);
            source.SetConfig(config);

            // doc-maker just for initiating content source inputs
            DocMaker docMaker = new DocMaker();
            docMaker.SetConfig(config, source);
            docMaker.ResetInputs();
            return source;
        }

        [Test]
        public void TestTwoDocuments()
        {
            String docs =
                "<mediawiki>\r\n" +
                    PAGE1 +
                    PAGE2 +
                "</mediawiki>";

            EnwikiContentSource source = createContentSource(docs, false);

            DocData dd1 = source.GetNextDocData(new DocData());
            assertDocData(dd1, "1", "Title1", "Some text 1 here", "14-SEP-2011 11:35:09.000");

            DocData dd2 = source.GetNextDocData(new DocData());
            assertDocData(dd2, "2", "Title2", "Some text 2 here", "14-SEP-2022 22:35:09.000");


            assertNoMoreDataException(source);
        }

        [Test]
        public void TestForever()
        {
            String docs =
                "<mediawiki>\r\n" +
                    PAGE1 +
                    PAGE2 +
                "</mediawiki>";

            EnwikiContentSource source = createContentSource(docs, true);

            // same documents several times
            for (int i = 0; i < 3; i++)
            {
                DocData dd1 = source.GetNextDocData(new DocData());
                assertDocData(dd1, "1", "Title1", "Some text 1 here", "14-SEP-2011 11:35:09.000");

                DocData dd2 = source.GetNextDocData(new DocData());
                assertDocData(dd2, "2", "Title2", "Some text 2 here", "14-SEP-2022 22:35:09.000");
                // Don't test that NoMoreDataException is thrown, since the forever flag is turned on.
            }

            source.Dispose();
        }
    }
}
