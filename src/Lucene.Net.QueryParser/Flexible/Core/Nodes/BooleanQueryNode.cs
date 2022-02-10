using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using System.Collections.Generic;
using System.Text;

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
    /// A <see cref="BooleanQueryNode"/> represents a list of elements which do not have an
    /// explicit boolean operator defined between them. It can be used to express a
    /// boolean query that intends to use the default boolean operator.
    /// </summary>
    public class BooleanQueryNode : QueryNode
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clauses">the query nodes to be and'ed</param>
        public BooleanQueryNode(IList<IQueryNode> clauses)
        {
            IsLeaf = false;
            Allocate();
            Set(clauses);
        }

        public override string ToString()
        {
            var children = GetChildren();
            if (children is null || children.Count == 0)
                return "<boolean operation='default'/>";
            StringBuilder sb = new StringBuilder();
            sb.Append("<boolean operation='default'>");
            foreach (IQueryNode child in children)
            {
                sb.Append("\n");
                sb.Append(child.ToString());
            }
            sb.Append("\n</boolean>");
            return sb.ToString();
        }

        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            var children = GetChildren();
            if (children is null || children.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder();
            string filler = "";
            foreach (IQueryNode child in children)
            {
                sb.Append(filler).Append(child.ToQueryString(escapeSyntaxParser));
                filler = " ";
            }

            // in case is root or the parent is a group node avoid parenthesis
            if ((Parent != null && Parent is GroupQueryNode)
                || IsRoot)
                return sb.ToString();
            else
                return "( " + sb.ToString() + " )";
        }

        public override IQueryNode CloneTree()
        {
            BooleanQueryNode clone = (BooleanQueryNode)base.CloneTree();

            // nothing to do here

            return clone;
        }
    }
}
