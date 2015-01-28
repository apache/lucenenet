using System;
using NUnit.Framework;

namespace Lucene.Net.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class WeeklyAttribute : ExplicitAttribute
    {
        public WeeklyAttribute() : base("Weekly")
        {
            
        }
    }
}
