using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Processors;
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
    /// Builds a <see cref="PrefixQuery"/> object from a <see cref="PrefixWildcardQueryNode"/>
    /// object.
    /// </summary>
    public class PrefixWildcardQueryNodeBuilder : IStandardQueryBuilder
    {
        public PrefixWildcardQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            PrefixWildcardQueryNode wildcardNode = (PrefixWildcardQueryNode)queryNode;

            string text = wildcardNode.Text.Subsequence(0, wildcardNode.Text.Length - 1).ToString(); // LUCENENET: Checked 2nd Subsequence parameter
            PrefixQuery q = new PrefixQuery(new Term(wildcardNode.GetFieldAsString(), text));

            MultiTermQuery.RewriteMethod method = (MultiTermQuery.RewriteMethod)queryNode.GetTag(MultiTermRewriteMethodProcessor.TAG_ID);
            if (method != null)
            {
                q.MultiTermRewriteMethod = method;
            }

            return q;
        }
    }
}
