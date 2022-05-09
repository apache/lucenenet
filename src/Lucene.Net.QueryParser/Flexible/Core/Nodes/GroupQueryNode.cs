using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// A <see cref="GroupQueryNode"/> represents a location where the original user typed
    /// real parenthesis on the query string. This class is useful for queries like:
    /// a) a AND b OR c b) ( a AND b) OR c
    /// 
    /// Parenthesis might be used to define the boolean operation precedence.
    /// </summary>
    public class GroupQueryNode : QueryNode
    {
        /// <summary>
        /// This IQueryNode is used to identify parenthesis on the original query string
        /// </summary>
        public GroupQueryNode(IQueryNode query)
        {
            // LUCENENET: Factored out NLS/Message/IMessage so end users can optionally utilize the built-in .NET localization.
            // LUCENENET: Added paramName parameter and changed to the same error message as the default of ArgumentNullException.
            // However, we still need this to be an error type so it is not caught in StandardSyntaxParser.
            if (query is null)
                throw new QueryNodeError(QueryParserMessages.ARGUMENT_CANNOT_BE_NULL, nameof(query));

            Allocate();
            IsLeaf = false;
            Add(query);
        }

        public virtual IQueryNode GetChild()
        {
            return GetChildren()[0];
        }

        public override string ToString()
        {
            return "<group>" + "\n" + GetChild().ToString() + "\n</group>";
        }


        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (GetChild() is null)
                return "";

            return "( " + GetChild().ToQueryString(escapeSyntaxParser) + " )";
        }

        public override IQueryNode CloneTree()
        {
            GroupQueryNode clone = (GroupQueryNode)base.CloneTree();

            return clone;
        }

        public virtual void SetChild(IQueryNode child)
        {
            IList<IQueryNode> list = new JCG.List<IQueryNode>
            {
                child
            };
            this.Set(list);
        }
    }
}
