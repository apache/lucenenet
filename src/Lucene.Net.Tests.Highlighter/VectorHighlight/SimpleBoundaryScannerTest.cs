using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Text;

namespace Lucene.Net.Search.VectorHighlight
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

    public class SimpleBoundaryScannerTest : LuceneTestCase
    {
        static readonly String TEXT =
            "Apache Lucene(TM) is a high-performance, full-featured\ntext search engine library written entirely in Java.";

        [Test]
        public void TestFindStartOffset()
        {
            StringBuilder text = new StringBuilder(TEXT);
            IBoundaryScanner scanner = new SimpleBoundaryScanner();

            // test out of range
            int start = TEXT.Length + 1;
            assertEquals(start, scanner.FindStartOffset(text, start));
            start = 0;
            assertEquals(start, scanner.FindStartOffset(text, start));

            start = TEXT.IndexOf("formance", StringComparison.Ordinal);
            int expected = TEXT.IndexOf("high-performance", StringComparison.Ordinal);
            assertEquals(expected, scanner.FindStartOffset(text, start));

            start = TEXT.IndexOf("che", StringComparison.Ordinal);
            expected = TEXT.IndexOf("Apache", StringComparison.Ordinal);
            assertEquals(expected, scanner.FindStartOffset(text, start));
        }

        [Test]
        public void TestFindEndOffset()
        {
            StringBuilder text = new StringBuilder(TEXT);
            IBoundaryScanner scanner = new SimpleBoundaryScanner();

            // test out of range
            int start = TEXT.Length + 1;
            assertEquals(start, scanner.FindEndOffset(text, start));
            start = -1;
            assertEquals(start, scanner.FindEndOffset(text, start));

            start = TEXT.IndexOf("full-", StringComparison.Ordinal);
            int expected = TEXT.IndexOf("\ntext", StringComparison.Ordinal);
            assertEquals(expected, scanner.FindEndOffset(text, start));
        }
    }
}
