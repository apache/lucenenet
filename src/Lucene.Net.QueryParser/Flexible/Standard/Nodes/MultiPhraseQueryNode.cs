using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
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
    /// A <see cref="MultiPhraseQueryNode"/> indicates that its children should be used to
    /// build a <see cref="Search.MultiPhraseQuery"/> instead of <see cref="Search.PhraseQuery"/>.
    /// </summary>
    public class MultiPhraseQueryNode : QueryNode, IFieldableNode
    {
        public MultiPhraseQueryNode()
        {
            IsLeaf = false;
            Allocate();
        }

        public override string ToString()
        {
            var children = GetChildren();
            if (children is null || children.Count == 0)
                return "<multiPhrase/>";
            StringBuilder sb = new StringBuilder();
            sb.Append("<multiPhrase>");
            foreach (IQueryNode child in children)
            {
                sb.Append("\n");
                sb.Append(child.ToString());
            }
            sb.Append("\n</multiPhrase>");
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
                filler = ",";
            }

            return "[MTP[" + sb.ToString() + "]]";
        }

        public override IQueryNode CloneTree()
        {
            MultiPhraseQueryNode clone = (MultiPhraseQueryNode)base.CloneTree();

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
