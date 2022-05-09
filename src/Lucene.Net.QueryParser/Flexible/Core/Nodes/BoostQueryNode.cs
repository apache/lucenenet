using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
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
    /// A <see cref="BoostQueryNode"/> boosts the QueryNode tree which is under this node.
    /// So, it must only and always have one child.
    /// 
    /// The boost value may vary from 0.0 to 1.0.
    /// </summary>
    public class BoostQueryNode : QueryNode
    {
        private float value = 0;

        /// <summary>
        /// Constructs a boost node
        /// </summary>
        /// <param name="query">the query to be boosted</param>
        /// <param name="value">the boost value, it may vary from 0.0 to 1.0</param>
        public BoostQueryNode(IQueryNode query, float value)
        {
            // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
            // LUCENENET: Added paramName parameter and changed to the same error message as the default of ArgumentNullException.
            // However, we still need this to be an error type so it is not caught in StandardSyntaxParser.
            if (query is null)
                throw new QueryNodeError(QueryParserMessages.ARGUMENT_CANNOT_BE_NULL, nameof(query));

            this.value = value;
            IsLeaf = false;
            Allocate();
            Add(query);
        }

        /// <summary>
        /// Gets the single child which this node boosts.
        /// </summary>
        public virtual IQueryNode Child
        {
            get
            {
                IList<IQueryNode> children = GetChildren();

                if (children is null || children.Count == 0)
                {
                    return null;
                }

                return children[0];
            }
        }

        /// <summary>
        /// Gets the boost value. It may vary from 0.0 to 1.0.
        /// </summary>
        public virtual float Value => this.value;

        /// <summary>
        /// Returns the boost value parsed to a string.
        /// </summary>
        /// <returns>the parsed value</returns>
        private string GetValueString()
        {
            float f = this.value;
            if (f == (long)f)
                return "" + (long)f;
            else
                return "" + f.ToString("0.0#######"); // LUCENENET TODO: Culture
        }

        public override string ToString()
        {
            return "<boost value='" + GetValueString() + "'>" + "\n"
                + Child.ToString() + "\n</boost>";
        }

        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (Child is null)
                return "";
            return Child.ToQueryString(escapeSyntaxParser) + "^"
                + GetValueString();
        }

        public override IQueryNode CloneTree()
        {
            BoostQueryNode clone = (BoostQueryNode)base.CloneTree();

            clone.value = this.value;

            return clone;
        }
    }
}
