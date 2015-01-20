/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Standard.Builders;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// Builds a
	/// <see cref="Lucene.Net.Search.TermQuery">Lucene.Net.Search.TermQuery
	/// 	</see>
	/// object from a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode
	/// 	</see>
	/// object.
	/// </summary>
	public class FieldQueryNodeBuilder : StandardQueryBuilder
	{
		public FieldQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual TermQuery Build(QueryNode queryNode)
		{
			FieldQueryNode fieldNode = (FieldQueryNode)queryNode;
			return new TermQuery(new Term(fieldNode.GetFieldAsString(), fieldNode.GetTextAsString
				()));
		}
	}
}
