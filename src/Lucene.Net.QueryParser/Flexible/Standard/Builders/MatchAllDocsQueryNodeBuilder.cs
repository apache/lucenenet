using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
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
    /// Builds a <see cref="MatchAllDocsQuery"/> object from a
    /// <see cref="MatchAllDocsQueryNode"/> object.
    /// </summary>
    public class MatchAllDocsQueryNodeBuilder : IStandardQueryBuilder
    {
        public MatchAllDocsQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            // validates node
            if (!(queryNode is MatchAllDocsQueryNode))
            {
                // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
                throw new QueryNodeException(string.Format(
                    QueryParserMessages.LUCENE_QUERY_CONVERSION_ERROR, queryNode
                        .ToQueryString(new EscapeQuerySyntax()), queryNode.GetType()
                        .Name));
            }

            return new MatchAllDocsQuery();
        }
    }
}
