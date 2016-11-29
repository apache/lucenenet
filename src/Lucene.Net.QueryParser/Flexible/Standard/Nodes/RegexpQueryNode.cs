using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    /// <summary>
    /// A {@link RegexpQueryNode} represents {@link RegexpQuery} query Examples: /[a-z]|[0-9]/
    /// </summary>
    public class RegexpQueryNode : QueryNodeImpl, ITextableQueryNode, IFieldableNode
    {
        private ICharSequence text;
        private string field;

        /**
         * @param field
         *          - field name
         * @param text
         *          - value that contains a regular expression
         * @param begin
         *          - position in the query string
         * @param end
         *          - position in the query string
         */
         // LUCENENET specific overload for passing text as string
        public RegexpQueryNode(string field, string text, int begin,
            int end)
            : this(field, new StringCharSequenceWrapper(text), begin, end)
        {
        }

        /**
         * @param field
         *          - field name
         * @param text
         *          - value that contains a regular expression
         * @param begin
         *          - position in the query string
         * @param end
         *          - position in the query string
         */
        public RegexpQueryNode(string field, ICharSequence text, int begin,
            int end)
        {
            this.field = field;
            this.text = text.SubSequence(begin, end);
        }

        public virtual BytesRef TextToBytesRef()
        {
            return new BytesRef(text.ToString());
        }


        public override string ToString()
        {
            return "<regexp field='" + this.field + "' term='" + this.text + "'/>";
        }


        public override IQueryNode CloneTree()
        {
            RegexpQueryNode clone = (RegexpQueryNode)base.CloneTree();
            clone.field = this.field;
            clone.text = this.text;
            return clone;
        }


        public virtual ICharSequence Text
        {
            get { return text; }
            set { this.text = value; }
        }

        public virtual string Field
        {
            get { return field; }
            set { this.field = value; }
        }

        public virtual string GetFieldAsString()
        {
            return field.ToString();
        }

        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            return IsDefaultField(field) ? "/" + text + "/" : field + ":/" + text + "/";
        }

    }
}
