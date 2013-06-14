using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;

namespace Lucene.Net.Documents
{
    public class TextField : Field
    {
        public static readonly FieldType TYPE_NOT_STORED = new FieldType();
        public static readonly FieldType TYPE_STORED = new FieldType();

        static TextField()
        {
            TYPE_NOT_STORED.Indexed = true;
            TYPE_NOT_STORED.Tokenized = true;
            TYPE_NOT_STORED.Freeze();

            TYPE_STORED.Indexed = true;
            TYPE_STORED.Tokenized = true;
            TYPE_STORED.Stored = true;
            TYPE_STORED.Freeze();
        }

        public TextField(String name, TextReader reader) : base(name, reader, TYPE_NOT_STORED)
        {
            
        }

        public TextField(String name, String value, Store store)
            : base(name, value, store == Store.YES ? TYPE_STORED : TYPE_NOT_STORED)
        {
            
        }

        public TextField(String name, TokenStream stream) : base(name, stream, TYPE_NOT_STORED)
        {
            
        }
    }
}
