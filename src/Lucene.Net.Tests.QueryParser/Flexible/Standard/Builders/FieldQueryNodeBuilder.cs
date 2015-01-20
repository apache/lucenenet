/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// Builds a
	/// <see cref="Org.Apache.Lucene.Search.TermQuery">Org.Apache.Lucene.Search.TermQuery
	/// 	</see>
	/// object from a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FieldQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FieldQueryNode
	/// 	</see>
	/// object.
	/// </summary>
	public class FieldQueryNodeBuilder : StandardQueryBuilder
	{
		public FieldQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual TermQuery Build(QueryNode queryNode)
		{
			FieldQueryNode fieldNode = (FieldQueryNode)queryNode;
			return new TermQuery(new Term(fieldNode.GetFieldAsString(), fieldNode.GetTextAsString
				()));
		}
	}
}
