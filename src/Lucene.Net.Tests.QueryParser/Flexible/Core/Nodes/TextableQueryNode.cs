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
	/// Interface for a node that has text as a
	/// <see cref="Sharpen.CharSequence">Sharpen.CharSequence</see>
	/// </summary>
	public interface TextableQueryNode
	{
		CharSequence GetText();

		void SetText(CharSequence text);
	}
}
