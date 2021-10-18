using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Operator = Lucene.Net.QueryParsers.Flexible.Standard.Config.StandardQueryConfigHandler.Operator;

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
    /// The <see cref="Core.Parser.ISyntaxParser"/>
    /// generates query node trees that consider the boolean operator precedence, but
    /// Lucene current syntax does not support boolean precedence, so this processor
    /// remove all the precedence and apply the equivalent modifier according to the
    /// boolean operation defined on an specific query node.
    /// <para/>
    /// If there is a <see cref="GroupQueryNode"/> in the query node tree, the query node
    /// tree is not merged with the one above it.
    /// <para/>
    /// Example: TODO: describe a good example to show how this processor works
    /// </summary>
    /// <seealso cref="StandardQueryConfigHandler"/>
    [Obsolete("Use BooleanQuery2ModifierNodeProcessor instead")]
    public class GroupQueryNodeProcessor : IQueryNodeProcessor
    {
        private IList<IQueryNode> queryNodeList;

        private bool latestNodeVerified;

        private QueryConfigHandler queryConfig;

        private bool usingAnd = false;

        public GroupQueryNodeProcessor()
        {
            // empty constructor
        }

        public virtual IQueryNode Process(IQueryNode queryTree)
        {
            Operator? defaultOperator = GetQueryConfigHandler().Get(ConfigurationKeys.DEFAULT_OPERATOR);

            if (defaultOperator is null)
            {
                throw new ArgumentException(
                    "DEFAULT_OPERATOR should be set on the QueryConfigHandler");
            }

            this.usingAnd = Operator.AND == defaultOperator;

            if (queryTree is GroupQueryNode groupQueryNode)
            {
                queryTree = groupQueryNode.GetChild();
            }

            this.queryNodeList = new JCG.List<IQueryNode>();
            this.latestNodeVerified = false;
            ReadTree(queryTree);

            IList<IQueryNode> actualQueryNodeList = this.queryNodeList;

            for (int i = 0; i < actualQueryNodeList.Count; i++)
            {
                IQueryNode node = actualQueryNodeList[i];

                if (node is GroupQueryNode)
                {
                    actualQueryNodeList[i] = Process(node);
                }
            }

            this.usingAnd = false;

            if (queryTree is BooleanQueryNode)
            {
                queryTree.Set(actualQueryNodeList);

                return queryTree;
            }
            else
            {
                return new BooleanQueryNode(actualQueryNodeList);
            }
        }

        private IQueryNode ApplyModifier(IQueryNode node, IQueryNode parent)
        {
            if (this.usingAnd)
            {
                if (parent is OrQueryNode)
                {
                    if (node is ModifierQueryNode modNode)
                    {
                        if (modNode.Modifier == Modifier.MOD_REQ)
                        {
                            return modNode.GetChild();
                        }
                    }
                }
                else
                {
                    if (node is ModifierQueryNode modNode)
                    {
                        if (modNode.Modifier == Modifier.MOD_NONE)
                        {
                            return new BooleanModifierNode(modNode.GetChild(), Modifier.MOD_REQ);
                        }
                    }
                    else
                    {
                        return new BooleanModifierNode(node, Modifier.MOD_REQ);
                    }
                }
            }
            else
            {
                if (node.Parent is AndQueryNode)
                {
                    if (node is ModifierQueryNode modNode)
                    {
                        if (modNode.Modifier == Modifier.MOD_NONE)
                        {
                            return new BooleanModifierNode(modNode.GetChild(), Modifier.MOD_REQ);
                        }
                    }
                    else
                    {
                        return new BooleanModifierNode(node, Modifier.MOD_REQ);
                    }
                }
            }

            return node;
        }

        private void ReadTree(IQueryNode node)
        {
            if (node is BooleanQueryNode)
            {
                IList<IQueryNode> children = node.GetChildren();

                if (children != null && children.Count > 0)
                {
                    for (int i = 0; i < children.Count - 1; i++)
                    {
                        ReadTree(children[i]);
                    }

                    ProcessNode(node);
                    ReadTree(children[children.Count - 1]);
                }
                else
                {
                    ProcessNode(node);
                }
            }
            else
            {
                ProcessNode(node);
            }
        }

        private void ProcessNode(IQueryNode node)
        {
            if (node is AndQueryNode || node is OrQueryNode)
            {
                if (!this.latestNodeVerified && this.queryNodeList.Count > 0)
                {
                    var value = this.queryNodeList[this.queryNodeList.Count - 1];
                    this.queryNodeList.Remove(value);

                    this.queryNodeList.Add(ApplyModifier(value, node));
                    this.latestNodeVerified = true;
                }
            }
            else if (!(node is BooleanQueryNode))
            {
                this.queryNodeList.Add(ApplyModifier(node, node.Parent));
                this.latestNodeVerified = false;
            }
        }

        public virtual QueryConfigHandler GetQueryConfigHandler()
        {
            return this.queryConfig;
        }

        public virtual void SetQueryConfigHandler(QueryConfigHandler queryConfigHandler)
        {
            this.queryConfig = queryConfigHandler;
        }
    }
}
