using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    public class MultiTermRewriteMethodProcessor : QueryNodeProcessor
    {
        public const string TAG_ID = "MultiTermRewriteMethodConfiguration";

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            // set setMultiTermRewriteMethod for WildcardQueryNode and
            // PrefixWildcardQueryNode
            if (node is WildcardQueryNode
                || node is IAbstractRangeQueryNode || node is RegexpQueryNode)
            {

                MultiTermQuery.RewriteMethod rewriteMethod = QueryConfigHandler.Get(StandardQueryConfigHandler.ConfigurationKeys.MULTI_TERM_REWRITE_METHOD);

                if (rewriteMethod == null)
                {
                    // This should not happen, this configuration is set in the
                    // StandardQueryConfigHandler
                    throw new ArgumentException(
                        "StandardQueryConfigHandler.ConfigurationKeys.MULTI_TERM_REWRITE_METHOD should be set on the QueryConfigHandler");
                }

                // use a TAG to take the value to the Builder
                node.SetTag(MultiTermRewriteMethodProcessor.TAG_ID, rewriteMethod);

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
