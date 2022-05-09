using Lucene.Net.Documents;
using Lucene.Net.Util;
using System;
using System.Globalization;
#nullable enable

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

    /// <summary>
    /// LUCENENET specific enum for mimicking the Java DateFormat
    /// </summary>
    public enum DateFormat
    {
        /// <summary>
        /// Full style pattern.
        /// </summary>
        FULL,
        /// <summary>
        /// Long style pattern.
        /// </summary>
        LONG,
        /// <summary>
        /// Medium style pattern.
        /// </summary>
        MEDIUM,
        /// <summary>
        /// Short style pattern.
        /// </summary>
        SHORT
    }

    /// <summary>
    /// This <see cref="NumberFormat"/> parses <see cref="long"/> into date strings and vice-versa. It
    /// uses the given <c>dateFormat</c> and <see cref="CultureInfo">locale</see> to parse and format dates, but before, it
    /// converts <see cref="long"/> to <see cref="DateTime"/> objects or vice-versa.
    /// <para/>
    /// The <see cref="long"/> value the dates are parsed into and out of is specified by changing <see cref="NumericRepresentation"/>.
    /// The default value is <see cref="Documents.NumericRepresentation.UNIX_TIME_MILLISECONDS"/>, which is the number of milliseconds
    /// since January 1, 1970 0:00:00 UTC, also known as the "epoch".
    /// </summary>
    public class NumberDateFormat : NumberFormat
    {
        private string? dateFormat;
        private readonly DateFormat dateStyle;
        private readonly DateFormat? timeStyle;
        private TimeZoneInfo timeZone = TimeZoneInfo.Local;

        /// <summary>
        /// Constructs a <see cref="NumberDateFormat"/> object using the given <paramref name="dateFormat"/>
        /// and <paramref name="provider"/>.
        /// </summary>
        /// <param name="dateFormat">Date format used to parse and format dates.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        public NumberDateFormat(string? dateFormat, IFormatProvider? provider)
            : base(provider)
        {
            this.dateFormat = dateFormat;
        }

        /// <summary>
        /// Constructs a <see cref="NumberDateFormat"/> object using the provided <paramref name="dateStyle"/>
        /// and <paramref name="provider"/>.
        /// </summary>
        /// <param name="dateStyle">The date formatting style. For example, <see cref="DateFormat.SHORT"/> for "M/d/yy" in the en-US culture.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        public NumberDateFormat(DateFormat dateStyle, IFormatProvider? provider)
            : this(GetDateFormat(dateStyle, provider), provider)
        {
            this.dateStyle = dateStyle;
        }

        /// <summary>
        /// Constructs a <see cref="NumberDateFormat"/> object using the provided <paramref name="dateStyle"/>,
        /// <paramref name="timeStyle"/>, and <paramref name="provider"/>.
        /// </summary>
        /// <param name="dateStyle">The date formatting style. For example, <see cref="DateFormat.SHORT"/> for "M/d/yyyy" in the en-US culture.</param>
        /// <param name="timeStyle">The time formatting style. For example, <see cref="DateFormat.SHORT"/> for "h:mm tt" in the en-US culture.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        public NumberDateFormat(DateFormat dateStyle, DateFormat timeStyle, IFormatProvider? provider)
            : base(provider)
        {
            this.dateStyle = dateStyle;
            this.timeStyle = timeStyle;
        }

        /// <summary>
        /// The numeric representation to convert the date into when calling <see cref="Parse(string)"/>
        /// or convert the date from when calling <see cref="Format(long)"/> overloads.
        /// </summary>
        public NumericRepresentation NumericRepresentation { get; set; } = NumericRepresentation.UNIX_TIME_MILLISECONDS;

        /// <summary>
        /// The time zone to convert dates to when using <see cref="Format(long)"/>
        /// or from when using <see cref="Parse(string)"/>.
        /// </summary>
        public virtual TimeZoneInfo TimeZone
        {
            get => this.timeZone;
            set => this.timeZone = value;
        }

        public override string Format(double number)
        {
            return Format(Convert.ToInt64(number));
        }

        public override string Format(long number)
        {
            long ticks = NumericRepresentation switch
            {
                NumericRepresentation.UNIX_TIME_MILLISECONDS => DateTools.UnixTimeMillisecondsToTicks(number),
                NumericRepresentation.TICKS => number,
                NumericRepresentation.TICKS_AS_MILLISECONDS => number * TimeSpan.TicksPerMillisecond,
                _ => throw new ArgumentException($"'{NumericRepresentation}' is not a valid {nameof(NumericRepresentation)}.")
            };

            DateTimeOffset offset = new DateTimeOffset(ticks, TimeSpan.Zero); // Always UTC time
            DateTimeOffset timeZoneAdjusted = TimeZoneInfo.ConvertTime(offset, TimeZone);
            return timeZoneAdjusted.ToString(GetDateFormat(), FormatProvider);
        }

        public override J2N.Numerics.Number Parse(string source)
        {
            DateTimeOffset parsedDate = DateTimeOffset.ParseExact(source, GetDateFormat(), FormatProvider, DateTimeStyles.None);
            DateTimeOffset timeZoneAdjusted;
            if (parsedDate.DateTime.Kind == DateTimeKind.Unspecified)
                timeZoneAdjusted = new DateTimeOffset(parsedDate.DateTime, TimeZoneInfo.ConvertTime(parsedDate.ToUniversalTime(), TimeZone).Offset);
            else
                timeZoneAdjusted = TimeZoneInfo.ConvertTime(parsedDate, TimeZone);
            long ticks = timeZoneAdjusted.UtcDateTime.Ticks;
            long result = NumericRepresentation switch
            {
                NumericRepresentation.UNIX_TIME_MILLISECONDS => DateTools.TicksToUnixTimeMilliseconds(ticks),
                NumericRepresentation.TICKS => ticks,
                NumericRepresentation.TICKS_AS_MILLISECONDS => ticks / TimeSpan.TicksPerMillisecond,
                _ => throw new ArgumentException($"'{NumericRepresentation}' is not a valid {nameof(NumericRepresentation)}.")
            };
            return J2N.Numerics.Int64.GetInstance(result);
        }

        public override string Format(object number)
        {
            return Format(Convert.ToInt64(number, FormatProvider));
        }

        public void SetDateFormat(string dateFormat)
        {
            this.dateFormat = dateFormat;
        }

        /// <summary>
        /// Returns the .NET date format that will be used to Format the date.
        /// Note that parsing the date uses <see cref="DateTime.ParseExact(string, string, IFormatProvider)"/>.
        /// </summary>
        // LUCENENET specific
        public string GetDateFormat()
        {
            if (dateFormat != null) return dateFormat;

            if (!timeStyle.HasValue) return GetDateFormat(dateStyle, FormatProvider);
            return GetDateFormat(dateStyle, timeStyle.Value, FormatProvider);
        }

        /// <summary>
        /// Gets a date format string similar to Java's SimpleDateFormat using the specified <paramref name="dateStyle"/>,
        /// <paramref name="timeStyle"/> and <paramref name="provider"/>.
        /// </summary>
        /// <param name="dateStyle">The date formatting style. For example, <see cref="DateFormat.SHORT"/> for "M/d/yyyy" in the en-US culture.</param>
        /// <param name="timeStyle">The time formatting style. For example, <see cref="DateFormat.SHORT"/> for "h:mm tt" in the en-US culture.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information. If <c>null</c> the culture of the current thread will be used.</param>
        /// <returns>A date and time format string with a space separator.</returns>
        public static string GetDateFormat(DateFormat dateStyle, DateFormat timeStyle, IFormatProvider? provider)
        {
            DateTimeFormatInfo dateTimeFormat = DateTimeFormatInfo.GetInstance(provider);

            string datePattern = GetDateFormat(dateStyle, provider);
            string timePattern = timeStyle switch
            {
                DateFormat.SHORT => dateTimeFormat.ShortTimePattern,
                DateFormat.MEDIUM => dateTimeFormat.LongTimePattern,
                DateFormat.LONG => dateTimeFormat.LongTimePattern.Replace("z", "").Trim() + " z",
                DateFormat.FULL => dateTimeFormat.LongTimePattern.Replace("z", "").Trim() + " zzz",
                _ => throw new ArgumentException($"'{timeStyle}' is not a valid {nameof(DateFormat)}."),
            };

            return string.Concat(datePattern, " ", timePattern);
        }

        /// <summary>
        /// Gets a date format string similar to Java's SimpleDateFormat using the specified <paramref name="dateStyle"/>
        /// and <paramref name="provider"/>.
        /// </summary>
        /// <param name="dateStyle">The date formatting style. For example, <see cref="DateFormat.SHORT"/> for "M/d/yyyy" in the en-US culture.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information. If <c>null</c> the culture of the current thread will be used.</param>
        /// <returns>A date format string.</returns>
        public static string GetDateFormat(DateFormat dateStyle, IFormatProvider? provider)
        {
            DateTimeFormatInfo dateTimeFormat = DateTimeFormatInfo.GetInstance(provider);
            return dateStyle switch
            {
                DateFormat.SHORT => dateTimeFormat.ShortDatePattern,
                DateFormat.MEDIUM => dateTimeFormat.LongDatePattern
                    .Replace("dddd, ", "").Replace(", dddd", "") // Remove the day of the week
                    .Replace("MMMM", "MMM"), // Replace month with abbreviated month
                DateFormat.LONG => dateTimeFormat.LongDatePattern
                    .Replace("dddd, ", "").Replace(", dddd", ""), // Remove the day of the week
                DateFormat.FULL => dateTimeFormat.LongDatePattern,
                _ => throw new ArgumentException($"'{dateStyle}' is not a valid {nameof(DateFormat)}."),
            };
        }
    }
}
