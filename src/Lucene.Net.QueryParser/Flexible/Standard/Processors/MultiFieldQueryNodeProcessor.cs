using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// This processor is used to expand terms so the query looks for the same term
    /// in different fields. It also boosts a query based on its field.
    /// <para/>
    /// This processor looks for every <see cref="IFieldableNode"/> contained in the query
    /// node tree. If a <see cref="IFieldableNode"/> is found, it checks if there is a
    /// <see cref="ConfigurationKeys.MULTI_FIELDS"/> defined in the <see cref="Core.Config.QueryConfigHandler"/>. If
    /// there is, the <see cref="IFieldableNode"/> is cloned N times and the clones are
    /// added to a <see cref="BooleanQueryNode"/> together with the original node. N is
    /// defined by the number of fields that it will be expanded to. The
    /// <see cref="BooleanQueryNode"/> is returned.
    /// </summary>
    /// <seealso cref="ConfigurationKeys.MULTI_FIELDS"/>
    public class MultiFieldQueryNodeProcessor : QueryNodeProcessor
    {
        private bool processChildren = true;

        public MultiFieldQueryNodeProcessor()
        {
            // empty constructor
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
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

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            if (node is IFieldableNode fieldNode)
            {
                this.processChildren = false;
                if (fieldNode.Field is null)
                {
                    string[] fields = GetQueryConfigHandler().Get(ConfigurationKeys.MULTI_FIELDS);

                    if (fields is null)
                    {
                        throw new ArgumentException(
                            "StandardQueryConfigHandler.ConfigurationKeys.MULTI_FIELDS should be set on the QueryConfigHandler");
                    }

                    if (fields != null && fields.Length > 0)
                    {
                        fieldNode.Field = fields[0];

                        if (fields.Length == 1)
                        {
                            return fieldNode;
                        }
                        else
                        {
                            IList<IQueryNode> children = new JCG.List<IQueryNode>
                            {
                                fieldNode
                            };

                            for (int i = 1; i < fields.Length; i++)
                            {
                                fieldNode = (IFieldableNode)fieldNode.CloneTree();
                                fieldNode.Field = fields[i];

                                children.Add(fieldNode);
                            }

                            return new GroupQueryNode(new OrQueryNode(children));
                        }
                    }
                }
            }

            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
