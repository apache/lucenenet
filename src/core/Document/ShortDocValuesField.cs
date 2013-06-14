using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Documents
{
    public class ShortDocValuesField : NumericDocValuesField
    {
        public ShortDocValuesField(String name, short value) : base(name, value)
        {
        }

        public override void SetShortValue(short value)
        {
            base.SetLongValue(value);
        }
    }
}
