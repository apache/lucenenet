using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Globalization;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.QueryParsers.Support.Flexible.Core.Messages // LUCENENET: There is no control over the namespace for code generation other than the folder, so we must use this namespace
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

    public class TestQueryParserResourceProvider : LuceneTestCase
    {
        [Test]
        public void TestOverrideResourceStrings()
        {
            var provider = new QueryParserResourceProvider(MessagesTest.ResourceManager);

            var actual = provider.GetString("INVALID_SYNTAX", null);

            Assert.IsTrue(actual.Contains("(TEST)"));

            actual = provider.GetString("INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION", null);

            Assert.IsTrue(actual.Contains("(TEST)"));

            actual = provider.GetString("INVALID_SYNTAX_ESCAPE_NONE_HEX_UNICODE", null);

            Assert.IsTrue(actual.Contains("(TEST)"));

            actual = provider.GetString("INVALID_SYNTAX_FUZZY_LIMITS", null);

            Assert.AreEqual("The similarity value for a fuzzy search must be between 0.0 and 1.0.", actual); // Fallback to default ResourceManager
        }

        [Test]
        public void TestOverrideResourceStrings_ja()
        {
            var provider = new QueryParserResourceProvider(MessagesTest.ResourceManager);

            var actual = provider.GetString("INVALID_SYNTAX", new CultureInfo("ja"));

            Assert.AreEqual("構文エラー: {0}", actual);

            actual = provider.GetString("INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION", new CultureInfo("ja"));

            Assert.AreEqual("切り捨てられたユニコード・エスケープ・シーケンス。", actual);

            actual = provider.GetString("INVALID_SYNTAX_ESCAPE_NONE_HEX_UNICODE", new CultureInfo("ja"));

            Assert.AreEqual("Non-hex character in Unicode escape sequence: {0} (TEST)", actual); // Fallback to non-localized test

            actual = provider.GetString("INVALID_SYNTAX_FUZZY_LIMITS", new CultureInfo("ja"));

            Assert.AreEqual("The similarity value for a fuzzy search must be between 0.0 and 1.0.", actual); // Fallback to default ResourceManager
        }

        [Test]
        public void TestOverrideResourceStrings_ja_JP()
        {
            var provider = new QueryParserResourceProvider(MessagesTest.ResourceManager);

            var actual = provider.GetString("INVALID_SYNTAX", new CultureInfo("ja-JP"));

            Assert.AreEqual("構文エラー: {0}", actual);

            actual = provider.GetString("INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION", new CultureInfo("ja-JP"));

            Assert.AreEqual("切り捨てられたユニコード・エスケープ・シーケンス。", actual);

            actual = provider.GetString("INVALID_SYNTAX_ESCAPE_NONE_HEX_UNICODE", new CultureInfo("ja-JP"));

            Assert.AreEqual("Non-hex character in Unicode escape sequence: {0} (TEST)", actual); // Fallback to non-localized test

            actual = provider.GetString("INVALID_SYNTAX_FUZZY_LIMITS", new CultureInfo("ja-JP"));

            Assert.AreEqual("The similarity value for a fuzzy search must be between 0.0 and 1.0.", actual); // Fallback to default ResourceManager
        }

        [Test]
        public void TestGetImageAsObject()
        {
            // Get the expected bytes
            using var expectedStream = GetType().Assembly.GetManifestResourceStream("Lucene.Net.QueryParsers.Support.Flexible.Core.Messages.lucene-net-icon-32x32.png");
            byte[] expectedBytes = new byte[expectedStream.Length];
            expectedStream.Read(expectedBytes, 0, (int)expectedStream.Length);

            // Check the wrapper to ensure we can read the bytes
            Assert.AreEqual(expectedBytes, MessagesTest.LUCENE_NET_ICON_32x32);


            var provider = new QueryParserResourceProvider(MessagesTest.ResourceManager);

            byte[] actualBytes = (byte[])provider.GetObject("LUCENE_NET_ICON_32x32", null);

            Assert.AreEqual(expectedBytes, actualBytes);
        }
    }
}
