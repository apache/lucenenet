using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
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

    public class TestNumericRangeQueryBuilder : LuceneTestCase
    {
        [Test]
        public void TestGetFilterHandleNumericParseErrorStrict()
        {
            NumericRangeQueryBuilder filterBuilder = new NumericRangeQueryBuilder();

            String xml = "<NumericRangeQuery fieldName='AGE' type='int' lowerTerm='-1' upperTerm='NaN'/>";
            XmlDocument doc = GetDocumentFromString(xml);
            try
            {
                filterBuilder.GetQuery(doc.DocumentElement);
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
        public void TestGetFilterInt()
        {
            NumericRangeQueryBuilder filterBuilder = new NumericRangeQueryBuilder();

            String xml = "<NumericRangeQuery fieldName='AGE' type='int' lowerTerm='-1' upperTerm='10'/>";
            XmlDocument doc = GetDocumentFromString(xml);
            Query filter = filterBuilder.GetQuery(doc.DocumentElement);
            assertTrue(filter is NumericRangeQuery<int>);

            NumericRangeQuery<int> numRangeFilter = (NumericRangeQuery<int>)filter;
            assertEquals(Convert.ToInt32(-1), numRangeFilter.Min);
            assertEquals(Convert.ToInt32(10), numRangeFilter.Max);
            assertEquals("AGE", numRangeFilter.Field);
            assertTrue(numRangeFilter.IncludesMin);
            assertTrue(numRangeFilter.IncludesMax);

            String xml2 = "<NumericRangeQuery fieldName='AGE' type='int' lowerTerm='-1' upperTerm='10' includeUpper='false'/>";
            XmlDocument doc2 = GetDocumentFromString(xml2);
            Query filter2 = filterBuilder.GetQuery(doc2.DocumentElement);
            assertTrue(filter2 is NumericRangeQuery<int>);

            NumericRangeQuery<int> numRangeFilter2 = (NumericRangeQuery<int>)filter2;
            assertEquals(Convert.ToInt32(-1), numRangeFilter2.Min);
            assertEquals(Convert.ToInt32(10), numRangeFilter2.Max);
            assertEquals("AGE", numRangeFilter2.Field);
            assertTrue(numRangeFilter2.IncludesMin);
            assertFalse(numRangeFilter2.IncludesMax);
        }

        [Test]
        public void TestGetFilterLong()
        {
            NumericRangeQueryBuilder filterBuilder = new NumericRangeQueryBuilder();

            String xml = "<NumericRangeQuery fieldName='AGE' type='LoNg' lowerTerm='-2321' upperTerm='60000000'/>";
            XmlDocument doc = GetDocumentFromString(xml);
            Query filter = filterBuilder.GetQuery(doc.DocumentElement);
            assertTrue(filter is NumericRangeQuery<long>);
            NumericRangeQuery<long> numRangeFilter = (NumericRangeQuery<long>)filter;
            assertEquals(Convert.ToInt64(-2321L), numRangeFilter.Min);
            assertEquals(Convert.ToInt64(60000000L), numRangeFilter.Max);
            assertEquals("AGE", numRangeFilter.Field);
            assertTrue(numRangeFilter.IncludesMin);
            assertTrue(numRangeFilter.IncludesMax);

            String xml2 = "<NumericRangeQuery fieldName='AGE' type='LoNg' lowerTerm='-2321' upperTerm='60000000' includeUpper='false'/>";
            XmlDocument doc2 = GetDocumentFromString(xml2);
            Query filter2 = filterBuilder.GetQuery(doc2.DocumentElement);
            assertTrue(filter2 is NumericRangeQuery<long>);

            NumericRangeQuery<long> numRangeFilter2 = (NumericRangeQuery<long>)filter2;
            assertEquals(Convert.ToInt64(-2321L), numRangeFilter2.Min);
            assertEquals(Convert.ToInt64(60000000L), numRangeFilter2.Max);
            assertEquals("AGE", numRangeFilter2.Field);
            assertTrue(numRangeFilter2.IncludesMin);
            assertFalse(numRangeFilter2.IncludesMax);
        }

        [Test]
        public void TestGetFilterDouble()
        {
            NumericRangeQueryBuilder filterBuilder = new NumericRangeQueryBuilder();

            String xml = "<NumericRangeQuery fieldName='AGE' type='doubLe' lowerTerm='-23.21' upperTerm='60000.00023'/>";
            XmlDocument doc = GetDocumentFromString(xml);

            Query filter = filterBuilder.GetQuery(doc.DocumentElement);
            assertTrue(filter is NumericRangeQuery<double>);

            NumericRangeQuery<double> numRangeFilter = (NumericRangeQuery<double>)filter;
            assertEquals(Convert.ToDouble(-23.21d), numRangeFilter.Min);
            assertEquals(Convert.ToDouble(60000.00023d), numRangeFilter.Max);
            assertEquals("AGE", numRangeFilter.Field);
            assertTrue(numRangeFilter.IncludesMin);
            assertTrue(numRangeFilter.IncludesMax);

            String xml2 = "<NumericRangeQuery fieldName='AGE' type='doubLe' lowerTerm='-23.21' upperTerm='60000.00023' includeUpper='false'/>";
            XmlDocument doc2 = GetDocumentFromString(xml2);
            Query filter2 = filterBuilder.GetQuery(doc2.DocumentElement);
            assertTrue(filter2 is NumericRangeQuery<double>);

            NumericRangeQuery<double> numRangeFilter2 = (NumericRangeQuery<double>)filter2;
            assertEquals(Convert.ToDouble(-23.21d), numRangeFilter2.Min);
            assertEquals(Convert.ToDouble(60000.00023d), numRangeFilter2.Max);
            assertEquals("AGE", numRangeFilter2.Field);
            assertTrue(numRangeFilter2.IncludesMin);
            assertFalse(numRangeFilter2.IncludesMax);
        }

        [Test]
        public void TestGetFilterFloat()
        {
            NumericRangeQueryBuilder filterBuilder = new NumericRangeQueryBuilder();

            String xml = "<NumericRangeQuery fieldName='AGE' type='FLOAT' lowerTerm='-2.321432' upperTerm='32432.23'/>";
            XmlDocument doc = GetDocumentFromString(xml);

            Query filter = filterBuilder.GetQuery(doc.DocumentElement);
            assertTrue(filter is NumericRangeQuery<float>);

            NumericRangeQuery<float> numRangeFilter = (NumericRangeQuery<float>)filter;
            assertEquals(Convert.ToSingle(-2.321432f), numRangeFilter.Min);
            assertEquals(Convert.ToSingle(32432.23f), numRangeFilter.Max);
            assertEquals("AGE", numRangeFilter.Field);
            assertTrue(numRangeFilter.IncludesMin);
            assertTrue(numRangeFilter.IncludesMax);

            String xml2 = "<NumericRangeQuery fieldName='AGE' type='FLOAT' lowerTerm='-2.321432' upperTerm='32432.23' includeUpper='false' precisionStep='2' />";
            XmlDocument doc2 = GetDocumentFromString(xml2);

            Query filter2 = filterBuilder.GetQuery(doc2.DocumentElement);
            assertTrue(filter2 is NumericRangeQuery<float>);

            NumericRangeQuery<float> numRangeFilter2 = (NumericRangeQuery<float>)filter2;
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
