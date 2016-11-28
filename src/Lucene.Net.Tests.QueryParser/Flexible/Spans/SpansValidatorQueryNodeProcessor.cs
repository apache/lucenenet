using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Spans
{
    /// <summary>
    /// Validates every query node in a query node tree. This processor will pass
    /// fine if the query nodes are only {@link BooleanQueryNode}s,
    /// {@link OrQueryNode}s or {@link FieldQueryNode}s, otherwise an exception will
    /// be thrown.
    /// <para/>
    /// If they are {@link AndQueryNode} or an instance of anything else that
    /// implements {@link FieldQueryNode} the exception will also be thrown.
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
