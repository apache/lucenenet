using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Spans
{
    /// <summary>
    /// This processor changes every field name of each {@link FieldableNode} query
    /// node contained in the query tree to the field name defined in the
    /// {@link UniqueFieldAttribute}. So, the {@link UniqueFieldAttribute} must be
    /// defined in the {@link QueryConfigHandler} object set in this processor,
    /// otherwise it throws an exception.
    /// </summary>
    /// <seealso cref="UniqueFieldAttribute"/>
    public class UniqueFieldQueryNodeProcessor : QueryNodeProcessorImpl
    {
        protected override IQueryNode PostProcessNode(IQueryNode node)
        {

            return node;

        }


        protected override IQueryNode PreProcessNode(IQueryNode node)
        {

            if (node is IFieldableNode)
            {
                IFieldableNode fieldNode = (IFieldableNode)node;

                QueryConfigHandler queryConfig = GetQueryConfigHandler();

                if (queryConfig == null)
                {
                    throw new ArgumentException(
                        "A config handler is expected by the processor UniqueFieldQueryNodeProcessor!");
                }

                if (!queryConfig.Has(SpansQueryConfigHandler.UNIQUE_FIELD))
                {
                    throw new ArgumentException(
                        "UniqueFieldAttribute should be defined in the config handler!");
                }

                String uniqueField = queryConfig.Get(SpansQueryConfigHandler.UNIQUE_FIELD);
                fieldNode.Field = (uniqueField);

            }

            return node;

        }


        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {

            return children;

        }
    }
}
