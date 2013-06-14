using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;

namespace Lucene.Net.Document
{
    [Obsolete("Use BinaryDocValuesField instead")]
    public class DerefBytesDocValuesField : BinaryDocValuesField
    {
        public static FieldType TYPE_FIXED_LEN = BinaryDocValuesField.TYPE;
        public static FieldType TYPE_VAR_LEN = BinaryDocValuesField.TYPE;

        public DerefBytesDocValuesField(String name, BytesRef bytes) : base (name, bytes)
        {
            
        }

        public DerefBytesDocValuesField(String name, BytesRef bytes, bool isFixedLength) : base (name, bytes)
        {
            
        }
    }
}
