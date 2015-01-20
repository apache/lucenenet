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
	/// A
	/// <see cref="MatchNoDocsQueryNode">MatchNoDocsQueryNode</see>
	/// indicates that a query node tree or subtree
	/// will not match any documents if executed in the index.
	/// </summary>
	public class MatchNoDocsQueryNode : DeletedQueryNode
	{
		public MatchNoDocsQueryNode()
		{
		}

		// empty constructor
		public override string ToString()
		{
			return "<matchNoDocsQueryNode/>";
		}
	}
}
