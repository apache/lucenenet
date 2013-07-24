using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Spans
{
    /// <summary>
    /// Base class for filtering a SpanQuery based on the position of a match.
    /// </summary>
    public abstract class SpanPositionCheckQuery : SpanQuery, ICloneable
    {
        protected SpanQuery match;

        protected SpanPositionCheckQuery(SpanQuery match)
        {
            this.match = match;
        }

        public SpanQuery Match { get { return match; } }

        public override string Field { get { return match.Field; } }

        public override void ExtractTerms(ISet<Term> terms)
        {
            match.ExtractTerms(terms);
        }
        
        protected enum AcceptStatus
        {
            /// <summary>
            /// Indicates the match should be accepted.
            /// </summary>
            YES,

            /// <summary>
            /// Indicates the match should be rejected.
            /// </summary>
            NO,

            /// <summary>
            /// Indicates the match should be rejected, and the enumeration should advance
            /// to the next document.
            /// </summary>
            NO_AND_ADVANCE
        }

        protected abstract AcceptStatus AcceptPosition(SpansBase spans);

        public override SpansBase GetSpans(AtomicReaderContext context, IBits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            return new PositionCheckSpan(context, acceptDocs, termContexts);
        }

        public override Query Rewrite(IndexReader reader)
        {
            SpanPositionCheckQuery clone = null;

            var rewritten = (SpanQuery) Match.Rewrite(reader);
            if (rewritten != Match)
            {
                clone = (SpanPositionCheckQuery) this.Clone();
                clone.match = rewritten;
            }

            if (clone != null)
            {
                return clone;
            }
            else
            {
                return this;
            }
        }

        protected class PositionCheckSpan : SpansBase
        {
            private SpansBase spans;

            private SpanPositionCheckQuery parent;

            public PositionCheckSpan(AtomicReaderContext context, IBits acceptDocs, IDictionary<Term, TermContext> termContexts, SpanPositionCheckQuery parent)
            {
                this.parent = parent;
                spans = parent.Match.GetSpans(context, acceptDocs, termContexts);
            }

            public override bool Next()
            {
                return spans.Next() && DoNext();
            }

            public override bool SkipTo(int target)
            {
                return spans.SkipTo(target) && DoNext();
            }

            public bool DoNext()
            {
                for (;;)
                {
                    switch (parent.AcceptPosition(this))
                    {
                        case AcceptStatus.YES:
                            return true;
                        case AcceptStatus.NO:
                            if (!spans.Next())
                                return false;
                            break;
                        case AcceptStatus.NO_AND_ADVANCE:
                            if (!spans.SkipTo(spans.Doc + 1))
                                return false;
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
            {
                get { return spans.End; }
            }

            public override ICollection<sbyte[]> GetPayload()
            {
                List<sbyte[]> result = null;
                if (spans.IsPayloadAvailable())
                {
                    result = new List<sbyte[]>(spans.GetPayload());
                }
                return result;
            }

            public override bool IsPayloadAvailable()
            {
                return spans.IsPayloadAvailable();
            }

            public override long Cost
            {
                get
                {
                    return spans.Cost;
                }
            }

            public override string ToString()
            {
                return "spans(" + parent.ToString() + ")";
            }
        }
    }
}
