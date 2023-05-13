using J2N.Text;
using Lucene.Net.Support;
using System;
using System.Globalization;
#nullable enable

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
    /// For indexing a <see cref="DateTime"/>, just get the <see cref="UnixTimeMillisecondsToTicks(long)"/> from <see cref="DateTime.Ticks"/> and index
    /// this as a numeric value with <see cref="Int64Field"/> and use <see cref="Search.NumericRangeQuery{T}"/>
    /// to query it.
    /// </summary>
    // LUCENENET: This class was refactored significantly to be usable on the .NET platform, but still allows
    // all of the same features as Java and prior versions of Lucene.NET
    public static class DateTools
    {
        /// <summary>
        /// Returns the date format string for the specified <paramref name="resolution"/>
        /// or <c>null</c> if the resolution is invalid.
        /// </summary>
        private static string? ToDateFormat(DateResolution resolution) => resolution switch
        {
            DateResolution.YEAR =>        "yyyy",
            DateResolution.MONTH =>       "yyyyMM",
            DateResolution.DAY =>         "yyyyMMdd",
            DateResolution.HOUR =>        "yyyyMMddHH",
            DateResolution.MINUTE =>      "yyyyMMddHHmm",
            DateResolution.SECOND =>      "yyyyMMddHHmmss",
            DateResolution.MILLISECOND => "yyyyMMddHHmmssfff",
            _ => null, // Invalid option
        };

        /// <summary>
        /// Converts a <see cref="DateTime"/> to a string suitable for indexing using the specified
        /// <paramref name="resolution"/>.
        /// <para/>
        /// The <paramref name="date"/> is converted according to its <see cref="DateTime.Kind"/> property
        /// to the Universal Coordinated Time (UTC) prior to rounding to the the specified
        /// <paramref name="resolution"/>. If <see cref="DateTime.Kind"/> is <see cref="DateTimeKind.Unspecified"/>,
        /// <see cref="DateTimeKind.Local"/> is assumed.
        /// </summary>
        /// <param name="date">The date to be converted.</param>
        /// <param name="resolution">The desired resolution, see
        /// <see cref="Round(DateTime, DateResolution)"/>.</param>
        /// <returns>An invariant string in format <c>yyyyMMddHHmmssSSS</c> or shorter,
        /// depending on <paramref name="resolution"/>; using UTC as the timezone.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="resolution"/> is not defined in the <see cref="DateResolution"/> enum.
        /// </exception>
        public static string DateToString(DateTime date, DateResolution resolution)
        {
            return DateToStringInternal(date, timeZone: null, resolution);
        }

        /// <summary>
        /// Converts a <see cref="DateTime"/> to a string suitable for indexing using the specified <paramref name="timeZone"/>
        /// and <paramref name="resolution"/>.
        /// <para/>
        /// The <paramref name="date"/> is converted from the specified <paramref name="timeZone"/> to Universal Coordinated Time
        /// (UTC) prior to rounding to the the specified <paramref name="resolution"/>.
        /// </summary>
        /// <param name="date">The date to be converted.</param>
        /// <param name="timeZone">The time zone of the specified <paramref name="date"/>.</param>
        /// <param name="resolution">The desired resolution, see
        /// <see cref="Round(DateTime, DateResolution)"/>.</param>
        /// <returns>An invariant string in format <c>yyyyMMddHHmmssSSS</c> or shorter,
        /// depending on <paramref name="resolution"/>; using UTC as the timezone.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="timeZone"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="resolution"/> is not defined in the <see cref="DateResolution"/> enum.
        /// </exception>
        public static string DateToString(DateTime date, TimeZoneInfo timeZone, DateResolution resolution)
        {
            if (timeZone is null)
                throw new ArgumentNullException(nameof(timeZone));

            return DateToStringInternal(date, timeZone, resolution);
        }

        private static string DateToStringInternal(DateTime date, TimeZoneInfo? timeZone, DateResolution resolution)
        {
            string? format = ToDateFormat(resolution);
            if (format is null)
                throw new ArgumentException($"Unknown resolution {resolution}.");

            DateTimeOffset timeZoneAdjusted = new DateTimeOffset(date.ToUniversalTime(), TimeSpan.Zero);
            if (timeZone is not null && !TimeZoneInfo.Utc.Equals(timeZone))
            {
                timeZoneAdjusted = TimeZoneInfo.ConvertTime(timeZoneAdjusted, timeZone);
            }

            DateTime d = Round(timeZoneAdjusted.UtcDateTime, resolution);
            return d.ToString(format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a <see cref="DateTimeOffset"/> to a string suitable for indexing using the specified 
        /// <paramref name="resolution"/>.
        /// <para/>
        /// The <paramref name="date"/> is converted using its <see cref="DateTimeOffset.UtcDateTime"/> property.
        /// </summary>
        /// <param name="date">The date to be converted.</param>
        /// <param name="resolution">The desired resolution, see <see cref="Round(DateTime, DateResolution)"/>.</param>
        /// <returns>An invariant string in format <c>yyyyMMddHHmmssSSS</c> or shorter,
        /// depending on <paramref name="resolution"/>; using UTC as the timezone.</returns>
        public static string DateToString(DateTimeOffset date, DateResolution resolution)
        {
            string? format = ToDateFormat(resolution);
            if (format is null)
                throw new ArgumentException($"Unknown resolution {resolution}.");
            DateTime d = Round(date.UtcDateTime, resolution);
            return d.ToString(format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts from a numeric representation of a time to a string suitable for indexing.
        /// <para/>
        /// <b>NOTE:</b> For compatibility with Lucene.NET 3.0.3 and Lucene.NET 4.8.0-beta00001 through 4.8.0-beta00015
        /// specify <paramref name="inputRepresentation"/> as <see cref="NumericRepresentation.TICKS_AS_MILLISECONDS"/>.
        /// </summary>
        /// <param name="time">The ticks that represent the date to be converted.</param>
        /// <param name="resolution">The desired resolution, see
        /// <see cref="Round(long, DateResolution, NumericRepresentation, NumericRepresentation)"/>.</param>
        /// <param name="inputRepresentation">The numeric representation of <paramref name="time"/>.</param>
        /// <returns>An invariant string in format <c>yyyyMMddHHmmssSSS</c> or shorter,
        /// depending on <paramref name="resolution"/>; using GMT as timezone.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="inputRepresentation"/> is not defined in the <see cref="NumericRepresentation"/> enum.
        /// </exception>
        public static string TimeToString(long time, DateResolution resolution,
            NumericRepresentation inputRepresentation = NumericRepresentation.UNIX_TIME_MILLISECONDS)
        {
            string? format = ToDateFormat(resolution);
            if (format is null)
                throw new ArgumentException($"Unknown resolution {resolution}.");
            DateTime date = new DateTime(Round(time, resolution, inputRepresentation, NumericRepresentation.TICKS), DateTimeKind.Utc);
            return date.ToString(format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a string produced by <see cref="TimeToString(long, DateResolution, NumericRepresentation)"/> or
        /// <see cref="DateToString(DateTime, DateResolution)"/> back to a time, represented as a <see cref="long"/>.
        /// <para/>
        /// <b>NOTE:</b> For compatibility with Lucene.NET 3.0.3 and Lucene.NET 4.8.0-beta00001 through 4.8.0-beta00015
        /// specify <paramref name="outputRepresentation"/> as <see cref="NumericRepresentation.TICKS"/>.
        /// </summary>
        /// <param name="dateString"> The date string to be converted. </param>
        /// <param name="outputRepresentation">The numeric representation of the return value.</param>
        /// <returns>A numeric representation of <paramref name="dateString"/> represented as specified by
        /// <paramref name="outputRepresentation"/>.</returns>
        /// <exception cref="ParseException"><paramref name="dateString"/> is not in the expected format.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dateString"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="outputRepresentation"/> is not defined in the <see cref="NumericRepresentation"/> enum.
        /// </exception>
        public static long StringToTime(string dateString, NumericRepresentation outputRepresentation = NumericRepresentation.UNIX_TIME_MILLISECONDS)
        {
            long ticks = StringToDate(dateString).Ticks;
            return outputRepresentation switch
            {
                NumericRepresentation.UNIX_TIME_MILLISECONDS => TicksToUnixTimeMilliseconds(ticks),
                NumericRepresentation.TICKS => ticks,
                NumericRepresentation.TICKS_AS_MILLISECONDS => ticks / TimeSpan.TicksPerMillisecond,
                _ => throw new ArgumentException($"'{outputRepresentation}' is not a valid {nameof(outputRepresentation)}.")
            };
        }

        /// <summary>
        /// Converts a string produced by <see cref="TimeToString(long, DateResolution, NumericRepresentation)"/> or
        /// <see cref="DateToString(DateTime, DateResolution)"/> back to a time, represented as a
        /// <see cref="DateTime"/> object.
        /// </summary>
        /// <param name="dateString"> the date string to be converted </param>
        /// <returns> the parsed time as a <see cref="DateTime"/> object </returns>
        /// <exception cref="ParseException"> if <paramref name="dateString"/> is not in the
        /// expected format </exception>
        /// <exception cref="ArgumentNullException"><paramref name="dateString"/> is <c>null</c>.</exception>
        public static DateTime StringToDate(string dateString)
        {
            if (dateString is null)
                throw new ArgumentNullException(nameof(dateString));

            string? format = ToDateFormat((DateResolution)dateString.Length);
            if (format is null || !DateTimeOffset.TryParseExact(dateString, format, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal, out DateTimeOffset dateOffset))
                throw new ParseException($"Input is not valid date string: '{dateString}'.", 0);

            return dateOffset.DateTime;
        }

        /// <summary>
        /// Limit a date's resolution. For example, the date <c>2004-09-21 13:50:11</c>
        /// will be changed to <c>2004-09-01 00:00:00</c> when using
        /// <see cref="DateResolution.MONTH"/>.
        /// </summary>
        /// <param name="date"> The <see cref="DateTime"/> to be rounded.</param>
        /// <param name="resolution"> The desired resolution of the <see cref="DateTime"/> to be returned. </param>
        /// <returns> The <see cref="DateTime"/> with all values more precise than <paramref name="resolution"/>
        /// set to their minimum value (0 or 1 depending on the field).</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="resolution"/> is not defined in the <see cref="DateResolution"/> enum.
        /// </exception>
        public static DateTime Round(DateTime date, DateResolution resolution)
        {
            return new DateTime(Round(date.Ticks, resolution,
                inputRepresentation: NumericRepresentation.TICKS,
                outputRepresentation: NumericRepresentation.TICKS));
        }

        /// <summary>
        /// Limit a date's resolution.
        /// <para/>
        /// For example, the time <c>1095774611000</c>
        /// (which represents 2004-09-21 13:50:11) will be changed to
        /// <c>1093996800000</c> (2004-09-01 00:00:00) when using
        /// <see cref="DateResolution.MONTH"/> and <see cref="NumericRepresentation.UNIX_TIME_MILLISECONDS"/>
        /// for both <paramref name="inputRepresentation"/> and <paramref name="outputRepresentation"/>.
        /// <para/>
        /// The ticks <c>632313714110000000</c>
        /// (which represents 2004-09-21 13:50:11) will be changed to
        /// <c>632295936000000000</c> (2004-09-01 00:00:00) when using
        /// <see cref="DateResolution.MONTH"/> and <see cref="NumericRepresentation.TICKS"/>
        /// for both <paramref name="inputRepresentation"/> and <paramref name="outputRepresentation"/>.
        /// <para/>
        /// <b>NOTE:</b> For compatibility with Lucene.NET 3.0.3 and Lucene.NET 4.8.0-beta00001 through 4.8.0-beta00015
        /// specify <paramref name="inputRepresentation"/> as <see cref="NumericRepresentation.TICKS_AS_MILLISECONDS"/> and
        /// <paramref name="outputRepresentation"/> as <see cref="NumericRepresentation.TICKS"/>.
        /// </summary>
        /// <param name="time">The ticks that represent the date to be rounded.</param>
        /// <param name="resolution">The desired resolution of the date to be returned.</param>
        /// <param name="inputRepresentation">The numeric representation of <paramref name="time"/>.</param>
        /// <param name="outputRepresentation">The numeric representation of the return value.</param>
        /// <returns>The date with all values more precise than <paramref name="resolution"/>
        /// set to their minimum value (0 or 1 depending on the field). The return value is expressed in ticks.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="resolution"/> is not defined in the <see cref="DateResolution"/> enum.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="inputRepresentation"/> is not defined in the <see cref="NumericRepresentation"/> enum.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="outputRepresentation"/> is not defined in the <see cref="NumericRepresentation"/> enum.
        /// </exception>
        public static long Round(long time, DateResolution resolution,
            NumericRepresentation inputRepresentation = NumericRepresentation.UNIX_TIME_MILLISECONDS,
            NumericRepresentation outputRepresentation = NumericRepresentation.UNIX_TIME_MILLISECONDS)
        {
            long ticks = inputRepresentation switch
            {
                NumericRepresentation.UNIX_TIME_MILLISECONDS => UnixTimeMillisecondsToTicks(time),
                NumericRepresentation.TICKS => time,
                NumericRepresentation.TICKS_AS_MILLISECONDS => time * TimeSpan.TicksPerMillisecond,
                _ => throw new ArgumentException($"'{inputRepresentation}' is not a valid {nameof(inputRepresentation)}.")
            };
            
            DateTimeOffset dt = new DateTimeOffset(ticks, TimeSpan.Zero);
            // Remove extra ticks beyond milliseconds
            dt = new DateTimeOffset(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, TimeSpan.Zero);

            if (resolution == DateResolution.YEAR)
            {
                dt = dt.AddMonths(1 - dt.Month);
                dt = dt.AddDays(1 - dt.Day);
                dt = dt.AddHours(0 - dt.Hour);
                dt = dt.AddMinutes(0 - dt.Minute);
                dt = dt.AddSeconds(0 - dt.Second);
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
            else if (resolution == DateResolution.MONTH)
            {
                dt = dt.AddDays(1 - dt.Day);
                dt = dt.AddHours(0 - dt.Hour);
                dt = dt.AddMinutes(0 - dt.Minute);
                dt = dt.AddSeconds(0 - dt.Second);
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
            else if (resolution == DateResolution.DAY)
            {
                dt = dt.AddHours(0 - dt.Hour);
                dt = dt.AddMinutes(0 - dt.Minute);
                dt = dt.AddSeconds(0 - dt.Second);
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
            else if (resolution == DateResolution.HOUR)
            {
                dt = dt.AddMinutes(0 - dt.Minute);
                dt = dt.AddSeconds(0 - dt.Second);
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
            else if (resolution == DateResolution.MINUTE)
            {
                dt = dt.AddSeconds(0 - dt.Second);
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
            else if (resolution == DateResolution.SECOND)
            {
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
            else if (resolution == DateResolution.MILLISECOND)
            {
                // don't cut off anything
            }
            else
            {
                throw new ArgumentException("unknown resolution " + resolution);
            }
            return outputRepresentation switch
            {
                NumericRepresentation.UNIX_TIME_MILLISECONDS => TicksToUnixTimeMilliseconds(dt.Ticks),
                NumericRepresentation.TICKS => dt.Ticks,
                NumericRepresentation.TICKS_AS_MILLISECONDS => dt.Ticks / TimeSpan.TicksPerMillisecond,
                _ => throw new ArgumentException($"'{outputRepresentation}' is not a valid {nameof(outputRepresentation)}.")
            };
        }

        /// <summary>
        /// Converts from .NET ticks to the number of milliseconds since January 1, 1970, 00:00:00
        /// UTC (also known as the "epoch").
        /// <para/>
        /// This is the value that is stored in Java Lucene indexes and can be used for storing values
        /// that can be read by Java Lucene.
        /// </summary>
        /// <param name="ticks">The .NET ticks to be converted.</param>
        /// <returns>The converted ticks to number of milliseconds since January 1, 1970, 00:00:00
        /// UTC (also known as the "epoch").</returns>
        public static long TicksToUnixTimeMilliseconds(long ticks)
        {
            return DateTimeOffsetUtil.ToUnixTimeMilliseconds(ticks);
        }

        /// <summary>
        /// Converts from the number of milliseconds since January 1, 1970, 00:00:00 UTC
        /// (also known as the "epoch") to .NET ticks.
        /// </summary>
        /// <param name="unixTimeMilliseconds">The number of milliseconds since January 1, 1970, 00:00:00
        /// UTC (also known as the "epoch") to be converted.</param>
        /// <returns>The converted .NET ticks that can be used to create a <see cref="DateTime"/> or <see cref="DateTimeOffset"/>.</returns>
        public static long UnixTimeMillisecondsToTicks(long unixTimeMilliseconds)
        {
            return DateTimeOffsetUtil.GetTicksFromUnixTimeMilliseconds(unixTimeMilliseconds);
        }
    }

    /// <summary>
    /// Specifies the time granularity. </summary>
    public enum DateResolution
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

    /// <summary>
    /// Specifies how a time will be represented as a <see cref="long"/>.
    /// </summary>
    public enum NumericRepresentation
    {
        /// <summary>
        /// The number of milliseconds since January 1, 1970, 00:00:00
        /// UTC (also known as the "epoch"). This is the format that Lucene
        /// uses, and it is recommended to store this value in the index for compatibility.
        /// </summary>
        UNIX_TIME_MILLISECONDS = 0,

        /// <summary>
        /// The .NET ticks representing a date. Specify this to pass the raw ticks from <see cref="DateTime.Ticks"/>
        /// or to instantiate a new <see cref="DateTime"/> from the result.
        /// </summary>
        TICKS = 1,

        /// <summary>
        /// .NET ticks as total milliseconds.
        /// Input values must be converted using the formula <c>ticks / <see cref="TimeSpan.TicksPerMillisecond"/></c>.
        /// Output values can be converted to ticks using <c>ticks * <see cref="TimeSpan.TicksPerMillisecond"/></c>.
        /// <para/>
        /// This option is provided for compatibility with Lucene.NET 3.0.3 and Lucene.NET 4.8.0-beta00001 through 4.8.0-beta00015,
        /// since it was the only option for input representation.
        /// </summary>
        TICKS_AS_MILLISECONDS = 2,
    }
}