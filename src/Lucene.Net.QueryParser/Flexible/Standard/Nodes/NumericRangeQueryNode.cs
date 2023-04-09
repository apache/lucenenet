using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using System;
using System.Text;
using NumericType = Lucene.Net.Documents.NumericType;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// This query node represents a range query composed by <see cref="NumericQueryNode"/>
    /// bounds, which means the bound values are <see cref="object"/>s representing a .NET numeric type.
    /// </summary>
    /// <seealso cref="NumericQueryNode"/>
    /// <seealso cref="AbstractRangeQueryNode{T}"/>
    public class NumericRangeQueryNode : AbstractRangeQueryNode<NumericQueryNode>
    {
        private NumericConfig numericConfig; // LUCENENET specific: made private and added a public setter to the property

        /// <summary>
        /// Constructs a <see cref="NumericRangeQueryNode"/> object using the given
        /// <see cref="NumericQueryNode"/> as its bounds and <see cref="Config.NumericConfig"/>.
        /// </summary>
        /// <param name="lower">the lower bound</param>
        /// <param name="upper">the upper bound</param>
        /// <param name="lowerInclusive"><c>true</c> if the lower bound is inclusive, otherwise, <c>false</c></param>
        /// <param name="upperInclusive"><c>true</c> if the upper bound is inclusive, otherwise, <c>false</c></param>
        /// <param name="numericConfig">the <see cref="Config.NumericConfig"/> that represents associated with the upper and lower bounds</param>
        /// <seealso cref="SetBounds(NumericQueryNode, NumericQueryNode, bool, bool, NumericConfig)"/>
        public NumericRangeQueryNode(NumericQueryNode lower, NumericQueryNode upper,
            bool lowerInclusive, bool upperInclusive, NumericConfig numericConfig)
        {
            SetBoundsInternal(lower, upper, lowerInclusive, upperInclusive, numericConfig);
        }

        private static NumericType GetNumericDataType(J2N.Numerics.Number number)
        {
            if (number is J2N.Numerics.Int64)
            {
                return NumericType.INT64;
            }
            else if (number is J2N.Numerics.Int32)
            {
                return NumericType.INT32;
            }
            else if (number is J2N.Numerics.Double)
            {
                return NumericType.DOUBLE;
            }
            else if (number is J2N.Numerics.Single)
            {
                return NumericType.SINGLE;
            }
            else
            {
                // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
                throw new QueryNodeException(
                    string.Format(
                        QueryParserMessages.NUMBER_CLASS_NOT_SUPPORTED_BY_NUMERIC_RANGE_QUERY,
                        number.GetType()));
            }
        }

        /// <summary>
        /// Sets the upper and lower bounds of this range query node and the
        /// <see cref="Config.NumericConfig"/> associated with these bounds.
        ///
        /// NOTE: When overriding this method, be aware that the constructor of this class calls 
        /// a private method and not this virtual method. So if you need to override
        /// the behavior during the initialization, call your own private method from the constructor
        /// with whatever custom behavior you need.
        /// </summary>
        /// <param name="lower">the lower bound</param>
        /// <param name="upper">the upper bound</param>
        /// <param name="lowerInclusive"><c>true</c> if the lower bound is inclusive, otherwise, <c>false</c></param>
        /// <param name="upperInclusive"><c>true</c> if the upper bound is inclusive, otherwise, <c>false</c></param>
        /// <param name="numericConfig">the <see cref="Config.NumericConfig"/> that represents associated with the upper and lower bounds</param>
        public virtual void SetBounds(NumericQueryNode lower, NumericQueryNode upper,
            bool lowerInclusive, bool upperInclusive, NumericConfig numericConfig) =>
                SetBoundsInternal(lower, upper, lowerInclusive, upperInclusive, numericConfig);

        // LUCENENET specific - created a private method that can be called from SetBounds
        // in order to avoid calling virtual methods from the constructor.
        private void SetBoundsInternal(NumericQueryNode lower, NumericQueryNode upper,
            bool lowerInclusive, bool upperInclusive, NumericConfig numericConfig)
        {
            if (numericConfig is null)
            {
                throw new ArgumentNullException(nameof(numericConfig), "numericConfig cannot be null!"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }

            NumericType lowerNumberType, upperNumberType;

            if (lower != null && lower.Value != null)
            {
                lowerNumberType = GetNumericDataType(lower.Value);
            }
            else
            {
                lowerNumberType = NumericType.NONE;
            }

            if (upper != null && upper.Value != null)
            {
                upperNumberType = GetNumericDataType(upper.Value);
            }
            else
            {
                upperNumberType = NumericType.NONE;
            }

            if (lowerNumberType != NumericType.NONE
                && !lowerNumberType.Equals(numericConfig.Type))
            {
                throw new ArgumentException(
                    "lower value's type should be the same as numericConfig type: "
                        + lowerNumberType + " != " + numericConfig.Type);
            }

            if (upperNumberType != NumericType.NONE
                && !upperNumberType.Equals(numericConfig.Type))
            {
                throw new ArgumentException(
                    "upper value's type should be the same as numericConfig type: "
                        + upperNumberType + " != " + numericConfig.Type);
            }

            base.SetBounds(lower, upper, lowerInclusive, upperInclusive);
            this.numericConfig = numericConfig;
        }

        /// <summary>
        /// Gets the <see cref="Config.NumericConfig"/> associated with the lower and upper bounds.
        /// </summary>
        public virtual NumericConfig NumericConfig
        {
            get => this.numericConfig;
            set => this.numericConfig = value; // LUCENENET specific: made the field private and added setter (confusing)
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
