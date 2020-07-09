using System;
using System.Globalization;

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

    /// <summary>
    /// Provides support for converting dates to strings and vice-versa.
    /// The strings are structured so that lexicographic sorting orders
    /// them by date, which makes them suitable for use as field values
    /// and search terms.
    ///
    /// <para/>This class also helps you to limit the resolution of your dates. Do not
    /// save dates with a finer resolution than you really need, as then
    /// <see cref="Search.TermRangeQuery"/> and <see cref="Search.PrefixQuery"/> will require more memory and become slower.
    ///
    /// <para/>
    /// Another approach is <see cref="Util.NumericUtils"/>, which provides
    /// a sortable binary representation (prefix encoded) of numeric values, which
    /// date/time are.
    /// 
    /// For indexing a <see cref="DateTime"/>, just get the <see cref="DateTime.Ticks"/> and index
    /// this as a numeric value with <see cref="Int64Field"/> and use <see cref="Search.NumericRangeQuery{T}"/>
    /// to query it.
    /// </summary>
    public static class DateTools
    {
        private static readonly string YEAR_FORMAT = "yyyy";
        private static readonly string MONTH_FORMAT = "yyyyMM";
        private static readonly string DAY_FORMAT = "yyyyMMdd";
        private static readonly string HOUR_FORMAT = "yyyyMMddHH";
        private static readonly string MINUTE_FORMAT = "yyyyMMddHHmm";
        private static readonly string SECOND_FORMAT = "yyyyMMddHHmmss";
        private static readonly string MILLISECOND_FORMAT = "yyyyMMddHHmmssfff";

        // LUCENENET - not used
        //private static readonly System.Globalization.Calendar calInstance = new System.Globalization.GregorianCalendar();

        /// <summary>
        /// Converts a <see cref="DateTime"/> to a string suitable for indexing.
        /// </summary>
        /// <param name="date"> the date to be converted </param>
        /// <param name="resolution"> the desired resolution, see
        /// <see cref="Round(DateTime, DateTools.Resolution)"/> </param>
        /// <returns> a string in format <c>yyyyMMddHHmmssSSS</c> or shorter,
        /// depending on <paramref name="resolution"/>; using GMT as timezone  </returns>
        public static string DateToString(DateTime date, Resolution resolution)
        {
            return TimeToString(date.Ticks / TimeSpan.TicksPerMillisecond, resolution);
        }

        /// <summary>
        /// Converts a millisecond time to a string suitable for indexing.
        /// </summary>
        /// <param name="time"> the date expressed as milliseconds since January 1, 1970, 00:00:00 GMT (also known as the "epoch") </param>
        /// <param name="resolution"> the desired resolution, see
        /// <see cref="Round(long, DateTools.Resolution)"/> </param>
        /// <returns> a string in format <c>yyyyMMddHHmmssSSS</c> or shorter,
        /// depending on <paramref name="resolution"/>; using GMT as timezone </returns>
        public static string TimeToString(long time, Resolution resolution)
        {
            DateTime date = new DateTime(Round(time, resolution));

            if (resolution == Resolution.YEAR)
            {
                return date.ToString(YEAR_FORMAT, CultureInfo.InvariantCulture);
            }
            else if (resolution == Resolution.MONTH)
            {
                return date.ToString(MONTH_FORMAT, CultureInfo.InvariantCulture);
            }
            else if (resolution == Resolution.DAY)
            {
                return date.ToString(DAY_FORMAT, CultureInfo.InvariantCulture);
            }
            else if (resolution == Resolution.HOUR)
            {
                return date.ToString(HOUR_FORMAT, CultureInfo.InvariantCulture);
            }
            else if (resolution == Resolution.MINUTE)
            {
                return date.ToString(MINUTE_FORMAT, CultureInfo.InvariantCulture);
            }
            else if (resolution == Resolution.SECOND)
            {
                return date.ToString(SECOND_FORMAT, CultureInfo.InvariantCulture);
            }
            else if (resolution == Resolution.MILLISECOND)
            {
                return date.ToString(MILLISECOND_FORMAT, CultureInfo.InvariantCulture);
            }

            throw new ArgumentException("unknown resolution " + resolution);
        }

        /// <summary>
        /// Converts a string produced by <see cref="TimeToString(long, Resolution)"/> or
        /// <see cref="DateToString(DateTime, Resolution)"/> back to a time, represented as the
        /// number of milliseconds since January 1, 1970, 00:00:00 GMT (also known as the "epoch").
        /// </summary>
        /// <param name="dateString"> the date string to be converted </param>
        /// <returns> the number of milliseconds since January 1, 1970, 00:00:00 GMT (also known as the "epoch")</returns>
        /// <exception cref="FormatException"> if <paramref name="dateString"/> is not in the
        /// expected format </exception>
        public static long StringToTime(string dateString)
        {
            return StringToDate(dateString).Ticks;
        }

        /// <summary>
        /// Converts a string produced by <see cref="TimeToString(long, Resolution)"/> or
        /// <see cref="DateToString(DateTime, Resolution)"/> back to a time, represented as a
        /// <see cref="DateTime"/> object.
        /// </summary>
        /// <param name="dateString"> the date string to be converted </param>
        /// <returns> the parsed time as a <see cref="DateTime"/> object </returns>
        /// <exception cref="FormatException"> if <paramref name="dateString"/> is not in the
        /// expected format </exception>
        public static DateTime StringToDate(string dateString)
        {
            DateTime date;
            if (dateString.Length == 4)
            {
                date = new DateTime(Convert.ToInt16(dateString.Substring(0, 4), CultureInfo.InvariantCulture),
                    1, 1, 0, 0, 0, 0);
            }
            else if (dateString.Length == 6)
            {
                date = new DateTime(Convert.ToInt16(dateString.Substring(0, 4), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(4, 2), CultureInfo.InvariantCulture),
                    1, 0, 0, 0, 0);
            }
            else if (dateString.Length == 8)
            {
                date = new DateTime(Convert.ToInt16(dateString.Substring(0, 4), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(4, 2), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(6, 2), CultureInfo.InvariantCulture),
                    0, 0, 0, 0);
            }
            else if (dateString.Length == 10)
            {
                date = new DateTime(Convert.ToInt16(dateString.Substring(0, 4), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(4, 2), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(6, 2), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(8, 2), CultureInfo.InvariantCulture),
                    0, 0, 0);
            }
            else if (dateString.Length == 12)
            {
                date = new DateTime(Convert.ToInt16(dateString.Substring(0, 4), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(4, 2), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(6, 2), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(8, 2), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(10, 2), CultureInfo.InvariantCulture),
                    0, 0);
            }
            else if (dateString.Length == 14)
            {
                date = new DateTime(Convert.ToInt16(dateString.Substring(0, 4), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(4, 2), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(6, 2), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(8, 2), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(10, 2), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(12, 2), CultureInfo.InvariantCulture),
                    0);
            }
            else if (dateString.Length == 17)
            {
                date = new DateTime(Convert.ToInt16(dateString.Substring(0, 4), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(4, 2), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(6, 2), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(8, 2), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(10, 2), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(12, 2), CultureInfo.InvariantCulture),
                    Convert.ToInt16(dateString.Substring(14, 3), CultureInfo.InvariantCulture));
            }
            else
            {
                throw new FormatException("Input is not valid date string: " + dateString);
            }
            return date;
        }

        /// <summary>
        /// Limit a date's resolution. For example, the date <c>2004-09-21 13:50:11</c>
        /// will be changed to <c>2004-09-01 00:00:00</c> when using
        /// <see cref="Resolution.MONTH"/>.
        /// </summary>
        /// <param name="date"> the date to be rounded </param>
        /// <param name="resolution"> The desired resolution of the date to be returned </param>
        /// <returns> the date with all values more precise than <paramref name="resolution"/>
        /// set to 0 or 1 </returns>
        public static DateTime Round(DateTime date, Resolution resolution)
        {
            return new DateTime(Round(date.Ticks / TimeSpan.TicksPerMillisecond, resolution));
        }

        /// <summary>
        /// Limit a date's resolution. For example, the date <c>1095767411000</c>
        /// (which represents 2004-09-21 13:50:11) will be changed to
        /// <c>1093989600000</c> (2004-09-01 00:00:00) when using
        /// <see cref="Resolution.MONTH"/>.
        /// </summary>
        /// <param name="time"> the time to be rounded </param>
        /// <param name="resolution"> The desired resolution of the date to be returned </param>
        /// <returns> the date with all values more precise than <paramref name="resolution"/>
        /// set to 0 or 1, expressed as milliseconds since January 1, 1970, 00:00:00 GMT 
        /// (also known as the "epoch")</returns>
        public static long Round(long time, Resolution resolution)
        {
            DateTime dt = new DateTime(time * TimeSpan.TicksPerMillisecond);

            if (resolution == Resolution.YEAR)
            {
                dt = dt.AddMonths(1 - dt.Month);
                dt = dt.AddDays(1 - dt.Day);
                dt = dt.AddHours(0 - dt.Hour);
                dt = dt.AddMinutes(0 - dt.Minute);
                dt = dt.AddSeconds(0 - dt.Second);
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
            else if (resolution == Resolution.MONTH)
            {
                dt = dt.AddDays(1 - dt.Day);
                dt = dt.AddHours(0 - dt.Hour);
                dt = dt.AddMinutes(0 - dt.Minute);
                dt = dt.AddSeconds(0 - dt.Second);
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
            else if (resolution == Resolution.DAY)
            {
                dt = dt.AddHours(0 - dt.Hour);
                dt = dt.AddMinutes(0 - dt.Minute);
                dt = dt.AddSeconds(0 - dt.Second);
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
            else if (resolution == Resolution.HOUR)
            {
                dt = dt.AddMinutes(0 - dt.Minute);
                dt = dt.AddSeconds(0 - dt.Second);
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
            else if (resolution == Resolution.MINUTE)
            {
                dt = dt.AddSeconds(0 - dt.Second);
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
            else if (resolution == Resolution.SECOND)
            {
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
            else if (resolution == Resolution.MILLISECOND)
            {
                // don't cut off anything
            }
            else
            {
                throw new ArgumentException("unknown resolution " + resolution);
            }
            return dt.Ticks;
        }

        /// <summary>
        /// Specifies the time granularity. </summary>
        public enum Resolution
        {
            /// <summary>
            /// Limit a date's resolution to year granularity. </summary>
            YEAR = 4,

            /// <summary>
            /// Limit a date's resolution to month granularity. </summary>
            MONTH = 6,

            /// <summary>
            /// Limit a date's resolution to day granularity. </summary>
            DAY = 8,

            /// <summary>
            /// Limit a date's resolution to hour granularity. </summary>
            HOUR = 10,

            /// <summary>
            /// Limit a date's resolution to minute granularity. </summary>
            MINUTE = 12,

            /// <summary>
            /// Limit a date's resolution to second granularity. </summary>
            SECOND = 14,

            /// <summary>
            /// Limit a date's resolution to millisecond granularity. </summary>
            MILLISECOND = 17
        }
    }
}