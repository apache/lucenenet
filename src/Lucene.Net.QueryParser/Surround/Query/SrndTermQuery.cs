using Lucene.Net.Index;
using Lucene.Net.Util;

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
    /// Simple single-term clause
    /// </summary>
    public class SrndTermQuery : SimpleTerm
    {
        public SrndTermQuery(string termText, bool quoted)
            : base(quoted)
        {
            this.termText = termText;
        }

        private readonly string termText;
        public virtual string TermText => termText;

        public virtual Term GetLuceneTerm(string fieldName)
        {
            return new Term(fieldName, TermText);
        }

        public override string ToStringUnquoted()
        {
            return TermText;
        }

        public override void VisitMatchingTerms(IndexReader reader, string fieldName, IMatchingTermVisitor mtv)
        {
            /* check term presence in index here for symmetry with other SimpleTerm's */
            Terms terms = MultiFields.GetTerms(reader, fieldName);
            if (terms != null)
            {
                TermsEnum termsEnum = terms.GetEnumerator();

                TermsEnum.SeekStatus status = termsEnum.SeekCeil(new BytesRef(TermText));
                if (status == TermsEnum.SeekStatus.FOUND)
                {
                    mtv.VisitMatchingTerm(GetLuceneTerm(fieldName));
                }
            }
        }
    }
}
