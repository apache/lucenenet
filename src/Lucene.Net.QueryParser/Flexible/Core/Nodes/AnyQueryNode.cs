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
    /// A <see cref="AnyQueryNode"/> represents an ANY operator performed on a list of
    /// nodes.
    /// </summary>
    public class AnyQueryNode : AndQueryNode
    {
        private string field = null;
        private int minimumMatchingmElements = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clauses">the query nodes to be or'ed</param>
        /// <param name="field"></param>
        /// <param name="minimumMatchingElements"></param>
        public AnyQueryNode(IList<IQueryNode> clauses, string field,
            int minimumMatchingElements)
            : base(clauses)
        {
            this.field = field;
            this.minimumMatchingmElements = minimumMatchingElements;

            if (clauses != null)
            {
                foreach (IQueryNode clause in clauses)
                {
                    if (clause is FieldQueryNode)
                    {
                        if (clause is QueryNode queryNode)
                            queryNode.m_toQueryStringIgnoreFields = true;

                        if (clause is IFieldableNode fieldableNode)
                            fieldableNode.Field = field;
                    }
                }
            }
        }

        public virtual int MinimumMatchingElements => this.minimumMatchingmElements;

        /// <summary>
        /// Gets or sets the field name. Returns null if the field was not specified.
        /// </summary>
        public virtual string Field
        {
            get => this.field;
            set => this.field = value;
        }


        // LUCENENET TODO: No need for GetFieldAsString method because
        // field is already type string
        /// <summary>
        /// null if the field was not specified
        /// </summary>
        /// <returns>the field as a <see cref="string"/></returns>
        public virtual string GetFieldAsString()
        {
            if (this.field is null)
                return null;
            else
                return this.field.ToString();
        }

        public override IQueryNode CloneTree()
        {
            AnyQueryNode clone = (AnyQueryNode)base.CloneTree();

            clone.field = this.field;
            clone.minimumMatchingmElements = this.minimumMatchingmElements;

            return clone;
        }

        public override string ToString()
        {
            var children = GetChildren();
            if (children is null || children.Count == 0)
                return "<any field='" + this.field + "'  matchelements="
                    + this.minimumMatchingmElements + "/>";
            StringBuilder sb = new StringBuilder();
            sb.Append("<any field='" + this.field + "'  matchelements="
                + this.minimumMatchingmElements + ">");
            foreach (IQueryNode clause in children)
            {
                sb.Append("\n");
                sb.Append(clause.ToString());
            }
            sb.Append("\n</any>");
            return sb.ToString();
        }

        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            string anySTR = "ANY " + this.minimumMatchingmElements;

            StringBuilder sb = new StringBuilder();
            var children = GetChildren();
            if (children is null || children.Count == 0)
            {
                // no childs case
            }
            else
            {
                string filler = "";
                foreach (IQueryNode clause in children)
                {
                    sb.Append(filler).Append(clause.ToQueryString(escapeSyntaxParser));
                    filler = " ";
                }
            }

            if (IsDefaultField(this.field))
            {
                return "( " + sb.ToString() + " ) " + anySTR;
            }
            else
            {
                return this.field + ":(( " + sb.ToString() + " ) " + anySTR + ")";
            }
        }
    }
}
