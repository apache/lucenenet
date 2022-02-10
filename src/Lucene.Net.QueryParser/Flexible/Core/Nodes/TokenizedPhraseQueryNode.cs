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
    /// A <see cref="TokenizedPhraseQueryNode"/> represents a node created by a code that
    /// tokenizes/lemmatizes/analyzes.
    /// </summary>
    public class TokenizedPhraseQueryNode : QueryNode, IFieldableNode
    {
        public TokenizedPhraseQueryNode()
        {
            IsLeaf = false;
            Allocate();
        }

        public override string ToString()
        {
            var children = GetChildren();
            if (children is null || children.Count == 0)
                return "<tokenizedphrase/>";
            StringBuilder sb = new StringBuilder();
            sb.Append("<tokenizedtphrase>");
            foreach (IQueryNode child in children)
            {
                sb.Append("\n");
                sb.Append(child.ToString());
            }
            sb.Append("\n</tokenizedphrase>");
            return sb.ToString();
        }

        // This text representation is not re-parseable
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
                filler = ",";
            }

            return "[TP[" + sb.ToString() + "]]";
        }

        public override IQueryNode CloneTree()
        {
            TokenizedPhraseQueryNode clone = (TokenizedPhraseQueryNode)base
                .CloneTree();

            // nothing to do

            return clone;
        }

        public virtual string Field
        {
            get
            {
                IList<IQueryNode> children = GetChildren();

                if (children is null || children.Count == 0)
                {
                    return null;

                }
                else
                {
                    return ((IFieldableNode)children[0]).Field;
                }
            }
            set
            {
                IList<IQueryNode> children = GetChildren();

                if (children != null)
                {
                    foreach (IQueryNode child in children)
                    {

                        if (child is IFieldableNode fieldableNode)
                        {
                            fieldableNode.Field = value;
                        }
                    }
                }
            }
        }
    }
}
