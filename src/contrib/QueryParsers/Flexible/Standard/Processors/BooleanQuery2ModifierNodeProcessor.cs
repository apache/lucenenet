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
    public class BooleanQuery2ModifierNodeProcessor : IQueryNodeProcessor
    {
        internal const string TAG_REMOVE = "remove";
        internal const string TAG_MODIFIER = "wrapWithModifier";
        internal const string TAG_BOOLEAN_ROOT = "booleanRoot";

        QueryConfigHandler queryConfigHandler;

        private readonly List<IQueryNode> childrenBuffer = new List<IQueryNode>();

        private bool usingAnd = false;

        public BooleanQuery2ModifierNodeProcessor()
        {
            // empty constructor
        }

        public IQueryNode Process(IQueryNode queryTree)
        {
            StandardQueryConfigHandler.Operator? op = QueryConfigHandler.Get(StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR);

            if (op == null)
            {
                throw new ArgumentException(
                    "StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR should be set on the QueryConfigHandler");
            }

            this.usingAnd = StandardQueryConfigHandler.Operator.AND == op;

            return ProcessIteration(queryTree);
        }

        protected void ProcessChildren(IQueryNode queryTree)
        {
            IList<IQueryNode> children = queryTree.Children;
            if (children != null && children.Count > 0)
            {
                foreach (IQueryNode child in children)
                {
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

        protected void FillChildrenBufferAndApplyModifiery(IQueryNode parent)
        {
            foreach (IQueryNode node in parent.Children)
            {
                if (node.ContainsTag(TAG_REMOVE))
                {
                    FillChildrenBufferAndApplyModifiery(node);
                }
                else if (node.ContainsTag(TAG_MODIFIER))
                {
                    childrenBuffer.Add(ApplyModifier(node, (ModifierQueryNode.Modifier)node.GetTag(TAG_MODIFIER)));
                }
                else
                {
                    childrenBuffer.Add(node);
                }
            }
        }

        protected IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node.ContainsTag(TAG_BOOLEAN_ROOT))
            {
                this.childrenBuffer.Clear();
                FillChildrenBufferAndApplyModifiery(node);
                node.Set(childrenBuffer);
            }
            return node;
        }

        protected IQueryNode PreProcessNode(IQueryNode node)
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
                    TagModifierButDoNotOverride(node, ModifierQueryNode.Modifier.MOD_REQ);
                }
            }
            return node;
        }

        protected bool IsDefaultBooleanQueryNode(IQueryNode toTest)
        {
            return toTest != null && typeof(BooleanQueryNode).Equals(toTest.GetType());
        }

        private IQueryNode ApplyModifier(IQueryNode node, ModifierQueryNode.Modifier mod)
        {
            // check if modifier is not already defined and is default
            if (!(node is ModifierQueryNode))
            {
                return new BooleanModifierNode(node, mod);

            }
            else
            {
                ModifierQueryNode modNode = (ModifierQueryNode)node;

                if (modNode.ModifierValue == ModifierQueryNode.Modifier.MOD_NONE)
                {
                    return new ModifierQueryNode(modNode.Child, mod);
                }
            }

            return node;
        }

        protected void TagModifierButDoNotOverride(IQueryNode node, ModifierQueryNode.Modifier mod)
        {
            if (node is ModifierQueryNode)
            {
                ModifierQueryNode modNode = (ModifierQueryNode)node;
                if (modNode.ModifierValue == ModifierQueryNode.Modifier.MOD_NONE)
                {
                    node.SetTag(TAG_MODIFIER, mod);
                }
            }
            else
            {
                node.SetTag(TAG_MODIFIER, ModifierQueryNode.Modifier.MOD_REQ);
            }
        }

        public QueryConfigHandler QueryConfigHandler
        {
            get
            {
                return queryConfigHandler;
            }
            set
            {
                queryConfigHandler = value;
            }
        }
    }
}
