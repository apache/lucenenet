using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Globalization;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.QueryParsers.Support.Flexible.Core.Messages
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

    public class TestQueryParserMessagesOverridden : LuceneTestCase
    {
        public override void BeforeClass()
        {
            base.BeforeClass();

            var provider = new QueryParserResourceProvider(MessagesTest.ResourceManager);
            QueryParserMessages.SetResourceProvider(provider);
        }

        public override void AfterClass()
        {
            // Return to the default
            var provider = new QueryParserResourceProvider();
            QueryParserMessages.SetResourceProvider(provider);

            base.AfterClass();
        }

        [Test]
        public void TestOverrideResourceStrings()
        {
            QueryParserMessages.Culture = null;

            var actual = QueryParserMessages.INVALID_SYNTAX;

            Assert.IsTrue(actual.Contains("(TEST)"));

            actual = QueryParserMessages.INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION;

            Assert.IsTrue(actual.Contains("(TEST)"));

            actual = QueryParserMessages.INVALID_SYNTAX_ESCAPE_NONE_HEX_UNICODE;

            Assert.IsTrue(actual.Contains("(TEST)"));

            actual = QueryParserMessages.INVALID_SYNTAX_FUZZY_LIMITS;

            Assert.IsFalse(actual.Contains("(TEST)")); // Fallback to default ResourceManager
        }

        [Test]
        public void TestOverrideResourceStrings_ja()
        {
            QueryParserMessages.Culture = new CultureInfo("ja");
            try
            {
                var actual = QueryParserMessages.INVALID_SYNTAX;

                Assert.AreEqual("構文エラー: {0}", actual);

                actual = QueryParserMessages.INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION;

                Assert.AreEqual("切り捨てられたユニコード・エスケープ・シーケンス。", actual);

                actual = QueryParserMessages.INVALID_SYNTAX_ESCAPE_NONE_HEX_UNICODE;

                Assert.AreEqual("Non-hex character in Unicode escape sequence: {0} (TEST)", actual); // Fallback to non-localized test

                actual = QueryParserMessages.INVALID_SYNTAX_FUZZY_LIMITS;

                Assert.AreEqual("The similarity value for a fuzzy search must be between 0.0 and 1.0.", actual); // Fallback to default ResourceManager
            }
            finally
            {
                QueryParserMessages.Culture = null;
            }
        }

        [Test]
        public void TestOverrideResourceStrings_ja_JP()
        {
            QueryParserMessages.Culture = new CultureInfo("ja-JP");
            try
            {
                var actual = QueryParserMessages.INVALID_SYNTAX;

                Assert.AreEqual("構文エラー: {0}", actual);

                actual = QueryParserMessages.INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION;

                Assert.AreEqual("切り捨てられたユニコード・エスケープ・シーケンス。", actual);

                actual = QueryParserMessages.INVALID_SYNTAX_ESCAPE_NONE_HEX_UNICODE;

                Assert.AreEqual("Non-hex character in Unicode escape sequence: {0} (TEST)", actual); // Fallback to non-localized test

                actual = QueryParserMessages.INVALID_SYNTAX_FUZZY_LIMITS;

                Assert.AreEqual("The similarity value for a fuzzy search must be between 0.0 and 1.0.", actual); // Fallback to default ResourceManager
            }
            finally
            {
                QueryParserMessages.Culture = null;
            }
        }
    }
}
