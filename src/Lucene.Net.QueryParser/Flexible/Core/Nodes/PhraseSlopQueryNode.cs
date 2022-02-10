using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;

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
    /// Query node for <see cref="Search.PhraseQuery"/>'s slop factor.
    /// </summary>
    public class PhraseSlopQueryNode : QueryNode, IFieldableNode
    {
        private int value = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="value"></param>
        /// <exception cref="QueryNodeError">throw in overridden method to disallow</exception>
        public PhraseSlopQueryNode(IQueryNode query, int value)
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

        public virtual IQueryNode GetChild()
        {
            return GetChildren()[0];
        }

        public virtual int Value => this.value;

        private string GetValueString()
        {
            float f = this.value;
            if (f == (long)f)
                return "" + (long)f;
            else
                return "" + f;
        }

        public override string ToString()
        {
            return "<phraseslop value='" + GetValueString() + "'>" + "\n"
                + GetChild().ToString() + "\n</phraseslop>";
        }

        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (GetChild() is null)
                return "";
            return GetChild().ToQueryString(escapeSyntaxParser) + "~"
                + GetValueString();
        }

        public override IQueryNode CloneTree()
        {
            PhraseSlopQueryNode clone = (PhraseSlopQueryNode)base.CloneTree();

            clone.value = this.value;

            return clone;
        }

        public virtual string Field
        {
            get
            {
                IQueryNode child = GetChild();

                if (child is IFieldableNode fieldableNode)
                {
                    return fieldableNode.Field;
                }

                return null;
            }
            set
            {
                IQueryNode child = GetChild();

                if (child is IFieldableNode fieldableNode)
                {
                    fieldableNode.Field = value;
                }
            }
        }
    }
}
