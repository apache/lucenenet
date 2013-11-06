using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Documents
{
    public class StringField : Field
    {
        public static readonly FieldType TYPE_NOT_STORED = new FieldType();
        public static readonly FieldType TYPE_STORED = new FieldType();

        static StringField()
        {
            TYPE_NOT_STORED.Indexed = true;
            TYPE_NOT_STORED.OmitNorms = true;
            TYPE_NOT_STORED.IndexOptions = FieldInfo.IndexOptions.DOCS_ONLY;
            TYPE_NOT_STORED.Tokenized = false;
            TYPE_NOT_STORED.Freeze();

            TYPE_STORED.Indexed = true;
            TYPE_STORED.OmitNorms = true;
            TYPE_STORED.IndexOptions = FieldInfo.IndexOptions.DOCS_ONLY;
            TYPE_STORED.Stored = true;
            TYPE_STORED.Tokenized = false;
            TYPE_STORED.Freeze();
        }

        public StringField(String name, String value, Store stored)
            : base(name, value, stored == Store.YES ? TYPE_STORED : TYPE_NOT_STORED)
        {
        }

    }
}
