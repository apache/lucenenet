using Lucene.Net.QueryParsers.Flexible.Core.Messages;
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

    public class TestQueryParserMessagesDefault
    {
        [Test]
        public void TestOverrideResourceStrings()
        {
            QueryParserMessages.Culture = null;

            var actual = QueryParserMessages.INVALID_SYNTAX;

            Assert.AreEqual("Syntax Error: {0}", actual);

            actual = QueryParserMessages.INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION;

            Assert.AreEqual("Truncated unicode escape sequence.", actual);

            actual = QueryParserMessages.INVALID_SYNTAX_ESCAPE_NONE_HEX_UNICODE;

            Assert.AreEqual("Non-hex character in Unicode escape sequence: {0}", actual);

            actual = QueryParserMessages.INVALID_SYNTAX_FUZZY_LIMITS;

            Assert.AreEqual("The similarity value for a fuzzy search must be between 0.0 and 1.0.", actual);
        }

        [Test]
        public void TestOverrideResourceStrings_ja()
        {
            QueryParserMessages.Culture = new CultureInfo("ja");
            try
            {
                var actual = QueryParserMessages.INVALID_SYNTAX;

                Assert.AreEqual("Syntax Error: {0}", actual);

                actual = QueryParserMessages.INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION;

                Assert.AreEqual("Truncated unicode escape sequence.", actual);

                actual = QueryParserMessages.INVALID_SYNTAX_ESCAPE_NONE_HEX_UNICODE;

                Assert.AreEqual("Non-hex character in Unicode escape sequence: {0}", actual);

                actual = QueryParserMessages.INVALID_SYNTAX_FUZZY_LIMITS;

                Assert.AreEqual("The similarity value for a fuzzy search must be between 0.0 and 1.0.", actual);
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

                Assert.AreEqual("Syntax Error: {0}", actual);

                actual = QueryParserMessages.INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION;

                Assert.AreEqual("Truncated unicode escape sequence.", actual);

                actual = QueryParserMessages.INVALID_SYNTAX_ESCAPE_NONE_HEX_UNICODE;

                Assert.AreEqual("Non-hex character in Unicode escape sequence: {0}", actual);

                actual = QueryParserMessages.INVALID_SYNTAX_FUZZY_LIMITS;

                Assert.AreEqual("The similarity value for a fuzzy search must be between 0.0 and 1.0.", actual);
            }
            finally
            {
                QueryParserMessages.Culture = null;
            }
        }
    }
}
