using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lucene.Net.QueryParsers.Flexible.Standard
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

    public class TestNumericQueryParser : LuceneTestCase
    {
        public enum NumberType
        {
            NEGATIVE, ZERO, POSITIVE
        }

        private readonly static DateFormat[] DATE_STYLES = {DateFormat.FULL, DateFormat.LONG,
            DateFormat.MEDIUM, DateFormat.SHORT};

        private readonly static int PRECISION_STEP = 8;
        private readonly static String FIELD_NAME = "field";
        private static CultureInfo LOCALE;
        private static TimeZoneInfo TIMEZONE;
        private static IDictionary<String, /*Number*/ object> RANDOM_NUMBER_MAP;
        private readonly static IEscapeQuerySyntax ESCAPER = new EscapeQuerySyntaxImpl();
        private readonly static String DATE_FIELD_NAME = "date";
        private static DateFormat DATE_STYLE;
        private static DateFormat TIME_STYLE;

        private static Analyzer ANALYZER;

        private static NumberFormat NUMBER_FORMAT;
        

        private static StandardQueryParser qp;

        private static NumberDateFormat DATE_FORMAT;

        private static Directory directory = null;
        private static IndexReader reader = null;
        private static IndexSearcher searcher = null;

        private static bool checkDateFormatSanity(/*DateFormat*/string dateFormat, long date)
        {
            DateTime result;
            return DateTime.TryParseExact(new DateTime(NumberDateFormat.EPOCH).AddMilliseconds(date).ToString(dateFormat),
                dateFormat, CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind, out result);
        }

        [OneTimeSetUp]
        public void BeforeClass()
        {
            ANALYZER = new MockAnalyzer(Random());

            qp = new StandardQueryParser(ANALYZER);

            HashMap<String, /*Number*/object> randomNumberMap = new HashMap<string, object>();

            /*SimpleDateFormat*/
            string dateFormat;
            long randomDate;
            bool dateFormatSanityCheckPass;
            int count = 0;
            do
            {
                if (count > 100)
                {
                    fail("This test has problems to find a sane random DateFormat/NumberFormat. Stopped trying after 100 iterations.");
                }

                dateFormatSanityCheckPass = true;
                LOCALE = randomLocale(Random());
                TIMEZONE = randomTimeZone(Random());
                DATE_STYLE = randomDateStyle(Random());
                TIME_STYLE = randomDateStyle(Random());

                //// assumes localized date pattern will have at least year, month, day,
                //// hour, minute
                //dateFormat = (SimpleDateFormat)DateFormat.getDateTimeInstance(
                //    DATE_STYLE, TIME_STYLE, LOCALE);

                //// not all date patterns includes era, full year, timezone and second,
                //// so we add them here
                //dateFormat.applyPattern(dateFormat.toPattern() + " G s Z yyyy");
                //dateFormat.setTimeZone(TIMEZONE);

                DATE_FORMAT = new NumberDateFormat(DATE_STYLE, TIME_STYLE, LOCALE)
                {
                    TimeZone = TIMEZONE
                };
                dateFormat = DATE_FORMAT.GetDateFormat();

                do
                {
                    randomDate = Random().nextLong();

                    // prune date value so it doesn't pass in insane values to some
                    // calendars.
                    randomDate = randomDate % 3400000000000L;

                    // truncate to second
                    randomDate = (randomDate / 1000L) * 1000L;

                    // only positive values
                    randomDate = Math.Abs(randomDate);
                } while (randomDate == 0L);

                dateFormatSanityCheckPass &= checkDateFormatSanity(dateFormat, randomDate);

                dateFormatSanityCheckPass &= checkDateFormatSanity(dateFormat, 0);

                dateFormatSanityCheckPass &= checkDateFormatSanity(dateFormat,
                          -randomDate);

                count++;
            } while (!dateFormatSanityCheckPass);

            //NUMBER_FORMAT = NumberFormat.getNumberInstance(LOCALE);
            //NUMBER_FORMAT.setMaximumFractionDigits((Random().nextInt() & 20) + 1);
            //NUMBER_FORMAT.setMinimumFractionDigits((Random().nextInt() & 20) + 1);
            //NUMBER_FORMAT.setMaximumIntegerDigits((Random().nextInt() & 20) + 1);
            //NUMBER_FORMAT.setMinimumIntegerDigits((Random().nextInt() & 20) + 1);

            NUMBER_FORMAT = new NumberFormat(LOCALE);

            double randomDouble;
            long randomLong;
            int randomInt;
            float randomFloat;

            while ((randomLong = Convert.ToInt64(NormalizeNumber(Math.Abs(Random().nextLong()))
                )) == 0L)
                ;
            while ((randomDouble = Convert.ToDouble(NormalizeNumber(Math.Abs(Random().NextDouble()))
                )) == 0.0)
                ;
            while ((randomFloat = Convert.ToSingle(NormalizeNumber(Math.Abs(Random().nextFloat()))
                )) == 0.0f)
                ;
            while ((randomInt = Convert.ToInt32(NormalizeNumber(Math.Abs(Random().nextInt())))) == 0)
                ;

            randomNumberMap.Put(NumericType.LONG.ToString(), randomLong);
            randomNumberMap.Put(NumericType.INT.ToString(), randomInt);
            randomNumberMap.Put(NumericType.FLOAT.ToString(), randomFloat);
            randomNumberMap.Put(NumericType.DOUBLE.ToString(), randomDouble);
            randomNumberMap.Put(DATE_FIELD_NAME, randomDate);

            RANDOM_NUMBER_MAP = Collections.UnmodifiableMap(randomNumberMap);

            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random(), directory,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
                    .SetMaxBufferedDocs(TestUtil.NextInt(Random(), 50, 1000))
                    .SetMergePolicy(NewLogMergePolicy()));

            Document doc = new Document();
            HashMap<String, NumericConfig> numericConfigMap = new HashMap<String, NumericConfig>();
            HashMap<String, Field> numericFieldMap = new HashMap<String, Field>();
            qp.NumericConfigMap = (numericConfigMap);

            foreach (NumericType type in Enum.GetValues(typeof(NumericType)))
            {
                numericConfigMap.Put(type.ToString(), new NumericConfig(PRECISION_STEP,
                    NUMBER_FORMAT, type)); 

                FieldType ft2 = new FieldType(IntField.TYPE_NOT_STORED);
                ft2.NumericType = (type);
                ft2.IsStored = (true);
                ft2.NumericPrecisionStep = (PRECISION_STEP);
                ft2.Freeze();
                Field field;

                switch (type)
                {
                    case NumericType.INT:
                        field = new IntField(type.ToString(), 0, ft2);
                        break;
                    case NumericType.FLOAT:
                        field = new FloatField(type.ToString(), 0.0f, ft2);
                        break;
                    case NumericType.LONG:
                        field = new LongField(type.ToString(), 0L, ft2);
                        break;
                    case NumericType.DOUBLE:
                        field = new DoubleField(type.ToString(), 0.0, ft2);
                        break;
                    default:
                        fail();
                        field = null;
                        break;
                }
                numericFieldMap.Put(type.ToString(), field);
                doc.Add(field);
            }

            numericConfigMap.Put(DATE_FIELD_NAME, new NumericConfig(PRECISION_STEP,
                DATE_FORMAT, NumericType.LONG));
            FieldType ft = new FieldType(LongField.TYPE_NOT_STORED);
            ft.IsStored = (true);
            ft.NumericPrecisionStep = (PRECISION_STEP);
            LongField dateField = new LongField(DATE_FIELD_NAME, 0L, ft);
            numericFieldMap.Put(DATE_FIELD_NAME, dateField);
            doc.Add(dateField);

            foreach (NumberType numberType in Enum.GetValues(typeof(NumberType)))
            {
                setFieldValues(numberType, numericFieldMap);
                if (VERBOSE) Console.WriteLine("Indexing document: " + doc);
                writer.AddDocument(doc);
            }

            reader = writer.Reader;
            searcher = NewSearcher(reader);
            writer.Dispose();

        }

        private static /*Number*/ object GetNumberType(NumberType? numberType, String fieldName)
        {

            if (numberType == null)
            {
                return null;
            }

            switch (numberType)
            {

                case NumberType.POSITIVE:
                    return RANDOM_NUMBER_MAP[fieldName];

                case NumberType.NEGATIVE:
                    /*Number*/
                    object number = RANDOM_NUMBER_MAP[fieldName];

                    if (NumericType.LONG.ToString().equals(fieldName)
                        || DATE_FIELD_NAME.equals(fieldName))
                    {
                        number = -Convert.ToInt64(number);

                    }
                    else if (NumericType.DOUBLE.ToString().equals(fieldName))
                    {
                        number = -Convert.ToDouble(number);

                    }
                    else if (NumericType.FLOAT.ToString().equals(fieldName))
                    {
                        number = -Convert.ToSingle(number);

                    }
                    else if (NumericType.INT.ToString().equals(fieldName))
                    {
                        number = -Convert.ToInt32(number);

                    }
                    else
                    {
                        throw new ArgumentException("field name not found: "
                            + fieldName);
                    }

                    return number;

                default:
                    return 0;

            }

        }

        private static void setFieldValues(NumberType numberType,
            HashMap<String, Field> numericFieldMap)
        {

            /*Number*/
            object number = GetNumberType(numberType, NumericType.DOUBLE
                .ToString());
            numericFieldMap[NumericType.DOUBLE.ToString()].SetDoubleValue(Convert.ToDouble(
                number));

            number = GetNumberType(numberType, NumericType.INT.ToString());
            numericFieldMap[NumericType.INT.ToString()].SetInt32Value(Convert.ToInt32(
                number));

            number = GetNumberType(numberType, NumericType.LONG.ToString());
            numericFieldMap[NumericType.LONG.ToString()].SetInt64Value(Convert.ToInt64(
                number));

            number = GetNumberType(numberType, NumericType.FLOAT.ToString());
            numericFieldMap[NumericType.FLOAT.ToString()].SetSingleValue(Convert.ToSingle(
                number));

            number = GetNumberType(numberType, DATE_FIELD_NAME);
            numericFieldMap[DATE_FIELD_NAME].SetInt64Value(Convert.ToInt64(number));
        }

        private static DateFormat randomDateStyle(Random random)
        {
            return DATE_STYLES[random.nextInt(DATE_STYLES.Length)];
        }

        [Test]
        public void TestInclusiveNumericRange()
        {
            assertRangeQuery(NumberType.ZERO, NumberType.ZERO, true, true, 1);
            assertRangeQuery(NumberType.ZERO, NumberType.POSITIVE, true, true, 2);
            assertRangeQuery(NumberType.NEGATIVE, NumberType.ZERO, true, true, 2);
            assertRangeQuery(NumberType.NEGATIVE, NumberType.POSITIVE, true, true, 3);
            assertRangeQuery(NumberType.NEGATIVE, NumberType.NEGATIVE, true, true, 1);
        }

        [Test]
        // test disabled since standard syntax parser does not work with inclusive and
        // exclusive at the same time
        public void TestInclusiveLowerNumericRange()
        {
            assertRangeQuery(NumberType.NEGATIVE, NumberType.ZERO, false, true, 1);
            assertRangeQuery(NumberType.ZERO, NumberType.POSITIVE, false, true, 1);
            assertRangeQuery(NumberType.NEGATIVE, NumberType.POSITIVE, false, true, 2);
            assertRangeQuery(NumberType.NEGATIVE, NumberType.NEGATIVE, false, true, 0);
        }

        [Test]
        // test disabled since standard syntax parser does not work with inclusive and
        // exclusive at the same time
        public void TestInclusiveUpperNumericRange()
        {
            assertRangeQuery(NumberType.NEGATIVE, NumberType.ZERO, true, false, 1);
            assertRangeQuery(NumberType.ZERO, NumberType.POSITIVE, true, false, 1);
            assertRangeQuery(NumberType.NEGATIVE, NumberType.POSITIVE, true, false, 2);
            assertRangeQuery(NumberType.NEGATIVE, NumberType.NEGATIVE, true, false, 0);
        }

        [Test]
        public void TestExclusiveNumericRange()
        {
            assertRangeQuery(NumberType.ZERO, NumberType.ZERO, false, false, 0);
            assertRangeQuery(NumberType.ZERO, NumberType.POSITIVE, false, false, 0);
            assertRangeQuery(NumberType.NEGATIVE, NumberType.ZERO, false, false, 0);
            assertRangeQuery(NumberType.NEGATIVE, NumberType.POSITIVE, false, false, 1);
            assertRangeQuery(NumberType.NEGATIVE, NumberType.NEGATIVE, false, false, 0);
        }

        [Test]
        public void TestOpenRangeNumericQuery()
        {
            assertOpenRangeQuery(NumberType.ZERO, "<", 1);
            assertOpenRangeQuery(NumberType.POSITIVE, "<", 2);
            assertOpenRangeQuery(NumberType.NEGATIVE, "<", 0);

            assertOpenRangeQuery(NumberType.ZERO, "<=", 2);
            assertOpenRangeQuery(NumberType.POSITIVE, "<=", 3);
            assertOpenRangeQuery(NumberType.NEGATIVE, "<=", 1);

            assertOpenRangeQuery(NumberType.ZERO, ">", 1);
            assertOpenRangeQuery(NumberType.POSITIVE, ">", 0);
            assertOpenRangeQuery(NumberType.NEGATIVE, ">", 2);

            assertOpenRangeQuery(NumberType.ZERO, ">=", 2);
            assertOpenRangeQuery(NumberType.POSITIVE, ">=", 1);
            assertOpenRangeQuery(NumberType.NEGATIVE, ">=", 3);

            assertOpenRangeQuery(NumberType.NEGATIVE, "=", 1);
            assertOpenRangeQuery(NumberType.ZERO, "=", 1);
            assertOpenRangeQuery(NumberType.POSITIVE, "=", 1);

            assertRangeQuery(NumberType.NEGATIVE, null, true, true, 3);
            assertRangeQuery(NumberType.NEGATIVE, null, false, true, 2);
            assertRangeQuery(NumberType.POSITIVE, null, true, false, 1);
            assertRangeQuery(NumberType.ZERO, null, false, false, 1);

            assertRangeQuery(null, NumberType.POSITIVE, true, true, 3);
            assertRangeQuery(null, NumberType.POSITIVE, true, false, 2);
            assertRangeQuery(null, NumberType.NEGATIVE, false, true, 1);
            assertRangeQuery(null, NumberType.ZERO, false, false, 1);

            assertRangeQuery(null, null, false, false, 3);
            assertRangeQuery(null, null, true, true, 3);

        }

        [Test]
        public void TestSimpleNumericQuery()
        {
            assertSimpleQuery(NumberType.ZERO, 1);
            assertSimpleQuery(NumberType.POSITIVE, 1);
            assertSimpleQuery(NumberType.NEGATIVE, 1);
        }

        public void assertRangeQuery(NumberType? lowerType, NumberType? upperType,
            bool lowerInclusive, bool upperInclusive, int expectedDocCount)
        {


            StringBuilder sb = new StringBuilder();

            String lowerInclusiveStr = (lowerInclusive ? "[" : "{");
            String upperInclusiveStr = (upperInclusive ? "]" : "}");

            foreach (NumericType type in Enum.GetValues(typeof(NumericType)))
            {
                String lowerStr = NumberToString(GetNumberType(lowerType, type.ToString()));
                String upperStr = NumberToString(GetNumberType(upperType, type.ToString()));

                sb.append("+").append(type.ToString()).append(':').append(lowerInclusiveStr)
                  .append('"').append(lowerStr).append("\" TO \"").append(upperStr)
                  .append('"').append(upperInclusiveStr).append(' ');
            }

            /*Number*/
            object lowerDateNumber = GetNumberType(lowerType, DATE_FIELD_NAME);
            /*Number*/
            object upperDateNumber = GetNumberType(upperType, DATE_FIELD_NAME);
            String lowerDateStr;
            String upperDateStr;

            if (lowerDateNumber != null)
            {
                //lowerDateStr = ESCAPER.Escape(
                //    DATE_FORMAT.format(new DateTime(lowerDateNumber.longValue())), LOCALE,
                //    EscapeQuerySyntax.Type.STRING).toString();

                lowerDateStr = ESCAPER.Escape(
                            DATE_FORMAT.Format(Convert.ToInt64(lowerDateNumber)),
                            LOCALE,
                            EscapeQuerySyntax.Type.STRING).toString();
            }
            else
            {
                lowerDateStr = "*";
            }

            if (upperDateNumber != null)
            {
                //upperDateStr = ESCAPER.Escape(
                //      DATE_FORMAT.format(new DateTime(upperDateNumber.longValue())), LOCALE,
                //      EscapeQuerySyntax.Type.STRING).toString();

                upperDateStr = ESCAPER.Escape(
                                DATE_FORMAT.Format(Convert.ToInt64(upperDateNumber)),
                                LOCALE,
                                EscapeQuerySyntax.Type.STRING).toString();
            }
            else
            {
                upperDateStr = "*";
            }

            sb.append("+").append(DATE_FIELD_NAME).append(':')
                .append(lowerInclusiveStr).append('"').append(lowerDateStr).append(
                    "\" TO \"").append(upperDateStr).append('"').append(
                    upperInclusiveStr);


            TestQuery(sb.toString(), expectedDocCount);

        }

        public void assertOpenRangeQuery(NumberType boundType, String @operator, int expectedDocCount)
        {

            StringBuilder sb = new StringBuilder();

            foreach (NumericType type in Enum.GetValues(typeof(NumericType)))
            {
                String boundStr = NumberToString(GetNumberType(boundType, type.ToString()));

                sb.append("+").append(type.ToString()).append(@operator).append('"').append(boundStr).append('"').append(' ');
            }

            //String boundDateStr = ESCAPER.Escape(
            //    DATE_FORMAT.format(new Date(getNumberType(boundType, DATE_FIELD_NAME)
            //        .longValue())), LOCALE, EscapeQuerySyntax.Type.STRING).toString();

            string boundDateStr = ESCAPER.Escape(
                                DATE_FORMAT.Format(Convert.ToInt64(GetNumberType(boundType, DATE_FIELD_NAME))),
                                LOCALE,
                                EscapeQuerySyntax.Type.STRING).toString();

            sb.append("+").append(DATE_FIELD_NAME).append(@operator).append('"').append(boundDateStr).append('"');


            TestQuery(sb.toString(), expectedDocCount);
        }

        public void assertSimpleQuery(NumberType numberType, int expectedDocCount)
        {
            StringBuilder sb = new StringBuilder();

            foreach (NumericType type in Enum.GetValues(typeof(NumericType)))
            {
                String numberStr = NumberToString(GetNumberType(numberType, type.ToString()));
                sb.append('+').append(type.ToString()).append(":\"").append(numberStr)
                          .append("\" ");
            }

            //String dateStr = ESCAPER.Escape(
            //    DATE_FORMAT.format(new DateTime(getNumberType(numberType, DATE_FIELD_NAME)
            //        .longValue())), LOCALE, EscapeQuerySyntax.Type.STRING).toString();

            string dateStr = ESCAPER.Escape(
                                DATE_FORMAT.Format(Convert.ToInt64(GetNumberType(numberType, DATE_FIELD_NAME))),
                                LOCALE,
                                EscapeQuerySyntax.Type.STRING).toString();

            sb.append('+').append(DATE_FIELD_NAME).append(":\"").append(dateStr)
                    .append('"');


            TestQuery(sb.toString(), expectedDocCount);

        }

        private void TestQuery(String queryStr, int expectedDocCount)
        {
            if (VERBOSE) Console.WriteLine("Parsing: " + queryStr);

            Query query = qp.Parse(queryStr, FIELD_NAME);
            if (VERBOSE) Console.WriteLine("Querying: " + query);
            TopDocs topDocs = searcher.Search(query, 1000);

            String msg = "Query <" + queryStr + "> retrieved " + topDocs.TotalHits
                + " document(s), " + expectedDocCount + " document(s) expected.";

            if (VERBOSE) Console.WriteLine(msg);


            assertEquals(msg, expectedDocCount, topDocs.TotalHits);
        }

        private static String NumberToString(/*Number*/ object number)
        {
            return number == null ? "*" : ESCAPER.Escape(NUMBER_FORMAT.Format(number),
                LOCALE, EscapeQuerySyntax.Type.STRING).toString();
        }

        private static /*Number*/ object NormalizeNumber(/*Number*/ object number)
        {
            return NUMBER_FORMAT.Parse(NUMBER_FORMAT.Format(number));
        }

        [OneTimeTearDown]
        public static void AfterClass()
        {
            searcher = null;
            reader.Dispose();
            reader = null;
            directory.Dispose();
            directory = null;
            qp = null;
        }
    }
}
