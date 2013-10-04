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
    public class SlopQueryNode : QueryNode, IFieldableNode
    {
        private int value = 0;

        public SlopQueryNode(IQueryNode query, int value)
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
                return Children[0];
            }
        }

        public int Value
        {
            get { return value; }
        }

        public string ValueString
        {
            get { return value.ToString(); }
        }

        public override string ToString()
        {
            return "<slop value='" + ValueString + "'>" + "\n" + Child.ToString() + "\n</slop>";
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (Child == null)
                return new StringCharSequenceWrapper("");
            return new StringCharSequenceWrapper(Child.ToQueryString(escapeSyntaxParser) + "~" + ValueString);
        }

        public override IQueryNode CloneTree()
        {
            SlopQueryNode clone = (SlopQueryNode)base.CloneTree();

            clone.value = this.value;

            return clone;
        }

        public string Field
        {
            get
            {
                IQueryNode child = Child;

                if (child is IFieldableNode)
                {
                    return ((IFieldableNode)child).Field;
                }

                return null;
            }
            set
            {
                IQueryNode child = Child;

                if (child is IFieldableNode)
                {
                    ((IFieldableNode)child).Field = value;
                }
            }
        }
    }
}
