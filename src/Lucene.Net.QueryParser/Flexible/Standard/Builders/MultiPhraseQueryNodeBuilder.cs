using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Search;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// Builds a <see cref="MultiPhraseQuery"/> object from a <see cref="MultiPhraseQueryNode"/>
    /// object.
    /// </summary>
    public class MultiPhraseQueryNodeBuilder : IStandardQueryBuilder
    {
        public MultiPhraseQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            MultiPhraseQueryNode phraseNode = (MultiPhraseQueryNode)queryNode;

            MultiPhraseQuery phraseQuery = new MultiPhraseQuery();

            IList<IQueryNode> children = phraseNode.GetChildren();

            if (children != null)
            {
                IDictionary<int, JCG.List<Term>> positionTermMap = new JCG.SortedDictionary<int, JCG.List<Term>>();

                foreach (IQueryNode child in children)
                {
                    FieldQueryNode termNode = (FieldQueryNode)child;
                    TermQuery termQuery = (TermQuery)termNode
                        .GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);

                    if (!positionTermMap.TryGetValue(termNode.PositionIncrement, out JCG.List<Term> termList) || termList is null)
                    {
                        termList = new JCG.List<Term>();
                        positionTermMap[termNode.PositionIncrement] = termList;
                    }

                    termList.Add(termQuery.Term);
                }

                foreach (int positionIncrement in positionTermMap.Keys)
                {
                    JCG.List<Term> termList = positionTermMap[positionIncrement];

                    phraseQuery.Add(termList.ToArray(/*new Term[termList.size()]*/),
                                positionIncrement);
                }
            }

            return phraseQuery;
        }
    }
}
