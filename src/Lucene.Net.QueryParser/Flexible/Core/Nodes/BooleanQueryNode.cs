using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A {@link BooleanQueryNode} represents a list of elements which do not have an
    /// explicit boolean operator defined between them. It can be used to express a
    /// boolean query that intends to use the default boolean operator.
    /// </summary>
    public class BooleanQueryNode : QueryNodeImpl
    {
        /**
        * @param clauses
        *          - the query nodes to be and'ed
        */
        public BooleanQueryNode(IList<IQueryNode> clauses)
        {
            SetLeaf(false);
            Allocate();
            Set(clauses);
        }


        public override string ToString()
        {
            var children = GetChildren();
            if (children == null || children.Count == 0)
                return "<boolean operation='default'/>";
            StringBuilder sb = new StringBuilder();
            sb.Append("<boolean operation='default'>");
            foreach (IQueryNode child in children)
            {
                sb.Append("\n");
                sb.Append(child.ToString());
            }
            sb.Append("\n</boolean>");
            return sb.ToString();
        }


        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            var children = GetChildren();
            if (children == null || children.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder();
            string filler = "";
            foreach (IQueryNode child in children)
            {
                sb.Append(filler).Append(child.ToQueryString(escapeSyntaxParser));
                filler = " ";
            }

            // in case is root or the parent is a group node avoid parenthesis
            if ((Parent != null && Parent is GroupQueryNode)
                || IsRoot)
                return sb.ToString();
            else
                return "( " + sb.ToString() + " )";
        }


        public override IQueryNode CloneTree()
        {
            BooleanQueryNode clone = (BooleanQueryNode)base.CloneTree();

            // nothing to do here

            return clone;
        }

    }
}
