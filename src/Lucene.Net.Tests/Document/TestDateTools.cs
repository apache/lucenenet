using Lucene.Net.Attributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Globalization;
using TimeZoneConverter;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Documents
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

    [TestFixture]
    public class TestDateTools : LuceneTestCase
    {
        //public TestRule TestRules = RuleChain.outerRule(new SystemPropertiesRestoreRule());

        [Test]
        public virtual void TestStringToDate()
        {
            DateTime d = default;
            d = DateTools.StringToDate("2004");
            Assert.AreEqual("2004-01-01 00:00:00:000", IsoFormat(d));
            d = DateTools.StringToDate("20040705");
            Assert.AreEqual("2004-07-05 00:00:00:000", IsoFormat(d));
            d = DateTools.StringToDate("200407050910");
            Assert.AreEqual("2004-07-05 09:10:00:000", IsoFormat(d));
            d = DateTools.StringToDate("20040705091055990");
            Assert.AreEqual("2004-07-05 09:10:55:990", IsoFormat(d));

            try
            {
                d = DateTools.StringToDate("97"); // no date
                Assert.Fail();
            } // expected exception
#pragma warning disable 168
            catch (Exception e)
#pragma warning restore 168
            {
            }
            try
            {
                d = DateTools.StringToDate("200401011235009999"); // no date
                Assert.Fail();
            } // expected exception
#pragma warning disable 168
            catch (Exception e)
#pragma warning restore 168
            {
            }
            try
            {
                d = DateTools.StringToDate("aaaa"); // no date
                Assert.Fail();
            } // expected exception
#pragma warning disable 168
            catch (Exception e)
#pragma warning restore 168
            {
            }
        }

        [Test]
        public virtual void TestStringtoTime_UnixEpoch()
        {
            long time = DateTools.StringToTime("197001010000", NumericRepresentation.UNIX_TIME_MILLISECONDS);

            // we use default locale since LuceneTestCase randomizes it
            //Calendar cal = new GregorianCalendar(TimeZone.GetTimeZone("GMT"), Locale.Default);
            //cal.Clear();

            DateTime cal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc); //new GregorianCalendar().ToDateTime(1970, 1, 1, 0, 0, 0, 0); // hour, minute, second -  year=1970, month=january, day=1
            //cal.set(DateTime.MILLISECOND, 0);
            Assert.AreEqual(DateTools.TicksToUnixTimeMilliseconds(cal.Ticks), time);

            cal = new DateTime(1980, 2, 2, 11, 5, 0, 0, DateTimeKind.Utc); //new GregorianCalendar().ToDateTime(1980, 2, 2, 11, 5, 0, 0); // hour, minute, second -  year=1980, month=february, day=2
            //cal.set(DateTime.MILLISECOND, 0);
            time = DateTools.StringToTime("198002021105", NumericRepresentation.UNIX_TIME_MILLISECONDS);
            Assert.AreEqual(DateTools.TicksToUnixTimeMilliseconds(cal.Ticks), time);
        }

        [Test]
        public virtual void TestStringtoTime_Ticks()
        {
            long time = DateTools.StringToTime("197001010000", NumericRepresentation.TICKS);

            // we use default locale since LuceneTestCase randomizes it
            //Calendar cal = new GregorianCalendar(TimeZone.GetTimeZone("GMT"), Locale.Default);
            //cal.Clear();

            DateTime cal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc); //new GregorianCalendar().ToDateTime(1970, 1, 1, 0, 0, 0, 0); // hour, minute, second -  year=1970, month=january, day=1
            //cal.set(DateTime.MILLISECOND, 0);
            Assert.AreEqual(cal.Ticks, time);

            cal = new DateTime(1980, 2, 2, 11, 5, 0, 0, DateTimeKind.Utc); //new GregorianCalendar().ToDateTime(1980, 2, 2, 11, 5, 0, 0); // hour, minute, second -  year=1980, month=february, day=2
            //cal.set(DateTime.MILLISECOND, 0);
            time = DateTools.StringToTime("198002021105", NumericRepresentation.TICKS);
            Assert.AreEqual(cal.Ticks, time);
        }

        [Test]
        public virtual void TestStringtoTime_TicksAsMilliseconds()
        {
            long time = DateTools.StringToTime("197001010000", NumericRepresentation.TICKS_AS_MILLISECONDS);

            // we use default locale since LuceneTestCase randomizes it
            //Calendar cal = new GregorianCalendar(TimeZone.GetTimeZone("GMT"), Locale.Default);
            //cal.Clear();

            DateTime cal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc); //new GregorianCalendar().ToDateTime(1970, 1, 1, 0, 0, 0, 0); // hour, minute, second -  year=1970, month=january, day=1
            //cal.set(DateTime.MILLISECOND, 0);
            Assert.AreEqual(cal.Ticks / TimeSpan.TicksPerMillisecond, time);

            cal = new DateTime(1980, 2, 2, 11, 5, 0, 0, DateTimeKind.Utc); //new GregorianCalendar().ToDateTime(1980, 2, 2, 11, 5, 0, 0); // hour, minute, second -  year=1980, month=february, day=2
            //cal.set(DateTime.MILLISECOND, 0);
            time = DateTools.StringToTime("198002021105", NumericRepresentation.TICKS_AS_MILLISECONDS);
            Assert.AreEqual(cal.Ticks / TimeSpan.TicksPerMillisecond, time);
        }

        [Test]
        public virtual void TestDateAndTimetoString()
        {
            // we use default locale since LuceneTestCase randomizes it
            //Calendar cal = new GregorianCalendar(TimeZone.getTimeZone("GMT"), Locale.Default);
            //DateTime cal = new GregorianCalendar(GregorianCalendarTypes.Localized).ToDateTime(2004, 2, 3, 22, 8, 56, 333);
            DateTime cal = new DateTime(2004, 2, 3, 22, 8, 56, 333, DateTimeKind.Utc);
            

            /*cal.clear();
            cal = new DateTime(2004, 1, 3, 22, 8, 56); // hour, minute, second -  year=2004, month=february(!), day=3
            cal.set(DateTime.MILLISECOND, 333);*/

            string dateString = DateTools.DateToString(cal, DateResolution.YEAR);
            Assert.AreEqual("2004", dateString);
            Assert.AreEqual("2004-01-01 00:00:00:000", IsoFormat(DateTools.StringToDate(dateString)));

            dateString = DateTools.DateToString(cal, DateResolution.MONTH);
            Assert.AreEqual("200402", dateString);
            Assert.AreEqual("2004-02-01 00:00:00:000", IsoFormat(DateTools.StringToDate(dateString)));

            dateString = DateTools.DateToString(cal, DateResolution.DAY);
            Assert.AreEqual("20040203", dateString);
            Assert.AreEqual("2004-02-03 00:00:00:000", IsoFormat(DateTools.StringToDate(dateString)));

            dateString = DateTools.DateToString(cal, DateResolution.HOUR);
            Assert.AreEqual("2004020322", dateString);
            Assert.AreEqual("2004-02-03 22:00:00:000", IsoFormat(DateTools.StringToDate(dateString)));

            dateString = DateTools.DateToString(cal, DateResolution.MINUTE);
            Assert.AreEqual("200402032208", dateString);
            Assert.AreEqual("2004-02-03 22:08:00:000", IsoFormat(DateTools.StringToDate(dateString)));

            dateString = DateTools.DateToString(cal, DateResolution.SECOND);
            Assert.AreEqual("20040203220856", dateString);
            Assert.AreEqual("2004-02-03 22:08:56:000", IsoFormat(DateTools.StringToDate(dateString)));

            dateString = DateTools.DateToString(cal, DateResolution.MILLISECOND);
            Assert.AreEqual("20040203220856333", dateString);
            Assert.AreEqual("2004-02-03 22:08:56:333", IsoFormat(DateTools.StringToDate(dateString)));

            // date before 1970:
            //cal = new GregorianCalendar().ToDateTime(1961, 3, 5, 23, 9, 51, 444); // hour, minute, second -  year=1961, month=march(!), day=5
            //cal.set(DateTime.MILLISECOND, 444);
            cal = new DateTime(1961, 3, 5, 23, 9, 51, 444, DateTimeKind.Utc);
            dateString = DateTools.DateToString(cal, DateResolution.MILLISECOND);
            Assert.AreEqual("19610305230951444", dateString);
            Assert.AreEqual("1961-03-05 23:09:51:444", IsoFormat(DateTools.StringToDate(dateString)));

            dateString = DateTools.DateToString(cal, DateResolution.HOUR);
            Assert.AreEqual("1961030523", dateString);
            Assert.AreEqual("1961-03-05 23:00:00:000", IsoFormat(DateTools.StringToDate(dateString)));

            // timeToString:

            // ticks:

            //cal = new GregorianCalendar().ToDateTime(1970, 1, 1, 0, 0, 0, 0); // hour, minute, second -  year=1970, month=january, day=1
            //cal.set(DateTime.MILLISECOND, 0);
            cal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateString = DateTools.TimeToString(cal.Ticks, DateResolution.MILLISECOND, NumericRepresentation.TICKS);
            Assert.AreEqual("19700101000000000", dateString);

            cal = new GregorianCalendar().ToDateTime(1970, 1, 1, 1, 2, 3, 0); // hour, minute, second -  year=1970, month=january, day=1
            //cal.set(DateTime.MILLISECOND, 0);
            dateString = DateTools.TimeToString(cal.Ticks, DateResolution.MILLISECOND, NumericRepresentation.TICKS);
            Assert.AreEqual("19700101010203000", dateString);

            // unix epoch:

            cal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateString = DateTools.TimeToString(DateTools.TicksToUnixTimeMilliseconds(cal.Ticks), DateResolution.MILLISECOND, NumericRepresentation.UNIX_TIME_MILLISECONDS);
            Assert.AreEqual("19700101000000000", dateString);

            cal = new GregorianCalendar().ToDateTime(1970, 1, 1, 1, 2, 3, 0); // hour, minute, second -  year=1970, month=january, day=1
            //cal.set(DateTime.MILLISECOND, 0);
            dateString = DateTools.TimeToString(DateTools.TicksToUnixTimeMilliseconds(cal.Ticks), DateResolution.MILLISECOND, NumericRepresentation.UNIX_TIME_MILLISECONDS);
            Assert.AreEqual("19700101010203000", dateString);

            // ticks as ms:

            cal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateString = DateTools.TimeToString(cal.Ticks / TimeSpan.TicksPerMillisecond, DateResolution.MILLISECOND, NumericRepresentation.TICKS_AS_MILLISECONDS);
            Assert.AreEqual("19700101000000000", dateString);

            cal = new GregorianCalendar().ToDateTime(1970, 1, 1, 1, 2, 3, 0); // hour, minute, second -  year=1970, month=january, day=1
            //cal.set(DateTime.MILLISECOND, 0);
            dateString = DateTools.TimeToString(cal.Ticks / TimeSpan.TicksPerMillisecond, DateResolution.MILLISECOND, NumericRepresentation.TICKS_AS_MILLISECONDS);
            Assert.AreEqual("19700101010203000", dateString);
        }

        [Test]
        public virtual void TestRound()
        {
            // we use default locale since LuceneTestCase randomizes it
            //Calendar cal = new GregorianCalendar(TimeZone.getTimeZone("GMT"), Locale.Default);
            //cal.clear();
            //DateTime cal = new GregorianCalendar().ToDateTime(2004, 2, 3, 22, 8, 56, 333); // hour, minute, second -  year=2004, month=february(!), day=3
            //cal.set(DateTime.MILLISECOND, 333);
            DateTime date = new DateTime(2004, 2, 3, 22, 8, 56, 333, DateTimeKind.Utc);
            Assert.AreEqual("2004-02-03 22:08:56:333", IsoFormat(date));

            DateTime dateYear = DateTools.Round(date, DateResolution.YEAR);
            Assert.AreEqual("2004-01-01 00:00:00:000", IsoFormat(dateYear));

            DateTime dateMonth = DateTools.Round(date, DateResolution.MONTH);
            Assert.AreEqual("2004-02-01 00:00:00:000", IsoFormat(dateMonth));

            DateTime dateDay = DateTools.Round(date, DateResolution.DAY);
            Assert.AreEqual("2004-02-03 00:00:00:000", IsoFormat(dateDay));

            DateTime dateHour = DateTools.Round(date, DateResolution.HOUR);
            Assert.AreEqual("2004-02-03 22:00:00:000", IsoFormat(dateHour));

            DateTime dateMinute = DateTools.Round(date, DateResolution.MINUTE);
            Assert.AreEqual("2004-02-03 22:08:00:000", IsoFormat(dateMinute));

            DateTime dateSecond = DateTools.Round(date, DateResolution.SECOND);
            Assert.AreEqual("2004-02-03 22:08:56:000", IsoFormat(dateSecond));

            DateTime dateMillisecond = DateTools.Round(date, DateResolution.MILLISECOND);
            Assert.AreEqual("2004-02-03 22:08:56:333", IsoFormat(dateMillisecond));

            // long parameter:

            // ticks:

            long dateYearLong = DateTools.Round(date.Ticks, DateResolution.YEAR, NumericRepresentation.TICKS, NumericRepresentation.TICKS);
            Assert.AreEqual("2004-01-01 00:00:00:000", IsoFormat(new DateTime(dateYearLong)));

            long dateMillisecondLong = DateTools.Round(date.Ticks, DateResolution.MILLISECOND, NumericRepresentation.TICKS, NumericRepresentation.TICKS);
            Assert.AreEqual("2004-02-03 22:08:56:333", IsoFormat(new DateTime(dateMillisecondLong)));

            // unix epoch:

            dateYearLong = DateTools.Round(DateTools.TicksToUnixTimeMilliseconds(date.Ticks), DateResolution.YEAR, NumericRepresentation.UNIX_TIME_MILLISECONDS, NumericRepresentation.TICKS);
            Assert.AreEqual("2004-01-01 00:00:00:000", IsoFormat(new DateTime(dateYearLong)));

            dateMillisecondLong = DateTools.Round(DateTools.TicksToUnixTimeMilliseconds(date.Ticks), DateResolution.MILLISECOND, NumericRepresentation.UNIX_TIME_MILLISECONDS, NumericRepresentation.TICKS);
            Assert.AreEqual("2004-02-03 22:08:56:333", IsoFormat(new DateTime(dateMillisecondLong)));

            // ticks as ms:

            dateYearLong = DateTools.Round(date.Ticks / TimeSpan.TicksPerMillisecond, DateResolution.YEAR, NumericRepresentation.TICKS_AS_MILLISECONDS, NumericRepresentation.TICKS);
            Assert.AreEqual("2004-01-01 00:00:00:000", IsoFormat(new DateTime(dateYearLong)));

            dateMillisecondLong = DateTools.Round(date.Ticks / TimeSpan.TicksPerMillisecond, DateResolution.MILLISECOND, NumericRepresentation.TICKS_AS_MILLISECONDS, NumericRepresentation.TICKS);
            Assert.AreEqual("2004-02-03 22:08:56:333", IsoFormat(new DateTime(dateMillisecondLong)));
        }

        private string IsoFormat(DateTime date)
        {
            /*SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss:SSS", Locale.ROOT);
            sdf.TimeZone = TimeZone.getTimeZone("GMT");
            return sdf.Format(date);*/
            return date.ToString("yyyy-MM-dd HH:mm:ss:fff", CultureInfo.InvariantCulture);
        }

        [Test]
        public virtual void TestDateToolsUTC_UnixEpoch()
        {
            // Sun, 30 Oct 2005 00:00:00 +0000 -- the last second of 2005's DST in Europe/London
            //long time = 1130630400;
            DateTime time1 = new DateTime(2005, 10, 30, 0, 0, 0, DateTimeKind.Utc);
            DateTime time2 = time1.AddHours(1);

            {
                TimeZoneInfo timeZone = TZConvert.GetTimeZoneInfo("Europe/London");
                string d1 = DateTools.DateToString(time1, timeZone, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, timeZone, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1), DateTools.TicksToUnixTimeMilliseconds(time1.Ticks), "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2), DateTools.TicksToUnixTimeMilliseconds(time2.Ticks), "later");
            }

            {
                string d1 = DateTools.DateToString(time1, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1), DateTools.TicksToUnixTimeMilliseconds(time1.Ticks), "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2), DateTools.TicksToUnixTimeMilliseconds(time2.Ticks), "later");
            }

            time1 = new DateTime(2005, 10, 30, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
            time2 = time1.AddHours(1);

            {
                string d1 = DateTools.DateToString(time1, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1), DateTools.TicksToUnixTimeMilliseconds(time1.ToUniversalTime().Ticks), "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2), DateTools.TicksToUnixTimeMilliseconds(time2.ToUniversalTime().Ticks), "later");
            }

            time1 = new DateTime(2005, 10, 30, 0, 0, 0, DateTimeKind.Unspecified);
            time2 = time1.AddHours(1);

            {
                string d1 = DateTools.DateToString(time1, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1), DateTools.TicksToUnixTimeMilliseconds(time1.ToUniversalTime().Ticks), "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2), DateTools.TicksToUnixTimeMilliseconds(time2.ToUniversalTime().Ticks), "later");
            }
        }

        [Test]
        public virtual void TestDateToolsUTC_Ticks()
        {
            // Sun, 30 Oct 2005 00:00:00 +0000 -- the last second of 2005's DST in Europe/London
            //long time = 1130630400;
            DateTime time1 = new DateTime(2005, 10, 30, 0, 0, 0, DateTimeKind.Utc);
            DateTime time2 = time1.AddHours(1);

            {
                TimeZoneInfo timeZone = TZConvert.GetTimeZoneInfo("Europe/London");
                string d1 = DateTools.DateToString(time1, timeZone, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, timeZone, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.TICKS), time1.Ticks, "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.TICKS), time2.Ticks, "later");
            }

            {
                string d1 = DateTools.DateToString(time1, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.TICKS), time1.Ticks, "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.TICKS), time2.Ticks, "later");
            }

            time1 = new DateTime(2005, 10, 30, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
            time2 = time1.AddHours(1);

            {
                string d1 = DateTools.DateToString(time1, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.TICKS), time1.ToUniversalTime().Ticks, "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.TICKS), time2.ToUniversalTime().Ticks, "later");
            }

            time1 = new DateTime(2005, 10, 30, 0, 0, 0, DateTimeKind.Unspecified);
            time2 = time1.AddHours(1);

            {
                string d1 = DateTools.DateToString(time1, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.TICKS), time1.ToUniversalTime().Ticks, "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.TICKS), time2.ToUniversalTime().Ticks, "later");
            }
        }

        [Test]
        public virtual void TestDateToolsUTC_TicksAsMilliseconds()
        {
            // Sun, 30 Oct 2005 00:00:00 +0000 -- the last second of 2005's DST in Europe/London
            //long time = 1130630400;
            DateTime time1 = new DateTime(2005, 10, 30, 0, 0, 0, DateTimeKind.Utc);
            DateTime time2 = time1.AddHours(1);

            {
                TimeZoneInfo timeZone = TZConvert.GetTimeZoneInfo("Europe/London");
                string d1 = DateTools.DateToString(time1, timeZone, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, timeZone, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.TICKS_AS_MILLISECONDS), time1.Ticks / TimeSpan.TicksPerMillisecond, "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.TICKS_AS_MILLISECONDS), time2.Ticks / TimeSpan.TicksPerMillisecond, "later");
            }

            {
                string d1 = DateTools.DateToString(time1, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.TICKS_AS_MILLISECONDS), time1.Ticks / TimeSpan.TicksPerMillisecond, "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.TICKS_AS_MILLISECONDS), time2.Ticks / TimeSpan.TicksPerMillisecond, "later");
            }

            time1 = new DateTime(2005, 10, 30, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
            time2 = time1.AddHours(1);

            {
                string d1 = DateTools.DateToString(time1, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.TICKS_AS_MILLISECONDS), time1.ToUniversalTime().Ticks / TimeSpan.TicksPerMillisecond, "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.TICKS_AS_MILLISECONDS), time2.ToUniversalTime().Ticks / TimeSpan.TicksPerMillisecond, "later");
            }

            time1 = new DateTime(2005, 10, 30, 0, 0, 0, DateTimeKind.Unspecified);
            time2 = time1.AddHours(1);

            {
                string d1 = DateTools.DateToString(time1, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.TICKS_AS_MILLISECONDS), time1.ToUniversalTime().Ticks / TimeSpan.TicksPerMillisecond, "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.TICKS_AS_MILLISECONDS), time2.ToUniversalTime().Ticks / TimeSpan.TicksPerMillisecond, "later");
            }
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestDateToolsUTC_DateTimeOffset_UnixEpoch()
        {
            // Sun, 30 Oct 2005 00:00:00 +0000 -- the last second of 2005's DST in Europe/London
            //long time = 1130630400;
            DateTimeOffset time1 = new DateTimeOffset(2005, 10, 30, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset time2 = time1.AddHours(1);

            {
                TimeZoneInfo timeZone = TZConvert.GetTimeZoneInfo("Europe/London");
                DateTimeOffset tempAux = TimeZoneInfo.ConvertTime(time1, timeZone);
                string d1 = DateTools.DateToString(tempAux, DateResolution.MINUTE);
                DateTimeOffset tempAux2 = TimeZoneInfo.ConvertTime(time2, timeZone);
                string d2 = DateTools.DateToString(tempAux2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.UNIX_TIME_MILLISECONDS), DateTools.TicksToUnixTimeMilliseconds(time1.Ticks), "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.UNIX_TIME_MILLISECONDS), DateTools.TicksToUnixTimeMilliseconds(time2.Ticks), "later");
            }

            {
                string d1 = DateTools.DateToString(time1, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.UNIX_TIME_MILLISECONDS), DateTools.TicksToUnixTimeMilliseconds(time1.Ticks), "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.UNIX_TIME_MILLISECONDS), DateTools.TicksToUnixTimeMilliseconds(time2.Ticks), "later");
            }

            time1 = new DateTimeOffset(2005, 10, 30, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
            time2 = time1.AddHours(1);

            {
                string d1 = DateTools.DateToString(time1, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.UNIX_TIME_MILLISECONDS), DateTools.TicksToUnixTimeMilliseconds(time1.ToUniversalTime().Ticks), "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.UNIX_TIME_MILLISECONDS), DateTools.TicksToUnixTimeMilliseconds(time2.ToUniversalTime().Ticks), "later");
            }
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestDateToolsUTC_DateTimeOffset_Ticks()
        {
            // Sun, 30 Oct 2005 00:00:00 +0000 -- the last second of 2005's DST in Europe/London
            //long time = 1130630400;
            DateTimeOffset time1 = new DateTimeOffset(2005, 10, 30, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset time2 = time1.AddHours(1);

            {
                TimeZoneInfo timeZone = TZConvert.GetTimeZoneInfo("Europe/London");
                DateTimeOffset tempAux = TimeZoneInfo.ConvertTime(time1, timeZone);
                string d1 = DateTools.DateToString(tempAux, DateResolution.MINUTE);
                DateTimeOffset tempAux2 = TimeZoneInfo.ConvertTime(time2, timeZone);
                string d2 = DateTools.DateToString(tempAux2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.TICKS), time1.Ticks, "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.TICKS), time2.Ticks, "later");
            }

            {
                string d1 = DateTools.DateToString(time1, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.TICKS), time1.Ticks, "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.TICKS), time2.Ticks, "later");
            }

            time1 = new DateTimeOffset(2005, 10, 30, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
            time2 = time1.AddHours(1);

            {
                string d1 = DateTools.DateToString(time1, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.TICKS), time1.ToUniversalTime().Ticks, "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.TICKS), time2.ToUniversalTime().Ticks, "later");
            }
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestDateToolsUTC_DateTimeOffset_TicksAsMilliseconds()
        {
            // Sun, 30 Oct 2005 00:00:00 +0000 -- the last second of 2005's DST in Europe/London
            //long time = 1130630400;
            DateTimeOffset time1 = new DateTimeOffset(2005, 10, 30, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset time2 = time1.AddHours(1);

            {
                TimeZoneInfo timeZone = TZConvert.GetTimeZoneInfo("Europe/London");
                DateTimeOffset tempAux = TimeZoneInfo.ConvertTime(time1, timeZone);
                string d1 = DateTools.DateToString(tempAux, DateResolution.MINUTE);
                DateTimeOffset tempAux2 = TimeZoneInfo.ConvertTime(time2, timeZone);
                string d2 = DateTools.DateToString(tempAux2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.TICKS_AS_MILLISECONDS), time1.Ticks / TimeSpan.TicksPerMillisecond, "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.TICKS_AS_MILLISECONDS), time2.Ticks / TimeSpan.TicksPerMillisecond, "later");
            }

            {
                string d1 = DateTools.DateToString(time1, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.TICKS_AS_MILLISECONDS), time1.Ticks / TimeSpan.TicksPerMillisecond, "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.TICKS_AS_MILLISECONDS), time2.Ticks / TimeSpan.TicksPerMillisecond, "later");
            }

            time1 = new DateTimeOffset(2005, 10, 30, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
            time2 = time1.AddHours(1);

            {
                string d1 = DateTools.DateToString(time1, DateResolution.MINUTE);
                string d2 = DateTools.DateToString(time2, DateResolution.MINUTE);
                Assert.IsFalse(d1.Equals(d2, StringComparison.Ordinal), "different times");
                Assert.AreEqual(DateTools.StringToTime(d1, NumericRepresentation.TICKS_AS_MILLISECONDS), time1.ToUniversalTime().Ticks / TimeSpan.TicksPerMillisecond, "midnight");
                Assert.AreEqual(DateTools.StringToTime(d2, NumericRepresentation.TICKS_AS_MILLISECONDS), time2.ToUniversalTime().Ticks / TimeSpan.TicksPerMillisecond, "later");
            }
        }

        [Test]
        [LuceneNetSpecific]
        public void TestDocumentationComments()
        {
            long unixEpochDate = 1095774611000; // javadoc appears to be wrong - this is actually what changes the GMT time as below
            long ticks = 632313714110000000; // 2004-09-21 13:50:11
            Assert.AreEqual(ticks, DateTools.UnixTimeMillisecondsToTicks(unixEpochDate));

            long ticksOut = DateTools.Round(ticks, DateResolution.MONTH, NumericRepresentation.TICKS, NumericRepresentation.TICKS);
            long expected = 1093996800000; // javadoc appears to be wrong - this is actually what the above is converted to
            long actual = DateTools.TicksToUnixTimeMilliseconds(ticksOut);
            Assert.AreEqual(expected, actual);

            long unixEpochDateOut = DateTools.Round(unixEpochDate, DateResolution.MONTH, NumericRepresentation.UNIX_TIME_MILLISECONDS, NumericRepresentation.UNIX_TIME_MILLISECONDS);
            Assert.AreEqual(expected, unixEpochDateOut);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestLuceneCompatibility()
        {
            long unixDate = 1075846136333L;

            string convertedDate = DateTools.TimeToString(unixDate, DateResolution.MILLISECOND, NumericRepresentation.UNIX_TIME_MILLISECONDS);
            string expectedDate = "20040203220856333";
            Assert.AreEqual(expectedDate, convertedDate);
            Assert.AreEqual("2004-02-03 22:08:56:333", IsoFormat(DateTools.StringToDate(convertedDate)));
            Assert.AreEqual(unixDate, DateTools.StringToTime(convertedDate));
        }

        [Test]
        [LuceneNetSpecific] // GH-772
        public void DateToString_MinDate_ShouldNotThrowArgumentOutOfRangeException()
        {
            var date = new DateTime();
            var value = DateTools.DateToString(date, DateResolution.DAY);
            Assert.AreEqual("00010101", value);
        }
    }
}