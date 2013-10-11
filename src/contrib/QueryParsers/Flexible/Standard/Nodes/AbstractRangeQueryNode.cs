using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    public interface IAbstractRangeQueryNode : IFieldableNode
    {
        // .NET Port: non-generic marker interface

        bool IsLowerInclusive { get; }

        bool IsUpperInclusive { get; }
    }

    public class AbstractRangeQueryNode<T, TInner> : QueryNode, IRangeQueryNode<T, TInner>, IAbstractRangeQueryNode
        where T : IFieldValuePairQueryNode<TInner>
    {
        private bool lowerInclusive, upperInclusive;

        protected AbstractRangeQueryNode()
        {
            SetLeaf(false);
            Allocate();
        }
        
        public ICharSequence Field
        {
            get
            {
                ICharSequence field = null;
                T lower = LowerBound;
                T upper = UpperBound;

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
                T lower = LowerBound;
                T upper = UpperBound;

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

        public T LowerBound
        {
            get { return (T)Children[0]; }
        }

        public T UpperBound
        {
            get { return (T)Children[1]; }
        }

        public bool IsLowerInclusive
        {
            get { return lowerInclusive; }
        }

        public bool IsUpperInclusive
        {
            get { return upperInclusive; }
        }

        public void SetBounds(T lower, T upper, bool lowerInclusive, bool upperInclusive)
        {
            if (lower != null && upper != null)
            {
                String lowerField = StringUtils.ToString(lower.Field);
                String upperField = StringUtils.ToString(upper.Field);

                if ((upperField != null || lowerField != null)
                    && ((upperField != null && !upperField.Equals(lowerField)) || !lowerField.Equals(upperField)))
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

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            StringBuilder sb = new StringBuilder();

            T lower = LowerBound;
            T upper = UpperBound;

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

            return new StringCharSequenceWrapper(sb.ToString());    
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("<").Append(GetType().FullName);
            sb.Append(" lowerInclusive=").Append(IsLowerInclusive);
            sb.Append(" upperInclusive=").Append(IsUpperInclusive);
            sb.Append(">\n\t");
            sb.Append(UpperBound).Append("\n\t");
            sb.Append(LowerBound).Append("\n");
            sb.Append("</").Append(GetType().FullName).Append(">\n");

            return sb.ToString();
        }
    }
}
