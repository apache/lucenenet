using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public class AnyQueryNode : AndQueryNode
    {
        private ICharSequence field = null;
        private int minimumMatchingmElements = 0;

        public AnyQueryNode(IList<IQueryNode> clauses, ICharSequence field, int minimumMatchingElements)
            : base(clauses)
        {
            this.field = field;
            this.minimumMatchingmElements = minimumMatchingElements;

            if (clauses != null)
            {
                foreach (IQueryNode clause in clauses)
                {
                    if (clause is FieldQueryNode)
                    {
                        if (clause is QueryNode)
                        {
                            ((QueryNode)clause).toQueryStringIgnoreFields = true;
                        }

                        if (clause is IFieldableNode)
                        {
                            ((IFieldableNode)clause).Field = field;
                        }
                    }
                }
            }
        }

        public int MinimumMatchingElements
        {
            get
            {
                return this.minimumMatchingmElements;
            }
        }

        public ICharSequence Field
        {
            get
            {
                return this.field;
            }
            set
            {
                this.field = value;
            }
        }

        public string FieldAsString
        {
            get
            {
                if (this.field == null)
                    return null;
                else
                    return this.field.ToString();
            }
        }

        public override IQueryNode CloneTree()
        {
            AnyQueryNode clone = (AnyQueryNode)base.CloneTree();

            clone.field = this.field;
            clone.minimumMatchingmElements = this.minimumMatchingmElements;

            return clone;
        }

        public override string ToString()
        {
            if (Children == null || Children.Count == 0)
                return "<any field='" + this.field + "'  matchelements="
                    + this.minimumMatchingmElements + "/>";
            StringBuilder sb = new StringBuilder();
            sb.Append("<any field='" + this.field + "'  matchelements="
                + this.minimumMatchingmElements + ">");
            foreach (IQueryNode clause in Children)
            {
                sb.Append("\n");
                sb.Append(clause.ToString());
            }
            sb.Append("\n</any>");
            return sb.ToString();
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            String anySTR = "ANY " + this.minimumMatchingmElements;

            StringBuilder sb = new StringBuilder();
            if (Children == null || Children.Count == 0)
            {
                // no childs case
            }
            else
            {
                String filler = "";
                foreach (IQueryNode clause in Children)
                {
                    sb.Append(filler).Append(clause.ToQueryString(escapeSyntaxParser));
                    filler = " ";
                }
            }

            if (IsDefaultField(this.field))
            {
                return new StringCharSequenceWrapper("( " + sb.ToString() + " ) " + anySTR);
            }
            else
            {
                return new StringCharSequenceWrapper(this.field + ":(( " + sb.ToString() + " ) " + anySTR + ")");
            }
        }
    }
}
