using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Messages;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Spans
{
    /// <summary>
    /// Validates every query node in a query node tree. This processor will pass
    /// fine if the query nodes are only <see cref="BooleanQueryNode"/>s,
    /// <see cref="OrQueryNode"/>s or <see cref="FieldQueryNode"/>s, otherwise an exception will
    /// be thrown.
    /// <para/>
    /// If they are <see cref="AndQueryNode"/> or an instance of anything else that
    /// implements <see cref="FieldQueryNode"/> the exception will also be thrown.
    /// </summary>
    public class SpansValidatorQueryNodeProcessor : QueryNodeProcessorImpl
    {
        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            if (!((node is BooleanQueryNode && !(node is AndQueryNode)) || node
                .GetType() == typeof(FieldQueryNode)))
            {
                throw new QueryNodeException(new MessageImpl(
                    QueryParserMessages.NODE_ACTION_NOT_SUPPORTED));
            }

            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
