using Lucene.Net.Support;
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
            DateTimeOffset offset = DateTimeOffsetUtil.FromUnixTimeMilliseconds(Convert.ToInt64(number));
            DateTimeOffset timeZoneAdjusted = TimeZoneInfo.ConvertTime(offset, TimeZone);
            return timeZoneAdjusted.ToString(GetDateFormat(), FormatProvider);
        }

        public override string Format(long number)
        {
            DateTimeOffset offset = DateTimeOffsetUtil.FromUnixTimeMilliseconds(Convert.ToInt64(number));
            DateTimeOffset timeZoneAdjusted = TimeZoneInfo.ConvertTime(offset, TimeZone);
            return timeZoneAdjusted.ToString(GetDateFormat(), FormatProvider);
        }

        public override object Parse(string source)
        {
            DateTimeOffset parsedDate = DateTimeOffset.ParseExact(source, GetDateFormat(), FormatProvider, DateTimeStyles.None);
            DateTimeOffset timeZoneAdjusted;
            if (parsedDate.DateTime.Kind == DateTimeKind.Unspecified)
                timeZoneAdjusted = new DateTimeOffset(parsedDate.DateTime, TimeZoneInfo.ConvertTime(parsedDate.ToUniversalTime(), TimeZone).Offset);
            else
                timeZoneAdjusted = TimeZoneInfo.ConvertTime(parsedDate, TimeZone);
            return DateTimeOffsetUtil.ToUnixTimeMilliseconds(timeZoneAdjusted);
        }

        public override string Format(object number)
        {
            DateTimeOffset offset = DateTimeOffsetUtil.FromUnixTimeMilliseconds(Convert.ToInt64(number, FormatProvider));
            DateTimeOffset timeZoneAdjusted = TimeZoneInfo.ConvertTime(offset, TimeZone);
            return timeZoneAdjusted.ToString(GetDateFormat(), FormatProvider);
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
                        .Replace("dddd, ", "").Replace(", dddd", "") // Remove the day of the week
                        .Replace("MMMM", "MMM"); // Replace month with abbreviated month
                    break;
                case DateFormat.LONG:
                    datePattern = dateTimeFormat.LongDatePattern
                        .Replace("dddd, ", "").Replace(", dddd", ""); // Remove the day of the week
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
                    timePattern = dateTimeFormat.LongTimePattern.Replace("z", "").Trim() + " zzz";
                    break;
            }

            return string.Concat(datePattern, " ", timePattern);
        }
    }
}
