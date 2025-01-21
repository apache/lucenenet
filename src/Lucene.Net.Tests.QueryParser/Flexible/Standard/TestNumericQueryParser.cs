using J2N.Numerics;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using JCG = J2N.Collections.Generic;
#nullable enable

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

    /// <summary>
    /// Tests <see cref="StandardQueryParser"/> with numeric range queries.
    /// </summary>
    /// <remarks>
    /// LUCENENET NOTE: Several properties have been changed from static to instance,
    /// so that the test can be run multiple times with different random values, such
    /// as with the <see cref="RepeatAttribute"/>. This also helps with repeatability
    /// of tests with a fixed seed.
    /// <para />
    /// However, note that test failures are not guaranteed to be repeatable across
    /// different systems, since the set of available cultures may be different.
    /// </remarks>
    public class TestNumericQueryParser : LuceneTestCase
    {
        public enum NumberType
        {
            NEGATIVE, ZERO, POSITIVE
        }

        private readonly static DateFormat[] DATE_STYLES = {DateFormat.FULL, DateFormat.LONG,
            DateFormat.MEDIUM, DateFormat.SHORT};

        // LUCENENET NOTE: Several fields changed from static to instance, see class remarks.
        private const int PRECISION_STEP = 8;
        private const string FIELD_NAME = "field";
        private CultureInfo? LOCALE;
        private TimeZoneInfo? TIMEZONE;
        private IDictionary<string, J2N.Numerics.Number>? RANDOM_NUMBER_MAP;
        private readonly static IEscapeQuerySyntax ESCAPER = new Standard.Parser.EscapeQuerySyntax();
        private const string DATE_FIELD_NAME = "date";
        private DateFormat DATE_STYLE;
        private DateFormat TIME_STYLE;

        private Analyzer? ANALYZER;

        private NumberFormat? NUMBER_FORMAT;

        private StandardQueryParser? qp;

        private NumberDateFormat? DATE_FORMAT;

        private Directory? directory = null;
        private IndexReader? reader = null;
        private IndexSearcher? searcher = null;

        private static bool CheckDateFormatSanity(NumberDateFormat dateFormat, long date, TimeZoneInfo timeZone)
        {
            IFormatProvider provider = dateFormat.FormatProvider ?? CultureInfo.CurrentCulture;

            if (IsOutOfBounds(date, provider))
                return false;

            string format = dateFormat.GetDateFormat();
            DateTimeOffset offset = DateTimeOffsetUtil.FromUnixTimeMilliseconds(Convert.ToInt64(date));
            offset = TimeZoneInfo.ConvertTime(offset, timeZone);
            string formattedDate = offset.ToString(format, provider);

            return DateTimeOffset.TryParseExact(formattedDate, format, provider, DateTimeStyles.None, out DateTimeOffset parsed)
                && parsed.ToUnixTimeMilliseconds() == date;
        }

        // LUCENENET specific bounds check
        // We need to be sure that the date is within the range of the current calendar, or we will get
        // an ArgumentOutOfRangeException when attempting to materialize it.
        private static bool IsOutOfBounds(double date, IFormatProvider provider)
        {
            Calendar calendar = GetCalendar(provider);

            if (date < DateTimeOffsetUtil.MinMilliseconds || date > DateTimeOffsetUtil.MaxMilliseconds)
                return false;

            // We can't convert to a DateTimeOffset because it will do the calendar check and throw ArgumentOutOfRangeException
            // before we can check the range.
            long newDateTicks = DateTimeOffsetUtil.GetTicksFromUnixTimeMilliseconds(Convert.ToInt64(date));
            return newDateTicks < calendar.MinSupportedDateTime.Ticks || newDateTicks > calendar.MaxSupportedDateTime.Ticks;
        }

        // LUCENENET specific bounds check
        private static bool IsOutOfBoundsOrZero(double absNumber, IFormatProvider provider)
        {
            return absNumber == 0 || IsOutOfBounds(absNumber, provider) || IsOutOfBounds(-absNumber, provider);
        }

        /// <summary>
        /// Returns the <see cref="Calendar"/> from the specified <paramref name="provider"/>.
        /// </summary>
        /// <param name="provider">
        /// The provider to use to format the value.
        /// <para/>
        /// -or-
        /// <para/>
        /// A null reference (Nothing in Visual Basic) to obtain the numeric format information from the locale setting of the current thread.
        /// </param>
        /// <returns>The <see cref="Calendar"/> instance.</returns>
        /// <exception cref="NotSupportedException">The supplied <paramref name="provider"/> returned <c>null</c> for the requested type <see cref="DateTimeFormatInfo"/>.</exception>
        internal static Calendar GetCalendar(IFormatProvider? provider)
        {
            DateTimeFormatInfo? dateTimeFormat = (provider ?? DateTimeFormatInfo.CurrentInfo).GetFormat(typeof(DateTimeFormatInfo)) as DateTimeFormatInfo;
            if (dateTimeFormat is null)
                throw new NotSupportedException($"The specified format provider did not return a '{typeof(DateTimeFormatInfo).FullName}' instance from IFormatProvider.GetFormat(System.Type).");

            return dateTimeFormat.Calendar;
        }

        // LUCENENET specific - changed from OneTimeSetUp to SetUp, see class remarks.
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            ANALYZER = new MockAnalyzer(Random);

            qp = new StandardQueryParser(ANALYZER);

            IDictionary<string, Number> randomNumberMap = new JCG.Dictionary<string, Number>();

            /*SimpleDateFormat*/
            //string dateFormat;
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
                LOCALE = RandomCulture(Random);
                TIMEZONE = RandomTimeZone(Random);
                DATE_STYLE = randomDateStyle(Random);
                TIME_STYLE = randomDateStyle(Random);

                //// assumes localized date pattern will have at least year, month, day,
                //// hour, minute
                //dateFormat = (SimpleDateFormat)DateFormat.getDateTimeInstance(
                //    DATE_STYLE, TIME_STYLE, LOCALE);

                //// not all date patterns includes era, full year, timezone and second,
                //// so we add them here
                //dateFormat.applyPattern(dateFormat.toPattern() + " G s Z yyyy");
                //dateFormat.setTimeZone(TIMEZONE);

                // assumes localized date pattern will have at least year, month, day,
                // hour, minute
                DATE_FORMAT = new NumberDateFormat(DATE_STYLE, TIME_STYLE, LOCALE)
                {
                    TimeZone = TIMEZONE
                };

                // not all date patterns includes era, full year, timezone and second,
                // so we add them here
                DATE_FORMAT.SetDateFormat(DATE_FORMAT.GetDateFormat() + " %g s zzz yyyy");


                //dateFormat = DATE_FORMAT.GetDateFormat();

                do
                {
                    randomDate = Random.nextLong();

                    // prune date value so it doesn't pass in insane values to some
                    // calendars.
                    randomDate = randomDate % 3400000000000L;

                    // truncate to second
                    randomDate = (randomDate / 1000L) * 1000L;

                    // only positive values
                    randomDate = Math.Abs(randomDate);
                } while (randomDate == 0L);

                dateFormatSanityCheckPass &= CheckDateFormatSanity(DATE_FORMAT, randomDate, TIMEZONE);

                // LUCENENET NOTE - issue #846: the zh-Hant-TW culture on net5.0-net8.0 does not include
                // the "tt" AM/PM designator in the long time pattern, causing midnight at the unix epoch
                // to be interpreted as an AM time the day before when the time zone has a negative offset,
                // instead of PM as it should be. This "sanity" check also makes sure that the round trip
                // from the unix epoch to a string and back to a date is consistent. If it is not, we will
                // try again with a different locale/time zone. This was fixed in net9.0, so that culture
                // with a negative offset should work as expected.
                dateFormatSanityCheckPass &= CheckDateFormatSanity(DATE_FORMAT, 0, TIMEZONE);

                dateFormatSanityCheckPass &= CheckDateFormatSanity(DATE_FORMAT,
                          -randomDate, TIMEZONE);
                count++;
            } while (!dateFormatSanityCheckPass);

            //NUMBER_FORMAT = NumberFormat.getNumberInstance(LOCALE);
            //NUMBER_FORMAT.setMaximumFractionDigits((Random().nextInt() & 20) + 1);
            //NUMBER_FORMAT.setMinimumFractionDigits((Random().nextInt() & 20) + 1);
            //NUMBER_FORMAT.setMaximumIntegerDigits((Random().nextInt() & 20) + 1);
            //NUMBER_FORMAT.setMinimumIntegerDigits((Random().nextInt() & 20) + 1);

            NUMBER_FORMAT = new MockNumberFormat(LOCALE);

            double randomDouble;
            long randomLong;
            int randomInt;
            float randomFloat;

            while (IsOutOfBoundsOrZero((randomLong = Convert.ToInt64(NormalizeNumber(Math.Abs(Random.nextLong()))
                )), LOCALE))
                ;
            while (IsOutOfBoundsOrZero((randomDouble = Convert.ToDouble(NormalizeNumber(Math.Abs(Random.NextDouble()))
                )), LOCALE))
                ;
            while (IsOutOfBoundsOrZero((randomFloat = Convert.ToSingle(NormalizeNumber(Math.Abs(Random.nextFloat()))
                )), LOCALE))
                ;
            while (IsOutOfBoundsOrZero((randomInt = Convert.ToInt32(NormalizeNumber(Math.Abs(Random.nextInt()))
                )), LOCALE))
                ;

            randomNumberMap[NumericType.INT64.ToString()] = (J2N.Numerics.Int64)randomLong;
            randomNumberMap[NumericType.INT32.ToString()] = (J2N.Numerics.Int32)randomInt;
            randomNumberMap[NumericType.SINGLE.ToString()] = (J2N.Numerics.Single)randomFloat;
            randomNumberMap[NumericType.DOUBLE.ToString()] = (J2N.Numerics.Double)randomDouble;
            randomNumberMap[DATE_FIELD_NAME] = (J2N.Numerics.Int64)randomDate;

            RANDOM_NUMBER_MAP = JCG.Extensions.DictionaryExtensions.AsReadOnly(randomNumberMap);

            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory,
                NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random))
                    .SetMaxBufferedDocs(TestUtil.NextInt32(Random, 50, 1000))
                    .SetMergePolicy(NewLogMergePolicy()));

            Document doc = new Document();
            IDictionary<string, NumericConfig> numericConfigMap = new JCG.Dictionary<string, NumericConfig>();
            IDictionary<string, Field> numericFieldMap = new JCG.Dictionary<string, Field>();
            qp.NumericConfigMap = numericConfigMap;

            foreach (NumericType type in (NumericType[])Enum.GetValues(typeof(NumericType)))
            {
                if (type == NumericType.NONE)
                {
                    continue;
                }

                numericConfigMap[type.ToString()] = new NumericConfig(PRECISION_STEP, NUMBER_FORMAT, type);

                FieldType ft2 = new FieldType(Int32Field.TYPE_NOT_STORED);
                ft2.NumericType = type;
                ft2.IsStored = true;
                ft2.NumericPrecisionStep = PRECISION_STEP;
                ft2.Freeze();
                Field field;

                switch (type)
                {
                    case NumericType.INT32:
                        field = new Int32Field(type.ToString(), 0, ft2);
                        break;
                    case NumericType.SINGLE:
                        field = new SingleField(type.ToString(), 0.0f, ft2);
                        break;
                    case NumericType.INT64:
                        field = new Int64Field(type.ToString(), 0L, ft2);
                        break;
                    case NumericType.DOUBLE:
                        field = new DoubleField(type.ToString(), 0.0, ft2);
                        break;
                    default:
                        fail();
                        field = null!;
                        break;
                }
                numericFieldMap[type.ToString()] = field;
                doc.Add(field);
            }

            numericConfigMap[DATE_FIELD_NAME] = new NumericConfig(PRECISION_STEP, DATE_FORMAT, NumericType.INT64);
            FieldType ft = new FieldType(Int64Field.TYPE_NOT_STORED);
            ft.IsStored = true;
            ft.NumericPrecisionStep = PRECISION_STEP;
            Int64Field dateField = new Int64Field(DATE_FIELD_NAME, 0L, ft);
            numericFieldMap[DATE_FIELD_NAME] = dateField;
            doc.Add(dateField);

            foreach (NumberType numberType in (NumberType[])Enum.GetValues(typeof(NumberType)))
            {
                SetFieldValues(numberType, numericFieldMap);
                if (Verbose) Console.WriteLine("Indexing document: " + doc);
                writer.AddDocument(doc);
            }

            reader = writer.GetReader();
            searcher = NewSearcher(reader);
            writer.Dispose();
        }

        private Number? GetNumberType(NumberType? numberType, string fieldName)
        {
            if (numberType is null)
            {
                return null;
            }

            switch (numberType)
            {

                case NumberType.POSITIVE:
                    return RANDOM_NUMBER_MAP![fieldName];

                case NumberType.NEGATIVE:
                    Number number = RANDOM_NUMBER_MAP![fieldName];

                    if (NumericType.INT64.ToString().Equals(fieldName, StringComparison.Ordinal)
                        || DATE_FIELD_NAME.Equals(fieldName, StringComparison.Ordinal))
                    {
                        number = J2N.Numerics.Int64.GetInstance(-number.ToInt64());

                    }
                    else if (NumericType.DOUBLE.ToString().Equals(fieldName, StringComparison.Ordinal))
                    {
                        number = J2N.Numerics.Double.GetInstance(-number.ToDouble());

                    }
                    else if (NumericType.SINGLE.ToString().Equals(fieldName, StringComparison.Ordinal))
                    {
                        number = J2N.Numerics.Single.GetInstance(-number.ToSingle());

                    }
                    else if (NumericType.INT32.ToString().Equals(fieldName, StringComparison.Ordinal))
                    {
                        number = J2N.Numerics.Int32.GetInstance(-number.ToInt32());

                    }
                    else
                    {
                        throw new ArgumentException("field name not found: "
                            + fieldName);
                    }

                    return number;

                default:
                    return J2N.Numerics.Int32.GetInstance(0);
            }
        }

        private void SetFieldValues(NumberType numberType,
            IDictionary<string, Field> numericFieldMap)
        {

            Number? number = GetNumberType(numberType, NumericType.DOUBLE
                .ToString());
            numericFieldMap[NumericType.DOUBLE.ToString()].SetDoubleValue(Convert.ToDouble(
                number));

            number = GetNumberType(numberType, NumericType.INT32.ToString());
            numericFieldMap[NumericType.INT32.ToString()].SetInt32Value(Convert.ToInt32(
                number));

            number = GetNumberType(numberType, NumericType.INT64.ToString());
            numericFieldMap[NumericType.INT64.ToString()].SetInt64Value(Convert.ToInt64(
                number));

            number = GetNumberType(numberType, NumericType.SINGLE.ToString());
            numericFieldMap[NumericType.SINGLE.ToString()].SetSingleValue(Convert.ToSingle(
                number));

            number = GetNumberType(numberType, DATE_FIELD_NAME);
            numericFieldMap[DATE_FIELD_NAME].SetInt64Value(Convert.ToInt64(number));
        }

        private static DateFormat randomDateStyle(Random random)
        {
            return DATE_STYLES[random.nextInt(DATE_STYLES.Length)];
        }

        private class MockNumberFormat : NumberFormat
        {
            public MockNumberFormat(IFormatProvider? provider) : base(provider) { }

            public override Number Parse(string source)
            {
                double dbl = J2N.Numerics.Double.Parse(source, FormatProvider);
                if (dbl == (long)dbl)
                    return J2N.Numerics.Int64.GetInstance((long)dbl);
                return J2N.Numerics.Double.GetInstance(dbl);
            }
        }

        [Test]
        public void TestInclusiveNumericRange()
        {
            AssertRangeQuery(NumberType.ZERO, NumberType.ZERO, true, true, 1);
            AssertRangeQuery(NumberType.ZERO, NumberType.POSITIVE, true, true, 2);
            AssertRangeQuery(NumberType.NEGATIVE, NumberType.ZERO, true, true, 2);
            AssertRangeQuery(NumberType.NEGATIVE, NumberType.POSITIVE, true, true, 3);
            AssertRangeQuery(NumberType.NEGATIVE, NumberType.NEGATIVE, true, true, 1);
        }

        [Test]
        // test disabled since standard syntax parser does not work with inclusive and
        // exclusive at the same time
        public void TestInclusiveLowerNumericRange()
        {
            AssertRangeQuery(NumberType.NEGATIVE, NumberType.ZERO, false, true, 1);
            AssertRangeQuery(NumberType.ZERO, NumberType.POSITIVE, false, true, 1);
            AssertRangeQuery(NumberType.NEGATIVE, NumberType.POSITIVE, false, true, 2);
            AssertRangeQuery(NumberType.NEGATIVE, NumberType.NEGATIVE, false, true, 0);
        }

        [Test]
        // test disabled since standard syntax parser does not work with inclusive and
        // exclusive at the same time
        public void TestInclusiveUpperNumericRange()
        {
            AssertRangeQuery(NumberType.NEGATIVE, NumberType.ZERO, true, false, 1);
            AssertRangeQuery(NumberType.ZERO, NumberType.POSITIVE, true, false, 1);
            AssertRangeQuery(NumberType.NEGATIVE, NumberType.POSITIVE, true, false, 2);
            AssertRangeQuery(NumberType.NEGATIVE, NumberType.NEGATIVE, true, false, 0);
        }

        [Test]
        public void TestExclusiveNumericRange()
        {
            AssertRangeQuery(NumberType.ZERO, NumberType.ZERO, false, false, 0);
            AssertRangeQuery(NumberType.ZERO, NumberType.POSITIVE, false, false, 0);
            AssertRangeQuery(NumberType.NEGATIVE, NumberType.ZERO, false, false, 0);
            AssertRangeQuery(NumberType.NEGATIVE, NumberType.POSITIVE, false, false, 1);
            AssertRangeQuery(NumberType.NEGATIVE, NumberType.NEGATIVE, false, false, 0);
        }

        [Test]
        public void TestOpenRangeNumericQuery()
        {
            AssertOpenRangeQuery(NumberType.ZERO, "<", 1);
            AssertOpenRangeQuery(NumberType.POSITIVE, "<", 2);
            AssertOpenRangeQuery(NumberType.NEGATIVE, "<", 0);

            AssertOpenRangeQuery(NumberType.ZERO, "<=", 2);
            AssertOpenRangeQuery(NumberType.POSITIVE, "<=", 3);
            AssertOpenRangeQuery(NumberType.NEGATIVE, "<=", 1);

            AssertOpenRangeQuery(NumberType.ZERO, ">", 1);
            AssertOpenRangeQuery(NumberType.POSITIVE, ">", 0);
            AssertOpenRangeQuery(NumberType.NEGATIVE, ">", 2);

            AssertOpenRangeQuery(NumberType.ZERO, ">=", 2);
            AssertOpenRangeQuery(NumberType.POSITIVE, ">=", 1);
            AssertOpenRangeQuery(NumberType.NEGATIVE, ">=", 3);

            AssertOpenRangeQuery(NumberType.NEGATIVE, "=", 1);
            AssertOpenRangeQuery(NumberType.ZERO, "=", 1);
            AssertOpenRangeQuery(NumberType.POSITIVE, "=", 1);

            AssertRangeQuery(NumberType.NEGATIVE, null, true, true, 3);
            AssertRangeQuery(NumberType.NEGATIVE, null, false, true, 2);
            AssertRangeQuery(NumberType.POSITIVE, null, true, false, 1);
            AssertRangeQuery(NumberType.ZERO, null, false, false, 1);

            AssertRangeQuery(null, NumberType.POSITIVE, true, true, 3);
            AssertRangeQuery(null, NumberType.POSITIVE, true, false, 2);
            AssertRangeQuery(null, NumberType.NEGATIVE, false, true, 1);
            AssertRangeQuery(null, NumberType.ZERO, false, false, 1);

            AssertRangeQuery(null, null, false, false, 3);
            AssertRangeQuery(null, null, true, true, 3);

        }

        [Test]
        public void TestSimpleNumericQuery()
        {
            AssertSimpleQuery(NumberType.ZERO, 1);
            AssertSimpleQuery(NumberType.POSITIVE, 1);
            AssertSimpleQuery(NumberType.NEGATIVE, 1);
        }

        public void AssertRangeQuery(NumberType? lowerType, NumberType? upperType,
            bool lowerInclusive, bool upperInclusive, int expectedDocCount)
        {
            StringBuilder sb = new StringBuilder();

            string lowerInclusiveStr = (lowerInclusive ? "[" : "{");
            string upperInclusiveStr = (upperInclusive ? "]" : "}");

            foreach (NumericType type in (NumericType[])Enum.GetValues(typeof(NumericType)))
            {
                if (type == NumericType.NONE)
                {
                    continue;
                }

                string lowerStr = NumberToString(GetNumberType(lowerType, type.ToString()));
                string upperStr = NumberToString(GetNumberType(upperType, type.ToString()));

                sb.append("+").append(type.ToString()).append(':').append(lowerInclusiveStr)
                  .append('"').append(lowerStr).append("\" TO \"").append(upperStr)
                  .append('"').append(upperInclusiveStr).append(' ');
            }

            Number? lowerDateNumber = GetNumberType(lowerType, DATE_FIELD_NAME);
            Number? upperDateNumber = GetNumberType(upperType, DATE_FIELD_NAME);
            string lowerDateStr;
            string upperDateStr;

            if (lowerDateNumber != null)
            {
                //lowerDateStr = ESCAPER.Escape(
                //    DATE_FORMAT.format(new DateTime(lowerDateNumber.longValue())), LOCALE,
                //    EscapeQuerySyntax.Type.STRING).toString();

                lowerDateStr = ESCAPER.Escape(
                            DATE_FORMAT!.Format(Convert.ToInt64(lowerDateNumber, CultureInfo.InvariantCulture)),
                            LOCALE,
                            EscapeQuerySyntaxType.STRING).toString();
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
                                DATE_FORMAT!.Format(Convert.ToInt64(upperDateNumber, CultureInfo.InvariantCulture)),
                                LOCALE,
                                EscapeQuerySyntaxType.STRING).toString();
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

        public void AssertOpenRangeQuery(NumberType boundType, string @operator, int expectedDocCount)
        {
            StringBuilder sb = new StringBuilder();

            foreach (NumericType type in (NumericType[])Enum.GetValues(typeof(NumericType)))
            {
                if (type == NumericType.NONE)
                {
                    continue;
                }

                string boundStr = NumberToString(GetNumberType(boundType, type.ToString()));

                sb.append("+").append(type.ToString()).append(@operator).append('"').append(boundStr).append('"').append(' ');
            }

            //String boundDateStr = ESCAPER.Escape(
            //    DATE_FORMAT.format(new Date(getNumberType(boundType, DATE_FIELD_NAME)
            //        .longValue())), LOCALE, EscapeQuerySyntax.Type.STRING).toString();

            string boundDateStr = ESCAPER.Escape(
                                DATE_FORMAT!.Format(Convert.ToInt64(GetNumberType(boundType, DATE_FIELD_NAME))),
                                LOCALE,
                                EscapeQuerySyntaxType.STRING).toString();

            sb.append("+").append(DATE_FIELD_NAME).append(@operator).append('"').append(boundDateStr).append('"');


            TestQuery(sb.toString(), expectedDocCount);
        }

        public void AssertSimpleQuery(NumberType numberType, int expectedDocCount)
        {
            StringBuilder sb = new StringBuilder();

            foreach (NumericType type in (NumericType[])Enum.GetValues(typeof(NumericType)))
            {
                if (type == NumericType.NONE)
                {
                    continue;
                }

                string numberStr = NumberToString(GetNumberType(numberType, type.ToString()));
                sb.append('+').append(type.ToString()).append(":\"").append(numberStr)
                          .append("\" ");
            }

            //String dateStr = ESCAPER.Escape(
            //    DATE_FORMAT.format(new DateTime(getNumberType(numberType, DATE_FIELD_NAME)
            //        .longValue())), LOCALE, EscapeQuerySyntax.Type.STRING).toString();

            string dateStr = ESCAPER.Escape(
                                DATE_FORMAT!.Format(Convert.ToInt64(GetNumberType(numberType, DATE_FIELD_NAME))),
                                LOCALE,
                                EscapeQuerySyntaxType.STRING).toString();

            sb.append('+').append(DATE_FIELD_NAME).append(":\"").append(dateStr)
                    .append('"');


            TestQuery(sb.toString(), expectedDocCount);
        }

        private void TestQuery(string queryStr, int expectedDocCount)
        {
            if (Verbose) Console.WriteLine("Parsing: " + queryStr);

            Query query = qp!.Parse(queryStr, FIELD_NAME);
            if (Verbose) Console.WriteLine("Querying: " + query);
            TopDocs topDocs = searcher!.Search(query, 1000);

            string msg = $"Query <{queryStr}> retrieved {topDocs.TotalHits} document(s), {expectedDocCount} document(s) expected. " +
                         $"[LOCALE: {LOCALE}, TIMEZONE: {TIMEZONE!.Id}, DATE_STYLE: {DATE_STYLE}, TIME_STYLE: {TIME_STYLE}]";

            if (Verbose) Console.WriteLine(msg);

            assertEquals(msg, expectedDocCount, topDocs.TotalHits);
        }

        private string NumberToString(Number? number)
        {
            return number is null ? "*" : ESCAPER.Escape(NUMBER_FORMAT!.Format(number),
                LOCALE, EscapeQuerySyntaxType.STRING).toString();
        }

        private Number NormalizeNumber(/*Number*/ object number)
        {
            return NUMBER_FORMAT!.Parse(NUMBER_FORMAT.Format(number));
        }

        // LUCENENET specific - changed from OneTimeTearDown to TearDown, see class remarks.
        [TearDown]
        public override void TearDown()
        {
            searcher = null;
            reader?.Dispose();
            reader = null;
            directory?.Dispose();
            directory = null;
            qp = null;

            base.TearDown();
        }
    }
}
