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
	/// <see cref="Lucene.Net.Search.FuzzyQuery">Lucene.Net.Search.FuzzyQuery
	/// 	</see>
	/// object from a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode
	/// 	</see>
	/// object.
	/// </summary>
	public class FuzzyQueryNodeBuilder : StandardQueryBuilder
	{
		public FuzzyQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
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
