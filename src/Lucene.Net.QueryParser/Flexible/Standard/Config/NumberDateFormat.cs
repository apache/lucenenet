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
        FULL, LONG,
        MEDIUM, SHORT
    }

    /// <summary>
    /// This <see cref="NumberFormat"/> parses <see cref="long"/> into date strings and vice-versa. It
    /// uses the given <c>dateFormat</c> and <see cref="CultureInfo">locale</see> to parse and format dates, but before, it
    /// converts <see cref="long"/> to <see cref="DateTime"/> objects or vice-versa.
    /// <para/>
    /// Note that the <see cref="long"/> value the dates are parsed into and out of represent the number of milliseconds
    /// since January 1, 1970 0:00:00, also known as the "epoch".
    /// </summary>
    public class NumberDateFormat : NumberFormat
    {
        //private static readonly long serialVersionUID = 964823936071308283L;

        // The .NET ticks representing January 1, 1970 0:00:00 GMT, also known as the "epoch".
        public const long EPOCH = 621355968000000000;

        private string? dateFormat;
        private readonly DateFormat dateStyle;
        private readonly DateFormat timeStyle;
        private TimeZoneInfo timeZone = TimeZoneInfo.Local;

        /// <summary>
        /// Constructs a <see cref="NumberDateFormat"/> object using the given <paramref name="dateFormat"/>
        /// and <paramref name="formatProvider"/>.
        /// </summary>
        /// <param name="dateFormat">Date format used to parse and format dates.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
        public NumberDateFormat(string? dateFormat, IFormatProvider? formatProvider)
            : base(formatProvider)
        {
            this.dateFormat = dateFormat;
        }

        /// <summary>
        /// Constructs a <see cref="NumberDateFormat"/> object using the given <paramref name="dateStyle"/>,
        /// <paramref name="timeStyle"/>, and <paramref name="formatProvider"/>.
        /// </summary>
        /// <param name="dateStyle"></param>
        /// <param name="timeStyle"></param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
        public NumberDateFormat(DateFormat dateStyle, DateFormat timeStyle, IFormatProvider? formatProvider)
            : base(formatProvider)
        {
            this.dateStyle = dateStyle;
            this.timeStyle = timeStyle;
        }

        public virtual TimeZoneInfo TimeZone
        {
            get => this.timeZone;
            set => this.timeZone = value;
        }

        public override string Format(double number)
        {
            return J2N.Time.UnixEpoch
                .ToCalendar(FormatProvider)
                .ToTimeZone(TimeZone)
                .AddMilliseconds(number)
                .ToString(GetDateFormat(), FormatProvider);
        }

        public override string Format(long number)
        {
            return J2N.Time.UnixEpoch
                .ToCalendar(FormatProvider)
                .ToTimeZone(TimeZone)
                .AddMilliseconds(number)
                .ToString(GetDateFormat(), FormatProvider);
        }

        public override object Parse(string source)
        {
            DateTime d = DateTime.ParseExact(source, GetDateFormat(), FormatProvider, DateTimeStyles.None);
            return (d.ToCalendar(FormatProvider) - J2N.Time.UnixEpoch.ToCalendar(FormatProvider).ToTimeZone(TimeZone)).TotalMilliseconds;
        }

        public override string Format(object number)
        {
            return J2N.Time.UnixEpoch
                .ToCalendar(FormatProvider)
                .ToTimeZone(TimeZone)
                .AddMilliseconds(Convert.ToInt64(number, CultureInfo.InvariantCulture))
                .ToString(GetDateFormat(), FormatProvider);
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

            return GetDateFormat(this.dateStyle, this.timeStyle, FormatProvider);
        }

        public static string GetDateFormat(DateFormat dateStyle, DateFormat timeStyle, IFormatProvider? provider)
        {
            string datePattern = "", timePattern = "";
            DateTimeFormatInfo dateTimeFormat = (provider ?? DateTimeFormatInfo.CurrentInfo)
                .GetFormat(typeof(DateTimeFormatInfo)) as DateTimeFormatInfo ?? DateTimeFormatInfo.CurrentInfo;

            switch (dateStyle)
            {
                case DateFormat.SHORT:
                    datePattern = dateTimeFormat.ShortDatePattern;
                    break;
                case DateFormat.MEDIUM:
                    datePattern = dateTimeFormat.LongDatePattern
                        .Replace("dddd,", "").Replace(", dddd", "") // Remove the day of the week
                        .Replace("MMMM", "MMM"); // Replace month with abbreviated month
                    break;
                case DateFormat.LONG:
                    datePattern = dateTimeFormat.LongDatePattern
                        .Replace("dddd,", "").Replace(", dddd", ""); // Remove the day of the week
                    break;
                case DateFormat.FULL:
                    datePattern = dateTimeFormat.LongDatePattern;
                    break;
            }

            switch (timeStyle)
            {
                case DateFormat.SHORT:
                    timePattern = dateTimeFormat.ShortTimePattern;
                    break;
                case DateFormat.MEDIUM:
                    timePattern = dateTimeFormat.LongTimePattern;
                    break;
                case DateFormat.LONG:
                    timePattern = dateTimeFormat.LongTimePattern.Replace("z", "").Trim() + " z";
                    break;
                case DateFormat.FULL:
                    timePattern = dateTimeFormat.LongTimePattern.Replace("z", "").Trim() + " z"; // LUCENENET TODO: Time zone info not being added to match behavior of Java, but Java doc is unclear on what the difference is between this and LONG
                    break;
            }

            return string.Concat(datePattern, " ", timePattern);
        }
    }

    /// <summary>
    /// Extensions to <see cref="DateTime"/>.
    /// </summary>
    internal static class DateTimeExtensions
    {
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

        /// <summary>
        /// Returns a new <see cref="DateTime"/> that converts the value of <paramref name="input"/> (current instance) into
        /// the corresponding value in the <see cref="DateTimeFormatInfo.Calendar"/> of the <paramref name="provider"/>.
        /// </summary>
        /// <param name="input">The current <see cref="DateTime"/> instance.</param>
        /// <param name="provider">
        /// The provider to use to format the value.
        /// <para/>
        /// -or-
        /// <para/>
        /// A null reference (Nothing in Visual Basic) to obtain the numeric format information from the locale setting of the current thread.
        /// </param>
        /// <returns>A new <see cref="DateTime"/> instance with the same value of <paramref name="input"/> adjusted
        /// for the <see cref="DateTimeFormatInfo.Calendar"/> of the <paramref name="provider"/>.</returns>
        /// <exception cref="NotSupportedException">The supplied <paramref name="provider"/> returned <c>null</c> for the requested type <see cref="DateTimeFormatInfo"/>.</exception>
        public static DateTime ToCalendar(this DateTime input, IFormatProvider? provider)
        {
            return ToCalendar(input, GetCalendar(provider));
        }

        /// <summary>
        /// Returns a new <see cref="DateTime"/> that converts the value of <paramref name="input"/> (current instance) into
        /// the corresponding value in the provided <paramref name="calendar"/>.
        /// </summary>
        /// <param name="input">The current <see cref="DateTime"/> instance.</param>
        /// <param name="calendar">A <see cref="Calendar"/> instance.</param>
        /// <returns>A new <see cref="DateTime"/> instance with the same value of <paramref name="input"/> adjusted
        /// for the provided <paramref name="calendar"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="calendar"/> is null.</exception>
        public static DateTime ToCalendar(this DateTime input, Calendar calendar)
        {
            if (calendar is null)
                throw new ArgumentNullException(nameof(calendar));

            // Get the remaining ticks so we don't lose resolution.
            DateTime temp = new DateTime(input.Year, input.Month, input.Day, input.Hour, input.Minute, input.Second, input.Millisecond, input.Kind);
            long diffTicks = input.Ticks - temp.Ticks;

            return new DateTime(
                calendar.GetYear(input),
                calendar.GetMonth(input),
                calendar.GetDayOfMonth(input),
                calendar.GetHour(input),
                calendar.GetMinute(input),
                calendar.GetSecond(input),
                (int)calendar.GetMilliseconds(input)
                , calendar
                //)
                , input.Kind)
                .AddTicks(diffTicks);
        }

        public static DateTime ToTimeZone(this DateTime input, TimeZoneInfo timeZone)
        {
            return TimeZoneInfo.ConvertTime(input, timeZone);
        }
    }
}
