using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class DivFloatFunction : DualFloatFunction
    {
        public DivFloatFunction(ValueSource a, ValueSource b)
            : base(a, b)
        {
        }

        protected override string Name
        {
            get
            {
                return @"div";
            }
        }

        protected override float Func(int doc, FunctionValues aVals, FunctionValues bVals)
        {
            return aVals.FloatVal(doc) / bVals.FloatVal(doc);
        }

    }
}
