using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using J2N;
using Lucene.Net.Attributes;
using Lucene.Net.Util;
using TimeZoneConverter;
using NUnit.Framework;
using Console = Lucene.Net.Util.SystemConsole;


namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
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

    [LuceneNetSpecific]
    public class TestNumberDateFormat : LuceneTestCase
    {
        [Test]
        [LuceneNetSpecific]
        public void TestTimeZone_PacificTime()
        {
            TimeZoneInfo timeZone = TZConvert.GetTimeZoneInfo("Pacific Standard Time");

            CultureInfo culture = new CultureInfo("en-US");

            var formatter = new NumberDateFormat(DateFormat.LONG, DateFormat.LONG, culture)
            {
                TimeZone = timeZone
            };

            // Convert from Unix epoch to time zone.
            DateTime dateToParse = TimeZoneInfo.ConvertTimeFromUtc(J2N.Time.UnixEpoch, timeZone);

            // Get the difference since the Unix epoch in milliseconds.
            long dateAsLong = dateToParse.GetMillisecondsSinceUnixEpoch();

            string actual = formatter.Format(dateAsLong);

            Console.WriteLine("Output of formatter.Format():");
            Console.WriteLine($"\"{actual}\"");


            // Make sure time zone is correct in the string for PST, including DST.
            if (timeZone.IsDaylightSavingTime(dateToParse))
                Assert.IsTrue(Regex.IsMatch(actual, @"\-\s?0?7"));
            else
                Assert.IsTrue(Regex.IsMatch(actual, @"\-\s?0?8"));

            // Convert the parsed result back to a long
            long parsedLong = Convert.ToInt64(formatter.Parse(actual));

            // Make sure round trip results in the same number
            Assert.AreEqual(dateAsLong, parsedLong);
        }

        // Verify that we can round-trip and convert to the time zone that is set after the parse.
        [Test]
        [LuceneNetSpecific]
        public void TestTimeZone_ShortTimeFormat()
        {
            TimeZoneInfo timeZone = TZConvert.GetTimeZoneInfo("Pacific Standard Time");

            CultureInfo culture = new CultureInfo("en-US");

            var formatter = new NumberDateFormat(DateFormat.LONG, DateFormat.SHORT, culture) // Short time = no time zone info
            {
                TimeZone = timeZone
            };

            // Convert from Unix epoch to time zone.
            DateTime dateToParse = TimeZoneInfo.ConvertTimeFromUtc(J2N.Time.UnixEpoch, timeZone);

            // Get the difference since the Unix epoch in milliseconds.
            long dateAsLong = dateToParse.GetMillisecondsSinceUnixEpoch();

            string actual = formatter.Format(dateAsLong);

            Console.WriteLine("Output of formatter.Format():");
            Console.WriteLine($"\"{actual}\"");


            // Convert the parsed result back to a long
            long parsedLong = Convert.ToInt64(formatter.Parse(actual));

            // Make sure round trip results in the same number
            Assert.AreEqual(dateAsLong, parsedLong);
        }

        // Verify that we can round-trip and convert to the time zone that is set after the parse
        // in a time zone with different rules prior to 1903. See: https://github.com/dotnet/runtime/issues/62247
        [Test]
        [LuceneNetSpecific]
        public void TestTimeZone_ShortTimeFormat_CentralAfricaTime()
        {
            TimeZoneInfo timeZone = TZConvert.GetTimeZoneInfo("Africa/Gaborone");

            CultureInfo culture = new CultureInfo("en-US");

            var formatter = new NumberDateFormat(DateFormat.LONG, DateFormat.MEDIUM, culture) // Medium time = no time zone info, but contains seconds
            {
                TimeZone = timeZone
            };

            const long Oct_31_1871_Ticks = 590376582130000000L; // Oct 31, 1871 05:30:13 UTC

            DateTime dateToParse = TimeZoneInfo.ConvertTime(new DateTime(Oct_31_1871_Ticks, DateTimeKind.Utc), timeZone);

            // Get the difference since the Unix epoch in milliseconds.
            long dateAsLong = dateToParse.GetMillisecondsSinceUnixEpoch();

            string actual = formatter.Format(dateAsLong);

            Console.WriteLine("Output of formatter.Format():");
            Console.WriteLine($"\"{actual}\"");

            // Convert the parsed result back to a long
            long parsedLong = Convert.ToInt64(formatter.Parse(actual));

            // Make sure round trip results in the same number
            Assert.AreEqual(dateAsLong, parsedLong);
        }
    }
}
