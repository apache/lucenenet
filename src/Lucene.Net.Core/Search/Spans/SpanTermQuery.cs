using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Search.Spans
{
    using System;

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
    using Bits = Lucene.Net.Util.Bits;
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
        protected Term term; // LUCENENET TODO: rename

        /// <summary>
        /// Construct a SpanTermQuery matching the named term's spans. </summary>
        public SpanTermQuery(Term term)
        {
            this.term = term;
        }

        /// <summary>
        /// Return the term whose spans are matched. </summary>
        public virtual Term Term
        {
            get
            {
                return term;
            }
        }

        public override string Field
        {
            get
            {
                return term.Field;
            }
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            terms.Add(term);
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            if (term.Field.Equals(field))
            {
                buffer.Append(term.Text());
            }
            else
            {
                buffer.Append(term.ToString());
            }
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((term == null) ? 0 : term.GetHashCode());
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
            if (term == null)
            {
                if (other.term != null)
                {
                    return false;
                }
            }
            else if (!term.Equals(other.term))
            {
                return false;
            }
            return true;
        }

        public override Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            TermContext termContext;
            termContexts.TryGetValue(term, out termContext);
            TermState state;
            if (termContext == null)
            {
                // this happens with span-not query, as it doesn't include the NOT side in extractTerms()
                // so we seek to the term now in this segment..., this sucks because its ugly mostly!
                Fields fields = context.AtomicReader.Fields;
                if (fields != null)
                {
                    Terms terms = fields.Terms(term.Field);
                    if (terms != null)
                    {
                        TermsEnum termsEnum = terms.Iterator(null);
                        if (termsEnum.SeekExact(term.Bytes))
                        {
                            state = termsEnum.TermState();
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

            if (state == null) // term is not present in that reader
            {
                return TermSpans.EMPTY_TERM_SPANS;
            }

            TermsEnum termsEnum_ = context.AtomicReader.Terms(term.Field).Iterator(null);
            termsEnum_.SeekExact(term.Bytes, state);

            DocsAndPositionsEnum postings = termsEnum_.DocsAndPositions(acceptDocs, null, DocsAndPositionsEnum.FLAG_PAYLOADS);

            if (postings != null)
            {
                return new TermSpans(postings, term);
            }
            else
            {
                // term does exist, but has no positions
                throw new InvalidOperationException("field \"" + term.Field + "\" was indexed without position data; cannot run SpanTermQuery (term=" + term.Text() + ")");
            }
        }
    }
}