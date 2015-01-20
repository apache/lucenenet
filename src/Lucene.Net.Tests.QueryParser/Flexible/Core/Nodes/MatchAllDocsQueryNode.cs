/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Parser;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// A
	/// <see cref="MatchAllDocsQueryNode">MatchAllDocsQueryNode</see>
	/// indicates that a query node tree or subtree
	/// will match all documents if executed in the index.
	/// </summary>
	public class MatchAllDocsQueryNode : QueryNodeImpl
	{
		public MatchAllDocsQueryNode()
		{
		}

		// empty constructor
		public override string ToString()
		{
			return "<matchAllDocs field='*' term='*'/>";
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escapeSyntaxParser)
		{
			return "*:*";
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.MatchAllDocsQueryNode clone = (
				Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.MatchAllDocsQueryNode)base.CloneTree
				();
			// nothing to clone
			return clone;
		}
	}
}
