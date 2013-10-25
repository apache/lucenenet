using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class ProductFloatFunction : MultiFloatFunction
    {
        public ProductFloatFunction(ValueSource[] sources)
            : base(sources)
        {
        }

        protected override string Name
        {
            get
            {
                return "product";
            }
        }

        protected override float Func(int doc, FunctionValues[] valsArr)
        {
            float val = 1.0f;
            foreach (FunctionValues vals in valsArr)
            {
                val = vals.FloatVal(doc);
            }

            return val;
        }
    }
}
