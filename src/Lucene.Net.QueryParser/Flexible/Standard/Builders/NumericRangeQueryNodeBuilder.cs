/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Document;
using Lucene.Net.Queryparser.Flexible.Core;
using Lucene.Net.Queryparser.Flexible.Core.Messages;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Util;
using Lucene.Net.Queryparser.Flexible.Messages;
using Lucene.Net.Queryparser.Flexible.Standard.Builders;
using Lucene.Net.Queryparser.Flexible.Standard.Config;
using Lucene.Net.Queryparser.Flexible.Standard.Nodes;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// Builds
	/// <see cref="Lucene.Net.Search.NumericRangeQuery{T}">Lucene.Net.Search.NumericRangeQuery&lt;T&gt;
	/// 	</see>
	/// s out of
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Nodes.NumericRangeQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Nodes.NumericRangeQueryNode</see>
	/// s.
	/// </summary>
	/// <seealso cref="Lucene.Net.Search.NumericRangeQuery{T}">Lucene.Net.Search.NumericRangeQuery&lt;T&gt;
	/// 	</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.Nodes.NumericRangeQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Nodes.NumericRangeQueryNode</seealso>
	public class NumericRangeQueryNodeBuilder : StandardQueryBuilder
	{
		/// <summary>
		/// Constructs a
		/// <see cref="NumericRangeQueryNodeBuilder">NumericRangeQueryNodeBuilder</see>
		/// object.
		/// </summary>
		public NumericRangeQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual NumericRangeQuery<Number> Build(QueryNode queryNode)
		{
			NumericRangeQueryNode numericRangeNode = (NumericRangeQueryNode)queryNode;
			NumericQueryNode lowerNumericNode = numericRangeNode.GetLowerBound();
			NumericQueryNode upperNumericNode = numericRangeNode.GetUpperBound();
			Number lowerNumber = lowerNumericNode.GetValue();
			Number upperNumber = upperNumericNode.GetValue();
			NumericConfig numericConfig = numericRangeNode.GetNumericConfig();
			FieldType.NumericType numberType = numericConfig.GetType();
			string field = StringUtils.ToString(numericRangeNode.GetField());
			bool minInclusive = numericRangeNode.IsLowerInclusive();
			bool maxInclusive = numericRangeNode.IsUpperInclusive();
			int precisionStep = numericConfig.GetPrecisionStep();
			switch (numberType)
			{
				case FieldType.NumericType.LONG:
				{
					return NumericRangeQuery.NewLongRange(field, precisionStep, (long)lowerNumber, (long
						)upperNumber, minInclusive, maxInclusive);
				}

				case FieldType.NumericType.INT:
				{
					return NumericRangeQuery.NewIntRange(field, precisionStep, (int)lowerNumber, (int
						)upperNumber, minInclusive, maxInclusive);
				}

				case FieldType.NumericType.FLOAT:
				{
					return NumericRangeQuery.NewFloatRange(field, precisionStep, (float)lowerNumber, 
						(float)upperNumber, minInclusive, maxInclusive);
				}

				case FieldType.NumericType.DOUBLE:
				{
					return NumericRangeQuery.NewDoubleRange(field, precisionStep, (double)lowerNumber
						, (double)upperNumber, minInclusive, maxInclusive);
				}

				default:
				{
					throw new QueryNodeException(new MessageImpl(QueryParserMessages.UNSUPPORTED_NUMERIC_DATA_TYPE
						, numberType));
				}
			}
		}
	}
}
