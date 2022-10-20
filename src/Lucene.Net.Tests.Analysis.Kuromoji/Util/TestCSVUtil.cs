// Lucene version compatibility level 8.2.0
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Ja.Util
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
    /// Tests for the CSVUtil class.
    /// </summary>
    public class TestCSVUtil : LuceneTestCase
    {
        [Test]
        public void TestQuoteEscapeQuotes()
        {
            String input = "\"Let It Be\" is a song and album by the The Beatles.";
            String expectedOutput = input.Replace("\"", "\"\"");
            ImplTestQuoteEscape(input, expectedOutput);
        }

        [Test]
        public void TestQuoteEscapeComma()
        {
            String input = "To be, or not to be ...";
            String expectedOutput = '"' + input + '"';
            ImplTestQuoteEscape(input, expectedOutput);
        }

        [Test]
        public void TestQuoteEscapeQuotesAndComma()
        {
            String input = "\"To be, or not to be ...\" is a well-known phrase from Shakespeare's Hamlet.";
            String expectedOutput = '"' + input.Replace("\"", "\"\"") + '"';
            ImplTestQuoteEscape(input, expectedOutput);
        }

        private void ImplTestQuoteEscape(String input, String expectedOutput)
        {
            String actualOutput = CSVUtil.QuoteEscape(input);
            assertEquals(expectedOutput, actualOutput);
        }
    }
}
