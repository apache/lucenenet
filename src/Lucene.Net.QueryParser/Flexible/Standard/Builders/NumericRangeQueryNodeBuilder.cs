using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /// <summary>
    /// Builds {@link NumericRangeQuery}s out of {@link NumericRangeQueryNode}s.
    /// </summary>
    /// <seealso cref="NumericRangeQuery"/>
    /// <seealso cref="NumericRangeQueryNode"/>
    public class NumericRangeQueryNodeBuilder : IStandardQueryBuilder
    {
        /**
   * Constructs a {@link NumericRangeQueryNodeBuilder} object.
   */
        public NumericRangeQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            NumericRangeQueryNode numericRangeNode = (NumericRangeQueryNode)queryNode;

            NumericQueryNode lowerNumericNode = (NumericQueryNode)numericRangeNode.LowerBound;
            NumericQueryNode upperNumericNode = (NumericQueryNode)numericRangeNode.UpperBound;

            /*Number*/
            object lowerNumber = lowerNumericNode.Value;
            /*Number*/
            object upperNumber = upperNumericNode.Value;

            NumericConfig numericConfig = numericRangeNode.NumericConfig;
            FieldType.NumericType numberType = numericConfig.Type;
            string field = StringUtils.ToString(numericRangeNode.Field);
            bool minInclusive = numericRangeNode.IsLowerInclusive;
            bool maxInclusive = numericRangeNode.IsUpperInclusive;
            int precisionStep = numericConfig.PrecisionStep;

            switch (numberType)
            {
                case FieldType.NumericType.LONG:
                    return NumericRangeQuery.NewLongRange(field, precisionStep,
                        (long?)lowerNumber, (long?)upperNumber, minInclusive, maxInclusive);

                case FieldType.NumericType.INT:
                    return NumericRangeQuery.NewIntRange(field, precisionStep,
                        (int?)lowerNumber, (int?)upperNumber, minInclusive,
                        maxInclusive);

                case FieldType.NumericType.FLOAT:
                    return NumericRangeQuery.NewFloatRange(field, precisionStep,
                        (float?)lowerNumber, (float?)upperNumber, minInclusive,
                        maxInclusive);

                case FieldType.NumericType.DOUBLE:
                    return NumericRangeQuery.NewDoubleRange(field, precisionStep,
                        (double?)lowerNumber, (double?)upperNumber, minInclusive,
                        maxInclusive);

                default:
                    throw new QueryNodeException(new MessageImpl(
                      QueryParserMessages.UNSUPPORTED_NUMERIC_DATA_TYPE, numberType));
            }
        }
    }
}
