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
    /// <summary>
    /// This processor is used to apply the correct {@link ModifierQueryNode} to
    /// {@link BooleanQueryNode}s children. This is a variant of
    /// {@link BooleanModifiersQueryNodeProcessor} which ignores precedence.
    /// <para/>
    /// The {@link StandardSyntaxParser} knows the rules of precedence, but lucene
    /// does not. e.g. <code>(A AND B OR C AND D)</code> ist treated like
    /// <code>(+A +B +C +D)</code>.
    /// <para/>
    /// This processor walks through the query node tree looking for
    /// {@link BooleanQueryNode}s. If an {@link AndQueryNode} is found, every child,
    /// which is not a {@link ModifierQueryNode} or the {@link ModifierQueryNode} is
    /// {@link Modifier#MOD_NONE}, becomes a {@link Modifier#MOD_REQ}. For default
    /// {@link BooleanQueryNode}, it checks the default operator is
    /// {@link Operator#AND}, if it is, the same operation when an
    /// {@link AndQueryNode} is found is applied to it. Each {@link BooleanQueryNode}
    /// which direct parent is also a {@link BooleanQueryNode} is removed (to ignore
    /// the rules of precedence).
    /// </summary>
    /// <seealso cref="ConfigurationKeys#DEFAULT_OPERATOR"/>
    /// <seealso cref="BooleanModifiersQueryNodeProcessor"/>
    public class BooleanQuery2ModifierNodeProcessor : IQueryNodeProcessor
    {
        internal readonly static string TAG_REMOVE = "remove";
        internal readonly static string TAG_MODIFIER = "wrapWithModifier";
        internal readonly static string TAG_BOOLEAN_ROOT = "booleanRoot";

        QueryConfigHandler queryConfigHandler;

        private readonly List<IQueryNode> childrenBuffer = new List<IQueryNode>();

        private bool usingAnd = false;

        public BooleanQuery2ModifierNodeProcessor()
        {
            // empty constructor
        }


        public virtual IQueryNode Process(IQueryNode queryTree)
        {
            Operator? op = GetQueryConfigHandler().Get(
                ConfigurationKeys.DEFAULT_OPERATOR);

            if (op == null)
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
            IQueryNode parent = node.GetParent();
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

        private IQueryNode ApplyModifier(IQueryNode node, Modifier mod)
        {

            // check if modifier is not already defined and is default
            if (!(node is ModifierQueryNode))
            {
                return new BooleanModifierNode(node, mod);

            }
            else
            {
                ModifierQueryNode modNode = (ModifierQueryNode)node;

                if (modNode.GetModifier() == Modifier.MOD_NONE)
                {
                    return new ModifierQueryNode(modNode.GetChild(), mod);
                }

            }

            return node;

        }

        protected virtual void TagModifierButDoNotOverride(IQueryNode node, Modifier mod)
        {
            if (node is ModifierQueryNode)
            {
                ModifierQueryNode modNode = (ModifierQueryNode)node;
                if (modNode.GetModifier() == Modifier.MOD_NONE)
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
