using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public class DeletedQueryNode : QueryNode
    {
        public DeletedQueryNode()
        {
            // empty constructor
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            return new StringCharSequenceWrapper("[DELETEDCHILD]");
        }

        public override string ToString()
        {
            return "<deleted/>";
        }

        public override IQueryNode CloneTree()
        {
            DeletedQueryNode clone = (DeletedQueryNode)base.CloneTree();

            return clone;
        }
    }
}
