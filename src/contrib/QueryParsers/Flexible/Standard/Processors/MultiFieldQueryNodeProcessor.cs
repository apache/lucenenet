using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
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
            if (node is IFieldableNode)
            {
                this.processChildren = false;
                IFieldableNode fieldNode = (IFieldableNode)node;

                if (fieldNode.Field == null)
                {
                    string[] fields = QueryConfigHandler.Get(StandardQueryConfigHandler.ConfigurationKeys.MULTI_FIELDS);

                    if (fields == null)
                    {
                        throw new ArgumentException(
                            "StandardQueryConfigHandler.ConfigurationKeys.MULTI_FIELDS should be set on the QueryConfigHandler");
                    }

                    if (fields != null && fields.Length > 0)
                    {
                        fieldNode.Field = new StringCharSequenceWrapper(fields[0]);

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
                                try
                                {
                                    fieldNode = (IFieldableNode)fieldNode.CloneTree();
                                    fieldNode.Field = new StringCharSequenceWrapper(fields[i]);

                                    children.Add(fieldNode);
                                }
                                catch (NotSupportedException)
                                {
                                    // should never happen
                                }
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
