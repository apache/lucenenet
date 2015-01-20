/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Parser;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes
{
	/// <summary>This class should be extended by nodes intending to represent range queries.
	/// 	</summary>
	/// <remarks>This class should be extended by nodes intending to represent range queries.
	/// 	</remarks>
	/// <?></?>
	public class AbstractRangeQueryNode<T> : QueryNodeImpl, RangeQueryNode<FieldValuePairQueryNode
		<object>> where T:FieldValuePairQueryNode<object>
	{
		private bool lowerInclusive;

		private bool upperInclusive;

		/// <summary>
		/// Constructs an
		/// <see cref="AbstractRangeQueryNode{T}">AbstractRangeQueryNode&lt;T&gt;</see>
		/// , it should be invoked only by
		/// its extenders.
		/// </summary>
		public AbstractRangeQueryNode()
		{
			SetLeaf(false);
			Allocate();
		}

		/// <summary>Returns the field associated with this node.</summary>
		/// <remarks>Returns the field associated with this node.</remarks>
		/// <returns>the field associated with this node</returns>
		/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FieldableNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FieldableNode
		/// 	</seealso>
		public virtual CharSequence GetField()
		{
			CharSequence field = null;
			T lower = GetLowerBound();
			T upper = GetUpperBound();
			if (lower != null)
			{
				field = lower.GetField();
			}
			else
			{
				if (upper != null)
				{
					field = upper.GetField();
				}
			}
			return field;
		}

		/// <summary>Sets the field associated with this node.</summary>
		/// <remarks>Sets the field associated with this node.</remarks>
		/// <param name="fieldName">the field associated with this node</param>
		public virtual void SetField(CharSequence fieldName)
		{
			T lower = GetLowerBound();
			T upper = GetUpperBound();
			if (lower != null)
			{
				lower.SetField(fieldName);
			}
			if (upper != null)
			{
				upper.SetField(fieldName);
			}
		}

		/// <summary>Returns the lower bound node.</summary>
		/// <remarks>Returns the lower bound node.</remarks>
		/// <returns>the lower bound node.</returns>
		public virtual T GetLowerBound()
		{
			return (T)GetChildren()[0];
		}

		/// <summary>Returns the upper bound node.</summary>
		/// <remarks>Returns the upper bound node.</remarks>
		/// <returns>the upper bound node.</returns>
		public virtual T GetUpperBound()
		{
			return (T)GetChildren()[1];
		}

		/// <summary>Returns whether the lower bound is inclusive or exclusive.</summary>
		/// <remarks>Returns whether the lower bound is inclusive or exclusive.</remarks>
		/// <returns><code>true</code> if the lower bound is inclusive, otherwise, <code>false</code>
		/// 	</returns>
		public virtual bool IsLowerInclusive()
		{
			return lowerInclusive;
		}

		/// <summary>Returns whether the upper bound is inclusive or exclusive.</summary>
		/// <remarks>Returns whether the upper bound is inclusive or exclusive.</remarks>
		/// <returns><code>true</code> if the upper bound is inclusive, otherwise, <code>false</code>
		/// 	</returns>
		public virtual bool IsUpperInclusive()
		{
			return upperInclusive;
		}

		/// <summary>Sets the lower and upper bounds.</summary>
		/// <remarks>Sets the lower and upper bounds.</remarks>
		/// <param name="lower">the lower bound, <code>null</code> if lower bound is open</param>
		/// <param name="upper">the upper bound, <code>null</code> if upper bound is open</param>
		/// <param name="lowerInclusive"><code>true</code> if the lower bound is inclusive, otherwise, <code>false</code>
		/// 	</param>
		/// <param name="upperInclusive"><code>true</code> if the upper bound is inclusive, otherwise, <code>false</code>
		/// 	</param>
		/// <seealso cref="AbstractRangeQueryNode{T}.GetLowerBound()">AbstractRangeQueryNode&lt;T&gt;.GetLowerBound()
		/// 	</seealso>
		/// <seealso cref="AbstractRangeQueryNode{T}.GetUpperBound()">AbstractRangeQueryNode&lt;T&gt;.GetUpperBound()
		/// 	</seealso>
		/// <seealso cref="AbstractRangeQueryNode{T}.IsLowerInclusive()">AbstractRangeQueryNode&lt;T&gt;.IsLowerInclusive()
		/// 	</seealso>
		/// <seealso cref="AbstractRangeQueryNode{T}.IsUpperInclusive()">AbstractRangeQueryNode&lt;T&gt;.IsUpperInclusive()
		/// 	</seealso>
		public virtual void SetBounds(T lower, T upper, bool lowerInclusive, bool upperInclusive
			)
		{
			if (lower != null && upper != null)
			{
				string lowerField = StringUtils.ToString(lower.GetField());
				string upperField = StringUtils.ToString(upper.GetField());
				if ((upperField != null || lowerField != null) && ((upperField != null && !upperField
					.Equals(lowerField)) || !lowerField.Equals(upperField)))
				{
					throw new ArgumentException("lower and upper bounds should have the same field name!"
						);
				}
				this.lowerInclusive = lowerInclusive;
				this.upperInclusive = upperInclusive;
				AList<QueryNode> children = new AList<QueryNode>(2);
				children.AddItem(lower);
				children.AddItem(upper);
				Set(children);
			}
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escapeSyntaxParser)
		{
			StringBuilder sb = new StringBuilder();
			T lower = GetLowerBound();
			T upper = GetUpperBound();
			if (lowerInclusive)
			{
				sb.Append('[');
			}
			else
			{
				sb.Append('{');
			}
			if (lower != null)
			{
				sb.Append(lower.ToQueryString(escapeSyntaxParser));
			}
			else
			{
				sb.Append("...");
			}
			sb.Append(' ');
			if (upper != null)
			{
				sb.Append(upper.ToQueryString(escapeSyntaxParser));
			}
			else
			{
				sb.Append("...");
			}
			if (upperInclusive)
			{
				sb.Append(']');
			}
			else
			{
				sb.Append('}');
			}
			return sb.ToString();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder("<").Append(GetType().GetCanonicalName());
			sb.Append(" lowerInclusive=").Append(IsLowerInclusive());
			sb.Append(" upperInclusive=").Append(IsUpperInclusive());
			sb.Append(">\n\t");
			sb.Append(GetUpperBound()).Append("\n\t");
			sb.Append(GetLowerBound()).Append("\n");
			sb.Append("</").Append(GetType().GetCanonicalName()).Append(">\n");
			return sb.ToString();
		}
	}
}
