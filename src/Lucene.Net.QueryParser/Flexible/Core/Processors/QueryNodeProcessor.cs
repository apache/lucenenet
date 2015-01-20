/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Flexible.Core.Config;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Processors;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Processors
{
	/// <summary>
	/// <p>
	/// A
	/// <see cref="QueryNodeProcessor">QueryNodeProcessor</see>
	/// is an interface for classes that process a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode
	/// 	</see>
	/// tree.
	/// <p>
	/// </p>
	/// The implementor of this class should perform some operation on a query node
	/// tree and return the same or another query node tree.
	/// <p>
	/// </p>
	/// It also may carry a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler"
	/// 	>Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
	/// object that contains
	/// configuration about the query represented by the query tree or the
	/// collection/index where it's intended to be executed.
	/// <p>
	/// </p>
	/// In case there is any
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler"
	/// 	>Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
	/// associated to the query tree
	/// to be processed, it should be set using
	/// <see cref="SetQueryConfigHandler(Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler)
	/// 	">SetQueryConfigHandler(Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler)
	/// 	</see>
	/// before
	/// <see cref="Process(Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode)">Process(Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode)
	/// 	</see>
	/// is invoked.
	/// </summary>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode
	/// 	</seealso>
	/// <seealso cref="QueryNodeProcessor">QueryNodeProcessor</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</seealso>
	public interface QueryNodeProcessor
	{
		/// <summary>Processes a query node tree.</summary>
		/// <remarks>
		/// Processes a query node tree. It may return the same or another query tree.
		/// I should never return <code>null</code>.
		/// </remarks>
		/// <param name="queryTree">tree root node</param>
		/// <returns>the processed query tree</returns>
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		QueryNode Process(QueryNode queryTree);

		/// <summary>
		/// Sets the
		/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler"
		/// 	>Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
		/// associated to the query tree.
		/// </summary>
		void SetQueryConfigHandler(QueryConfigHandler queryConfigHandler);

		/// <summary>
		/// Returns the
		/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler"
		/// 	>Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
		/// associated to the query tree if any,
		/// otherwise it returns <code>null</code>
		/// </summary>
		/// <returns>
		/// the
		/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler"
		/// 	>Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
		/// associated to the query tree if any,
		/// otherwise it returns <code>null</code>
		/// </returns>
		QueryConfigHandler GetQueryConfigHandler();
	}
}
