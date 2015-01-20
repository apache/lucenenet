/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// This interface should be implemented by a
	/// <see cref="QueryNode">QueryNode</see>
	/// that represents
	/// some kind of range query.
	/// </summary>
	public interface RangeQueryNode<T> : FieldableNode where T:FieldValuePairQueryNode
		<object>
	{
		T GetLowerBound();

		T GetUpperBound();

		bool IsLowerInclusive();

		bool IsUpperInclusive();
	}
}
