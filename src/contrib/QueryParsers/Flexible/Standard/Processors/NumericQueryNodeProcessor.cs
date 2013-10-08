using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    public class NumericQueryNodeProcessor : QueryNodeProcessor
    {
        public NumericQueryNodeProcessor()
        {
            // empty constructor
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is FieldQueryNode
       && !(node.Parent is IRangeQueryNode))
            {

                var config = QueryConfigHandler;

                if (config != null)
                {
                    FieldQueryNode fieldNode = (FieldQueryNode)node;
                    FieldConfig fieldConfig = config.GetFieldConfig(fieldNode.FieldAsString);

                    if (fieldConfig != null)
                    {
                        NumericConfig numericConfig = fieldConfig.Get(StandardQueryConfigHandler.ConfigurationKeys.NUMERIC_CONFIG);

                        if (numericConfig != null)
                        {

                            NumberFormatInfo numberFormat = numericConfig.NumberFormat;
                            String text = fieldNode.TextAsString;
                            double? doubleNumber = null;
                            float? floatNumber = null;
                            long? longNumber = null;
                            int? intNumber = null;

                            if (text.Length > 0)
                            {

                                try
                                {
                                    // .NET Port: we have to do the switch here instead of down below
                                    // so we know what type number it is.
                                    switch (numericConfig.Type)
                                    {
                                        case FieldType.NumericType.LONG:
                                            longNumber = long.Parse(text, numberFormat);
                                            break;
                                        case FieldType.NumericType.INT:
                                            intNumber = int.Parse(text, numberFormat);
                                            break;
                                        case FieldType.NumericType.DOUBLE:
                                            doubleNumber = double.Parse(text, numberFormat);
                                            break;
                                        case FieldType.NumericType.FLOAT:
                                            floatNumber = float.Parse(text, numberFormat);
                                            break;
                                    }
                                }
                                catch (FormatException e)
                                {
                                    throw new QueryNodeParseException(new Message(
                                        QueryParserMessages.COULD_NOT_PARSE_NUMBER, fieldNode
                                            .TextAsString, numberFormat.GetType().FullName), e);
                                }

                                // .NET Port: switch here moved up into try block above

                            }
                            else
                            {
                                throw new QueryNodeParseException(new Message(
                                    QueryParserMessages.NUMERIC_CANNOT_BE_EMPTY, fieldNode.FieldAsString));
                            }

                            // .NET Port: we have to add these if's since we don't know what type it is and don't have
                            // a common "Number" base type like Java
                            if (longNumber.HasValue)
                            {
                                NumericQueryNode<long> lowerNode = new NumericQueryNode<long>(fieldNode.Field, longNumber.Value, numberFormat);
                                NumericQueryNode<long> upperNode = new NumericQueryNode<long>(fieldNode.Field, longNumber.Value, numberFormat);
                                return new NumericRangeQueryNode<long>(lowerNode, upperNode, true, true,
                                numericConfig);
                            }
                            else if (intNumber.HasValue)
                            {
                                NumericQueryNode<int> lowerNode = new NumericQueryNode<int>(fieldNode.Field, intNumber.Value, numberFormat);
                                NumericQueryNode<int> upperNode = new NumericQueryNode<int>(fieldNode.Field, intNumber.Value, numberFormat);
                                return new NumericRangeQueryNode<int>(lowerNode, upperNode, true, true,
                                numericConfig);
                            }
                            else if (doubleNumber.HasValue)
                            {
                                NumericQueryNode<double> lowerNode = new NumericQueryNode<double>(fieldNode.Field, doubleNumber.Value, numberFormat);
                                NumericQueryNode<double> upperNode = new NumericQueryNode<double>(fieldNode.Field, doubleNumber.Value, numberFormat);
                                return new NumericRangeQueryNode<double>(lowerNode, upperNode, true, true,
                                numericConfig);
                            }
                            else if (floatNumber.HasValue)
                            {
                                NumericQueryNode<float> lowerNode = new NumericQueryNode<float>(fieldNode.Field, floatNumber.Value, numberFormat);
                                NumericQueryNode<float> upperNode = new NumericQueryNode<float>(fieldNode.Field, floatNumber.Value, numberFormat);
                                return new NumericRangeQueryNode<float>(lowerNode, upperNode, true, true,
                                numericConfig);
                            }
                            else
                            {
                                throw new InvalidOperationException("No numbers to use for numeric query node!");
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
