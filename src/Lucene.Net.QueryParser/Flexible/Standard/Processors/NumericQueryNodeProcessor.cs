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

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
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
    /// This processor is used to convert <see cref="FieldQueryNode"/>s to
    /// <see cref="NumericRangeQueryNode"/>s. It looks for
    /// <see cref="ConfigurationKeys.NUMERIC_CONFIG"/> set in the <see cref="FieldConfig"/> of
    /// every <see cref="FieldQueryNode"/> found. If
    /// <see cref="ConfigurationKeys.NUMERIC_CONFIG"/> is found, it considers that
    /// <see cref="FieldQueryNode"/> to be a numeric query and convert it to
    /// <see cref="NumericRangeQueryNode"/> with upper and lower inclusive and lower and
    /// upper equals to the value represented by the <see cref="FieldQueryNode"/> converted
    /// to <see cref="object"/> representing a .NET numeric type. It means that <b>field:1</b> is converted to <b>field:[1
    /// TO 1]</b>.
    /// <para/>
    /// Note that <see cref="FieldQueryNode"/>s children of a
    /// <see cref="IRangeQueryNode"/> are ignored.
    /// </summary>
    /// <seealso cref="ConfigurationKeys.NUMERIC_CONFIG"/>
    /// <seealso cref="FieldQueryNode"/>
    /// <seealso cref="NumericConfig"/>
    /// <seealso cref="NumericQueryNode"/>
    public class NumericQueryNodeProcessor : QueryNodeProcessor
    {
        /// <summary>
        /// Constructs a <see cref="NumericQueryNodeProcessor"/> object.
        /// </summary>
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
                                }
                                catch (FormatException e)
                                {
                                    throw new QueryNodeParseException(new Message(
                                        QueryParserMessages.COULD_NOT_PARSE_NUMBER, fieldNode
                                            .GetTextAsString(), numberFormat.GetType()
                                            .AssemblyQualifiedName), e);
                                }

                                switch (numericConfig.Type)
                                {
                                    case NumericType.INT64:
                                        number = Convert.ToInt64(number);
                                        break;
                                    case NumericType.INT32:
                                        number = Convert.ToInt32(number);
                                        break;
                                    case NumericType.DOUBLE:
                                        number = Convert.ToDouble(number);
                                        break;
                                    case NumericType.SINGLE:
                                        number = Convert.ToSingle(number);
                                        break;
                                }

                            }
                            else
                            {
                                throw new QueryNodeParseException(new Message(
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
