using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Operator = Lucene.Net.QueryParsers.Flexible.Standard.Config.StandardQueryConfigHandler.Operator;

namespace Lucene.Net.QueryParsers.Flexible.Precedence.Processors
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
    /// This processor is used to apply the correct <see cref="ModifierQueryNode"/> to <see cref="BooleanQueryNode"/>s children.
    /// <para>
    /// It walks through the query node tree looking for <see cref="BooleanQueryNode"/>s. If an <see cref="AndQueryNode"/> is found,
    /// every child, which is not a <see cref="ModifierQueryNode"/> or the <see cref="ModifierQueryNode"/> 
    /// is <see cref="Modifier.MOD_NONE"/>, becomes a <see cref="Modifier.MOD_REQ"/>. For any other
    /// <see cref="BooleanQueryNode"/> which is not an <see cref="OrQueryNode"/>, it checks the default operator is <see cref="Operator.AND"/>,
    /// if it is, the same operation when an <see cref="AndQueryNode"/> is found is applied to it.
    /// </para>
    /// </summary>
    /// <seealso cref="ConfigurationKeys.DEFAULT_OPERATOR"/>
    public class BooleanModifiersQueryNodeProcessor : QueryNodeProcessor
    {
        private readonly IList<IQueryNode> childrenBuffer = new JCG.List<IQueryNode>(); // LUCENENET: marked readonly

        private bool usingAnd = false;

        public BooleanModifiersQueryNodeProcessor()
        {
            // empty constructor
        }

        public override IQueryNode Process(IQueryNode queryTree)
        {
            Operator? op = GetQueryConfigHandler().Get(ConfigurationKeys.DEFAULT_OPERATOR);

            if (op is null)
            {
                throw new ArgumentException(
                    "StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR should be set on the QueryConfigHandler");
            }

            this.usingAnd = Operator.AND == op;

            return base.Process(queryTree);
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is AndQueryNode)
            {
                this.childrenBuffer.Clear();
                IList<IQueryNode> children = node.GetChildren();

                foreach (IQueryNode child in children)
                {
                    this.childrenBuffer.Add(ApplyModifier(child, Modifier.MOD_REQ));
                }

                node.Set(this.childrenBuffer);
            }
            else if (this.usingAnd && node is BooleanQueryNode
                && !(node is OrQueryNode))
            {
                this.childrenBuffer.Clear();
                IList<IQueryNode> children = node.GetChildren();

                foreach (IQueryNode child in children)
                {
                    this.childrenBuffer.Add(ApplyModifier(child, Modifier.MOD_REQ));
                }

                node.Set(this.childrenBuffer);
            }

            return node;
        }

        private static IQueryNode ApplyModifier(IQueryNode node, Modifier mod) // LUCENENET: CA1822: Mark members as static
        {
            // check if modifier is not already defined and is default
            if (!(node is ModifierQueryNode modNode))
            {
                return new ModifierQueryNode(node, mod);
            }
            else
            {
                if (modNode.Modifier == Modifier.MOD_NONE)
                {
                    return new ModifierQueryNode(modNode.GetChild(), mod);
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
