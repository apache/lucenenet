using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Processors
{
    /// <summary>
    /// A {@link QueryNodeProcessor} is an interface for classes that process a
    /// {@link QueryNode} tree.
    /// <para>
    /// The implementor of this class should perform some operation on a query node
    /// tree and return the same or another query node tree.
    /// </para>
    /// <para>
    /// It also may carry a {@link QueryConfigHandler} object that contains
    /// configuration about the query represented by the query tree or the
    /// collection/index where it's intended to be executed.
    /// </para>
    /// <para>
    /// In case there is any {@link QueryConfigHandler} associated to the query tree
    /// to be processed, it should be set using
    /// {@link QueryNodeProcessor#setQueryConfigHandler(QueryConfigHandler)} before
    /// {@link QueryNodeProcessor#process(QueryNode)} is invoked.
    /// </para>
    /// </summary>
    /// <seealso cref="IQueryNode"/>
    /// <seealso cref="QueryNodeProcessor"/>
    /// <seealso cref="QueryConfigHandler"/>
    public interface IQueryNodeProcessor
    {
        /**
        * Processes a query node tree. It may return the same or another query tree.
        * I should never return <code>null</code>.
        * 
        * @param queryTree
        *          tree root node
        * 
        * @return the processed query tree
        */
        IQueryNode Process(IQueryNode queryTree);

        /**
         * Sets the {@link QueryConfigHandler} associated to the query tree.
         */
        void SetQueryConfigHandler(QueryConfigHandler queryConfigHandler);

        /**
         * Returns the {@link QueryConfigHandler} associated to the query tree if any,
         * otherwise it returns <code>null</code>
         * 
         * @return the {@link QueryConfigHandler} associated to the query tree if any,
         *         otherwise it returns <code>null</code>
         */
        QueryConfigHandler GetQueryConfigHandler();

    }
}
