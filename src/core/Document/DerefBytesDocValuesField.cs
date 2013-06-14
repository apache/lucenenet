using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Documents
{
    [Obsolete]
    public class DerefBytesDocValuesField : BinaryDocValuesField
    {
        /**
        * Type for bytes DocValues: all with the same length
        */
        public static readonly FieldType TYPE_FIXED_LEN = BinaryDocValuesField.TYPE;

        /**
         * Type for bytes DocValues: can have variable lengths
         */
        public static readonly FieldType TYPE_VAR_LEN = BinaryDocValuesField.TYPE;

        
        public DerefBytesDocValuesField(String name, BytesRef bytes)
            : base(name, bytes)
        {
        }

        public DerefBytesDocValuesField(String name, BytesRef bytes, bool isFixedLength)
            : base(name, bytes)
        {
        }
    }
}
