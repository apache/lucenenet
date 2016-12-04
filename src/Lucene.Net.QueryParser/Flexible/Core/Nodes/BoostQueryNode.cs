using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Messages;
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
    /// A {@link BoostQueryNode} boosts the QueryNode tree which is under this node.
    /// So, it must only and always have one child.
    /// 
    /// The boost value may vary from 0.0 to 1.0.
    /// </summary>
    public class BoostQueryNode : QueryNodeImpl
    {
        private float value = 0;

        /**
         * Constructs a boost node
         * 
         * @param query
         *          the query to be boosted
         * @param value
         *          the boost value, it may vary from 0.0 to 1.0
         */
        public BoostQueryNode(IQueryNode query, float value)
        {
            if (query == null)
            {
                throw new QueryNodeError(new MessageImpl(
                    QueryParserMessages.NODE_ACTION_NOT_SUPPORTED, "query", "null"));
            }

            this.value = value;
            IsLeaf = false;
            Allocate();
            Add(query);
        }

        /**
         * Returns the single child which this node boosts.
         * 
         * @return the single child which this node boosts
         */
        public virtual IQueryNode Child
        {
            get
            {
                IList<IQueryNode> children = GetChildren();

                if (children == null || children.Count == 0)
                {
                    return null;
                }

                return children[0];
            }
        }

        /**
         * Returns the boost value. It may vary from 0.0 to 1.0.
         * 
         * @return the boost value
         */
        public virtual float Value
        {
            get { return this.value; }
        }

        /**
         * Returns the boost value parsed to a string.
         * 
         * @return the parsed value
         */
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
            if (Child == null)
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
