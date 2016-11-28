using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Processors;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /// <summary>
    /// Builds a {@link TermRangeQuery} object from a {@link TermRangeQueryNode}
    /// object.
    /// </summary>
    public class TermRangeQueryNodeBuilder : IStandardQueryBuilder
    {
        public TermRangeQueryNodeBuilder()
        {
            // empty constructor
        }


        public virtual Query Build(IQueryNode queryNode)
        {
            TermRangeQueryNode rangeNode = (TermRangeQueryNode)queryNode;
            FieldQueryNode upper = (FieldQueryNode)rangeNode.UpperBound;
            FieldQueryNode lower = (FieldQueryNode)rangeNode.LowerBound;

            string field = StringUtils.ToString(rangeNode.Field);
            string lowerText = lower.GetTextAsString();
            string upperText = upper.GetTextAsString();

            if (lowerText.Length == 0)
            {
                lowerText = null;
            }

            if (upperText.Length == 0)
            {
                upperText = null;
            }

            TermRangeQuery rangeQuery = TermRangeQuery.NewStringRange(field, lowerText, upperText, rangeNode
                .IsLowerInclusive, rangeNode.IsUpperInclusive);

            MultiTermQuery.RewriteMethod method = (MultiTermQuery.RewriteMethod)queryNode
                .GetTag(MultiTermRewriteMethodProcessor.TAG_ID);
            if (method != null)
            {
                rangeQuery.SetRewriteMethod(method);
            }

            return rangeQuery;

        }

        /// <summary>
        /// LUCENENET specific overload for supporting IQueryBuilder
        /// </summary>
        object IQueryBuilder.Build(IQueryNode queryNode)
        {
            return Build(queryNode);
        }
    }
}
