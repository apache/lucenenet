using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A {@link AnyQueryNode} represents an ANY operator performed on a list of
    /// nodes.
    /// </summary>
    public class AnyQueryNode : AndQueryNode
    {
        private string field = null;
        private int minimumMatchingmElements = 0;

        /**
         * @param clauses
         *          - the query nodes to be or'ed
         */
        public AnyQueryNode(IList<IQueryNode> clauses, string field,
            int minimumMatchingElements)
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
                        if (clause is QueryNodeImpl)
                        {
                            ((QueryNodeImpl)clause).toQueryStringIgnoreFields = true;
                        }

                        if (clause is IFieldableNode)
                        {
                            ((IFieldableNode)clause).Field = field;
                        }
                    }
                }
            }
        }

        public virtual int MinimumMatchingElements
        {
            get { return this.minimumMatchingmElements; }
        }

        /// <summary>
        /// Gets or sets the field name. Returns null if the field was not specified.
        /// </summary>
        public virtual string Field
        {
            get { return this.field; }
            set { this.field = value; }
        }


        // LUCENENET TODO: No need for GetFieldAsString method because
        // field is already type string
        /**
         * returns - null if the field was not specified
         * 
         * @return the field as a String
         */
        public virtual string GetFieldAsString()
        {
            if (this.field == null)
                return null;
            else
                return this.field.ToString();
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
            var children = GetChildren();
            if (children == null || children.Count == 0)
                return "<any field='" + this.field + "'  matchelements="
                    + this.minimumMatchingmElements + "/>";
            StringBuilder sb = new StringBuilder();
            sb.Append("<any field='" + this.field + "'  matchelements="
                + this.minimumMatchingmElements + ">");
            foreach (IQueryNode clause in children)
            {
                sb.Append("\n");
                sb.Append(clause.ToString());
            }
            sb.Append("\n</any>");
            return sb.ToString();
        }

        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            string anySTR = "ANY " + this.minimumMatchingmElements;

            StringBuilder sb = new StringBuilder();
            var children = GetChildren();
            if (children == null || children.Count == 0)
            {
                // no childs case
            }
            else
            {
                string filler = "";
                foreach (IQueryNode clause in children)
                {
                    sb.Append(filler).Append(clause.ToQueryString(escapeSyntaxParser));
                    filler = " ";
                }
            }

            if (IsDefaultField(this.field))
            {
                return "( " + sb.ToString() + " ) " + anySTR;
            }
            else
            {
                return this.field + ":(( " + sb.ToString() + " ) " + anySTR + ")";
            }
        }
    }
}
