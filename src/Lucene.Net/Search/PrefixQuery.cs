using System;
using System.Text;

namespace Lucene.Net.Search
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

    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// A <see cref="Query"/> that matches documents containing terms with a specified prefix. A <see cref="PrefixQuery"/>
    /// is built by QueryParser for input like <c>app*</c>.
    ///
    /// <para/>This query uses the
    /// <see cref="MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT"/>
    /// rewrite method.
    /// </summary>
    public class PrefixQuery : MultiTermQuery
    {
        private readonly Term _prefix;

        /// <summary>
        /// Constructs a query for terms starting with <paramref name="prefix"/>. </summary>
        public PrefixQuery(Term prefix)
            : base(prefix.Field)
        {
            this._prefix = prefix;
        }

        /// <summary>
        /// Returns the prefix of this query. </summary>
        public virtual Term Prefix => _prefix;

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            TermsEnum tenum = terms.GetEnumerator();

            if (_prefix.Bytes.Length == 0)
            {
                // no prefix -- match all terms for this field:
                return tenum;
            }
            return new PrefixTermsEnum(tenum, _prefix.Bytes);
        }

        /// <summary>
        /// Prints a user-readable version of this query. </summary>
        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            if (!Field.Equals(field, StringComparison.Ordinal))
            {
                buffer.Append(Field);
                buffer.Append(':');
            }
            buffer.Append(_prefix.Text);
            buffer.Append('*');
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((_prefix is null) ? 0 : _prefix.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            PrefixQuery other = (PrefixQuery)obj;
            if (_prefix is null)
            {
                if (other._prefix != null)
                {
                    return false;
                }
            }
            else if (!_prefix.Equals(other._prefix))
            {
                return false;
            }
            return true;
        }
    }
}