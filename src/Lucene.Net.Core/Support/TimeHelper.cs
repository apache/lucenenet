using System;

namespace Lucene.Net.Support
{
    public static class TimeHelper
    {
        public static long NanoTime()
        {
            return DateTime.Now.Ticks / 100;
        }
    }
}