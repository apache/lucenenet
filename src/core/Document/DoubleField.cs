using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Document
{
    public sealed class DoubleField : Field
    {
        public static readonly FieldType TYPE_NOT_STORED = new FieldType();
        public static readonly FieldType TYPE_STORED = new FieldType();

        static DoubleField()
        {
            TYPE_NOT_STORED.SetIndexed(true);
            TYPE_NOT_STORED.SetTokenized(true);
            TYPE_NOT_STORED.SetOmitNorms(true);
            TYPE_NOT_STORED.SetIndexOptions(IndexOptions.DOCS_ONLY);
            TYPE_NOT_STORED.SetNumericType(FieldType.NumericType.DOUBLE);
            TYPE_NOT_STORED.Freeze();

            TYPE_STORED.SetIndexed(true);
            TYPE_STORED.SetTokenized(true);
            TYPE_STORED.SetOmitNorms(true);
            TYPE_STORED.SetIndexOptions(IndexOptions.DOCS_ONLY);
            TYPE_STORED.SetNumericType(FieldType.NumericType.DOUBLE);
            TYPE_STORED.SetStored(true);
            TYPE_STORED.Freeze();
        }

        public DoubleField(String name, double value, Store stored) : base(name, stored == Store.YES ? TYPE_STORED : TYPE_NOT_STORED )
        {
            fieldsData = Convert.ToDouble(value);
        }

        public DoubleField(String name, double value, FieldType type) : base(name, type)
        {
            if (type.NumericType() != NumericType.DOUBLE)
            {
                throw new ArgumentException("type.numericType() must be DOUBLE but got " + type.NumericType());
            }
            fieldsData = Convert.ToDouble(value);
        }
    }
}
