

namespace Lucene.Net
{
    using System;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class PerformanceAttribute : CategoryAttribute
    {

        public PerformanceAttribute()
            : base("Performance")
        {

        }
    }
}
