using J2N.Text;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
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
    /// Processes <see cref="Search.TermRangeQuery"/>s with open ranges.
    /// </summary>
    public class OpenRangeQueryNodeProcessor : QueryNodeProcessor
    {
        public readonly static string OPEN_RANGE_TOKEN = "*";

        public OpenRangeQueryNodeProcessor() { }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is TermRangeQueryNode rangeNode)
            {
                FieldQueryNode lowerNode = (FieldQueryNode)rangeNode.LowerBound;
                FieldQueryNode upperNode = (FieldQueryNode)rangeNode.UpperBound;
                ICharSequence lowerText = lowerNode.Text;
                ICharSequence upperText = upperNode.Text;

                if (OPEN_RANGE_TOKEN.Equals(upperNode.GetTextAsString(), StringComparison.Ordinal)
                    && (!(upperText is UnescapedCharSequence unescapedUpperText) || !unescapedUpperText.WasEscaped(0)))
                {
                    upperText = "".AsCharSequence();
                }

                if (OPEN_RANGE_TOKEN.Equals(lowerNode.GetTextAsString(), StringComparison.Ordinal)
                    && (!(lowerText is UnescapedCharSequence unescapedLowerText) || !unescapedLowerText.WasEscaped(0)))
                {
                    lowerText = "".AsCharSequence();
                }

                lowerNode.Text = lowerText;
                upperNode.Text = upperText;
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
