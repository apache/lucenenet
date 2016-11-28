using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Precedence.Processors
{
    /// <summary>
    /// This processor is used to apply the correct {@link ModifierQueryNode} to {@link BooleanQueryNode}s children.
    /// <para>
    /// It walks through the query node tree looking for {@link BooleanQueryNode}s. If an {@link AndQueryNode} is found,
    /// every child, which is not a {@link ModifierQueryNode} or the {@link ModifierQueryNode} 
    /// is {@link Modifier#MOD_NONE}, becomes a {@link Modifier#MOD_REQ}. For any other
    /// {@link BooleanQueryNode} which is not an {@link OrQueryNode}, it checks the default operator is {@link Operator#AND},
    /// if it is, the same operation when an {@link AndQueryNode} is found is applied to it.
    /// </para>
    /// </summary>
    /// <seealso cref="ConfigurationKeys#DEFAULT_OPERATOR"/>
    /// <seealso cref="PrecedenceQueryParser#setDefaultOperator"/>
    public class BooleanModifiersQueryNodeProcessor : QueryNodeProcessorImpl
    {
        private List<IQueryNode> childrenBuffer = new List<IQueryNode>();

        private bool usingAnd = false;

        public BooleanModifiersQueryNodeProcessor()
        {
            // empty constructor
        }


        public override IQueryNode Process(IQueryNode queryTree)
        {
            Operator? op = GetQueryConfigHandler().Get(ConfigurationKeys.DEFAULT_OPERATOR);

            if (op == null)
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

        private IQueryNode ApplyModifier(IQueryNode node, Modifier mod)
        {

            // check if modifier is not already defined and is default
            if (!(node is ModifierQueryNode))
            {
                return new ModifierQueryNode(node, mod);

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
