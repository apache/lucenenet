//using Lucene.Net;
//using Lucene.Net.Analysis;
//using Lucene.Net.Documents;
//using Lucene.Net.Index;
//using Lucene.Net.QueryParsers.Flexible.Core.Parser;
//using Lucene.Net.QueryParsers.Flexible.Standard.Config;
//using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
//using Lucene.Net.Search;
//using Lucene.Net.Store;
//using Lucene.Net.Support;
//using Lucene.Net.Util;
//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Lucene.Net.QueryParsers.Flexible.Standard
//{
//    public class TestNumericQueryParser : LuceneTestCase
//    {
//        public enum NumberType
//        {
//            NEGATIVE, ZERO, POSITIVE
//        }

//        /// <summary>
//        /// LUCENENET specific enum for mimicking the Java DateFormat
//        /// </summary>
//        public enum DateFormat
//        {
//            FULL, LONG,
//            MEDIUM, SHORT
//        }

//        //  private readonly static int[] DATE_STYLES = {DateFormat.FULL, DateFormat.LONG,
//        //DateFormat.MEDIUM, DateFormat.SHORT};
//        private readonly static int[] DATE_STYLES = {(int)DateFormat.FULL, (int)DateFormat.LONG,
//            (int)DateFormat.MEDIUM, (int)DateFormat.SHORT};

//        private readonly static int PRECISION_STEP = 8;
//        private readonly static String FIELD_NAME = "field";
//        private static CultureInfo LOCALE;
//        private static TimeZoneInfo TIMEZONE;
//        private static IDictionary<String, /*Number*/ object> RANDOM_NUMBER_MAP;
//        private readonly static IEscapeQuerySyntax ESCAPER = new EscapeQuerySyntaxImpl();
//        private readonly static String DATE_FIELD_NAME = "date";
//        private static int DATE_STYLE;
//        private static int TIME_STYLE;

//        private static Analyzer ANALYZER;

//        private static /*NumberFormat*/ string NUMBER_FORMAT;

//        private static StandardQueryParser qp;

//        private static /*NumberDateFormat*/ string DATE_FORMAT;

//        private static Directory directory = null;
//        private static IndexReader reader = null;
//        private static IndexSearcher searcher = null;

//        private static bool checkDateFormatSanity(/*DateFormat*/string dateFormat, long date)
//        {
//            DateTime result;
//            return DateTime.TryParseExact(new DateTime(date).ToString(dateFormat),
//                dateFormat, CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind, out result);
//            //try
//            //{
//            //    return date == dateFormat.parse(dateFormat.format(new DateTime(date)))
//            //      .getTime();
//            //}
//            //catch (ParseException e)
//            //{
//            //    return false;
//            //}
//        }

//        [TestFixtureSetUp]
//        public void BeforeClass()
//        {
//            ANALYZER = new MockAnalyzer(Random());

//            qp = new StandardQueryParser(ANALYZER);

//            HashMap<String, /*Number*/object> randomNumberMap = new HashMap<string, object>();

//            /*SimpleDateFormat*/
//            string dateFormat;
//            long randomDate;
//            bool dateFormatSanityCheckPass;
//            int count = 0;
//            do
//            {
//                if (count > 100)
//                {
//                    fail("This test has problems to find a sane random DateFormat/NumberFormat. Stopped trying after 100 iterations.");
//                }

//                dateFormatSanityCheckPass = true;
//                LOCALE = randomLocale(Random());
//                TIMEZONE = randomTimeZone(Random());
//                DATE_STYLE = randomDateStyle(Random());
//                TIME_STYLE = randomDateStyle(Random());

//                //// assumes localized date pattern will have at least year, month, day,
//                //// hour, minute
//                //dateFormat = (SimpleDateFormat)DateFormat.getDateTimeInstance(
//                //    DATE_STYLE, TIME_STYLE, LOCALE);

//                //// not all date patterns includes era, full year, timezone and second,
//                //// so we add them here
//                //dateFormat.applyPattern(dateFormat.toPattern() + " G s Z yyyy");
//                //dateFormat.setTimeZone(TIMEZONE);

//                //DATE_FORMAT = new NumberDateFormat(dateFormat);


//                do
//                {
//                    randomDate = Random().nextLong();

//                    // prune date value so it doesn't pass in insane values to some
//                    // calendars.
//                    randomDate = randomDate % 3400000000000l;

//                    // truncate to second
//                    randomDate = (randomDate / 1000L) * 1000L;

//                    // only positive values
//                    randomDate = Math.Abs(randomDate);
//                } while (randomDate == 0L);

//                dateFormatSanityCheckPass &= checkDateFormatSanity(dateFormat, randomDate);

//                dateFormatSanityCheckPass &= checkDateFormatSanity(dateFormat, 0);

//                dateFormatSanityCheckPass &= checkDateFormatSanity(dateFormat,
//                          -randomDate);

//                count++;
//            } while (!dateFormatSanityCheckPass);

//            NUMBER_FORMAT = NumberFormat.getNumberInstance(LOCALE);
//            NUMBER_FORMAT.setMaximumFractionDigits((Random().nextInt() & 20) + 1);
//            NUMBER_FORMAT.setMinimumFractionDigits((Random().nextInt() & 20) + 1);
//            NUMBER_FORMAT.setMaximumIntegerDigits((Random().nextInt() & 20) + 1);
//            NUMBER_FORMAT.setMinimumIntegerDigits((Random().nextInt() & 20) + 1);

//            double randomDouble;
//            long randomLong;
//            int randomInt;
//            float randomFloat;

//            while ((randomLong = Convert.ToInt64(normalizeNumber(Math.Abs(Random().nextLong()))
//                )) == 0L)
//                ;
//            while ((randomDouble = Convert.ToDouble(normalizeNumber(Math.Abs(Random().NextDouble()))
//                )) == 0.0)
//                ;
//            while ((randomFloat = Convert.ToSingle(normalizeNumber(Math.Abs(Random().nextFloat()))
//                )) == 0.0f)
//                ;
//            while ((randomInt = Convert.ToInt32(normalizeNumber(Math.Abs(Random().nextInt())))) == 0)
//                ;

//            randomNumberMap.Put(FieldType.NumericType.LONG.ToString(), randomLong);
//            randomNumberMap.Put(FieldType.NumericType.INT.ToString(), randomInt);
//            randomNumberMap.Put(FieldType.NumericType.FLOAT.ToString(), randomFloat);
//            randomNumberMap.Put(FieldType.NumericType.DOUBLE.ToString(), randomDouble);
//            randomNumberMap.Put(DATE_FIELD_NAME, randomDate);

//            RANDOM_NUMBER_MAP = Collections.UnmodifiableMap(randomNumberMap);

//            directory = NewDirectory();
//            RandomIndexWriter writer = new RandomIndexWriter(Random(), directory,
//                NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()))
//                    .SetMaxBufferedDocs(TestUtil.NextInt(Random(), 50, 1000))
//                    .SetMergePolicy(NewLogMergePolicy()));

//            Document doc = new Document();
//            HashMap<String, NumericConfig> numericConfigMap = new HashMap<String, NumericConfig>();
//            HashMap<String, Field> numericFieldMap = new HashMap<String, Field>();
//            qp.SetNumericConfigMap(numericConfigMap);

//            foreach (FieldType.NumericType type in Enum.GetValues(typeof(FieldType.NumericType)))
//            {
//                numericConfigMap.Put(type.ToString(), new NumericConfig(PRECISION_STEP,
//                    NUMBER_FORMAT, type));

//                FieldType ft2 = new FieldType(IntField.TYPE_NOT_STORED);
//                ft2.NumericTypeValue = (type);
//                ft2.Stored = (true);
//                ft2.NumericPrecisionStep = (PRECISION_STEP);
//                ft2.Freeze();
//                Field field;

//                switch (type)
//                {
//                    case FieldType.NumericType.INT:
//                        field = new IntField(type.ToString(), 0, ft2);
//                        break;
//                    case FieldType.NumericType.FLOAT:
//                        field = new FloatField(type.ToString(), 0.0f, ft2);
//                        break;
//                    case FieldType.NumericType.LONG:
//                        field = new LongField(type.ToString(), 0L, ft2);
//                        break;
//                    case FieldType.NumericType.DOUBLE:
//                        field = new DoubleField(type.ToString(), 0.0, ft2);
//                        break;
//                    default:
//                        fail();
//                        field = null;
//                        break;
//                }
//                numericFieldMap.Put(type.ToString(), field);
//                doc.Add(field);
//            }

//            numericConfigMap.Put(DATE_FIELD_NAME, new NumericConfig(PRECISION_STEP,
//                DATE_FORMAT, FieldType.NumericType.LONG));
//            FieldType ft = new FieldType(LongField.TYPE_NOT_STORED);
//            ft.Stored = (true);
//            ft.NumericPrecisionStep = (PRECISION_STEP);
//            LongField dateField = new LongField(DATE_FIELD_NAME, 0l, ft);
//            numericFieldMap.Put(DATE_FIELD_NAME, dateField);
//            doc.Add(dateField);

//            foreach (NumberType numberType in Enum.GetValues(typeof(NumberType)))
//            {
//                setFieldValues(numberType, numericFieldMap);
//                if (VERBOSE) Console.WriteLine("Indexing document: " + doc);
//                writer.AddDocument(doc);
//            }

//            reader = writer.Reader;
//            searcher = NewSearcher(reader);
//            writer.Dispose();

//        }

//        private static /*Number*/ object getNumberType(NumberType? numberType, String fieldName)
//        {

//            if (numberType == null)
//            {
//                return null;
//            }

//            switch (numberType)
//            {

//                case NumberType.POSITIVE:
//                    return RANDOM_NUMBER_MAP[fieldName];

//                case NumberType.NEGATIVE:
//                    /*Number*/
//                    object number = RANDOM_NUMBER_MAP[fieldName];

//                    if (FieldType.NumericType.LONG.ToString().equals(fieldName)
//                        || DATE_FIELD_NAME.equals(fieldName))
//                    {
//                        number = -Convert.ToInt64(number);

//                    }
//                    else if (FieldType.NumericType.DOUBLE.ToString().equals(fieldName))
//                    {
//                        number = -Convert.ToDouble(number);

//                    }
//                    else if (FieldType.NumericType.FLOAT.ToString().equals(fieldName))
//                    {
//                        number = -Convert.ToSingle(number);

//                    }
//                    else if (FieldType.NumericType.INT.ToString().equals(fieldName))
//                    {
//                        number = -Convert.ToInt32(number);

//                    }
//                    else
//                    {
//                        throw new ArgumentException("field name not found: "
//                            + fieldName);
//                    }

//                    return number;

//                default:
//                    return 0;

//            }

//        }

//        private static void setFieldValues(NumberType numberType,
//            HashMap<String, Field> numericFieldMap)
//        {

//            /*Number*/
//            object number = getNumberType(numberType, FieldType.NumericType.DOUBLE
//     .ToString());
//            numericFieldMap[FieldType.NumericType.DOUBLE.ToString()].DoubleValue = Convert.ToDouble(
//                number);

//            number = getNumberType(numberType, FieldType.NumericType.INT.ToString());
//            numericFieldMap[FieldType.NumericType.INT.ToString()].IntValue = Convert.ToInt32(
//                number);

//            number = getNumberType(numberType, FieldType.NumericType.LONG.ToString());
//            numericFieldMap[FieldType.NumericType.LONG.ToString()].LongValue = Convert.ToInt64(
//                number);

//            number = getNumberType(numberType, FieldType.NumericType.FLOAT.ToString());
//            numericFieldMap[FieldType.NumericType.FLOAT.ToString()].FloatValue = Convert.ToSingle(
//                number);

//            number = getNumberType(numberType, DATE_FIELD_NAME);
//            numericFieldMap[DATE_FIELD_NAME].LongValue = Convert.ToInt64(number);
//        }

//        private static int randomDateStyle(Random random)
//        {
//            return DATE_STYLES[random.nextInt(DATE_STYLES.Length)];
//        }

//        [Test]
//        public void testInclusiveNumericRange()
//        {
//            assertRangeQuery(NumberType.ZERO, NumberType.ZERO, true, true, 1);
//            assertRangeQuery(NumberType.ZERO, NumberType.POSITIVE, true, true, 2);
//            assertRangeQuery(NumberType.NEGATIVE, NumberType.ZERO, true, true, 2);
//            assertRangeQuery(NumberType.NEGATIVE, NumberType.POSITIVE, true, true, 3);
//            assertRangeQuery(NumberType.NEGATIVE, NumberType.NEGATIVE, true, true, 1);
//        }

//        [Test]
//        // test disabled since standard syntax parser does not work with inclusive and
//        // exclusive at the same time
//        public void testInclusiveLowerNumericRange()
//        {
//            assertRangeQuery(NumberType.NEGATIVE, NumberType.ZERO, false, true, 1);
//            assertRangeQuery(NumberType.ZERO, NumberType.POSITIVE, false, true, 1);
//            assertRangeQuery(NumberType.NEGATIVE, NumberType.POSITIVE, false, true, 2);
//            assertRangeQuery(NumberType.NEGATIVE, NumberType.NEGATIVE, false, true, 0);
//        }

//        [Test]
//        // test disabled since standard syntax parser does not work with inclusive and
//        // exclusive at the same time
//        public void testInclusiveUpperNumericRange()
//        {
//            assertRangeQuery(NumberType.NEGATIVE, NumberType.ZERO, true, false, 1);
//            assertRangeQuery(NumberType.ZERO, NumberType.POSITIVE, true, false, 1);
//            assertRangeQuery(NumberType.NEGATIVE, NumberType.POSITIVE, true, false, 2);
//            assertRangeQuery(NumberType.NEGATIVE, NumberType.NEGATIVE, true, false, 0);
//        }

//        [Test]
//        public void testExclusiveNumericRange()
//        {
//            assertRangeQuery(NumberType.ZERO, NumberType.ZERO, false, false, 0);
//            assertRangeQuery(NumberType.ZERO, NumberType.POSITIVE, false, false, 0);
//            assertRangeQuery(NumberType.NEGATIVE, NumberType.ZERO, false, false, 0);
//            assertRangeQuery(NumberType.NEGATIVE, NumberType.POSITIVE, false, false, 1);
//            assertRangeQuery(NumberType.NEGATIVE, NumberType.NEGATIVE, false, false, 0);
//        }

//        [Test]
//        public void testOpenRangeNumericQuery()
//        {
//            assertOpenRangeQuery(NumberType.ZERO, "<", 1);
//            assertOpenRangeQuery(NumberType.POSITIVE, "<", 2);
//            assertOpenRangeQuery(NumberType.NEGATIVE, "<", 0);

//            assertOpenRangeQuery(NumberType.ZERO, "<=", 2);
//            assertOpenRangeQuery(NumberType.POSITIVE, "<=", 3);
//            assertOpenRangeQuery(NumberType.NEGATIVE, "<=", 1);

//            assertOpenRangeQuery(NumberType.ZERO, ">", 1);
//            assertOpenRangeQuery(NumberType.POSITIVE, ">", 0);
//            assertOpenRangeQuery(NumberType.NEGATIVE, ">", 2);

//            assertOpenRangeQuery(NumberType.ZERO, ">=", 2);
//            assertOpenRangeQuery(NumberType.POSITIVE, ">=", 1);
//            assertOpenRangeQuery(NumberType.NEGATIVE, ">=", 3);

//            assertOpenRangeQuery(NumberType.NEGATIVE, "=", 1);
//            assertOpenRangeQuery(NumberType.ZERO, "=", 1);
//            assertOpenRangeQuery(NumberType.POSITIVE, "=", 1);

//            assertRangeQuery(NumberType.NEGATIVE, null, true, true, 3);
//            assertRangeQuery(NumberType.NEGATIVE, null, false, true, 2);
//            assertRangeQuery(NumberType.POSITIVE, null, true, false, 1);
//            assertRangeQuery(NumberType.ZERO, null, false, false, 1);

//            assertRangeQuery(null, NumberType.POSITIVE, true, true, 3);
//            assertRangeQuery(null, NumberType.POSITIVE, true, false, 2);
//            assertRangeQuery(null, NumberType.NEGATIVE, false, true, 1);
//            assertRangeQuery(null, NumberType.ZERO, false, false, 1);

//            assertRangeQuery(null, null, false, false, 3);
//            assertRangeQuery(null, null, true, true, 3);

//        }

//        [Test]
//        public void testSimpleNumericQuery()
//        {
//            assertSimpleQuery(NumberType.ZERO, 1);
//            assertSimpleQuery(NumberType.POSITIVE, 1);
//            assertSimpleQuery(NumberType.NEGATIVE, 1);
//        }

//        public void assertRangeQuery(NumberType? lowerType, NumberType? upperType,
//            bool lowerInclusive, bool upperInclusive, int expectedDocCount)
//        {


//            StringBuilder sb = new StringBuilder();

//            String lowerInclusiveStr = (lowerInclusive ? "[" : "{");
//            String upperInclusiveStr = (upperInclusive ? "]" : "}");

//            foreach (FieldType.NumericType type in Enum.GetValues(typeof(FieldType.NumericType)))
//            {
//                String lowerStr = numberToString(getNumberType(lowerType, type.ToString()));
//                String upperStr = numberToString(getNumberType(upperType, type.ToString()));

//                sb.append("+").append(type.ToString()).append(':').append(lowerInclusiveStr)
//                  .append('"').append(lowerStr).append("\" TO \"").append(upperStr)
//                  .append('"').append(upperInclusiveStr).append(' ');
//            }

//            /*Number*/
//            object lowerDateNumber = getNumberType(lowerType, DATE_FIELD_NAME);
//            /*Number*/
//            object upperDateNumber = getNumberType(upperType, DATE_FIELD_NAME);
//            String lowerDateStr;
//            String upperDateStr;

//            if (lowerDateNumber != null)
//            {
//                lowerDateStr = ESCAPER.Escape(
//                    DATE_FORMAT.format(new DateTime(lowerDateNumber.longValue())), LOCALE,
//                    EscapeQuerySyntax.Type.STRING).toString();

//            }
//            else
//            {
//                lowerDateStr = "*";
//            }

//            if (upperDateNumber != null)
//            {
//                upperDateStr = ESCAPER.Escape(
//                      DATE_FORMAT.format(new DateTime(upperDateNumber.longValue())), LOCALE,
//                      EscapeQuerySyntax.Type.STRING).toString();

//            }
//            else
//            {
//                upperDateStr = "*";
//            }

//            sb.append("+").append(DATE_FIELD_NAME).append(':')
//                .append(lowerInclusiveStr).append('"').append(lowerDateStr).append(
//                    "\" TO \"").append(upperDateStr).append('"').append(
//                    upperInclusiveStr);


//            testQuery(sb.toString(), expectedDocCount);

//        }

//        public void assertOpenRangeQuery(NumberType boundType, String @operator, int expectedDocCount)
//        {

//            StringBuilder sb = new StringBuilder();

//            foreach (FieldType.NumericType type in Enum.GetValues(typeof(FieldType.NumericType)))
//            {
//                String boundStr = numberToString(getNumberType(boundType, type.ToString()));

//                sb.append("+").append(type.ToString()).append(@operator).append('"').append(boundStr).append('"').append(' ');
//            }

//            String boundDateStr = ESCAPER.Escape(
//                DATE_FORMAT.format(new Date(getNumberType(boundType, DATE_FIELD_NAME)
//                    .longValue())), LOCALE, EscapeQuerySyntax.Type.STRING).toString();

//            sb.append("+").append(DATE_FIELD_NAME).append(@operator).append('"').append(boundDateStr).append('"');


//            testQuery(sb.toString(), expectedDocCount);
//        }

//        public void assertSimpleQuery(NumberType numberType, int expectedDocCount)
//        {
//            StringBuilder sb = new StringBuilder();

//            foreach (FieldType.NumericType type in Enum.GetValues(typeof(FieldType.NumericType)))
//            {
//                String numberStr = numberToString(getNumberType(numberType, type.ToString()));
//                sb.append('+').append(type.ToString()).append(":\"").append(numberStr)
//                          .append("\" ");
//            }

//            String dateStr = ESCAPER.Escape(
//                DATE_FORMAT.format(new DateTime(getNumberType(numberType, DATE_FIELD_NAME)
//                    .longValue())), LOCALE, EscapeQuerySyntax.Type.STRING).toString();

//            sb.append('+').append(DATE_FIELD_NAME).append(":\"").append(dateStr)
//                    .append('"');


//            testQuery(sb.toString(), expectedDocCount);

//        }

//        [Test]
//        private void testQuery(String queryStr, int expectedDocCount)
//        {
//            if (VERBOSE) Console.WriteLine("Parsing: " + queryStr);

//            Query query = qp.Parse(queryStr, FIELD_NAME);
//            if (VERBOSE) Console.WriteLine("Querying: " + query);
//            TopDocs topDocs = searcher.Search(query, 1000);

//            String msg = "Query <" + queryStr + "> retrieved " + topDocs.TotalHits
//                + " document(s), " + expectedDocCount + " document(s) expected.";

//            if (VERBOSE) Console.WriteLine(msg);


//            assertEquals(msg, expectedDocCount, topDocs.TotalHits);
//        }

//        private static String numberToString(/*Number*/ object number)
//        {
//            return number == null ? "*" : ESCAPER.Escape(/*NUMBER_FORMAT.format(number)*/ string.Format(NUMBER_FORMAT, number).ToCharSequence(),
//                LOCALE, EscapeQuerySyntax.Type.STRING).toString();
//        }

//        private static /*Number*/ object normalizeNumber(/*Number*/ object number)
//        {
//            return decimal.Parse(string.Format(NUMBER_FORMAT, number));
//            //return NUMBER_FORMAT.parse(NUMBER_FORMAT.format(number));
//        }

//        [TestFixtureTearDown]
//        public static void afterClass()
//        {
//            searcher = null;
//            reader.Dispose();
//            reader = null;
//            directory.Dispose();
//            directory = null;
//            qp = null;
//        }
//    }
//}
