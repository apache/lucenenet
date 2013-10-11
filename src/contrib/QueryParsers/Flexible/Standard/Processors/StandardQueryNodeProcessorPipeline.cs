using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    public class StandardQueryNodeProcessorPipeline : QueryNodeProcessorPipeline
    {
        public StandardQueryNodeProcessorPipeline(QueryConfigHandler queryConfig)
            : base(queryConfig)
        {
            Add(new WildcardQueryNodeProcessor());
            Add(new MultiFieldQueryNodeProcessor());
            Add(new FuzzyQueryNodeProcessor());
            Add(new MatchAllDocsQueryNodeProcessor());
            Add(new OpenRangeQueryNodeProcessor());
            Add(new NumericQueryNodeProcessor());
            Add(new NumericRangeQueryNodeProcessor());
            Add(new LowercaseExpandedTermsQueryNodeProcessor());
            Add(new TermRangeQueryNodeProcessor());
            Add(new AllowLeadingWildcardProcessor());
            Add(new AnalyzerQueryNodeProcessor());
            Add(new PhraseSlopQueryNodeProcessor());
            //Add(new GroupQueryNodeProcessor());
            Add(new BooleanQuery2ModifierNodeProcessor());
            Add(new NoChildOptimizationQueryNodeProcessor());
            Add(new RemoveDeletedQueryNodesProcessor());
            Add(new RemoveEmptyNonLeafQueryNodeProcessor());
            Add(new BooleanSingleChildOptimizationQueryNodeProcessor());
            Add(new DefaultPhraseSlopQueryNodeProcessor());
            Add(new BoostQueryNodeProcessor());
            Add(new MultiTermRewriteMethodProcessor());
        }
    }
}
