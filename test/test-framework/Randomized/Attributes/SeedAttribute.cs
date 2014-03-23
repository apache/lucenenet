using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Randomized.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SeedAttribute : System.Attribute
    {
        public string Value { get; protected set; }

        public SeedAttribute(string value)
        {
            this.Value= value;
        }
    }
}
