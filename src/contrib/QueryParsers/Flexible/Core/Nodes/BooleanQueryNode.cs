using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public class BooleanQueryNode : QueryNode
    {
        public BooleanQueryNode(IList<IQueryNode> clauses)
        {
            SetLeaf(false);
            Allocate();
            Set(clauses);
        }

        public override string ToString()
        {
            if (Children == null || Children.Count == 0)
                return "<boolean operation='default'/>";
            StringBuilder sb = new StringBuilder();
            sb.Append("<boolean operation='default'>");
            foreach (IQueryNode child in Children)
            {
                sb.Append("\n");
                sb.Append(child.ToString());
            }
            sb.Append("\n</boolean>");
            return sb.ToString();
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (Children == null || Children.Count == 0)
                return new StringCharSequenceWrapper("");

            StringBuilder sb = new StringBuilder();
            String filler = "";
            foreach (QueryNode child in Children)
            {
                sb.Append(filler).Append(child.ToQueryString(escapeSyntaxParser));
                filler = " ";
            }

            // in case is root or the parent is a group node avoid parenthesis
            if ((Parent != null && Parent is GroupQueryNode) || IsRoot)
                return new StringCharSequenceWrapper(sb.ToString());
            else
                return new StringCharSequenceWrapper("( " + sb.ToString() + " )");
        }

        public override IQueryNode CloneTree()
        {
            BooleanQueryNode clone = (BooleanQueryNode)base.CloneTree();

            // nothing to do here

            return clone;
        }
    }
}
