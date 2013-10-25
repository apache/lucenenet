using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class PowFloatFunction : DualFloatFunction
    {
        public PowFloatFunction(ValueSource a, ValueSource b)
            : base(a, b)
        {
        }

        protected override string Name
        {
            get
            {
                return "pow";
            }
        }

        protected override float Func(int doc, FunctionValues aVals, FunctionValues bVals)
        {
            return (float)Math.Pow(aVals.FloatVal(doc), bVals.FloatVal(doc));
        }
    }
}
