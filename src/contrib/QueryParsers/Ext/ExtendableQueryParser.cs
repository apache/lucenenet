using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.QueryParsers.Ext
{
    public class ExtendableQueryParser : QueryParser
    {
        private readonly String defaultField;
        private readonly Extensions extensions;

        private static readonly Extensions DEFAULT_EXTENSION = new Extensions();

        public ExtendableQueryParser(Version matchVersion, string f, Analyzer a)
            : this(matchVersion, f, a, DEFAULT_EXTENSION)
        {
        }

        public ExtendableQueryParser(Version matchVersion, string f, Analyzer a, Extensions ext)
            : base(matchVersion, f, a)
        {
            this.defaultField = f;
            this.extensions = ext;
        }

        public char ExtensionFieldDelimiter
        {
            get
            {
                return extensions.ExtensionFieldDelimiter;
            }
        }

        protected override Query GetFieldQuery(string field, string queryText, bool quoted)
        {
            var splitExtensionField = this.extensions.SplitExtensionField(defaultField, field);
            ParserExtension extension = this.extensions.GetExtension(splitExtensionField.cud);
            if (extension != null)
            {
                return extension.Parse(new ExtensionQuery(this, splitExtensionField.cur, queryText));
            }
            return base.GetFieldQuery(field, queryText, quoted);
        }
    }
}
