using System;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Documents
{
    public class BinaryDocValuesField : Field
    {
        public static readonly FieldType TYPE = new FieldType();

        static BinaryDocValuesField()
        {
            TYPE.DocValueType = FieldInfo.DocValuesType.BINARY;
            TYPE.Freeze();
        }

        public BinaryDocValuesField(String name, BytesRef value)
            : base(name, TYPE)
        {
            this.fieldsData = value;
        }
    }
}

