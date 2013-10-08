using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    public class GroupQueryNodeProcessor : IQueryNodeProcessor
    {
        private List<IQueryNode> queryNodeList;

        private bool latestNodeVerified;

        private QueryConfigHandler queryConfig;

        private bool usingAnd = false;

        public GroupQueryNodeProcessor()
        {
            // empty constructor
        }

        public IQueryNode Process(IQueryNode queryTree)
        {
            StandardQueryConfigHandler.Operator? defaultOperator = QueryConfigHandler.Get(StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR);

            if (defaultOperator == null)
            {
                throw new ArgumentException(
                    "DEFAULT_OPERATOR should be set on the QueryConfigHandler");
            }

            this.usingAnd = StandardQueryConfigHandler.Operator.AND == defaultOperator;

            if (queryTree is GroupQueryNode)
            {
                queryTree = ((GroupQueryNode)queryTree).Child;
            }

            this.queryNodeList = new List<IQueryNode>();
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
                    if (node is ModifierQueryNode)
                    {
                        ModifierQueryNode modNode = (ModifierQueryNode)node;

                        if (modNode.ModifierValue == ModifierQueryNode.Modifier.MOD_REQ)
                        {
                            return modNode.Child;
                        }
                    }
                }
                else
                {
                    if (node is ModifierQueryNode)
                    {
                        ModifierQueryNode modNode = (ModifierQueryNode)node;

                        if (modNode.ModifierValue == ModifierQueryNode.Modifier.MOD_NONE)
                        {
                            return new BooleanModifierNode(modNode.Child, ModifierQueryNode.Modifier.MOD_REQ);
                        }
                    }
                    else
                    {
                        return new BooleanModifierNode(node, ModifierQueryNode.Modifier.MOD_REQ);
                    }
                }
            }
            else
            {
                if (node.Parent is AndQueryNode)
                {
                    if (node is ModifierQueryNode)
                    {
                        ModifierQueryNode modNode = (ModifierQueryNode)node;

                        if (modNode.ModifierValue == ModifierQueryNode.Modifier.MOD_NONE)
                        {
                            return new BooleanModifierNode(modNode.Child, ModifierQueryNode.Modifier.MOD_REQ);
                        }
                    }
                    else
                    {
                        return new BooleanModifierNode(node, ModifierQueryNode.Modifier.MOD_REQ);
                    }
                }
            }

            return node;
        }

        private void ReadTree(IQueryNode node)
        {
            if (node is BooleanQueryNode)
            {
                IList<IQueryNode> children = node.Children;

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
                    var removed = this.queryNodeList[this.queryNodeList.Count - 1];
                    this.queryNodeList.Remove(removed);
                    this.queryNodeList.Add(ApplyModifier(removed, node));
                    this.latestNodeVerified = true;
                }
            }
            else if (!(node is BooleanQueryNode))
            {
                this.queryNodeList.Add(ApplyModifier(node, node.Parent));
                this.latestNodeVerified = false;
            }
        }

        public QueryConfigHandler QueryConfigHandler
        {
            get
            {
                return queryConfig;
            }
            set
            {
                queryConfig = value;
            }
        }
    }
}
