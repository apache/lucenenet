using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Documents
{
    public class DoubleDocValuesField : NumericDocValuesField
    {
        public DoubleDocValuesField(String name, double value)
            : base(name, BitConverter.DoubleToInt64Bits(value))
        {
        }
        
        public override void SetDoubleValue(double value)
        {
            base.SetLongValue(BitConverter.DoubleToInt64Bits(value));
        }

        public override void SetLongValue(long value)
        {
            throw new ArgumentException("cannot change value type from Double to Long");
        }
    }
}
