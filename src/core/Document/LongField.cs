using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Documents
{
    public class LongField : Field
    {
        public static readonly FieldType TYPE_NOT_STORED = new FieldType();
        public static readonly FieldType TYPE_STORED = new FieldType();

        static LongField()
        {
            TYPE_NOT_STORED.Indexed = true;
            TYPE_NOT_STORED.Tokenized = true;
            TYPE_NOT_STORED.OmitNorms = true;
            TYPE_NOT_STORED.IndexOptions = FieldInfo.IndexOptions.DOCS_ONLY;
            TYPE_NOT_STORED.NumericTypeValue = FieldType.NumericType.LONG;
            TYPE_NOT_STORED.Freeze();

            TYPE_STORED.Indexed = true;
            TYPE_STORED.Tokenized = true;
            TYPE_STORED.OmitNorms = true;
            TYPE_STORED.IndexOptions = FieldInfo.IndexOptions.DOCS_ONLY;
            TYPE_STORED.NumericTypeValue = FieldType.NumericType.LONG;
            TYPE_STORED.Stored = true;
            TYPE_STORED.Freeze();
        }

        public LongField(String name, long value, Store stored)
            : base(name, stored == Store.YES ? TYPE_STORED : TYPE_NOT_STORED)
        {
            this.fieldsData = Convert.ToInt64(value);
        }

        public LongField(String name, long value, FieldType type)
            : base(name, type)
        {
            if (type.NumericTypeValue != FieldType.NumericType.INT)
            {
                throw new ArgumentException("type.NumericTypeValue must be LONG but got " + type.NumericTypeValue);
            }

            this.fieldsData = Convert.ToInt64(value);
        }
    }
}
