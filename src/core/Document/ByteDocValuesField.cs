using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Documents
{
    [Obsolete]
    public class ByteDocValuesField : NumericDocValuesField
    {
        public ByteDocValuesField(String name, sbyte value)
            : base(name, value)
        {
        }

        public override void SetByteValue(sbyte value)
        {
            SetLongValue(value);
        }
    }
}
