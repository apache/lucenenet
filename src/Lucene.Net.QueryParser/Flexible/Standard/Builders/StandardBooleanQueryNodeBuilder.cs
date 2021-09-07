using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
using Lucene.Net.Search;
using System.Collections.Generic;

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
    /// This builder does the same as the <see cref="BooleanQueryNodeBuilder"/>, but this
    /// considers if the built <see cref="BooleanQuery"/> should have its coord disabled or
    /// not.
    /// </summary>
    /// <seealso cref="BooleanQueryNodeBuilder"/>
    /// <seealso cref="BooleanQuery"/>
    /// <seealso cref="Search.Similarities.Similarity.Coord(int, int)"/>
    public class StandardBooleanQueryNodeBuilder : IStandardQueryBuilder
    {
        public StandardBooleanQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            StandardBooleanQueryNode booleanNode = (StandardBooleanQueryNode)queryNode;

            BooleanQuery bQuery = new BooleanQuery(booleanNode.DisableCoord);
            IList<IQueryNode> children = booleanNode.GetChildren();

            if (children != null)
            {
                foreach (IQueryNode child in children)
                {
                    object obj = child.GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);

                    if (obj != null)
                    {
                        Query query = (Query)obj;

                        try
                        {
                            bQuery.Add(query, GetModifierValue(child));
                        }
                        catch (BooleanQuery.TooManyClausesException ex)
                        {
                            // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
                            throw new QueryNodeException(string.Format(
                                QueryParserMessages.TOO_MANY_BOOLEAN_CLAUSES, BooleanQuery
                                    .MaxClauseCount, queryNode
                                    .ToQueryString(new EscapeQuerySyntax())), ex);
                        }
                    }
                }
            }

            return bQuery;
        }

        private static Occur GetModifierValue(IQueryNode node)
        {
            if (node is ModifierQueryNode mNode)
            {
                Modifier modifier = mNode.Modifier;

                if (Modifier.MOD_NONE.Equals(modifier))
                {
                    return Occur.SHOULD;
                }
                else if (Modifier.MOD_NOT.Equals(modifier))
                {
                    return Occur.MUST_NOT;
                }
                else
                {
                    return Occur.MUST;
                }
            }

            return Occur.SHOULD;
        }
    }
}
