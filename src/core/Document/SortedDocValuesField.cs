using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Documents
{
    public class SortedDocValuesField : Field
    {
        public static readonly FieldType TYPE = new FieldType();

        static SortedDocValuesField()
        {
            TYPE.DocValueType = FieldInfo.DocValuesType.SORTED;
            TYPE.Freeze();
        }

        public SortedDocValuesField(String name, BytesRef bytes) : base(name, TYPE)
        {
            this.fieldsData = bytes;
        }
    }
}
