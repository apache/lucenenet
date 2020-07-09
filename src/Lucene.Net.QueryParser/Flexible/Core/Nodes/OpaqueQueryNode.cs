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
    /// A <see cref="OpaqueQueryNode"/> is used for specify values that are not supposed to
    /// be parsed by the parser. For example: and XPATH query in the middle of a
    /// query string a b @xpath:'/bookstore/book[1]/title' c d
    /// </summary>
    public class OpaqueQueryNode : QueryNode
    {
        private string schema = null;

        private string value = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="schema">schema identifier</param>
        /// <param name="value">value that was not parsed</param>
        public OpaqueQueryNode(string schema, string value)
        {
            this.IsLeaf = true;

            this.schema = schema;
            this.value = value;
        }

        public override string ToString()
        {
            return "<opaque schema='" + this.schema + "' value='" + this.value + "'/>";
        }

        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            return "@" + this.schema + ":'" + this.value + "'";
        }

        public override IQueryNode CloneTree()
        {
            OpaqueQueryNode clone = (OpaqueQueryNode)base.CloneTree();

            clone.schema = this.schema;
            clone.value = this.value;

            return clone;
        }

        /// <summary>
        /// Gets the schema
        /// </summary>
        public virtual string Schema => this.schema;

        /// <summary>
        /// Gets the value
        /// </summary>
        public virtual string Value => this.value;
    }
}
