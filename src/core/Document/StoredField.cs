using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Documents
{
    public sealed class StoredField : Field
    {
        public static readonly FieldType TYPE;
        static StoredField()
        {
            TYPE = new FieldType();
            TYPE.Stored = true;
            TYPE.Freeze();
        }

        public StoredField(String name, sbyte[] value)
            : base(name, value, TYPE)
        {
        }

        public StoredField(String name, sbyte[] value, int offset, int length)
            : base(name, value, offset, length, TYPE)
        {
        }

        public StoredField(String name, BytesRef value)
            : base(name, value, TYPE)
        {
        }

        public StoredField(String name, String value)
            : base(name, value, TYPE)
        {
        }

        public StoredField(String name, int value)
            : base(name, TYPE)
        {
            fieldsData = value;
        }

        public StoredField(String name, float value)
            : base(name, TYPE)
        {
            fieldsData = value;
        }

        public StoredField(String name, long value)
            : base(name, TYPE)
        {
            fieldsData = value;
        }

        public StoredField(String name, double value)
            : base(name, TYPE)
        {
            fieldsData = value;
        }
    }
}
