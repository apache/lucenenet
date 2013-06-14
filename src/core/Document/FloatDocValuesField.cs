using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Lucene.Net.Documents
{
    public class FloatDocValuesField : NumericDocValuesField
    {
        public FloatDocValuesField(string name, float value)
            : base(name, Support.Single.FloatToIntBits(value))
        {
        }

        public override void SetFloatValue(float value)
        {
            base.SetFloatValue(Support.Single.FloatToIntBits(value));
        }

        public override void SetLongValue(long value)
        {
            throw new ArgumentException("cannot change value type from Float to Long");
        }
    }
}

