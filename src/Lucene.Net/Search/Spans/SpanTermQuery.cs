using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Search.Spans
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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IBits = Lucene.Net.Util.IBits;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using Fields = Lucene.Net.Index.Fields;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using TermState = Lucene.Net.Index.TermState;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// Matches spans containing a term. </summary>
    public class SpanTermQuery : SpanQuery
    {
        protected Term m_term;

        /// <summary>
        /// Construct a <see cref="SpanTermQuery"/> matching the named term's spans. </summary>
        public SpanTermQuery(Term term)
        {
            this.m_term = term;
        }

        /// <summary>
        /// Return the term whose spans are matched. </summary>
        public virtual Term Term => m_term;

        public override string Field => m_term.Field;

        public override void ExtractTerms(ISet<Term> terms)
        {
            terms.Add(m_term);
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            if (m_term.Field.Equals(field, StringComparison.Ordinal))
            {
                buffer.Append(m_term.Text);
            }
            else
            {
                buffer.Append(m_term.ToString());
            }
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((m_term is null) ? 0 : m_term.GetHashCode());
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
            SpanTermQuery other = (SpanTermQuery)obj;
            if (m_term is null)
            {
                if (other.m_term != null)
                {
                    return false;
                }
            }
            else if (!m_term.Equals(other.m_term))
            {
                return false;
            }
            return true;
        }

        public override Spans GetSpans(AtomicReaderContext context, IBits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            TermState state;
            if (!termContexts.TryGetValue(m_term, out TermContext termContext) || termContext is null)
            {
                // this happens with span-not query, as it doesn't include the NOT side in extractTerms()
                // so we seek to the term now in this segment..., this sucks because its ugly mostly!
                Fields fields = context.AtomicReader.Fields;
                if (fields != null)
                {
                    Terms terms = fields.GetTerms(m_term.Field);
                    if (terms != null)
                    {
                        TermsEnum termsEnum = terms.GetEnumerator();
                        if (termsEnum.SeekExact(m_term.Bytes))
                        {
                            state = termsEnum.GetTermState();
                        }
                        else
                        {
                            state = null;
                        }
                    }
                    else
                    {
                        state = null;
                    }
                }
                else
                {
                    state = null;
                }
            }
            else
            {
                state = termContext.Get(context.Ord);
            }

            if (state is null) // term is not present in that reader
            {
                return TermSpans.EMPTY_TERM_SPANS;
            }

            TermsEnum termsEnum_ = context.AtomicReader.GetTerms(m_term.Field).GetEnumerator();
            termsEnum_.SeekExact(m_term.Bytes, state);

            DocsAndPositionsEnum postings = termsEnum_.DocsAndPositions(acceptDocs, null, DocsAndPositionsFlags.PAYLOADS);

            if (postings != null)
            {
                return new TermSpans(postings, m_term);
            }
            else
            {
                // term does exist, but has no positions
                throw IllegalStateException.Create("field \"" + m_term.Field + "\" was indexed without position data; cannot run SpanTermQuery (term=" + m_term.Text + ")");
            }
        }
    }
}