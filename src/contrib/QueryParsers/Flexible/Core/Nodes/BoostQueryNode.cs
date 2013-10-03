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
    public class BoostQueryNode : QueryNode
    {
        private float value = 0;

        public BoostQueryNode(IQueryNode query, float value)
        {
            if (query == null)
            {
                throw new QueryNodeError(new Message(QueryParserMessages.NODE_ACTION_NOT_SUPPORTED, "query", "null"));
            }

            this.value = value;
            SetLeaf(false);
            Allocate();
            Add(query);
        }

        public IQueryNode Child
        {
            get
            {
                IList<IQueryNode> children = Children;

                if (children == null || children.Count == 0)
                {
                    return null;
                }

                return children[0];
            }
        }

        public float Value
        {
            get
            {
                return this.value;
            }
        }

        private string ValueString
        {
            get
            {
                if (value == (long)value)
                    return "" + (long)value;
                else
                    return "" + value;
            }
        }

        public override string ToString()
        {
            return "<boost value='" + ValueString + "'>" + "\n"
                + Child.ToString() + "\n</boost>";
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (Child == null)
                return new StringCharSequenceWrapper("");

            return new StringCharSequenceWrapper(Child.ToQueryString(escapeSyntaxParser) + "^" + ValueString);
        }

        public override IQueryNode CloneTree()
        {
            BoostQueryNode clone = (BoostQueryNode)base.CloneTree();

            clone.value = this.value;

            return clone;
        }
    }
}
