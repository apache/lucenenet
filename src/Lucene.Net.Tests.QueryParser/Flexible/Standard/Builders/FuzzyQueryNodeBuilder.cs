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
	/// <see cref="Org.Apache.Lucene.Search.FuzzyQuery">Org.Apache.Lucene.Search.FuzzyQuery
	/// 	</see>
	/// object from a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode
	/// 	</see>
	/// object.
	/// </summary>
	public class FuzzyQueryNodeBuilder : StandardQueryBuilder
	{
		public FuzzyQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual FuzzyQuery Build(QueryNode queryNode)
		{
			FuzzyQueryNode fuzzyNode = (FuzzyQueryNode)queryNode;
			string text = fuzzyNode.GetTextAsString();
			int numEdits = FuzzyQuery.FloatToEdits(fuzzyNode.GetSimilarity(), text.CodePointCount
				(0, text.Length));
			return new FuzzyQuery(new Term(fuzzyNode.GetFieldAsString(), fuzzyNode.GetTextAsString
				()), numEdits, fuzzyNode.GetPrefixLength());
		}
	}
}
