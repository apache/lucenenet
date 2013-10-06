using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public class GroupQueryNode : QueryNode
    {
        public GroupQueryNode(IQueryNode query)
        {
            if (query == null)
            {
                throw new QueryNodeError(new Message(QueryParserMessages.PARAMETER_VALUE_NOT_SUPPORTED, "query", "null"));
            }

            Allocate();
            SetLeaf(false);
            Add(query);
        }

        public IQueryNode Child
        {
            get
            {
                return Children[0];
            }
            set
            {
                IList<IQueryNode> list = new List<IQueryNode>();
                list.Add(value);
                this.Set(list);
            }
        }

        public override string ToString()
        {
            return "<group>" + "\n" + Child.ToString() + "\n</group>";
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (Child == null)
                return new StringCharSequenceWrapper("");

            return new StringCharSequenceWrapper("( " + Child.ToQueryString(escapeSyntaxParser) + " )");
        }

        public override IQueryNode CloneTree()
        {
            GroupQueryNode clone = (GroupQueryNode)base.CloneTree();

            return clone;
        }
    }
}
