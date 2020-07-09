using J2N.Text;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Util;
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
    /// A <see cref="RegexpQueryNode"/> represents <see cref="Search.RegexpQuery"/> query Examples: /[a-z]|[0-9]/
    /// </summary>
    public class RegexpQueryNode : QueryNode, ITextableQueryNode, IFieldableNode
    {
        private ICharSequence text;
        private string field;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">field name</param>
        /// <param name="text">value that contains a regular expression</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
        // LUCENENET specific overload for passing text as string
        public RegexpQueryNode(string field, string text, int begin,
            int end)
            : this(field, text.AsCharSequence(), begin, end)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">field name</param>
        /// <param name="text">value that contains a regular expression</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
        // LUCENENET specific overload for passing text as StringBuilder
        public RegexpQueryNode(string field, StringBuilder text, int begin,
            int end)
            : this(field, text.AsCharSequence(), begin, end)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">field name</param>
        /// <param name="text">value that contains a regular expression</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
        public RegexpQueryNode(string field, ICharSequence text, int begin,
            int end) // LUCENENET TODO: API - Change to use length rather than end index to match .NET
        {
            this.field = field;
            this.text = text.Subsequence(begin, end - begin);
        }

        public virtual BytesRef TextToBytesRef()
        {
            return new BytesRef(text.ToString());
        }

        public override string ToString()
        {
            return "<regexp field='" + this.field + "' term='" + this.text + "'/>";
        }

        public override IQueryNode CloneTree()
        {
            RegexpQueryNode clone = (RegexpQueryNode)base.CloneTree();
            clone.field = this.field;
            clone.text = this.text;
            return clone;
        }

        public virtual ICharSequence Text
        {
            get => text;
            set => this.text = value;
        }

        public virtual string Field
        {
            get => field;
            set => this.field = value;
        }

        public virtual string GetFieldAsString()
        {
            return field.ToString();
        }

        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            return IsDefaultField(field) ? "/" + text + "/" : field + ":/" + text + "/";
        }
    }
}
