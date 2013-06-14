using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Documents
{
    public class NumericDocValuesField : Field
    {
        public static readonly FieldType TYPE = new FieldType();

        static NumericDocValuesField()
        {
            TYPE.DocValueType = FieldInfo.DocValuesType.NUMERIC;
            TYPE.Freeze();
        }

        public NumericDocValuesField(String name, long value)
            : base(name, TYPE)
        {
            this.fieldsData = Convert.ToInt64(value);
        }
    }
}
