using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class MaxFloatFunction : MultiFloatFunction
    {
        public MaxFloatFunction(ValueSource[] sources)
            : base(sources)
        {
        }

        protected override string Name
        {
            get
            {
                return "max";
            }
        }

        protected override float Func(int doc, FunctionValues[] valsArr)
        {
            bool first = true;
            float val = 0.0f;
            foreach (FunctionValues vals in valsArr)
            {
                if (first)
                {
                    first = false;
                    val = vals.FloatVal(doc);
                }
                else
                {
                    val = Math.Max(vals.FloatVal(doc), val);
                }
            }

            return val;
        }
    }
}
