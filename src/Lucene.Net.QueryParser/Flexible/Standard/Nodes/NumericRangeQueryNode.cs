using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Lucene.Net.Documents.FieldType;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    /// <summary>
    /// This query node represents a range query composed by {@link NumericQueryNode}
    /// bounds, which means the bound values are {@link Number}s.
    /// </summary>
    /// <seealso cref="NumericQueryNode"/>
    /// <seealso cref="AbstractRangeQueryNode{T}"/>
    public class NumericRangeQueryNode : AbstractRangeQueryNode<NumericQueryNode>
    {
        public NumericConfig numericConfig;

        /**
       * Constructs a {@link NumericRangeQueryNode} object using the given
       * {@link NumericQueryNode} as its bounds and {@link NumericConfig}.
       * 
       * @param lower the lower bound
       * @param upper the upper bound
       * @param lowerInclusive <code>true</code> if the lower bound is inclusive, otherwise, <code>false</code>
       * @param upperInclusive <code>true</code> if the upper bound is inclusive, otherwise, <code>false</code>
       * @param numericConfig the {@link NumericConfig} that represents associated with the upper and lower bounds
       * 
       * @see #setBounds(NumericQueryNode, NumericQueryNode, boolean, boolean, NumericConfig)
       */
        public NumericRangeQueryNode(NumericQueryNode lower, NumericQueryNode upper,
            bool lowerInclusive, bool upperInclusive, NumericConfig numericConfig)
        {
            SetBounds(lower, upper, lowerInclusive, upperInclusive, numericConfig);
        }

        private static NumericType GetNumericDataType(/*Number*/ object number)
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
                    new MessageImpl(
                        QueryParserMessages.NUMBER_CLASS_NOT_SUPPORTED_BY_NUMERIC_RANGE_QUERY,
                        number.GetType()));
            }

        }

        /**
         * Sets the upper and lower bounds of this range query node and the
         * {@link NumericConfig} associated with these bounds.
         * 
         * @param lower the lower bound
         * @param upper the upper bound
         * @param lowerInclusive <code>true</code> if the lower bound is inclusive, otherwise, <code>false</code>
         * @param upperInclusive <code>true</code> if the upper bound is inclusive, otherwise, <code>false</code>
         * @param numericConfig the {@link NumericConfig} that represents associated with the upper and lower bounds
         * 
         */
        public virtual void SetBounds(NumericQueryNode lower, NumericQueryNode upper,
            bool lowerInclusive, bool upperInclusive, NumericConfig numericConfig)
        {

            if (numericConfig == null)
            {
                throw new ArgumentException("numericConfig cannot be null!");
            }

            NumericType? lowerNumberType, upperNumberType;

            if (lower != null && lower.Value != null)
            {
                lowerNumberType = GetNumericDataType(lower.Value);
            }
            else
            {
                lowerNumberType = null;
            }

            if (upper != null && upper.Value != null)
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

        /**
         * Returns the {@link NumericConfig} associated with the lower and upper bounds.
         * 
         * @return the {@link NumericConfig} associated with the lower and upper bounds
         */
        public virtual NumericConfig NumericConfig
        {
            get { return this.numericConfig; }
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
