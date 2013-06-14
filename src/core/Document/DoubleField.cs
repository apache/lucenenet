using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Documents
{
    public class DoubleField : Field
    {
        public static readonly FieldType TYPE_NOT_STORED = new FieldType();
        public static readonly FieldType TYPE_STORED = new FieldType();

        static DoubleField()
        {
            TYPE_NOT_STORED.Indexed = true;
            TYPE_NOT_STORED.Tokenized = true;
            TYPE_NOT_STORED.OmitNorms = true;
            TYPE_NOT_STORED.IndexOptions = IndexOptions.DOCS_ONLY;
            TYPE_NOT_STORED.NumericTypeValue = FieldType.NumericType.DOUBLE;
            TYPE_NOT_STORED.Freeze();

            TYPE_STORED.Indexed = true;
            TYPE_STORED.Tokenized = true;
            TYPE_STORED.OmitNorms = true;
            TYPE_STORED.IndexOptions = IndexOptions.DOCS_ONLY;
            TYPE_STORED.NumericTypeValue = FieldType.NumericType.DOUBLE;
            TYPE_STORED.Stored = true;
            TYPE_STORED.Freeze();
        }

        public DoubleField(string name, double value, Store stored)
            : base(name, stored == Store.YES ? TYPE_STORED : TYPE_NOT_STORED)
        {
            fieldsData = value;
        }

        public DoubleField(string name, double value, FieldType type)
            : base(name, type)
        {
            if (type.NumericTypeValue != FieldType.NumericType.DOUBLE)
            {
                throw new ArgumentException("type.numericType() must be DOUBLE but got " + type.NumericTypeValue);
            }
            fieldsData = value;
        }
    }
}
