using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Documents
{
    public class SortedSetDocValuesField : Field
    {
        public static readonly FieldType TYPE = new FieldType();

        static SortedSetDocValuesField()
        {
            TYPE.DocValueType = FieldInfo.DocValuesType.SORTED_SET;
            TYPE.Freeze();
        }

        public SortedSetDocValuesField(String name, BytesRef bytes) : base(name, TYPE)
        {
            this.fieldsData = bytes;
        }
    }
}
