using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;

namespace Lucene.Net.Documents
{
    public class StraightBytesDocValuesField : BinaryDocValuesField
    {
        public static readonly FieldType TYPE_FIXED_LEN = BinaryDocValuesField.TYPE;
        public static readonly FieldType TYPE_VAR_LEN = BinaryDocValuesField.TYPE;

        public StraightBytesDocValuesField(String name, BytesRef value) : base(name, value)
        {
        }

        public StraightBytesDocValuesField(String name, BytesRef bytes, bool isFixedLength) : base(name, bytes)
        {
            
        }
    }
}
