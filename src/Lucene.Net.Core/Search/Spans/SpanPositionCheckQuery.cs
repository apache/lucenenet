using System.Collections.Generic;

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
    using Bits = Lucene.Net.Util.Bits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;

    /// <summary>
    /// Base class for filtering a SpanQuery based on the position of a match.
    ///
    /// </summary>
    public abstract class SpanPositionCheckQuery : SpanQuery
    {
        protected SpanQuery m_match;

        public SpanPositionCheckQuery(SpanQuery match)
        {
            this.m_match = match;
        }

        /// <returns> the SpanQuery whose matches are filtered.
        ///
        ///  </returns>
        public virtual SpanQuery Match
        {
            get
            {
                return m_match;
            }
        }

        public override string Field
        {
            get
            {
                return m_match.Field;
            }
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            m_match.ExtractTerms(terms);
        }

        /// <summary>
        /// Return value for <seealso cref="SpanPositionCheckQuery#acceptPosition(Spans)"/>.
        /// </summary>
        protected internal enum AcceptStatus
        {
            /// <summary>
            /// Indicates the match should be accepted </summary>
            YES,

            /// <summary>
            /// Indicates the match should be rejected </summary>
            NO,

            /// <summary>
            /// Indicates the match should be rejected, and the enumeration should advance
            /// to the next document.
            /// </summary>
            NO_AND_ADVANCE
        }

        /// <summary>
        /// Implementing classes are required to return whether the current position is a match for the passed in
        /// "match" <seealso cref="Lucene.Net.Search.Spans.SpanQuery"/>.
        ///
        /// this is only called if the underlying <seealso cref="Lucene.Net.Search.Spans.Spans#next()"/> for the
        /// match is successful
        ///
        /// </summary>
        /// <param name="spans"> The <seealso cref="Lucene.Net.Search.Spans.Spans"/> instance, positioned at the spot to check </param>
        /// <returns> whether the match is accepted, rejected, or rejected and should move to the next doc.
        /// </returns>
        /// <seealso cref= Lucene.Net.Search.Spans.Spans#next()
        ///  </seealso>
        protected abstract AcceptStatus AcceptPosition(Spans spans);

        public override Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            return new PositionCheckSpan(this, context, acceptDocs, termContexts);
        }

        public override Query Rewrite(IndexReader reader)
        {
            SpanPositionCheckQuery clone = null;

            var rewritten = (SpanQuery)m_match.Rewrite(reader);
            if (rewritten != m_match)
            {
                clone = (SpanPositionCheckQuery)this.Clone();
                clone.m_match = rewritten;
            }

            if (clone != null)
            {
                return clone; // some clauses rewrote
            }
            else
            {
                return this; // no clauses rewrote
            }
        }

        protected class PositionCheckSpan : Spans
        {
            private readonly SpanPositionCheckQuery outerInstance;

            private Spans spans;

            public PositionCheckSpan(SpanPositionCheckQuery outerInstance, AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
            {
                this.outerInstance = outerInstance;
                spans = outerInstance.m_match.GetSpans(context, acceptDocs, termContexts);
            }

            public override bool Next()
            {
                if (!spans.Next())
                {
                    return false;
                }

                return DoNext();
            }

            public override bool SkipTo(int target)
            {
                if (!spans.SkipTo(target))
                {
                    return false;
                }

                return DoNext();
            }

            protected internal virtual bool DoNext()
            {
                for (; ; )
                {
                    switch (outerInstance.AcceptPosition(this))
                    {
                        case AcceptStatus.YES:
                            return true;

                        case AcceptStatus.NO:
                            if (!spans.Next())
                            {
                                return false;
                            }
                            break;

                        case AcceptStatus.NO_AND_ADVANCE:
                            if (!spans.SkipTo(spans.Doc + 1))
                            {
                                return false;
                            }
                            break;
                    }
                }
            }

            public override int Doc
            {
                get { return spans.Doc; }
            }

            public override int Start
            {
                get { return spans.Start; }
            }

            public override int End
            // TODO: Remove warning after API has been finalized
            {
                get { return spans.End; }
            }

            public override ICollection<byte[]> Payload
            {
                get
                {
                    List<byte[]> result = null;
                    if (spans.IsPayloadAvailable)
                    {
                        result = new List<byte[]>(spans.Payload);
                    }
                    return result; //TODO: any way to avoid the new construction?
                }
            }

            // TODO: Remove warning after API has been finalized

            public override bool IsPayloadAvailable
            {
                get
                {
                    return spans.IsPayloadAvailable;
                }
            }

            public override long Cost()
            {
                return spans.Cost();
            }

            public override string ToString()
            {
                return "spans(" + outerInstance.ToString() + ")";
            }
        }
    }
}