using J2N.Globalization;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using static Lucene.Net.Benchmarks.ByTask.Feeds.DemoHTMLParser;

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

    public class TestHtmlParser : LuceneTestCase
    {
        [Test]
        public void TestUnicode()
        {
            String text = "<html><body>汉语</body></html>";
            Parser parser = new Parser(new StringReader(text));
            assertEquals("汉语", parser.Body);
        }

        [Test]
        public void TestEntities()
        {
            String text = "<html><body>&#x6C49;&#x8BED;&yen;</body></html>";
            Parser parser = new Parser(new StringReader(text));
            assertEquals("汉语¥", parser.Body);
        }

        [Test]
        public void TestComments()
        {
            String text = "<html><body>foo<!-- bar --><! baz --></body></html>";
            Parser parser = new Parser(new StringReader(text));
            assertEquals("foo", parser.Body);
        }

        [Test]
        public void TestScript()
        {
            String text = "<html><body><script type=\"text/javascript\">" +
                          "document.write(\"test\")</script>foo</body></html>";
            Parser parser = new Parser(new StringReader(text));
            assertEquals("foo", parser.Body);
        }

        [Test]
        public void TestStyle()
        {
            String text = "<html><head><style type=\"text/css\">" +
                          "body{background-color:blue;}</style>" +
                          "</head><body>foo</body></html>";
            Parser parser = new Parser(new StringReader(text));
            assertEquals("foo", parser.Body);
        }

        [Test]
        public void TestDoctype()
        {
            String text = "<!DOCTYPE HTML PUBLIC " +
            "\"-//W3C//DTD HTML 4.01 Transitional//EN\"" +
            "\"http://www.w3.org/TR/html4/loose.dtd\">" +
            "<html><body>foo</body></html>";
            Parser parser = new Parser(new StringReader(text));
            assertEquals("foo", parser.Body);
        }

        [Test]
        public void TestMeta()
        {
            String text = "<html><head>" +
            "<meta name=\"a\" content=\"1\" />" +
            "<meta name=\"b\" content=\"2\" />" +
            "<meta name=\"keywords\" content=\"this is a test\" />" +
            "<meta http-equiv=\"Content-Type\" content=\"text/html;charset=UTF-8\" />" +
            "</head><body>foobar</body></html>";
            Parser parser = new Parser(new StringReader(text));
            IDictionary<string, string> tags = parser.MetaTags;
            assertEquals(4, tags.size());
            assertEquals("1", tags["a"]);
            assertEquals("2", tags["b"]);
            assertEquals("this is a test", tags["keywords"]);
            assertEquals("text/html;charset=UTF-8", tags["content-type"]);
        }

        [Test]
        public void TestTitle()
        {
            String text = "<html><head><TITLE>foo</TITLE><head><body>bar</body></html>";
            Parser parser = new Parser(new StringReader(text));
            assertEquals("foo", parser.Title);
        }

        // LUCENE-2246
        [Test]
        public void TestTurkish()
        {
            using var context = new CultureContext("tr-TR");
            String text = "<html><HEAD><TITLE>ııı</TITLE></head><body>" +
                "<IMG SRC=\"../images/head.jpg\" WIDTH=570 HEIGHT=47 BORDER=0 ALT=\"ş\">" +
                "<a title=\"(ııı)\"></body></html>";
            Parser parser = new Parser(new StringReader(text));
            assertEquals("ııı", parser.Title);
            assertEquals("[ş]", parser.Body);
        }

        [Test]
        public void TestSampleTRECDoc()
        {
            String text = "<html>\r\n" +
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
                "\r\n";
            Parser parser = new Parser(new StringReader(text));
            assertEquals("TEST-000 title", parser.Title);
            assertEquals("TEST-000 text", parser.Body.Trim());
        }

        [Test]
        public void TestNoHTML()
        {
            String text = "hallo";
            Parser parser = new Parser(new StringReader(text));
            assertEquals("", parser.Title);
            assertEquals("hallo", parser.Body);
        }

        [Test]
        public void Testivalid()
        {
            String text = "<title>foo</title>bar";
            Parser parser = new Parser(new StringReader(text));
            assertEquals("foo", parser.Title);
            assertEquals("bar", parser.Body);
        }
    }
}
