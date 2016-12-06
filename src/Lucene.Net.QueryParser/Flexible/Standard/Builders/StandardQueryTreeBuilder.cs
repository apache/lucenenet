using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Search;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// This query tree builder only defines the necessary map to build a
    /// <see cref="Query"/> tree object. It should be used to generate a <see cref="Query"/> tree
    /// object from a query node tree processed by a
    /// <see cref="Processors.StandardQueryNodeProcessorPipeline"/>.
    /// </summary>
    /// <seealso cref="QueryTreeBuilder"/>
    /// <seealso cref="Processors.StandardQueryNodeProcessorPipeline"/>
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
