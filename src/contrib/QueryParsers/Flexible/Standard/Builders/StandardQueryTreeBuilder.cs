using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    public class StandardQueryTreeBuilder : QueryTreeBuilder, IStandardQueryBuilder
    {
        public StandardQueryTreeBuilder()
        {
            SetBuilder(typeof(GroupQueryNode), new GroupQueryNodeBuilder());
            SetBuilder(typeof(FieldQueryNode), new FieldQueryNodeBuilder());
            SetBuilder(typeof(BooleanQueryNode), new BooleanQueryNodeBuilder());
            SetBuilder(typeof(FuzzyQueryNode), new FuzzyQueryNodeBuilder());
            SetBuilder(typeof(INumericQueryNode), new DummyQueryNodeBuilder());
            SetBuilder(typeof(INumericRangeQueryNode), new NumericRangeQueryNodeBuilder());
            SetBuilder(typeof(BoostQueryNode), new BoostQueryNodeBuilder());
            SetBuilder(typeof(ModifierQueryNode), new ModifierQueryNodeBuilder());
            SetBuilder(typeof(WildcardQueryNode), new WildcardQueryNodeBuilder());
            SetBuilder(typeof(TokenizedPhraseQueryNode), new PhraseQueryNodeBuilder());
            SetBuilder(typeof(MatchNoDocsQueryNode), new MatchNoDocsQueryNodeBuilder());
            SetBuilder(typeof(PrefixWildcardQueryNode), new PrefixWildcardQueryNodeBuilder());
            SetBuilder(typeof(TermRangeQueryNode), new TermRangeQueryNodeBuilder());
            SetBuilder(typeof(RegexpQueryNode), new RegexpQueryNodeBuilder());
            SetBuilder(typeof(SlopQueryNode), new SlopQueryNodeBuilder());
            SetBuilder(typeof(StandardBooleanQueryNode), new StandardBooleanQueryNodeBuilder());
            SetBuilder(typeof(MultiPhraseQueryNode), new MultiPhraseQueryNodeBuilder());
            SetBuilder(typeof(MatchAllDocsQueryNode), new MatchAllDocsQueryNodeBuilder());
        }

        public new Query Build(IQueryNode queryNode)
        {
            return (Query)base.Build(queryNode);
        }
    }
}
