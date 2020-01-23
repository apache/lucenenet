using J2N.Text;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
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
    /// A <see cref="PrefixWildcardQueryNode"/> represents wildcardquery that matches abc*
    /// or *. This does not apply to phrases, this is a special case on the original
    /// lucene parser. TODO: refactor the code to remove this special case from the
    /// parser. and probably do it on a Processor
    /// </summary>
    public class PrefixWildcardQueryNode : WildcardQueryNode
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">field name</param>
        /// <param name="text">value including the wildcard</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
        // LUCENENET specific overload for passing text as string
        public PrefixWildcardQueryNode(string field, string text,
            int begin, int end)
            : this(field, text.AsCharSequence(), begin, end)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">field name</param>
        /// <param name="text">value including the wildcard</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
        // LUCENENET specific overload for passing text as StringBuilder
        public PrefixWildcardQueryNode(string field, StringBuilder text,
            int begin, int end)
            : this(field, text.AsCharSequence(), begin, end)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">field name</param>
        /// <param name="text">value including the wildcard</param>
        /// <param name="begin">position in the query string</param>
        /// <param name="end">position in the query string</param>
        public PrefixWildcardQueryNode(string field, ICharSequence text,
            int begin, int end)
            : base(field, text, begin, end)
        {
        }

        public PrefixWildcardQueryNode(FieldQueryNode fqn)
            : this(fqn.Field, fqn.Text, fqn.Begin, fqn.End)
        {
        }

        public override string ToString()
        {
            return "<prefixWildcard field='" + this.m_field + "' term='" + this.m_text
                + "'/>";
        }

        public override IQueryNode CloneTree()
        {
            PrefixWildcardQueryNode clone = (PrefixWildcardQueryNode)base.CloneTree();

            // nothing to do here

            return clone;
        }
    }
}
