/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// This interface should be implemented by
	/// <see cref="QueryNode">QueryNode</see>
	/// that holds a field
	/// and an arbitrary value.
	/// </summary>
	/// <seealso cref="FieldableNode">FieldableNode</seealso>
	/// <seealso cref="ValueQueryNode{T}">ValueQueryNode&lt;T&gt;</seealso>
	public interface FieldValuePairQueryNode<T> : FieldableNode, ValueQueryNode<T>
	{
	}
}
