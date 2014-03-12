using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Randomized.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SeedDecoratorAttribute : System.Attribute
    {
        public IList<Type> Decorators { get; set; } 


        public SeedDecoratorAttribute(params Type[] decorators)
        {
            this.Decorators = new List<Type>();

            foreach (var item in decorators)
            {
                if (item.GetInterfaces().Contains(typeof(ISeedDecorator)))
                    this.Decorators.Add(item);
            }
        }
    }
}
