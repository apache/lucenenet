using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Search;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /// <summary>
    /// This query tree builder only defines the necessary map to build a
    /// {@link Query} tree object. It should be used to generate a {@link Query} tree
    /// object from a query node tree processed by a
    /// {@link StandardQueryNodeProcessorPipeline}.
    /// </summary>
    /// <seealso cref="QueryTreeBuilder"/>
    /// <seealso cref="StandardQueryNodeProcessorPipeline"/>
    public class StandardQueryTreeBuilder : QueryTreeBuilder<Query>, IStandardQueryBuilder
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
            SetBuilder(typeof(PrefixWildcardQueryNode),
                new PrefixWildcardQueryNodeBuilder());
            SetBuilder(typeof(TermRangeQueryNode), new TermRangeQueryNodeBuilder());
            SetBuilder(typeof(RegexpQueryNode), new RegexpQueryNodeBuilder());
            SetBuilder(typeof(SlopQueryNode), new SlopQueryNodeBuilder());
            SetBuilder(typeof(StandardBooleanQueryNode),
                new StandardBooleanQueryNodeBuilder());
            SetBuilder(typeof(MultiPhraseQueryNode), new MultiPhraseQueryNodeBuilder());
            SetBuilder(typeof(MatchAllDocsQueryNode), new MatchAllDocsQueryNodeBuilder());
        }

        public override Query Build(IQueryNode queryNode)
        {
            return base.Build(queryNode);
        }
    }
}
