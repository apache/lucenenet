using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    /// <summary>
    /// This processor removes every {@link QueryNode} that is not a leaf and has not
    /// children. If after processing the entire tree the root node is not a leaf and
    /// has no children, a {@link MatchNoDocsQueryNode} object is returned.
    /// <para/>
    /// This processor is used at the end of a pipeline to avoid invalid query node
    /// tree structures like a {@link GroupQueryNode} or {@link ModifierQueryNode}
    /// with no children.
    /// </summary>
    /// <seealso cref="QueryNode"/>
    /// <seealso cref="MatchNoDocsQueryNode"/>
    public class RemoveEmptyNonLeafQueryNodeProcessor : QueryNodeProcessorImpl
    {
        private List<IQueryNode> childrenBuffer = new List<IQueryNode>();

        public RemoveEmptyNonLeafQueryNodeProcessor()
        {
            // empty constructor
        }

        public override IQueryNode Process(IQueryNode queryTree)
        {
            queryTree = base.Process(queryTree);

            if (!queryTree.IsLeaf)
            {
                IList<IQueryNode> children = queryTree.GetChildren();

                if (children == null || children.Count == 0)
                {
                    return new MatchNoDocsQueryNode();
                }
            }

            return queryTree;
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            try
            {
                foreach (IQueryNode child in children)
                {
                    if (!child.IsLeaf)
                    {
                        IList<IQueryNode> grandChildren = child.GetChildren();

                        if (grandChildren != null && grandChildren.Count > 0)
                        {
                            this.childrenBuffer.Add(child);
                        }
                    }
                    else
                    {
                        this.childrenBuffer.Add(child);
                    }
                }

                children.Clear();
                children.AddRange(this.childrenBuffer);
            }
            finally
            {
                this.childrenBuffer.Clear();
            }

            return children;
        }
    }
}
