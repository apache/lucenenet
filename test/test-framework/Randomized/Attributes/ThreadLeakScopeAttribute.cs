using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Randomized.Attributes
{
    public enum ThreadLeakScopes
    {
        Test,
        Suite,
        None
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ThreadLeakScopeAttribute : System.Attribute
    {

        public ThreadLeakScopes Scope { get; protected set; }

        public ThreadLeakScopeAttribute(ThreadLeakScopes scope)
        {
            this.Scope = scope;
        }
    }
}
