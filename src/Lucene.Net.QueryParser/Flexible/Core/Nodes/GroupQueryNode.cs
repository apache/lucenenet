using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A {@link GroupQueryNode} represents a location where the original user typed
    /// real parenthesis on the query string. This class is useful for queries like:
    /// a) a AND b OR c b) ( a AND b) OR c
    /// 
    /// Parenthesis might be used to define the boolean operation precedence.
    /// </summary>
    public class GroupQueryNode : QueryNodeImpl
    {
        /**
   * This QueryNode is used to identify parenthesis on the original query string
   */
        public GroupQueryNode(IQueryNode query)
        {
            if (query == null)
            {
                throw new QueryNodeError(new MessageImpl(
                    QueryParserMessages.PARAMETER_VALUE_NOT_SUPPORTED, "query", "null"));
            }

            Allocate();
            SetLeaf(false);
            Add(query);
        }

        public virtual IQueryNode GetChild()
        {
            return GetChildren()[0];
        }


        public override string ToString()
        {
            return "<group>" + "\n" + GetChild().ToString() + "\n</group>";
        }


        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (GetChild() == null)
                return "";

            return "( " + GetChild().ToQueryString(escapeSyntaxParser) + " )";
        }


        public override IQueryNode CloneTree()
        {
            GroupQueryNode clone = (GroupQueryNode)base.CloneTree();

            return clone;
        }

        public virtual void SetChild(IQueryNode child)
        {
            List<IQueryNode> list = new List<IQueryNode>();
            list.Add(child);
            this.Set(list);
        }

    }
}
