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
	/// A query node implements
	/// <see cref="FieldableNode">FieldableNode</see>
	/// interface to indicate that its
	/// children and itself are associated to a specific field.
	/// If it has any children which also implements this interface, it must ensure
	/// the children are associated to the same field.
	/// </summary>
	public interface FieldableNode : QueryNode
	{
		/// <summary>Returns the field associated to the node and every node under it.</summary>
		/// <remarks>Returns the field associated to the node and every node under it.</remarks>
		/// <returns>the field name</returns>
		CharSequence GetField();

		/// <summary>Associates the node to a field.</summary>
		/// <remarks>Associates the node to a field.</remarks>
		/// <param name="fieldName">the field name</param>
		void SetField(CharSequence fieldName);
	}
}
