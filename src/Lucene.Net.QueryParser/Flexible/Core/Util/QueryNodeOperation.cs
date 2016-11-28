using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Util
{
    /// <summary>
    /// Allow joining 2 QueryNode Trees, into one.
    /// </summary>
    public sealed class QueryNodeOperation
    {
        private QueryNodeOperation()
        {
            // Exists only to defeat instantiation.
        }

        private enum ANDOperation
        {
            BOTH, Q1, Q2, NONE
        }

        /**
         * perform a logical and of 2 QueryNode trees. if q1 and q2 are ANDQueryNode
         * nodes it uses head Node from q1 and adds the children of q2 to q1 if q1 is
         * a AND node and q2 is not, add q2 as a child of the head node of q1 if q2 is
         * a AND node and q1 is not, add q1 as a child of the head node of q2 if q1
         * and q2 are not ANDQueryNode nodes, create a AND node and make q1 and q2
         * children of that node if q1 or q2 is null it returns the not null node if
         * q1 = q2 = null it returns null
         */
        public static IQueryNode LogicalAnd(IQueryNode q1, IQueryNode q2)
        {
            if (q1 == null)
                return q2;
            if (q2 == null)
                return q1;

            ANDOperation op/* = null*/;
            if (q1 is AndQueryNode && q2 is AndQueryNode)
                op = ANDOperation.BOTH;
            else if (q1 is AndQueryNode)
                op = ANDOperation.Q1;
            else if (q1 is AndQueryNode)
                op = ANDOperation.Q2;
            else
                op = ANDOperation.NONE;

            //try
            //{
            IQueryNode result = null;
            switch (op)
            {
                case ANDOperation.NONE:
                    List<IQueryNode> children = new List<IQueryNode>();
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
                    result.Add(q2.CloneTree().GetChildren());
                    return result;
            }
            //}
            //catch (CloneNotSupportedException e)
            //{
            //    throw new QueryNodeError(e);
            //}

            return null;

        }
    }
}
