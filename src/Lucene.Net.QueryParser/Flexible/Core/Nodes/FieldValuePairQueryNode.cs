namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// This interface should be implemented by {@link QueryNode} that holds a field
    /// and an arbitrary value.
    /// </summary>
    /// <seealso cref="IFieldableNode"/>
    /// <seealso cref="IValueQueryNode{T}"/>
    /// <typeparam name="T"></typeparam>
    public interface IFieldValuePairQueryNode<T> : IFieldableNode, IValueQueryNode<T>
    {
    }
}
