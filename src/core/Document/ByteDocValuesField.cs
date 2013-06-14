using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Document
{
    [Obsolete("Use NumericDocValuesField instead")]
    public class ByteDocValuesField : NumericDocValuesField
    {
        public ByteDocValuesField(String name, byte value) : base(name, value)
        {
            
        }

        public override void SetByteValue(byte value)
        {
            this.SetLongValue(value);
        }
    }  
}
