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

        /// <summary>
        /// A token that has not been terminated by whitespace is processed when its
        /// containing element ends.
        /// </summary>
        [Test]
        public virtual void TestEndElementProcessesRemainingToken()
        {
            var parser = new TrackingPatternParser();

            parser.StartElement(
                string.Empty,
                "classes",
                string.Empty,
                new Dictionary<string, string>());

            parser.Characters("test-token".AsSpan());

            // The token has not been terminated by whitespace, so it should not
            // have been processed yet.
            Assert.AreEqual(0, parser.ProcessedTokens.Count);

            parser.EndElement(string.Empty, "classes", string.Empty);

            Assert.AreEqual(1, parser.ProcessedTokens.Count);
            Assert.AreEqual("test-token", parser.ProcessedTokens[0]);
        }


        /// <summary>
        /// A token terminated by whitespace is processed immediately.
        /// </summary>
        [Test]
        public virtual void TestCharactersProcessesWhitespaceTerminatedToken()
        {
            var parser = new TrackingPatternParser();

            parser.StartElement(string.Empty, "classes", string.Empty,
                new Dictionary<string, string>());

            parser.Characters("first ".AsSpan());

            Assert.AreEqual(new[] { "first" }, parser.ProcessedTokens);

            parser.EndElement(string.Empty, "classes", string.Empty);

            Assert.AreEqual(new[] { "first" }, parser.ProcessedTokens);
        }

        /// <summary>
        /// A token split across multiple character callbacks is processed as one token.
        /// </summary>
        [Test]
        public virtual void TestCharactersAccumulatesSplitToken()
        {
            var parser = new TrackingPatternParser();

            parser.StartElement(string.Empty, "classes", string.Empty,
                new Dictionary<string, string>());

            parser.Characters("first".AsSpan());
            Assert.AreEqual(0, parser.ProcessedTokens.Count);

            parser.Characters("-part ".AsSpan());

            Assert.AreEqual(new[] { "first-part" }, parser.ProcessedTokens);

            parser.EndElement(string.Empty, "classes", string.Empty);
            Assert.AreEqual(new[] { "first-part" }, parser.ProcessedTokens);
        }

        /// <summary>
        /// Consecutive whitespace characters do not produce empty tokens.
        /// </summary>
        [Test]
        public virtual void TestCharactersSkipsConsecutiveWhitespace()
        {
            var parser = new TrackingPatternParser();

            parser.StartElement(string.Empty, "classes", string.Empty,
                new Dictionary<string, string>());

            parser.Characters("  first   second  ".AsSpan());

            Assert.AreEqual(new[] { "first", "second" }, parser.ProcessedTokens);

            parser.EndElement(string.Empty, "classes", string.Empty);
            Assert.AreEqual(new[] { "first", "second" }, parser.ProcessedTokens);
        }

        /// <summary>
        /// An element containing no character data does not process a token.
        /// </summary>
        [Test]
        public virtual void TestEndElementDoesNotProcessEmptyToken()
        {
            var parser = new TrackingPatternParser();

            parser.StartElement(string.Empty, "classes", string.Empty,
                new Dictionary<string, string>());

            parser.EndElement(string.Empty, "classes", string.Empty);

            Assert.AreEqual(0, parser.ProcessedTokens.Count);
        }

        /// <summary>
        /// A token processed by Characters is not processed again by EndElement.
        /// </summary>
        [Test]
        public virtual void TestEndElementDoesNotProcessTokenTwice()
        {
            var parser = new TrackingPatternParser();

            parser.StartElement(string.Empty, "classes", string.Empty,
                new Dictionary<string, string>());

            parser.Characters("first ".AsSpan());

            Assert.AreEqual(new[] { "first" }, parser.ProcessedTokens);

            parser.EndElement(string.Empty, "classes", string.Empty);

            Assert.AreEqual(new[] { "first" }, parser.ProcessedTokens);
        }

        /// <summary>
        /// Text surrounding a hyphen element is preserved and delivered as one exception.
        /// </summary>
        [Test]
        public virtual void TestHyphenElementPreservesSurroundingExceptionText()
        {
            var consumer = new RecordingPatternConsumer();
            var parser = new PatternParser(consumer);

            parser.StartElement(string.Empty, "exceptions", string.Empty,
                new Dictionary<string, string>());

            parser.Characters("for".AsSpan());

            var attributes = new Dictionary<string, string>
            {
                ["pre"] = "for",
                ["no"] = string.Empty,
                ["post"] = "get"
            };

            parser.StartElement(string.Empty, "hyphen", string.Empty, attributes);
            parser.EndElement(string.Empty, "hyphen", string.Empty);

            parser.Characters("get".AsSpan());
            parser.EndElement(string.Empty, "exceptions", string.Empty);

            Assert.AreEqual(1, consumer.Exceptions.Count);
            Assert.AreEqual("forget", consumer.Exceptions[0].Word);
            Assert.AreEqual(3, consumer.Exceptions[0].HyphenatedWord.Count);
            Assert.AreEqual("for", consumer.Exceptions[0].HyphenatedWord[0]);
            Assert.IsInstanceOf<Hyphen>(consumer.Exceptions[0].HyphenatedWord[1]);
            Assert.AreEqual("get", consumer.Exceptions[0].HyphenatedWord[2]);
        }


        /// <summary>
        /// An element without a namespace has an empty namespace URI, and its
        /// local and qualified names are identical.
        /// </summary>
        [Test]
        public virtual void TestElementNamesWithoutNamespace()
        {
            const string xml =
                "<hyphenation-info>" +
                "  <classes>abc</classes>" +
                "</hyphenation-info>";

            var parser = new RecordingElementParser();

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            parser.Parse(stream);

            CollectionAssert.AreEqual(
                new[]
                {
                    (string.Empty, "hyphenation-info", "hyphenation-info"),
                    (string.Empty, "classes", "classes")
                },
                parser.StartElements);

            CollectionAssert.AreEqual(
                new[]
                {
                    (string.Empty, "classes", "classes"),
                    (string.Empty, "hyphenation-info", "hyphenation-info")
                },
                parser.EndElements);
        }

        /// <summary>
        /// Elements with namespace prefixes provide the namespace URI, local name,
        /// and qualified name separately. Empty elements produce both start and
        /// end notifications with the same values.
        /// </summary>
        [Test]
        public virtual void TestElementNamesWithNamespaces()
        {
            const string xml =
                "<root xmlns=\"urn:default\" xmlns:h=\"urn:hyphen\">" +
                "  <classes>" +
                "    <h:patterns>abc</h:patterns>" +
                "    <h:exceptions />" +
                "  </classes>" +
                "</root>";

            var parser = new RecordingElementParser();

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            parser.Parse(stream);

            CollectionAssert.AreEqual(
                new[]
                {
                    ("urn:default", "root", "root"),
                    ("urn:default", "classes", "classes"),
                    ("urn:hyphen", "patterns", "h:patterns"),
                    ("urn:hyphen", "exceptions", "h:exceptions")
                },
                parser.StartElements);

            CollectionAssert.AreEqual(
                new[]
                {
                    ("urn:hyphen", "patterns", "h:patterns"),
                    ("urn:hyphen", "exceptions", "h:exceptions"),
                    ("urn:default", "classes", "classes"),
                    ("urn:default", "root", "root")
                },
                parser.EndElements);
        }

        private sealed class NoOpPatternConsumer : IPatternConsumer
        {
            public void AddClass(ReadOnlySpan<char> chargroup) { }

            public void AddException(ReadOnlySpan<char> word, IList<object> hyphenatedword) { }

            public void AddPattern(ReadOnlySpan<char> pattern, ReadOnlySpan<char> values) { }
        }

        private sealed class TrackingPatternParser : PatternParser
        {
            public IList<string> ProcessedTokens { get; } = new List<string>();

            protected override void ProcessToken(ReadOnlySpan<char> token)
            {
                ProcessedTokens.Add(token.ToString());
            }
        }

        private sealed class RecordingPatternConsumer : IPatternConsumer
        {
            public IList<(string Word, IList<object> HyphenatedWord)> Exceptions { get; }
                = new List<(string Word, IList<object> HyphenatedWord)>();

            public void AddClass(ReadOnlySpan<char> chargroup) { }

            public void AddPattern(ReadOnlySpan<char> pattern, ReadOnlySpan<char> values) { }

            public void AddException(ReadOnlySpan<char> word, IList<object> hyphenatedword)
            {
                Exceptions.Add((word.ToString(), hyphenatedword));
            }
        }

        private sealed class RecordingElementParser : PatternParser
        {
            public IList<(string Uri, string Local, string Raw)> StartElements { get; }
                = new List<(string Uri, string Local, string Raw)>();

            public IList<(string Uri, string Local, string Raw)> EndElements { get; }
                = new List<(string Uri, string Local, string Raw)>();

            public RecordingElementParser()
                : base(new NoOpPatternConsumer())
            {
            }

            public override void StartElement(
                string uri,
                string local,
                string raw,
                IDictionary<string, string> attrs)
            {
                StartElements.Add((uri, local, raw));
                base.StartElement(uri, local, raw, attrs);
            }

            public override void EndElement(string uri, string local, string raw)
            {
                EndElements.Add((uri, local, raw));
                base.EndElement(uri, local, raw);
            }
        }
    }
}
