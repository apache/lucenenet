using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public class OpaqueQueryNode : QueryNode
    {
        private string schema = null;

        private string value = null;

        public OpaqueQueryNode(string schema, string value)
        {
            this.SetLeaf(true);

            this.schema = schema;
            this.value = value;
        }

        public override string ToString()
        {
            return "<opaque schema='" + this.schema + "' value='" + this.value + "'/>";
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            return new StringCharSequenceWrapper("@" + this.schema + ":'" + this.value + "'");
        }

        public override IQueryNode CloneTree()
        {
            OpaqueQueryNode clone = (OpaqueQueryNode)base.CloneTree();

            // .NET Port: shouldn't this have already been done by MemberwiseClone()?
            clone.schema = this.schema;
            clone.value = this.value;

            return clone;
        }

        public string Schema
        {
            get { return schema; }
        }

        public string Value
        {
            get { return value; }
        }
    }
}
