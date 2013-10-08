using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    public class FuzzyQueryNodeProcessor : QueryNodeProcessor
    {
        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            if (node is FuzzyQueryNode)
            {
                FuzzyQueryNode fuzzyNode = (FuzzyQueryNode)node;
                var config = QueryConfigHandler;

                FuzzyConfig fuzzyConfig = null;

                if (config != null && (fuzzyConfig = config.Get(StandardQueryConfigHandler.ConfigurationKeys.FUZZY_CONFIG)) != null)
                {
                    fuzzyNode.PrefixLength = fuzzyConfig.PrefixLength;

                    if (fuzzyNode.Similarity < 0)
                    {
                        fuzzyNode.Similarity = fuzzyConfig.MinSimilarity;
                    }

                }
                else if (fuzzyNode.Similarity < 0)
                {
                    throw new ArgumentException("No FUZZY_CONFIG set in the config");
                }
            }

            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
