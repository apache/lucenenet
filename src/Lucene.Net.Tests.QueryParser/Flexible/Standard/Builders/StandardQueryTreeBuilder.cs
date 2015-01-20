/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Flexible.Core.Builders;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// This query tree builder only defines the necessary map to build a
	/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
	/// tree object. It should be used to generate a
	/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
	/// tree
	/// object from a query node tree processed by a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	</see>
	/// . <br/>
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryTreeBuilder
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryTreeBuilder</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors.StandardQueryNodeProcessorPipeline
	/// 	</seealso>
	public class StandardQueryTreeBuilder : QueryTreeBuilder, StandardQueryBuilder
	{
		public StandardQueryTreeBuilder()
		{
			SetBuilder(typeof(GroupQueryNode), new GroupQueryNodeBuilder());
			SetBuilder(typeof(FieldQueryNode), new FieldQueryNodeBuilder());
			SetBuilder(typeof(BooleanQueryNode), new BooleanQueryNodeBuilder());
			SetBuilder(typeof(FuzzyQueryNode), new FuzzyQueryNodeBuilder());
			SetBuilder(typeof(NumericQueryNode), new DummyQueryNodeBuilder());
			SetBuilder(typeof(NumericRangeQueryNode), new NumericRangeQueryNodeBuilder());
			SetBuilder(typeof(BoostQueryNode), new BoostQueryNodeBuilder());
			SetBuilder(typeof(ModifierQueryNode), new ModifierQueryNodeBuilder());
			SetBuilder(typeof(WildcardQueryNode), new WildcardQueryNodeBuilder());
			SetBuilder(typeof(TokenizedPhraseQueryNode), new PhraseQueryNodeBuilder());
			SetBuilder(typeof(MatchNoDocsQueryNode), new MatchNoDocsQueryNodeBuilder());
			SetBuilder(typeof(PrefixWildcardQueryNode), new PrefixWildcardQueryNodeBuilder());
			SetBuilder(typeof(TermRangeQueryNode), new TermRangeQueryNodeBuilder());
			SetBuilder(typeof(RegexpQueryNode), new RegexpQueryNodeBuilder());
			SetBuilder(typeof(SlopQueryNode), new SlopQueryNodeBuilder());
			SetBuilder(typeof(StandardBooleanQueryNode), new StandardBooleanQueryNodeBuilder(
				));
			SetBuilder(typeof(MultiPhraseQueryNode), new MultiPhraseQueryNodeBuilder());
			SetBuilder(typeof(MatchAllDocsQueryNode), new MatchAllDocsQueryNodeBuilder());
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public override object Build(QueryNode queryNode)
		{
			return (Query)base.Build(queryNode);
		}
	}
}
