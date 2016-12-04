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
    /// A {@link SlopQueryNode} represents phrase query with a slop.
    /// 
    /// From Lucene FAQ: Is there a way to use a proximity operator (like near or
    /// within) with Lucene? There is a variable called slop that allows you to
    /// perform NEAR/WITHIN-like queries. By default, slop is set to 0 so that only
    /// exact phrases will match. When using TextParser you can use this syntax to
    /// specify the slop: "doug cutting"~2 will find documents that contain
    /// "doug cutting" as well as ones that contain "cutting doug".
    /// </summary>
    public class SlopQueryNode : QueryNodeImpl, IFieldableNode
    {
        private int value = 0;

        /**
         * @param query
         *          - QueryNode Tree with the phrase
         * @param value
         *          - slop value
         */
        public SlopQueryNode(IQueryNode query, int value)
        {
            if (query == null)
            {
                throw new QueryNodeError(new MessageImpl(
                    QueryParserMessages.NODE_ACTION_NOT_SUPPORTED, "query", "null"));
            }

            this.value = value;
            IsLeaf = false;
            Allocate();
            Add(query);
        }

        public virtual IQueryNode GetChild()
        {
            return GetChildren()[0];
        }

        public virtual int Value
        {
            get { return this.value; }
        }

        private string GetValueString()
        {
            float f = this.value;
            if (f == (long)f)
                return "" + (long)f;
            else
                return "" + f;
        }

        public override string ToString()
        {
            return "<slop value='" + GetValueString() + "'>" + "\n"
                + GetChild().ToString() + "\n</slop>";
        }

        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (GetChild() == null)
                return "";
            return GetChild().ToQueryString(escapeSyntaxParser) + "~"
                + GetValueString();
        }

        public override IQueryNode CloneTree()
        {
            SlopQueryNode clone = (SlopQueryNode)base.CloneTree();

            clone.value = this.value;

            return clone;
        }

        public virtual string Field
        {
            get
            {
                IQueryNode child = GetChild();

                if (child is IFieldableNode)
                {
                    return ((IFieldableNode)child).Field;
                }

                return null;
            }
            set
            {
                IQueryNode child = GetChild();

                if (child is IFieldableNode)
                {
                    ((IFieldableNode)child).Field = value;
                }
            }
        }
    }
}
