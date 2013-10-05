using Lucene.Net.QueryParsers.Classic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Ext
{
    public class ExtensionQuery
    {
        private readonly string field;
        private readonly string rawQueryString;
        private readonly QueryParser topLevelParser;

        public ExtensionQuery(QueryParser topLevelParser, string field, string rawQueryString)
        {
            this.field = field;
            this.rawQueryString = rawQueryString;
            this.topLevelParser = topLevelParser;
        }

        public string Field
        {
            get
            {
                return field;
            }
        }

        public string RawQueryString
        {
            get
            {
                return rawQueryString;
            }
        }

        public QueryParser TopLevelParser
        {
            get
            {
                return topLevelParser;
            }
        }
    }
}
