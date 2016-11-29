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
    /// Query node for <see cref="PhraseQuery"/>'s slop factor.
    /// </summary>
    public class PhraseSlopQueryNode : QueryNodeImpl, IFieldableNode
    {
        private int value = 0;

        /**
         * @exception QueryNodeError throw in overridden method to disallow
         */
        public PhraseSlopQueryNode(IQueryNode query, int value)
        {
            if (query == null)
            {
                throw new QueryNodeError(new MessageImpl(
                    QueryParserMessages.NODE_ACTION_NOT_SUPPORTED, "query", "null"));
            }

            this.value = value;
            SetLeaf(false);
            Allocate();
            Add(query);
        }

        public virtual IQueryNode GetChild()
        {
            return GetChildren()[0];
        }

        public virtual int GetValue()
        {
            return this.value;
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
            return "<phraseslop value='" + GetValueString() + "'>" + "\n"
                + GetChild().ToString() + "\n</phraseslop>";
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
            PhraseSlopQueryNode clone = (PhraseSlopQueryNode)base.CloneTree();

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
