// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Lucene.Net.Support
{
    // Source: https://github.com/dotnet/runtime/blob/af4efb1936b407ca5f4576e81484cf5687b79a26/src/libraries/System.Private.CoreLib/src/System/DateTimeOffset.cs
    internal static class DateTimeOffsetUtil
    {
        /// <summary>
        /// The .NET ticks representing January 1, 1970 0:00:00, also known as the "epoch".
        /// </summary>
        private const long UnixEpochTicks = 621355968000000000L;

        private const long UnixEpochMilliseconds = UnixEpochTicks / TimeSpan.TicksPerMillisecond; // 62,135,596,800,000

        public const long MinMilliseconds = /*DateTime.*/MinTicks / TimeSpan.TicksPerMillisecond - UnixEpochMilliseconds;
        public const long MaxMilliseconds = /*DateTime.*/MaxTicks / TimeSpan.TicksPerMillisecond - UnixEpochMilliseconds;

        // From System.DateTime

        // Number of 100ns ticks per time unit
        private const long TicksPerMillisecond = 10000;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;
        private const long TicksPerMinute = TicksPerSecond * 60;
        private const long TicksPerHour = TicksPerMinute * 60;
        private const long TicksPerDay = TicksPerHour * 24;

        // Number of days in a non-leap year
        private const int DaysPerYear = 365;
        // Number of days in 4 years
        private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
        // Number of days in 100 years
        private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
        // Number of days in 400 years
        private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097

        // Number of days from 1/1/0001 to 12/31/9999
        private const int DaysTo10000 = DaysPer400Years * 25 - 366;  // 3652059

        internal const long MinTicks = 0;
        internal const long MaxTicks = DaysTo10000 * TicksPerDay - 1;


        public static long GetTicksFromUnixTimeMilliseconds(long milliseconds)
        {
            if (milliseconds < MinMilliseconds || milliseconds > MaxMilliseconds)
            {
                throw new ArgumentOutOfRangeException(nameof(milliseconds),
                    string.Format("Valid values are between {0} and {1}, inclusive.", MinMilliseconds, MaxMilliseconds));
            }

            long ticks = milliseconds * TimeSpan.TicksPerMillisecond + UnixEpochTicks;
            return ticks;
        }

        public static DateTimeOffset FromUnixTimeMilliseconds(long milliseconds)
        {
            if (milliseconds < MinMilliseconds || milliseconds > MaxMilliseconds)
            {
                throw new ArgumentOutOfRangeException(nameof(milliseconds),
                    string.Format("Valid values are between {0} and {1}, inclusive.", MinMilliseconds, MaxMilliseconds));
            }

            long ticks = milliseconds * TimeSpan.TicksPerMillisecond + UnixEpochTicks;
            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }

        public static long ToUnixTimeMilliseconds(DateTimeOffset offset)
        {
            // Truncate sub-millisecond precision before offsetting by the Unix Epoch to avoid
            // the last digit being off by one for dates that result in negative Unix times
            long milliseconds = offset.UtcDateTime.Ticks / TimeSpan.TicksPerMillisecond;
            return milliseconds - UnixEpochMilliseconds;
        }

        public static long ToUnixTimeMilliseconds(long ticks)
        {
            // Truncate sub-millisecond precision before offsetting by the Unix Epoch to avoid
            // the last digit being off by one for dates that result in negative Unix times
            long milliseconds = ticks / TimeSpan.TicksPerMillisecond;
            return milliseconds - UnixEpochMilliseconds;
        }
    }
}
