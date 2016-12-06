using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
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
    /// Builds a <see cref="PhraseQuery"/> object from a <see cref="TokenizedPhraseQueryNode"/>
    /// object.
    /// </summary>
    public class PhraseQueryNodeBuilder : IStandardQueryBuilder
    {
        public PhraseQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            TokenizedPhraseQueryNode phraseNode = (TokenizedPhraseQueryNode)queryNode;

            PhraseQuery phraseQuery = new PhraseQuery();

            IList<IQueryNode> children = phraseNode.GetChildren();

            if (children != null)
            {
                foreach (IQueryNode child in children)
                {
                    TermQuery termQuery = (TermQuery)child
                        .GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);
                    FieldQueryNode termNode = (FieldQueryNode)child;

                    phraseQuery.Add(termQuery.Term, termNode.PositionIncrement);
                }
            }

            return phraseQuery;
        }
    }
}
