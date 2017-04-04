using System;

namespace Lucene.Net.Support
{
    public static class Time
    {
        public const long MILLISECONDS_PER_NANOSECOND = 1000000;
        public const long TICKS_PER_NANOSECOND = 100;

        public static long NanoTime()
        {
            return DateTime.Now.Ticks * TICKS_PER_NANOSECOND;
        }
    }
}