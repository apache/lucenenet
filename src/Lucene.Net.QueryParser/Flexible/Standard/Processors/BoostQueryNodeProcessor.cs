using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
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
    /// {@link FieldableNode} that has {@link ConfigurationKeys#BOOST} in its
    /// config. If there is, the boost is applied to that {@link FieldableNode}.
    /// </summary>
    /// <seealso cref="ConfigurationKeys#BOOST"/>
    /// <seealso cref="QueryConfigHandler"/>
    /// <seealso cref="IFieldableNode"/>
    public class BoostQueryNodeProcessor : QueryNodeProcessorImpl
    {
        protected override IQueryNode PostProcessNode(IQueryNode node)
        {

            if (node is IFieldableNode &&
                (node.GetParent() == null || !(node.GetParent() is IFieldableNode)))
            {

                IFieldableNode fieldNode = (IFieldableNode)node;
                QueryConfigHandler config = GetQueryConfigHandler();

                if (config != null)
                {
                    string field = fieldNode.Field;
                    FieldConfig fieldConfig = config.GetFieldConfig(StringUtils.ToString(field));

                    if (fieldConfig != null)
                    {
                        float? boost = fieldConfig.Get(ConfigurationKeys.BOOST);

                        if (boost != null)
                        {
                            return new BoostQueryNode(node, boost.Value);
                        }

                    }

                }

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
