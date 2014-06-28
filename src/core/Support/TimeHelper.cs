using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    public static class TimeHelper
    {
        public static long NanoTime()
        {
            return DateTime.Now.Ticks/100;
        }
    }
}
