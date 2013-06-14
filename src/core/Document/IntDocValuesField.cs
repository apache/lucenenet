using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Documents
{
    public class IntDocValuesField : NumericDocValuesField
    {
        public IntDocValuesField(String name, int value) : base(name, value)
        {
        }

        public override void SetIntValue(int value)
        {
            this.SetLongValue(value);
        }

    }
}
