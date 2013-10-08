using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    public class BoostQueryNodeProcessor : QueryNodeProcessor
    {
        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is IFieldableNode &&
                (node.Parent == null || !(node.Parent is IFieldableNode)))
            {

                IFieldableNode fieldNode = (IFieldableNode)node;
                var config = QueryConfigHandler;

                if (config != null)
                {
                    ICharSequence field = fieldNode.Field;
                    FieldConfig fieldConfig = config.GetFieldConfig(StringUtils.ToString(field));

                    if (fieldConfig != null)
                    {
                        float? boost = fieldConfig.Get(StandardQueryConfigHandler.ConfigurationKeys.BOOST);

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
