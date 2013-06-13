using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Util
{
    public class NamedThreadFactory : ThreadFactory
    {
        private static int threadPoolNumber = 0;
        private int threadNumber = 0;
        private const string NAME_PATTERN = "{0}-{1}-thread";
        private readonly string threadNamePrefix;

        public NamedThreadFactory(string threadNamePrefix)
        {
            this.threadNamePrefix = string.Format(CultureInfo.InvariantCulture, NAME_PATTERN,
                CheckPrefix(threadNamePrefix), Interlocked.Increment(ref threadPoolNumber));
        }

        private static string CheckPrefix(string prefix)
        {
            return prefix == null || prefix.Length == 0 ? "Lucene" : prefix;
        }

        public Thread NewThread(IThreadRunnable r)
        {
            Thread t = new Thread(r.Run) 
            { 
                Name = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", 
                    this.threadNamePrefix, Interlocked.Increment(ref threadNumber)),
                IsBackground = false,
                Priority = ThreadPriority.Normal
            };

            return t;
        }
    }
}
