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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    /// <summary>
    /// This processor is used to convert {@link TermRangeQueryNode}s to
    /// {@link NumericRangeQueryNode}s. It looks for
    /// {@link ConfigurationKeys#NUMERIC_CONFIG} set in the {@link FieldConfig} of
    /// every {@link TermRangeQueryNode} found. If
    /// {@link ConfigurationKeys#NUMERIC_CONFIG} is found, it considers that
    /// {@link TermRangeQueryNode} to be a numeric range query and convert it to
    /// {@link NumericRangeQueryNode}.
    /// </summary>
    /// <seealso cref="ConfigurationKeys#NUMERIC_CONFIG"/>
    /// <seealso cref="TermRangeQueryNode"/>
    /// <seealso cref="NumericConfig"/>
    /// <seealso cref="NumericRangeQueryNode"/>
    public class NumericRangeQueryNodeProcessor : QueryNodeProcessorImpl
    {
        /**
   * Constructs an empty {@link NumericRangeQueryNode} object.
   */
        public NumericRangeQueryNodeProcessor()
        {
            // empty constructor
        }


        protected override IQueryNode PostProcessNode(IQueryNode node)
        {

            if (node is TermRangeQueryNode)
            {
                QueryConfigHandler config = GetQueryConfigHandler();

                if (config != null)
                {
                    TermRangeQueryNode termRangeNode = (TermRangeQueryNode)node;
                    FieldConfig fieldConfig = config.GetFieldConfig(StringUtils
                        .ToString(termRangeNode.Field));

                    if (fieldConfig != null)
                    {

                        NumericConfig numericConfig = fieldConfig
                            .Get(ConfigurationKeys.NUMERIC_CONFIG);

                        if (numericConfig != null)
                        {

                            FieldQueryNode lower = (FieldQueryNode)termRangeNode.LowerBound;
                            FieldQueryNode upper = (FieldQueryNode)termRangeNode.UpperBound;

                            string lowerText = lower.GetTextAsString();
                            string upperText = upper.GetTextAsString();
                            /*NumberFormat*/ string numberFormat = numericConfig.NumberFormat;
                            /*Number*/
                            object lowerNumber = null, upperNumber = null;

                            if (lowerText.Length > 0)
                            {

                                try
                                {
                                    //lowerNumber = numberFormat.parse(lowerText);
                                    lowerNumber = decimal.Parse(lowerText, NumberStyles.Any);// LUCENENET TODO: use the current culture?
                                }
                                catch (FormatException e)
                                {
                                    throw new QueryNodeParseException(new MessageImpl(
                                        QueryParserMessages.COULD_NOT_PARSE_NUMBER, lower
                                            .GetTextAsString(), numberFormat.GetType()
                                            .AssemblyQualifiedName), e);
                                }

                            }

                            if (upperText.Length > 0)
                            {

                                try
                                {
                                    //upperNumber = numberFormat.parse(upperText);
                                    upperNumber = decimal.Parse(upperText, NumberStyles.Any);// LUCENENET TODO: use the current culture?
                                }
                                catch (FormatException e)
                                {
                                    throw new QueryNodeParseException(new MessageImpl(
                                        QueryParserMessages.COULD_NOT_PARSE_NUMBER, upper
                                            .GetTextAsString(), numberFormat.GetType()
                                            .AssemblyQualifiedName), e);
                                }

                            }

                            switch (numericConfig.Type)
                            {
                                case FieldType.NumericType.LONG:
                                    if (upperNumber != null) upperNumber = Convert.ToInt64(upperNumber);
                                    if (lowerNumber != null) lowerNumber = Convert.ToInt64(lowerNumber);
                                    break;
                                case FieldType.NumericType.INT:
                                    if (upperNumber != null) upperNumber = Convert.ToInt32(upperNumber);
                                    if (lowerNumber != null) lowerNumber = Convert.ToInt32(lowerNumber);
                                    break;
                                case FieldType.NumericType.DOUBLE:
                                    if (upperNumber != null) upperNumber = Convert.ToDouble(upperNumber);
                                    if (lowerNumber != null) lowerNumber = Convert.ToDouble(lowerNumber);
                                    break;
                                case FieldType.NumericType.FLOAT:
                                    if (upperNumber != null) upperNumber = Convert.ToSingle(upperNumber);
                                    if (lowerNumber != null) lowerNumber = Convert.ToSingle(lowerNumber);
                                    break;
                            }

                            NumericQueryNode lowerNode = new NumericQueryNode(
                                termRangeNode.Field, lowerNumber, numberFormat);
                            NumericQueryNode upperNode = new NumericQueryNode(
                                termRangeNode.Field, upperNumber, numberFormat);

                            bool lowerInclusive = termRangeNode.IsLowerInclusive;
                            bool upperInclusive = termRangeNode.IsUpperInclusive;

                            return new NumericRangeQueryNode(lowerNode, upperNode,
                                lowerInclusive, upperInclusive, numericConfig);

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
