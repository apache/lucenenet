using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Messages;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Spans
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
    /// Validates every query node in a query node tree. This processor will pass
    /// fine if the query nodes are only <see cref="BooleanQueryNode"/>s,
    /// <see cref="OrQueryNode"/>s or <see cref="FieldQueryNode"/>s, otherwise an exception will
    /// be thrown.
    /// <para/>
    /// If they are <see cref="AndQueryNode"/> or an instance of anything else that
    /// implements <see cref="FieldQueryNode"/> the exception will also be thrown.
    /// </summary>
    public class SpansValidatorQueryNodeProcessor : QueryNodeProcessor
    {
        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            if (!((node is BooleanQueryNode && !(node is AndQueryNode)) || node
                .GetType() == typeof(FieldQueryNode)))
            {
                // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
                throw new QueryNodeException(
                    QueryParserMessages.NODE_ACTION_NOT_SUPPORTED);
            }

            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
