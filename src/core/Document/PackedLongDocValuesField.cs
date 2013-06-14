using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Documents
{
    public class PackedLongDocValuesField : NumericDocValuesField
    {
        public PackedLongDocValuesField(String name, long value) : base(name, value)
        {
        }
    }
}
