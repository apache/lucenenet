using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Lucene.Net.QueryParsers.Xml.Builders
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

    public class TestNumericRangeFilterBuilder : LuceneTestCase
    {
        [Test]
        public void TestGetFilterHandleNumericParseErrorStrict()
        {
            NumericRangeFilterBuilder filterBuilder = new NumericRangeFilterBuilder();
            filterBuilder.SetStrictMode(true);

            String xml = "<NumericRangeFilter fieldName='AGE' type='int' lowerTerm='-1' upperTerm='NaN'/>";
            XmlDocument doc = GetDocumentFromString(xml);
            try
            {
                filterBuilder.GetFilter(doc.DocumentElement);
            }
#pragma warning disable 168
            catch (ParserException e)
#pragma warning restore 168
            {
                return;
            }
            fail("Expected to throw " + typeof(ParserException));
        }

        [Test]
        public void TestGetFilterHandleNumericParseError()
        {
            NumericRangeFilterBuilder filterBuilder = new NumericRangeFilterBuilder();
            filterBuilder.SetStrictMode(false);

            String xml = "<NumericRangeFilter fieldName='AGE' type='int' lowerTerm='-1' upperTerm='NaN'/>";
            XmlDocument doc = GetDocumentFromString(xml);
            Filter filter = filterBuilder.GetFilter(doc.DocumentElement);
            Store.Directory ramDir = NewDirectory();
            IndexWriter writer = new IndexWriter(ramDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, null));
            writer.Commit();
            try
            {
                AtomicReader reader = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(ramDir));
                try
                {
                    assertNull(filter.GetDocIdSet(reader.AtomicContext, reader.LiveDocs));
                }
                finally
                {
                    reader.Dispose();
                }
            }
            finally
            {
                writer.Commit();
                writer.Dispose();
                ramDir.Dispose();
            }
        }

        [Test]
        public void TestGetFilterInt()
        {
            NumericRangeFilterBuilder filterBuilder = new NumericRangeFilterBuilder();
            filterBuilder.SetStrictMode(true);

            String xml = "<NumericRangeFilter fieldName='AGE' type='int' lowerTerm='-1' upperTerm='10'/>";
            XmlDocument doc = GetDocumentFromString(xml);
            Filter filter = filterBuilder.GetFilter(doc.DocumentElement);
            assertTrue(filter is NumericRangeFilter<int>);

            NumericRangeFilter<int> numRangeFilter = (NumericRangeFilter<int>)filter;
            assertEquals(Convert.ToInt32(-1), numRangeFilter.Min);
            assertEquals(Convert.ToInt32(10), numRangeFilter.Max);
            assertEquals("AGE", numRangeFilter.Field);
            assertTrue(numRangeFilter.IncludesMin);
            assertTrue(numRangeFilter.IncludesMax);

            String xml2 = "<NumericRangeFilter fieldName='AGE' type='int' lowerTerm='-1' upperTerm='10' includeUpper='false'/>";
            XmlDocument doc2 = GetDocumentFromString(xml2);
            Filter filter2 = filterBuilder.GetFilter(doc2.DocumentElement);
            assertTrue(filter2 is NumericRangeFilter<int>);

            NumericRangeFilter<int> numRangeFilter2 = (NumericRangeFilter<int>)filter2;
            assertEquals(Convert.ToInt32(-1), numRangeFilter2.Min);
            assertEquals(Convert.ToInt32(10), numRangeFilter2.Max);
            assertEquals("AGE", numRangeFilter2.Field);
            assertTrue(numRangeFilter2.IncludesMin);
            assertFalse(numRangeFilter2.IncludesMax);
        }

        [Test]
        public void TestGetFilterLong()
        {
            NumericRangeFilterBuilder filterBuilder = new NumericRangeFilterBuilder();
            filterBuilder.SetStrictMode(true);

            String xml = "<NumericRangeFilter fieldName='AGE' type='LoNg' lowerTerm='-2321' upperTerm='60000000'/>";
            XmlDocument doc = GetDocumentFromString(xml);
            Filter filter = filterBuilder.GetFilter(doc.DocumentElement);
            assertTrue(filter is NumericRangeFilter<long>);

            NumericRangeFilter<long> numRangeFilter = (NumericRangeFilter<long>)filter;
            assertEquals(Convert.ToInt64(-2321L), numRangeFilter.Min);
            assertEquals(Convert.ToInt64(60000000L), numRangeFilter.Max);
            assertEquals("AGE", numRangeFilter.Field);
            assertTrue(numRangeFilter.IncludesMin);
            assertTrue(numRangeFilter.IncludesMax);

            String xml2 = "<NumericRangeFilter fieldName='AGE' type='LoNg' lowerTerm='-2321' upperTerm='60000000' includeUpper='false'/>";
            XmlDocument doc2 = GetDocumentFromString(xml2);
            Filter filter2 = filterBuilder.GetFilter(doc2.DocumentElement);
            assertTrue(filter2 is NumericRangeFilter<long>);
            NumericRangeFilter<long> numRangeFilter2 = (NumericRangeFilter<long>)filter2;
            assertEquals(Convert.ToInt64(-2321L), numRangeFilter2.Min);
            assertEquals(Convert.ToInt64(60000000L), numRangeFilter2.Max);
            assertEquals("AGE", numRangeFilter2.Field);
            assertTrue(numRangeFilter2.IncludesMin);
            assertFalse(numRangeFilter2.IncludesMax);
        }

        [Test]
        public void TestGetFilterDouble()
        {
            NumericRangeFilterBuilder filterBuilder = new NumericRangeFilterBuilder();
            filterBuilder.SetStrictMode(true);

            String xml = "<NumericRangeFilter fieldName='AGE' type='doubLe' lowerTerm='-23.21' upperTerm='60000.00023'/>";
            XmlDocument doc = GetDocumentFromString(xml);

            Filter filter = filterBuilder.GetFilter(doc.DocumentElement);
            assertTrue(filter is NumericRangeFilter<double>);

            NumericRangeFilter<double> numRangeFilter = (NumericRangeFilter<double>)filter;
            assertEquals(Convert.ToDouble(-23.21d), numRangeFilter.Min);
            assertEquals(Convert.ToDouble(60000.00023d), numRangeFilter.Max);
            assertEquals("AGE", numRangeFilter.Field);
            assertTrue(numRangeFilter.IncludesMin);
            assertTrue(numRangeFilter.IncludesMax);

            String xml2 = "<NumericRangeFilter fieldName='AGE' type='doubLe' lowerTerm='-23.21' upperTerm='60000.00023' includeUpper='false'/>";
            XmlDocument doc2 = GetDocumentFromString(xml2);
            Filter filter2 = filterBuilder.GetFilter(doc2.DocumentElement);
            assertTrue(filter2 is NumericRangeFilter<double>);

            NumericRangeFilter<double> numRangeFilter2 = (NumericRangeFilter<double>)filter2;
            assertEquals(Convert.ToDouble(-23.21d), numRangeFilter2.Min);
            assertEquals(Convert.ToDouble(60000.00023d), numRangeFilter2.Max);
            assertEquals("AGE", numRangeFilter2.Field);
            assertTrue(numRangeFilter2.IncludesMin);
            assertFalse(numRangeFilter2.IncludesMax);
        }

        [Test]
        public void TestGetFilterFloat()
        {
            NumericRangeFilterBuilder filterBuilder = new NumericRangeFilterBuilder();
            filterBuilder.SetStrictMode(true);

            String xml = "<NumericRangeFilter fieldName='AGE' type='FLOAT' lowerTerm='-2.321432' upperTerm='32432.23'/>";
            XmlDocument doc = GetDocumentFromString(xml);

            Filter filter = filterBuilder.GetFilter(doc.DocumentElement);
            assertTrue(filter is NumericRangeFilter<float>);

            NumericRangeFilter<float> numRangeFilter = (NumericRangeFilter<float>)filter;
            assertEquals(Convert.ToSingle(-2.321432f), numRangeFilter.Min);
            assertEquals(Convert.ToSingle(32432.23f), numRangeFilter.Max);
            assertEquals("AGE", numRangeFilter.Field);
            assertTrue(numRangeFilter.IncludesMin);
            assertTrue(numRangeFilter.IncludesMax);

            String xml2 = "<NumericRangeFilter fieldName='AGE' type='FLOAT' lowerTerm='-2.321432' upperTerm='32432.23' includeUpper='false' precisionStep='2' />";
            XmlDocument doc2 = GetDocumentFromString(xml2);

            Filter filter2 = filterBuilder.GetFilter(doc2.DocumentElement);
            assertTrue(filter2 is NumericRangeFilter<float>);

            NumericRangeFilter<float> numRangeFilter2 = (NumericRangeFilter<float>)filter2;
            assertEquals(Convert.ToSingle(-2.321432f), numRangeFilter2.Min);
            assertEquals(Convert.ToSingle(32432.23f), numRangeFilter2.Max);
            assertEquals("AGE", numRangeFilter2.Field);
            assertTrue(numRangeFilter2.IncludesMin);
            assertFalse(numRangeFilter2.IncludesMax);
        }

        private static XmlDocument GetDocumentFromString(String str)
        {
            XmlDocument result = new XmlDocument();
            result.LoadXml(str);
            return result;
        }
    }
}
