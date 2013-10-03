using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Util
{
    public static class QueryNodeOperation
    {
        private enum ANDOperation
        {
            BOTH, Q1, Q2, NONE
        }

        public static IQueryNode LogicalAnd(IQueryNode q1, IQueryNode q2)
        {
            if (q1 == null)
                return q2;
            if (q2 == null)
                return q1;

            ANDOperation? op = null;
            if (q1 is AndQueryNode && q2 is AndQueryNode)
                op = ANDOperation.BOTH;
            else if (q1 is AndQueryNode)
                op = ANDOperation.Q1;
            else if (q1 is AndQueryNode)
                op = ANDOperation.Q2;
            else
                op = ANDOperation.NONE;

            try
            {
                IQueryNode result = null;
                switch (op.GetValueOrDefault())
                {
                    case ANDOperation.NONE:
                        IList<IQueryNode> children = new List<IQueryNode>();
                        children.Add(q1.CloneTree());
                        children.Add(q2.CloneTree());
                        result = new AndQueryNode(children);
                        return result;
                    case ANDOperation.Q1:
                        result = q1.CloneTree();
                        result.Add(q2.CloneTree());
                        return result;
                    case ANDOperation.Q2:
                        result = q2.CloneTree();
                        result.Add(q1.CloneTree());
                        return result;
                    case ANDOperation.BOTH:
                        result = q1.CloneTree();
                        result.Add(q2.CloneTree().Children);
                        return result;
                }
            }
            catch (NotSupportedException e)
            {
                throw new QueryNodeError(e);
            }

            return null;
        }
    }
}
