using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
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
    /// This processor verifies if <see cref="ConfigurationKeys.PHRASE_SLOP"/>
    /// is defined in the <see cref="QueryConfigHandler"/>. If it is, it looks for every
    /// <see cref="TokenizedPhraseQueryNode"/> and <see cref="MultiPhraseQueryNode"/> that does
    /// not have any <see cref="SlopQueryNode"/> applied to it and creates an
    /// <see cref="SlopQueryNode"/> and apply to it. The new <see cref="SlopQueryNode"/> has the
    /// same slop value defined in the configuration.
    /// </summary>
    /// <seealso cref="SlopQueryNode"/>
    /// <seealso cref="ConfigurationKeys.PHRASE_SLOP"/>
    public class DefaultPhraseSlopQueryNodeProcessor : QueryNodeProcessor
    {
        private bool processChildren = true;

        private int defaultPhraseSlop;

        public DefaultPhraseSlopQueryNodeProcessor()
        {
            // empty constructor
        }

        public override IQueryNode Process(IQueryNode queryTree)
        {
            QueryConfigHandler queryConfig = GetQueryConfigHandler();

            if (queryConfig != null)
            {
                if (queryConfig.TryGetValue(ConfigurationKeys.PHRASE_SLOP, out int defaultPhraseSlop))
                {
                    this.defaultPhraseSlop = defaultPhraseSlop;

                    return base.Process(queryTree);
                }
            }

            return queryTree;
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is TokenizedPhraseQueryNode
                || node is MultiPhraseQueryNode)
            {
                return new SlopQueryNode(node, this.defaultPhraseSlop);
            }

            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            if (node is SlopQueryNode)
            {
                this.processChildren = false;
            }

            return node;
        }

        protected override void ProcessChildren(IQueryNode queryTree)
        {
            if (this.processChildren)
            {
                base.ProcessChildren(queryTree);
            }
            else
            {
                this.processChildren = true;
            }
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
