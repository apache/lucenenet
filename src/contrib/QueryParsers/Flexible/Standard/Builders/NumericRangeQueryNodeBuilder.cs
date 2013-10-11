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
    public class NumericRangeQueryNodeBuilder : IStandardQueryBuilder
    {
        public NumericRangeQueryNodeBuilder()
        {
            // empty constructor
        }
                
        public Query Build(IQueryNode queryNode)
        {
            // .NET Port: this method is radically different than the Java version due to
            // our necessitated use of generics instead of the "Number" base type casting.
            
            INumericRangeQueryNode numericRangeNode = (INumericRangeQueryNode)queryNode;

            NumericConfig numericConfig = numericRangeNode.NumericConfig;
            FieldType.NumericType numberType = numericConfig.Type;
            
            String field = StringUtils.ToString(numericRangeNode.Field);
            
            bool minInclusive = numericRangeNode.IsLowerInclusive;
            bool maxInclusive = numericRangeNode.IsUpperInclusive;
            
            int precisionStep = numericConfig.PrecisionStep;

            switch (numberType)
            {
                case FieldType.NumericType.LONG:
                    {
                        var longRangeNode = (NumericRangeQueryNode<long>)numericRangeNode;

                        NumericQueryNode<long> lowerNumericNode = longRangeNode.LowerBound;
                        NumericQueryNode<long> upperNumericNode = longRangeNode.UpperBound;

                        long lowerNumber = lowerNumericNode.Value;
                        long upperNumber = upperNumericNode.Value;

                        return NumericRangeQuery.NewLongRange(field, precisionStep,
                            lowerNumber, upperNumber, minInclusive, maxInclusive);
                    }
                case FieldType.NumericType.INT:
                    {
                        var intRangeNode = (NumericRangeQueryNode<int>)numericRangeNode;

                        NumericQueryNode<int> lowerNumericNode = intRangeNode.LowerBound;
                        NumericQueryNode<int> upperNumericNode = intRangeNode.UpperBound;

                        int lowerNumber = lowerNumericNode.Value;
                        int upperNumber = upperNumericNode.Value;

                        return NumericRangeQuery.NewIntRange(field, precisionStep,
                            lowerNumber, upperNumber, minInclusive, maxInclusive);
                    }
                case FieldType.NumericType.DOUBLE:
                    {
                        var doubleRangeNode = (NumericRangeQueryNode<double>)numericRangeNode;

                        NumericQueryNode<double> lowerNumericNode = doubleRangeNode.LowerBound;
                        NumericQueryNode<double> upperNumericNode = doubleRangeNode.UpperBound;

                        double lowerNumber = lowerNumericNode.Value;
                        double upperNumber = upperNumericNode.Value;

                        return NumericRangeQuery.NewDoubleRange(field, precisionStep,
                            lowerNumber, upperNumber, minInclusive, maxInclusive);
                    }
                case FieldType.NumericType.FLOAT:
                    {
                        var floatRangeNode = (NumericRangeQueryNode<float>)numericRangeNode;

                        NumericQueryNode<float> lowerNumericNode = floatRangeNode.LowerBound;
                        NumericQueryNode<float> upperNumericNode = floatRangeNode.UpperBound;

                        float lowerNumber = lowerNumericNode.Value;
                        float upperNumber = upperNumericNode.Value;

                        return NumericRangeQuery.NewFloatRange(field, precisionStep,
                            lowerNumber, upperNumber, minInclusive, maxInclusive);
                    }
                default:
                    throw new QueryNodeException(new Message(
                      QueryParserMessages.UNSUPPORTED_NUMERIC_DATA_TYPE, numberType));
            }
        }

        object IQueryBuilder.Build(IQueryNode queryNode)
        {
            return Build(queryNode);
        }
    }
}
