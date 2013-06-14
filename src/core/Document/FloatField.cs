using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Documents
{
    public class FloatField : Field
    {
        public static readonly FieldType TYPE_NOT_STORED = new FieldType();
        public static readonly FieldType TYPE_STORED = new FieldType();
        static FloatField()
        {
            TYPE_NOT_STORED.Indexed = true;
            TYPE_NOT_STORED.Tokenized = true;
            TYPE_NOT_STORED.OmitNorms = true;
            TYPE_NOT_STORED.IndexOptions = IndexOptions.DOCS_ONLY;
            TYPE_NOT_STORED.NumericTypeValue = FieldType.NumericType.FLOAT;
            TYPE_NOT_STORED.Freeze();

            TYPE_STORED.Indexed = true;
            TYPE_STORED.Tokenized = true;
            TYPE_STORED.OmitNorms = true;
            TYPE_STORED.IndexOptions = IndexOptions.DOCS_ONLY;
            TYPE_STORED.NumericTypeValue = FieldType.NumericType.FLOAT;
            TYPE_STORED.Stored = true;
            TYPE_STORED.Freeze();

        }

        public FloatField(String name, float value, Store stored) : base(name, stored == Store.YES ? TYPE_STORED : TYPE_NOT_STORED)
        {
            this.fieldsData = Convert.ToSingle(value);
        }

        public FloatField(String name, float value, FieldType type) : base(name, type)
        {
            if (type.NumericTypeValue != FieldType.NumericType.FLOAT)
            {
                throw new ArgumentException("type.NumericTypevalue must be FLOAT but got " + type.NumericTypeValue);
            }

            this.fieldsData = Convert.ToSingle(value);
        }
    }
}
