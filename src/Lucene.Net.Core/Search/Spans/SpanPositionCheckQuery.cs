using System;
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
        protected internal SpanQuery match;

        public SpanPositionCheckQuery(SpanQuery match)
        {
            this.match = match;
        }

        /// <returns> the SpanQuery whose matches are filtered.
        ///
        ///  </returns>
        public virtual SpanQuery Match
        {
            get
            {
                return match;
            }
        }

        public override string Field
        {
            get
            {
                return match.Field;
            }
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            match.ExtractTerms(terms);
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
        protected internal abstract AcceptStatus AcceptPosition(Spans spans);

        public override Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            return new PositionCheckSpan(this, context, acceptDocs, termContexts);
        }

        public override Query Rewrite(IndexReader reader)
        {
            SpanPositionCheckQuery clone = null;

            var rewritten = (SpanQuery)match.Rewrite(reader);
            if (rewritten != match)
            {
                clone = (SpanPositionCheckQuery)this.Clone();
                clone.match = rewritten;
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

        protected internal class PositionCheckSpan : Spans
        {
            private readonly SpanPositionCheckQuery OuterInstance;

            internal Spans Spans;

            public PositionCheckSpan(SpanPositionCheckQuery outerInstance, AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
            {
                this.OuterInstance = outerInstance;
                Spans = outerInstance.match.GetSpans(context, acceptDocs, termContexts);
            }

            public override bool Next()
            {
                if (!Spans.Next())
                {
                    return false;
                }

                return DoNext();
            }

            public override bool SkipTo(int target)
            {
                if (!Spans.SkipTo(target))
                {
                    return false;
                }

                return DoNext();
            }

            protected internal virtual bool DoNext()
            {
                for (; ; )
                {
                    switch (OuterInstance.AcceptPosition(this))
                    {
                        case AcceptStatus.YES:
                            return true;

                        case AcceptStatus.NO:
                            if (!Spans.Next())
                            {
                                return false;
                            }
                            break;

                        case AcceptStatus.NO_AND_ADVANCE:
                            if (!Spans.SkipTo(Spans.Doc() + 1))
                            {
                                return false;
                            }
                            break;
                    }
                }
            }

            public override int Doc()
            {
                return Spans.Doc();
            }

            public override int Start()
            {
                return Spans.Start();
            }

            public override int End()
            // TODO: Remove warning after API has been finalized
            {
                return Spans.End();
            }

            public override ICollection<byte[]> Payload
            {
                get
                {
                    List<byte[]> result = null;
                    if (Spans.PayloadAvailable)
                    {
                        result = new List<byte[]>(Spans.Payload);
                    }
                    return result; //TODO: any way to avoid the new construction?
                }
            }

            // TODO: Remove warning after API has been finalized

            public override bool PayloadAvailable
            {
                get
                {
                    return Spans.PayloadAvailable;
                }
            }

            public override long Cost()
            {
                return Spans.Cost();
            }

            public override string ToString()
            {
                return "spans(" + OuterInstance.ToString() + ")";
            }
        }
    }
}