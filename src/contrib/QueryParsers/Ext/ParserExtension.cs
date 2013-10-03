using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Ext
{
    public abstract class ParserExtension
    {
        public abstract Query Parse(ExtensionQuery query);
    }
}
