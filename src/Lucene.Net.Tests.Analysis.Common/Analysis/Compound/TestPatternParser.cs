// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Compound.Hyphenation;
using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Lucene.Net.Analysis.Compound
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

    [LuceneNetSpecific]
    public class TestPatternParser : LuceneTestCase
    {
        /// <summary>
        /// A well-formed hyphenation file that references the standard, embedded
        /// <c>hyphenation.dtd</c> parses without error.
        /// </summary>
        [Test]
        public virtual void TestValidHyphenationDataParses()
        {
            using var stream = this.GetType().getResourceAsStream("da_UTF8.xml");
            var parser = new PatternParser(new NoOpPatternConsumer());

            Assert.DoesNotThrow(() => parser.Parse(stream));
        }

        /// <summary>
        /// A hyphenation file that references an external entity other than the
        /// embedded <c>hyphenation.dtd</c> is rejected rather than resolving the
        /// reference.
        /// </summary>
        [Test]
        public virtual void TestExternalEntityIsRejected()
        {
            // Point the external reference at a real, readable file. If the reference were
            // resolved, its contents would be pulled into the parsed document; instead the
            // parser must refuse the reference.
            FileInfo target = CreateTempFile("lucene_pp_", ".txt");
            File.WriteAllText(target.FullName, "marker-contents");

            string targetUri = new Uri(target.FullName).AbsoluteUri;
            string xml =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<!DOCTYPE hyphenation-info [\n" +
                "  <!ENTITY ext SYSTEM \"" + targetUri + "\">\n" +
                "]>\n" +
                "<hyphenation-info>\n" +
                "  <exceptions>&ext;</exceptions>\n" +
                "</hyphenation-info>\n";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var parser = new PatternParser(new NoOpPatternConsumer());

            Assert.Throws<XmlException>(() => parser.Parse(stream));
        }

        /// <summary>
        /// A reference to an external DTD other than the embedded
        /// <c>hyphenation.dtd</c> is rejected.
        /// </summary>
        [Test]
        public virtual void TestExternalDtdIsRejected()
        {
            FileInfo target = CreateTempFile("lucene_pp_", ".dtd");
            File.WriteAllText(target.FullName, "<!ELEMENT hyphenation-info ANY>");

            string targetUri = new Uri(target.FullName).AbsoluteUri;
            string xml =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<!DOCTYPE hyphenation-info SYSTEM \"" + targetUri + "\">\n" +
                "<hyphenation-info></hyphenation-info>\n";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var parser = new PatternParser(new NoOpPatternConsumer());

            Assert.Throws<XmlException>(() => parser.Parse(stream));
        }

        private sealed class NoOpPatternConsumer : IPatternConsumer
        {
            public void AddClass(string chargroup) { }

            public void AddException(string word, IList<object> hyphenatedword) { }

            public void AddPattern(string pattern, string values) { }
        }
    }
}
