using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public class TokenizedPhraseQueryNode : QueryNode, IFieldableNode
    {
        public TokenizedPhraseQueryNode()
        {
            SetLeaf(false);
            Allocate();
        }

        public override string ToString()
        {
            if (Children == null || Children.Count == 0)
                return "<tokenizedphrase/>";
            StringBuilder sb = new StringBuilder();
            sb.Append("<tokenizedtphrase>");
            foreach (IQueryNode child in Children)
            {
                sb.Append("\n");
                sb.Append(child.ToString());
            }
            sb.Append("\n</tokenizedphrase>");
            return sb.ToString();
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (Children == null || Children.Count == 0)
                return new StringCharSequenceWrapper("");

            StringBuilder sb = new StringBuilder();
            String filler = "";
            foreach (IQueryNode child in Children)
            {
                sb.Append(filler).Append(child.ToQueryString(escapeSyntaxParser));
                filler = ",";
            }

            return new StringCharSequenceWrapper("[TP[" + sb.ToString() + "]]");
        }

        public override IQueryNode CloneTree()
        {
            TokenizedPhraseQueryNode clone = (TokenizedPhraseQueryNode)base.CloneTree();

            // nothing to do

            return clone;
        }

        public string Field
        {
            get
            {
                IList<IQueryNode> children = Children;

                if (children == null || children.Count == 0)
                {
                    return null;

                }
                else
                {
                    return ((IFieldableNode)children[0]).Field;
                }
            }
            set
            {
                IList<IQueryNode> children = Children;

                if (children != null)
                {
                    foreach (IQueryNode child in Children)
                    {
                        if (child is IFieldableNode)
                        {
                            ((IFieldableNode)child).Field = value;
                        }
                    }
                }
            }
        }
    }
}
