using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Support;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    /// <summary>
    /// Processes {@link TermRangeQuery}s with open ranges.
    /// </summary>
    public class OpenRangeQueryNodeProcessor : QueryNodeProcessorImpl
    {
        public readonly static string OPEN_RANGE_TOKEN = "*";

        public OpenRangeQueryNodeProcessor() { }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is TermRangeQueryNode)
            {
                TermRangeQueryNode rangeNode = (TermRangeQueryNode)node;
                FieldQueryNode lowerNode = (FieldQueryNode)rangeNode.LowerBound;
                FieldQueryNode upperNode = (FieldQueryNode)rangeNode.UpperBound;
                ICharSequence lowerText = lowerNode.Text;
                ICharSequence upperText = upperNode.Text;

                if (OPEN_RANGE_TOKEN.Equals(upperNode.GetTextAsString())
                    && (!(upperText is UnescapedCharSequence) || !((UnescapedCharSequence)upperText)
                        .WasEscaped(0)))
                {
                    upperText = "".ToCharSequence();
                }

                if (OPEN_RANGE_TOKEN.Equals(lowerNode.GetTextAsString())
                    && (!(lowerText is UnescapedCharSequence) || !((UnescapedCharSequence)lowerText)
                        .WasEscaped(0)))
                {
                    lowerText = "".ToCharSequence();
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
