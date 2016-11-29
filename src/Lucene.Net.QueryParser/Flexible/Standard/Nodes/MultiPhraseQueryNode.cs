using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    /// <summary>
    /// A {@link MultiPhraseQueryNode} indicates that its children should be used to
    /// build a {@link MultiPhraseQuery} instead of {@link PhraseQuery}.
    /// </summary>
    public class MultiPhraseQueryNode : QueryNodeImpl, IFieldableNode
    {
        public MultiPhraseQueryNode()
        {
            SetLeaf(false);
            Allocate();

        }

        public override string ToString()
        {
            var children = GetChildren();
            if (children == null || children.Count == 0)
                return "<multiPhrase/>";
            StringBuilder sb = new StringBuilder();
            sb.Append("<multiPhrase>");
            foreach (IQueryNode child in children)
            {
                sb.Append("\n");
                sb.Append(child.ToString());
            }
            sb.Append("\n</multiPhrase>");
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
                filler = ",";
            }

            return "[MTP[" + sb.ToString() + "]]";
        }


        public override IQueryNode CloneTree()
        {
            MultiPhraseQueryNode clone = (MultiPhraseQueryNode)base.CloneTree();

            // nothing to do

            return clone;
        }


        public virtual string Field
        {
            get
            {
                IList<IQueryNode> children = GetChildren();

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
                IList<IQueryNode> children = GetChildren();

                if (children != null)
                {

                    foreach (IQueryNode child in children)
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
