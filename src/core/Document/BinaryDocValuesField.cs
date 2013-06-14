using System;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Document
{
    public class BinaryDocValuesField : Field
    {

        public static readonly FieldType TYPE = new FieldType();

        static BinaryDocValuesField()
        {
            TYPE.SetDocValueType(FieldInfo.DocValuesType.BINARY);
            TYPE.Freeze();
        }

        public BinaryDocValuesField(String name, BytesRef value)
            : base(name, TYPE)
        {
            this.fieldsData = value;
        }
    }
}

