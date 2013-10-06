using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Ext
{
    public class Extensions
    {
        private readonly IDictionary<string, ParserExtension> extensions = new HashMap<string, ParserExtension>();
        private readonly char extensionFieldDelimiter;

        public const char DEFAULT_EXTENSION_FIELD_DELIMITER = ':';

        public Extensions()
            : this(DEFAULT_EXTENSION_FIELD_DELIMITER)
        {
        }

        public Extensions(char extensionFieldDelimiter)
        {
            this.extensionFieldDelimiter = extensionFieldDelimiter;
        }

        public virtual void Add(string key, ParserExtension extension)
        {
            this.extensions[key] = extension;
        }

        public ParserExtension GetExtension(string key)
        {
            return this.extensions[key];
        }

        public char ExtensionFieldDelimiter
        {
            get
            {
                return extensionFieldDelimiter;
            }
        }

        public Pair<string, string> SplitExtensionField(string defaultField, string field)
        {
            int indexOf = field.IndexOf(this.extensionFieldDelimiter);
            if (indexOf < 0)
                return new Pair<string, string>(field, null);
            string indexField = indexOf == 0 ? defaultField : field.Substring(0, indexOf);
            string extensionKey = field.Substring(indexOf + 1);
            return new Pair<string, string>(indexField, extensionKey);
        }

        public string EscapeExtensionField(string extfield)
        {
            return QueryParserBase.Escape(extfield);
        }

        public string BuildExtensionField(string extensionKey)
        {
            return BuildExtensionField(extensionKey, "");
        }

        public string BuildExtensionField(string extensionKey, string field)
        {
            StringBuilder builder = new StringBuilder(field);
            builder.Append(this.extensionFieldDelimiter);
            builder.Append(extensionKey);
            return EscapeExtensionField(builder.ToString());
        }

        public class Pair<Cur, Cud>
        {
            // .NET Port: We could use Tuple<T1, T2> instead of this, but I ported this class
            // so that there wouldn't be any confusion as to which of {cur,cud} comes first.

            public readonly Cur cur;
            public readonly Cud cud;

            public Pair(Cur cur, Cud cud)
            {
                this.cur = cur;
                this.cud = cud;
            }
        }
    }
}
