using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
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
    /// This processor instates the default
    /// <see cref="MultiTermQuery.RewriteMethod"/>,
    /// <see cref="MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT"/>, for multi-term
    /// query nodes.
    /// </summary>
    public class MultiTermRewriteMethodProcessor : QueryNodeProcessor
    {
        public static readonly string TAG_ID = "MultiTermRewriteMethodConfiguration";

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            // set setMultiTermRewriteMethod for WildcardQueryNode and
            // PrefixWildcardQueryNode
            if (node is WildcardQueryNode
                || node is IAbstractRangeQueryNode || node is RegexpQueryNode)
            {
                MultiTermQuery.RewriteMethod rewriteMethod = GetQueryConfigHandler().Get(ConfigurationKeys.MULTI_TERM_REWRITE_METHOD);

                if (rewriteMethod is null)
                {
                    // This should not happen, this configuration is set in the
                    // StandardQueryConfigHandler
                    throw new ArgumentException(
                        "StandardQueryConfigHandler.ConfigurationKeys.MULTI_TERM_REWRITE_METHOD should be set on the QueryConfigHandler");
                }

                // use a TAG to take the value to the Builder
                node.SetTag(MultiTermRewriteMethodProcessor.TAG_ID, rewriteMethod);
            }

            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
