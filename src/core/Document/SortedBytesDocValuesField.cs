using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Util;

namespace Lucene.Net.Documents
{
    public class SortedBytesDocValuesField : SortedDocValuesField
    {
        public static readonly FieldType TYPE_FIXED_LEN = SortedDocValuesField.TYPE;
        public static readonly FieldType TYPE_VAR_LEN = SortedDocValuesField.TYPE;


        public SortedBytesDocValuesField(String name, BytesRef bytes) : base(name, bytes)
        {
        }

        public SortedBytesDocValuesField(String name, BytesRef bytes, bool isFixedLength) : base (name, bytes)
        {
            
        }
    }
}
