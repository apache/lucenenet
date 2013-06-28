/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Term = Lucene.Net.Index.Term;
using ToStringUtils = Lucene.Net.Util.ToStringUtils;

namespace Lucene.Net.Search.Spans
{

    /// <summary>Matches spans containing a term. </summary>
    [Serializable]
    public class SpanTermQuery : SpanQuery
    {
        protected Term internalTerm;

        /// <summary>Construct a SpanTermQuery matching the named term's spans. </summary>
        public SpanTermQuery(Term term)
        {
            this.internalTerm = term;
        }

        /// <summary>Return the term whose spans are matched. </summary>
        public virtual Term Term
        {
            get { return internalTerm; }
        }

        public override string Field
        {
            get { return internalTerm.Field; }
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            terms.Add(internalTerm);
        }

        public override string ToString(string field)
        {
            var buffer = new StringBuilder();
            if (internalTerm.Field.Equals(field))
                buffer.Append(internalTerm.Text);
            else
            {
                buffer.Append(internalTerm.ToString());
            }
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + ((Term == null) ? 0 : Term.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (!base.Equals(obj))
                return false;
            if (GetType() != obj.GetType())
                return false;
            var other = (SpanTermQuery)obj;
            if (Term == null)
            {
                if (other.Term != null)
                    return false;
            }
            else if (!Term.Equals(other.Term))
                return false;
            return true;
        }

        public override Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            var termContext = termContexts[Term];
            TermState state;
            if (termContext == null)
            {
                // this happens with span-not query, as it doesn't include the NOT side in extractTerms()
                // so we seek to the term now in this segment..., this sucks because its ugly mostly!
                var fields = context.Reader.Fields;
                if (fields != null)
                {
                    var terms = fields.Terms(Term.Field);
                    if (terms != null)
                    {
                        var termsEnum = terms.Iterator(null);
                        if (termsEnum.SeekExact(Term.Bytes, true))
                        {
                            state = termsEnum.TermState;
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
                state = termContext.Get(context.ord);
            }

            if (state == null)
            { // term is not present in that reader
                return TermSpans.EMPTY_TERM_SPANS;
            }

            var termsIter = context.Reader.Terms(Term.Field).Iterator(null);
            termsIter.SeekExact(Term.Bytes, state);

            var postings = termsIter.DocsAndPositions(acceptDocs, null, DocsAndPositionsEnum.FLAG_PAYLOADS);

            if (postings != null)
            {
                return new TermSpans(postings, Term);
            }
            else
            {
                // term does exist, but has no positions
                throw new InvalidOperationException("field \"" + Term.Field + "\" was indexed without position data; cannot run SpanTermQuery (term=" + Term.Text + ")");
            }
        }
    }
}