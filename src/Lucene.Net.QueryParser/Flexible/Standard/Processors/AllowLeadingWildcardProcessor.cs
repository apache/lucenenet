using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
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
    /// This processor verifies if
    /// <see cref="ConfigurationKeys.ALLOW_LEADING_WILDCARD"/> is defined in the
    /// <see cref="Core.Config.QueryConfigHandler"/>. If it is and leading wildcard is not allowed, it
    /// looks for every <see cref="WildcardQueryNode"/> contained in the query node tree
    /// and throws an exception if any of them has a leading wildcard ('*' or '?').
    /// </summary>
    /// <seealso cref="ConfigurationKeys.ALLOW_LEADING_WILDCARD"/>
    public class AllowLeadingWildcardProcessor : QueryNodeProcessor
    {
        public AllowLeadingWildcardProcessor()
        {
            // empty constructor
        }

        public override IQueryNode Process(IQueryNode queryTree)
        {
            bool? allowsLeadingWildcard = GetQueryConfigHandler().Get(ConfigurationKeys.ALLOW_LEADING_WILDCARD);

            if (allowsLeadingWildcard != null)
            {
                if (!allowsLeadingWildcard.Value)
                {
                    return base.Process(queryTree);
                }
            }

            return queryTree;
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is WildcardQueryNode wildcardNode)
            {
                if (wildcardNode.Text.Length > 0)
                {
                    // Validate if the wildcard was escaped
                    if (UnescapedCharSequence.WasEscaped(wildcardNode.Text, 0))
                        return node;

                    switch (wildcardNode.Text[0])
                    {
                        case '*':
                        case '?':
                            // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
                            throw new QueryNodeException(string.Format(
                                QueryParserMessages.LEADING_WILDCARD_NOT_ALLOWED, node
                                    .ToQueryString(new EscapeQuerySyntax())));
                    }
                }
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
