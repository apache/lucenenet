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
	/// that holds an
	/// arbitrary value.
	/// </summary>
	public interface ValueQueryNode<T> : QueryNode
	{
		void SetValue(T value);

		T GetValue();
	}
}
