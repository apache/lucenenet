using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Search.Spans
{
    using Lucene.Net.Support;

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
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// Removes matches which overlap with another SpanQuery or
    /// within a x tokens before or y tokens after another SpanQuery.
    /// </summary>
    public class SpanNotQuery : SpanQuery
    {
        private SpanQuery include;
        private SpanQuery exclude;
        private readonly int Pre;
        private readonly int Post;

        /// <summary>
        /// Construct a SpanNotQuery matching spans from <code>include</code> which
        /// have no overlap with spans from <code>exclude</code>.
        /// </summary>
        public SpanNotQuery(SpanQuery include, SpanQuery exclude)
            : this(include, exclude, 0, 0)
        {
        }

        /// <summary>
        /// Construct a SpanNotQuery matching spans from <code>include</code> which
        /// have no overlap with spans from <code>exclude</code> within
        /// <code>dist</code> tokens of <code>include</code>.
        /// </summary>
        public SpanNotQuery(SpanQuery include, SpanQuery exclude, int dist)
            : this(include, exclude, dist, dist)
        {
        }

        /// <summary>
        /// Construct a SpanNotQuery matching spans from <code>include</code> which
        /// have no overlap with spans from <code>exclude</code> within
        /// <code>pre</code> tokens before or <code>post</code> tokens of <code>include</code>.
        /// </summary>
        public SpanNotQuery(SpanQuery include, SpanQuery exclude, int pre, int post)
        {
            this.include = include;
            this.exclude = exclude;
            this.Pre = (pre >= 0) ? pre : 0;
            this.Post = (post >= 0) ? post : 0;

            if (include.Field != null && exclude.Field != null && !include.Field.Equals(exclude.Field))
            {
                throw new System.ArgumentException("Clauses must have same field.");
            }
        }

        /// <summary>
        /// Return the SpanQuery whose matches are filtered. </summary>
        public virtual SpanQuery Include
        {
            get
            {
                return include;
            }
        }

        /// <summary>
        /// Return the SpanQuery whose matches must not overlap those returned. </summary>
        public virtual SpanQuery Exclude
        {
            get
            {
                return exclude;
            }
        }

        public override string Field
        {
            get
            {
                return include.Field;
            }
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            include.ExtractTerms(terms);
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("spanNot(");
            buffer.Append(include.ToString(field));
            buffer.Append(", ");
            buffer.Append(exclude.ToString(field));
            buffer.Append(", ");
            buffer.Append(Convert.ToString(Pre));
            buffer.Append(", ");
            buffer.Append(Convert.ToString(Post));
            buffer.Append(")");
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        public override object Clone()
        {
            SpanNotQuery spanNotQuery = new SpanNotQuery((SpanQuery)include.Clone(), (SpanQuery)exclude.Clone(), Pre, Post);
            spanNotQuery.Boost = Boost;
            return spanNotQuery;
        }

        public override Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            return new SpansAnonymousInnerClassHelper(this, context, acceptDocs, termContexts);
        }

        private class SpansAnonymousInnerClassHelper : Spans
        {
            private readonly SpanNotQuery OuterInstance;

            private AtomicReaderContext Context;
            private Bits AcceptDocs;
            private IDictionary<Term, TermContext> TermContexts;

            public SpansAnonymousInnerClassHelper(SpanNotQuery outerInstance, AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
            {
                this.OuterInstance = outerInstance;
                this.Context = context;
                this.AcceptDocs = acceptDocs;
                this.TermContexts = termContexts;
                includeSpans = outerInstance.include.GetSpans(context, acceptDocs, termContexts);
                moreInclude = true;
                excludeSpans = outerInstance.exclude.GetSpans(context, acceptDocs, termContexts);
                moreExclude = excludeSpans.Next();
            }

            private Spans includeSpans;
            private bool moreInclude;

            private Spans excludeSpans;
            private bool moreExclude;

            public override bool Next()
            {
                if (moreInclude) // move to next include
                {
                    moreInclude = includeSpans.Next();
                }

                while (moreInclude && moreExclude)
                {
                    if (includeSpans.Doc() > excludeSpans.Doc()) // skip exclude
                    {
                        moreExclude = excludeSpans.SkipTo(includeSpans.Doc());
                    }

                    while (moreExclude && includeSpans.Doc() == excludeSpans.Doc() && excludeSpans.End() <= includeSpans.Start() - OuterInstance.Pre) // while exclude is before
                    {
                        moreExclude = excludeSpans.Next(); // increment exclude
                    }

                    if (!moreExclude || includeSpans.Doc() != excludeSpans.Doc() || includeSpans.End() + OuterInstance.Post <= excludeSpans.Start()) // if no intersection
                    {
                        break; // we found a match
                    }

                    moreInclude = includeSpans.Next(); // intersected: keep scanning
                }
                return moreInclude;
            }

            public override bool SkipTo(int target)
            {
                if (moreInclude) // skip include
                {
                    moreInclude = includeSpans.SkipTo(target);
                }

                if (!moreInclude)
                {
                    return false;
                }

                if (moreExclude && includeSpans.Doc() > excludeSpans.Doc()) // skip exclude
                {
                    moreExclude = excludeSpans.SkipTo(includeSpans.Doc());
                }

                while (moreExclude && includeSpans.Doc() == excludeSpans.Doc() && excludeSpans.End() <= includeSpans.Start() - OuterInstance.Pre) // while exclude is before
                {
                    moreExclude = excludeSpans.Next(); // increment exclude
                }

                if (!moreExclude || includeSpans.Doc() != excludeSpans.Doc() || includeSpans.End() + OuterInstance.Post <= excludeSpans.Start()) // if no intersection
                {
                    return true; // we found a match
                }

                return Next(); // scan to next match
            }

            public override int Doc()
            {
                return includeSpans.Doc();
            }

            public override int Start()
            {
                return includeSpans.Start();
            }

            public override int End()
            // TODO: Remove warning after API has been finalized
            {
                return includeSpans.End();
            }

            public override ICollection<byte[]> Payload
            {
                get
                {
                    List<byte[]> result = null;
                    if (includeSpans.PayloadAvailable)
                    {
                        result = new List<byte[]>(includeSpans.Payload);
                    }
                    return result;
                }
            }

            // TODO: Remove warning after API has been finalized
            public override bool PayloadAvailable
            {
                get
                {
                    return includeSpans.PayloadAvailable;
                }
            }

            public override long Cost()
            {
                return includeSpans.Cost();
            }

            public override string ToString()
            {
                return "spans(" + OuterInstance.ToString() + ")";
            }
        }

        public override Query Rewrite(IndexReader reader)
        {
            SpanNotQuery clone = null;

            var rewrittenInclude = (SpanQuery)include.Rewrite(reader);
            if (rewrittenInclude != include)
            {
                clone = (SpanNotQuery)this.Clone();
                clone.include = rewrittenInclude;
            }
            var rewrittenExclude = (SpanQuery)exclude.Rewrite(reader);
            if (rewrittenExclude != exclude)
            {
                if (clone == null)
                {
                    clone = (SpanNotQuery)this.Clone();
                }
                clone.exclude = rewrittenExclude;
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

        /// <summary>
        /// Returns true iff <code>o</code> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (!base.Equals(o))
            {
                return false;
            }

            SpanNotQuery other = (SpanNotQuery)o;
            return this.include.Equals(other.include) && this.exclude.Equals(other.exclude) && this.Pre == other.Pre && this.Post == other.Post;
        }

        public override int GetHashCode()
        {
            int h = base.GetHashCode();
            h = Number.RotateLeft(h, 1);
            h ^= include.GetHashCode();
            h = Number.RotateLeft(h, 1);
            h ^= exclude.GetHashCode();
            h = Number.RotateLeft(h, 1);
            h ^= Pre;
            h = Number.RotateLeft(h, 1);
            h ^= Post;
            return h;
        }
    }
}