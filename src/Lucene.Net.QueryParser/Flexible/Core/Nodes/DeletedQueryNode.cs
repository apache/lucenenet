using Lucene.Net.QueryParsers.Flexible.Core.Parser;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A {@link DeletedQueryNode} represents a node that was deleted from the query
    /// node tree. It can be removed from the tree using the
    /// {@link RemoveDeletedQueryNodesProcessor} processor.
    /// </summary>
    public class DeletedQueryNode : QueryNodeImpl
    {
        public DeletedQueryNode()
        {
            // empty constructor
        }

        public override string ToQueryString(IEscapeQuerySyntax escaper)
        {
            return "[DELETEDCHILD]";
        }

        public override string ToString()
        {
            return "<deleted/>";
        }

        public override IQueryNode CloneTree()
        {
            DeletedQueryNode clone = (DeletedQueryNode)base.CloneTree();

            return clone;
        }
    }
}
