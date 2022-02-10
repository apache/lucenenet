using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using System;
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
    /// This processor changes every field name of each <see cref="IFieldableNode"/> query
    /// node contained in the query tree to the field name defined in the
    /// <see cref="IUniqueFieldAttribute"/>. So, the <see cref="IUniqueFieldAttribute"/> must be
    /// defined in the <see cref="QueryConfigHandler"/> object set in this processor,
    /// otherwise it throws an exception.
    /// </summary>
    /// <seealso cref="IUniqueFieldAttribute"/>
    public class UniqueFieldQueryNodeProcessor : QueryNodeProcessor
    {
        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            if (node is IFieldableNode)
            {
                IFieldableNode fieldNode = (IFieldableNode)node;

                QueryConfigHandler queryConfig = GetQueryConfigHandler();

                if (queryConfig is null)
                {
                    throw new ArgumentException(
                        "A config handler is expected by the processor UniqueFieldQueryNodeProcessor!");
                }

                if (!queryConfig.Has(SpansQueryConfigHandler.UNIQUE_FIELD))
                {
                    throw new ArgumentException(
                        "UniqueFieldAttribute should be defined in the config handler!");
                }

                String uniqueField = queryConfig.Get(SpansQueryConfigHandler.UNIQUE_FIELD);
                fieldNode.Field = (uniqueField);
            }

            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
