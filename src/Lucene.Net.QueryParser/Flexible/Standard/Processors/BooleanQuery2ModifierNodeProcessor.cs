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
    /// This processor is used to apply the correct <see cref="ModifierQueryNode"/> to
    /// <see cref="BooleanQueryNode"/>s children. This is a variant of
    /// <see cref="Precedence.Processors.BooleanModifiersQueryNodeProcessor"/> which ignores precedence.
    /// <para/>
    /// The <see cref="Parser.StandardSyntaxParser"/> knows the rules of precedence, but lucene
    /// does not. e.g. <code>(A AND B OR C AND D)</code> ist treated like
    /// <code>(+A +B +C +D)</code>.
    /// <para/>
    /// This processor walks through the query node tree looking for
    /// <see cref="BooleanQueryNode"/>s. If an <see cref="AndQueryNode"/> is found, every child,
    /// which is not a <see cref="ModifierQueryNode"/> or the <see cref="ModifierQueryNode"/> is
    /// <see cref="Modifier.MOD_NONE"/>, becomes a <see cref="Modifier.MOD_REQ"/>. For default
    /// <see cref="BooleanQueryNode"/>, it checks the default operator is
    /// <see cref="Operator.AND"/>, if it is, the same operation when an
    /// <see cref="AndQueryNode"/> is found is applied to it. Each <see cref="BooleanQueryNode"/>
    /// which direct parent is also a <see cref="BooleanQueryNode"/> is removed (to ignore
    /// the rules of precedence).
    /// </summary>
    /// <seealso cref="ConfigurationKeys.DEFAULT_OPERATOR"/>
    /// <seealso cref="Precedence.Processors.BooleanModifiersQueryNodeProcessor"/>
    public class BooleanQuery2ModifierNodeProcessor : IQueryNodeProcessor
    {
        internal const string TAG_REMOVE = "remove";
        internal const string TAG_MODIFIER = "wrapWithModifier";
        internal const string TAG_BOOLEAN_ROOT = "booleanRoot";

        private QueryConfigHandler queryConfigHandler;

        private readonly IList<IQueryNode> childrenBuffer = new JCG.List<IQueryNode>();

        private bool usingAnd = false;

        public BooleanQuery2ModifierNodeProcessor()
        {
            // empty constructor
        }

        public virtual IQueryNode Process(IQueryNode queryTree)
        {
            Operator? op = GetQueryConfigHandler().Get(
                ConfigurationKeys.DEFAULT_OPERATOR);

            if (op is null)
            {
                throw new ArgumentException(
                    "StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR should be set on the QueryConfigHandler");
            }

            this.usingAnd = Operator.AND == op;

            return ProcessIteration(queryTree);
        }


        protected virtual void ProcessChildren(IQueryNode queryTree)
        {
            IList<IQueryNode> children = queryTree.GetChildren();
            if (children != null && children.Count > 0)
            {
                foreach (IQueryNode child in children)
                {
                    /*child = */
                    ProcessIteration(child);
                }
            }
        }

        private IQueryNode ProcessIteration(IQueryNode queryTree)
        {
            queryTree = PreProcessNode(queryTree);

            ProcessChildren(queryTree);

            queryTree = PostProcessNode(queryTree);

            return queryTree;
        }

        protected virtual void FillChildrenBufferAndApplyModifiery(IQueryNode parent)
        {
            foreach (IQueryNode node in parent.GetChildren())
            {
                if (node.ContainsTag(TAG_REMOVE))
                {
                    FillChildrenBufferAndApplyModifiery(node);
                }
                else if (node.ContainsTag(TAG_MODIFIER))
                {
                    childrenBuffer.Add(ApplyModifier(node,
                        (Modifier)node.GetTag(TAG_MODIFIER)));
                }
                else
                {
                    childrenBuffer.Add(node);
                }
            }
        }

        protected virtual IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node.ContainsTag(TAG_BOOLEAN_ROOT))
            {
                this.childrenBuffer.Clear();
                FillChildrenBufferAndApplyModifiery(node);
                node.Set(childrenBuffer);
            }
            return node;
        }

        protected virtual IQueryNode PreProcessNode(IQueryNode node)
        {
            IQueryNode parent = node.Parent;
            if (node is BooleanQueryNode)
            {
                if (parent is BooleanQueryNode)
                {
                    node.SetTag(TAG_REMOVE, true); // no precedence
                }
                else
                {
                    node.SetTag(TAG_BOOLEAN_ROOT, true);
                }
            }
            else if (parent is BooleanQueryNode)
            {
                if ((parent is AndQueryNode)
                    || (usingAnd && IsDefaultBooleanQueryNode(parent)))
                {
                    TagModifierButDoNotOverride(node, Modifier.MOD_REQ);
                }
            }
            return node;
        }

        protected virtual bool IsDefaultBooleanQueryNode(IQueryNode toTest)
        {
            return toTest != null && typeof(BooleanQueryNode).Equals(toTest.GetType());
        }

        private static IQueryNode ApplyModifier(IQueryNode node, Modifier mod) // LUCENENET: CA1822: Mark members as static
        {
            // check if modifier is not already defined and is default
            if (!(node is ModifierQueryNode modNode))
            {
                return new BooleanModifierNode(node, mod);
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

        protected virtual void TagModifierButDoNotOverride(IQueryNode node, Modifier mod)
        {
            if (node is ModifierQueryNode modNode)
            {
                if (modNode.Modifier == Modifier.MOD_NONE)
                {
                    node.SetTag(TAG_MODIFIER, mod);
                }
            }
            else
            {
                node.SetTag(TAG_MODIFIER, Modifier.MOD_REQ);
            }
        }

        public virtual void SetQueryConfigHandler(QueryConfigHandler queryConfigHandler)
        {
            this.queryConfigHandler = queryConfigHandler;
        }

        public virtual QueryConfigHandler GetQueryConfigHandler()
        {
            return queryConfigHandler;
        }
    }
}
