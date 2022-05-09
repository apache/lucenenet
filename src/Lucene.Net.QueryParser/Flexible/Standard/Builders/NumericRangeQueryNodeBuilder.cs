using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Search;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
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
    /// Builds <see cref="NumericRangeQuery"/>s out of <see cref="NumericRangeQueryNode"/>s.
    /// </summary>
    /// <seealso cref="NumericRangeQuery"/>
    /// <seealso cref="NumericRangeQueryNode"/>
    public class NumericRangeQueryNodeBuilder : IStandardQueryBuilder
    {
        /// <summary>
        /// Constructs a <see cref="NumericRangeQueryNodeBuilder"/> object.
        /// </summary>
        public NumericRangeQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            NumericRangeQueryNode numericRangeNode = (NumericRangeQueryNode)queryNode;

            NumericQueryNode lowerNumericNode = (NumericQueryNode)numericRangeNode.LowerBound;
            NumericQueryNode upperNumericNode = (NumericQueryNode)numericRangeNode.UpperBound;

            J2N.Numerics.Number lowerNumber = lowerNumericNode.Value;
            J2N.Numerics.Number upperNumber = upperNumericNode.Value;

            NumericConfig numericConfig = numericRangeNode.NumericConfig;
            NumericType numberType = numericConfig.Type;
            string field = StringUtils.ToString(numericRangeNode.Field);
            bool minInclusive = numericRangeNode.IsLowerInclusive;
            bool maxInclusive = numericRangeNode.IsUpperInclusive;
            int precisionStep = numericConfig.PrecisionStep;

            switch (numberType)
            {
                case NumericType.INT64:
                    return NumericRangeQuery.NewInt64Range(field, precisionStep,
                        lowerNumber?.ToInt64(), upperNumber?.ToInt64(), minInclusive, maxInclusive);

                case NumericType.INT32:
                    return NumericRangeQuery.NewInt32Range(field, precisionStep,
                        lowerNumber?.ToInt32(), upperNumber?.ToInt32(), minInclusive,
                        maxInclusive);

                case NumericType.SINGLE:
                    return NumericRangeQuery.NewSingleRange(field, precisionStep,
                        lowerNumber?.ToSingle(), upperNumber?.ToSingle(), minInclusive,
                        maxInclusive);

                case NumericType.DOUBLE:
                    return NumericRangeQuery.NewDoubleRange(field, precisionStep,
                        lowerNumber?.ToDouble(), upperNumber?.ToDouble(), minInclusive,
                        maxInclusive);

                default:
                    // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
                    throw new QueryNodeException(string.Format(
                      QueryParserMessages.UNSUPPORTED_NUMERIC_DATA_TYPE, numberType));
            }
        }
    }
}
