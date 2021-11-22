using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

            DateTime dateToParse = J2N.Time.UnixEpoch.ToCalendar(culture).ToTimeZone(timeZone);

            long dateAsLong = (long)(dateToParse - J2N.Time.UnixEpoch.ToCalendar(culture)).TotalMilliseconds;

            string actual = formatter.Format(dateAsLong);

            Console.WriteLine("Output of formatter.Format():");
            Console.WriteLine($"\"{actual}\"");


            // Make sure time zone is correct in the string
            if (timeZone.IsDaylightSavingTime(dateToParse))
                Assert.IsTrue(Regex.IsMatch(actual, @"\+\s?0?8"));
            else
                Assert.IsTrue(Regex.IsMatch(actual, @"\+\s?0?7"));


            long parsedLong = Convert.ToInt64(formatter.Parse(actual));

            // Make sure round trip results in the same number
            Assert.AreEqual(dateAsLong, parsedLong);
        }
    }
}
