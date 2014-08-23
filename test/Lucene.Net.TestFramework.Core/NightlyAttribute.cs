


namespace Lucene.Net
{
    using System;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class NightlyAttribute : CategoryAttribute
    {

        public NightlyAttribute()
            : base("Nightly")
        {

        }
    }
}
