using System;

namespace Lucene.Net.Support
{
    public enum TimeUnit
    {
        Days,
        Hours,
        Microseconds,
        Milliseconds,
        Minutes,
        Nanoseconds,
        Seconds
    }

    public static class TimeUnitMethods
    {
        public static long Convert(this TimeUnit timeUnit, long sourceDuration, TimeUnit sourceUnit)
        {
            throw new NotImplementedException();
        }

        public static long ToDays(this TimeUnit timeUnit, long duration)
        {
            return timeUnit.Convert(duration, TimeUnit.Days);
        }

        public static long ToHours(this TimeUnit timeUnit, long duration)
        {
            return timeUnit.Convert(duration, TimeUnit.Hours);
        }

        public static long ToMicros(this TimeUnit timeUnit, long duration)
        {
            return timeUnit.Convert(duration, TimeUnit.Microseconds);
        }

        public static long ToMillis(this TimeUnit timeUnit, long duration)
        {
            return timeUnit.Convert(duration, TimeUnit.Milliseconds);
        }

        public static long ToMinutes(this TimeUnit timeUnit, long duration)
        {
            return timeUnit.Convert(duration, TimeUnit.Minutes);
        }

        public static long ToNanos(this TimeUnit timeUnit, long duration)
        {
            return timeUnit.Convert(duration, TimeUnit.Nanoseconds);
        }

        public static long ToSeconds(this TimeUnit timeUnit, long duration)
        {
            return timeUnit.Convert(duration, TimeUnit.Seconds);
        }
    }
}