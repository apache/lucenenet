using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfigurationKeys = Lucene.Net.QueryParsers.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys;
using Operator = Lucene.Net.QueryParsers.Flexible.Standard.Config.StandardQueryConfigHandler.Operator;

namespace Lucene.Net.QueryParsers.Flexible.Precedence.Processors
{
    public class BooleanModifiersQueryNodeProcessor : QueryNodeProcessor
    {
        private List<IQueryNode> childrenBuffer = new List<IQueryNode>();

        private bool usingAnd = false;

        public BooleanModifiersQueryNodeProcessor()
        {
            // empty constructor
        }

        public override IQueryNode Process(IQueryNode queryTree)
        {
            Operator? op = QueryConfigHandler.Get(ConfigurationKeys.DEFAULT_OPERATOR);

            if (op == null)
            {
                throw new ArgumentException(
                    "StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR should be set on the QueryConfigHandler");
            }

            this.usingAnd = StandardQueryConfigHandler.Operator.AND == op;

            return base.Process(queryTree);
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is AndQueryNode)
            {
                this.childrenBuffer.Clear();
                IList<IQueryNode> children = node.Children;

                foreach (IQueryNode child in children)
                {
                    this.childrenBuffer.Add(ApplyModifier(child, ModifierQueryNode.Modifier.MOD_REQ));
                }

                node.Set(this.childrenBuffer);
            }
            else if (this.usingAnd && node is BooleanQueryNode
              && !(node is OrQueryNode))
            {

                this.childrenBuffer.Clear();
                IList<IQueryNode> children = node.Children;

                foreach (IQueryNode child in children)
                {
                    this.childrenBuffer.Add(ApplyModifier(child, ModifierQueryNode.Modifier.MOD_REQ));
                }

                node.Set(this.childrenBuffer);
            }

            return node;
        }

        private static IQueryNode ApplyModifier(IQueryNode node, ModifierQueryNode.Modifier mod)
        {
            // check if modifier is not already defined and is default
            if (!(node is ModifierQueryNode))
            {
                return new ModifierQueryNode(node, mod);
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
