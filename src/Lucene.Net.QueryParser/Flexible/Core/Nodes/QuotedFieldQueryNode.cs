using J2N.Text;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
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
    /// A <see cref="QuotedFieldQueryNode"/> represents phrase query. Example:
    /// "life is great"
    /// </summary>
    public class QuotedFieldQueryNode : FieldQueryNode
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">field name</param>
        /// <param name="text">value</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
        // LUCENENET specific overload for string text
        public QuotedFieldQueryNode(string field, string text, int begin,
            int end)
            : this(field, text.AsCharSequence(), begin, end)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">field name</param>
        /// <param name="text">value</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
        // LUCENENET specific overload for StringBuilder text
        public QuotedFieldQueryNode(string field, StringBuilder text, int begin,
            int end)
            : this(field, text.AsCharSequence(), begin, end)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">field name</param>
        /// <param name="text">value</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
        public QuotedFieldQueryNode(string field, ICharSequence text, int begin,
            int end)
            : base(field, text, begin, end)
        {
        }

        public override string ToQueryString(IEscapeQuerySyntax escaper)
        {
            if (IsDefaultField(this.m_field))
            {
                return "\"" + GetTermEscapeQuoted(escaper) + "\"";
            }
            else
            {
                return this.m_field + ":" + "\"" + GetTermEscapeQuoted(escaper) + "\"";
            }
        }

        public override string ToString()
        {
            return "<quotedfield start='" + this.m_begin + "' end='" + this.m_end
                + "' field='" + this.m_field + "' term='" + this.m_text + "'/>";
        }

        public override IQueryNode CloneTree()
        {
            QuotedFieldQueryNode clone = (QuotedFieldQueryNode)base.CloneTree();
            // nothing to do here
            return clone;
        }
    }
}
