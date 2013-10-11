using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    public class NumericRangeQueryNodeProcessor : QueryNodeProcessor
    {
        public NumericRangeQueryNodeProcessor()
        {
            // empty constructor
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is TermRangeQueryNode)
            {
                var config = QueryConfigHandler;

                if (config != null)
                {
                    TermRangeQueryNode termRangeNode = (TermRangeQueryNode)node;
                    FieldConfig fieldConfig = config.GetFieldConfig(StringUtils.ToString(termRangeNode.Field));

                    if (fieldConfig != null)
                    {

                        NumericConfig numericConfig = fieldConfig.Get(StandardQueryConfigHandler.ConfigurationKeys.NUMERIC_CONFIG);

                        if (numericConfig != null)
                        {

                            FieldQueryNode lower = termRangeNode.LowerBound;
                            FieldQueryNode upper = termRangeNode.UpperBound;

                            String lowerText = lower.TextAsString;
                            String upperText = upper.TextAsString;
                            NumberFormatInfo numberFormat = numericConfig.NumberFormat;
                            long? lowerNumberLong = null, upperNumberLong = null;
                            int? lowerNumberInt = null, upperNumberInt = null;
                            double? lowerNumberDouble = null, upperNumberDouble = null;
                            float? lowerNumberFloat = null, upperNumberFloat = null;

                            if (lowerText.Length > 0)
                            {
                                try
                                {
                                    // .NET Port: we had to move the switch from below here 
                                    // due to missing a common "Number" base class in .NET

                                    switch (numericConfig.Type)
                                    {
                                        case FieldType.NumericType.LONG:
                                            lowerNumberLong = long.Parse(lowerText, numberFormat);
                                            break;
                                        case FieldType.NumericType.INT:
                                            lowerNumberInt = int.Parse(lowerText, numberFormat);
                                            break;
                                        case FieldType.NumericType.DOUBLE:
                                            lowerNumberDouble = double.Parse(lowerText, numberFormat);
                                            break;
                                        case FieldType.NumericType.FLOAT:
                                            lowerNumberFloat = float.Parse(lowerText, numberFormat);
                                            break;
                                        default:
                                            throw new InvalidOperationException("Unknown numeric type");
                                    }
                                }
                                catch (ParseException e)
                                {
                                    throw new QueryNodeParseException(new Message(
                                        QueryParserMessages.COULD_NOT_PARSE_NUMBER, lower
                                            .TextAsString, numberFormat.GetType()
                                            .FullName), e);
                                }

                            }

                            if (upperText.Length > 0)
                            {
                                try
                                {
                                    // .NET Port: we had to move the switch from below here 
                                    // due to missing a common "Number" base class in .NET

                                    switch (numericConfig.Type)
                                    {
                                        case FieldType.NumericType.LONG:
                                            upperNumberLong = long.Parse(upperText, numberFormat);
                                            break;
                                        case FieldType.NumericType.INT:
                                            upperNumberInt = int.Parse(upperText, numberFormat);
                                            break;
                                        case FieldType.NumericType.DOUBLE:
                                            upperNumberDouble = double.Parse(upperText, numberFormat);
                                            break;
                                        case FieldType.NumericType.FLOAT:
                                            upperNumberFloat = float.Parse(upperText, numberFormat);
                                            break;
                                        default:
                                            throw new InvalidOperationException("Unknown numeric type");
                                    }
                                }
                                catch (ParseException e)
                                {
                                    throw new QueryNodeParseException(new Message(
                                        QueryParserMessages.COULD_NOT_PARSE_NUMBER, upper
                                            .TextAsString, numberFormat.GetType()
                                            .FullName), e);
                                }

                            }

                            // .NET Port: we moved this switch into each of the try blocks above
                            // due to missing a common "Number" base type in .NET like they have in Java
                            //switch (numericConfig.getType())
                            //{
                            //    case LONG:
                            //        if (upperNumber != null) upperNumber = upperNumber.longValue();
                            //        if (lowerNumber != null) lowerNumber = lowerNumber.longValue();
                            //        break;
                            //    case INT:
                            //        if (upperNumber != null) upperNumber = upperNumber.intValue();
                            //        if (lowerNumber != null) lowerNumber = lowerNumber.intValue();
                            //        break;
                            //    case DOUBLE:
                            //        if (upperNumber != null) upperNumber = upperNumber.doubleValue();
                            //        if (lowerNumber != null) lowerNumber = lowerNumber.doubleValue();
                            //        break;
                            //    case FLOAT:
                            //        if (upperNumber != null) upperNumber = upperNumber.floatValue();
                            //        if (lowerNumber != null) lowerNumber = lowerNumber.floatValue();
                            //}

                            // .NET Port: again, since we don't have a common "Number" base type,
                            // we have to make this a 4-way if
                            if (lowerNumberLong.HasValue)
                            {
                                NumericQueryNode<long> lowerNode = new NumericQueryNode<long>(
                                    termRangeNode.Field, lowerNumberLong.Value, numberFormat);
                                NumericQueryNode<long> upperNode = new NumericQueryNode<long>(
                                    termRangeNode.Field, upperNumberLong.Value, numberFormat);

                                bool lowerInclusive = termRangeNode.IsLowerInclusive;
                                bool upperInclusive = termRangeNode.IsUpperInclusive;

                                return new NumericRangeQueryNode<long>(lowerNode, upperNode,
                                    lowerInclusive, upperInclusive, numericConfig);
                            }
                            else if (lowerNumberInt.HasValue)
                            {
                                NumericQueryNode<int> lowerNode = new NumericQueryNode<int>(
                                    termRangeNode.Field, lowerNumberInt.Value, numberFormat);
                                NumericQueryNode<int> upperNode = new NumericQueryNode<int>(
                                    termRangeNode.Field, upperNumberInt.Value, numberFormat);

                                bool lowerInclusive = termRangeNode.IsLowerInclusive;
                                bool upperInclusive = termRangeNode.IsUpperInclusive;

                                return new NumericRangeQueryNode<int>(lowerNode, upperNode,
                                    lowerInclusive, upperInclusive, numericConfig);
                            }
                            else if (lowerNumberDouble.HasValue)
                            {
                                NumericQueryNode<double> lowerNode = new NumericQueryNode<double>(
                                    termRangeNode.Field, lowerNumberDouble.Value, numberFormat);
                                NumericQueryNode<double> upperNode = new NumericQueryNode<double>(
                                    termRangeNode.Field, upperNumberDouble.Value, numberFormat);

                                bool lowerInclusive = termRangeNode.IsLowerInclusive;
                                bool upperInclusive = termRangeNode.IsUpperInclusive;

                                return new NumericRangeQueryNode<double>(lowerNode, upperNode,
                                    lowerInclusive, upperInclusive, numericConfig);
                            }
                            else if (lowerNumberFloat.HasValue)
                            {
                                NumericQueryNode<float> lowerNode = new NumericQueryNode<float>(
                                    termRangeNode.Field, lowerNumberFloat.Value, numberFormat);
                                NumericQueryNode<float> upperNode = new NumericQueryNode<float>(
                                    termRangeNode.Field, upperNumberFloat.Value, numberFormat);

                                bool lowerInclusive = termRangeNode.IsLowerInclusive;
                                bool upperInclusive = termRangeNode.IsUpperInclusive;

                                return new NumericRangeQueryNode<float>(lowerNode, upperNode,
                                    lowerInclusive, upperInclusive, numericConfig);
                            }
                            else
                            {
                                throw new InvalidOperationException("Can't determine what type of NumericQueryNode to use");
                            }
                        }

                    }

                }

            }

            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
