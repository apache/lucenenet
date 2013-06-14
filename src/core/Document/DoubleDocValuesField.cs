using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Document
{
    public class DoubleDocValuesField : NumericDocValuesField
    {
        public DoubleDocValuesField(String name, double value) : base (name, BitConverter.DoubleToInt64Bits(value))
        {
            
        }

        public override void SetDoubleValue(double value)
        {
            this.SetLongValue(BitConverter.DoubleToInt64Bits(value));
        }

        public override void SetLongValue(long value)
        {
            throw new ArgumentException("cannot change value type from double to long");
        }
    }
}
