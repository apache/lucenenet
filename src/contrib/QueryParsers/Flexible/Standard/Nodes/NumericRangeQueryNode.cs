using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NumericType = Lucene.Net.Documents.FieldType.NumericType;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    public class NumericRangeQueryNode<T> : AbstractRangeQueryNode<NumericQueryNode<T>>
        where T : struct
    {
        public NumericConfig numericConfig;

        public NumericRangeQueryNode(NumericQueryNode<T> lower, NumericQueryNode<T> upper,
            bool lowerInclusive, bool upperInclusive, NumericConfig numericConfig)
        {
            SetBounds(lower, upper, lowerInclusive, upperInclusive, numericConfig);
        }

        private static NumericType GetNumericDataType(T number)
        {
            if (number is long)
            {
                return NumericType.LONG;
            }
            else if (number is int)
            {
                return NumericType.INT;
            }
            else if (number is double)
            {
                return NumericType.DOUBLE;
            }
            else if (number is float)
            {
                return NumericType.FLOAT;
            }
            else
            {
                throw new QueryNodeException(
                    new Message(
                        QueryParserMessages.NUMBER_CLASS_NOT_SUPPORTED_BY_NUMERIC_RANGE_QUERY,
                        number.GetType()));
            }
        }

        public void SetBounds(NumericQueryNode<T> lower, NumericQueryNode<T> upper,
            bool lowerInclusive, bool upperInclusive, NumericConfig numericConfig)
        {

            if (numericConfig == null)
            {
                throw new ArgumentException("numericConfig cannot be null!");
            }

            NumericType? lowerNumberType, upperNumberType;

            if (lower != null /* && lower.Value != null */)
            {
                lowerNumberType = GetNumericDataType(lower.Value);
            }
            else
            {
                lowerNumberType = null;
            }

            if (upper != null /* && upper.Value != null */)
            {
                upperNumberType = GetNumericDataType(upper.Value);
            }
            else
            {
                upperNumberType = null;
            }

            if (lowerNumberType != null
                && !lowerNumberType.Equals(numericConfig.Type))
            {
                throw new ArgumentException(
                    "lower value's type should be the same as numericConfig type: "
                        + lowerNumberType + " != " + numericConfig.Type);
            }

            if (upperNumberType != null
                && !upperNumberType.Equals(numericConfig.Type))
            {
                throw new ArgumentException(
                    "upper value's type should be the same as numericConfig type: "
                        + upperNumberType + " != " + numericConfig.Type);
            }

            base.SetBounds(lower, upper, lowerInclusive, upperInclusive);
            this.numericConfig = numericConfig;
        }

        public NumericConfig NumericConfig
        {
            get
            {
                return this.numericConfig;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("<numericRange lowerInclusive='");

            sb.Append(IsLowerInclusive).Append("' upperInclusive='").Append(
                IsUpperInclusive).Append(
                "' precisionStep='" + numericConfig.PrecisionStep).Append(
                "' type='" + numericConfig.Type).Append("'>\n");

            sb.Append(LowerBound).Append('\n');
            sb.Append(UpperBound).Append('\n');
            sb.Append("</numericRange>");

            return sb.ToString();    
        }
    }
}
