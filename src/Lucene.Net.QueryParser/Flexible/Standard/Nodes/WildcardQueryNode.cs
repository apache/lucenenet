using J2N.Text;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
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
    /// A <see cref="WildcardQueryNode"/> represents wildcard query This does not apply to
    /// phrases. Examples: a*b*c Fl?w? m?ke*g
    /// </summary>
    public class WildcardQueryNode : FieldQueryNode
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">field name</param>
        /// <param name="text">value that contains one or more wild card characters (? or *)</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
        // LUCENENET specific overload for passing text as string
        public WildcardQueryNode(string field, string text, int begin,
            int end)
            : this(field, text.AsCharSequence(), begin, end)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">field name</param>
        /// <param name="text">value that contains one or more wild card characters (? or *)</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
        // LUCENENET specific overload for passing text as StringBuilder
        public WildcardQueryNode(string field, StringBuilder text, int begin,
            int end)
            : this(field, text.AsCharSequence(), begin, end)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">field name</param>
        /// <param name="text">value that contains one or more wild card characters (? or *)</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
        public WildcardQueryNode(string field, ICharSequence text, int begin,
            int end)
            : base(field, text, begin, end)
        {
        }

        public WildcardQueryNode(FieldQueryNode fqn)
            : this(fqn.Field, fqn.Text, fqn.Begin, fqn.End)
        {
        }

        public override string ToQueryString(IEscapeQuerySyntax escaper)
        {
            if (IsDefaultField(this.m_field))
            {
                return this.m_text.ToString();
            }
            else
            {
                return this.m_field + ":" + this.m_text;
            }
        }

        public override string ToString()
        {
            return "<wildcard field='" + this.m_field + "' term='" + this.m_text + "'/>";
        }

        public override IQueryNode CloneTree()
        {
            WildcardQueryNode clone = (WildcardQueryNode)base.CloneTree();

            // nothing to do here

            return clone;
        }
    }
}
