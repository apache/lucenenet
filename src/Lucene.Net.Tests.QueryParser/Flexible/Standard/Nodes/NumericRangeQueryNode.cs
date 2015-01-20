/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using Org.Apache.Lucene.Document;
using Org.Apache.Lucene.Queryparser.Flexible.Core;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Messages;
using Org.Apache.Lucene.Queryparser.Flexible.Messages;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes
{
	/// <summary>
	/// This query node represents a range query composed by
	/// <see cref="NumericQueryNode">NumericQueryNode</see>
	/// bounds, which means the bound values are
	/// <see cref="Sharpen.Number">Sharpen.Number</see>
	/// s.
	/// </summary>
	/// <seealso cref="NumericQueryNode">NumericQueryNode</seealso>
	/// <seealso cref="AbstractRangeQueryNode{T}">AbstractRangeQueryNode&lt;T&gt;</seealso>
	public class NumericRangeQueryNode : AbstractRangeQueryNode<NumericQueryNode>
	{
		public NumericConfig numericConfig;

		/// <summary>
		/// Constructs a
		/// <see cref="NumericRangeQueryNode">NumericRangeQueryNode</see>
		/// object using the given
		/// <see cref="NumericQueryNode">NumericQueryNode</see>
		/// as its bounds and
		/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig
		/// 	</see>
		/// .
		/// </summary>
		/// <param name="lower">the lower bound</param>
		/// <param name="upper">the upper bound</param>
		/// <param name="lowerInclusive"><code>true</code> if the lower bound is inclusive, otherwise, <code>false</code>
		/// 	</param>
		/// <param name="upperInclusive"><code>true</code> if the upper bound is inclusive, otherwise, <code>false</code>
		/// 	</param>
		/// <param name="numericConfig">
		/// the
		/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig
		/// 	</see>
		/// that represents associated with the upper and lower bounds
		/// </param>
		/// <seealso cref="SetBounds(NumericQueryNode, NumericQueryNode, bool, bool, Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig)
		/// 	">SetBounds(NumericQueryNode, NumericQueryNode, bool, bool, Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig)
		/// 	</seealso>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public NumericRangeQueryNode(NumericQueryNode lower, NumericQueryNode upper, bool
			 lowerInclusive, bool upperInclusive, NumericConfig numericConfig)
		{
			SetBounds(lower, upper, lowerInclusive, upperInclusive, numericConfig);
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		private static FieldType.NumericType GetNumericDataType(Number number)
		{
			if (number is long)
			{
				return FieldType.NumericType.LONG;
			}
			else
			{
				if (number is int)
				{
					return FieldType.NumericType.INT;
				}
				else
				{
					if (number is double)
					{
						return FieldType.NumericType.DOUBLE;
					}
					else
					{
						if (number is float)
						{
							return FieldType.NumericType.FLOAT;
						}
						else
						{
							throw new QueryNodeException(new MessageImpl(QueryParserMessages.NUMBER_CLASS_NOT_SUPPORTED_BY_NUMERIC_RANGE_QUERY
								, number.GetType()));
						}
					}
				}
			}
		}

		/// <summary>
		/// Sets the upper and lower bounds of this range query node and the
		/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig
		/// 	</see>
		/// associated with these bounds.
		/// </summary>
		/// <param name="lower">the lower bound</param>
		/// <param name="upper">the upper bound</param>
		/// <param name="lowerInclusive"><code>true</code> if the lower bound is inclusive, otherwise, <code>false</code>
		/// 	</param>
		/// <param name="upperInclusive"><code>true</code> if the upper bound is inclusive, otherwise, <code>false</code>
		/// 	</param>
		/// <param name="numericConfig">
		/// the
		/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig
		/// 	</see>
		/// that represents associated with the upper and lower bounds
		/// </param>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual void SetBounds(NumericQueryNode lower, NumericQueryNode upper, bool
			 lowerInclusive, bool upperInclusive, NumericConfig numericConfig)
		{
			if (numericConfig == null)
			{
				throw new ArgumentException("numericConfig cannot be null!");
			}
			FieldType.NumericType lowerNumberType;
			FieldType.NumericType upperNumberType;
			if (lower != null && lower.GetValue() != null)
			{
				lowerNumberType = GetNumericDataType(lower.GetValue());
			}
			else
			{
				lowerNumberType = null;
			}
			if (upper != null && upper.GetValue() != null)
			{
				upperNumberType = GetNumericDataType(upper.GetValue());
			}
			else
			{
				upperNumberType = null;
			}
			if (lowerNumberType != null && !lowerNumberType.Equals(numericConfig.GetType()))
			{
				throw new ArgumentException("lower value's type should be the same as numericConfig type: "
					 + lowerNumberType + " != " + numericConfig.GetType());
			}
			if (upperNumberType != null && !upperNumberType.Equals(numericConfig.GetType()))
			{
				throw new ArgumentException("upper value's type should be the same as numericConfig type: "
					 + upperNumberType + " != " + numericConfig.GetType());
			}
			base.SetBounds(lower, upper, lowerInclusive, upperInclusive);
			this.numericConfig = numericConfig;
		}

		/// <summary>
		/// Returns the
		/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig
		/// 	</see>
		/// associated with the lower and upper bounds.
		/// </summary>
		/// <returns>
		/// the
		/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig
		/// 	</see>
		/// associated with the lower and upper bounds
		/// </returns>
		public virtual NumericConfig GetNumericConfig()
		{
			return this.numericConfig;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder("<numericRange lowerInclusive='");
			sb.Append(IsLowerInclusive()).Append("' upperInclusive='").Append(IsUpperInclusive
				()).Append("' precisionStep='" + numericConfig.GetPrecisionStep()).Append("' type='"
				 + numericConfig.GetType()).Append("'>\n");
			sb.Append(GetLowerBound()).Append('\n');
			sb.Append(GetUpperBound()).Append('\n');
			sb.Append("</numericRange>");
			return sb.ToString();
		}
	}
}
