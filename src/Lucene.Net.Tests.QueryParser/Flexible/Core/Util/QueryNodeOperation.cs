/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core.Util
{
	/// <summary>Allow joining 2 QueryNode Trees, into one.</summary>
	/// <remarks>Allow joining 2 QueryNode Trees, into one.</remarks>
	public sealed class QueryNodeOperation
	{
		public QueryNodeOperation()
		{
		}

		private enum ANDOperation
		{
			BOTH,
			Q1,
			Q2,
			NONE
		}

		// Exists only to defeat instantiation.
		/// <summary>perform a logical and of 2 QueryNode trees.</summary>
		/// <remarks>
		/// perform a logical and of 2 QueryNode trees. if q1 and q2 are ANDQueryNode
		/// nodes it uses head Node from q1 and adds the children of q2 to q1 if q1 is
		/// a AND node and q2 is not, add q2 as a child of the head node of q1 if q2 is
		/// a AND node and q1 is not, add q1 as a child of the head node of q2 if q1
		/// and q2 are not ANDQueryNode nodes, create a AND node and make q1 and q2
		/// children of that node if q1 or q2 is null it returns the not null node if
		/// q1 = q2 = null it returns null
		/// </remarks>
		public static QueryNode LogicalAnd(QueryNode q1, QueryNode q2)
		{
			if (q1 == null)
			{
				return q2;
			}
			if (q2 == null)
			{
				return q1;
			}
			QueryNodeOperation.ANDOperation op = null;
			if (q1 is AndQueryNode && q2 is AndQueryNode)
			{
				op = QueryNodeOperation.ANDOperation.BOTH;
			}
			else
			{
				if (q1 is AndQueryNode)
				{
					op = QueryNodeOperation.ANDOperation.Q1;
				}
				else
				{
					if (q1 is AndQueryNode)
					{
						op = QueryNodeOperation.ANDOperation.Q2;
					}
					else
					{
						op = QueryNodeOperation.ANDOperation.NONE;
					}
				}
			}
			QueryNode result = null;
			switch (op)
			{
				case QueryNodeOperation.ANDOperation.NONE:
				{
					IList<QueryNode> children = new AList<QueryNode>();
					children.AddItem(q1.CloneTree());
					children.AddItem(q2.CloneTree());
					result = new AndQueryNode(children);
					return result;
				}

				case QueryNodeOperation.ANDOperation.Q1:
				{
					result = q1.CloneTree();
					result.Add(q2.CloneTree());
					return result;
				}

				case QueryNodeOperation.ANDOperation.Q2:
				{
					result = q2.CloneTree();
					result.Add(q1.CloneTree());
					return result;
				}

				case QueryNodeOperation.ANDOperation.BOTH:
				{
					result = q1.CloneTree();
					result.Add(q2.CloneTree().GetChildren());
					return result;
				}
			}
			return null;
		}
	}
}
