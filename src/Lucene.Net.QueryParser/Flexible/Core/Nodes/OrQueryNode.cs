using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A {@link OrQueryNode} represents an OR boolean operation performed on a list
    /// of nodes.
    /// </summary>
    public class OrQueryNode : BooleanQueryNode
    {
        /**
   * @param clauses
   *          - the query nodes to be or'ed
   */
        public OrQueryNode(IList<IQueryNode> clauses)
            : base(clauses)
        {
            if ((clauses == null) || (clauses.Count == 0))
            {
                throw new ArgumentException(
                    "OR query must have at least one clause");
            }
        }

        public override string ToString()
        {
            var children = GetChildren();
            if (children == null || children.Count == 0)
                return "<boolean operation='or'/>";
            StringBuilder sb = new StringBuilder();
            sb.Append("<boolean operation='or'>");
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
            for (IEnumerator<IQueryNode> it = children.GetEnumerator(); it.MoveNext();)
            {
                sb.Append(filler).Append(it.Current.ToQueryString(escapeSyntaxParser));
                filler = " OR ";
            }

            // in case is root or the parent is a group node avoid parenthesis
            if ((Parent != null && Parent is GroupQueryNode)
                || IsRoot)
                return sb.ToString();
            else
                return "( " + sb.ToString() + " )";
        }
    }
}
