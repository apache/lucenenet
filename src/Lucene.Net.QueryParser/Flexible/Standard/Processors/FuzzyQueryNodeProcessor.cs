using Lucene.Net.QueryParsers.Flexible.Core.Config;
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
    /// <summary>
    /// This processor iterates the query node tree looking for every
    /// {@link FuzzyQueryNode}, when this kind of node is found, it checks on the
    /// query configuration for
    /// {@link ConfigurationKeys#FUZZY_CONFIG}, gets the
    /// fuzzy prefix length and default similarity from it and set to the fuzzy node.
    /// For more information about fuzzy prefix length check: {@link FuzzyQuery}.
    /// </summary>
    /// <seealso cref="ConfigurationKeys#FUZZY_CONFIG"/>
    /// <seealso cref="FuzzyQuery"/>
    /// <seealso cref="FuzzyQueryNode"/>
    public class FuzzyQueryNodeProcessor : QueryNodeProcessorImpl
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
                QueryConfigHandler config = GetQueryConfigHandler();

                FuzzyConfig fuzzyConfig = null;

                if (config != null && (fuzzyConfig = config.Get(ConfigurationKeys.FUZZY_CONFIG)) != null)
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
