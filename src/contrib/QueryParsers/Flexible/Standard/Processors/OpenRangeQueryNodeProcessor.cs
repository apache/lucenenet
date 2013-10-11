using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    public class OpenRangeQueryNodeProcessor : QueryNodeProcessor
    {
        public const string OPEN_RANGE_TOKEN = "*";

        public OpenRangeQueryNodeProcessor() { }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is TermRangeQueryNode)
            {
                TermRangeQueryNode rangeNode = (TermRangeQueryNode)node;
                FieldQueryNode lowerNode = rangeNode.LowerBound;
                FieldQueryNode upperNode = rangeNode.UpperBound;
                ICharSequence lowerText = lowerNode.Text;
                ICharSequence upperText = upperNode.Text;

                if (OPEN_RANGE_TOKEN.Equals(upperNode.TextAsString)
                    && (!(upperText is UnescapedCharSequence) || !((UnescapedCharSequence)upperText)
                        .WasEscaped(0)))
                {
                    upperText = StringCharSequenceWrapper.Empty;
                }

                if (OPEN_RANGE_TOKEN.Equals(lowerNode.TextAsString)
                    && (!(lowerText is UnescapedCharSequence) || !((UnescapedCharSequence)lowerText)
                        .WasEscaped(0)))
                {
                    lowerText = StringCharSequenceWrapper.Empty;
                }

                lowerNode.Text = lowerText;
                upperNode.Text = upperText;
            }

            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
