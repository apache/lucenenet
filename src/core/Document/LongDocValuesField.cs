using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Documents
{
    public class LongDocValuesField : NumericDocValuesField
    {
        public LongDocValuesField(String name, long value) : base(name, value)
        {
        }
    }
}
