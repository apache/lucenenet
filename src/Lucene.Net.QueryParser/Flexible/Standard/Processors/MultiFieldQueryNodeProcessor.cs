using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    /// <summary>
    /// This processor is used to expand terms so the query looks for the same term
    /// in different fields. It also boosts a query based on its field.
    /// <para/>
    /// This processor looks for every {@link FieldableNode} contained in the query
    /// node tree. If a {@link FieldableNode} is found, it checks if there is a
    /// {@link ConfigurationKeys#MULTI_FIELDS} defined in the {@link QueryConfigHandler}. If
    /// there is, the {@link FieldableNode} is cloned N times and the clones are
    /// added to a {@link BooleanQueryNode} together with the original node. N is
    /// defined by the number of fields that it will be expanded to. The
    /// {@link BooleanQueryNode} is returned.
    /// </summary>
    /// <seealso cref="ConfigurationKeys#MULTI_FIELDS"/>
    public class MultiFieldQueryNodeProcessor : QueryNodeProcessorImpl
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

            if (node is IFieldableNode)
            {
                this.processChildren = false;
                IFieldableNode fieldNode = (IFieldableNode)node;

                if (fieldNode.Field == null)
                {
                    string[] fields = GetQueryConfigHandler().Get(ConfigurationKeys.MULTI_FIELDS);

                    if (fields == null)
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
                            List<IQueryNode> children = new List<IQueryNode>();
                            children.Add(fieldNode);

                            for (int i = 1; i < fields.Length; i++)
                            {
                                //try
                                //{
                                fieldNode = (IFieldableNode)fieldNode.CloneTree();
                                fieldNode.Field = fields[i];

                                children.Add(fieldNode);

                                //}
                                //catch (CloneNotSupportedException e)
                                //{
                                //    // should never happen
                                //}

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
