/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Parser;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// A
	/// <see cref="QueryNode">QueryNode</see>
	/// is a interface implemented by all nodes on a QueryNode
	/// tree.
	/// </summary>
	public interface QueryNode
	{
		/// <summary>convert to a query string understood by the query parser</summary>
		CharSequence ToQueryString(EscapeQuerySyntax escapeSyntaxParser);

		// TODO: this interface might be changed in the future
		/// <summary>for printing</summary>
		string ToString();

		/// <summary>get Children nodes</summary>
		IList<QueryNode> GetChildren();

		/// <summary>verify if a node is a Leaf node</summary>
		bool IsLeaf();

		/// <summary>verify if a node contains a tag</summary>
		bool ContainsTag(string tagName);

		/// <summary>Returns object stored under that tag name</summary>
		object GetTag(string tagName);

		QueryNode GetParent();

		/// <summary>
		/// Recursive clone the QueryNode tree The tags are not copied to the new tree
		/// when you call the cloneTree() method
		/// </summary>
		/// <returns>the cloned tree</returns>
		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		QueryNode CloneTree();

		// Below are the methods that can change state of a QueryNode
		// Write Operations (not Thread Safe)
		// add a new child to a non Leaf node
		void Add(QueryNode child);

		void Add(IList<QueryNode> children);

		// reset the children of a node
		void Set(IList<QueryNode> children);

		/// <summary>Associate the specified value with the specified tagName.</summary>
		/// <remarks>
		/// Associate the specified value with the specified tagName. If the tagName
		/// already exists, the old value is replaced. The tagName and value cannot be
		/// null. tagName will be converted to lowercase.
		/// </remarks>
		void SetTag(string tagName, object value);

		/// <summary>Unset a tag.</summary>
		/// <remarks>Unset a tag. tagName will be converted to lowercase.</remarks>
		void UnsetTag(string tagName);

		/// <summary>Returns a map containing all tags attached to this query node.</summary>
		/// <remarks>Returns a map containing all tags attached to this query node.</remarks>
		/// <returns>a map containing all tags attached to this query node</returns>
		IDictionary<string, object> GetTagMap();

		/// <summary>Removes this query node from its parent.</summary>
		/// <remarks>Removes this query node from its parent.</remarks>
		void RemoveFromParent();
	}
}
