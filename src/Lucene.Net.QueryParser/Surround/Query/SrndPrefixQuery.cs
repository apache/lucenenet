using Lucene.Net.Index;
using Lucene.Net.Util;
using System.Text;

namespace Lucene.Net.QueryParsers.Surround.Query
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
    /// Query that matches string prefixes
    /// </summary>
    public class SrndPrefixQuery : SimpleTerm
    {
        private readonly BytesRef prefixRef;
        public SrndPrefixQuery(string prefix, bool quoted, char truncator)
            : base(quoted)
        {
            this.prefix = prefix;
            prefixRef = new BytesRef(prefix);
            this.truncator = truncator;
        }

        private readonly string prefix;
        public virtual string Prefix => prefix;

        private readonly char truncator;
        public virtual char SuffixOperator => truncator;

        public virtual Term GetLucenePrefixTerm(string fieldName)
        {
            return new Term(fieldName, Prefix);
        }

        public override string ToStringUnquoted()
        {
            return Prefix;
        }

        protected override void SuffixToString(StringBuilder r)
        {
            r.Append(SuffixOperator);
        }

        public override void VisitMatchingTerms(IndexReader reader, string fieldName, IMatchingTermVisitor mtv)
        {
            /* inspired by PrefixQuery.rewrite(): */
            Terms terms = MultiFields.GetTerms(reader, fieldName);
            if (terms != null)
            {
                TermsEnum termsEnum = terms.GetEnumerator();

                bool skip = false;
                TermsEnum.SeekStatus status = termsEnum.SeekCeil(new BytesRef(Prefix));
                if (status == TermsEnum.SeekStatus.FOUND)
                {
                    mtv.VisitMatchingTerm(GetLucenePrefixTerm(fieldName));
                }
                else if (status == TermsEnum.SeekStatus.NOT_FOUND)
                {
                    if (StringHelper.StartsWith(termsEnum.Term, prefixRef))
                    {
                        mtv.VisitMatchingTerm(new Term(fieldName, termsEnum.Term.Utf8ToString()));
                    }
                    else
                    {
                        skip = true;
                    }
                }
                else
                {
                    // EOF
                    skip = true;
                }

                if (!skip)
                {
                    while (termsEnum.MoveNext())
                    {
                        BytesRef text = termsEnum.Term;
                        if (StringHelper.StartsWith(text, prefixRef))
                            mtv.VisitMatchingTerm(new Term(fieldName, text.Utf8ToString()));
                        else
                            break;
                    }
                }
            }
        }
    }
}
