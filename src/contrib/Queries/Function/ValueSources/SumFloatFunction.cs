using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class SumFloatFunction : MultiFloatFunction
    {
        public SumFloatFunction(ValueSource[] sources)
            : base(sources)
        {
        }

        protected override string Name
        {
            get
            {
                return "sum";
            }
        }

        protected override float Func(int doc, FunctionValues[] valsArr)
        {
            float val = 0.0f;
            foreach (FunctionValues vals in valsArr)
            {
                val = vals.FloatVal(doc);
            }

            return val;
        }
    }
}
