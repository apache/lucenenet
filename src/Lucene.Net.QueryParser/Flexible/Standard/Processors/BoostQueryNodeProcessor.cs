using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
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
    /// This processor iterates the query node tree looking for every
    /// <see cref="IFieldableNode"/> that has <see cref="ConfigurationKeys.BOOST"/> in its
    /// config. If there is, the boost is applied to that <see cref="IFieldableNode"/>.
    /// </summary>
    /// <seealso cref="ConfigurationKeys.BOOST"/>
    /// <seealso cref="QueryConfigHandler"/>
    /// <seealso cref="IFieldableNode"/>
    public class BoostQueryNodeProcessor : QueryNodeProcessor
    {
        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is IFieldableNode fieldNode &&
                (node.Parent is null || !(node.Parent is IFieldableNode)))
            {
                QueryConfigHandler config = GetQueryConfigHandler();

                if (config != null)
                {
                    string field = fieldNode.Field;
                    FieldConfig fieldConfig = config.GetFieldConfig(StringUtils.ToString(field));

                    if (fieldConfig != null)
                    {
                        if (fieldConfig.TryGetValue(ConfigurationKeys.BOOST, out float boost))
                        {
                            return new BoostQueryNode(node, boost);
                        }
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
