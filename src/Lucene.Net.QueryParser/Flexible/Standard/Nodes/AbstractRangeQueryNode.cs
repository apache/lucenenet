using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    /// <summary>
    /// This class should be extended by nodes intending to represent range queries.
    /// </summary>
    /// <typeparam name="T">the type of the range query bounds (lower and upper)</typeparam>
    public class AbstractRangeQueryNode<T> : QueryNodeImpl, IRangeQueryNode<IFieldableNode>, IAbstractRangeQueryNode where T : IFieldableNode /*IFieldValuePairQueryNode<?>*/
    { /*IRangeQueryNode<IFieldValuePairQueryNode<?>>*/

        private bool lowerInclusive, upperInclusive;

        /**
         * Constructs an {@link AbstractRangeQueryNode}, it should be invoked only by
         * its extenders.
         */
        protected AbstractRangeQueryNode()
        {
            SetLeaf(false);
            Allocate();
        }

        /**
         * Gets or Sets the field associated with this node.
         * 
         * @return the field associated with this node
         * 
         * @see FieldableNode
         */

        public virtual string Field
        {
            get
            {
                string field = null;
                T lower = (T)LowerBound;
                T upper = (T)UpperBound;

                if (lower != null)
                {
                    field = lower.Field;

                }
                else if (upper != null)
                {
                    field = upper.Field;
                }

                return field;
            }
            set
            {
                T lower = (T)LowerBound;
                T upper = (T)UpperBound;

                if (lower != null)
                {
                    lower.Field = value;
                }

                if (upper != null)
                {
                    upper.Field = value;
                }
            }
        }

        /**
         * Sets the field associated with this node.
         * 
         * @param fieldName the field associated with this node
         */
        //      @Override
        //public void setField(CharSequence fieldName)
        //      {
        //          T lower = getLowerBound();
        //          T upper = getUpperBound();

        //          if (lower != null)
        //          {
        //              lower.setField(fieldName);
        //          }

        //          if (upper != null)
        //          {
        //              upper.setField(fieldName);
        //          }

        //      }

        /**
         * Returns the lower bound node.
         * 
         * @return the lower bound node.
         */
        public virtual IFieldableNode LowerBound
        {
            get { return (IFieldableNode)GetChildren()[0]; }
        }

        /**
         * Returns the upper bound node.
         * 
         * @return the upper bound node.
         */
        public virtual IFieldableNode UpperBound
        {
            get { return (IFieldableNode)GetChildren()[1]; }
        }

        /**
         * Returns whether the lower bound is inclusive or exclusive.
         * 
         * @return <code>true</code> if the lower bound is inclusive, otherwise, <code>false</code>
         */

        public virtual bool IsLowerInclusive
        {
            get { return lowerInclusive; }
        }

        /**
         * Returns whether the upper bound is inclusive or exclusive.
         * 
         * @return <code>true</code> if the upper bound is inclusive, otherwise, <code>false</code>
         */
        public virtual bool IsUpperInclusive
        {
            get { return upperInclusive; }
        }

        /**
         * Sets the lower and upper bounds.
         * 
         * @param lower the lower bound, <code>null</code> if lower bound is open
         * @param upper the upper bound, <code>null</code> if upper bound is open
         * @param lowerInclusive <code>true</code> if the lower bound is inclusive, otherwise, <code>false</code>
         * @param upperInclusive <code>true</code> if the upper bound is inclusive, otherwise, <code>false</code>
         * 
         * @see #getLowerBound()
         * @see #getUpperBound()
         * @see #isLowerInclusive()
         * @see #isUpperInclusive()
         */
        public virtual void SetBounds(T lower, T upper, bool lowerInclusive,
            bool upperInclusive)
        {

            if (lower != null && upper != null)
            {
                string lowerField = StringUtils.ToString(lower.Field);
                string upperField = StringUtils.ToString(upper.Field);

                if ((upperField != null || lowerField != null)
                    && ((upperField != null && !upperField.Equals(lowerField)) || !lowerField
                        .Equals(upperField)))
                {
                    throw new ArgumentException(
                        "lower and upper bounds should have the same field name!");
                }

                this.lowerInclusive = lowerInclusive;
                this.upperInclusive = upperInclusive;

                List<IQueryNode> children = new List<IQueryNode>(2);
                children.Add(lower);
                children.Add(upper);

                Set(children);

            }

        }


        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            StringBuilder sb = new StringBuilder();

            T lower = (T)LowerBound;
            T upper = (T)UpperBound;

            if (lowerInclusive)
            {
                sb.Append('[');

            }
            else
            {
                sb.Append('{');
            }

            if (lower != null)
            {
                sb.Append(lower.ToQueryString(escapeSyntaxParser));

            }
            else
            {
                sb.Append("...");
            }

            sb.Append(' ');

            if (upper != null)
            {
                sb.Append(upper.ToQueryString(escapeSyntaxParser));

            }
            else
            {
                sb.Append("...");
            }

            if (upperInclusive)
            {
                sb.Append(']');

            }
            else
            {
                sb.Append('}');
            }

            return sb.ToString();

        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("<").Append(GetType().AssemblyQualifiedName);
            sb.Append(" lowerInclusive=").Append(IsLowerInclusive);
            sb.Append(" upperInclusive=").Append(IsUpperInclusive);
            sb.Append(">\n\t");
            sb.Append(UpperBound).Append("\n\t");
            sb.Append(LowerBound).Append("\n");
            sb.Append("</").Append(GetType().AssemblyQualifiedName).Append(">\n");

            return sb.ToString();

        }
    }

    /// <summary>
    /// LUCENENET specific interface used to identify
    /// an AbstractRangeQueryNode without referring to 
    /// its generic closing type
    /// </summary>
    public interface IAbstractRangeQueryNode
    { }
}
