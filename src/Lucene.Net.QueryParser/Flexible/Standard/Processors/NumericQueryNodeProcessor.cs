using Lucene.Net.Documents;
using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    /// <summary>
    /// This processor is used to convert {@link FieldQueryNode}s to
    /// {@link NumericRangeQueryNode}s. It looks for
    /// {@link ConfigurationKeys#NUMERIC_CONFIG} set in the {@link FieldConfig} of
    /// every {@link FieldQueryNode} found. If
    /// {@link ConfigurationKeys#NUMERIC_CONFIG} is found, it considers that
    /// {@link FieldQueryNode} to be a numeric query and convert it to
    /// {@link NumericRangeQueryNode} with upper and lower inclusive and lower and
    /// upper equals to the value represented by the {@link FieldQueryNode} converted
    /// to {@link Number}. It means that <b>field:1</b> is converted to <b>field:[1
    /// TO 1]</b>.
    /// <para/>
    /// Note that {@link FieldQueryNode}s children of a
    /// {@link RangeQueryNode} are ignored.
    /// </summary>
    /// <seealso cref="ConfigurationKeys#NUMERIC_CONFIG"/>
    /// <seealso cref="FieldQueryNode"/>
    /// <seealso cref="NumericConfig"/>
    /// <seealso cref="NumericQueryNode"/>
    public class NumericQueryNodeProcessor : QueryNodeProcessorImpl
    {
        /**
   * Constructs a {@link NumericQueryNodeProcessor} object.
   */
        public NumericQueryNodeProcessor()
        {
            // empty constructor
        }


        protected override IQueryNode PostProcessNode(IQueryNode node)
        {

            if (node is FieldQueryNode
                && !(node.Parent is IRangeQueryNode))
            {

                QueryConfigHandler config = GetQueryConfigHandler();

                if (config != null)
                {
                    FieldQueryNode fieldNode = (FieldQueryNode)node;
                    FieldConfig fieldConfig = config.GetFieldConfig(fieldNode
                        .GetFieldAsString());

                    if (fieldConfig != null)
                    {
                        NumericConfig numericConfig = fieldConfig
                            .Get(ConfigurationKeys.NUMERIC_CONFIG);

                        if (numericConfig != null)
                        {

                            NumberFormat numberFormat = numericConfig.NumberFormat;
                            string text = fieldNode.GetTextAsString();
                            /*Number*/
                            object number = null;

                            if (text.Length > 0)
                            {

                                try
                                {
                                    number = numberFormat.Parse(text);
                                    //number = decimal.Parse(text, NumberStyles.Any);// LUCENENET TODO: use the current culture?

                                }
                                catch (FormatException e)
                                {
                                    throw new QueryNodeParseException(new MessageImpl(
                                        QueryParserMessages.COULD_NOT_PARSE_NUMBER, fieldNode
                                            .GetTextAsString(), numberFormat.GetType()
                                            .AssemblyQualifiedName), e);
                                }

                                switch (numericConfig.Type)
                                {
                                    case FieldType.NumericType.LONG:
                                        number = Convert.ToInt64(number);
                                        break;
                                    case FieldType.NumericType.INT:
                                        number = Convert.ToInt32(number);
                                        break;
                                    case FieldType.NumericType.DOUBLE:
                                        number = Convert.ToDouble(number);
                                        break;
                                    case FieldType.NumericType.FLOAT:
                                        number = Convert.ToSingle(number);
                                        break;
                                }

                            }
                            else
                            {
                                throw new QueryNodeParseException(new MessageImpl(
                                    QueryParserMessages.NUMERIC_CANNOT_BE_EMPTY, fieldNode.GetFieldAsString()));
                            }

                            NumericQueryNode lowerNode = new NumericQueryNode(fieldNode
                                .Field, number, numberFormat);
                            NumericQueryNode upperNode = new NumericQueryNode(fieldNode
                                .Field, number, numberFormat);

                            return new NumericRangeQueryNode(lowerNode, upperNode, true, true,
                                numericConfig);

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
